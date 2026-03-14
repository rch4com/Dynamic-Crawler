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
    /// <remarks>반환되는 Post 객체는 반드시 Discovered 상태여야 합니다.</remarks>
    IReadOnlyList<Post> ParseList(string html, string siteKey);

    /// <summary>상세 HTML에서 미디어 URL 추출</summary>
    /// <returns>추출된 미디어 URL 목록; 미디어가 없으면 빈 컬렉션 반환</returns>
    IReadOnlyList<DiscoveredMedia> ParseMedia(string html);

    /// <summary>상세 HTML에서 댓글 추출</summary>
    /// <returns>추출된 댓글 목록; 댓글이 없으면 빈 컬렉션 반환</returns>
    IReadOnlyList<DiscoveredComment> ParseComments(string html);
}
