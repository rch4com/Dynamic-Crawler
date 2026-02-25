# Dynamic-Crawler 개발 및 유지보수 로드맵 (Roadmap)

## 1. 완료된 단계 (Completed Phases)

- **Phase 1: 인프라 및 핵심 파운데이션 (Foundation)**
  - .NET 10.0 기반 다중 프로젝트 솔루션 구축
  - 중앙 집중식 패키지 관리 (`Directory.Packages.props`) 및 Nullable 방어 로직 적용
- **Phase 2: 비즈니스 도메인 및 인터페이스 (Core Domain)**
  - `Result<T>` 및 Record 기반 DTO 아키텍처 수립
  - `IHttpClientFactory` 분리, CQRS 관점 기반 Repository 계약 명세화
- **Phase 3: 영속성 레이어 개발 (Supabase/DB)**
  - RLS 활성화, RPC 함수(`claim_next_post` 등) 적용을 통한 안전한 트랜잭션 수립
  - 충돌 시 무결성을 확보하기 위한 Postgrest API Upsert 픽스
- **Phase 4: 다운로더 및 복원력 내재화 (Downloader & Resilience)**
  - Polly 재시도/서킷브레이커 구현
  - SHA256을 거친 다운로드 미디어 파일 통합 중복 대응
- **Phase 5~6: 크롤링 엔진 (Engine & Sites)**
  - 재활용이 가능한 `BrowserManager` 풀 구비 (좀비 프로세스 방지)
  - 확장 가능한 `ISiteStrategy` 포트 구현 (`Aagag` 파서)
- **Phase 7: 파이프라인 통합 (Worker Orchestration)**
  - Producer/Consumer `Channel<T>` 비동기 구조
  - `Worker` 백그라운드 컨텍스트 내 Orphaned Rollback 스케줄 완수
  - Serilog 구조적 로깅 전면 도입 
- **Phase 8: 통합 및 안정성 (CI & Testing)**
  - `xUnit` 기반 28건 테스트(단위/통합 기능, Rollback 대응 테스트 포함) 작성 및 100% 성공
  - 봇 챌린지 징후(Networkidle 상향, 타임아웃 예외 처리) 파악 및 안전 실패(Fail-safe) 증명

---

## 2. 향후 진행 목표 (Future Roadmap)

### Phase 9: 우회 및 프록시 고도화 (Bypassing & Proxies)
- **현상**: Aagag와 같은 사이트에서 반복 접근 시 Cloudflare Anti-bot(Captcha) 응답 반환.
- **조치 방안**:
  - Puppeteer Sharp Stealth 플러그인 또는 C# 호환 회피 모듈 결합.
  - 상점 IP나 Residential Proxy 체인을 연동시키는 Proxy Rotation Manager 개발.
  - User-Agent 무작위 회전 및 쿠키 유지/캐싱 설계 구현.

### Phase 10: 수평적 타겟 사이트 확장 (Target Expansion)
- 다양한 타겟 사이트의 DOM 패턴을 분석하여 신규 `ISiteStrategy` 클래스를 라이브러리 추가 방식으로 투입.
- 비동기 JS가 필요하지 않은 단순 정적 페이지 수집 시, `Puppeteer` 대신 `HttpClient`를 쓰는 경량 `ICrawlEngine` 분리 구현(Hybrid 엔진 패턴).

### Phase 11: 운영 및 모니터링 적용 (Operations & Monitoring)
- **Grafana / Seq 로그 모음**: 현재 파일 시스템으로 수록되고 있는 Serilog 로그 타겟을 중앙 통합 로깅 시스템(Elastic Stack, Seq 등)으로 싱크.
- **애플리케이션 메트릭**: `Microsoft.Extensions.Diagnostics.HealthChecks`를 웹 API(`MapHealthChecks`) 포맷으로 외부 프로브(K8s) 상용 개방.

### Phase 12: 컨테이너화 및 클라우드 배포 (Containerization & Deployment)
- `Dockerfile` 패키징 적용. (Headless 리눅스 컨테이너 상의 원활한 Chromium 설치 의존 모듈 대비)
- Supabase 실 운영(Production) 데이터베이스 전환과 클라우드 배포 파이프라인(GitHub Actions) 정립 확립.
- 크롤러 인스턴스 확장에 대비하여, 큐(Queue/PostgreSQL 상태 테이블) 스케일 아웃이 잘 동작하는지 Distributed Test 수행.
