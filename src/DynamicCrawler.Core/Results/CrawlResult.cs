namespace DynamicCrawler.Core.Results;

/// <summary>크롤링 결과 — 발견된 미디어와 댓글</summary>
public sealed record CrawlResult(
    IReadOnlyList<DiscoveredMedia> Media,
    IReadOnlyList<DiscoveredComment> Comments);

/// <summary>발견된 미디어 URL 정보</summary>
public sealed record DiscoveredMedia(string Url, string? ContentType);

/// <summary>발견된 댓글</summary>
public sealed record DiscoveredComment(string? Author, string Content, DateTime? CreatedAt);
