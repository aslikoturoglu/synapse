using System.Net;
using Client.Pages.CreateNote;
using Client.Services;

namespace Client.Pages.Home;

// Holds session-wide UI state. Posts now come from the real backend (via PostsApiClient);
// Groups/Profile/Settings are still mock-only — kept here for the session the same way Posts
// used to be, until those get their own backend wiring.
public class NotesStore(AuthState authState, PostsApiClient postsApi)
{
    public List<Post> Posts { get; private set; } = [];

    public List<string> Groups { get; } = [];

    public UserProfile Profile { get; } = new();

    public UserSettings Settings { get; } = new();

    public NoteDraft Draft { get; } = new();

    // Hydrates the session's mock Profile from a real authenticated user — called on login
    // and again on app startup once AuthState restores a session from localStorage, since
    // this singleton is otherwise reconstructed with its hardcoded defaults on every reload.
    public void ApplyAuthUser(UserDto user)
    {
        Profile.Name = user.Name;
        Profile.Surname = user.Surname;
        Profile.Username = user.Username;
        Profile.Email = user.Email;
        Profile.Phone = user.Phone;
        Profile.JobTitle = user.JobTitle;
    }

    // Admin-only: usernames the admin has deactivated. Mock only, same as everything else
    // here — there's no Users table on the client, so "an account" is just an AuthorName.
    public HashSet<string> DeactivatedUsers { get; } = [];

    // Swallows anything except 401: a transient/network failure shouldn't crash whatever
    // flow is loading the feed (login, signup, app boot) — it just leaves Posts empty for
    // now. A 401 means the caller's token is dead and propagates, since only the caller
    // (Program.cs, on app boot) knows to clear the stale session out of localStorage.
    public async Task LoadFeedAsync()
    {
        try
        {
            Posts = (await postsApi.GetFeedAsync()).Select(ToPost).ToList();
        }
        catch (HttpRequestException ex) when (ex.StatusCode != HttpStatusCode.Unauthorized)
        {
        }
    }

    public List<Post> GetMyPosts() => Posts.Where(p => p.AuthorId == authState.CurrentUser?.Id).ToList();

    public List<Post> GetFavorites() => Posts.Where(p => p.FavoritedByMe).ToList();

    public List<Post> GetPostsByAuthor(string authorName) => Posts.Where(p => p.AuthorName == authorName).ToList();

    // LikedByMe/FavoritedByMe only reflect the signed-in caller's own reactions — the backend
    // doesn't expose who else liked/favorited a post, so these are only meaningful for the
    // signed-in user's own name. Other authors' tabs come back empty until that exists.
    public List<Post> GetFavoritesByAuthor(string authorName) =>
        authorName == Profile.FullName ? GetFavorites() : [];

    public List<Post> GetLikesByAuthor(string authorName) =>
        authorName == Profile.FullName ? Posts.Where(p => p.LikedByMe).ToList() : [];

    public List<(Post Post, PostComment Comment)> GetCommentsByAuthor(string authorName) =>
        Posts.SelectMany(p => p.Comments.Where(c => c.AuthorName == authorName).Select(c => (Post: p, Comment: c))).ToList();

    public void ToggleUserDeactivated(string authorName)
    {
        if (!DeactivatedUsers.Remove(authorName))
            DeactivatedUsers.Add(authorName);
    }

    // Called once the wizard reaches the finished-note view, so the note it just built
    // actually shows up in My Posts / All Notes — not a moment earlier, not a moment later.
    public async Task CreatePostFromDraftIfNeededAsync()
    {
        if (Draft.IsCreated)
            return;

        var request = new CreatePostRequest
        {
            Title = Draft.Title,
            Description = Draft.Description,
            Files =
            [
                new NoteFileDto
                {
                    FileName = $"{Draft.Title}.pdf",
                    FileUrl = "sample-pdfs/sps303-final-notes.pdf",
                    PageCount = Math.Max(Draft.Pages.Count, 1),
                },
            ],
        };

        var dto = await postsApi.CreateAsync(request);
        if (dto is not null)
            Posts.Insert(0, ToPost(dto));

        Draft.IsCreated = true;
    }

    public async Task<bool> DeletePostAsync(Post post)
    {
        if (!await postsApi.DeleteAsync(post.Id))
            return false;

        Posts.Remove(post);
        return true;
    }

    public async Task ToggleLikeAsync(Post post) => Apply(post, await postsApi.ToggleLikeAsync(post.Id), (p, r) =>
    {
        p.LikedByMe = r.Active;
        p.LikeCount = r.Count;
    });

    public async Task ToggleFavoriteAsync(Post post) => Apply(post, await postsApi.ToggleFavoriteAsync(post.Id), (p, r) =>
    {
        p.FavoritedByMe = r.Active;
        p.FavoriteCount = r.Count;
    });

    public async Task ToggleRepostAsync(Post post) => Apply(post, await postsApi.ToggleRepostAsync(post.Id), (p, r) =>
    {
        p.RepostedByMe = r.Active;
        p.RepostCount = r.Count;
    });

    public async Task AddCommentAsync(Post post, string text)
    {
        var comment = await postsApi.AddCommentAsync(post.Id, text);
        if (comment is not null)
            post.Comments.Add(ToComment(comment));
    }

    public async Task DeleteCommentAsync(Post post, PostComment comment)
    {
        if (await postsApi.DeleteCommentAsync(comment.Id))
            post.Comments.Remove(comment);
    }

    private static void Apply(Post post, ReactionResponse? result, Action<Post, ReactionResponse> apply)
    {
        if (result is not null)
            apply(post, result);
    }

    private static Post ToPost(PostDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        Description = dto.Description,
        MiniDescription = dto.MiniDescription,
        CreatedDate = dto.CreatedDate,
        AuthorId = dto.AuthorId,
        AuthorName = dto.AuthorName,
        AuthorRole = dto.AuthorRole,
        GroupId = dto.GroupId,
        GroupName = dto.GroupName,
        Sends = dto.Sends,
        Downloads = dto.Downloads,
        Files = dto.Files.Select(f => new NoteFile { FileName = f.FileName, FileUrl = f.FileUrl, PageCount = f.PageCount }).ToList(),
        Comments = dto.Comments.Select(ToComment).ToList(),
        LikeCount = dto.LikeCount,
        FavoriteCount = dto.FavoriteCount,
        RepostCount = dto.RepostCount,
        LikedByMe = dto.LikedByMe,
        FavoritedByMe = dto.FavoritedByMe,
        RepostedByMe = dto.RepostedByMe,
    };

    private static PostComment ToComment(PostCommentDto dto) => new()
    {
        Id = dto.Id,
        AuthorId = dto.AuthorId,
        AuthorName = dto.AuthorName,
        Text = dto.Text,
        CreatedAt = dto.CreatedAt,
    };
}
