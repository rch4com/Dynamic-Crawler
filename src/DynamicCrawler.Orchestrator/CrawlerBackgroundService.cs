using DynamicCrawler.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DynamicCrawler.Orchestrator;

/// <summary>크롤러 BackgroundService — Scoped 서비스 패턴 적용</summary>
public sealed class CrawlerBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<CrawlerBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Dynamic-Crawler 서비스 시작");

        // 시작 시 orphaned 상태 롤백
        using (var scope = serviceProvider.CreateScope())
        {
            var postRepo = scope.ServiceProvider.GetRequiredService<IPostRepository>();
            var mediaRepo = scope.ServiceProvider.GetRequiredService<IMediaRepository>();

            var postRolled = await postRepo.RollbackOrphanedAsync(stoppingToken).ConfigureAwait(false);
            var mediaRolled = await mediaRepo.RollbackOrphanedAsync(stoppingToken).ConfigureAwait(false);

            logger.LogInformation("Orphaned 롤백 완료: Posts={PostCount}, Media={MediaCount}", postRolled, mediaRolled);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();

                // 크롤링 사이클
                var crawlOrch = scope.ServiceProvider.GetRequiredService<CrawlOrchestrator>();
                await crawlOrch.RunCycleAsync(stoppingToken).ConfigureAwait(false);

                // 다운로드 사이클
                var dlOrch = scope.ServiceProvider.GetRequiredService<DownloadOrchestrator>();
                await dlOrch.RunCycleAsync(stoppingToken).ConfigureAwait(false);

                // 다음 사이클까지 대기
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "사이클 실행 중 예외 발생. 30초 후 재시도...");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);
            }
        }

        logger.LogInformation("Dynamic-Crawler 서비스 종료");
    }
}
