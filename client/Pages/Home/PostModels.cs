namespace Client.Pages.Home;

public class PostComment
{
    public required string AuthorName { get; init; }
    public required string Text { get; init; }
}

public class NoteFile
{
    public required string FileName { get; init; }
    public required string FileUrl { get; init; }
    public int PageCount { get; init; } = 1;
}

// A Post's Title, AuthorName (creator) and Files (its content) can never be empty —
// a post without a title, a creator, or at least one attached file is not a valid post.
// Everything else (Description, MiniDescription, comments, likes, favorites, reposts,
// sends, downloads, group) is optional and starts empty/zero. This mirrors the shape the
// real backend Post entity will need.
public class Post
{
    private readonly string _title = "";
    private readonly string _authorName = "";
    private readonly List<NoteFile> _files = [];

    public required string Title
    {
        get => _title;
        init => _title = RequireNonEmpty(value, nameof(Title));
    }

    public required string AuthorName
    {
        get => _authorName;
        init => _authorName = RequireNonEmpty(value, nameof(AuthorName));
    }

    public required List<NoteFile> Files
    {
        get => _files;
        init => _files = value.Count > 0
            ? value
            : throw new ArgumentException("Post.Files must contain at least one file.");
    }

    public string Description { get; init; } = "";
    public string MiniDescription { get; init; } = "";
    public string AuthorRole { get; init; } = "";
    public DateOnly CreatedDate { get; init; }
    public string? GroupName { get; set; }

    public int Sends { get; init; }
    public int Downloads { get; init; }

    public List<PostComment> Comments { get; init; } = [];
    public List<string> LikedByUsers { get; init; } = [];
    public List<string> FavoritedByUsers { get; init; } = [];
    public List<string> RepostedByUsers { get; init; } = [];

    public bool IsExpanded { get; set; }
    public bool IsFollowing { get; set; }
    public bool ShowFiles { get; set; }

    public int FileCount => Files.Count;
    public int TotalPageCount => Files.Sum(f => f.PageCount);

    private static string RequireNonEmpty(string value, string propertyName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"Post.{propertyName} cannot be empty.")
            : value;
}

// Mock data only: there is no backend Post/File storage yet. Files link to the two static
// sample PDFs in wwwroot/sample-pdfs/ so "open the file" is always a real download, even
// though the feed/notes data itself is hardcoded here instead of coming from an API.
public static class MockPosts
{
    private static readonly string[] SamplePeople =
    [
        "Mert Yılmaz", "Zeynep Aksoy", "Deniz Kaya", "Elif Demir", "Can Şahin",
        "Ayşe Yıldız", "Burak Çelik", "Selin Arslan", "Emre Koç", "Ece Aydın",
        "Kerem Özdemir", "Naz Kurt", "Onur Polat", "Gizem Tan", "Umut Er",
    ];

