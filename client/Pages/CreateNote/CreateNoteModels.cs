using Client.Pages.Home;

namespace Client.Pages.CreateNote;

public enum NoteDraftStep { Upload, Describe, Orchestrator, BrainMap, Processing, Note }

// A file the user picked in the Upload step, kept in full (not just its name) so the
// Orchestrator step can actually upload its bytes to Azure AI Foundry.
public class UploadedFileDraft
{
    public required string Name { get; init; }
    public required byte[] Bytes { get; init; }
}

// The Pipeline Directive orchestrator-chat-agent-synapse emits once the user picks their
// menu items — drives both the initial Brain Map seed and the document-rag/topic-synthesizer
// generation step that follows it.
public class NoteAiDirective
{
    public List<string> SeedKeywords { get; set; } = [];
    public List<string> FocusAreas { get; set; } = [];
    public string SynthesisMode { get; set; } = "";
    public string UserCustomInstructions { get; set; } = "";
}

// The in-progress wizard state lives here for the session (held by the NotesStore
// singleton) so a partially-finished note survives navigating away and back — see
// HasProgress/CreateNote.razor's resume-draft banner. Once IsCreated is set the wizard is
// done with this draft; CreateNote.razor resets it back to blank before starting another.
public class NoteDraft
{
    public string Title { get; set; } = "New Note";
    public List<string> FileNames { get; } = [];
    public List<UploadedFileDraft> Files { get; } = [];
    public string Description { get; set; } = "";
    public NoteDraftStep Step { get; set; } = NoteDraftStep.Upload;
    public List<BrainMapKeyword> Keywords { get; } = [];
    public List<NotePage> Pages { get; } = [];
    public bool IsCreated { get; set; }
    public int? CreatedPostId { get; set; }

    // Orchestrator/document-rag pipeline state, carried through the wizard steps.
    public string? OrchestratorThreadId { get; set; }
    public List<string> FoundryFileIds { get; } = [];
    public string OrchestratorSummary { get; set; } = "";
    public List<int> SelectedMenuNumbers { get; } = [];
    public NoteAiDirective? Directive { get; set; }
    public string? DocumentKnowledgeBase { get; set; }

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
        Description = "";
        Step = NoteDraftStep.Upload;
        Keywords.Clear();
        Pages.Clear();
        IsCreated = false;
        CreatedPostId = null;
        OrchestratorThreadId = null;
        FoundryFileIds.Clear();
        OrchestratorSummary = "";
        SelectedMenuNumbers.Clear();
        Directive = null;
        DocumentKnowledgeBase = null;
    }

    // Seeds the Brain Map from the orchestrator's directive once it's available; falls back
    // to a small generic seed if the wizard somehow reaches BrainMap without one (shouldn't
    // happen via the normal Orchestrator step, but keeps the step from rendering empty).
    public void EnsureKeywordsSeeded()
    {
        if (Keywords.Count > 0)
            return;

        var seeds = Directive is { } directive
            ? directive.SeedKeywords.Concat(directive.FocusAreas).Distinct(StringComparer.OrdinalIgnoreCase)
            : ["main topic"];

        Keywords.AddRange(seeds.Select(text => new BrainMapKeyword { Text = text, Count = null }));
    }
}
