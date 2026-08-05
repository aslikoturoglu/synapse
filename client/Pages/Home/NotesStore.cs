using Client.Pages.CreateNote;

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

    public NoteDraft Draft { get; } = new();

    public List<Post> GetMyPosts() => Posts.Where(p => p.AuthorName == CurrentUserName).ToList();

    public List<Post> GetFavorites() => Posts.Where(p => p.FavoritedByUsers.Contains(CurrentUserName)).ToList();

    // Called once the wizard reaches the finished-note view, so the note it just built
    // actually shows up in My Posts / All Notes — not a moment earlier, not a moment later.
    public void CreatePostFromDraftIfNeeded()
    {
        if (Draft.IsCreated)
            return;

        Posts.Insert(0, new Post
        {
            Title = Draft.Title,
            Description = Draft.Description,
            AuthorName = CurrentUserName,
            AuthorRole = Profile.JobTitle,
            CreatedDate = DateOnly.FromDateTime(DateTime.Now),
            Files = [new NoteFile
            {
                FileName = $"{Draft.Title}.pdf",
                FileUrl = "sample-pdfs/sps303-final-notes.pdf",
                PageCount = Math.Max(Draft.Pages.Count, 1),
            }],
        });

        Draft.IsCreated = true;
    }
}
