using DynamicCrawler.Core.Configuration;
using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Data.Supabase;
using DynamicCrawler.Downloader;
using DynamicCrawler.Engine;
using DynamicCrawler.Orchestrator;
using DynamicCrawler.Sites.Aagag;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Serilog 구조적 로깅
builder.Services.AddSerilog(config => config
    .ReadFrom.Configuration(builder.Configuration));

// Windows Service 지원
builder.Services.AddWindowsService();

// IOptions<CrawlerSettings>
builder.Services.Configure<CrawlerSettings>(
    builder.Configuration.GetSection(CrawlerSettings.SectionName));

// Persistence — Supabase (교체 지점)
builder.Services.AddSupabasePersistence(builder.Configuration);

// Engine
builder.Services.AddSingleton<BrowserManager>();
builder.Services.AddScoped<ICrawlEngine, PuppeteerCrawlEngine>();

// Downloader (IHttpClientFactory + Polly)
builder.Services.AddDownloader(builder.Configuration);

// Sites
builder.Services.AddAagagSiteStrategy();

// Orchestrator
builder.Services.AddSingleton<RoundRobinScheduler>();
builder.Services.AddScoped<CrawlOrchestrator>();
builder.Services.AddScoped<DownloadOrchestrator>();
builder.Services.AddHostedService<CrawlerBackgroundService>();

var host = builder.Build();
host.Run();
