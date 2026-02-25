using DynamicCrawler.Core.Enums;
using DynamicCrawler.Core.Models;
using DynamicCrawler.Tests.Fakes;
using Xunit;

namespace DynamicCrawler.Tests.IntegrationTests;

public class OrphanedDataRecoveryTests
{
    [Fact]
    public async Task RollbackOrphanedAsync_Should_Restore_LeaseExpired_Posts_And_Media()
    {
        // Arrange
        var postRepo = new InMemoryPostRepository();
        var mediaRepo = new InMemoryMediaRepository();

        // 1. 정상 상태의 Post (Discovered)
        postRepo.Seed(new Post
        {
            SiteKey = "test",
            ExternalId = "1",
            Url = "http://test/1",
            Status = PostStatus.Discovered
        });

        // 2. Lease 만료된 Orphaned Post (Collecting & LeaseUntil < Now)
        postRepo.Seed(new Post
        {
            SiteKey = "test",
            ExternalId = "2",
            Url = "http://test/2",
            Status = PostStatus.Collecting,
            LeaseUntil = DateTime.UtcNow.AddMinutes(-5) // 5분 전 만료됨
        });

        // 3. Lease가 아직 유효한 Post (Collecting & LeaseUntil > Now)
        postRepo.Seed(new Post
        {
            SiteKey = "test",
            ExternalId = "3",
            Url = "http://test/3",
            Status = PostStatus.Collecting,
            LeaseUntil = DateTime.UtcNow.AddMinutes(5) // 5분 후 만료됨
        });

        // 미디어 데이터 세팅
        // 1. 만료된 Orphaned Media
        mediaRepo.Seed(new Media
        {
            PostId = 1,
            MediaUrl = "http://test/1.jpg",
            Status = MediaStatus.Downloading,
            LeaseUntil = DateTime.UtcNow.AddMinutes(-5)
        });

        // 2. 정상 진행 중인 Media
        mediaRepo.Seed(new Media
        {
            PostId = 1,
            MediaUrl = "http://test/2.jpg",
            Status = MediaStatus.Downloading,
            LeaseUntil = DateTime.UtcNow.AddMinutes(5)
        });

        // Act
        var rolledBackPosts = await postRepo.RollbackOrphanedAsync(CancellationToken.None);
        var rolledBackMedia = await mediaRepo.RollbackOrphanedAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, rolledBackPosts);
        Assert.Equal(1, rolledBackMedia);

        // Post 상태 검증
        var posts = postRepo.Posts;
        Assert.Equal(PostStatus.Discovered, posts.Single(p => p.ExternalId == "1").Status);
        
        // Orphaned였던 2번이 Discovered로 롤백되었고 LeaseUntil이 초기화됨을 확인
        var restoredPost = posts.Single(p => p.ExternalId == "2");
        Assert.Equal(PostStatus.Discovered, restoredPost.Status);
        Assert.Null(restoredPost.LeaseUntil);
        
        // 정상 진행 중이던 3번은 Collecting 상태 유지
        Assert.Equal(PostStatus.Collecting, posts.Single(p => p.ExternalId == "3").Status);
        
        // 미디어 상태 검증
        var medias = mediaRepo.MediaList;
        var restoredMedia = medias.Single(m => m.MediaUrl == "http://test/1.jpg");
        Assert.Equal(MediaStatus.PendingDownload, restoredMedia.Status);
        Assert.Null(restoredMedia.LeaseUntil);
    }
}
