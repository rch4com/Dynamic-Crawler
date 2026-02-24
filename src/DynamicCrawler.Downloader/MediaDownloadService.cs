using DynamicCrawler.Core.Common;
using DynamicCrawler.Core.Interfaces;
using DynamicCrawler.Core.Models;
using DynamicCrawler.Core.Results;
using Microsoft.Extensions.Logging;

namespace DynamicCrawler.Downloader;

/// <summary>미디어 다운로드 서비스 — IHttpClientFactory + SHA256 해싱 + dedup</summary>
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
        try
        {
            var tempPath = pathResolver.ResolveTempPath(siteKey, postExternalId);

            using var client = httpClientFactory.CreateClient("MediaDownloader");
            using var response = await client.GetAsync(media.MediaUrl, HttpCompletionOption.ResponseHeadersRead, ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return Result<DownloadResult>.Failure(
                    $"HTTP {(int)response.StatusCode}: {media.MediaUrl}", "HTTP_ERROR");

            var contentType = response.Content.Headers.ContentType?.MediaType ?? media.ContentType;

            // 스트림 → 임시 파일 저장
            await using (var sourceStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
            await using (var fileStream = File.Create(tempPath))
            {
                await sourceStream.CopyToAsync(fileStream, ct).ConfigureAwait(false);
            }

            // SHA256 해싱
            var sha256 = await HashHelper.ComputeSha256FromFileAsync(tempPath, ct).ConfigureAwait(false);

            // dedup 확인
            if (await mediaRepo.ExistsBySha256Async(sha256, ct).ConfigureAwait(false))
            {
                File.Delete(tempPath);
                logger.LogDebug("중복 파일 스킵: SHA256={Sha256}, URL={Url}", sha256, media.MediaUrl);
            }

            // 최종 경로로 이동
            var extension = contentTypeMapper.GetExtension(contentType, media.MediaUrl);
            var finalPath = pathResolver.Resolve(siteKey, postExternalId, sha256, extension);

            if (!File.Exists(finalPath))
                File.Move(tempPath, finalPath, overwrite: false);
            else
                File.Delete(tempPath);

            var byteSize = new FileInfo(finalPath).Length;

            return Result<DownloadResult>.Success(new DownloadResult(sha256, byteSize, contentType ?? "unknown", finalPath));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "미디어 다운로드 실패: {Url}", media.MediaUrl);
            return Result<DownloadResult>.Failure(ex.Message, "DOWNLOAD_ERROR");
        }
    }
}
