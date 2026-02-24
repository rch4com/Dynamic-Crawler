using DynamicCrawler.Core.Common;
using DynamicCrawler.Core.Enums;
using DynamicCrawler.Core.Models;

namespace DynamicCrawler.Core.Interfaces;

/// <summary>게시글 저장소</summary>
public interface IPostRepository
{
    /// <summary>다음 크롤링 대상을 lease 기반으로 획득</summary>
    Task<Result<Post>> ClaimNextAsync(string siteKey, int leaseSeconds, CancellationToken ct = default);

    /// <summary>게시글 상태 업데이트</summary>
    Task UpdateStatusAsync(long postId, PostStatus status, CancellationToken ct = default);

    /// <summary>게시글 bulk upsert (중복 external_id는 무시)</summary>
    Task BulkUpsertAsync(IEnumerable<Post> posts, CancellationToken ct = default);

    /// <summary>orphaned 상태(Collecting) 롤백 — 서비스 재시작 시 호출</summary>
    Task<int> RollbackOrphanedAsync(CancellationToken ct = default);
}
