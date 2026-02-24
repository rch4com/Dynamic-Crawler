using DynamicCrawler.Core.Common;
using DynamicCrawler.Core.Enums;
using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Core.Models;

namespace DynamicCrawler.Tests.Fakes;

/// <summary>InMemory Post Repository — 테스트용</summary>
public sealed class InMemoryPostRepository : IPostRepository
{
    private readonly List<Post> _posts = [];
    private long _nextId = 1;

    public IReadOnlyList<Post> Posts => _posts;

    public Task<Result<Post>> ClaimNextAsync(string siteKey, int leaseSeconds, CancellationToken ct = default)
    {
        var post = _posts.FirstOrDefault(p =>
            p.SiteKey == siteKey &&
            p.Status == PostStatus.Discovered &&
            (p.LeaseUntil is null || p.LeaseUntil < DateTime.UtcNow));

        if (post is null)
            return Task.FromResult(Result<Post>.Failure("큐에 처리할 게시글이 없습니다", "EMPTY_QUEUE"));

        post.Status = PostStatus.Collecting;
        post.LeaseUntil = DateTime.UtcNow.AddSeconds(leaseSeconds);
        post.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult(Result<Post>.Success(post));
    }

    public Task UpdateStatusAsync(long postId, PostStatus status, CancellationToken ct = default)
    {
        var post = _posts.FirstOrDefault(p => p.Id == postId);
        if (post is not null)
        {
            post.Status = status;
            post.UpdatedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task BulkUpsertAsync(IEnumerable<Post> posts, CancellationToken ct = default)
    {
        foreach (var p in posts)
        {
            var existing = _posts.FirstOrDefault(e =>
                e.SiteKey == p.SiteKey && e.ExternalId == p.ExternalId);

            if (existing is null)
            {
                var newPost = new Post
                {
                    Id = _nextId++,
                    SiteKey = p.SiteKey,
                    ExternalId = p.ExternalId,
                    Url = p.Url,
                    Title = p.Title,
                    Status = p.Status
                };
                _posts.Add(newPost);
            }
        }
        return Task.CompletedTask;
    }

    public Task<int> RollbackOrphanedAsync(CancellationToken ct = default)
    {
        var count = 0;
        foreach (var post in _posts.Where(p => p.Status == PostStatus.Collecting && p.LeaseUntil < DateTime.UtcNow))
        {
            post.Status = PostStatus.Discovered;
            post.LeaseUntil = null;
            count++;
        }
        return Task.FromResult(count);
    }

    /// <summary>테스트용 시드 메서드</summary>
    public void Seed(Post post)
    {
        var seeded = new Post
        {
            Id = _nextId++,
            SiteKey = post.SiteKey,
            ExternalId = post.ExternalId,
            Url = post.Url,
            Title = post.Title,
            Status = post.Status,
            RetryCount = post.RetryCount,
            NextRetryAt = post.NextRetryAt,
            LeaseUntil = post.LeaseUntil,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt
        };
        _posts.Add(seeded);
    }
}
