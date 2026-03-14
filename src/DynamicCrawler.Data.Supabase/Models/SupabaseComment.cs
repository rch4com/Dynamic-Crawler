using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace DynamicCrawler.Data.Supabase.Models;

[Table("comments")]
public sealed class SupabaseComment : BaseModel
{
    [PrimaryKey("id", false)] public long? Id { get; set; }
    [Column("post_id")] public long PostId { get; set; }
    [Column("author")] public string? Author { get; set; }
    [Column("content")] public string Content { get; set; } = "";
    [Column("commented_at")] public DateTime? CommentedAt { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; }
}
