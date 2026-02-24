using DynamicCrawler.Sites.Aagag;
using FluentAssertions;

namespace DynamicCrawler.Tests.Sites;

public class AagagSiteStrategyTests
{
    private readonly AagagSiteStrategy _strategy = new();

    [Fact]
    public void SiteKey_ShouldBeAagag()
    {
        _strategy.SiteKey.Should().Be("aagag");
    }

    [Theory]
    [InlineData(1, "https://aagag.com/issue/?page=1")]
    [InlineData(3, "https://aagag.com/issue/?page=3")]
    public void BuildListUrl_ShouldGenerateCorrectUrl(int page, string expected)
    {
        _strategy.BuildListUrl(page).Should().Be(expected);
    }

    [Fact]
    public void ParseList_ShouldExtractPosts()
    {
        var html = """
            <html><body>
                <a class="article t" href="/issue/view/?idx=12345">
                    <span>테스트 게시글 1</span>
                </a>
                <a class="article t" href="/issue/view/?idx=67890">
                    <span>테스트 게시글 2</span>
                </a>
            </body></html>
            """;

        var posts = _strategy.ParseList(html, "aagag");

        posts.Should().HaveCount(2);
        posts[0].ExternalId.Should().Be("12345");
        posts[0].Title.Should().Be("테스트 게시글 1");
        posts[1].ExternalId.Should().Be("67890");
    }

    [Fact]
    public void ParseMedia_ShouldExtractImages()
    {
        var html = """
            <html><body>
                <div class="stag img">
                    <img src="https://cdn.aagag.com/image1.jpg" />
                    <img src="//cdn.aagag.com/image2.png" />
                </div>
            </body></html>
            """;

        var media = _strategy.ParseMedia(html);

        media.Should().HaveCount(2);
        media[0].Url.Should().Be("https://cdn.aagag.com/image1.jpg");
        media[0].ContentType.Should().Be("image/jpeg");
        media[1].Url.Should().StartWith("https:");
        media[1].ContentType.Should().Be("image/png");
    }

    [Fact]
    public void ParseMedia_ShouldExtractVideos()
    {
        var html = """
            <html><body>
                <div class="stag img">
                    <video src="https://cdn.aagag.com/video.mp4"></video>
                </div>
            </body></html>
            """;

        var media = _strategy.ParseMedia(html);

        media.Should().HaveCount(1);
        media[0].Url.Should().Be("https://cdn.aagag.com/video.mp4");
        media[0].ContentType.Should().Be("video/mp4");
    }

    [Fact]
    public void ParseMedia_NoContainer_ShouldReturnEmpty()
    {
        var html = "<html><body><p>본문만 있음</p></body></html>";

        var media = _strategy.ParseMedia(html);

        media.Should().BeEmpty();
    }

    [Fact]
    public void ParseComments_NoCommentSection_ShouldReturnEmpty()
    {
        var html = "<html><body><p>댓글 없음</p></body></html>";

        var comments = _strategy.ParseComments(html);

        comments.Should().BeEmpty();
    }
}
