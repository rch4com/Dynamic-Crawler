using System.Security.Cryptography;

namespace DynamicCrawler.Downloader;

/// <summary>SHA256 스트림 해싱</summary>
public static class HashHelper
{
    /// <summary>스트림에서 SHA256 해시를 계산</summary>
    public static async Task<string> ComputeSha256Async(Stream stream, CancellationToken ct = default)
    {
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, ct).ConfigureAwait(false);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>파일에서 SHA256 해시를 계산</summary>
    public static async Task<string> ComputeSha256FromFileAsync(string filePath, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(filePath);
        return await ComputeSha256Async(stream, ct).ConfigureAwait(false);
    }
}
