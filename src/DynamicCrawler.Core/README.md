# DynamicCrawler.Core

`DynamicCrawler.Core`는 시스템 아키텍처의 심장부(Domain Layer) 역할을 하는 가장 중요한 프로젝트입니다. 다른 어떠한 외부 프로젝트에도 의존하지 않는 순수 C# 라이브러리(Zero-Dependency)로 구성되어 있습니다.

## 주요 구성 요소

1. **Models (엔티티)**  
   - `Post`, `Media`, `Site`, `Comment` 등의 핵심 데이터 클래스는 C# 10+ 이상의 `sealed`, `init` 식별자 등을 차용해 불변성 및 안전성을 보장하고 있습니다.
2. **Interfaces (계약)**  
   - 영속성을 위한 저장소 계약: `IPostRepository`, `IMediaRepository`, `ISiteRepository`
   - 크롤러/분석기 구동을 위한 로직 계약: `ICrawlEngine`, `ISiteStrategy`, `IMediaDownloader`
3. **Common (`Result<T>`)**  
   - 애플리케이션 전체에서 일관된 성공 응답 및 에러 객체를 반환하기 위해 Exception 대신 구조화된 `Result<T>` 패턴을 채택했습니다.
4. **Configuration (설정 옵션)**  
   - `.NET IOptions` 패턴으로 바인딩하기 위해 솔루션 전반의 `CrawlerSettings` 설정 클래스를 위치시킵니다.

## 아키텍처 관점
클린 아키텍처 원칙에 따라 Data, Worker, Engine 등 모든 서브 구현체 프로젝트는 이 `Core` 프로젝트를 단방향으로 참조해야 합니다.
