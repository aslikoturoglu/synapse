namespace Server.Dtos;

public class NoteFileDto
{
    public required string FileName { get; set; }
    public required string FileUrl { get; set; }
    public int PageCount { get; set; } = 1;
}

public class PostDto
{
    public required int Id { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = "";
    public string MiniDescription { get; set; } = "";
    public required DateOnly CreatedDate { get; set; }

    public required int AuthorId { get; set; }
    public required string AuthorName { get; set; }
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
}

public class CreatePostRequest
{
    public required string Title { get; set; }
    public string Description { get; set; } = "";
    public string MiniDescription { get; set; } = "";
    public required List<NoteFileDto> Files { get; set; }
}

public class UpdatePostGroupRequest
{
    public int? GroupId { get; set; }
}
