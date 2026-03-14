using DynamicCrawler.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DynamicCrawler.Orchestrator;

/// <summary>크롤러 BackgroundService — Scoped 서비스 패턴 적용</summary>
public sealed class CrawlerBackgroundService(
    IServiceProvider serviceProvider,
    CrawlPipeline pipeline,
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

        // Producer(크롤링)와 Consumer(다운로드)를 별도 스코프에서 병렬 실행
        var crawlTask = RunProducerLoopAsync(stoppingToken);
        var downloadTask = RunConsumerLoopAsync(stoppingToken);

        await Task.WhenAll(crawlTask, downloadTask).ConfigureAwait(false);
    }

    /// <summary>Producer 루프 — 크롤링 사이클을 반복 실행</summary>
    private async Task RunProducerLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var crawlOrch = scope.ServiceProvider.GetRequiredService<CrawlOrchestrator>();
                await crawlOrch.RunCycleAsync(ct).ConfigureAwait(false);

                await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "크롤링 사이클 예외 발생. 30초 후 재시도...");
                await Task.Delay(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);
            }
        }

        pipeline.Complete();
    }

    /// <summary>Consumer 루프 — 다운로드 사이클을 반복 실행</summary>
    private async Task RunConsumerLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var dlOrch = scope.ServiceProvider.GetRequiredService<DownloadOrchestrator>();
                await dlOrch.RunCycleAsync(ct).ConfigureAwait(false);

                await Task.Delay(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "다운로드 사이클 예외 발생. 30초 후 재시도...");
                await Task.Delay(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);
            }
        }

        logger.LogInformation("Dynamic-Crawler 서비스 종료");
    }
}
