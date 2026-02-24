using AngleSharp;
using AngleSharp.Html.Parser;
using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Core.Models;
using DynamicCrawler.Core.Results;

namespace DynamicCrawler.Sites.Aagag;

/// <summary>aagag.com 사이트 파싱 전략</summary>
public sealed class AagagSiteStrategy : ISiteStrategy
{
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
            if (string.IsNullOrEmpty(href) || !href.Contains("idx=")) continue;

            var idx = ExtractIdx(href);
            if (string.IsNullOrEmpty(idx)) continue;

            var title = link.QuerySelector("span")?.TextContent?.Trim();

            posts.Add(new Post
            {
                SiteKey = siteKey,
                ExternalId = idx,
                Url = $"https://aagag.com{href}",
                Title = title
            });
        }

        return posts
            .GroupBy(p => p.ExternalId)
            .Select(g => g.First())
            .ToList();
    }

    public IReadOnlyList<DiscoveredMedia> ParseMedia(string html)
    {
        var parser = new HtmlParser();
        var doc = parser.ParseDocument(html);
        var result = new List<DiscoveredMedia>();

        var container = doc.QuerySelector("div.stag.img");
        if (container is null) return result;

        // 이미지
        foreach (var img in container.QuerySelectorAll("img"))
        {
            var src = img.GetAttribute("src");
            if (string.IsNullOrEmpty(src)) continue;
            src = NormalizeUrl(src);
            result.Add(new DiscoveredMedia(src, GuessContentType(src)));
        }

        // 동영상 (video > source 또는 video[src])
        foreach (var video in container.QuerySelectorAll("video"))
        {
            var src = video.GetAttribute("src");
            if (string.IsNullOrEmpty(src))
            {
                var source = video.QuerySelector("source");
                src = source?.GetAttribute("src");
            }
            if (string.IsNullOrEmpty(src)) continue;
            src = NormalizeUrl(src);
            result.Add(new DiscoveredMedia(src, GuessContentType(src)));
        }

        return result;
    }

    public IReadOnlyList<DiscoveredComment> ParseComments(string html)
    {
        var parser = new HtmlParser();
        var doc = parser.ParseDocument(html);
        var result = new List<DiscoveredComment>();

        var commentArea = doc.QuerySelector("#comment");
        if (commentArea is null) return result;

        // aagag 댓글은 서버사이드 렌더링, 구조가 단순
        foreach (var node in commentArea.Children)
        {
            var text = node.TextContent?.Trim();
            if (!string.IsNullOrEmpty(text))
                result.Add(new DiscoveredComment(null, text, null));
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
        if (url.StartsWith("//")) return "https:" + url;
        return url;
    }

    private static string? GuessContentType(string url)
    {
        var ext = Path.GetExtension(new Uri(url).AbsolutePath).ToLowerInvariant();
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
