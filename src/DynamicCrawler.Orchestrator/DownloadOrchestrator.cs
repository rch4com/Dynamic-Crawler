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
        var channelCount = 0;
        while (pipeline.Reader.TryRead(out var task))
        {
            await ProcessMediaAsync(task.Media, task.SiteKey, task.PostExternalId, ct).ConfigureAwait(false);
            channelCount++;
        }

        if (channelCount > 0)
        {
            logger.LogInformation("Processed {Count} media items from the channel.", channelCount);
            return;
        }

        var sites = await siteRepo.GetActiveSitesAsync(ct).ConfigureAwait(false);
        if (sites.Count == 0)
        {
            return;
        }

        scheduler.SetSiteKeys(sites.Select(site => site.SiteKey));

        var emptyCount = 0;
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

            await ProcessMediaAsync(media, siteKey, postExternalId, ct).ConfigureAwait(false);
        }
    }

    private async Task ProcessMediaAsync(Core.Models.Media media, string siteKey, string postExternalId, CancellationToken ct)
    {
        var downloadResult = await downloader.DownloadAsync(media, siteKey, postExternalId, ct).ConfigureAwait(false);

        if (downloadResult.IsSuccess)
        {
            var result = downloadResult.Value!;
            media.Sha256 = result.Sha256;
            media.ByteSize = result.ByteSize;
            media.ContentType = result.ContentType;
            media.LocalPath = result.LocalPath;
            media.Status = MediaStatus.Downloaded;
            media.NextRetryAt = null;
            media.LeaseUntil = null;

            await mediaRepo.UpdateAsync(media, ct).ConfigureAwait(false);
            logger.LogInformation("Downloaded media {Url} as {Sha256}", media.MediaUrl, result.Sha256[..12]);
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
}
