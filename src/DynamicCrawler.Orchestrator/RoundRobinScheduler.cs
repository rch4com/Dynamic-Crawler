using System.Collections.Immutable;

namespace DynamicCrawler.Orchestrator;

/// <summary>Round-robin 사이트 스케줄러 — Idle 회피 (thread-safe)</summary>
public sealed class RoundRobinScheduler
{
    private ImmutableArray<string> _siteKeys = [];
    private int _currentIndex;
    private readonly object _lock = new();

    public void SetSiteKeys(IEnumerable<string> keys)
    {
        lock (_lock)
        {
            _siteKeys = [.. keys];
            _currentIndex = 0;
        }
    }

    /// <summary>다음 사이트 키를 반환 (라운드 로빈)</summary>
    public string? Next()
    {
        lock (_lock)
        {
            var keys = _siteKeys;
            if (keys.Length == 0) return null;

            var key = keys[_currentIndex % keys.Length];
            _currentIndex = (_currentIndex + 1) % keys.Length;
            return key;
        }
    }

    public int SiteCount => _siteKeys.Length;
}
