# DynamicCrawler.Sites.Aagag

`DynamicCrawler.Sites.Aagag` 프로젝트는 코어에 정의된 크롤링 파서 계약인 `ISiteStrategy` 인터페이스를 상속하여 타겟 도메인(`aagag.com`)의 HTML 페이지 요소를 구체적으로 추출하는 파싱 특화 플러그인입니다.

## 주요 관리 기능

- **`AagagSiteStrategy.cs`**  
  AngleSharp 파서를 활용하여 대상 사이트 고유의 DOM(Document Object Model) 트리를 탐색하고 각 객체(`Post`, `DiscoveredMedia`, `DiscoveredComment`)를 추출합니다.
- **비즈니스 로직(데이터 식별) 캡슐화**  
  목록의 URL 리스트 구조 조립, 게시글 ID 파싱(`idx=...`), 사이트 내부에 존재하는 비디오 및 이미지의 `src` 속성 색출, 댓글 내역 등을 추출하는 등 각 사이트의 웹 구조 변화 시 이 프로젝트 하나만 수정하면 전체 파이프라인이 자동 적응하도록 되어있습니다.
- **의존성 주입 확장 매서드 (`AagagServiceExtensions.cs`)**  
  이 라이브러리를 사용하는 최상위 호스트(`Worker`)가 단 한 줄의 `services.AddAagagSiteStrategy()` 만으로 스캐너를 등록할 수 있게 해줍니다.
