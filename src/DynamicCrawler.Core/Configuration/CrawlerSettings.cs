namespace DynamicCrawler.Core.Configuration;

/// <summary>크롤러 전역 설정 (IOptions 패턴)</summary>
public sealed class CrawlerSettings
{
    public const string SectionName = "Crawler";

    /// <summary>파일 저장 루트 경로</summary>
    public string StorageRoot { get; set; } = @"D:\CrawlerData";

    /// <summary>기본 동시 다운로드 수</summary>
    public int DefaultMaxDownloads { get; set; } = 4;

    /// <summary>브라우저 재기동 기준 처리 건수</summary>
    public int BrowserRecycleCount { get; set; } = 50;

    /// <summary>유휴 타임아웃 (분)</summary>
    public int IdleTimeoutMinutes { get; set; } = 10;

    /// <summary>Lease 시간 (초)</summary>
    public int LeaseSeconds { get; set; } = 300;

    /// <summary>최대 재시도 횟수</summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>크롤링 대상 페이지 목록 최대 페이지 수</summary>
    public int MaxListPages { get; set; } = 3;
}
