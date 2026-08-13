namespace Client.Pages.Home;

// Mock only: there is no real AI backend yet, so "asking the AI" about a highlighted
// passage produces a templated answer referencing the selection and the note, the same way
// NoteGenerator fakes the note's body text during creation.
public static class NoteAiMockResponder
{
    public static string Answer(string question, string selectedText, string noteTitle)
    {
        var trimmedQuestion = string.IsNullOrWhiteSpace(question) ? "What does this mean?" : question.Trim();
        var snippet = selectedText.Trim();

        return $"On “{snippet}” in “{noteTitle}” — {trimmedQuestion} " +
               $"This passage ties into the wider argument the note is making, and is worth reading " +
               "alongside the other highlighted terms on this page. " +
               "(This is a mock preview — a real AI answer will replace this text.)";
    }
}
