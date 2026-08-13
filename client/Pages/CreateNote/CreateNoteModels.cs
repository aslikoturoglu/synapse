using Client.Pages.Home;

namespace Client.Pages.CreateNote;

public enum NoteDraftStep { Upload, Describe, Processing, BrainMap, Finalizing, Note }

// Mock only: no backend note creation yet, so the in-progress wizard state lives here
// for the session (held by the NotesStore singleton), the same way Posts/Groups do.
public class NoteDraft
{
    public string Title { get; set; } = "New Note";
    public List<string> FileNames { get; } = [];
    public string Description { get; set; } = "";
    public NoteDraftStep Step { get; set; } = NoteDraftStep.Upload;
    public List<BrainMapKeyword> Keywords { get; } = [];
    public List<NotePage> Pages { get; } = [];
    public bool IsCreated { get; set; }

    public bool HasProgress => FileNames.Count > 0 || Step != NoteDraftStep.Upload;

    public void EnsureKeywordsSeeded()
    {
        if (Keywords.Count > 0)
            return;

        Keywords.AddRange(
        [
            new BrainMapKeyword { Text = "harm principle", Count = 40 },
            new BrainMapKeyword { Text = "justice", Count = 40 },
            new BrainMapKeyword { Text = "freedom", Count = 40 },
            new BrainMapKeyword { Text = "law", Count = 40 },
            new BrainMapKeyword { Text = "ethic", Count = 40 },
            new BrainMapKeyword { Text = "state", Count = 40 },
            new BrainMapKeyword { Text = "egalitarian", Count = 40 },
        ]);
    }
}
