using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace Client.Services;

// interactive-chat-agent-synapse answers in markdown (headings, bold, bullet lists) — Axon's
// chat bubbles are plain <p> text, so without this its answers showed up as literal "###" and
// "**" characters. Deliberately minimal (headings/bold/bullets/paragraphs only, no links or
// nested lists): covers what this one agent actually produces without pulling in a markdown
// library just for a chat bubble. Input is HTML-encoded before any tag is added, so nothing in
// the model's output can inject markup.
public static class MiniMarkdown
{
    private static readonly Regex BoldPattern = new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);

    public static MarkupString ToHtml(string markdown)
    {
        var html = new StringBuilder();
        var inList = false;

        foreach (var rawLine in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.Trim();

            if (line.Length == 0)
            {
                CloseList(html, ref inList);
                continue;
            }

            var headingLevel = 0;
            while (headingLevel < line.Length && headingLevel < 6 && line[headingLevel] == '#')
                headingLevel++;

            if (headingLevel is > 0 and <= 6 && headingLevel < line.Length && line[headingLevel] == ' ')
            {
                CloseList(html, ref inList);
                html.Append("<p class=\"axon-md-heading\">").Append(FormatInline(line[(headingLevel + 1)..])).Append("</p>");
                continue;
            }

            if (line.StartsWith("* ") || line.StartsWith("- "))
            {
                if (!inList)
                {
                    html.Append("<ul class=\"axon-md-list\">");
                    inList = true;
                }
                html.Append("<li>").Append(FormatInline(line[2..])).Append("</li>");
                continue;
            }

            CloseList(html, ref inList);
            html.Append("<p>").Append(FormatInline(line)).Append("</p>");
        }

        CloseList(html, ref inList);
        return new MarkupString(html.ToString());
    }

    private static void CloseList(StringBuilder html, ref bool inList)
    {
        if (!inList)
            return;

        html.Append("</ul>");
        inList = false;
    }

    private static string FormatInline(string text)
    {
        var encoded = WebUtility.HtmlEncode(text);
        return BoldPattern.Replace(encoded, "<strong>$1</strong>");
    }
}
