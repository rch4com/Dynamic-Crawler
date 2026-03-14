using System.Text.Json;
using DynamicCrawler.Core.Common;
using DynamicCrawler.Core.Enums;
using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Core.Models;
using DynamicCrawler.Data.Supabase.Mappers;
using DynamicCrawler.Data.Supabase.Models;
using Microsoft.Extensions.Logging;

namespace DynamicCrawler.Data.Supabase.Repositories;

/// <summary>Supabase Postgrest + RPC 기반 IMediaRepository 구현</summary>
public sealed class SupabaseMediaRepository(
    global::Supabase.Client client,
    ILogger<SupabaseMediaRepository> logger) : IMediaRepository
{
    public async Task<Result<Media>> ClaimNextAsync(string siteKey, int leaseSeconds, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var response = await client.Rpc("claim_next_media", new Dictionary<string, object>
            {
                { "p_site_key", siteKey },
                { "p_lease_seconds", leaseSeconds }
            }).ConfigureAwait(false);

            if (response.Content is null || response.Content == "[]" || response.Content == "null")
                return Result<Media>.Failure("다운로드할 미디어가 없습니다", "EMPTY_QUEUE");

            var mediaList = JsonSerializer.Deserialize<List<SupabaseMedia>>(response.Content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (mediaList is null || mediaList.Count == 0)
                return Result<Media>.Failure("다운로드할 미디어가 없습니다", "EMPTY_QUEUE");

            return Result<Media>.Success(MediaMapper.ToDomain(mediaList[0]));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "미디어 claim 실패: SiteKey={SiteKey}", siteKey);
            return Result<Media>.Failure(ex.Message, "CLAIM_ERROR");
        }
    }

    public async Task UpdateAsync(Media media, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var model = MediaMapper.ToSupabase(media);
            await client.From<SupabaseMedia>()
                .Where(m => m.Id == media.Id)
                .Set(m => m.Status, model.Status)
                .Set(m => m.RetryCount, model.RetryCount)
                .Set(m => m.Sha256!, model.Sha256)
                .Set(m => m.ByteSize!, model.ByteSize)
                .Set(m => m.ContentType!, model.ContentType)
                .Set(m => m.LocalPath!, model.LocalPath)
                .Set(m => m.NextRetryAt!, model.NextRetryAt)
                .Set(m => m.LeaseUntil!, model.LeaseUntil)
                .Update()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "미디어 업데이트 실패: MediaId={MediaId}", media.Id);
        }
    }

    public async Task<IReadOnlyList<Media>> BulkInsertAsync(IEnumerable<Media> mediaList, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var models = mediaList.Select(MediaMapper.ToSupabase).ToList();
            if (models.Count == 0) return [];

            var response = await client.From<SupabaseMedia>()
                .Insert(models)
                .ConfigureAwait(false);

            return response.Models.Select(MediaMapper.ToDomain).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "미디어 일괄 삽입 실패");
            return [];
        }
    }

    public async Task<bool> ExistsBySha256Async(string sha256, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256);
        ct.ThrowIfCancellationRequested();
        var response = await client.From<SupabaseMedia>()
            .Select("id")
            .Where(m => m.Sha256 == sha256)
            .Limit(1)
            .Get()
            .ConfigureAwait(false);

        return response.Models.Count > 0;
    }

    public async Task<int> RollbackOrphanedAsync(CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var response = await client.Rpc("rollback_orphaned_media", null).ConfigureAwait(false);
            return int.TryParse(response.Content, out var count) ? count : 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Orphaned media 롤백 실패");
            return -1;
        }
    }
}
