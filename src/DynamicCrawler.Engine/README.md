# DynamicCrawler.Engine

`DynamicCrawler.Engine`은 동적 자바스크립트가 로딩된 페이지를 포함한 웹 환경에서 최상급의 안정성으로 HTML과 DOM 파편을 추출해오는 모듈입니다. 본 엔진 계층에서는 수집한 DOM 자체를 조작하지 않고 `ICrawlEngine` 계약만 준수하며, 파싱은 `ISiteStrategy`에게 위임합니다.

## 주요 특징

- **PuppeteerSharp 활용 및 관리**  
  `PuppeteerSharp` 기반 Headless 브라우저를 백엔드 서버 로직 내에서 부드럽게 가동시킵니다.
- **강건한 브라우저 생명주기 제어 (BrowserManager)**  
  단일 브라우저 인스턴스를 장시간 돌릴 때 생기는 고질적인 Memory Leak과 좀비 프로세스 문제를 제거하기 위해, 지정된 횟수 이상 또는 일정 유휴시간(Idle Timeout) 지속 시 백그라운드에서 브라우저를 안전하게 재생성(Recycling)합니다.
- **크롤링 및 Anti-bot 대응 회피 조치**  
  특정 페이지의 네트워크 트래픽 절약과 최적화를 도모하기 위해 폰트 및 기타 불필요 리소스를 제거하는 `NetworkOptimizer`가 플러그인 되어 있으며, 
  동시에 Headless 모드에서도 챌린지 페이지에 대응하기 위한 Timeout 제어(`WaitUntilNavigation.Networkidle2`, `Task.WhenAny` 타임아웃 래핑) 로직을 겸비합니다.
- **플러그인화된 구조**
  향후 JS 렌더링이 필요없는 가벼운 정적 처리 목적의 `HttpClient`용 크롤 엔진이나 Playwright 등 새로운 엔진으로 대체/확장하기 쉽습니다.
