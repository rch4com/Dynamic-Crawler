using DynamicCrawler.Core.Configuration;
using DynamicCrawler.Core.Enums;
using DynamicCrawler.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DynamicCrawler.Orchestrator;

public sealed class DownloadOrchestrator(
    IMediaRepository mediaRepo,
    IPostRepository postRepo,
    ISiteRepository siteRepo,
    IMediaDownloader downloader,
    RoundRobinScheduler scheduler,
    CrawlPipeline pipeline,
    IOptions<CrawlerSettings> settings,
    ILogger<DownloadOrchestrator> logger)
{
    private readonly CrawlerSettings _settings = settings.Value;

    public async Task RunCycleAsync(CancellationToken ct)
    {
        var maxConcurrency = Math.Max(1, _settings.DefaultMaxDownloads);
        var channelTasks = new List<Task>(maxConcurrency);
        var channelCount = 0;

        while (pipeline.Reader.TryRead(out var task))
        {
            channelTasks.Add(ProcessMediaAsync(task.Media, task.SiteKey, task.PostExternalId, ct));
            channelCount++;

            if (channelTasks.Count >= maxConcurrency)
            {
                await Task.WhenAll(channelTasks).ConfigureAwait(false);
                channelTasks.Clear();
            }
        }

        if (channelTasks.Count > 0)
        {
            await Task.WhenAll(channelTasks).ConfigureAwait(false);
        }

        // 채널만 계속 소비하면 DB 적체가 굶주릴 수 있어 주기적으로 fallback을 강제합니다.
        var cycles = pipeline.IncrementAndGetDbFallbackCycles();
        if (channelCount > 0 && cycles < 5)
        {
            logger.LogInformation("Processed {Count} media items from the channel.", channelCount);
            return;
        }

        pipeline.ResetDbFallbackCycles();

        var sites = await siteRepo.GetActiveSitesAsync(ct).ConfigureAwait(false);
        if (sites.Count == 0)
        {
            return;
        }

        scheduler.SetSiteKeys(sites.Select(site => site.SiteKey));

        var emptyCount = 0;
        var dbTasks = new List<Task>(maxConcurrency);
        while (emptyCount < sites.Count && !ct.IsCancellationRequested)
        {
            var siteKey = scheduler.Next();
            if (siteKey is null)
            {
                break;
            }

            var claimResult = await mediaRepo.ClaimNextAsync(siteKey, _settings.LeaseSeconds, ct).ConfigureAwait(false);
            if (!claimResult.IsSuccess)
            {
                emptyCount++;
                continue;
            }

            emptyCount = 0;
            var media = claimResult.Value!;
            var postExternalId = await postRepo.GetExternalIdAsync(media.PostId, ct).ConfigureAwait(false)
                ?? media.PostId.ToString();

            dbTasks.Add(ProcessMediaAsync(media, siteKey, postExternalId, ct));
            if (dbTasks.Count >= maxConcurrency)
            {
                await Task.WhenAll(dbTasks).ConfigureAwait(false);
                dbTasks.Clear();
            }
        }

        if (dbTasks.Count > 0)
        {
            await Task.WhenAll(dbTasks).ConfigureAwait(false);
        }
    }

    private async Task ProcessMediaAsync(Core.Models.Media media, string siteKey, string postExternalId, CancellationToken ct)
    {
        try
        {
            var downloadResult = await downloader.DownloadAsync(media, siteKey, postExternalId, ct).ConfigureAwait(false);

            if (downloadResult.IsSuccess)
            {
                var result = downloadResult.Value!;
                media.Sha256 = result.Sha256;
                media.ContentType = result.ContentType;
                media.LocalPath = result.LocalPath;
                media.ByteSize = result.IsDuplicate ? null : result.ByteSize;
                media.Status = result.IsDuplicate ? MediaStatus.SkippedDuplicate : MediaStatus.Downloaded;
                media.RetryCount = 0;
                media.NextRetryAt = null;
                media.LeaseUntil = null;

                await mediaRepo.UpdateAsync(media, ct).ConfigureAwait(false);
                logger.LogInformation(
                    result.IsDuplicate
                        ? "Skipped duplicate media {Url} as {Sha256}"
                        : "Downloaded media {Url} as {Sha256}",
                    media.MediaUrl,
                    result.Sha256[..12]);
            }
            else
            {
                media.RetryCount++;
                media.Status = media.RetryCount >= _settings.MaxRetryCount
                    ? MediaStatus.Failed
                    : MediaStatus.PendingDownload;
                media.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, media.RetryCount));
                media.LeaseUntil = null;

                await mediaRepo.UpdateAsync(media, ct).ConfigureAwait(false);
                logger.LogWarning("Failed to download media {Url}: {Error}", media.MediaUrl, downloadResult.Error);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "미디어 처리 중 예기치 않은 오류: {Url}", media.MediaUrl);
        }
    }
}
