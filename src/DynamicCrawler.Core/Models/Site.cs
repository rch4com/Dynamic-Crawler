using DynamicCrawler.Core.Enums;

namespace DynamicCrawler.Core.Models;

/// <summary>크롤링 대상 사이트</summary>
public sealed class Site
{
    public int Id { get; init; }
    public required string SiteKey { get; init; }
    public required string BaseUrl { get; init; }
    public int MaxConcurrentCollects { get; set; } = 2;
    public int MaxConcurrentDownloads { get; set; } = 4;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
