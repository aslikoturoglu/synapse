namespace Server.Dtos;

public class AdminChatRequest
{
    // Null on the first question of a conversation; the prior call's ResponseId on every
    // follow-up, so interactive-chat-agent-synapse sees the real exchange (see AskAsync).
    public string? ThreadId { get; set; }
    public required string Question { get; set; }
}

public class AdminChatResponse
{
    public required string Answer { get; set; }
    public required string ThreadId { get; set; }
}
