using DynamicCrawler.Core.Enums;

namespace DynamicCrawler.Core.Models;

/// <summary>크롤링 대상 게시글</summary>
public sealed class Post
{
    public long Id { get; init; }
    public required string SiteKey { get; init; }
    public required string ExternalId { get; init; }
    public required string Url { get; init; }
    public string? Title { get; init; }
    public PostStatus Status { get; set; } = PostStatus.Discovered;
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime? LeaseUntil { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
