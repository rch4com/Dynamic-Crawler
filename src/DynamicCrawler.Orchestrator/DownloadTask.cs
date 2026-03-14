using DynamicCrawler.Core.Models;

namespace DynamicCrawler.Orchestrator;

/// <summary>Channel을 통해 전달되는 다운로드 작업 단위 — Media + 라우팅 메타데이터</summary>
public sealed record DownloadTask(Media Media, string SiteKey, string PostExternalId);
