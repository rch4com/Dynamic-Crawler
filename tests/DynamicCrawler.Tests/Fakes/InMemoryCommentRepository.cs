using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Core.Models;

namespace DynamicCrawler.Tests.Fakes;

public sealed class InMemoryCommentRepository : ICommentRepository
{
    private readonly List<Comment> _comments = [];
    private long _nextId = 1;

    public IReadOnlyList<Comment> Comments => _comments;

    public Task ReplaceForPostAsync(long postId, IEnumerable<Comment> comments, CancellationToken ct = default)
    {
        _comments.RemoveAll(comment => comment.PostId == postId);

        foreach (var comment in comments)
        {
            _comments.Add(new Comment
            {
                Id = _nextId++,
                PostId = postId,
                Author = comment.Author,
                Content = comment.Content,
                CommentedAt = comment.CommentedAt,
                CreatedAt = comment.CreatedAt
            });
        }

        return Task.CompletedTask;
    }
}
