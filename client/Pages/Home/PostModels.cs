namespace Client.Pages.Home;

public class PostComment
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public required string AuthorName { get; set; }
    public required string Text { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NoteFile
{
    public required string FileName { get; init; }
    public required string FileUrl { get; init; }
    public int PageCount { get; init; } = 1;
}

public class Post
{
    public int Id { get; set; }

    public required string Title { get; set; }
    public string Description { get; set; } = "";
    public string MiniDescription { get; set; } = "";
    public DateOnly CreatedDate { get; set; }

    public int AuthorId { get; set; }
    public required string AuthorName { get; set; }
    public string AuthorRole { get; set; } = "";

    public int? GroupId { get; set; }
    public string? GroupName { get; set; }

    public int Sends { get; set; }
    public int Downloads { get; set; }

    public required List<NoteFile> Files { get; set; }
    public List<PostComment> Comments { get; set; } = [];

    public int LikeCount { get; set; }
    public int FavoriteCount { get; set; }
    public int RepostCount { get; set; }

    public bool LikedByMe { get; set; }
    public bool FavoritedByMe { get; set; }
    public bool RepostedByMe { get; set; }

    public bool IsExpanded { get; set; }
    public bool IsFollowing { get; set; }
    public bool ShowFiles { get; set; }

    public int FileCount => Files.Count;
    public int TotalPageCount => Files.Sum(f => f.PageCount);
}
