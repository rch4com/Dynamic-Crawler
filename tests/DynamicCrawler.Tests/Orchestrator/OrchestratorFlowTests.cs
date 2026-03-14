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

    [Fact]
    public async Task DownloadOrchestrator_Duplicate_ShouldMarkAsSkippedWithoutRetry()
    {
        var postRepo = new InMemoryPostRepository();
        var mediaRepo = new InMemoryMediaRepository();
        postRepo.Seed(new Post { SiteKey = "aagag", ExternalId = "external-101", Url = "https://aagag.com/101" });
        mediaRepo.Seed(new Media { PostId = 1, MediaUrl = "https://cdn.test/duplicate.jpg", RetryCount = 2 });

        var siteRepo = new Mock<ISiteRepository>();
        siteRepo.Setup(repo => repo.GetActiveSitesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Site { SiteKey = "aagag", BaseUrl = "https://aagag.com" }]);

        var downloader = new Mock<IMediaDownloader>();
        downloader.Setup(service => service.DownloadAsync(
                It.IsAny<Media>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DownloadResult>.Success(
                new DownloadResult("dup123dup123", 0, "image/jpeg", null, IsDuplicate: true)));

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

        var skipped = mediaRepo.MediaList.Single();
        skipped.Status.Should().Be(MediaStatus.SkippedDuplicate);
        skipped.RetryCount.Should().Be(0);
        skipped.LeaseUntil.Should().BeNull();
        skipped.NextRetryAt.Should().BeNull();
        skipped.Sha256.Should().Be("dup123dup123");
    }

    [Fact]
    public async Task CrawlOrchestrator_ShouldThrottleRepeatedDiscoveryPerSite()
    {
        var postRepo = new InMemoryPostRepository();
        postRepo.Seed(new Post { SiteKey = "aagag", ExternalId = "42", Url = "https://aagag.com/42" });

        var mediaRepo = new InMemoryMediaRepository();
        var commentRepo = new InMemoryCommentRepository();

        var siteRepo = new Mock<ISiteRepository>();
        siteRepo.Setup(repo => repo.GetActiveSitesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Site { SiteKey = "aagag", BaseUrl = "https://aagag.com" }]);

        var strategy = new Mock<ISiteStrategy>();
        strategy.SetupGet(value => value.SiteKey).Returns("aagag");
        strategy.Setup(value => value.BuildListUrl(1)).Returns("https://aagag.com/issue/?page=1");
        strategy.Setup(value => value.ParseList(It.IsAny<string>(), "aagag")).Returns([]);

        var crawlEngine = new Mock<ICrawlEngine>();
        crawlEngine.Setup(engine => engine.GetHtmlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("<html></html>"));
        crawlEngine.Setup(engine => engine.CrawlAsync(It.IsAny<Post>(), strategy.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CrawlResult>.Failure("empty", "EMPTY"));

        var orchestrator = new CrawlOrchestrator(
            postRepo,
            mediaRepo,
            commentRepo,
            siteRepo.Object,
            crawlEngine.Object,
            [strategy.Object],
            new RoundRobinScheduler(),
            new CrawlPipeline(),
            Options.Create(new CrawlerSettings { MaxListPages = 1 }),
            NullLogger<CrawlOrchestrator>.Instance);

        await orchestrator.RunCycleAsync(CancellationToken.None);
        await orchestrator.RunCycleAsync(CancellationToken.None);

        crawlEngine.Verify(engine => engine.GetHtmlAsync("https://aagag.com/issue/?page=1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
