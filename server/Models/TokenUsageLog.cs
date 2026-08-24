namespace Server.Models;

// One row per authenticated request that made at least one AI call (see
// TokenUsageActionFilter) — not one row per individual AskAsync call, since a single request
// can make several (e.g. NoteCreationAiService.StartAsync's preliminary scan + RAG + draft
// map). Mirrors EmailLog's role as a plain, un-related-to-anything-else audit table.
public class TokenUsageLog
{
    public int Id { get; set; }
    public required int UserId { get; set; }
    public required int Tokens { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
