using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Server.Dtos;

namespace Server.Services.AiFoundry;

// Converts topic-synthesizer-agent's Markdown output into paginated NotePageDtos — one page
// per level-2 (##) or level-3 (###) heading, in document order, further split by length so a
// single long section becomes multiple pages instead of one overflowing one (the reader must
// never need to scroll inside a page — worst case it just continues on the next page). Only
// handles the small, predictable subset of Markdown the agent's own OUTPUT FORMAT actually
// produces (headings, bold text, bullet lists, paragraphs, "---" separators) — not a
// general-purpose parser.
public static class MarkdownPager
{
    private static readonly Regex BoldPattern = new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);

    // Rough budget, in raw Markdown characters, for how much a single page can hold before
    // splitting onto a continuation page — calibrated against the reader's page card
    // width/font-size (see .note-file-page-surface in app.css), not exact since markdown
    // syntax characters (**, - ) don't render 1:1, but comfortably conservative so pages
    // don't need to scroll.
    private const int MaxCharsPerPage = 2000;

    public static List<NotePageDto> SplitIntoPages(string markdown)
    {
        var sections = SplitOnHeadings(markdown);
        var pages = new List<NotePageDto>();

        foreach (var section in sections)
            AddSectionPages(pages, section.Heading, section.Body);

        if (pages.Count == 0)
            AddSectionPages(pages, null, markdown);

        return pages;
    }

    // Only the first continuation chunk of a section keeps its heading — like a book chapter
    // that doesn't repeat "Chapter 5" on every one of its pages.
    private static void AddSectionPages(List<NotePageDto> pages, string? heading, string body)
    {
        var chunks = SplitBodyIntoChunks(body, MaxCharsPerPage);
        for (var i = 0; i < chunks.Count; i++)
            pages.Add(new NotePageDto { Number = pages.Count + 1, Heading = i == 0 ? heading : null, Body = ToHtml(chunks[i]) });
    }

    // Cuts only on line boundaries — every line this format actually produces (a bullet item,
    // a paragraph, a blank separator) is self-contained, so this never splits mid-syntax.
    private static List<string> SplitBodyIntoChunks(string body, int maxChars)
    {
        var lines = body.Replace("\r\n", "\n").Split('\n');
        var chunks = new List<string>();
        var current = new List<string>();
        var currentLength = 0;

        foreach (var line in lines)
        {
            var lineLength = line.Length + 1;
            if (currentLength + lineLength > maxChars && current.Count > 0)
            {
                chunks.Add(string.Join('\n', current));
                current = [];
                currentLength = 0;
            }

            current.Add(line);
            currentLength += lineLength;
        }

        if (current.Count > 0)
            chunks.Add(string.Join('\n', current));

        return chunks.Count > 0 ? chunks : [body];
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
