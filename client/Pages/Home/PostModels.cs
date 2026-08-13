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

    public Guid Id { get; init; } = Guid.NewGuid();

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

    // The AI-generated note itself: brain-map keywords (with their AI/user/deleted history),
    // the finished pages, and what the user did while reading them (highlights + AI chats,
    // manual edits). Empty for posts that never went through the note-creation wizard.
    public List<BrainMapKeyword> Keywords { get; init; } = [];
    public List<NotePage> Pages { get; init; } = [];
    public List<NoteHighlight> Highlights { get; init; } = [];
    public int DocumentChangeCount { get; set; }

    public bool IsExpanded { get; set; }
    public bool IsFollowing { get; set; }
    public bool ShowFiles { get; set; }
    public bool ShowGeneratedPreview { get; set; }

    public int FileCount => Files.Count;
    public int TotalPageCount => Files.Sum(f => f.PageCount);
    public int TotalHighlightCount => Highlights.Count;
    public int TotalAiChatCount => Highlights.Sum(h => h.Messages.Count);

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
        BuildSps303Post(),
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

    // Seeded with a full Keywords/Pages/Highlights history (not just Files) so the Note
    // Reader, Brain Map history and Process views have something real to show without the
    // user having to run the creation wizard first.
    private static Post BuildSps303Post()
    {
        var millHighlight = new Guid("11111111-1111-1111-1111-111111111111");
        var harmHighlight = new Guid("22222222-2222-2222-2222-222222222222");
        var tyrannyHighlight = new Guid("33333333-3333-3333-3333-333333333333");
        var eccentricityHighlight = new Guid("44444444-4444-4444-4444-444444444444");

        const string title = "Sabancı Üniversitesi SPS303";

        var page1Body =
            $"<strong>Week 7: Individual Liberty &amp; Harm Principle</strong><br/><br/>" +
            $"1. <mark class=\"note-highlight\" data-highlight-id=\"{harmHighlight}\">Mill's Harm Principle</mark> — " +
            $"<mark class=\"note-highlight\" data-highlight-id=\"{millHighlight}\">John Stuart Mill</mark> argues that the only " +
            "legitimate reason society may restrict an individual's liberty is to prevent harm to others; a person's own good " +
            "is not by itself sufficient justification for interference.<br/><br/>" +
            "2. Two Forms of Tyranny — political tyranny (laws, police, courts) and social tyranny (custom, exclusion, cultural " +
            "pressure). Mill treats social tyranny as the more persistent threat, since it works on the mind directly.<br/><br/>" +
            $"3. <mark class=\"note-highlight\" data-highlight-id=\"{tyrannyHighlight}\">Tyranny of the Majority</mark> — even a " +
            "democratic majority can suppress minorities; legitimacy requires protecting individual rights, not just counting votes.<br/><br/>" +
            "4. Three Domains of Liberty — liberty of conscience, liberty of tastes and pursuits, and liberty of association: " +
            "without these, no form of government amounts to real freedom.";

        var page2Body =
            "5. The Self-Regarding Sphere — actions whose primary and direct effect falls on the person themself belong to the " +
            "individual alone.<br/><br/>" +
            "6. Critique of Social Conformity — pressuring everyone into the same mold suppresses individuality, slows social " +
            "progress, and narrows the range of human happiness.<br/><br/>" +
            $"7. Individuality as <mark class=\"note-highlight\" data-highlight-id=\"{eccentricityHighlight}\">Eccentricity</mark> " +
            "— for Mill, eccentricity and non-conformity are signs of a society's vitality, not a threat to it.<br/><br/>" +
            "8. Direct vs Indirect Harm — only conduct whose harm is direct and demonstrable justifies intervention; vague or " +
            "indirect offense does not.<br/><br/>" +
            "9. Feinberg's Taxonomy of Harm — physical, economic, psychological and moral harm give later philosophers a " +
            "finer-grained way to test where Mill's principle should apply.";

        return new Post
        {
            Title = title,
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
            DocumentChangeCount = 4,
            Keywords =
            [
                new BrainMapKeyword { Text = "harm principle", Count = 40 },
                new BrainMapKeyword { Text = "justice", Count = 40 },
                new BrainMapKeyword { Text = "freedom", Count = 40 },
                new BrainMapKeyword { Text = "law", Count = 40 },
                new BrainMapKeyword { Text = "ethic", Count = 40 },
                new BrainMapKeyword { Text = "state", Count = 40, Status = KeywordStatus.AiDeleted },
                new BrainMapKeyword { Text = "egalitarian", Count = 40 },
                new BrainMapKeyword { Text = "capitalism", Status = KeywordStatus.UserAdded },
                new BrainMapKeyword { Text = "socialism", Status = KeywordStatus.UserAdded },
                new BrainMapKeyword { Text = "community", Status = KeywordStatus.UserAdded },
            ],
            Pages =
            [
                new NotePage { Number = 1, Heading = "Mill's Harm Principle", Body = page1Body },
                new NotePage { Number = 2, Heading = "Individuality & Social Conformity", Body = page2Body },
            ],
            Highlights =
            [
                new NoteHighlight
                {
                    Id = millHighlight,
                    PageNumber = 1,
                    SelectedText = "John Stuart Mill",
                    Messages = { new AiChatMessage { Question = "When did he come onto the scene?", Answer = NoteAiMockResponder.Answer("When did he come onto the scene?", "John Stuart Mill", title) } },
                },
                new NoteHighlight
                {
                    Id = harmHighlight,
                    PageNumber = 1,
                    SelectedText = "Mill's Harm Principle",
                    Messages = { new AiChatMessage { Question = "When did this idea emerge?", Answer = NoteAiMockResponder.Answer("When did this idea emerge?", "Mill's Harm Principle", title) } },
                },
                new NoteHighlight
                {
                    Id = tyrannyHighlight,
                    PageNumber = 1,
                    SelectedText = "Tyranny of the Majority",
                    Messages = { new AiChatMessage { Question = "What is it?", Answer = NoteAiMockResponder.Answer("What is it?", "Tyranny of the Majority", title) } },
                },
                new NoteHighlight { Id = eccentricityHighlight, PageNumber = 2, SelectedText = "Eccentricity" },
            ],
        };
    }

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
