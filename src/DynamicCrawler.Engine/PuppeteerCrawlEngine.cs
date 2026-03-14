using DynamicCrawler.Core.Common;
using DynamicCrawler.Core.Configuration;
using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Core.Models;
using DynamicCrawler.Core.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace DynamicCrawler.Engine;

/// <summary>PuppeteerSharp 기반 크롤링 엔진 — DOM 확보 후 ISiteStrategy에 파싱 위임</summary>
public sealed class PuppeteerCrawlEngine(
    BrowserManager browserManager,
    IOptions<CrawlerSettings> settings,
    ILogger<PuppeteerCrawlEngine> logger) : ICrawlEngine
{
    private const string DefaultUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36";

    private readonly CrawlerSettings _settings = settings.Value;

    public async Task<Result<CrawlResult>> CrawlAsync(Post post, ISiteStrategy strategy, CancellationToken ct = default)
    {
        var htmlResult = await FetchHtmlCoreAsync(post.Url, ct).ConfigureAwait(false);
        if (!htmlResult.IsSuccess)
            return Result<CrawlResult>.Failure(htmlResult.Error!, htmlResult.ErrorCode);

        var html = htmlResult.Value!;

        // ISiteStrategy에 파싱 위임
        var media = strategy.ParseMedia(html);
        var comments = strategy.ParseComments(html);

        logger.LogInformation(
            "크롤링 완료: {Url} / 미디어={MediaCount}, 댓글={CommentCount}",
            post.Url, media.Count, comments.Count);

        return Result<CrawlResult>.Success(new CrawlResult(media, comments));
    }

    public async Task<Result<string>> GetHtmlAsync(string url, CancellationToken ct = default)
        => await FetchHtmlCoreAsync(url, ct).ConfigureAwait(false);

    private async Task<Result<string>> FetchHtmlCoreAsync(string url, CancellationToken ct)
    {
        IPage? page = null;
        try
        {
            var browser = await browserManager.GetBrowserAsync(ct).ConfigureAwait(false);
            logger.LogInformation("브라우저 획득 성공. NewPage 생성 시도: {Url}", url);

            var newPageTask = browser.NewPageAsync();
            var completedNewPage = await Task.WhenAny(newPageTask, Task.Delay(_settings.PageOperationTimeoutMs, ct)).ConfigureAwait(false);
            if (completedNewPage != newPageTask)
                throw new TimeoutException($"NewPageAsync timed out after {_settings.PageOperationTimeoutMs}ms: {url}");

            page = await newPageTask.ConfigureAwait(false);
            logger.LogInformation("NewPage 생성 성공. 네트워크 최적화 적용 중: {Url}", url);

            await page.SetUserAgentAsync(DefaultUserAgent).ConfigureAwait(false);

            await NetworkOptimizer.ApplyAsync(page, logger).ConfigureAwait(false);
            logger.LogInformation("네트워크 최적화 적용 완료. GoToAsync 호출: {Url}", url);

            var navigationTask = page.GoToAsync(url, new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.Networkidle2],
                Timeout = _settings.NavigationTimeoutMs
            });

            var timeoutTask = Task.Delay(_settings.NavigationTimeoutMs, ct);
            var completedTask = await Task.WhenAny(navigationTask, timeoutTask).ConfigureAwait(false);
            if (completedTask == timeoutTask)
                throw new TimeoutException($"Navigation timed out after {_settings.NavigationTimeoutMs}ms: {url}");

            await navigationTask.ConfigureAwait(false);
            logger.LogInformation("GoToAsync 완료. GetContentAsync 호출: {Url}", url);

            var contentTask = page.GetContentAsync();
            completedTask = await Task.WhenAny(contentTask, Task.Delay(_settings.PageOperationTimeoutMs, ct)).ConfigureAwait(false);
            if (completedTask != contentTask)
                throw new TimeoutException($"GetContentAsync timed out after {_settings.PageOperationTimeoutMs}ms: {url}");

            var html = await contentTask.ConfigureAwait(false);
            var snippet = html.Length > 200 ? html.Substring(0, 200).Replace("\n", "").Replace("\r", "") : html;
            logger.LogInformation("GetContentAsync 완료. HTML 확보: {Url} (길이: {Length}) 미리보기: {Snippet}", url, html.Length, snippet);

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
                catch (Exception ex) { logger.LogDebug(ex, "페이지 CloseAsync 실패 (무시됨)"); }
            }
        }
    }
}
