using DynamicCrawler.Core.Configuration;
using DynamicCrawler.Core.Enums;
using DynamicCrawler.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DynamicCrawler.Orchestrator;

/// <summary>다운로드 오케스트레이터 — claim → download → 완료 처리</summary>
public sealed class DownloadOrchestrator(
    IMediaRepository mediaRepo,
    ISiteRepository siteRepo,
    IMediaDownloader downloader,
    RoundRobinScheduler scheduler,
    IOptions<CrawlerSettings> settings,
    ILogger<DownloadOrchestrator> logger)
{
    private readonly CrawlerSettings _settings = settings.Value;

    /// <summary>1 사이클: 라운드로빈으로 미디어 다운로드</summary>
    public async Task RunCycleAsync(CancellationToken ct)
    {
        var sites = await siteRepo.GetActiveSitesAsync(ct).ConfigureAwait(false);

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
            var media = claimResult.Value!;

            // 게시글 정보 필요 (경로 생성용) — 간소화를 위해 postId 사용
            var downloadResult = await downloader.DownloadAsync(
                media, siteKey, media.PostId.ToString(), ct).ConfigureAwait(false);

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
}
