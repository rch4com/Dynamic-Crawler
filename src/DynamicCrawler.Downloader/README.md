# DynamicCrawler.Downloader

`DynamicCrawler.Downloader`는 원격 매체의 네트워크 장애나 불안정성에 대비하여, 다운로드(미디어 콘텐츠)를 안정적이고 일관되게 수행하기 위해 구현된 전문 모듈입니다.

## 주요 특징

- **`IHttpClientFactory` 분배 및 풀링**  
  매 다운로드 시 HttpClient 인스턴스를 무분별하게 생성하지 않고 Factory 풀에서 관리되는 안전한 풀링을 기반으로 성능을 높였습니다.
- **Polly 기반의 복원력(Resilience)**  
  웹 미디어 데이터 호출 시 발생 가능한 순단, Timeout 오류 등을 방어하기 위해 `Polly`의 `WaitAndRetryAsync`, `CircuitBreakerAsync` 확장 정책을 등록해 다운로드 탄력성을 보장합니다.
- **SHA256 해시 데이터 정합성 보장**  
  중복 미디어 저장에 따른 디스크 낭비를 방지하고자, 스트림 형태로 데이터를 내려받은 후 실시간으로 SHA256 체크섬을 검사하여 동일한 콘텐츠를 거름으로써(Deduplication) 효율을 향상했습니다.
- **확장성**  
  다양한 미디어 포맷(Mime Type)을 파일 형식으로 자유롭게 변환하는 `ContentTypeMapper` 등을 제공합니다.
