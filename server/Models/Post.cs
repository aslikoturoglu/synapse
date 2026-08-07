namespace Server.Models;

public class Post
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public string Description { get; set; } = "";
    public string MiniDescription { get; set; } = "";

    public DateOnly CreatedDate { get; set; }

    public int? GroupId { get; set; }
    public Group? Group { get; set; }

    public int Sends { get; set; }
    public int Downloads { get; set; }

    public List<NoteFile> Files { get; } = [];
    public List<PostComment> Comments { get; } = [];

    public List<User> LikedByUsers { get; } = [];
    public List<User> FavoritedByUsers { get; } = [];
    public List<User> RepostedByUsers { get; } = [];
}
