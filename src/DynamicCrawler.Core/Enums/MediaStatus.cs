namespace DynamicCrawler.Core.Enums;

/// <summary>미디어 다운로드 상태머신</summary>
public enum MediaStatus
{
    /// <summary>다운로드 대기</summary>
    PendingDownload,

    /// <summary>다운로드 중 (lease 획득됨)</summary>
    Downloading,

    /// <summary>다운로드 완료</summary>
    Downloaded,

    /// <summary>다운로드 실패</summary>
    Failed
}
