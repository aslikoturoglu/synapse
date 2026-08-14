namespace Server.Models;

public class NotePage
{
    public int Id { get; set; }

    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public int Number { get; set; }
    public string? Heading { get; set; }
    public string Body { get; set; } = "";
}
