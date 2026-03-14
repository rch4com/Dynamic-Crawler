using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace DynamicCrawler.Engine;

/// <summary>PuppeteerSharp 브라우저 프로세스 상태 헬스 체크</summary>
public sealed class BrowserHealthCheck(
    BrowserManager browserManager,
    ILogger<BrowserHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // BrowserManager를 통해 브라우저 인스턴스 획득 가능 여부 확인
            var browser = await browserManager.GetBrowserAsync(cancellationToken).ConfigureAwait(false);

            if (browser is null)
                return HealthCheckResult.Unhealthy("브라우저 인스턴스 없음");

            return HealthCheckResult.Healthy("브라우저 정상 동작 중");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "브라우저 헬스 체크 실패");
            return HealthCheckResult.Unhealthy("브라우저 연결 실패", ex);
        }
    }
}
