using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace DynamicCrawler.Data.Supabase.Models;

[Table("media")]
public sealed class SupabaseMedia : BaseModel
{
    [PrimaryKey("id", false)]  public long? Id { get; set; }
    [Column("post_id")]        public long PostId { get; set; }
    [Column("media_url")]      public string MediaUrl { get; set; } = "";
    [Column("content_type")]   public string? ContentType { get; set; }
    [Column("sha256")]         public string? Sha256 { get; set; }
    [Column("byte_size")]      public long? ByteSize { get; set; }
    [Column("local_path")]     public string? LocalPath { get; set; }
    [Column("status")]         public string Status { get; set; } = "PendingDownload";
    [Column("retry_count")]    public int RetryCount { get; set; }
    [Column("next_retry_at")]  public DateTime? NextRetryAt { get; set; }
    [Column("lease_until")]    public DateTime? LeaseUntil { get; set; }
    [Column("created_at")]     public DateTime CreatedAt { get; set; }
}
