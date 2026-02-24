using DynamicCrawler.Core.Enums;
using DynamicCrawler.Core.Models;
using DynamicCrawler.Data.Supabase.Models;

namespace DynamicCrawler.Data.Supabase.Mappers;

internal static class PostMapper
{
    public static Post ToDomain(SupabasePost sp) => new()
    {
        Id = sp.Id,
        SiteKey = sp.SiteKey,
        ExternalId = sp.ExternalId,
        Url = sp.Url,
        Title = sp.Title,
        Status = Enum.TryParse<PostStatus>(sp.Status, out var s) ? s : PostStatus.Discovered,
        RetryCount = sp.RetryCount,
        NextRetryAt = sp.NextRetryAt,
        LeaseUntil = sp.LeaseUntil,
        CreatedAt = sp.CreatedAt,
        UpdatedAt = sp.UpdatedAt
    };

    public static SupabasePost ToSupabase(Post p) => new()
    {
        Id = p.Id,
        SiteKey = p.SiteKey,
        ExternalId = p.ExternalId,
        Url = p.Url,
        Title = p.Title,
        Status = p.Status.ToString(),
        RetryCount = p.RetryCount,
        NextRetryAt = p.NextRetryAt,
        LeaseUntil = p.LeaseUntil,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
