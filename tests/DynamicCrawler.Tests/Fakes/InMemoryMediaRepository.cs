using DynamicCrawler.Core.Common;
using DynamicCrawler.Core.Enums;
using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Core.Models;

namespace DynamicCrawler.Tests.Fakes;

/// <summary>InMemory Media Repository — 테스트용</summary>
public sealed class InMemoryMediaRepository : IMediaRepository
{
    private readonly List<Media> _media = [];
    private long _nextId = 1;

    public IReadOnlyList<Media> MediaList => _media;

    public Task<Result<Media>> ClaimNextAsync(string siteKey, int leaseSeconds, CancellationToken ct = default)
    {
        // siteKey 기반 필터링은 간소화 (InMemory에서는 모든 미디어에서 찾음)
        var media = _media.FirstOrDefault(m =>
            m.Status == MediaStatus.PendingDownload &&
            (m.LeaseUntil is null || m.LeaseUntil < DateTime.UtcNow));

        if (media is null)
            return Task.FromResult(Result<Media>.Failure("다운로드할 미디어가 없습니다", "EMPTY_QUEUE"));

        media.Status = MediaStatus.Downloading;
        media.LeaseUntil = DateTime.UtcNow.AddSeconds(leaseSeconds);

        return Task.FromResult(Result<Media>.Success(media));
    }

    public Task UpdateAsync(Media media, CancellationToken ct = default)
    {
        var existing = _media.FirstOrDefault(m => m.Id == media.Id);
        if (existing is not null)
        {
            existing.Status = media.Status;
            existing.Sha256 = media.Sha256;
            existing.ByteSize = media.ByteSize;
            existing.ContentType = media.ContentType;
            existing.LocalPath = media.LocalPath;
            existing.RetryCount = media.RetryCount;
        }
        return Task.CompletedTask;
    }

    public Task BulkInsertAsync(IEnumerable<Media> mediaList, CancellationToken ct = default)
    {
        foreach (var m in mediaList)
        {
            var newMedia = new Media
            {
                Id = _nextId++,
                PostId = m.PostId,
                MediaUrl = m.MediaUrl,
                ContentType = m.ContentType,
                Status = m.Status
            };
            _media.Add(newMedia);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsBySha256Async(string sha256, CancellationToken ct = default)
    {
        return Task.FromResult(_media.Any(m => m.Sha256 == sha256));
    }

    public Task<int> RollbackOrphanedAsync(CancellationToken ct = default)
    {
        var count = 0;
        foreach (var m in _media.Where(m => m.Status == MediaStatus.Downloading && m.LeaseUntil < DateTime.UtcNow))
        {
            m.Status = MediaStatus.PendingDownload;
            m.LeaseUntil = null;
            count++;
        }
        return Task.FromResult(count);
    }

    /// <summary>테스트용 시드</summary>
    public void Seed(Media media)
    {
        var seeded = new Media
        {
            Id = _nextId++,
            PostId = media.PostId,
            MediaUrl = media.MediaUrl,
            ContentType = media.ContentType,
            Sha256 = media.Sha256,
            ByteSize = media.ByteSize,
            LocalPath = media.LocalPath,
            Status = media.Status,
            RetryCount = media.RetryCount,
            NextRetryAt = media.NextRetryAt,
            LeaseUntil = media.LeaseUntil,
            CreatedAt = media.CreatedAt
        };
        _media.Add(seeded);
    }
}
