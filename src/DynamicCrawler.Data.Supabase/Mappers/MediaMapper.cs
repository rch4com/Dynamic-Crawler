using DynamicCrawler.Core.Enums;
using DynamicCrawler.Core.Models;
using DynamicCrawler.Data.Supabase.Models;

namespace DynamicCrawler.Data.Supabase.Mappers;

internal static class MediaMapper
{
    public static Media ToDomain(SupabaseMedia sm) => new()
    {
        Id = sm.Id ?? 0,
        PostId = sm.PostId,
        MediaUrl = sm.MediaUrl,
        ContentType = sm.ContentType,
        Sha256 = sm.Sha256,
        ByteSize = sm.ByteSize,
        LocalPath = sm.LocalPath,
        Status = Enum.TryParse<MediaStatus>(sm.Status, out var s) ? s : MediaStatus.PendingDownload,
        RetryCount = sm.RetryCount,
        NextRetryAt = sm.NextRetryAt,
        LeaseUntil = sm.LeaseUntil,
        CreatedAt = sm.CreatedAt
    };

    public static SupabaseMedia ToSupabase(Media m) => new()
    {
        Id = m.Id == 0 ? null : m.Id,
        PostId = m.PostId,
        MediaUrl = m.MediaUrl,
        ContentType = m.ContentType,
        Sha256 = m.Sha256,
        ByteSize = m.ByteSize,
        LocalPath = m.LocalPath,
        Status = m.Status.ToString(),
        RetryCount = m.RetryCount,
        NextRetryAt = m.NextRetryAt,
        LeaseUntil = m.LeaseUntil,
        CreatedAt = m.CreatedAt
    };
}
