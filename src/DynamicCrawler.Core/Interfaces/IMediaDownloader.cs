using DynamicCrawler.Core.Common;
using DynamicCrawler.Core.Models;
using DynamicCrawler.Core.Results;

namespace DynamicCrawler.Core.Interfaces;

/// <summary>미디어 다운로드 서비스</summary>
public interface IMediaDownloader
{
    /// <summary>미디어 파일을 다운로드하고 SHA256 해싱하여 저장</summary>
    Task<Result<DownloadResult>> DownloadAsync(Media media, string siteKey, string postExternalId, CancellationToken ct = default);
}
