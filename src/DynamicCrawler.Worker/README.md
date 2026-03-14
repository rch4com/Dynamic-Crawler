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

---

## 실행 방법

### 1. 콘솔 모드로 실행

개발 및 디버깅 시 콘솔 애플리케이션으로 직접 실행할 수 있습니다.

#### 소스에서 바로 실행 (`dotnet run`)

```powershell
cd d:\sources\github\Dynamic-Crawler\src\DynamicCrawler.Worker
dotnet run
```

개발 환경 변수를 명시하려면:

```powershell
dotnet run --environment Development
```

#### 빌드 후 실행

```powershell
# 빌드
dotnet publish -c Release -o ./publish

# 실행
.\publish\DynamicCrawler.Worker.exe
```

> [!TIP]
> 콘솔 모드에서는 `Ctrl+C`로 Graceful Shutdown이 수행됩니다. `BackgroundService`의 `StopAsync`가 호출되어 진행 중인 작업이 안전하게 종료됩니다.

#### 환경별 설정 파일

| 파일 | 용도 |
|------|------|
| `appsettings.json` | 기본 설정 (Supabase 연결, 크롤러 옵션, Serilog) |
| `appsettings.Development.json` | 개발 환경 오버라이드 |

`DOTNET_ENVIRONMENT` 환경 변수를 `Development`, `Staging`, `Production` 등으로 설정하면 해당 환경의 설정 파일이 자동으로 병합됩니다.

---

### 2. Windows 서비스로 등록

`Program.cs`에 `AddWindowsService()`가 이미 구성되어 있으므로, 빌드된 실행 파일을 Windows 서비스로 등록하면 백그라운드 데몬으로 동작합니다.

#### 사전 준비

```powershell
# Release 빌드 및 게시
dotnet publish -c Release -o C:\Services\DynamicCrawler
```

> [!IMPORTANT]
> 서비스 등록 및 관리에는 **관리자 권한** PowerShell이 필요합니다.

#### 서비스 등록 (`sc.exe`)

```powershell
sc.exe create DynamicCrawler `
    binPath= "C:\Services\DynamicCrawler\DynamicCrawler.Worker.exe" `
    start= delayed-auto `
    DisplayName= "Dynamic Crawler Worker"
```

| 매개변수 | 설명 |
|----------|------|
| `binPath` | 게시된 실행 파일의 전체 경로 |
| `start` | 시작 유형 (`auto`, `delayed-auto`, `demand`) |
| `DisplayName` | 서비스 관리자에 표시될 이름 |

#### 서비스 설명 추가

```powershell
sc.exe description DynamicCrawler "웹 크롤링 및 콘텐츠 다운로드를 수행하는 백그라운드 서비스"
```

#### 서비스 시작 / 중지 / 상태 확인

```powershell
# 시작
sc.exe start DynamicCrawler

# 중지
sc.exe stop DynamicCrawler

# 상태 확인
sc.exe query DynamicCrawler
```

또는 PowerShell cmdlet을 사용할 수 있습니다:

```powershell
Start-Service DynamicCrawler
Stop-Service DynamicCrawler
Get-Service DynamicCrawler
```

#### 서비스 삭제

```powershell
sc.exe stop DynamicCrawler
sc.exe delete DynamicCrawler
```

> [!CAUTION]
> 서비스 삭제 전에 반드시 먼저 중지하세요. 실행 중인 서비스를 삭제하면 시스템 재시작 전까지 프로세스가 남아 있을 수 있습니다.

#### 서비스 로그 확인

서비스 모드에서도 Serilog 파일 로그는 동일하게 기록됩니다:

```
<게시 경로>\logs\crawler-YYYYMMDD.log
```

Windows 이벤트 로그에서도 서비스 시작/중지 이벤트를 확인할 수 있습니다:

```powershell
Get-EventLog -LogName Application -Source DynamicCrawler -Newest 20
```
