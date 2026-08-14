namespace Server.Models;

// A piece of text a specific viewer selected in a note to ask the AI about. Scoped to the
// viewer who made it (UserId) — everyone who reads a shared post highlights their own copy
// of it, they don't see each other's highlights.
public class NoteHighlight
{
    public Guid Id { get; set; }

    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public int UserId { get; set; }

    public int PageNumber { get; set; }
    public required string SelectedText { get; set; }

    public List<AiChatMessage> Messages { get; } = [];

    // The Responses API's continuation id (NoteChatAiService.AskAsync's ResponseId) from the
    // most recent turn of this highlight's Q&A with interactive-chat-agent-synapse — passed
    // back in as previousResponseId on the next follow-up so the agent sees the real
    // conversation history instead of a fresh, context-less prompt each time. Named
    // "AgentThreadId" from an earlier Threads/Runs-based design; kept as-is to avoid an
    // unnecessary migration, but it's a response id, not a literal thread id.
    public string? AgentThreadId { get; set; }
}
