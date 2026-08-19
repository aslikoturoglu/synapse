using System.Net;
using System.Text.Json;
using Client.Pages.CreateNote;
using Client.Services;
using Microsoft.JSInterop;

namespace Client.Pages.Home;

// Holds session-wide UI state. Posts now come from the real backend (via PostsApiClient);
// Groups/Profile/Settings are still mock-only — kept here for the session the same way Posts
// used to be, until those get their own backend wiring.
public class NotesStore(AuthState authState, PostsApiClient postsApi, ConnectionsApiClient connectionsApi, GroupsApiClient groupsApi, ProfileApiClient profileApi, UsersApiClient usersApi, IJSRuntime js)
{
    private const string ActiveDraftStorageKey = "synapse_active_draft";
    private const string ArchivedDraftsStorageKey = "synapse_archived_drafts";

    public List<Post> Posts { get; private set; } = [];

    public List<GroupDto> Groups { get; private set; } = [];

    public UserProfile Profile { get; } = new();

    public UserSettings Settings { get; } = new();

    public NoteDraft Draft { get; private set; } = new();

    // Other in-progress notes the user stepped away from without finishing — see
    // StartFreshDraftAsync/SwapActiveDraftAsync. Excludes anything already IsCreated (that's
    // just a finished note now, not an "unfinished project").
    public List<NoteDraft> ArchivedDrafts { get; private set; } = [];

    public List<NoteDraft> AllUnfinishedDrafts =>
        (Draft.HasProgress && !Draft.IsCreated ? [Draft] : Enumerable.Empty<NoteDraft>())
            .Concat(ArchivedDrafts)
            .OrderByDescending(d => d.UpdatedAt)
            .ToList();

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

    // Restores whatever wizard state survived a reload — called once at boot (see Program.cs),
    // after AuthState so this only runs for a signed-in session.
    public async Task LoadDraftStateAsync()
    {
        var activeJson = await js.InvokeAsync<string?>("draftStorage.getItem", ActiveDraftStorageKey);
        if (!string.IsNullOrEmpty(activeJson) && JsonSerializer.Deserialize<NoteDraft>(activeJson) is { } restored)
            Draft = restored;

        var archivedJson = await js.InvokeAsync<string?>("draftStorage.getItem", ArchivedDraftsStorageKey);
        if (!string.IsNullOrEmpty(archivedJson))
            ArchivedDrafts = JsonSerializer.Deserialize<List<NoteDraft>>(archivedJson) ?? [];
    }

    // Call after every step transition (see CreateNote.razor's GoTo) so the active draft never
    // falls more than one step behind what's actually in localStorage.
    public async Task SaveDraftStateAsync()
    {
        if (Draft.HasProgress && !Draft.IsCreated)
        {
            Draft.UpdatedAt = DateTime.UtcNow;
            await js.InvokeVoidAsync("draftStorage.setItem", ActiveDraftStorageKey, JsonSerializer.Serialize(Draft));
        }
        else
        {
            await js.InvokeVoidAsync("draftStorage.removeItem", ActiveDraftStorageKey);
        }
    }

    // Archives the current draft (if it's genuinely unfinished work, not just a blank slate)
    // and swaps in a brand new one — used when the user wants to start a different note
    // without losing track of the one they were already partway through.
    public async Task StartFreshDraftAsync()
    {
        if (Draft.HasProgress && !Draft.IsCreated)
            ArchivedDrafts.Insert(0, Draft);

        Draft = new NoteDraft();
        await PersistArchivedDraftsAsync();
        await js.InvokeVoidAsync("draftStorage.removeItem", ActiveDraftStorageKey);
    }

    // Makes an archived draft the active one again, archiving whatever was active in its place
    // (same reasoning as StartFreshDraftAsync — nothing unfinished just gets silently dropped).
    public async Task SwapActiveDraftAsync(Guid targetId)
    {
        if (Draft.Id == targetId)
            return;

        var target = ArchivedDrafts.FirstOrDefault(d => d.Id == targetId);
        if (target is null)
            return;

        ArchivedDrafts.Remove(target);
        if (Draft.HasProgress && !Draft.IsCreated)
            ArchivedDrafts.Insert(0, Draft);

        Draft = target;
        await PersistArchivedDraftsAsync();
        await SaveDraftStateAsync();
    }

