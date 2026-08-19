namespace Server.Dtos;

public class NoteFileDto
{
    public required string FileName { get; set; }
    public required string FileUrl { get; set; }
    public int PageCount { get; set; } = 1;
}

public class PostDto
{
    public required int Id { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = "";
    public string MiniDescription { get; set; } = "";
    public required DateOnly CreatedDate { get; set; }
    public bool IsShared { get; set; }

    // Which of Brain Map/Process the author has left visible to other viewers — see
    // Post.ShareBrainMap. Always true for the author's own view; NoteReaderPage/PostCard gate
    // on (IsAuthor || this flag) per button. Map has no such flag — always visible.
    public bool ShareBrainMap { get; set; } = true;
    public bool ShareProcess { get; set; } = true;

    // The generated note's first page — enough for the home feed's inline preview (PostCard)
    // without dragging the whole Pages list along on every list response; null for a post
    // whose note hasn't finished generating yet.
    public NotePageDto? FirstPage { get; set; }

    public required int AuthorId { get; set; }
    public required string AuthorName { get; set; }
    public string AuthorRole { get; set; } = "";

    public int? GroupId { get; set; }
    public string? GroupName { get; set; }

    public int Sends { get; set; }
    public int Downloads { get; set; }

    public List<NoteFileDto> Files { get; set; } = [];

    // The generated document's page count — kept separate from the full Pages list (which
    // only PostDetailDto carries) so list views (feed/mine) can show an accurate "N Pages"
    // without paying for every page's full body on every card.
    public int GeneratedPageCount { get; set; }

    public int LikeCount { get; set; }
    public int FavoriteCount { get; set; }
    public int RepostCount { get; set; }
    public int CommentCount { get; set; }

    public bool LikedByMe { get; set; }
    public bool FavoritedByMe { get; set; }
    public bool RepostedByMe { get; set; }
    public bool FollowedByMe { get; set; }

    public List<PostCommentDto> Comments { get; set; } = [];
}

public class PostCommentDto
{
    public required int Id { get; set; }
    public required int AuthorId { get; set; }
    public required string AuthorName { get; set; }
    public required string Text { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotePageDto
{
    public required int Number { get; set; }
    public string? Heading { get; set; }
    public string Body { get; set; } = "";
}

// Status is a string ("AiKept" / "AiDeleted" / "UserAdded"), same convention as UserDto.Role.
public class BrainMapKeywordDto
{
    public required int Id { get; set; }
    public required string Text { get; set; }
    public int? Count { get; set; }
    public required string Status { get; set; }
}

public class AiChatMessageDto
{
    public required int Id { get; set; }
    public required string Question { get; set; }
    public required string Answer { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool AddedToDocument { get; set; }
}

public class NoteHighlightDto
{
    public required Guid Id { get; set; }
    public required int PageNumber { get; set; }
    public required string SelectedText { get; set; }
    public bool TargetsHeading { get; set; }
    public List<AiChatMessageDto> Messages { get; set; } = [];
}

// Everything PostDto has, plus the generated note content — kept as a separate, heavier DTO
// so feed/mine list responses (which can be many posts) don't drag full page bodies along.
public class PostDetailDto : PostDto
{
    public int DocumentChangeCount { get; set; }
    public List<NotePageDto> Pages { get; set; } = [];
    public List<BrainMapKeywordDto> Keywords { get; set; } = [];
    public List<NoteHighlightDto> Highlights { get; set; } = [];
}

public class CreatePostRequest
{
    public required string Title { get; set; }
    public string Description { get; set; } = "";
    public string MiniDescription { get; set; } = "";
    public required List<NoteFileDto> Files { get; set; }

    // Already fully generated (by the real AI pipeline — see AiController/NoteCreationAiService)
    // by the time the note is saved — sent along with creation instead of round-tripping page
    // by page.
    public List<NotePageDto> Pages { get; set; } = [];
    public List<BrainMapKeywordDto> Keywords { get; set; } = [];

    // document-rag-agent-synapse's raw structured output, carried through the wizard in
    // NoteDraft — persisted here so the Map view can (re)generate its graph later without
    // needing the original uploaded files again.
    public string? DocumentKnowledgeBase { get; set; }

    // topic-synthesizer-agent-synapse's raw Markdown — see Post.SynthesizedDocumentMarkdown.
    public string? SynthesizedDocumentMarkdown { get; set; }

    // brain-map-agent-synapse's final-mode graph, already generated by the wizard's
    // Processing step — persisted straight into Post.GraphJson so the Map page shows it
    // immediately instead of lazily generating on first visit.
    public GraphDto? FinalGraph { get; set; }
}

public class AddKeywordRequest
{
    public required string Text { get; set; }
}

public class UpdatePostTitleRequest
{
    public required string Title { get; set; }
}

public class UpdatePostDescriptionRequest
{
    public required string Description { get; set; }
}

public class UpdateShareSettingsRequest
{
    public bool ShareBrainMap { get; set; } = true;
    public bool ShareProcess { get; set; } = true;
}

public class UpdatePageBodyRequest
{
    public required string Body { get; set; }
}

public class UpdatePageHeadingRequest
{
    public required string Heading { get; set; }
}

// No Answer field: the mock used to have the client compute it, but the real
// interactive-chat-agent-synapse answer can only be produced server-side (see
// PostService.CreateHighlightAsync).
public class CreateHighlightRequest
{
    public required Guid Id { get; set; }
    public required int PageNumber { get; set; }
    public required string SelectedText { get; set; }
    public required string Question { get; set; }
    public bool TargetsHeading { get; set; }
}

public class AddHighlightMessageRequest
{
    public required string Question { get; set; }
}

public class UpdatePostGroupRequest
{
    public int? GroupId { get; set; }
}

public class AddCommentRequest
{
    public required string Text { get; set; }
}

public class ReactionResponse
{
    public bool Active { get; set; }
    public int Count { get; set; }
}
