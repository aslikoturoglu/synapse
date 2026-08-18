using System.Text.Json.Serialization;
using Client.Pages.Home;
using Client.Services;

namespace Client.Pages.CreateNote;

// Intake replaces the old fixed 5-item numbered-menu Orchestrator step and the separate
// Describe step: it's what actually fires StartAsync (document-rag-agent + the Orchestrator's
// preliminary scan + brain-map-agent's draft mode, all immediately, per the Orchestrator's own
// "don't wait for user input" spec) as soon as it loads, right after Upload.
public enum NoteDraftStep { Upload, Intake, BrainMap, Processing, Note }

// A file the user picked in the Upload step, kept in full (not just its name) so StartAsync
// can actually upload its bytes to Azure AI Foundry.
public class UploadedFileDraft
{
    public required string Name { get; init; }
    public required byte[] Bytes { get; init; }
}

// The in-progress wizard state — held by NotesStore and mirrored to localStorage (see
// NotesStore.SaveDraftStateAsync) so a partially-finished note survives a reload, not just
// in-tab navigation. Once IsCreated is set the wizard is done with this draft.
//
// Files is deliberately excluded from persistence ([JsonIgnore]) — raw file bytes don't fit
// localStorage's quota and, past the Intake step, aren't needed again anyway (StartAsync is
// the only thing that ever reads them; every later step works off OrchestratorThreadId /
// DocumentKnowledgeBase / Keywords / Pages instead). FileNames alone survives for display.
public class NoteDraft
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string Title { get; set; } = "New Note";
    public List<string> FileNames { get; } = [];
    [JsonIgnore] public List<UploadedFileDraft> Files { get; } = [];
    public NoteDraftStep Step { get; set; } = NoteDraftStep.Upload;
    public List<BrainMapKeyword> Keywords { get; } = [];
    public List<NotePage> Pages { get; } = [];
    public bool IsCreated { get; set; }
    public int? CreatedPostId { get; set; }

    // Orchestrator/document-rag/brain-map-draft pipeline state — all fired and populated by
    // AiApiClient.StartAsync immediately on upload (see IntakeStep.razor), then consumed
    // later at Processing.
    public string? OrchestratorThreadId { get; set; }
    public List<string> FoundryFileIds { get; } = [];
    public List<string> PreliminaryKeywords { get; } = [];
    public PreliminaryScanDto? PreliminaryClassification { get; set; }
    public string? DocumentKnowledgeBase { get; set; }
    public GraphDto? DraftGraph { get; set; }

    // Collected on the free-form Intake step. Extra keywords are added directly on the
    // BrainMap step's own editor instead (BrainMapDiagram's existing add/remove UI) — no
    // separate field needed here for that.
    public string UserNotes { get; set; } = "";
    public string FormatPreference { get; set; } = "";

    // Produced by the Processing step.
    public string? SynthesizedMarkdown { get; set; }
    public GraphDto? FinalGraph { get; set; }

    public bool HasProgress => FileNames.Count > 0 || Step != NoteDraftStep.Upload;

    // Called before starting a fresh wizard run when the previous draft already became a
    // saved post — otherwise every subsequent "Create New Note" would silently reuse that
    // same old draft (same Pages/Keywords/IsCreated) and CreatePostFromDraftIfNeededAsync
    // would just hand back the already-created post instead of creating a new one.
    public void Reset()
    {
        Title = "New Note";
        FileNames.Clear();
        Files.Clear();
        Step = NoteDraftStep.Upload;
        Keywords.Clear();
        Pages.Clear();
        IsCreated = false;
        CreatedPostId = null;
        OrchestratorThreadId = null;
        FoundryFileIds.Clear();
        PreliminaryKeywords.Clear();
        PreliminaryClassification = null;
        DocumentKnowledgeBase = null;
        DraftGraph = null;
        UserNotes = "";
        FormatPreference = "";
        SynthesizedMarkdown = null;
        FinalGraph = null;
    }

    // Seeds the Brain Map from the Orchestrator's preliminary scan once it's available;
    // falls back to a small generic seed if the wizard somehow reaches BrainMap without one
    // (shouldn't happen via the normal Upload step, but keeps the step from rendering empty).
    public void EnsureKeywordsSeeded()
    {
        if (Keywords.Count > 0)
            return;

        var seeds = PreliminaryKeywords.Count > 0 ? PreliminaryKeywords : ["main topic"];
        Keywords.AddRange(seeds.Select(text => new BrainMapKeyword { Text = text, Count = null }));
    }
}
