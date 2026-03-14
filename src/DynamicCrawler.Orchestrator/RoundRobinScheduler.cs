using System.Collections.Immutable;

namespace DynamicCrawler.Orchestrator;

/// <summary>Round-robin 사이트 스케줄러 — Idle 회피 (thread-safe)</summary>
public sealed class RoundRobinScheduler
{
    private ImmutableArray<string> _siteKeys = [];
    private int _currentIndex;
    private readonly object _lock = new();
    private readonly Dictionary<string, DateTime> _lastDiscoveryAt = new(StringComparer.OrdinalIgnoreCase);

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

    /// <summary>SiteCount — lock으로 보호 (thread-safe)</summary>
    public int SiteCount { get { lock (_lock) { return _siteKeys.Length; } } }

    /// <summary>discovery cooldown이 지났으면 true를 반환하고 마지막 발견 시간을 업데이트</summary>
    public bool ShouldDiscover(string siteKey, TimeSpan cooldown)
    {
        lock (_lock)
        {
            if (_lastDiscoveryAt.TryGetValue(siteKey, out var last) &&
                DateTime.UtcNow - last < cooldown)
            {
                return false;
            }

            _lastDiscoveryAt[siteKey] = DateTime.UtcNow;
            return true;
        }
    }

    /// <summary>discovery 완료 시 마지막 발견 시간을 현재로 갱신</summary>
    public void MarkDiscovered(string siteKey)
    {
        lock (_lock)
        {
            _lastDiscoveryAt[siteKey] = DateTime.UtcNow;
        }
    }
}
