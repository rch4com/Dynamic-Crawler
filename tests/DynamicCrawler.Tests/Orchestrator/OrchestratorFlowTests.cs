using DynamicCrawler.Core.Common;
using DynamicCrawler.Core.Configuration;
using DynamicCrawler.Core.Enums;
using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Core.Models;
using DynamicCrawler.Core.Results;
using DynamicCrawler.Orchestrator;
using DynamicCrawler.Tests.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace DynamicCrawler.Tests.Orchestrator;

public class OrchestratorFlowTests
{
    [Fact]
    public async Task CrawlOrchestrator_ShouldPersistComments()
    {
        var postRepo = new InMemoryPostRepository();
        var mediaRepo = new InMemoryMediaRepository();
        var commentRepo = new InMemoryCommentRepository();
        postRepo.Seed(new Post { SiteKey = "aagag", ExternalId = "42", Url = "https://aagag.com/42" });

        var siteRepo = new Mock<ISiteRepository>();
        siteRepo.Setup(repo => repo.GetActiveSitesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Site { SiteKey = "aagag", BaseUrl = "https://aagag.com" }]);

        var strategy = new Mock<ISiteStrategy>();
        strategy.SetupGet(value => value.SiteKey).Returns("aagag");

        var crawlEngine = new Mock<ICrawlEngine>();
        crawlEngine.Setup(engine => engine.CrawlAsync(It.IsAny<Post>(), strategy.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CrawlResult>.Success(new CrawlResult([], [new DiscoveredComment("bot", "hello", DateTime.UtcNow)])));

        var orchestrator = new CrawlOrchestrator(
            postRepo,
            mediaRepo,
            commentRepo,
            siteRepo.Object,
            crawlEngine.Object,
            [strategy.Object],
            new RoundRobinScheduler(),
            new CrawlPipeline(),
            Options.Create(new CrawlerSettings { MaxListPages = 0 }),
            NullLogger<CrawlOrchestrator>.Instance);

        await orchestrator.RunCycleAsync(CancellationToken.None);

        commentRepo.Comments.Should().ContainSingle();
        commentRepo.Comments.Single().Content.Should().Be("hello");
        postRepo.Posts.Single().Status.Should().Be(PostStatus.Collected);
    }

    [Fact]
    public async Task DownloadOrchestrator_DbFallback_ShouldUsePostExternalId()
    {
        var postRepo = new InMemoryPostRepository();
        var mediaRepo = new InMemoryMediaRepository();
        postRepo.Seed(new Post { SiteKey = "aagag", ExternalId = "external-99", Url = "https://aagag.com/99" });
        mediaRepo.Seed(new Media { PostId = 1, MediaUrl = "https://cdn.test/1.jpg" });

        var siteRepo = new Mock<ISiteRepository>();
        siteRepo.Setup(repo => repo.GetActiveSitesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Site { SiteKey = "aagag", BaseUrl = "https://aagag.com" }]);

        var downloader = new Mock<IMediaDownloader>();
        downloader.Setup(service => service.DownloadAsync(
                It.IsAny<Media>(),
                "aagag",
                "external-99",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DownloadResult>.Success(new DownloadResult("abc123abc123", 10, "image/jpeg", "D:\\CrawlerData\\aagag\\external-99\\abc.jpg")));

        var orchestrator = new DownloadOrchestrator(
            mediaRepo,
            postRepo,
            siteRepo.Object,
            downloader.Object,
            new RoundRobinScheduler(),
            new CrawlPipeline(),
            Options.Create(new CrawlerSettings()),
            NullLogger<DownloadOrchestrator>.Instance);

        await orchestrator.RunCycleAsync(CancellationToken.None);

        downloader.Verify(service => service.DownloadAsync(
            It.IsAny<Media>(),
            "aagag",
            "external-99",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DownloadOrchestrator_Failure_ShouldClearLeaseAndPreserveBackoff()
    {
        var postRepo = new InMemoryPostRepository();
        var mediaRepo = new InMemoryMediaRepository();
        postRepo.Seed(new Post { SiteKey = "aagag", ExternalId = "external-100", Url = "https://aagag.com/100" });
        mediaRepo.Seed(new Media { PostId = 1, MediaUrl = "https://cdn.test/fail.jpg" });

        var siteRepo = new Mock<ISiteRepository>();
        siteRepo.Setup(repo => repo.GetActiveSitesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Site { SiteKey = "aagag", BaseUrl = "https://aagag.com" }]);

        var downloader = new Mock<IMediaDownloader>();
        downloader.Setup(service => service.DownloadAsync(
                It.IsAny<Media>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DownloadResult>.Failure("boom", "DOWNLOAD_ERROR"));

        var orchestrator = new DownloadOrchestrator(
            mediaRepo,
            postRepo,
            siteRepo.Object,
            downloader.Object,
            new RoundRobinScheduler(),
            new CrawlPipeline(),
            Options.Create(new CrawlerSettings()),
            NullLogger<DownloadOrchestrator>.Instance);

        await orchestrator.RunCycleAsync(CancellationToken.None);

        var failed = mediaRepo.MediaList.Single();
        failed.Status.Should().Be(MediaStatus.PendingDownload);
        failed.LeaseUntil.Should().BeNull();
        failed.NextRetryAt.Should().NotBeNull();
    }
}
