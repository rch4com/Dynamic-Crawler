using DynamicCrawler.Core.Configuration;
using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Data.Supabase;
using DynamicCrawler.Downloader;
using DynamicCrawler.Engine;
using DynamicCrawler.Orchestrator;
using DynamicCrawler.Sites.Aagag;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(config =>
    config.ReadFrom.Configuration(builder.Configuration));

builder.Services.AddWindowsService();

builder.Services.Configure<CrawlerSettings>(
    builder.Configuration.GetSection(CrawlerSettings.SectionName));

builder.Services.AddSupabasePersistence(builder.Configuration);

builder.Services.AddSingleton<BrowserManager>();
builder.Services.AddScoped<ICrawlEngine, PuppeteerCrawlEngine>();

builder.Services.AddDownloader(builder.Configuration);
builder.Services.AddAagagSiteStrategy();

builder.Services.AddSingleton<RoundRobinScheduler>();
builder.Services.AddSingleton<CrawlPipeline>();
builder.Services.AddScoped<CrawlOrchestrator>();
builder.Services.AddScoped<DownloadOrchestrator>();
builder.Services.AddHostedService<CrawlerBackgroundService>();

builder.Services.AddHealthChecks()
    .AddCheck<BrowserHealthCheck>("browser", tags: ["engine"]);

var host = builder.Build();

host.Services.GetRequiredService<IHostApplicationLifetime>()
    .ApplicationStopping.Register(() =>
    {
        host.Services.GetRequiredService<BrowserManager>()
            .DisposeAsync().AsTask().GetAwaiter().GetResult();
    });

host.Run();
