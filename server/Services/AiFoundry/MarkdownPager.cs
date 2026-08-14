using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Server.Dtos;

namespace Server.Services.AiFoundry;

// Converts topic-synthesizer-agent's Markdown output into paginated NotePageDtos — one page
// per level-2 (##) or level-3 (###) heading, in document order. Only handles the small,
// predictable subset of Markdown the agent's own OUTPUT FORMAT actually produces (headings,
// bold text, bullet lists, paragraphs, "---" separators) — not a general-purpose parser.
public static class MarkdownPager
{
    private static readonly Regex BoldPattern = new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);

    public static List<NotePageDto> SplitIntoPages(string markdown)
    {
        var sections = SplitOnHeadings(markdown);
        var pages = sections
            .Select((section, index) => new NotePageDto { Number = index + 1, Heading = section.Heading, Body = ToHtml(section.Body) })
            .ToList();

        if (pages.Count == 0)
            pages.Add(new NotePageDto { Number = 1, Heading = null, Body = ToHtml(markdown) });

        return pages;
    }

    private static List<(string? Heading, string Body)> SplitOnHeadings(string markdown)
    {
        var sections = new List<(string? Heading, string Body)>();
        string? currentHeading = null;
        var currentBody = new List<string>();

        foreach (var line in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.StartsWith("## ") || line.StartsWith("### "))
            {
                if (currentHeading is not null || currentBody.Any(l => !string.IsNullOrWhiteSpace(l)))
                    sections.Add((currentHeading, string.Join('\n', currentBody)));

                currentHeading = line.TrimStart('#', ' ');
                currentBody = [];
            }
            else if (line.StartsWith("# "))
            {
                // The document's own top-level title — not a page of its own.
            }
            else
            {
                currentBody.Add(line);
            }
        }

        if (currentHeading is not null || currentBody.Any(l => !string.IsNullOrWhiteSpace(l)))
            sections.Add((currentHeading, string.Join('\n', currentBody)));

        return sections;
    }

    private static string ToHtml(string markdown)
    {
        var html = new StringBuilder();
        var inList = false;

        foreach (var rawLine in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            var trimmed = rawLine.Trim();

            if (trimmed.Length == 0 || trimmed.StartsWith("---"))
            {
                if (inList) { html.Append("</ul>"); inList = false; }
                continue;
            }

            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
            {
                if (!inList) { html.Append("<ul>"); inList = true; }
                html.Append("<li>").Append(InlineToHtml(trimmed[2..])).Append("</li>");
                continue;
            }

            if (inList) { html.Append("</ul>"); inList = false; }
            html.Append("<p>").Append(InlineToHtml(trimmed)).Append("</p>");
        }

        if (inList)
            html.Append("</ul>");

        return html.ToString();
    }

    private static string InlineToHtml(string text) =>
        BoldPattern.Replace(WebUtility.HtmlEncode(text), "<strong>$1</strong>");
}
