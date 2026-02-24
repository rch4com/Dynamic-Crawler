using System.Text.Json;
using DynamicCrawler.Core.Common;
using DynamicCrawler.Core.Enums;
using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Core.Models;
using DynamicCrawler.Data.Supabase.Mappers;
using DynamicCrawler.Data.Supabase.Models;
using Microsoft.Extensions.Logging;

namespace DynamicCrawler.Data.Supabase.Repositories;

/// <summary>Supabase Postgrest + RPC 기반 IPostRepository 구현</summary>
public sealed class SupabasePostRepository(
    global::Supabase.Client client,
    ILogger<SupabasePostRepository> logger) : IPostRepository
{
    public async Task<Result<Post>> ClaimNextAsync(string siteKey, int leaseSeconds, CancellationToken ct = default)
    {
        try
        {
            var response = await client.Rpc("claim_next_post", new Dictionary<string, object>
            {
                { "p_site_key", siteKey },
                { "p_lease_seconds", leaseSeconds }
            }).ConfigureAwait(false);

            if (response.Content is null || response.Content == "[]" || response.Content == "null")
                return Result<Post>.Failure("큐에 처리할 게시글이 없습니다", "EMPTY_QUEUE");

            var posts = JsonSerializer.Deserialize<List<SupabasePost>>(response.Content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (posts is null || posts.Count == 0)
                return Result<Post>.Failure("큐에 처리할 게시글이 없습니다", "EMPTY_QUEUE");

            return Result<Post>.Success(PostMapper.ToDomain(posts[0]));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "게시글 claim 실패: SiteKey={SiteKey}", siteKey);
            return Result<Post>.Failure(ex.Message, "CLAIM_ERROR");
        }
    }

    public async Task UpdateStatusAsync(long postId, PostStatus status, CancellationToken ct = default)
    {
        await client.From<SupabasePost>()
            .Where(p => p.Id == postId)
            .Set(p => p.Status, status.ToString())
            .Set(p => p.UpdatedAt!.Value, DateTime.UtcNow)
            .Update()
            .ConfigureAwait(false);
    }

    public async Task BulkUpsertAsync(IEnumerable<Post> posts, CancellationToken ct = default)
    {
        var models = posts.Select(PostMapper.ToSupabase).ToList();
        if (models.Count == 0) return;

        await client.From<SupabasePost>()
            .Upsert(models, new global::Supabase.Postgrest.QueryOptions { OnConflict = "site_key,external_id" })
            .ConfigureAwait(false);
    }

    public async Task<int> RollbackOrphanedAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await client.Rpc("rollback_orphaned_posts", null).ConfigureAwait(false);
            return int.TryParse(response.Content, out var count) ? count : 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Orphaned posts 롤백 실패");
            return 0;
        }
    }
}
