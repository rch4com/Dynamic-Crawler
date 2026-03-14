using DynamicCrawler.Core.Common;
using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Core.Models;
using DynamicCrawler.Core.Results;
using Microsoft.Extensions.Logging;

namespace DynamicCrawler.Downloader;

/// <summary>미디어 다운로드 서비스. 재시도 정책, SHA256 계산, dedup을 담당합니다.</summary>
public sealed class MediaDownloadService(
    IHttpClientFactory httpClientFactory,
    IMediaRepository mediaRepo,
    PathResolver pathResolver,
    ContentTypeMapper contentTypeMapper,
    ILogger<MediaDownloadService> logger) : IMediaDownloader
{
    public async Task<Result<DownloadResult>> DownloadAsync(
        Media media, string siteKey, string postExternalId, CancellationToken ct = default)
    {
        string? tempPath = null;
        try
        {
            tempPath = pathResolver.ResolveTempPath(siteKey, postExternalId);

            using var client = httpClientFactory.CreateClient("MediaDownloader");
            using var response = await client.GetAsync(media.MediaUrl, HttpCompletionOption.ResponseHeadersRead, ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return Result<DownloadResult>.Failure(
                    $"HTTP {(int)response.StatusCode}: {media.MediaUrl}",
                    "HTTP_ERROR");
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? media.ContentType;
            var extension = contentTypeMapper.GetExtension(contentType, media.MediaUrl);

            // 스트림을 임시 파일로 저장한 뒤 해시를 계산합니다.
            await using (var sourceStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
            await using (var fileStream = File.Create(tempPath))
            {
                await sourceStream.CopyToAsync(fileStream, ct).ConfigureAwait(false);
            }

            var sha256 = await HashHelper.ComputeSha256FromFileAsync(tempPath, ct).ConfigureAwait(false);

            if (await mediaRepo.ExistsBySha256Async(sha256, ct).ConfigureAwait(false))
            {
                File.Delete(tempPath);
                tempPath = null;

                logger.LogInformation("중복 미디어를 건너뜁니다. SHA256={Sha256}, URL={Url}", sha256, media.MediaUrl);

                return Result<DownloadResult>.Success(
                    new DownloadResult(
                        sha256,
                        0,
                        contentType ?? "unknown",
                        null,
                        IsDuplicate: true));
            }

            var finalPath = pathResolver.Resolve(siteKey, postExternalId, sha256, extension);

            // SHA256이 동일하면 내용이 동일하므로 overwrite가 안전합니다 (TOCTOU 제거).
            File.Move(tempPath, finalPath, overwrite: true);
            tempPath = null;
            var byteSize = new FileInfo(finalPath).Length;

            return Result<DownloadResult>.Success(
                new DownloadResult(sha256, byteSize, contentType ?? "unknown", finalPath));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "미디어 다운로드 실패: {Url}", media.MediaUrl);
            return Result<DownloadResult>.Failure(ex.Message, "DOWNLOAD_ERROR");
        }
        finally
        {
            if (tempPath is not null)
            {
                try { File.Delete(tempPath); }
                catch (Exception ex) { logger.LogDebug(ex, "임시 파일 삭제 실패: {TempPath}", tempPath); }
            }
        }
    }
}
