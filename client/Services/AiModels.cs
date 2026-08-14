namespace Client.Services;

public class OrchestratorStartResponse
{
    public string ThreadId { get; set; } = "";
    public string Summary { get; set; } = "";
    public List<string> FileIds { get; set; } = [];
}

public class OrchestratorSelectRequest
{
    public required string ThreadId { get; set; }
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
    public string DocumentKnowledgeBase { get; set; } = "";
}

public class AskAiRequest
{
    public required string Question { get; set; }
    public string? ThreadId { get; set; }
}

public class AskAiResponse
{
    public string Answer { get; set; } = "";
    public string ThreadId { get; set; } = "";
}
