# DynamicCrawler.Worker

`DynamicCrawler.Worker`는 애플리케이션 진입점(Host Runner)으로 설계된 .NET 애플리케이션이며 최상단 모듈입니다. Windows Service 등 데몬으로도 실행될 수 있도록 구성되어 있습니다.

## 주요 특징

- **`Program.cs` 통합 진입점 (Host Bootstrap)**  
  DI(Dependency Injection) 컨테이너를 생성하여 시스템 전체의 모든 서비스(크롤러 엔진, 다운로더, DB Persistence, 서드파티 크롤링 플러그인)를 조합하는 역할을 수행합니다.
- **백그라운드 스케줄러 (`CrawlerBackgroundService.cs`)**  
  `BackgroundService`를 상속하여 무한 루프 내에서 지속적인 스케줄 타임을 관장합니다. 예기치 않은 종료로 인한 Orphaned(고아) 트랜잭션 수거를 `ExecuteAsync`가 시작할 가장 첫 단계에서 실행하여 데이터 무결성을 유지시킵니다.
- **구조적 로깅 (Serilog 연동)**  
  콘솔과 파일(`logs/crawler-.log` 기반 RollingInterval 적용)로 동시에 로깅되며, 모든 에러와 수행 이벤트를 일자별로 추적하게 돕습니다.
- **`appsettings.json` 환경 제어**  
  데이터베이스 연결 정보(Supabase URL, Anon/Service Role 킷값), 저장소 루트 경로(StorageRoot), Idle Timeout, 동시성 제한 등의 옵션 파라미터를 중앙에서 편집하고 읽어옵니다.
- **Health Check 연동**  
  DB(`SupabaseHealthCheck`)와 Browser Node의 활성 상태 진단을 통합 관리하여 쿠버네티스(K8s)와 같은 모던 인프라에 배포 시 서비스 자가 진단망을 제공합니다.
