using DynamicCrawler.Core.Common;
using DynamicCrawler.Core.Models;

namespace DynamicCrawler.Core.Interfaces;

/// <summary>미디어 저장소</summary>
public interface IMediaRepository
{
    /// <summary>다음 다운로드 대상을 lease 기반으로 획득</summary>
    Task<Result<Media>> ClaimNextAsync(string siteKey, int leaseSeconds, CancellationToken ct = default);

    /// <summary>미디어 정보 업데이트 (SHA256, 크기, 경로 등)</summary>
    Task UpdateAsync(Media media, CancellationToken ct = default);

    /// <summary>미디어 bulk insert (한 게시글의 모든 미디어)</summary>
    Task BulkInsertAsync(IEnumerable<Media> mediaList, CancellationToken ct = default);

    /// <summary>SHA256 기반 글로벌 중복 확인</summary>
    Task<bool> ExistsBySha256Async(string sha256, CancellationToken ct = default);

    /// <summary>orphaned 상태(Downloading) 롤백</summary>
    Task<int> RollbackOrphanedAsync(CancellationToken ct = default);
}