    public async Task DiscardDraftAsync(Guid id)
    {
        if (Draft.Id == id)
        {
            Draft = new NoteDraft();
            await js.InvokeVoidAsync("draftStorage.removeItem", ActiveDraftStorageKey);
            return;
        }

        ArchivedDrafts.RemoveAll(d => d.Id == id);
        await PersistArchivedDraftsAsync();
    }

    private Task PersistArchivedDraftsAsync() =>
        js.InvokeVoidAsync("draftStorage.setItem", ArchivedDraftsStorageKey, JsonSerializer.Serialize(ArchivedDrafts)).AsTask();

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

    // Swallows failures the same way LoadFeedAsync does — an empty Groups list just means the
    // "Add to Group" picker offers nothing yet, not a crash.
    public async Task LoadGroupsAsync()
    {
        try
        {
            Groups = await groupsApi.GetMineAsync();
        }
        catch (HttpRequestException ex) when (ex.StatusCode != HttpStatusCode.Unauthorized)
        {
        }
    }

    public async Task<string?> CreateGroupAsync(string name)
    {
        var (group, error) = await groupsApi.CreateAsync(name);
        if (group is not null)
            Groups.Add(group);

        return error;
    }

    // Server-enforced: only the caller's own posts can move, and only into a group the caller
    // themselves owns (PostService.SetGroupAsync) — this just mirrors the result back locally.
    public async Task<bool> SetPostGroupAsync(Post post, int? groupId)
    {
        if (!await postsApi.SetGroupAsync(post.Id, groupId))
            return false;

        post.GroupId = groupId;
        post.GroupName = groupId is int id ? Groups.FirstOrDefault(g => g.Id == id)?.Name : null;
        return true;
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

    public async Task<bool> UpdateTitleAsync(Post post, string title)
    {
        if (!await postsApi.UpdateTitleAsync(post.Id, title))
            return false;

        post.Title = title;
        return true;
    }

    public async Task<bool> UpdateDescriptionAsync(Post post, string description)
    {
        if (!await postsApi.UpdateDescriptionAsync(post.Id, description))
            return false;

        post.Description = description;
        return true;
    }

    // Set right before the actual ShareAsync call — see ShareSettingsModal.
    public async Task<bool> UpdateShareSettingsAsync(Post post, bool showBrainMap, bool showProcess)
    {
        if (!await postsApi.UpdateShareSettingsAsync(post.Id, showBrainMap, showProcess))
            return false;

        post.ShareBrainMap = showBrainMap;
        post.ShareProcess = showProcess;
        return true;
    }

    public async Task<bool> UpdatePageHeadingAsync(Post post, int pageNumber, string heading)
    {
        if (!await postsApi.UpdatePageHeadingAsync(post.Id, pageNumber, heading))
            return false;

        var page = post.Pages.FirstOrDefault(p => p.Number == pageNumber);
        if (page is not null)
            page.Heading = heading;
        return true;
    }

    public async Task<bool> IncrementDocumentChangeAsync(Post post)
    {
        if (!await postsApi.IncrementDocumentChangeAsync(post.Id))
            return false;

        post.DocumentChangeCount++;
        return true;
    }

    // Same "Add to Document" event as IncrementDocumentChangeAsync above, but also tags the
    // specific message that produced the edit, so Process view can group/tag it distinctly.
    public async Task<bool> MarkAddedToDocumentAsync(Post post, Guid highlightId, int messageId)
    {
        if (!await postsApi.MarkAddedToDocumentAsync(post.Id, highlightId, messageId))
            return false;

        post.DocumentChangeCount++;
        var message = post.Highlights.FirstOrDefault(h => h.Id == highlightId)?.Messages.FirstOrDefault(m => m.Id == messageId);
        if (message is not null)
            message.AddedToDocument = true;
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
            TargetsHeading = highlight.TargetsHeading,
        };

        var dto = await postsApi.CreateHighlightAsync(post.Id, request);
        var realMessage = dto?.Messages.FirstOrDefault();
        if (realMessage is null)
            return false;

        pendingMessage.Id = realMessage.Id;
        pendingMessage.Answer = realMessage.Answer;
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

        message.Id = dto.Id;
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
            // One entry per file actually picked in the wizard's Upload step — not the note's
            // own generated title/page count, which described the AI-written note, not what was
            // uploaded. FileUrl still points at the shared sample PDF: the real bytes are only
            // ever sent to Azure AI Foundry (AiApiClient.StartAsync) and never persisted
            // server-side, so there's no real per-file URL to link to yet.
            Files = Draft.Files.Select(f => new NoteFileDto
            {
                FileName = f.Name,
                FileUrl = "sample-pdfs/sps303-final-notes.pdf",
                PageCount = 1,
            }).ToList(),
            Pages = Draft.Pages.Select(ToNotePageDto).ToList(),
            Keywords = Draft.Keywords.Select(ToKeywordDto).ToList(),
            DocumentKnowledgeBase = Draft.DocumentKnowledgeBase,
            SynthesizedDocumentMarkdown = Draft.SynthesizedMarkdown,
            FinalGraph = Draft.FinalGraph,
        };

