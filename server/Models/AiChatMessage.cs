namespace Server.Models;

public class AiChatMessage
{
    public int Id { get; set; }

    public Guid HighlightId { get; set; }
    public NoteHighlight Highlight { get; set; } = null!;

    public required string Question { get; set; }
    public required string Answer { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Set once the user clicks "Add to Document" for this message — lets the Process view
    // show document-changing messages distinctly from plain Q&A (PostService.
    // MarkAddedToDocumentAsync).
    public bool AddedToDocument { get; set; }
}
