using System.Threading.Channels;

namespace DynamicCrawler.Orchestrator;

/// <summary>크롤링→다운로드 비동기 파이프라인 (Channel&lt;DownloadTask&gt; 기반)</summary>
public sealed class CrawlPipeline
{
    private readonly Channel<DownloadTask> _downloadChannel =
        Channel.CreateBounded<DownloadTask>(new BoundedChannelOptions(200)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = false
        });

    /// <summary>크롤러(Producer)가 발견한 다운로드 작업을 채널에 씁니다</summary>
    public ChannelWriter<DownloadTask> Writer => _downloadChannel.Writer;

    /// <summary>다운로더(Consumer)가 다운로드 작업을 읽어갑니다</summary>
    public ChannelReader<DownloadTask> Reader => _downloadChannel.Reader;

    /// <summary>더 이상 쓸 작업이 없음을 알립니다</summary>
    public void Complete() => _downloadChannel.Writer.TryComplete();
}
