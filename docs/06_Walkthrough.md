# Dynamic-Crawler 구현 완료 Walkthrough

## 작업 요약

4개 .NET 스킬 기반 비판적 검토를 거쳐 12개 개선사항을 반영한 전체 구현을 완료했습니다.

---

## 프로젝트 구조 (8개 프로젝트)

| 프로젝트 | 역할 | 빌드 |
|----------|------|------|
| `DynamicCrawler.Core` | 순수 도메인 + 인터페이스 | ✅ |
| `DynamicCrawler.Data.Supabase` | Supabase 구현 (교체 가능) | ✅ |
| `DynamicCrawler.Engine` | PuppeteerSharp 크롤링 | ✅ |
| `DynamicCrawler.Downloader` | 미디어 다운로드 (IHttpClientFactory + Polly) | ✅ |
| `DynamicCrawler.Orchestrator` | Channel 파이프라인 + 스케줄링 | ✅ |
| `DynamicCrawler.Worker` | Host (Serilog + Windows Service) | ✅ |
| `DynamicCrawler.Sites.Aagag` | aagag.com 파서 | ✅ |
| `DynamicCrawler.Tests` | 테스트 프레임워크 준비 | ✅ |

---

## 적용된 스킬 리뷰 개선사항

| 개선 | 구현 파일 |
|------|-----------|
| sealed + required/init | [Post.cs](file:///d:/sources/github/Dynamic-Crawler/src/DynamicCrawler.Core/Models/Post.cs), [Site.cs](file:///d:/sources/github/Dynamic-Crawler/src/DynamicCrawler.Core/Models/Site.cs) 등 |
| `Result<T>` | [Result.cs](file:///d:/sources/github/Dynamic-Crawler/src/DynamicCrawler.Core/Common/Result.cs) |
| record DTO | [CrawlResult.cs](file:///d:/sources/github/Dynamic-Crawler/src/DynamicCrawler.Core/Results/CrawlResult.cs) |
| `IOptions<T>` | [CrawlerSettings.cs](file:///d:/sources/github/Dynamic-Crawler/src/DynamicCrawler.Core/Configuration/CrawlerSettings.cs) |
| IHttpClientFactory + Polly | [DownloaderServiceExtensions.cs](file:///d:/sources/github/Dynamic-Crawler/src/DynamicCrawler.Downloader/DownloaderServiceExtensions.cs) |
| Serilog | [Program.cs](file:///d:/sources/github/Dynamic-Crawler/src/DynamicCrawler.Worker/Program.cs) |
| Scoped BackgroundService | [CrawlerBackgroundService.cs](file:///d:/sources/github/Dynamic-Crawler/src/DynamicCrawler.Orchestrator/CrawlerBackgroundService.cs) |
| `IUnitOfWork` 제거 | Repository 직접 주입 |
| `ConfigureAwait(false)` | 모든 라이브러리 프로젝트 |

---

## 빌드 검증 결과

```
✅ 전체 빌드 성공 (경고 0, 에러 0)
8개 프로젝트 모두 컴파일 통과
```

---

## Persistence 교체 방법

```diff
// Worker/Program.cs에서 1줄만 변경
-builder.Services.AddSupabasePersistence(builder.Configuration);
+builder.Services.AddEfCorePersistence(builder.Configuration);
```

---

## 보안 및 테스트 (Phase 2 추가 진행)

### 1. Supabase 보안 취약점 해소
- [x] **RLS 활성화 및 정책 추가**: `service_role` 접근에 대한 Row Level Security 적용 (`003_secure_rls_policies.sql`)
- [x] **Search Path 취약점 방어**: 기존 생성된 DB RPC 함수에 `SECURITY DEFINER SET search_path = ''` 옵션 적용
- [x] Security Advisor 검증: 발견된 보안 취약점 경고 0건 달성 (안전성 확보)

### 2. 단위 테스트 구현 (의존성 격리)
- [x] **InMemory Fakes**: `InMemoryPostRepository`, `InMemoryMediaRepository` 구현으로 DB 호출 없는 순수 로직 테스트 지원
- [x] **코어 로직 검증**: 파싱(`AagagSiteStrategy`), 확장자 맵핑(`ContentTypeMapper`), 결과 반환 패턴(`Result<T>`) 검증
- [x] **테스트 통과**: 총 27개 xUnit 테스트 케이스 모두 정상 통과

---

## E2E 실행 및 안정화 (Phase 7 완료)

**1. Worker 실행 및 파이프라인 연동 확인**
- `DynamicCrawler.Worker` 프로젝트 기동 기반의 엔드투엔드(`NetworkOptimizer` 적용, 파이프라인 가동) 흐름을 검증 완료.
- Serilog (Rolling file) 로깅 체계를 통해 상세 디버깅 및 진행 상황 모니터링 확인 완료.

**2. 런타임/DB 안정성 개선(버그 픽스)**
- **Puppeteer 안정화**: 
  - `WaitUntilNavigation.DOMContentLoaded`를 `Networkidle2`로 상향하여 JS 리다이렉션 등의 흐름 안착 대기.
  - 브라우저 인스턴스 획득, `GoToAsync`, `GetContentAsync` 각각에 명시적인 `Task.WhenAny` 타임아웃 예외를 적용하여 무한 행업(Hang) 방지 확인.
- **Supabase DB 중복 키 오류 해결**: 
  - `Id` 필드(`long` 기반 `0` 할당) 삽입으로 인한 `duplicate key value violates unique constraint` 발생. 모델의 속성을 `long? Id`로 변경하고 Mapper까지 널-세이프하게 수정하여 자동 식별자 부여 문제 타개.
  - `OnConflict`의 제약조건값 지정 시, 빈칸이 들어간 파라미터 배열(`site_key, external_id`) 파싱 파괴를 발견해 `site_key,external_id` (공백 제거)로 수정, 중복 Upsert 시 충돌/갱신 로직 회복.

**3. Anti-bot 챌린지 대응 및 통합 테스트 (Integration Tests)**
- `aagag.com` 등의 크롤링 과정에서 봇 대항 페이지 모델 반환 시(`303 See Other` 또는 Captcha), Worker가 예외로 죽어버리지 않고 안전하게 크롤링 0건 처리 및 다음 사이클로 넘기는 안전성 입증. 
- (Puppeteer 엔진에서 `User-Agent`까지 데스크톱 환경으로 상향 적용하였으나, 대상 서버 측의 강력한 정책으로 Captcha가 표출됨은 확인됨. 이는 향후 프록시 도입 등으로 해결)
- **Orphaned 데이터 롤백 통합 테스트 작성**: 위와 같은 예기치 않은 중단 등으로 `Collecting` 혹은 `Downloading` 중 `Lease`가 만료된 항목들이 `CrawlerBackgroundService` 의 다음 주기나 시작 시에 올바르게 `Discovered` 및 `PendingDownload` 로 복원되는지 검증하는 E2E 성격의 롤백 테스트(`OrphanedDataRecoveryTests`)를 작성하였으며, **28개 테스트 전체 통과** 확인 성공.

---

## 다음 방향 제안
프로젝트 내 코어 로직과 Worker 연동, DB 입출력, 상태 복구(Rollback) 파이프라인이 모두 성공적으로 완성 및 검증되었습니다.
- **프록시 및 브라우저 우회 고도화**: 현재 Target 사이트(aagag 등)에서 빈번한 접근 시 Captcha 응답을 주고 있으므로, 향후 상용 배포 전 외부 프록시 연계나 Headless 설정 추가 튜닝 권장.
- **실 운영 배포**: `service_role`이 적용된 안전한 DB 환경에서 Windows Service나 Docker 기반으로 즉결 배포 가능.
