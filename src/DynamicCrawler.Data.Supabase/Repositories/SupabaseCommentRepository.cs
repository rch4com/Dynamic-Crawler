using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Core.Models;
using DynamicCrawler.Data.Supabase.Mappers;
using DynamicCrawler.Data.Supabase.Models;

namespace DynamicCrawler.Data.Supabase.Repositories;

public sealed class SupabaseCommentRepository(global::Supabase.Client client) : ICommentRepository
{
    public async Task ReplaceForPostAsync(long postId, IEnumerable<Comment> comments, CancellationToken ct = default)
    {
        await client.From<SupabaseComment>()
            .Where(comment => comment.PostId == postId)
            .Delete()
            .ConfigureAwait(false);

        var models = comments.Select(CommentMapper.ToSupabase).ToList();
        if (models.Count == 0)
        {
            return;
        }

        await client.From<SupabaseComment>()
            .Insert(models)
            .ConfigureAwait(false);
    }
}
