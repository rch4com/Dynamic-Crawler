namespace DynamicCrawler.Core.Models;

/// <summary>게시글 댓글</summary>
public sealed class Comment
{
    public long Id { get; init; }
    public long PostId { get; init; }
    public string? Author { get; init; }
    public required string Content { get; init; }
    public DateTime? CommentedAt { get; init; }
    /// <summary>DB에서 역직렬화 시 매퍼에서 반드시 명시적으로 설정해야 합니다.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
