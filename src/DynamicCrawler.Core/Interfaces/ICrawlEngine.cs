using DynamicCrawler.Core.Common;
using DynamicCrawler.Core.Models;
using DynamicCrawler.Core.Results;

namespace DynamicCrawler.Core.Interfaces;

/// <summary>크롤링 엔진 — DOM 확보 후 ISiteStrategy에 위임</summary>
public interface ICrawlEngine
{
    /// <summary>게시글 크롤링 (DOM 확보 + 파싱 위임)</summary>
    Task<Result<CrawlResult>> CrawlAsync(Post post, ISiteStrategy strategy, CancellationToken ct = default);
    /// <summary>목록 페이지 등의 HTML만 단순 확보</summary>
    Task<Result<string>> GetHtmlAsync(string url, CancellationToken ct = default);
}
