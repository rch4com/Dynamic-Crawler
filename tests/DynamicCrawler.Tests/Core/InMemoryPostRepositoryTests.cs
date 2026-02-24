using DynamicCrawler.Core.Enums;
using DynamicCrawler.Core.Models;
using DynamicCrawler.Tests.Fakes;
using FluentAssertions;

namespace DynamicCrawler.Tests.Core;

public class InMemoryPostRepositoryTests
{
    private readonly InMemoryPostRepository _repo = new();

    [Fact]
    public async Task ClaimNextAsync_EmptyQueue_ShouldReturnFailure()
    {
        var result = await _repo.ClaimNextAsync("aagag", 300);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("EMPTY_QUEUE");
    }

    [Fact]
    public async Task ClaimNextAsync_WithDiscoveredPost_ShouldReturnSuccess()
    {
        _repo.Seed(new Post { SiteKey = "aagag", ExternalId = "1", Url = "https://aagag.com/1" });

        var result = await _repo.ClaimNextAsync("aagag", 300);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SiteKey.Should().Be("aagag");
        result.Value.Status.Should().Be(PostStatus.Collecting);
        result.Value.LeaseUntil.Should().NotBeNull();
    }

    [Fact]
    public async Task ClaimNextAsync_ShouldNotClaimAlreadyCollecting()
    {
        _repo.Seed(new Post { SiteKey = "aagag", ExternalId = "1", Url = "https://aagag.com/1" });

        var first = await _repo.ClaimNextAsync("aagag", 300);
        first.IsSuccess.Should().BeTrue();

        // 두 번째 claim은 큐가 비어있어야 함
        var second = await _repo.ClaimNextAsync("aagag", 300);
        second.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task BulkUpsertAsync_ShouldNotCreateDuplicates()
    {
        var posts = new[]
        {
            new Post { SiteKey = "aagag", ExternalId = "1", Url = "https://aagag.com/1" },
            new Post { SiteKey = "aagag", ExternalId = "2", Url = "https://aagag.com/2" }
        };

        await _repo.BulkUpsertAsync(posts);
        await _repo.BulkUpsertAsync(posts); // 중복 삽입 시도

        _repo.Posts.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldUpdatePostStatus()
    {
        _repo.Seed(new Post { SiteKey = "aagag", ExternalId = "1", Url = "https://aagag.com/1" });
        var claimed = await _repo.ClaimNextAsync("aagag", 300);

        await _repo.UpdateStatusAsync(claimed.Value!.Id, PostStatus.Collected);

        _repo.Posts.First().Status.Should().Be(PostStatus.Collected);
    }

    [Fact]
    public async Task RollbackOrphanedAsync_ShouldResetExpiredLeases()
    {
        _repo.Seed(new Post
        {
            SiteKey = "aagag",
            ExternalId = "1",
            Url = "https://aagag.com/1",
            Status = PostStatus.Collecting,
            LeaseUntil = DateTime.UtcNow.AddMinutes(-1) // 이미 만료
        });

        var rolledBack = await _repo.RollbackOrphanedAsync();

        rolledBack.Should().Be(1);
        _repo.Posts.First().Status.Should().Be(PostStatus.Discovered);
    }
}
