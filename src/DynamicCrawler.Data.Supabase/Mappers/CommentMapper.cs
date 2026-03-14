using DynamicCrawler.Core.Models;
using DynamicCrawler.Data.Supabase.Models;

namespace DynamicCrawler.Data.Supabase.Mappers;

internal static class CommentMapper
{
    public static SupabaseComment ToSupabase(Comment comment) => new()
    {
        Id = comment.Id == 0 ? null : comment.Id,
        PostId = comment.PostId,
        Author = comment.Author,
        Content = comment.Content,
        CommentedAt = comment.CommentedAt,
        CreatedAt = comment.CreatedAt
    };
}
