using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace DynamicCrawler.Data.Supabase.Models;

[Table("posts")]
public sealed class SupabasePost : BaseModel
{
    [PrimaryKey("id", false)]  public long Id { get; set; }
    [Column("site_key")]       public string SiteKey { get; set; } = "";
    [Column("external_id")]    public string ExternalId { get; set; } = "";
    [Column("url")]            public string Url { get; set; } = "";
    [Column("title")]          public string? Title { get; set; }
    [Column("status")]         public string Status { get; set; } = "Discovered";
    [Column("retry_count")]    public int RetryCount { get; set; }
    [Column("next_retry_at")]  public DateTime? NextRetryAt { get; set; }
    [Column("lease_until")]    public DateTime? LeaseUntil { get; set; }
    [Column("created_at")]     public DateTime CreatedAt { get; set; }
    [Column("updated_at")]     public DateTime? UpdatedAt { get; set; }
}
