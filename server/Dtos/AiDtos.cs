namespace Server.Dtos;

public class OrchestratorStartRequest
{
    public required List<IFormFile> Files { get; set; }

    // Genuinely optional — the wizard has no dedicated description step anymore (see
    // NoteCreationAiService.DescribeSuffix), so the client always sends this empty. Must be
    // nullable, not just non-required: ASP.NET Core implicitly treats any non-nullable
    // reference-type property as [Required] for [FromForm] validation regardless of the C#
    // `required` keyword, and rejects an empty string the same as a missing one.
    public string? Description { get; set; }
}

// orchestrator-chat-agent-synapse's own quick read of the uploaded documents — candidate
// keywords plus a light classification, produced immediately on upload and forwarded
// unchanged to topic-synthesizer-agent-synapse later as preliminary_classification.
public class PreliminaryScanDto
{
    public List<string> SeedKeywords { get; set; } = [];
    public string Language { get; set; } = "";
    public string Domain { get; set; } = "";
    public string EstimatedStructure { get; set; } = "";
}

// Bundles everything that starts the moment files are uploaded: the Orchestrator's
// preliminary scan, document-rag-agent-synapse's full analysis, and brain-map-agent-synapse's
// draft (keyword-only, no edges) map — all fired immediately, none of it waits on user input.
public class OrchestratorStartResponse
{
    public required string ThreadId { get; set; }
    public List<string> FileIds { get; set; } = [];
    public PreliminaryScanDto PreliminaryScan { get; set; } = new();
    public required string DocumentKnowledgeBase { get; set; }
    public required GraphDto DraftGraph { get; set; }
}

public class GenerateNoteRequest
{
    public required string ThreadId { get; set; }
    public required string DocumentKnowledgeBase { get; set; }

    // Raw free text ("bulleted please", "give me a narrative walkthrough", ...) — the
    // Orchestrator agent (not this code) is responsible for mapping it into
    // bulleted_breakdown | narrative | summary, per its own strict "no technical jargon to
    // the user" constraint.
    public required string FormatPreference { get; set; }
    public string UserNotes { get; set; } = "";
    public List<string> FinalKeywords { get; set; } = [];
    public PreliminaryScanDto PreliminaryClassification { get; set; } = new();
}

public class GenerateNoteResponse
{
    public List<NotePageDto> Pages { get; set; } = [];

    // topic-synthesizer-agent-synapse's raw Markdown, before MarkdownPager splits it into
    // Pages — brain-map-agent-synapse's final mode needs this verbatim as its primary
    // grounding source, and it's what gets persisted as Post.SynthesizedDocumentMarkdown.
    public required string SynthesizedMarkdown { get; set; }

    // Always generated, regardless of whether it ends up used — the client only applies this
    // if the user never renamed the note away from the wizard's "New Note" default (see
    // NotesStore.CreatePostFromDraftIfNeededAsync).
    public string SuggestedTitle { get; set; } = "";
}

public class GenerateFinalBrainMapRequest
{
    public List<string> DraftKeywords { get; set; } = [];
    public required string SynthesizedMarkdown { get; set; }
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
    // "draft" (keywords only, no edges yet) | "final" (enriched with real relationships).
    // Defaults to "final" so deserializing an old cached Post.GraphJson blob (written before
    // this field existed, back when there was only ever one single-shot mode) still reads as
    // the correct mode rather than an empty string.
    public string Mode { get; set; } = "final";
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
