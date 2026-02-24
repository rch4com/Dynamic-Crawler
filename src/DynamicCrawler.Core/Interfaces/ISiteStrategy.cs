using DynamicCrawler.Core.Models;
using DynamicCrawler.Core.Results;

namespace DynamicCrawler.Core.Interfaces;

/// <summary>사이트별 파싱 전략 — 각 사이트 프로젝트에서 구현</summary>
public interface ISiteStrategy
{
    /// <summary>사이트 식별 키 (예: "aagag")</summary>
    string SiteKey { get; }

    /// <summary>목록 페이지 URL 생성</summary>
    string BuildListUrl(int page);

    /// <summary>목록 HTML에서 게시글 목록 추출</summary>
    IReadOnlyList<Post> ParseList(string html, string siteKey);

    /// <summary>상세 HTML에서 미디어 URL 추출</summary>
    IReadOnlyList<DiscoveredMedia> ParseMedia(string html);

    /// <summary>상세 HTML에서 댓글 추출</summary>
    IReadOnlyList<DiscoveredComment> ParseComments(string html);
}
