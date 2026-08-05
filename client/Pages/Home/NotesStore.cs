namespace Client.Pages.Home;

// Mock only: keeps Post/Group state alive for the duration of the browser session so that
// likes, follows, group assignments etc. survive navigating between pages. Registered as a
// singleton in Program.cs. Once a real backend exists, this is replaced by actual API calls.
public class NotesStore
{
    public const string CurrentUserName = "Aslı Koturoğlu";

    public List<Post> Posts { get; } = MockPosts.GetAll();

    public List<string> Groups { get; } = [];

    public UserProfile Profile { get; } = new();

    public UserSettings Settings { get; } = new();

    public List<Post> GetMyPosts() => Posts.Where(p => p.AuthorName == CurrentUserName).ToList();

    public List<Post> GetFavorites() => Posts.Where(p => p.FavoritedByUsers.Contains(CurrentUserName)).ToList();
}
