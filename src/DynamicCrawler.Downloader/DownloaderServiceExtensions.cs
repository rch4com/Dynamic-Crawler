using DynamicCrawler.Core.Configuration;
using DynamicCrawler.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace DynamicCrawler.Downloader;

/// <summary>Downloader 모듈 DI 등록</summary>
public static class DownloaderServiceExtensions
{
    public static IServiceCollection AddDownloader(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ContentTypeMapper>();
        services.AddSingleton<PathResolver>();
        services.AddScoped<IMediaDownloader, MediaDownloadService>();

        services.AddHttpClient("MediaDownloader")
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}
