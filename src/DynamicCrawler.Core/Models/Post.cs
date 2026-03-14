using DynamicCrawler.Core.Enums;

namespace DynamicCrawler.Core.Models;

/// <summary>크롤링 대상 게시글</summary>
public sealed class Post
{
    public long? Id { get; init; }
    public required string SiteKey { get; init; }
    public required string ExternalId { get; init; }
    public required string Url { get; init; }
    public string? Title { get; init; }
    public PostStatus Status { get; set; } = PostStatus.Discovered;
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime? LeaseUntil { get; set; }
    /// <summary>DB에서 역직렬화 시 매퍼에서 반드시 명시적으로 설정해야 합니다.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
