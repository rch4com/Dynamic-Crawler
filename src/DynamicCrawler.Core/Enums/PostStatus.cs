namespace DynamicCrawler.Core.Enums;

/// <summary>게시글 처리 상태머신</summary>
public enum PostStatus
{
    /// <summary>발견됨 — 크롤링 대기</summary>
    Discovered,

    /// <summary>크롤링 중 (lease 획득됨)</summary>
    Collecting,

    /// <summary>크롤링 완료 — 미디어 추출됨</summary>
    Collected,

    /// <summary>모든 미디어 다운로드 완료</summary>
    Done,

    /// <summary>크롤링 실패</summary>
    Failed
}
