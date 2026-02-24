using DynamicCrawler.Core.Configuration;
using Microsoft.Extensions.Options;

namespace DynamicCrawler.Downloader;

/// <summary>저장 경로 생성: {root}\{siteKey}\{postId}\{sha256}.{ext}</summary>
public sealed class PathResolver(IOptions<CrawlerSettings> settings)
{
    private readonly string _root = settings.Value.StorageRoot;

    /// <summary>미디어 파일의 최종 저장 경로를 생성</summary>
    public string Resolve(string siteKey, string postExternalId, string sha256, string extension)
    {
        var dir = Path.Combine(_root, siteKey, postExternalId);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"{sha256}{extension}");
    }

    /// <summary>임시 다운로드 파일 경로</summary>
    public string ResolveTempPath(string siteKey, string postExternalId)
    {
        var dir = Path.Combine(_root, siteKey, postExternalId, ".tmp");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, $"{Guid.NewGuid():N}.download");
    }
}
