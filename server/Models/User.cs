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

    // Null until an admin (ResetPassword) or the user themselves changes it for the first
    // time — the Signup password never counts as a "change".
    public DateTime? PasswordChangedAt { get; set; }

    // Admin-only moderation state. A deactivated account can't log in (AuthService.LoginAsync)
    // until an admin reactivates it; Reason is shown back on their profile and mailed to them.
    public bool IsDeactivated { get; set; }
    public string? DeactivationReason { get; set; }

    public UserSettings? Settings { get; set; }

    public List<Group> Groups { get; } = [];
    public List<Post> Posts { get; } = [];
    public List<PostComment> Comments { get; } = [];

    public List<Post> LikedPosts { get; } = [];
    public List<Post> FavoritedPosts { get; } = [];
    public List<Post> RepostedPosts { get; } = [];

    // Follow rows where this user is the follower (who they follow) vs. the one being
    // followed (their followers) — see AppDbContext for the two Restrict-delete FKs this needs.
    public List<Follow> FollowingLinks { get; } = [];
    public List<Follow> FollowerLinks { get; } = [];
}
