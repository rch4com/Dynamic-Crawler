using DynamicCrawler.Core.Models;
using DynamicCrawler.Data.Supabase.Models;

namespace DynamicCrawler.Data.Supabase.Mappers;

internal static class CommentMapper
{
    public static SupabaseComment ToSupabase(Comment comment)
    {
        ArgumentNullException.ThrowIfNull(comment);
        return new()
        {
            Id = comment.Id == 0 ? null : comment.Id,
            PostId = comment.PostId,
            Author = comment.Author,
            Content = comment.Content,
            CommentedAt = comment.CommentedAt,
            CreatedAt = comment.CreatedAt
        };
    }

    public static Comment ToDomain(SupabaseComment sc)
    {
        ArgumentNullException.ThrowIfNull(sc);
        return new()
        {
            Id = sc.Id ?? 0,
            PostId = sc.PostId,
            Author = sc.Author,
            Content = sc.Content,
            CommentedAt = sc.CommentedAt,
            CreatedAt = sc.CreatedAt
        };
    }
}
