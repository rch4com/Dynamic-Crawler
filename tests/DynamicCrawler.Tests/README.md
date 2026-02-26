# DynamicCrawler.Tests

`DynamicCrawler.Tests` 프로젝트는 솔루션의 핵심 코디네이션 로직과 데이터 트랜잭션, 매퍼의 멱등성이 파괴되지 않도록 지속해서 검증(Regression Test)하는 역할을 수행하는 xUnit 기반 통합/유닛 테스트 프레임워크입니다.

## 주요 관리 기능

- **코어 비즈니스 로직 단위 테스트 (Unit Tests)**  
  - `DynamicCrawler.Core.Common.Result<T>` 패턴 동작의 일관성 확인.
  - `ContentTypeMapper`에 따른 확장자(Mime Type) 분석이 철저한지 점검.
- **플러그인 전략 검증 테스트**
  - `AagagSiteStrategy` 대상 파싱 정확도 및 불량 HTML 시나리오 안전성 확인.
- **데이터베이스 가상화 (InMemory Fakes)**  
  `IPostRepository`와 `IMediaRepository`를 위한 `InMemory` 계층 객체를 수록하여, 원격 데이터베이스나 Supabase API 키 연결에 오버헤드를 발생시키지 않고도 순수 비즈니스 로직만 단독으로 테스트할 수 있도록 격리 환경을 보장합니다.
- **Rollback 복원력 검증 (Integration Tests)**  
  `OrphanedDataRecoveryTests` 파일 등을 통해 파이프라인에서 Lease 시간(점유 시간)이 만료된 크롤링 데이터 및 미디어가 Orchestrator 첫 사이클에서 올바르게 시스템 초기화 롤백 처리를 진행하는지 검증합니다.
