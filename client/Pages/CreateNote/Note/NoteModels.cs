using Client.Pages.CreateNote;

namespace Client.Pages.CreateNote.Note;

// Mock only: there is no AI backend, so the "generated" note is a short templated
// document built from the title, description and brain-map keywords the user picked.
public static class NoteGenerator
{
    public static List<NotePage> Generate(NoteDraft draft)
    {
        var pages = new List<NotePage>
        {
            new() { Number = 1, Heading = draft.Title, Body = BuildIntro(draft) },
        };

        var number = 2;
        foreach (var keyword in draft.Keywords)
        {
            pages.Add(new NotePage
            {
                Number = number++,
                Heading = Capitalize(keyword.Text),
                Body = BuildKeywordBody(keyword, draft),
            });
        }

        return pages;
    }

    private static string BuildIntro(NoteDraft draft)
    {
        var description = string.IsNullOrWhiteSpace(draft.Description)
            ? "a synthesis of the documents you uploaded"
            : draft.Description.Trim();

        var keywordList = draft.Keywords.Count > 0
            ? string.Join(", ", draft.Keywords.Select(k => k.Text))
            : "no keywords yet";

        return $"This note was generated from your uploaded files based on: \"{description}\". " +
               $"Key topics identified across the documents: {keywordList}.";
    }

    private static string BuildKeywordBody(BrainMapKeyword keyword, NoteDraft draft)
    {
        var frequency = keyword.Count is int count
            ? $"It appeared roughly {count} times across the uploaded documents."
            : "You added this topic by hand, so it has no measured frequency yet.";

        return $"\"{Capitalize(keyword.Text)}\" is one of the themes connected to \"{draft.Title}\". " +
               $"{frequency} This section gathers the passages related to {keyword.Text} and links them back to " +
               "the other topics in your brain map. (This is a mock preview — a real AI summary will replace this text.)";
    }

    private static string Capitalize(string text) =>
        text.Length == 0 ? text : char.ToUpper(text[0]) + text[1..];
}
