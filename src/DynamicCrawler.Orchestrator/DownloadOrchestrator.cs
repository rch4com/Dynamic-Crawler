using DynamicCrawler.Core.Configuration;
using DynamicCrawler.Core.Enums;
using DynamicCrawler.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DynamicCrawler.Orchestrator;

/// <summary>다운로드 오케스트레이터 — Channel&lt;DownloadTask&gt; Consumer + DB Poll 이중 처리</summary>
public sealed class DownloadOrchestrator(
    IMediaRepository mediaRepo,
    ISiteRepository siteRepo,
    IMediaDownloader downloader,
    RoundRobinScheduler scheduler,
    CrawlPipeline pipeline,
    IOptions<CrawlerSettings> settings,
    ILogger<DownloadOrchestrator> logger)
{
    private readonly CrawlerSettings _settings = settings.Value;

    /// <summary>1 사이클: Channel에서 미디어를 읽어 다운로드 (없으면 DB poll fallback)</summary>
    public async Task RunCycleAsync(CancellationToken ct)
    {
        // 1단계: Channel에서 즉시 처리 (DownloadTask에 SiteKey, PostExternalId 포함)
        var channelCount = 0;
        while (pipeline.Reader.TryRead(out var task))
        {
            await ProcessMediaAsync(task.Media, task.SiteKey, task.PostExternalId, ct)
                .ConfigureAwait(false);
            channelCount++;
        }

        if (channelCount > 0)
        {
            logger.LogInformation("Channel에서 {Count}건의 미디어 처리 완료", channelCount);
            return;
        }

        // 2단계: Channel이 비어있으면 DB poll fallback
        var sites = await siteRepo.GetActiveSitesAsync(ct).ConfigureAwait(false);
        if (sites.Count == 0) return;

        var emptyCount = 0;
        while (emptyCount < sites.Count && !ct.IsCancellationRequested)
        {
            var siteKey = scheduler.Next();
            if (siteKey is null) break;

            var claimResult = await mediaRepo.ClaimNextAsync(siteKey, _settings.LeaseSeconds, ct)
                .ConfigureAwait(false);

            if (!claimResult.IsSuccess)
            {
                emptyCount++;
                continue;
            }

            emptyCount = 0;
            await ProcessMediaAsync(claimResult.Value!, siteKey, claimResult.Value!.PostId.ToString(), ct)
                .ConfigureAwait(false);
        }
    }

    private async Task ProcessMediaAsync(Core.Models.Media media, string siteKey, string postExternalId, CancellationToken ct)
    {
        var downloadResult = await downloader.DownloadAsync(
            media, siteKey, postExternalId, ct).ConfigureAwait(false);

        if (downloadResult.IsSuccess)
        {
            var result = downloadResult.Value!;
            media.Sha256 = result.Sha256;
            media.ByteSize = result.ByteSize;
            media.ContentType = result.ContentType;
            media.LocalPath = result.LocalPath;
            media.Status = MediaStatus.Downloaded;

            await mediaRepo.UpdateAsync(media, ct).ConfigureAwait(false);
            logger.LogInformation("미디어 다운로드 완료: {Sha256} / {Url}", result.Sha256[..12], media.MediaUrl);
        }
        else
        {
            media.RetryCount++;
            media.Status = media.RetryCount >= _settings.MaxRetryCount
                ? MediaStatus.Failed
                : MediaStatus.PendingDownload;
            media.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, media.RetryCount));

            await mediaRepo.UpdateAsync(media, ct).ConfigureAwait(false);
            logger.LogWarning("미디어 다운로드 실패: {Url} / {Error}", media.MediaUrl, downloadResult.Error);
        }
    }
}