    public static List<Post> GetAll() =>
    [
        new Post
        {
            Title = "Sabancı Üniversitesi SPS303",
            MiniDescription = "Mill on individual liberty and the harm principle",
            Description = "Mill bireysel özgürlüğü liberal siyasetin merkezine koyar. Ona göre toplum veya devlet, " +
                           "bir bireyin iradesine karşı ancak başkalarına zarar verilmesini önlemek için müdahale edebilir. " +
                           "Kişinin kendi iyiliği, ahlaki gelişimi veya çoğunluğun rahatsızlığı tek başına müdahale " +
                           "gerekçesi değildir; özgür toplum bireyselliği, farklı yaşam tarzlarını ve non-conformity'yi " +
                           "korumalıdır. Bu örnek bir yazıdır; AI ile özeti yapılacaktır.",
            Files = [new NoteFile { FileName = "sps303-final-notes.pdf", FileUrl = "sample-pdfs/sps303-final-notes.pdf", PageCount = 3 }],
            CreatedDate = new DateOnly(2026, 7, 22),
            AuthorName = "Aslı Koturoğlu",
            AuthorRole = "Engineer",
            Sends = 3,
            Downloads = 9,
            LikedByUsers = ["Mert Yılmaz", "Zeynep Aksoy", "Deniz Kaya"],
            FavoritedByUsers = ["Mert Yılmaz"],
            Comments =
            [
                new PostComment { AuthorName = "Mert Yılmaz", Text = "Perfect for Sabancı University students" },
                new PostComment { AuthorName = "Zeynep Aksoy", Text = "Topics have changed!!!" },
            ],
        },
        new Post
        {
            Title = "CS201 Graph Algorithms",
            MiniDescription = "BFS, DFS, Dijkstra and Bellman-Ford compared",
            Description = "BFS ve DFS temel graf gezinme yöntemleridir; BFS ağırlıksız graflarda en kısa yolu bulur. " +
                           "Dijkstra algoritması negatif olmayan kenar ağırlıklarıyla çalışırken, Bellman-Ford negatif " +
                           "ağırlıkları da destekler ama daha yavaştır.",
            Files = [new NoteFile { FileName = "cs201-graph-algorithms.pdf", FileUrl = "sample-pdfs/cs201-graph-algorithms.pdf", PageCount = 2 }],
            CreatedDate = new DateOnly(2026, 7, 20),
            AuthorName = "Aslı Koturoğlu",
            AuthorRole = "Engineer",
            Sends = 1,
            Downloads = 4,
            LikedByUsers = ["Deniz Kaya"],
            RepostedByUsers = ["Zeynep Aksoy"],
            Comments =
            [
                new PostComment { AuthorName = "Deniz Kaya", Text = "Dijkstra example would help a lot" },
            ],
        },
        new Post
        {
            Title = "SPS303 Final: Kişiler ve Keyword Karşılaştırma Rehberi",
            MiniDescription = "Full-term thinkers & keyword comparison guide",
            Description = "Dönem boyunca işlenen tüm düşünürlerin (Mill, Rawls, Nozick...) temel argümanları, " +
                           "keyword karşılaştırmaları ve sınavda çıkabilecek kavram eşleştirmeleri tek dosyada.",
            Files = BuildKeywordGuideFiles(),
            CreatedDate = new DateOnly(2026, 7, 22),
            AuthorName = "Aslı Koturoğlu",
            AuthorRole = "Engineer",
            Sends = 5,
            Downloads = 5,
            LikedByUsers = [.. SamplePeople],
            FavoritedByUsers = [.. SamplePeople.Take(5)],
            RepostedByUsers = [.. SamplePeople.Skip(5).Take(5)],
            Comments =
            [
                new PostComment { AuthorName = "Kerem Özdemir", Text = "This saved my final grade, thank you!" },
                new PostComment { AuthorName = "Naz Kurt", Text = "Can you add a table for Rawls vs Nozick?" },
                new PostComment { AuthorName = "Onur Polat", Text = "Keyword list at the end is gold." },
                new PostComment { AuthorName = "Gizem Tan", Text = "Missing page 14 for me, anyone else?" },
                new PostComment { AuthorName = "Umut Er", Text = "Best summary I've seen for this course." },
            ],
        },
        new Post
        {
            Title = "Midterm Review Notes",
            MiniDescription = "Quick recap before the midterm",
            Description = "Vize öncesi hızlı tekrar: temel tanımlar, örnek sorular ve sık karıştırılan kavramlar.",
            Files = [new NoteFile { FileName = "sps303-final-notes.pdf", FileUrl = "sample-pdfs/sps303-final-notes.pdf", PageCount = 3 }],
            CreatedDate = new DateOnly(2026, 7, 22),
            AuthorName = "Aslı Koturoğlu",
            AuthorRole = "Engineer",
            Sends = 0,
            Downloads = 2,
            LikedByUsers = ["Can Şahin"],
        },
    ];

    private static List<NoteFile> BuildKeywordGuideFiles()
    {
        // 15 files, 22 pages total — enough to demonstrate the file list + paginated preview.
        int[] pageCounts = [2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1];
        var files = new List<NoteFile>();

        for (var i = 0; i < pageCounts.Length; i++)
        {
            var url = i % 2 == 0 ? "sample-pdfs/sps303-final-notes.pdf" : "sample-pdfs/cs201-graph-algorithms.pdf";
            files.Add(new NoteFile
            {
                FileName = $"SPS303_Keyword_Guide_Part_{i + 1:00}.pdf",
                FileUrl = url,
                PageCount = pageCounts[i],
            });
        }

        return files;
    }
}
