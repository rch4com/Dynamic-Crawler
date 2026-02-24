using DynamicCrawler.Core.Models;

namespace DynamicCrawler.Core.Interfaces;

/// <summary>사이트 저장소</summary>
public interface ISiteRepository
{
    /// <summary>활성화된 사이트 목록 조회</summary>
    Task<IReadOnlyList<Site>> GetActiveSitesAsync(CancellationToken ct = default);
}
