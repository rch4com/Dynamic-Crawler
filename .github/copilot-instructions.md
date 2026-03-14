# Dynamic-Crawler Copilot Instructions

## Build and test commands

```powershell
# Build the full solution
dotnet build DynamicCrawler.sln

# Run the test project
dotnet test tests\DynamicCrawler.Tests\DynamicCrawler.Tests.csproj

# Run a single test by fully qualified name
dotnet test tests\DynamicCrawler.Tests\DynamicCrawler.Tests.csproj --filter "FullyQualifiedName~DynamicCrawler.Tests.Core.ResultTests.Success_ShouldCreateSuccessResult"

# Publish the worker as a Windows Service build
dotnet publish src\DynamicCrawler.Worker\DynamicCrawler.Worker.csproj -c Release
```

## High-level architecture

Dynamic-Crawler is a layered .NET crawler built around a producer-consumer pipeline.

- `DynamicCrawler.Worker` is the host. `Program.cs` wires DI, Serilog, Windows Service hosting, health checks, the site strategy, the crawler engine, and the background service.
- `DynamicCrawler.Orchestrator` is the pipeline coordinator. `CrawlerBackgroundService` starts two parallel loops: `CrawlOrchestrator` produces discovered media into `CrawlPipeline`, and `DownloadOrchestrator` consumes channel items before falling back to claiming pending media from the database.
- `DynamicCrawler.Engine` handles browser automation with PuppeteerSharp. `BrowserManager` owns browser lifecycle, idle timeout handling, and browser recycling after a configured number of processed pages.
- `DynamicCrawler.Sites.Aagag` is the current site plugin. It implements `ISiteStrategy` to build list URLs and parse posts, media, and comments from HTML.
- `DynamicCrawler.Data.Supabase` implements persistence against Supabase/PostgreSQL for posts, media, comments, and sites.
- `DynamicCrawler.Downloader` downloads media over `HttpClient`, computes SHA256 hashes, and skips duplicate files.
- `DynamicCrawler.Core` holds domain models, shared enums, repository abstractions, crawl contracts, and the `Result<T>` type used across the pipeline.

The main runtime flow is:

1. `CrawlerBackgroundService` rolls back orphaned claimed records on startup.
2. `CrawlOrchestrator` discovers new posts from active sites, claims the next post per site, crawls the post detail page, stores comments/media, and pushes download work into the bounded channel.
3. `DownloadOrchestrator` drains the channel with configured concurrency, then periodically falls back to database claiming so queued rows do not starve.
4. Download results update media status, retry metadata, local path, and SHA256 state in persistence.

## Key conventions

- Use `Result<T>` for business outcomes instead of throwing for expected failures. `DynamicCrawler.Core\Common\Result.cs` also provides `Map` and `MapAsync` helpers that are already used as the standard success/failure container.
- In library projects, every `await` should use `ConfigureAwait(false)`. This is a repo policy called out in source and existing assistant guidance; the `Worker` host is the exception.
- Record claiming is lease-based. Posts and media are claimed with `LeaseUntil`, retried with exponential backoff, and orphaned claims are rolled back at service startup.
- Site-specific crawling logic belongs behind `ISiteStrategy`. New sites should follow the Aagag pattern: implement parsing in a site project and register the strategy in `DynamicCrawler.Worker`.
- The crawler/download handoff uses a bounded `Channel` (`CrawlPipeline`) instead of direct synchronous processing. Preserve that separation when changing orchestration behavior.
- Package versions are managed centrally in `Directory.Packages.props`; do not add package versions directly inside individual project files unless the repository pattern changes.
- Solution-wide MSBuild settings come from `Directory.Build.props`: `net10.0`, nullable enabled, implicit usings enabled, and warnings treated as errors.
- Tests use xUnit, FluentAssertions, and Moq. The repository also relies on in-memory fake repositories under `tests\DynamicCrawler.Tests\Fakes\` for orchestration and domain tests that should not hit Supabase.
- If you generate a commit message, follow the existing repo-wide AI rules from `AGENTS.md`, `CLAUDE.md`, `.cursorrules`, and `.clinerules`: allowed prefixes are `feat`, `fix`, `refactor`, `chore`, `docs`, `style`, `test`, `perf`, `ci`, and `build`, and the summary/body should be written in Korean.
