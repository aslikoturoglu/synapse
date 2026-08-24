namespace Client.Services;

public class PreliminaryScanDto
{
    public List<string> SeedKeywords { get; set; } = [];
    public string Language { get; set; } = "";
    public string Domain { get; set; } = "";
    public string EstimatedStructure { get; set; } = "";
}

public class OrchestratorStartResponse
{
    public string ThreadId { get; set; } = "";
    public List<string> FileIds { get; set; } = [];
    public PreliminaryScanDto PreliminaryScan { get; set; } = new();
    public string DocumentKnowledgeBase { get; set; } = "";
    public GraphDto DraftGraph { get; set; } = new();
}

public class GenerateNoteRequest
{
    public required string ThreadId { get; set; }
    public required string DocumentKnowledgeBase { get; set; }
    public required string FormatPreference { get; set; }
    public string UserNotes { get; set; } = "";
    public List<string> FinalKeywords { get; set; } = [];
    public PreliminaryScanDto PreliminaryClassification { get; set; } = new();
}

public class GenerateNoteResponse
{
    public List<NotePageDto> Pages { get; set; } = [];
    public string SynthesizedMarkdown { get; set; } = "";
    public string SuggestedTitle { get; set; } = "";
}

public class GenerateFinalBrainMapRequest
{
    public List<string> DraftKeywords { get; set; } = [];
    public required string SynthesizedMarkdown { get; set; }
    public required string DocumentKnowledgeBase { get; set; }
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