        var dto = await postsApi.CreateAsync(request);
        // Leave Draft.IsCreated false on failure (network blip, expired session, server error,
        // etc.) so the caller can retry — previously this was set unconditionally, which
        // silently marked the draft "done" with no post behind it AND wiped its localStorage
        // backup below, permanently losing the note with no error and no way to retry.
        if (dto is null)
            return null;

        var post = ToPost(dto);
        post.Keywords.AddRange(Draft.Keywords);
        post.Pages.AddRange(Draft.Pages);
        Posts.Insert(0, post);

        Draft.IsCreated = true;
        Draft.CreatedPostId = post.Id;
        // Now a real, saved Post — no longer "unfinished", so the persisted copy of it as an
        // in-progress draft has to go too, or a later reload would restore it and show a
        // resume banner for a note that's actually already done.
        await SaveDraftStateAsync();
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

    // Pulls a post back out of the public feed — likes/comments/CreatedDate are left alone
    // server-side, so re-sharing later just picks the same post back up.
    public async Task<bool> UnshareAsync(Post post)
    {
        if (!post.IsShared)
            return true;

        if (!await postsApi.UnshareAsync(post.Id))
            return false;

        post.IsShared = false;
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

    // Following is per-author, not per-post — every post by that author currently loaded
    // needs the same IsFollowing state, not just the card the button was clicked on.
    public async Task ToggleFollowAsync(Post post)
    {
        var following = await ToggleFollowByUserIdAsync(post.AuthorId);
        if (following is not null)
            post.IsFollowing = following.Value;
    }

    // Same toggle, usable from a profile header where there's no Post to hang it off of —
    // still fans the new state out to every already-loaded post by that author.
    public async Task<bool?> ToggleFollowByUserIdAsync(int userId)
    {
        var following = await connectionsApi.ToggleFollowAsync(userId);
        if (following is null)
            return null;

        foreach (var p in Posts.Where(p => p.AuthorId == userId))
            p.IsFollowing = following.Value;

        return following;
    }

    public async Task<List<Post>> GetRepostsByAuthorAsync(string fullName) =>
        (await profileApi.GetRepostsAsync(fullName)).Select(ToPost).ToList();

    // Prefer this over the name-based overload above whenever the caller already knows the
    // id — display name isn't unique, so the name-based lookup can resolve to the wrong account.
    public async Task<List<Post>> GetRepostsByAuthorIdAsync(int userId) =>
        (await profileApi.GetRepostsByIdAsync(userId)).Select(ToPost).ToList();

    // Admin-only: a target user's full note collection (shared and unshared, mirroring what
    // they see on their own All Notes page) and the groups they've organized them into — see
    // UsersController.GetNotes/GetGroups.
    public async Task<List<Post>> GetNotesByAuthorIdAsync(int userId) =>
        (await usersApi.GetNotesAsync(userId)).Select(ToPost).ToList();

    public async Task<List<GroupDto>> GetGroupsByAuthorIdAsync(int userId) =>
        await usersApi.GetGroupsAsync(userId);

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
        ShareBrainMap = dto.ShareBrainMap,
        ShareProcess = dto.ShareProcess,
        FirstPage = dto.FirstPage is null ? null : ToNotePage(dto.FirstPage),
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
        IsFollowing = dto.FollowedByMe,
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
            TargetsHeading = dto.TargetsHeading,
        };
        highlight.Messages.AddRange(dto.Messages.Select(ToMessage));
        return highlight;
    }

    private static AiChatMessage ToMessage(AiChatMessageDto dto) => new()
    {
        Id = dto.Id,
        Question = dto.Question,
        Answer = dto.Answer,
        CreatedAt = dto.CreatedAt,
        AddedToDocument = dto.AddedToDocument,
    };
}
