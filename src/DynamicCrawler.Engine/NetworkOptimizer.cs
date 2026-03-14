using PuppeteerSharp;

namespace DynamicCrawler.Engine;

/// <summary>네트워크 최적화 — CSS/폰트/이미지/미디어 차단, XHR/Fetch 허용</summary>
public static class NetworkOptimizer
{
    private static readonly HashSet<ResourceType> BlockedTypes =
    [
        ResourceType.StyleSheet,
        ResourceType.Font,
        ResourceType.Image,
        ResourceType.Media
    ];

    /// <summary>페이지에 리소스 차단 인터셉터 적용</summary>
    public static async Task ApplyAsync(IPage page)
    {
        await page.SetRequestInterceptionAsync(true).ConfigureAwait(false);

        page.Request += async (_, e) =>
        {
            try
            {
                if (BlockedTypes.Contains(e.Request.ResourceType))
                    await e.Request.AbortAsync().ConfigureAwait(false);
                else
                    await e.Request.ContinueAsync().ConfigureAwait(false);
            }
            catch
            {
                // 페이지 닫힘 등으로 인한 요청 처리 실패는 무시
            }
        };
    }
}
