using DynamicCrawler.Downloader;
using FluentAssertions;

namespace DynamicCrawler.Tests.Downloader;

public class ContentTypeMapperTests
{
    private readonly ContentTypeMapper _mapper = new();

    [Theory]
    [InlineData("image/jpeg", "https://example.com/img.jpg", ".jpg")]
    [InlineData("image/png", "https://example.com/img.png", ".png")]
    [InlineData("image/gif", "https://example.com/img.gif", ".gif")]
    [InlineData("image/webp", "https://example.com/img.webp", ".webp")]
    [InlineData("video/mp4", "https://example.com/vid.mp4", ".mp4")]
    [InlineData("video/webm", "https://example.com/vid.webm", ".webm")]
    public void GetExtension_WithKnownContentType_ShouldReturnCorrectExtension(
        string contentType, string url, string expected)
    {
        _mapper.GetExtension(contentType, url).Should().Be(expected);
    }

    [Fact]
    public void GetExtension_NullContentType_ShouldFallbackToUrl()
    {
        _mapper.GetExtension(null, "https://example.com/file.gif").Should().Be(".gif");
    }

    [Fact]
    public void GetExtension_UnknownBoth_ShouldReturnBin()
    {
        _mapper.GetExtension("application/octet-stream", "https://example.com/data")
            .Should().Be(".bin");
    }
}
