using DynamicCrawler.Core.Configuration;
using DynamicCrawler.Core.Enums;
using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DynamicCrawler.Orchestrator;

/// <summary>크롤링 오케스트레이터 — discover → claim → crawl → 미디어 등록</summary>
public sealed class CrawlOrchestrator(
    IPostRepository postRepo,
    IMediaRepository mediaRepo,
    ISiteRepository siteRepo,
    ICrawlEngine crawlEngine,
    IEnumerable<ISiteStrategy> strategies,
    RoundRobinScheduler scheduler,
    IOptions<CrawlerSettings> settings,
    ILogger<CrawlOrchestrator> logger)
{
    private readonly CrawlerSettings _settings = settings.Value;
    private readonly Dictionary<string, ISiteStrategy> _strategyMap =
        strategies.ToDictionary(s => s.SiteKey, s => s, StringComparer.OrdinalIgnoreCase);

    /// <summary>1 사이클 실행: 사이트 목록 로드 → 라운드로빈 크롤링</summary>
    public async Task RunCycleAsync(CancellationToken ct)
    {
        // 활성 사이트 로드 및 스케줄러 초기화
        var sites = await siteRepo.GetActiveSitesAsync(ct).ConfigureAwait(false);
        scheduler.SetSiteKeys(sites.Select(s => s.SiteKey));

        if (scheduler.SiteCount == 0)
        {
            logger.LogWarning("활성 사이트가 없습니다. 대기 중...");
            await Task.Delay(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);
            return;
        }

        // 목록 크롤링 → 게시글 discover
        foreach (var site in sites)
        {
            if (!_strategyMap.TryGetValue(site.SiteKey, out var strategy)) continue;

            await DiscoverPostsAsync(site, strategy, ct).ConfigureAwait(false);
        }

        // 라운드로빈으로 게시글 처리
        var emptyCount = 0;
        while (emptyCount < scheduler.SiteCount && !ct.IsCancellationRequested)
        {
            var siteKey = scheduler.Next();
            if (siteKey is null) break;

            if (!_strategyMap.TryGetValue(siteKey, out var strategy)) continue;

            var claimResult = await postRepo.ClaimNextAsync(siteKey, _settings.LeaseSeconds, ct)
                .ConfigureAwait(false);

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
                // 미디어 등록
                var mediaList = crawlResult.Value!.Media.Select(m => new Media
                {
                    PostId = post.Id,
                    MediaUrl = m.Url,
                    ContentType = m.ContentType
                });

                await mediaRepo.BulkInsertAsync(mediaList, ct).ConfigureAwait(false);
                await postRepo.UpdateStatusAsync(post.Id, PostStatus.Collected, ct).ConfigureAwait(false);

                logger.LogInformation("게시글 수집 완료: {PostId} ({Title})", post.Id, post.Title);
            }
            else
            {
                post.RetryCount++;
                post.Status = post.RetryCount >= _settings.MaxRetryCount ? PostStatus.Failed : PostStatus.Discovered;
                post.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, post.RetryCount));
                await postRepo.UpdateStatusAsync(post.Id, post.Status, ct).ConfigureAwait(false);

                logger.LogWarning("게시글 수집 실패: {PostId} / {Error}", post.Id, crawlResult.Error);
            }
        }
    }

    private async Task DiscoverPostsAsync(Site site, ISiteStrategy strategy, CancellationToken ct)
    {
        try
        {
            for (var page = 1; page <= _settings.MaxListPages; page++)
            {
                var listUrl = strategy.BuildListUrl(page);
                
                var result = await crawlEngine.GetHtmlAsync(listUrl, ct).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    logger.LogWarning("목록 HTML 확보 실패: {Url} - {Error}", listUrl, result.Error);
                    break;
                }

                var posts = strategy.ParseList(result.Value!, site.SiteKey);
                
                if (posts.Count > 0)
                {
                    await postRepo.BulkUpsertAsync(posts, ct).ConfigureAwait(false);
                    logger.LogInformation("목록 발견: {Url} / 게시글 {Count}건", listUrl, posts.Count);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "목록 탐색 실패: {SiteKey}", site.SiteKey);
        }
    }
}
