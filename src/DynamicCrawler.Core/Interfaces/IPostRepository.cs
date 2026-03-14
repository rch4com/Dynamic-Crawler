using DynamicCrawler.Core.Common;
using DynamicCrawler.Core.Models;

namespace DynamicCrawler.Core.Interfaces;

/// <summary>Post persistence contract.</summary>
public interface IPostRepository
{
    Task<Result<Post>> ClaimNextAsync(string siteKey, int leaseSeconds, CancellationToken ct = default);

    Task UpdateAsync(Post post, CancellationToken ct = default);

    Task BulkUpsertAsync(IEnumerable<Post> posts, CancellationToken ct = default);

    Task<int> RollbackOrphanedAsync(CancellationToken ct = default);

    Task<string?> GetExternalIdAsync(long postId, CancellationToken ct = default);
}
