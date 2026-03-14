using DynamicCrawler.Core.Models;

namespace DynamicCrawler.Core.Interfaces;

/// <summary>Comment persistence contract.</summary>
public interface ICommentRepository
{
    /// <summary>게시글의 기존 댓글을 모두 삭제하고 새 댓글로 교체 (멱등 보장)</summary>
    Task ReplaceForPostAsync(long postId, IEnumerable<Comment> comments, CancellationToken ct = default);
}
