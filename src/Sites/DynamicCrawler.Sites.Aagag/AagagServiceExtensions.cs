using DynamicCrawler.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DynamicCrawler.Sites.Aagag;

/// <summary>Aagag 사이트 전략 DI 등록</summary>
public static class AagagServiceExtensions
{
    public static IServiceCollection AddAagagSiteStrategy(this IServiceCollection services)
    {
        services.AddSingleton<ISiteStrategy, AagagSiteStrategy>();
        return services;
    }
}
