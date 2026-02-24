using DynamicCrawler.Core.Common;
using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Core.Models;
using DynamicCrawler.Core.Results;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace DynamicCrawler.Engine;

/// <summary>PuppeteerSharp 기반 크롤링 엔진 — DOM 확보 후 ISiteStrategy에 파싱 위임</summary>
public sealed class PuppeteerCrawlEngine(
    BrowserManager browserManager,
    ILogger<PuppeteerCrawlEngine> logger) : ICrawlEngine
{
    public async Task<Result<CrawlResult>> CrawlAsync(Post post, ISiteStrategy strategy, CancellationToken ct = default)
    {
        IPage? page = null;
        try
        {
            var browser = await browserManager.GetBrowserAsync(ct).ConfigureAwait(false);
            page = await browser.NewPageAsync().ConfigureAwait(false);

            // 네트워크 최적화: CSS/폰트/이미지/미디어 차단, XHR/Fetch 허용
            await NetworkOptimizer.ApplyAsync(page).ConfigureAwait(false);

            await page.GoToAsync(post.Url, new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.DOMContentLoaded],
                Timeout = 30_000
            }).ConfigureAwait(false);

            // DOM HTML 추출
            var html = await page.GetContentAsync().ConfigureAwait(false);

            // ISiteStrategy에 파싱 위임
            var media = strategy.ParseMedia(html);
            var comments = strategy.ParseComments(html);

            logger.LogInformation(
                "크롤링 완료: {Url} / 미디어={MediaCount}, 댓글={CommentCount}",
                post.Url, media.Count, comments.Count);

            browserManager.IncrementProcessedCount();

            return Result<CrawlResult>.Success(new CrawlResult(media, comments));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "크롤링 실패: {Url}", post.Url);
            return Result<CrawlResult>.Failure(ex.Message, "CRAWL_ERROR");
        }
        finally
        {
            if (page is not null)
            {
                try { await page.CloseAsync().ConfigureAwait(false); }
                catch { /* 무시 */ }
            }
        }
    }

    public async Task<Result<string>> GetHtmlAsync(string url, CancellationToken ct = default)
    {
        IPage? page = null;
        try
        {
            var browser = await browserManager.GetBrowserAsync(ct).ConfigureAwait(false);
            logger.LogInformation("브라우저 획득 성공. NewPage 생성 시도: {Url}", url);

            var newPageTask = browser.NewPageAsync();
            var completedNewPage = await Task.WhenAny(newPageTask, Task.Delay(10_000, ct)).ConfigureAwait(false);
            if (completedNewPage != newPageTask)
                throw new TimeoutException($"NewPageAsync timed out after 10s: {url}");
            
            page = await newPageTask.ConfigureAwait(false);
            logger.LogInformation("NewPage 생성 성공. 네트워크 최적화 적용 중: {Url}", url);

            await NetworkOptimizer.ApplyAsync(page).ConfigureAwait(false);
            logger.LogInformation("네트워크 최적화 적용 완료. GoToAsync 호출: {Url}", url);

            var navigationTask = page.GoToAsync(url, new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.DOMContentLoaded],
                Timeout = 30_000
            });
            
            var timeoutTask = Task.Delay(30_000, ct);
            var completedTask = await Task.WhenAny(navigationTask, timeoutTask).ConfigureAwait(false);
            if (completedTask == timeoutTask)
                throw new TimeoutException($"Navigation timed out after 30s: {url}");
            
            await navigationTask.ConfigureAwait(false); // throw if error
            logger.LogInformation("GoToAsync 완료. GetContentAsync 호출: {Url}", url);
            
            var contentTask = page.GetContentAsync();
            completedTask = await Task.WhenAny(contentTask, Task.Delay(10_000, ct)).ConfigureAwait(false);
            if (completedTask != contentTask)
                throw new TimeoutException($"GetContentAsync timed out after 10s: {url}");
            
            var html = await contentTask.ConfigureAwait(false);
            logger.LogInformation("GetContentAsync 완료. HTML 확보: {Url} (길이: {Length})", url, html.Length);
            
            browserManager.IncrementProcessedCount();

            return Result<string>.Success(html);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HTML 확보 실패: {Url}", url);
            return Result<string>.Failure(ex.Message, "HTML_FETCH_ERROR");
        }
        finally
        {
            if (page is not null)
            {
                try { await page.CloseAsync().ConfigureAwait(false); }
                catch { /* 무시 */ }
            }
        }
    }
}
