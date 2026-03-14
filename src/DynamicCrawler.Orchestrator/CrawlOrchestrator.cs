using DynamicCrawler.Core.Configuration;
using DynamicCrawler.Core.Enums;
using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DynamicCrawler.Orchestrator;

public sealed class CrawlOrchestrator(
    IPostRepository postRepo,
    IMediaRepository mediaRepo,
    ICommentRepository commentRepo,
    ISiteRepository siteRepo,
    ICrawlEngine crawlEngine,
    IEnumerable<ISiteStrategy> strategies,
    RoundRobinScheduler scheduler,
    CrawlPipeline pipeline,
    IOptions<CrawlerSettings> settings,
    ILogger<CrawlOrchestrator> logger)
{
    private readonly CrawlerSettings _settings = settings.Value;
    private readonly TimeSpan _discoveryCooldown = TimeSpan.FromMinutes(1);
    private readonly Dictionary<string, ISiteStrategy> _strategyMap =
        strategies.ToDictionary(strategy => strategy.SiteKey, strategy => strategy, StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _lastDiscoveryAt = new(StringComparer.OrdinalIgnoreCase);

    public async Task RunCycleAsync(CancellationToken ct)
    {
        var sites = await siteRepo.GetActiveSitesAsync(ct).ConfigureAwait(false);
        scheduler.SetSiteKeys(sites.Select(site => site.SiteKey));

        if (scheduler.SiteCount == 0)
        {
            logger.LogWarning("No active sites configured.");
            await Task.Delay(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);
            return;
        }

        foreach (var site in sites)
        {
            if (_strategyMap.TryGetValue(site.SiteKey, out var strategy))
            {
                await DiscoverPostsIfDueAsync(site, strategy, ct).ConfigureAwait(false);
            }
        }

        var emptyCount = 0;
        while (emptyCount < scheduler.SiteCount && !ct.IsCancellationRequested)
        {
            var siteKey = scheduler.Next();
            if (siteKey is null)
            {
                break;
            }

            if (!_strategyMap.TryGetValue(siteKey, out var strategy))
            {
                continue;
            }

            var claimResult = await postRepo.ClaimNextAsync(siteKey, _settings.LeaseSeconds, ct).ConfigureAwait(false);
            if (!claimResult.IsSuccess)
            {
                emptyCount++;
                continue;
            }

            emptyCount = 0;
            var post = claimResult.Value!;
            var crawlResult = await crawlEngine.CrawlAsync(post, strategy, ct).ConfigureAwait(false);

            if (crawlResult.IsSuccess)
            {
                var comments = crawlResult.Value!.Comments.Select(comment => new Comment
                {
                    PostId = post.Id,
                    Author = comment.Author,
                    Content = comment.Content,
                    CommentedAt = comment.CreatedAt
                }).ToList();

                var mediaList = crawlResult.Value.Media.Select(media => new Media
                {
                    PostId = post.Id,
                    MediaUrl = media.Url,
                    ContentType = media.ContentType
                }).ToList();

                var insertedMedia = await mediaRepo.BulkInsertAsync(mediaList, ct).ConfigureAwait(false);
                await commentRepo.ReplaceForPostAsync(post.Id, comments, ct).ConfigureAwait(false);

                post.Status = PostStatus.Collected;
                post.LeaseUntil = null;
                post.UpdatedAt = DateTime.UtcNow;
                await postRepo.UpdateAsync(post, ct).ConfigureAwait(false);

                foreach (var media in insertedMedia)
                {
                    var task = new DownloadTask(media, siteKey, post.ExternalId);
                    if (!pipeline.Writer.TryWrite(task))
                    {
                        await pipeline.Writer.WriteAsync(task, ct).ConfigureAwait(false);
                    }
                }

                logger.LogInformation(
                    "Collected post {PostId} ({Title}) with {MediaCount} media and {CommentCount} comments.",
                    post.Id,
                    post.Title,
                    insertedMedia.Count,
                    comments.Count);
            }
            else
            {
                post.RetryCount++;
                post.Status = post.RetryCount >= _settings.MaxRetryCount ? PostStatus.Failed : PostStatus.Discovered;
                post.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, post.RetryCount));
                post.LeaseUntil = null;
                post.UpdatedAt = DateTime.UtcNow;

                await postRepo.UpdateAsync(post, ct).ConfigureAwait(false);
                logger.LogWarning("Failed to crawl post {PostId}: {Error}", post.Id, crawlResult.Error);
            }
        }
    }

    private async Task DiscoverPostsIfDueAsync(Site site, ISiteStrategy strategy, CancellationToken ct)
    {
        if (_lastDiscoveryAt.TryGetValue(site.SiteKey, out var lastDiscoveryAt) &&
            DateTime.UtcNow - lastDiscoveryAt < _discoveryCooldown)
        {
            return;
        }

        try
        {
            for (var page = 1; page <= _settings.MaxListPages; page++)
            {
                var listUrl = strategy.BuildListUrl(page);
                var result = await crawlEngine.GetHtmlAsync(listUrl, ct).ConfigureAwait(false);

                if (!result.IsSuccess)
                {
                    logger.LogWarning("Failed to fetch list HTML for {Url}: {Error}", listUrl, result.Error);
                    break;
                }

                var posts = strategy.ParseList(result.Value!, site.SiteKey);
                if (posts.Count > 0)
                {
                    await postRepo.BulkUpsertAsync(posts, ct).ConfigureAwait(false);
                    logger.LogInformation("Discovered {Count} posts from {Url}", posts.Count, listUrl);
                }
            }

            _lastDiscoveryAt[site.SiteKey] = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to discover posts for {SiteKey}", site.SiteKey);
        }
    }
}
