namespace DynamicCrawler.Core.Results;

/// <summary>미디어 다운로드 결과</summary>
public sealed record DownloadResult(
    string Sha256,
    long ByteSize,
    string ContentType,
    /// <summary>IsDuplicate이 true일 때 null</summary>
    string? LocalPath,
    bool IsDuplicate = false);
