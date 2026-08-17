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

    // Swallows anything except 401: a transient/network failure shouldn't crash whatever
    // flow is loading the feed (login, signup, app boot) — it just leaves Posts empty for
    // now. A 401 means the caller's token is dead and propagates, since only the caller
    // (Program.cs, on app boot) knows to clear the stale session out of localStorage.
    //
    // Posts is the union of the public feed (everyone's *shared* posts) and the caller's own
    // posts (shared or not, via GetMineAsync) — otherwise a not-yet-shared note the caller
    // just created wouldn't show up in their own All Notes. Home.razor is what's responsible
    // for filtering back down to IsShared when rendering the public feed.
    public async Task LoadFeedAsync()
    {
        try
        {
            var feed = await postsApi.GetFeedAsync();
            var mine = await postsApi.GetMineAsync();
            Posts = feed.Concat(mine).Select(ToPost).GroupBy(p => p.Id).Select(g => g.First()).ToList();
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

    public Post? FindById(int id) => Posts.FirstOrDefault(p => p.Id == id);

    // Fetches the server-authoritative Pages/Keywords/Highlights/DocumentChangeCount for a
    // note and merges them into the matching Post (adding it to Posts if this session hasn't
    // seen it via LoadFeedAsync yet). Called by the reader/brain-map/process pages on open,
    // since those don't come along with the lightweight feed/mine list payloads.
    public async Task<Post?> LoadPostDetailAsync(int postId)
    {
        var dto = await postsApi.GetDetailAsync(postId);
        if (dto is null)
            return null;

        var post = FindById(postId);
        if (post is null)
        {
            post = ToPost(dto);
            Posts.Insert(0, post);
        }

        post.DocumentChangeCount = dto.DocumentChangeCount;
        post.GeneratedPageCount = dto.GeneratedPageCount;
        post.Pages.Clear();
        post.Pages.AddRange(dto.Pages.Select(ToNotePage));
        post.Keywords.Clear();
        post.Keywords.AddRange(dto.Keywords.Select(ToKeyword));
        post.Highlights.Clear();
        post.Highlights.AddRange(dto.Highlights.Select(ToHighlight));
        return post;
    }

    public async Task<bool> AddKeywordAsync(Post post, string text)
    {
        var dto = await postsApi.AddKeywordAsync(post.Id, text);
        if (dto is null)
            return false;

        post.Keywords.Add(ToKeyword(dto));
        return true;
    }

    // Mirrors the server's own remove-or-mark-deleted decision (PostService.RemoveKeywordAsync)
    // so the local list stays in sync without a round-trip re-fetch.
    public async Task<bool> RemoveKeywordAsync(Post post, BrainMapKeyword keyword)
    {
        if (!await postsApi.RemoveKeywordAsync(post.Id, keyword.Id))
            return false;

        if (keyword.Status == KeywordStatus.UserAdded)
            post.Keywords.Remove(keyword);
        else
            keyword.Status = KeywordStatus.AiDeleted;
        return true;
    }

    public async Task<bool> UpdatePageBodyAsync(Post post, int pageNumber, string body)
    {
        if (!await postsApi.UpdatePageBodyAsync(post.Id, pageNumber, body))
            return false;

        var page = post.Pages.FirstOrDefault(p => p.Number == pageNumber);
        if (page is not null)
            page.Body = body;
        return true;
    }

    public async Task<bool> IncrementDocumentChangeAsync(Post post)
    {
        if (!await postsApi.IncrementDocumentChangeAsync(post.Id))
            return false;

        post.DocumentChangeCount++;
        return true;
    }

    // The caller builds the full NoteHighlight up front — with its first AiChatMessage's
    // Answer still empty — and adds it to Post.Highlights itself, synchronously, right as the
    // DOM gets marked, so the bubble appears immediately (in a loading state) rather than
    // after a network round-trip. This calls interactive-chat-agent-synapse for the real
    // answer and mutates that same pending message in place once it arrives.
    public async Task<bool> CreateHighlightAsync(Post post, NoteHighlight highlight)
    {
        var pendingMessage = highlight.Messages.FirstOrDefault();
        if (pendingMessage is null)
            return false;

        var request = new CreateHighlightRequest
        {
            Id = highlight.Id,
            PageNumber = highlight.PageNumber,
            SelectedText = highlight.SelectedText,
            Question = pendingMessage.Question,
        };

        var dto = await postsApi.CreateHighlightAsync(post.Id, request);
        var realAnswer = dto?.Messages.FirstOrDefault()?.Answer;
        if (realAnswer is null)
            return false;

        pendingMessage.Answer = realAnswer;
        return true;
    }

    // Same eager-local-then-persist pattern as CreateHighlightAsync above — the caller has
    // already appended `message` (empty Answer) to the highlight's Messages itself.
    public async Task<bool> AddHighlightMessageAsync(Post post, Guid highlightId, AiChatMessage message)
    {
        var request = new AddHighlightMessageRequest { Question = message.Question };
        var dto = await postsApi.AddHighlightMessageAsync(post.Id, highlightId, request);
        if (dto is null)
            return false;

        message.Answer = dto.Answer;
        return true;
    }

    // Called once the wizard reaches the finished-note view, so the note it just built
    // actually shows up in All Notes right away — not a moment earlier, not a moment later.
    // This only ever *saves* it (IsShared stays false server-side); ShareAsync below is the
    // separate, explicit step that puts it in front of other people.
    public async Task<Post?> CreatePostFromDraftIfNeededAsync()
    {
        if (Draft.IsCreated)
            return Draft.CreatedPostId is int id ? FindById(id) : null;

        var request = new CreatePostRequest
        {
            Title = Draft.Title,
            // No dedicated description step anymore — the Intake step's free-form notes are
            // the closest available substitute for "what this note is about".
            Description = Draft.UserNotes,
            Files =
            [
                new NoteFileDto
                {
                    FileName = $"{Draft.Title}.pdf",
                    FileUrl = "sample-pdfs/sps303-final-notes.pdf",
                    PageCount = Math.Max(Draft.Pages.Count, 1),
                },
            ],
            Pages = Draft.Pages.Select(ToNotePageDto).ToList(),
            Keywords = Draft.Keywords.Select(ToKeywordDto).ToList(),
            DocumentKnowledgeBase = Draft.DocumentKnowledgeBase,
            SynthesizedDocumentMarkdown = Draft.SynthesizedMarkdown,
            FinalGraph = Draft.FinalGraph,
        };

        var dto = await postsApi.CreateAsync(request);
        Post? post = null;
        if (dto is not null)
        {
            post = ToPost(dto);
            post.Keywords.AddRange(Draft.Keywords);
            post.Pages.AddRange(Draft.Pages);
            Posts.Insert(0, post);
        }

        Draft.IsCreated = true;
        Draft.CreatedPostId = post?.Id;
        return post;
    }

    // The explicit "Share on your profile" action — makes an already-saved note visible in
    // the public feed to everyone else, not just its author.
    public async Task<bool> ShareAsync(Post post)
    {
        if (post.IsShared)
            return true;

        if (!await postsApi.ShareAsync(post.Id))
            return false;

        post.IsShared = true;
        return true;
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
        IsShared = dto.IsShared,
        AuthorId = dto.AuthorId,
        AuthorName = dto.AuthorName,
        AuthorRole = dto.AuthorRole,
        GroupId = dto.GroupId,
        GroupName = dto.GroupName,
        Sends = dto.Sends,
        Downloads = dto.Downloads,
        Files = dto.Files.Select(f => new NoteFile { FileName = f.FileName, FileUrl = f.FileUrl, PageCount = f.PageCount }).ToList(),
        GeneratedPageCount = dto.GeneratedPageCount,
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

    private static NotePage ToNotePage(NotePageDto dto) => new()
    {
        Number = dto.Number,
        Heading = dto.Heading,
        Body = dto.Body,
    };

    private static BrainMapKeyword ToKeyword(BrainMapKeywordDto dto) => new()
    {
        Id = dto.Id,
        Text = dto.Text,
        Count = dto.Count,
        Status = Enum.Parse<KeywordStatus>(dto.Status),
    };

    private static NotePageDto ToNotePageDto(NotePage page) => new()
    {
        Number = page.Number,
        Heading = page.Heading,
        Body = page.Body,
    };

    private static BrainMapKeywordDto ToKeywordDto(BrainMapKeyword keyword) => new()
    {
        Id = keyword.Id,
        Text = keyword.Text,
        Count = keyword.Count,
        Status = keyword.Status.ToString(),
    };

    private static NoteHighlight ToHighlight(NoteHighlightDto dto)
    {
        var highlight = new NoteHighlight
        {
            Id = dto.Id,
            PageNumber = dto.PageNumber,
            SelectedText = dto.SelectedText,
        };
        highlight.Messages.AddRange(dto.Messages.Select(ToMessage));
        return highlight;
    }

    private static AiChatMessage ToMessage(AiChatMessageDto dto) => new()
    {
        Question = dto.Question,
        Answer = dto.Answer,
        CreatedAt = dto.CreatedAt,
    };
}
