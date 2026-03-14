using AngleSharp.Html.Parser;
using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Core.Models;
using DynamicCrawler.Core.Results;

namespace DynamicCrawler.Sites.Aagag;

/// <summary>aagag.com 사이트 전략 구현</summary>
public sealed class AagagSiteStrategy : ISiteStrategy
{
    private static readonly Uri BaseUri = new("https://aagag.com");

    public string SiteKey => "aagag";

    public string BuildListUrl(int page) =>
        $"https://aagag.com/issue/?page={page}";

    public IReadOnlyList<Post> ParseList(string html, string siteKey)
    {
        var parser = new HtmlParser();
        var doc = parser.ParseDocument(html);
        var posts = new List<Post>();

        foreach (var link in doc.QuerySelectorAll("a.article.t"))
        {
            var href = link.GetAttribute("href");
            if (string.IsNullOrEmpty(href) || !href.Contains("idx=", StringComparison.Ordinal))
            {
                continue;
            }

            var idx = ExtractIdx(href);
            if (string.IsNullOrEmpty(idx))
            {
                continue;
            }

            var title = link.QuerySelector("span")?.TextContent?.Trim();

            posts.Add(new Post
            {
                SiteKey = siteKey,
                ExternalId = idx,
                Url = new Uri(BaseUri, href).ToString(),
                Title = title
            });
        }

        return posts
            .GroupBy(post => post.ExternalId)
            .Select(group => group.First())
            .ToList();
    }

    public IReadOnlyList<DiscoveredMedia> ParseMedia(string html)
    {
        var parser = new HtmlParser();
        var doc = parser.ParseDocument(html);
        var result = new List<DiscoveredMedia>();

        var container = doc.QuerySelector("div.stag.img");
        if (container is null)
        {
            return result;
        }

        foreach (var img in container.QuerySelectorAll("img"))
        {
            var src = img.GetAttribute("src");
            if (string.IsNullOrWhiteSpace(src))
            {
                continue;
            }

            var normalizedUrl = NormalizeUrl(src);
            result.Add(new DiscoveredMedia(normalizedUrl, GuessContentType(normalizedUrl)));
        }

        foreach (var video in container.QuerySelectorAll("video"))
        {
            var src = video.GetAttribute("src");
            if (string.IsNullOrWhiteSpace(src))
            {
                src = video.QuerySelector("source")?.GetAttribute("src");
            }

            if (string.IsNullOrWhiteSpace(src))
            {
                continue;
            }

            var normalizedUrl = NormalizeUrl(src);
            result.Add(new DiscoveredMedia(normalizedUrl, GuessContentType(normalizedUrl)));
        }

        return result;
    }

    public IReadOnlyList<DiscoveredComment> ParseComments(string html)
    {
        var parser = new HtmlParser();
        var doc = parser.ParseDocument(html);
        var result = new List<DiscoveredComment>();

        var commentArea = doc.QuerySelector("#comment");
        if (commentArea is null)
        {
            return result;
        }

        foreach (var node in commentArea.Children)
        {
            var text = node.TextContent?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                result.Add(new DiscoveredComment(null, text, null));
            }
        }

        return result;
    }

    private static string? ExtractIdx(string href)
    {
        var parts = href.Split("idx=", StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[1].Split('&')[0] : null;
    }

    private static string NormalizeUrl(string url)
    {
        if (url.StartsWith("//", StringComparison.Ordinal))
        {
            return $"https:{url}";
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        return new Uri(BaseUri, url).ToString();
    }

    private static string? GuessContentType(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var ext = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            _ => null
        };
    }
}
