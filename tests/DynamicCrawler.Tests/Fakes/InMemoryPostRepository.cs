using DynamicCrawler.Core.Common;
using DynamicCrawler.Core.Enums;
using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Core.Models;

namespace DynamicCrawler.Tests.Fakes;

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
        {
            return Task.FromResult(Result<Post>.Failure("No posts available.", "EMPTY_QUEUE"));
        }

        post.Status = PostStatus.Collecting;
        post.LeaseUntil = DateTime.UtcNow.AddSeconds(leaseSeconds);
        post.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult(Result<Post>.Success(post));
    }

    public Task UpdateAsync(Post post, CancellationToken ct = default)
    {
        var existing = _posts.FirstOrDefault(p => p.Id == post.Id);
        if (existing is not null)
        {
            existing.Status = post.Status;
            existing.RetryCount = post.RetryCount;
            existing.NextRetryAt = post.NextRetryAt;
            existing.LeaseUntil = post.LeaseUntil;
            existing.UpdatedAt = post.UpdatedAt ?? DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task BulkUpsertAsync(IEnumerable<Post> posts, CancellationToken ct = default)
    {
        foreach (var post in posts)
        {
            var existing = _posts.FirstOrDefault(p => p.SiteKey == post.SiteKey && p.ExternalId == post.ExternalId);
            if (existing is not null)
            {
                continue;
            }

            _posts.Add(new Post
            {
                Id = _nextId++,
                SiteKey = post.SiteKey,
                ExternalId = post.ExternalId,
                Url = post.Url,
                Title = post.Title,
                Status = post.Status
            });
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

    public Task<string?> GetExternalIdAsync(long postId, CancellationToken ct = default)
    {
        return Task.FromResult(_posts.FirstOrDefault(post => post.Id == postId)?.ExternalId);
    }

    public void Seed(Post post)
    {
        _posts.Add(new Post
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
        });
    }
}
