namespace Server.Services.AiFoundry;

// Agents are instructed to emit raw JSON only, but models sometimes still wrap it in a
// ```json fenced block despite the instruction — strip that if present before parsing.
public static class JsonExtractor
{
    public static string ExtractJson(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```"))
            return trimmed;

        var firstNewline = trimmed.IndexOf('\n');
        var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        return firstNewline > 0 && lastFence > firstNewline
            ? trimmed[(firstNewline + 1)..lastFence].Trim()
            : trimmed;
    }
}
