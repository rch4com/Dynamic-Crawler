using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DynamicCrawler.Data.Supabase.Repositories;

public sealed class SupabaseCommentRepository(
    global::Supabase.Client client,
    ILogger<SupabaseCommentRepository> logger) : ICommentRepository
{
    public async Task ReplaceForPostAsync(long postId, IEnumerable<Comment> comments, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var payload = comments.Select(comment => new
            {
                author = comment.Author,
                content = comment.Content,
                commented_at = comment.CommentedAt
            }).ToList();

            await client.Rpc("replace_post_comments", new Dictionary<string, object>
            {
                ["p_post_id"] = postId,
                ["p_comments"] = JsonSerializer.Serialize(payload)
            })
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "댓글 교체 실패: PostId={PostId}", postId);
        }
    }
}
