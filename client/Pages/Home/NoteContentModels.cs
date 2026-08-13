namespace Client.Pages.Home;

public enum KeywordStatus { AiKept, AiDeleted, UserAdded }

public class BrainMapKeyword
{
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

// Mock only: there is no real AI, so this is one templated question/answer pair
// produced by NoteAiMockResponder instead of an actual model call.
public class AiChatMessage
{
    public required string Question { get; init; }
    public required string Answer { get; init; }
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

// Shared by the creation wizard's BrainMapStep and the reopened Brain Map history page —
// both just add/remove keywords on whichever list they're holding (NoteDraft.Keywords or
// Post.Keywords).
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
