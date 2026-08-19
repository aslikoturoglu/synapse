namespace Client.Pages.Home;

public enum KeywordStatus { AiKept, AiDeleted, UserAdded }

public class BrainMapKeyword
{
    // 0 for keywords that only exist in the creation wizard's Draft so far — assigned a real
    // id by the server once the note is actually saved (Post.Keywords).
    public int Id { get; set; }

    public required string Text { get; set; }

    // null means the keyword was added by hand (no real frequency to report).
    public int? Count { get; set; }

    public KeywordStatus Status { get; set; } = KeywordStatus.AiKept;
}

// One page of the finished note. Heading is set only on pages that start a new
// section, so the table of contents can list just those. Set (not init) like Body — both can
// be rewritten in place by a manual edit or the Ask AI "Add to Document" flow.
public class NotePage
{
    public required int Number { get; init; }
    public string? Heading { get; set; }
    public string Body { get; set; } = "";
}

// One question/answer pair from interactive-chat-agent-synapse. Answer starts empty and is
// filled in once the real server round-trip completes — see NotesStore.CreateHighlightAsync/
// AddHighlightMessageAsync, which mutate it in place so the caller's already-rendered bubble
// (same object reference) picks up the real answer instead of a re-add.
public class AiChatMessage
{
    // 0 until the server round-trip that created/persisted this message returns — see
    // NotesStore.CreateHighlightAsync/AddHighlightMessageAsync.
    public int Id { get; set; }
    public required string Question { get; init; }
    public string Answer { get; set; } = "";
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    // Set once the user actually clicks "Add to Document" for this message — lets the Process
    // view call out document-changing messages distinctly from plain Q&A (NotesStore.
    // MarkAddedToDocumentAsync).
    public bool AddedToDocument { get; set; }
}

// A piece of text the user selected in the generated note to ask the AI about.
// Kept on the Post so re-opening the note later shows the same highlight and its
// chat history (Process view).
public class NoteHighlight
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required int PageNumber { get; init; }
    public required string SelectedText { get; init; }

    // Whether this highlight was made in the page's Heading rather than its Body — decides
    // which element/field NotePageView's highlight/scroll/edit methods operate on.
    public bool TargetsHeading { get; init; }

    public List<AiChatMessage> Messages { get; } = [];
}

// Used by the creation wizard's BrainMapStep, where keywords only live in the local Draft
// and there's no server round-trip yet. The reopened Brain Map page (a real, saved Post)
// mirrors this same remove-or-mark-deleted decision, but the server makes it — see
// PostService.RemoveKeywordAsync.
public static class BrainMapKeywordActions
{
    public static void Remove(List<BrainMapKeyword> keywords, BrainMapKeyword keyword)
    {
        if (keyword.Status == KeywordStatus.UserAdded)
            keywords.Remove(keyword);
        else
            keyword.Status = KeywordStatus.AiDeleted;
    }

    public static void Add(List<BrainMapKeyword> keywords, string text) =>
        keywords.Add(new BrainMapKeyword { Text = text, Count = null, Status = KeywordStatus.UserAdded });

    // How many times each AI-suggested keyword actually shows up in the uploaded files —
    // searched against DocumentKnowledgeBase (document-rag-agent-synapse's extracted read of
    // those files), the closest thing to real file text available client-side; there's no PDF
    // text-extraction pipeline of our own to search the raw uploads directly. User-added
    // keywords are left at null (see BrainMapKeyword.Count) since they were never claimed to
    // come from the files in the first place.
    public static void ApplyOccurrenceCounts(List<BrainMapKeyword> keywords, string? fileText)
    {
        if (string.IsNullOrWhiteSpace(fileText))
            return;

        foreach (var keyword in keywords)
        {
            if (keyword.Status != KeywordStatus.UserAdded)
                keyword.Count = CountOccurrences(fileText, keyword.Text);
        }
    }

    private static int CountOccurrences(string text, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return 0;

        return System.Text.RegularExpressions.Regex.Matches(
            text, System.Text.RegularExpressions.Regex.Escape(keyword), System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
    }
}
