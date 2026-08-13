namespace Server.Models;

public class NoteFile
{
    public int Id { get; set; }

    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public required string FileName { get; set; }
    public required string FileUrl { get; set; }
    public int PageCount { get; set; } = 1;
}
