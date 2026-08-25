namespace Client.Pages.Home;

public class PostComment
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public required string AuthorName { get; set; }
    public required string Text { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NoteFile
{
    public required string FileName { get; init; }
    public required string FileUrl { get; init; }
    public int PageCount { get; init; } = 1;
}

public class Post
{
    public int Id { get; set; }

    public required string Title { get; set; }
    public string Description { get; set; } = "";
    public string MiniDescription { get; set; } = "";
    public DateOnly CreatedDate { get; set; }

    // Saved (created) as soon as the wizard reaches the finished-note view — shows up in the
    // author's own All Notes right away. Only becomes visible in the public feed, to other
    // users, once explicitly shared (NotesStore.ShareAsync).
    public bool IsShared { get; set; }

    // Which of Brain Map/Process the author left visible to other viewers, set from the share
    // confirmation popup (ShareSettingsModal). Always true for the author's own view — pages
    // gate on (IsAuthor || this flag) per button. Map has no such flag — always visible.
    public bool ShareBrainMap { get; set; } = true;
    public bool ShareProcess { get; set; } = true;

    // The generated note's first page — enough for the home feed's inline preview (PostCard)
    // without a full LoadPostDetailAsync round trip. Null until the note's finished generating.
    public NotePage? FirstPage { get; set; }

    public int AuthorId { get; set; }
    public required string AuthorName { get; set; }
    public string AuthorRole { get; set; } = "";

    public int? GroupId { get; set; }
    public string? GroupName { get; set; }

    public int Sends { get; set; }
    public int Downloads { get; set; }

    public required List<NoteFile> Files { get; set; }
    public List<PostComment> Comments { get; set; } = [];

    public int LikeCount { get; set; }
    public int FavoriteCount { get; set; }
    public int RepostCount { get; set; }

    public bool LikedByMe { get; set; }
    public bool FavoritedByMe { get; set; }
    public bool RepostedByMe { get; set; }

    // The AI-generated note itself: brain-map keywords (with their AI/user/deleted history),
    // the finished pages, and what the user did while reading them (highlights + AI chats,
    // manual edits). Server-backed (NotesStore.LoadPostDetailAsync) — empty here until that's
    // been called, since the lightweight feed/mine list payloads don't include it.
    public List<BrainMapKeyword> Keywords { get; init; } = [];
    public List<NotePage> Pages { get; init; } = [];
    public List<NoteHighlight> Highlights { get; init; } = [];
    public int DocumentChangeCount { get; set; }

    // The author's own free-form scratchpad — always "" for a non-author viewer, same
    // server-backed/empty-until-LoadPostDetailAsync caveat as Pages/Keywords/Highlights above.
    public string PersonalNotes { get; set; } = "";

    // The real, server-known page count — always accurate, unlike Pages.Count which is 0
    // until LoadPostDetailAsync has actually been called for this post this session.
    public int GeneratedPageCount { get; set; }

    public bool IsExpanded { get; set; }
    public bool IsFollowing { get; set; }
    public bool ShowFiles { get; set; }
    public bool ShowGeneratedPreview { get; set; }

    // Home feed (PostCard): whether the inline first-page + Note/Brain Map/Process/Map preview
    // is showing in place of the page thumbnails.
    public bool ShowNotePreview { get; set; }

    public int FileCount => Files.Count;
    public int TotalPageCount => Files.Sum(f => f.PageCount);
    public int TotalHighlightCount => Highlights.Count;
    public int TotalAiChatCount => Highlights.Sum(h => h.Messages.Count);
}
