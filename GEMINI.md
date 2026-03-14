# Dynamic-Crawler 프로젝트 가이드 (GEMINI.md)

이 문서는 Dynamic-Crawler 프로젝트의 구조, 기술 스택, 빌드 및 실행 방법, 그리고 개발 컨벤션을 정의합니다. 이 프로젝트는 자바스크립트 렌더링이 필요한 동적 웹사이트에서 데이터를 수집하고 저장하는 고성능 .NET 기반 크롤링 솔루션입니다.

## 🏗 프로젝트 개요 (Project Overview)

Dynamic-Crawler는 PuppeteerSharp를 이용한 헤드리스 브라우징과 .NET `Channel<T>`을 이용한 고성능 생산자-소비자(Producer-Consumer) 파이프라인을 결합하여 설계되었습니다. 수집된 데이터는 Supabase(PostgreSQL)에 저장되며, 미디어 파일은 로컬 스토리지에 다운로드됩니다.

### 핵심 아키텍처 (Layered Architecture)
- **Core**: 도메인 모델, 인터페이스, 공통 열거형(Enum), Result 패턴 정의.
- **Data.Supabase**: Supabase를 이용한 데이터 영속성 계층.
- **Engine**: PuppeteerSharp 기반의 크롤링 엔진 및 브라우저 관리.
- **Downloader**: 미디어 파일 다운로드 및 중복 방지(SHA256).
- **Orchestrator**: 크롤링(Producer)과 다운로드(Consumer)를 조율하는 파이프라인.
- **Worker**: 시스템 진입점(Host), 의존성 주입(DI), 백그라운드 서비스.
- **Sites**: 사이트별 파싱 전략(ISiteStrategy) 구현체 (예: Aagag).

## 🛠 주요 기술 스택 (Tech Stack)

- **Language**: C# (.NET 9/10)
- **Crawler Engine**: PuppeteerSharp
- **Storage/DB**: Supabase (PostgreSQL), Local File System
- **Pipeline**: System.Threading.Channels
- **Resilience**: Polly (Retry, Timeout)
- **Logging**: Serilog
- **Testing**: xUnit, FluentAssertions, Moq

## 🚀 빌드 및 실행 (Building and Running)

### 필수 요구사항
- .NET SDK (버전 9.0 이상 권장)
- Supabase 계정 및 프로젝트 URL/API Key (설정 필요)

### 주요 명령어
- **전체 빌드**:
  ```powershell
  dotnet build
  ```
- **워커 서비스 실행**:
  ```powershell
  dotnet run --project src/DynamicCrawler.Worker/DynamicCrawler.Worker.csproj
  ```
- **테스트 실행**:
  ```powershell
  dotnet test
  ```

### 설정 (Configuration)
`src/DynamicCrawler.Worker/appsettings.json` 파일을 통해 다음 항목을 설정해야 합니다:
- `SupabaseSettings`: URL, Key, BucketName 등
- `CrawlerSettings`: 수집 주기, 브라우저 타임아웃 등
- `Serilog`: 로깅 경로 및 레벨

## 📝 개발 컨벤션 (Development Conventions)

1. **Result 패턴 사용**: 모든 서비스 로직은 예외를 던지는 대신 `Result<T>` 타입을 반환하여 결과의 성공/실패를 명시적으로 처리합니다.
2. **의존성 역전 원칙 (DIP)**: 상위 모듈은 하위 모듈에 직접 의존하지 않고 인터페이스(`ICrawlEngine`, `IMediaRepository` 등)에 의존합니다.
3. **매퍼(Mapper) 패턴**: DB 엔티티(SupabaseModel)와 도메인 모델(Core.Models)을 분리하고 전용 매퍼를 통해 변환합니다.
4. **비동기 처리**: I/O 바운드 작업은 항상 `async/await`를 사용하며, 성능 최적화를 위해 `Channel<T>`을 적극 활용합니다.
5. **테스트 주도**: 새로운 기능이나 버그 수정 시 `tests/DynamicCrawler.Tests`에 테스트 케이스를 추가합니다.

## 📂 프로젝트 구조 가이드

- `docs/`: 요구사항, 아키텍처, 로드맵 등 상세 문서 포함.
- `sql/`: 데이터베이스 테이블 및 함수 생성 스크립트.
- `src/Sites/`: 새로운 수집 대상을 추가할 때 `ISiteStrategy`를 구현하는 곳.

---
*이 가이드는 프로젝트의 전반적인 이해를 돕기 위해 작성되었습니다. 상세한 내용은 각 프로젝트 내 README.md를 참고하세요.*
