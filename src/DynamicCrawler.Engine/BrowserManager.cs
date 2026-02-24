using DynamicCrawler.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace DynamicCrawler.Engine;

/// <summary>Chromium 브라우저 라이프사이클 관리 — N건 재기동 + 유휴 타임아웃</summary>
public sealed class BrowserManager : IAsyncDisposable
{
    private readonly CrawlerSettings _settings;
    private readonly ILogger<BrowserManager> _logger;
    private IBrowser? _browser;
    private int _processedCount;
    private DateTime _lastUsed = DateTime.UtcNow;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public BrowserManager(IOptions<CrawlerSettings> settings, ILogger<BrowserManager> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<IBrowser> GetBrowserAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var shouldRecycle = _browser is null
                || _processedCount >= _settings.BrowserRecycleCount
                || (DateTime.UtcNow - _lastUsed).TotalMinutes > _settings.IdleTimeoutMinutes;

            if (shouldRecycle)
            {
                await DisposeBrowserAsync().ConfigureAwait(false);
                await EnsureBrowserInstalledAsync().ConfigureAwait(false);

                _browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = ["--no-sandbox", "--disable-gpu", "--disable-dev-shm-usage"]
                }).ConfigureAwait(false);

                _processedCount = 0;
                _logger.LogInformation("브라우저 인스턴스 (재)생성 완료");
            }

            _lastUsed = DateTime.UtcNow;
            return _browser!;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void IncrementProcessedCount() => Interlocked.Increment(ref _processedCount);

    private async Task DisposeBrowserAsync()
    {
        if (_browser is not null)
        {
            try { await _browser.CloseAsync().ConfigureAwait(false); }
            catch { /* 무시 */ }
            _browser = null;
        }
    }

    private static async Task EnsureBrowserInstalledAsync()
    {
        var fetcher = new BrowserFetcher();
        await fetcher.DownloadAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeBrowserAsync().ConfigureAwait(false);
        _lock.Dispose();
    }
}
