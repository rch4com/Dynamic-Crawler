namespace DynamicCrawler.Core.Enums;

/// <summary>미디어 다운로드 상태</summary>
public enum MediaStatus
{
    /// <summary>다운로드 대기</summary>
    PendingDownload,

    /// <summary>다운로드 진행 중</summary>
    Downloading,

    /// <summary>다운로드 완료</summary>
    Downloaded,

    /// <summary>중복 파일로 판단되어 저장을 건너뜀</summary>
    SkippedDuplicate,

    /// <summary>다운로드 실패</summary>
    Failed
}
