namespace Client.Pages.Home;

public class PostComment
{
    public required string AuthorName { get; init; }
    public required string Text { get; init; }
}

// A Post's Title, AuthorName (creator) and FileUrl (its content) can never be empty —
// a post without a title, a creator, or an actual file attached to it is not a valid post.
// Everything else (Description, MiniDescription, comments, likes, favorites, reposts) is
// optional and starts empty. This mirrors the shape the real backend Post entity will need.
public class Post
{
    private readonly string _title = "";
    private readonly string _authorName = "";
    private readonly string _fileUrl = "";

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

    public required string FileUrl
    {
        get => _fileUrl;
        init => _fileUrl = RequireNonEmpty(value, nameof(FileUrl));
    }

    public string Description { get; init; } = "";
    public string MiniDescription { get; init; } = "";
    public string AuthorRole { get; init; } = "";
    public int PageCount { get; init; }

    public List<PostComment> Comments { get; init; } = [];
    public List<string> LikedByUsers { get; init; } = [];
    public List<string> FavoritedByUsers { get; init; } = [];
    public List<string> RepostedByUsers { get; init; } = [];

    public bool IsExpanded { get; set; }
    public bool IsFollowing { get; set; }

    private static string RequireNonEmpty(string value, string propertyName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"Post.{propertyName} cannot be empty.")
            : value;
}

// Mock data only: there is no backend Post/File storage yet. Each post links to a static
// sample PDF in wwwroot/sample-pdfs/ so the "open the file" interaction is real, even though
// the feed data itself is hardcoded here instead of coming from an API.
public static class MockPosts
{
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
            FileUrl = "sample-pdfs/sps303-final-notes.pdf",
            PageCount = 3,
            AuthorName = "Aslı Koturoğlu",
            AuthorRole = "Engineer",
            LikedByUsers = ["Mert Yılmaz", "Zeynep Aksoy", "Deniz Kaya"],
            FavoritedByUsers = ["Mert Yılmaz"],
            RepostedByUsers = [],
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
            FileUrl = "sample-pdfs/cs201-graph-algorithms.pdf",
            PageCount = 2,
            AuthorName = "Aslı Koturoğlu",
            AuthorRole = "Engineer",
            LikedByUsers = ["Deniz Kaya"],
            RepostedByUsers = ["Zeynep Aksoy"],
            Comments =
            [
                new PostComment { AuthorName = "Deniz Kaya", Text = "Dijkstra example would help a lot" },
            ],
        },
    ];
}
