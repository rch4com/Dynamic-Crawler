# Dynamic-Crawler 구현 체크리스트

## Phase 1: 솔루션 및 빌드 인프라
- [x] .NET 10.0.101 SDK 확인
- [x] `DynamicCrawler.sln` 생성
- [x] 8개 프로젝트 생성 (Core/Data.Supabase/Engine/Downloader/Orchestrator/Worker/Sites.Aagag/Tests)
- [x] `Directory.Build.props` (net10.0, nullable, TreatWarningsAsErrors)
- [x] `Directory.Packages.props` (중앙 패키지 버전 관리)
- [x] 프로젝트 참조 설정

## Phase 2: Core 도메인
- [x] Enums: `PostStatus`, `MediaStatus`
- [x] Models: `Site`, `Post`, `Media`, `Comment` (sealed + required/init)
- [x] `Result<T>` 패턴
- [x] Results: `CrawlResult`, `DownloadResult` (record DTO)
- [x] Configuration: `CrawlerSettings` (IOptions)
- [x] Interfaces: `IPostRepository`, `IMediaRepository`, `ISiteRepository`
- [x] Interfaces: `ICrawlEngine`, `ISiteStrategy`, `IMediaDownloader`

## Phase 3: SQL + Data.Supabase
- [x] `001_create_tables.sql` (sites/posts/media/comments + 인덱스)
- [x] `002_create_functions.sql` (claim_next_post/media, rollback)
- [x] SupabaseSettings (IOptions)
- [x] Supabase 모델 (SupabasePost/Media/Site — BaseModel)
- [x] 매퍼 (PostMapper/MediaMapper/SiteMapper)
- [x] Repository 구현체 (SupabasePostRepository/MediaRepository/SiteRepository)
- [x] `ServiceCollectionExtensions.AddSupabasePersistence()`

## Phase 4: Downloader
- [x] `HashHelper` (SHA256)
- [x] `ContentTypeMapper` (확장 가능)
- [x] `PathResolver` ({root}/{siteKey}/{postId}/{sha256}.{ext})
- [x] `MediaDownloadService` (IHttpClientFactory + Polly)
- [x] `DownloaderServiceExtensions` (Polly retry/circuit breaker)

## Phase 5: Engine
- [x] `BrowserManager` (N건 재기동 + 유휴 타임아웃 + IAsyncDisposable)
- [x] `PuppeteerCrawlEngine` (DOM → ISiteStrategy 위임)
- [x] `NetworkOptimizer` (CSS/폰트/이미지 차단)

## Phase 6: Sites.Aagag
- [x] `AagagSiteStrategy` (AngleSharp 파서)
- [x] `AagagServiceExtensions` (DI 등록)

## Phase 7: Orchestrator + Worker
- [x] `RoundRobinScheduler`
- [x] `CrawlOrchestrator` (discover + claim + crawl + 미디어 등록)
- [x] `DownloadOrchestrator` (claim + download + 완료 처리)
- [x] `CrawlerBackgroundService` (Scoped DI + orphaned 롤백)
- [x] Worker `Program.cs` (전체 DI 조합)
- [x] `appsettings.json` (Supabase/Crawler/Serilog)
- [x] e2e_worker (Worker E2E 실행 및 디버그)
  - [x] Worker 단독 실행 시 DB 저장, 파일 다운로드 과정 점검
  - [x] Serilog 및 Seq(또는 콘솔) 로깅 확인
  - [x] **해결된 이슈**:
    - [x] Puppeteer 무응답 행업(Hang) 이슈 완화 (Task.WhenAny + Networkidle2 적용)
    - [x] Supabase C# 클라이언트의 `OnConflict` 파라미터 공백 파싱 에러 수정 (`site_key,external_id`)
    - [x] `Id` 필드 기본값(`0`) 삽입으로 인한 PrimaryKey 중복에러 수정 (`long? Id` 적용)
  - [x] 실제 대상 사이트(aagag 등) 크롤링 차단(Captcha) 시 예외 처리 확인
  - [x] Orphaned 데이터 처리 등 복구 시나리오 통합 테스트 작성 및 통과

## Phase 8: 빌드 검증
- [x] 전체 솔루션 빌드 성공 (경고 0, 에러 0)

## 향후 작업
- [x] Supabase SQL 스크립트 실행
- [x] 단위 테스트 작성
- [x] 통합 사이클 방어 테스트 (Orphaned 데이터 롤백 검증 테스트)
