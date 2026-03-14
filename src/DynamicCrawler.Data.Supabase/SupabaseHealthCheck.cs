using DynamicCrawler.Data.Supabase.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DynamicCrawler.Data.Supabase;

/// <summary>Supabase DB 연결 상태 헬스 체크</summary>
public sealed class SupabaseHealthCheck(
    global::Supabase.Client client,
    IOptions<SupabaseSettings> settings,
    ILogger<SupabaseHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // sites 테이블에 간단한 query로 연결 확인
            var response = await client.From<Models.SupabaseSite>()
                .Select("id")
                .Limit(1)
                .Get()
                .ConfigureAwait(false);

            return HealthCheckResult.Healthy(
                $"Supabase 연결 정상 ({settings.Value.Url})");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Supabase 헬스 체크 실패");
            return HealthCheckResult.Unhealthy("Supabase 연결 실패", ex);
        }
    }
}
