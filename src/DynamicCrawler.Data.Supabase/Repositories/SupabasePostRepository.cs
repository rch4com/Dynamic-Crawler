using System.Text.Json;
using DynamicCrawler.Core.Common;
using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Core.Models;
using DynamicCrawler.Data.Supabase.Mappers;
using DynamicCrawler.Data.Supabase.Models;
using Microsoft.Extensions.Logging;

namespace DynamicCrawler.Data.Supabase.Repositories;

public sealed class SupabasePostRepository(
    global::Supabase.Client client,
    ILogger<SupabasePostRepository> logger) : IPostRepository
{
    public async Task<Result<Post>> ClaimNextAsync(string siteKey, int leaseSeconds, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var response = await client.Rpc("claim_next_post", new Dictionary<string, object>
            {
                { "p_site_key", siteKey },
                { "p_lease_seconds", leaseSeconds }
            }).ConfigureAwait(false);

            if (response.Content is null || response.Content == "[]" || response.Content == "null")
            {
                return Result<Post>.Failure("수집할 게시글이 없습니다", "EMPTY_QUEUE");
            }

            var posts = JsonSerializer.Deserialize<List<SupabasePost>>(response.Content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (posts is null || posts.Count == 0)
            {
                return Result<Post>.Failure("수집할 게시글이 없습니다", "EMPTY_QUEUE");
            }

            return Result<Post>.Success(PostMapper.ToDomain(posts[0]));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "게시글 claim 실패: SiteKey={SiteKey}", siteKey);
            return Result<Post>.Failure(ex.Message, "CLAIM_ERROR");
        }
    }

    public async Task UpdateAsync(Post post, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            await client.From<SupabasePost>()
                .Where(model => model.Id == post.Id)
                .Set(model => model.Status, post.Status.ToString())
                .Set(model => model.RetryCount, post.RetryCount)
                .Set(model => model.NextRetryAt!, post.NextRetryAt)
                .Set(model => model.LeaseUntil!, post.LeaseUntil)
                .Set(model => model.UpdatedAt!, post.UpdatedAt ?? DateTime.UtcNow)
                .Update()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "게시글 업데이트 실패: PostId={PostId}", post.Id);
        }
    }

    public async Task BulkUpsertAsync(IEnumerable<Post> posts, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var models = posts.Select(PostMapper.ToSupabase).ToList();
            if (models.Count == 0)
            {
                return;
            }

            await client.From<SupabasePost>()
                .Upsert(models, new global::Supabase.Postgrest.QueryOptions { OnConflict = "site_key,external_id" })
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "게시글 일괄 upsert 실패");
        }
    }

    public async Task<int> RollbackOrphanedAsync(CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var response = await client.Rpc("rollback_orphaned_posts", null).ConfigureAwait(false);
            return int.TryParse(response.Content, out var count) ? count : 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "고아 게시글 롤백 실패");
            return -1;
        }
    }

    public async Task<string?> GetExternalIdAsync(long postId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var response = await client.From<SupabasePost>()
            .Where(post => post.Id == postId)
            .Limit(1)
            .Get()
            .ConfigureAwait(false);

        return response.Models.FirstOrDefault()?.ExternalId;
    }
}
