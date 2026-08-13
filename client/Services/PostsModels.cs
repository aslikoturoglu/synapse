namespace Client.Services;

public class NoteFileDto
{
    public required string FileName { get; set; }
    public required string FileUrl { get; set; }
    public int PageCount { get; set; } = 1;
}

public class PostCommentDto
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class PostDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string MiniDescription { get; set; } = "";
    public DateOnly CreatedDate { get; set; }

    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = "";
    public string AuthorRole { get; set; } = "";

    public int? GroupId { get; set; }
    public string? GroupName { get; set; }

    public int Sends { get; set; }
    public int Downloads { get; set; }

    public List<NoteFileDto> Files { get; set; } = [];

    public int LikeCount { get; set; }
    public int FavoriteCount { get; set; }
    public int RepostCount { get; set; }
    public int CommentCount { get; set; }

    public bool LikedByMe { get; set; }
    public bool FavoritedByMe { get; set; }
    public bool RepostedByMe { get; set; }

    public List<PostCommentDto> Comments { get; set; } = [];
}

public class CreatePostRequest
{
    public required string Title { get; set; }
    public string Description { get; set; } = "";
    public string MiniDescription { get; set; } = "";
    public required List<NoteFileDto> Files { get; set; }
}

public class AddCommentRequest
{
    public required string Text { get; set; }
}

public class ReactionResponse
{
    public bool Active { get; set; }
    public int Count { get; set; }
}
