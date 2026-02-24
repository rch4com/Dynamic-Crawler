namespace DynamicCrawler.Downloader;

/// <summary>content-type → 확장자 매핑 (확장 가능)</summary>
public sealed class ContentTypeMapper
{
    private static readonly Dictionary<string, string> DefaultMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/jpg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/gif"] = ".gif",
        ["image/webp"] = ".webp",
        ["image/bmp"] = ".bmp",
        ["image/svg+xml"] = ".svg",
        ["video/mp4"] = ".mp4",
        ["video/webm"] = ".webm",
        ["video/ogg"] = ".ogv",
    };

    /// <summary>content-type에서 확장자를 추론</summary>
    public string GetExtension(string? contentType, string url)
    {
        // 1. content-type 매핑
        if (!string.IsNullOrEmpty(contentType) && DefaultMappings.TryGetValue(contentType, out var ext))
            return ext;

        // 2. URL 확장자에서 추론
        var urlExt = Path.GetExtension(new Uri(url).AbsolutePath);
        if (!string.IsNullOrEmpty(urlExt))
            return urlExt.ToLowerInvariant();

        // 3. 기본값
        return ".bin";
    }
}
