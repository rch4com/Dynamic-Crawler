using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Core.Models;
using DynamicCrawler.Data.Supabase.Mappers;
using DynamicCrawler.Data.Supabase.Models;

namespace DynamicCrawler.Data.Supabase.Repositories;

/// <summary>Supabase 기반 ISiteRepository 구현</summary>
public sealed class SupabaseSiteRepository(global::Supabase.Client client) : ISiteRepository
{
    public async Task<IReadOnlyList<Site>> GetActiveSitesAsync(CancellationToken ct = default)
    {
        var response = await client.From<SupabaseSite>()
            .Where(s => s.IsActive == true)
            .Get()
            .ConfigureAwait(false);

        return response.Models.Select(SiteMapper.ToDomain).ToList();
    }
}
