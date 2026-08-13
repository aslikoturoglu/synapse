namespace Server.Models;

public class Group
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public int OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public List<Post> Posts { get; } = [];
}
