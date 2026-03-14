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
    /// <summary>DB에서 역직렬화 시 매퍼에서 반드시 명시적으로 설정해야 합니다.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
