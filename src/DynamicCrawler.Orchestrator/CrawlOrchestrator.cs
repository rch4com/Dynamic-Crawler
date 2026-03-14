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
                var postId = post.Id ?? 0;
                var comments = crawlResult.Value!.Comments.Select(comment => new Comment
                {
                    PostId = postId,
                    Author = comment.Author,
                    Content = comment.Content,
                    CommentedAt = comment.CreatedAt
                }).ToList();

                var mediaList = crawlResult.Value.Media.Select(media => new Media
                {
                    PostId = postId,
                    MediaUrl = media.Url,
                    ContentType = media.ContentType
                }).ToList();

                IReadOnlyList<Media> insertedMedia = [];
                try
                {
                    insertedMedia = await mediaRepo.BulkInsertAsync(mediaList, ct).ConfigureAwait(false);

                    foreach (var media in insertedMedia)
                    {
                        var task = new DownloadTask(media, siteKey, post.ExternalId);
                        if (!pipeline.Writer.TryWrite(task))
                        {
                            await pipeline.Writer.WriteAsync(task, ct).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "미디어 저장 실패: PostId={PostId}", post.Id ?? 0);
                }

                try
                {
                    await commentRepo.ReplaceForPostAsync(postId, comments, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "댓글 저장 실패: PostId={PostId}", post.Id ?? 0);
                }

                // Post 상태 업데이트는 항상 실행
                post.Status = PostStatus.Collected;
                post.LeaseUntil = null;
                post.UpdatedAt = DateTime.UtcNow;
                await postRepo.UpdateAsync(post, ct).ConfigureAwait(false);

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
        if (!scheduler.ShouldDiscover(site.SiteKey, _discoveryCooldown))
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

            scheduler.MarkDiscovered(site.SiteKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to discover posts for {SiteKey}", site.SiteKey);
        }
    }
}
