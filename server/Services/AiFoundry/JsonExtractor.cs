using System.Text.Json;

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

    // Agents (orchestrator-chat-agent-synapse especially) sometimes emit more than one
    // top-level JSON object in the same response — e.g. proactively firing its immediate
    // START_DOCUMENT_RAG trigger AND whatever was actually asked for, back to back — so this
    // scans for every complete top-level {...} object instead of assuming the whole response
    // is exactly one JSON value.
    public static List<string> ExtractJsonObjects(string text)
    {
        var cleaned = ExtractJson(text);
        var objects = new List<string>();
        var depth = 0;
        var start = -1;
        var inString = false;
        var escaped = false;

        for (var i = 0; i < cleaned.Length; i++)
        {
            var c = cleaned[i];

            if (inString)
            {
                if (escaped)
                    escaped = false;
                else if (c == '\\')
                    escaped = true;
                else if (c == '"')
                    inString = false;
                continue;
            }

            switch (c)
            {
                case '"':
                    inString = true;
                    break;
                case '{':
                    if (depth == 0)
                        start = i;
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth == 0 && start >= 0)
                    {
                        objects.Add(cleaned[start..(i + 1)]);
                        start = -1;
                    }
                    break;
            }
        }

        return objects.Count > 0 ? objects : [cleaned];
    }

    // Picks whichever top-level object actually has `property` set, preferring the last
    // match — falls back to the last object overall (still the best guess) if none match, so
    // callers always get something parseable rather than an exception.
    public static string SelectJsonObject(string text, string property)
    {
        var objects = ExtractJsonObjects(text);
        for (var i = objects.Count - 1; i >= 0; i--)
        {
            try
            {
                using var doc = JsonDocument.Parse(objects[i]);
                if (doc.RootElement.TryGetProperty(property, out _))
                    return objects[i];
            }
            catch (JsonException)
            {
            }
        }

        return objects[^1];
    }
}
