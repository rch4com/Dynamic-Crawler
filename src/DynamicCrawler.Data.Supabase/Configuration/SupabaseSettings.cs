namespace DynamicCrawler.Data.Supabase.Configuration;

/// <summary>Supabase 연결 설정 (IOptions 패턴)</summary>
public sealed class SupabaseSettings
{
    public const string SectionName = "Supabase";

    /// <summary>Supabase 프로젝트 URL</summary>
    public string Url { get; set; } = "";

    /// <summary>Supabase API Key (publishable)</summary>
    public string ApiKey { get; set; } = "";
}
