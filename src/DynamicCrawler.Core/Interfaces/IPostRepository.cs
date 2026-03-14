using DynamicCrawler.Core.Common;
using DynamicCrawler.Core.Models;

namespace DynamicCrawler.Core.Interfaces;

/// <summary>Post persistence contract.</summary>
public interface IPostRepository
{
    /// <summary>다음 수집 대상 게시글을 lease 기반으로 획득</summary>
    Task<Result<Post>> ClaimNextAsync(string siteKey, int leaseSeconds, CancellationToken ct = default);

    /// <summary>게시글 상태 및 메타데이터 업데이트</summary>
    Task UpdateAsync(Post post, CancellationToken ct = default);

    /// <summary>게시글 목록 bulk upsert (ExternalId 기준 중복 무시)</summary>
    Task BulkUpsertAsync(IEnumerable<Post> posts, CancellationToken ct = default);

    /// <summary>orphaned 상태(Collecting) 게시글을 Discovered로 롤백</summary>
    Task<int> RollbackOrphanedAsync(CancellationToken ct = default);

    /// <summary>게시글 ID로 ExternalId 조회</summary>
    Task<string?> GetExternalIdAsync(long postId, CancellationToken ct = default);
}
