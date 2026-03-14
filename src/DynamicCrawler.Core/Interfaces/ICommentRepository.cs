using DynamicCrawler.Core.Models;

namespace DynamicCrawler.Core.Interfaces;

/// <summary>Comment persistence contract.</summary>
public interface ICommentRepository
{
    Task ReplaceForPostAsync(long postId, IEnumerable<Comment> comments, CancellationToken ct = default);
}
