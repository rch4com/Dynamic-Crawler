using DynamicCrawler.Core.Enums;

namespace DynamicCrawler.Core.Models;

/// <summary>미디어 파일 (이미지/gif/mp4/webm 등)</summary>
public sealed class Media
{
    public long Id { get; init; }
    public long PostId { get; init; }
    public required string MediaUrl { get; init; }
    public string? ContentType { get; set; }
    public string? Sha256 { get; set; }
    public long? ByteSize { get; set; }
    public string? LocalPath { get; set; }
    public MediaStatus Status { get; set; } = MediaStatus.PendingDownload;
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime? LeaseUntil { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
