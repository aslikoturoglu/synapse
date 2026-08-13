namespace Server.Models;

public enum UserRole { User, Admin }

public class User
{
    public int Id { get; set; }

    public required string Name { get; set; }
    public required string Surname { get; set; }

    // Permanent account key — must not be editable once the account exists (Service-layer rule).
    public required string Email { get; set; }

    // Not collected at signup — filled in later from the Profile page.
    public string Phone { get; set; } = "";
    public string JobTitle { get; set; } = "";

    public required string Username { get; set; }

    // Hash only — never store the plaintext password.
    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; } = UserRole.User;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserSettings? Settings { get; set; }

    public List<Group> Groups { get; } = [];
    public List<Post> Posts { get; } = [];
    public List<PostComment> Comments { get; } = [];

    public List<Post> LikedPosts { get; } = [];
    public List<Post> FavoritedPosts { get; } = [];
    public List<Post> RepostedPosts { get; } = [];
}
