# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

```bash
# Build
dotnet build DynamicCrawler.sln

# Run tests
dotnet test tests/DynamicCrawler.Tests/DynamicCrawler.Tests.csproj

# Run single test
dotnet test tests/DynamicCrawler.Tests/DynamicCrawler.Tests.csproj --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Publish (Windows Service)
dotnet publish src/DynamicCrawler.Worker/DynamicCrawler.Worker.csproj -c Release
```

## Architecture

**Clean Architecture** with Producer-Consumer pipeline for web crawling:

- **Core** — Pure domain models (`Post`, `Media`, `Comment`, `Site`), interfaces, `Result<T>` pattern. Zero external dependencies.
- **Data.Supabase** — Repository implementations against PostgreSQL/Supabase. Lease-based record claiming (`ClaimNextAsync` with `LeaseSeconds`) prevents duplicate processing.
- **Engine** — PuppeteerSharp headless browser automation. `BrowserManager` handles lifecycle with page-count recycling.
- **Orchestrator** — `CrawlOrchestrator` (producer) discovers/crawls posts → extracts media → pushes to `Channel<DownloadTask>`. `DownloadOrchestrator` (consumer) claims and downloads. `RoundRobinScheduler` distributes work across sites. `CrawlerBackgroundService` runs both loops in parallel.
- **Downloader** — HTTP downloads with SHA256 dedup, Polly retry + circuit breaker.
- **Sites/Aagag** — `ISiteStrategy` plugin for aagag.com (AngleSharp HTML parsing). New sites implement `ISiteStrategy`.
- **Worker** — .NET BackgroundService host, runs as Windows Service or console.

**Pipeline flow:** Discovery → Crawl → Extract Media/Comments → Channel → Download → Dedup → Store

## Key Patterns

- **`Result<T>`** (`Core/Common/Result.cs`): All operations return `Result<T>` instead of throwing. Supports `Map`/`MapAsync` chaining.
- **`ConfigureAwait(false)`**: Required in all library projects (everything except Worker).
- **Bounded Channel**: `Channel<DownloadTask>` with capacity 200 connects producer/consumer. DB fallback after 5 empty channel cycles.
- **Lease/Lock**: `LeaseUntil` timestamp on claimed records; orphaned records rolled back on startup.
- **Central Package Management**: Versions in `Directory.Packages.props`, not individual `.csproj` files.

## Testing

xUnit + Moq + FluentAssertions. In-memory fakes (`InMemoryPostRepository`, etc.) in `tests/DynamicCrawler.Tests/Fakes/` replace real repositories for unit tests.

## Commit Messages

**Korean required.** Format:
```
<prefix>: <한국어 요약 (max 60 chars)>

<한국어 상세 설명 (bullet points)>
```
Prefixes: `feat`, `fix`, `refactor`, `chore`, `docs`, `style`, `test`, `perf`, `ci`, `build`
