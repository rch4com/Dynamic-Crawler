using DynamicCrawler.Core.Models;
using DynamicCrawler.Data.Supabase.Models;

namespace DynamicCrawler.Data.Supabase.Mappers;

internal static class SiteMapper
{
    public static Site ToDomain(SupabaseSite ss) => new()
    {
        Id = ss.Id,
        SiteKey = ss.SiteKey,
        BaseUrl = ss.BaseUrl,
        MaxConcurrentCollects = ss.MaxConcurrentCollects,
        MaxConcurrentDownloads = ss.MaxConcurrentDownloads,
        IsActive = ss.IsActive,
        CreatedAt = ss.CreatedAt
    };
}
