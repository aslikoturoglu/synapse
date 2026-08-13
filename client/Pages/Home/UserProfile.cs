namespace Client.Pages.Home;

// Mock only: no backend account storage yet, so profile edits live here for the
// session (held by the NotesStore singleton), the same way Posts/Groups do.
public class UserProfile
{
    public string Name { get; set; } = "Aslı";
    public string Surname { get; set; } = "Koturoğlu";
    // Settable (not init) so login/signup can hydrate this session's profile with the real
    // authenticated user's email — Profile.razor still never renders an edit control for it,
    // so the "email isn't user-editable" rule stays enforced at the UI layer.
    public string Email { get; set; } = "asli.koturoglu@gmail.com";
    public string Phone { get; set; } = "+90 (552) 205 53 50";
    public string JobTitle { get; set; } = "Student";
    public string Username { get; set; } = "asli.koturoglu";
    public string Password { get; set; } = "Password123!";

    public string FullName => $"{Name} {Surname}";
}

public enum PostVisibility { Everyone, Connections, NoOne }

public enum PasswordExpiry { SixMonths, ThreeMonths, Never }

public class UserSettings
{
    public PostVisibility PostVisibility { get; set; } = PostVisibility.Everyone;
    public PostVisibility FavoritesVisibility { get; set; } = PostVisibility.Everyone;
    public PostVisibility CommentPermission { get; set; } = PostVisibility.Everyone;
    public PasswordExpiry PasswordExpiry { get; set; } = PasswordExpiry.SixMonths;
}
