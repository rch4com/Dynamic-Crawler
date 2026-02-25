# Dynamic-Crawler 요구사항 명세서 (Requirements)

## 1. 개요
Dynamic-Crawler는 자바스크립트 렌더링이 필수적인 동적 웹사이트(SPA 등)를 포함하여 다양한 웹소스에서 게시글, 미디어(이미지/비디오), 댓글 데이터를 안정적으로 수집하고 저장하기 위해 설계된 .NET 호스트 기반의 크롤링 파이프라인 시스템입니다.

---

## 2. 기능적 요구사항 (Functional Requirements)

1. **동적 웹페이지 크롤링**
   - Headless 브라우저(PuppeteerSharp)를 구동하여 JS 로딩, DOM 완성(Networkidle 대기) 이후의 완성된 HTML을 확보해야 합니다.
   - 불필요한 네트워크 트래픽(CSS, Font 등)을 차단하는 최적화(Network Optimizer)가 적용되어야 합니다.

2. **모듈식 사이트 파싱 (전략 패턴)**
   - 다양한 사이트에 대응하기 위해 사이트별 고유 파싱 로직을 `ISiteStrategy` 인터페이스로 캡슐화해야 합니다.
   - 예시 타겟: `aagag.com` (게시글 목록, 이미지/비디오 미디어, 댓글).

3. **게시글 및 미디어 추출**
   - 사이트별 게시글 고유 ID(ExternalId) 기반 중복 수집을 방지해야 합니다.
   - HTML 내에 포함된 미디어 콘텐츠를 찾아내 확장자 및 MimeType에 맞게 다운로드 대상으로 등록해야 합니다.

4. **미디어 안정적 다운로드**
   - Polly 기반의 재시도(Retry) 및 서킷 브레이커(Circuit Breaker)를 도입하여 원격 서버 불안정에 대응해야 합니다.
   - 미디어 다운로드 시 SHA256 해시를 추출하여 동일 파일의 중복 저장(Deduplication)을 방지해야 합니다.

5. **작업 배분 및 상태 관리**
   - 발견(Discovered) → 처리중(Collecting/Downloading) → 완료(Completed) 상태 전이 과정을 완벽히 관리해야 합니다.
   - 특정 아이템의 처리가 중간에 실패하거나 중단될 시, 일정 시간(Lease) 경과 후 재시도 가능한 롤백 메커니즘을 가져야 합니다.

---

## 3. 비기능적 요구사항 (Non-Functional Requirements)

1. **내결함성 및 안정성 (Resilience)**
   - Windows Service(또는 Linux Daemon) 형태로 장기간 실행되어도 메모리 누수가 없도록 브라우저 인스턴스를 주기적으로 재생성(Recycle)해야 합니다.
   - 파이프라인 오케스트레이터를 통해 특정 작업의 예외가 시스템 전체를 다운시키지 않도록 격리해야 합니다.

2. **확장성 및 교체 용이성 (Scalability)**
   - 데이터베이스 영속성 레이어는 `IPostRepository`, `IMediaRepository` 기반으로 분리되어 있어, 현재 배포된 Supabase 외에 MSSQL/PostgreSQL/MongoDB 등으로 즉시 교체 가능해야 합니다.
   - 크롤러 코어(Producer)와 미디어 다운로더(Consumer)는 `System.Threading.Channels`를 활용하여 병렬화 및 비동기 처리의 효율을 극대화해야 합니다.

3. **보안 (Security)**
   - Supabase 영속성 사용 시 데이터베이스 계층의 RLS(Row Level Security) 제약을 우회하지 않고, 인가된 Service Role 하에서만 작동하여 데이터 무결성을 보장해야 합니다.
   - 봇 차단 정책(Anti-Bot Challenge)에 대한 기본 User-Agent 위장 및 향후 프록시(Proxy) 체인 연계 기반이 마련되어야 합니다.
