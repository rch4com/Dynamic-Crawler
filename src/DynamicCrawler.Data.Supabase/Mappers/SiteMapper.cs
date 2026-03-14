using DynamicCrawler.Core.Models;
using DynamicCrawler.Data.Supabase.Models;

namespace DynamicCrawler.Data.Supabase.Mappers;

internal static class SiteMapper
{
    public static Site ToDomain(SupabaseSite ss)
    {
        ArgumentNullException.ThrowIfNull(ss);
        return new()
        {
            Id = checked((int)(ss.Id ?? 0)),
            SiteKey = ss.SiteKey,
            BaseUrl = ss.SiteBaseUrl,
            MaxConcurrentCollects = ss.MaxConcurrentCollects,
            MaxConcurrentDownloads = ss.MaxConcurrentDownloads,
            IsActive = ss.IsActive,
            CreatedAt = ss.CreatedAt
        };
    }

    public static SupabaseSite ToSupabase(Site s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return new()
        {
            Id = s.Id == 0 ? null : s.Id,
            SiteKey = s.SiteKey,
            SiteBaseUrl = s.BaseUrl,
            MaxConcurrentCollects = s.MaxConcurrentCollects,
            MaxConcurrentDownloads = s.MaxConcurrentDownloads,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt
        };
    }
}
