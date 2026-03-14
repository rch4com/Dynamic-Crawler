using DynamicCrawler.Core.Models;
using System.Threading.Channels;

namespace DynamicCrawler.Orchestrator;

/// <summary>크롤링→다운로드 비동기 파이프라인 (Channel&lt;Media&gt; 기반)</summary>
public sealed class CrawlPipeline
{
    private readonly Channel<Media> _downloadChannel =
        Channel.CreateBounded<Media>(new BoundedChannelOptions(200)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = false
        });

    /// <summary>크롤러(Producer)가 발견한 미디어를 채널에 씁니다</summary>
    public ChannelWriter<Media> Writer => _downloadChannel.Writer;

    /// <summary>다운로더(Consumer)가 미디어를 읽어갑니다</summary>
    public ChannelReader<Media> Reader => _downloadChannel.Reader;

    /// <summary>더 이상 쓸 미디어가 없음을 알립니다</summary>
    public void Complete() => _downloadChannel.Writer.TryComplete();
}
