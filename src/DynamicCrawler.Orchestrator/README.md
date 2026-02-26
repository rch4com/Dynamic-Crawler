# DynamicCrawler.Orchestrator

`DynamicCrawler.Orchestrator`는 다운로더(Consumer)와 크롤러(Producer) 사이에 비동기/고성능의 안전한 데이터 처리 파이프라인(`Channel<T>`) 매커니즘을 관장하는 코어 제어탑 모듈입니다.

## 주요 관리 기능

- **생산/소비 파이프라인 (Channel-based Architecture)**  
  `System.Threading.Channels`를 사용해 게시글 크롤링 도중 미디어가 발견되면, 이를 DB에 기록한 뒤 즉시 비동기 큐(`CrawlPipeline.DownloadChannel`)에 `Write` 시킵니다.  
  결과적으로 `DownloadOrchestrator`는 이 큐에서 대기하다가 신호를 건네받고 즉시 다른 Task 풀을 가져다 다운로드를 수행하므로 CPU 대기 시간을 획기적으로 줄였습니다(비동기 I/O 및 병렬 효율 극대화).
- **작업 점유 체계 (Lease / Lock)**  
  동일한 대상을 다른 Worker 노드가 중복 처리하지 못하도록 `ClaimNextAsync` 메서드와 `LeaseSeconds`를 기반으로 한 점유 방식(Lock Mechanism)을 도입하여, 분산/수평 아키텍처 스케일링에서 멱등성을 보장합니다.
- **다중 사이트 스케줄링 (`RoundRobinScheduler`)**  
  활성(Active)으로 간주되는 다수 대상 타겟(Site)을 순서대로 순회하며, 사이트 집중이나 서버 과부하를 회피하는 스케줄링을 구현했습니다.
- **Rollback / Recovery (오케스트레이션 안정성)**  
  처리 중 불의의 예외로 인스턴스가 멈춰 상태가 어정쩡해진 `Collecting` / `Downloading` 객체(`Orphaned`)들의 소명 기한(Lease)이 경과 시, 재개 가능한 처음 상태로 시스템 내 자동 롤백 복구를 시연합니다.
