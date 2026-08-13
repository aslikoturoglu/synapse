namespace Server.Models;

public enum PostVisibility { Everyone, Connections, NoOne }

public enum PasswordExpiry { SixMonths, ThreeMonths, Never }

public class UserSettings
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public PostVisibility PostVisibility { get; set; } = PostVisibility.Everyone;
    public PostVisibility FavoritesVisibility { get; set; } = PostVisibility.Everyone;
    public PostVisibility CommentPermission { get; set; } = PostVisibility.Everyone;
    public PasswordExpiry PasswordExpiry { get; set; } = PasswordExpiry.SixMonths;
}
