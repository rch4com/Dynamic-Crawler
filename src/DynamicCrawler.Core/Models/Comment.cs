namespace DynamicCrawler.Core.Models;

/// <summary>게시글 댓글</summary>
public sealed class Comment
{
    public long Id { get; init; }
    public long PostId { get; init; }
    public string? Author { get; init; }
    public required string Content { get; init; }
    public DateTime? CommentedAt { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
