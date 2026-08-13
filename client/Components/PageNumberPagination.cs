namespace Client.Components;

// Shared by every page-number strip in the note views (raw file preview, generated note
// pages, Process document column): shows 1, 2, 3 … second-to-last, last, plus whichever
// page is currently open, collapsing the rest behind an ellipsis.
public static class PageNumberPagination
{
    public static List<int?> GetItems(int total, int current)
    {
        if (total <= 7)
            return [.. Enumerable.Range(1, total).Select(i => (int?)i)];

        HashSet<int> shown = [1, 2, 3, total - 1, total, current];
        var sortedPages = shown.Where(p => p >= 1 && p <= total).OrderBy(p => p).ToList();

        var items = new List<int?>();
        int? previous = null;
        foreach (var p in sortedPages)
        {
            if (previous is not null && p - previous > 1)
                items.Add(null);

            items.Add(p);
            previous = p;
        }

        return items;
    }
}
