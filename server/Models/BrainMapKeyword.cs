namespace Server.Models;

public enum KeywordStatus { AiKept, AiDeleted, UserAdded }

public class BrainMapKeyword
{
    public int Id { get; set; }

    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public required string Text { get; set; }

    // null means the keyword was added by hand — no real frequency to report.
    public int? Count { get; set; }

    public KeywordStatus Status { get; set; } = KeywordStatus.AiKept;
}
