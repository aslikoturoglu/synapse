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
// section, so the table of contents can list just those.
public class NotePage
{
    public required int Number { get; init; }
    public string? Heading { get; init; }
    public string Body { get; set; } = "";
}

// One question/answer pair from interactive-chat-agent-synapse. Answer starts empty and is
// filled in once the real server round-trip completes — see NotesStore.CreateHighlightAsync/
// AddHighlightMessageAsync, which mutate it in place so the caller's already-rendered bubble
// (same object reference) picks up the real answer instead of a re-add.
public class AiChatMessage
{
    public required string Question { get; init; }
    public string Answer { get; set; } = "";
    public DateTime CreatedAt { get; init; } = DateTime.Now;
}

// A piece of text the user selected in the generated note to ask the AI about.
// Kept on the Post so re-opening the note later shows the same highlight and its
// chat history (Process view).
public class NoteHighlight
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required int PageNumber { get; init; }
    public required string SelectedText { get; init; }
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
}
