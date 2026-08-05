namespace Client.Pages.Home;

// Mock only: keeps Post/Group state alive for the duration of the browser session so that
// likes, follows, group assignments etc. survive navigating between pages. Registered as a
// singleton in Program.cs. Once a real backend exists, this is replaced by actual API calls.
public class NotesStore
{
    public List<Post> Posts { get; } = MockPosts.GetAll();

    public List<string> Groups { get; } = [];
}
