using DynamicCrawler.Core.Interfaces;

namespace DynamicCrawler.Orchestrator;

/// <summary>Round-robin 사이트 스케줄러 — Idle 회피</summary>
public sealed class RoundRobinScheduler
{
    private readonly List<string> _siteKeys = [];
    private int _currentIndex;

    public void SetSiteKeys(IEnumerable<string> keys)
    {
        _siteKeys.Clear();
        _siteKeys.AddRange(keys);
        _currentIndex = 0;
    }

    /// <summary>다음 사이트 키를 반환 (라운드 로빈)</summary>
    public string? Next()
    {
        if (_siteKeys.Count == 0) return null;

        var key = _siteKeys[_currentIndex % _siteKeys.Count];
        _currentIndex = (_currentIndex + 1) % _siteKeys.Count;
        return key;
    }

    public int SiteCount => _siteKeys.Count;
}
