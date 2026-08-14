namespace Server.Dtos;

public class OrchestratorStartRequest
{
    public required List<IFormFile> Files { get; set; }
    public required string Description { get; set; }
}

public class OrchestratorStartResponse
{
    public required string ThreadId { get; set; }
    public required string Summary { get; set; }
    public List<string> FileIds { get; set; } = [];
}

public class OrchestratorSelectRequest
{
    public required string ThreadId { get; set; }

    // Built client-side from the fixed 5-item menu's checkboxes, e.g. "1, 3, 5" or "All" —
    // matches the exact reply format orchestrator-chat-agent-synapse's own instructions expect.
    public required string Selection { get; set; }
}

public class NoteAiDirectiveDto
{
    public List<string> SeedKeywords { get; set; } = [];
    public List<string> FocusAreas { get; set; } = [];
    public string SynthesisMode { get; set; } = "";
    public string UserCustomInstructions { get; set; } = "";
}

public class GenerateNoteRequest
{
    public required string ThreadId { get; set; }
    public List<string> FileIds { get; set; } = [];
    public required NoteAiDirectiveDto Directive { get; set; }
    public List<BrainMapKeywordDto> Keywords { get; set; } = [];
}

public class GenerateNoteResponse
{
    public List<NotePageDto> Pages { get; set; } = [];
    public required string DocumentKnowledgeBase { get; set; }
}

public class GraphNodeDto
{
    public required string Id { get; set; }
    public required string Label { get; set; }
    public required string Type { get; set; } // "root" | "topic" | "concept"
    public int? Frequency { get; set; }
}

public class GraphEdgeDto
{
    public required string Source { get; set; }
    public required string Target { get; set; }
    public required string Label { get; set; }
}

public class GraphDto
{
    public List<GraphNodeDto> Nodes { get; set; } = [];
    public List<GraphEdgeDto> Edges { get; set; } = [];
}

// For the general (not tied to any highlight) chatbox — ThreadId lets the client continue the
// same conversation across follow-ups within a session; it's never persisted server-side
// (matches the general chatbox's existing un-persisted, client-only-list behavior).
public class AskAiRequest
{
    public required string Question { get; set; }
    public string? ThreadId { get; set; }
}

public class AskAiResponse
{
    public required string Answer { get; set; }
    public required string ThreadId { get; set; }
}
