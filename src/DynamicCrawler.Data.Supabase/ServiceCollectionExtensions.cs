using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Data.Supabase.Configuration;
using DynamicCrawler.Data.Supabase.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DynamicCrawler.Data.Supabase;

/// <summary>Supabase Persistence DI 등록 — 교체 지점</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Supabase 기반 Persistence를 DI에 등록합니다.
    /// 다른 구현으로 교체 시 이 호출만 변경하면 됩니다.
    /// </summary>
    public static IServiceCollection AddSupabasePersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SupabaseSettings>(configuration.GetSection(SupabaseSettings.SectionName));

        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<SupabaseSettings>>().Value;
            return new global::Supabase.Client(
                settings.Url,
                settings.ApiKey,
                new global::Supabase.SupabaseOptions { AutoRefreshToken = false });
        });

        services.AddScoped<IPostRepository, SupabasePostRepository>();
        services.AddScoped<IMediaRepository, SupabaseMediaRepository>();
        services.AddScoped<ISiteRepository, SupabaseSiteRepository>();

        return services;
    }
}
