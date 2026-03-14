using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace DynamicCrawler.Engine;

/// <summary>PuppeteerSharp 브라우저 프로세스 상태 헬스 체크</summary>
public sealed class BrowserHealthCheck(
    BrowserManager browserManager,
    ILogger<BrowserHealthCheck> logger) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!browserManager.IsBrowserAlive)
                return Task.FromResult(HealthCheckResult.Unhealthy("브라우저 인스턴스 없음"));

            return Task.FromResult(HealthCheckResult.Healthy("브라우저 정상 동작 중"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "브라우저 헬스 체크 실패");
            return Task.FromResult(HealthCheckResult.Unhealthy("브라우저 연결 실패", ex));
        }
    }
}
