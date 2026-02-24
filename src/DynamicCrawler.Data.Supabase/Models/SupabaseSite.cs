using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace DynamicCrawler.Data.Supabase.Models;

[Table("sites")]
public sealed class SupabaseSite : BaseModel
{
    [PrimaryKey("id")]                   public int Id { get; set; }
    [Column("site_key")]                 public string SiteKey { get; set; } = "";
    [Column("base_url")]                 public new string BaseUrl { get; set; } = "";
    [Column("max_concurrent_collects")]  public int MaxConcurrentCollects { get; set; } = 2;
    [Column("max_concurrent_downloads")] public int MaxConcurrentDownloads { get; set; } = 4;
    [Column("is_active")]                public bool IsActive { get; set; } = true;
    [Column("created_at")]               public DateTime CreatedAt { get; set; }
}
