namespace Client.Services;

public class NoteFileDto
{
    public required string FileName { get; set; }
    public required string FileUrl { get; set; }
    public int PageCount { get; set; } = 1;
}

public class PostCommentDto
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class PostDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string MiniDescription { get; set; } = "";
    public DateOnly CreatedDate { get; set; }
    public bool IsShared { get; set; }

    public bool ShareBrainMap { get; set; } = true;
    public bool ShareProcess { get; set; } = true;

    public NotePageDto? FirstPage { get; set; }

    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = "";
    public string AuthorRole { get; set; } = "";

    public int? GroupId { get; set; }
    public string? GroupName { get; set; }

    public int Sends { get; set; }
    public int Downloads { get; set; }

    public List<NoteFileDto> Files { get; set; } = [];

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

public class NotePageDto
{
    public int Number { get; set; }
    public string? Heading { get; set; }
    public string Body { get; set; } = "";
}

// Status is a string ("AiKept" / "AiDeleted" / "UserAdded"), same convention the server
// already uses for UserDto.Role.
public class BrainMapKeywordDto
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public int? Count { get; set; }
    public string Status { get; set; } = "AiKept";
}

public class AiChatMessageDto
{
    public int Id { get; set; }
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool AddedToDocument { get; set; }
}

public class NoteHighlightDto
{
    public Guid Id { get; set; }
    public int PageNumber { get; set; }
    public string SelectedText { get; set; } = "";
    public bool TargetsHeading { get; set; }
    public List<AiChatMessageDto> Messages { get; set; } = [];
}

public class PostDetailDto : PostDto
{
    public int DocumentChangeCount { get; set; }
    public List<NotePageDto> Pages { get; set; } = [];
    public List<BrainMapKeywordDto> Keywords { get; set; } = [];
    public List<NoteHighlightDto> Highlights { get; set; } = [];
    public string PersonalNotes { get; set; } = "";
}

public class CreatePostRequest
{
    public required string Title { get; set; }
    public string Description { get; set; } = "";
    public string MiniDescription { get; set; } = "";
    public required List<NoteFileDto> Files { get; set; }
    public List<NotePageDto> Pages { get; set; } = [];
    public List<BrainMapKeywordDto> Keywords { get; set; } = [];
    public string? DocumentKnowledgeBase { get; set; }
    public string? SynthesizedDocumentMarkdown { get; set; }
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

public class UpdatePersonalNotesRequest
{
    public required string Notes { get; set; }
}

public class UpdatePageHeadingRequest
{
    public required string Heading { get; set; }
}

// No Answer field — the real answer only exists once the server has actually called
// interactive-chat-agent-synapse (see NotesStore.CreateHighlightAsync).
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

public class GraphNodeDto
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string Type { get; set; } = "concept"; // "root" | "topic" | "concept"
    public int? Frequency { get; set; }
}

public class GraphEdgeDto
{
    public string Source { get; set; } = "";
    public string Target { get; set; } = "";
    public string Label { get; set; } = "";
}

public class GraphDto
{
    public string Mode { get; set; } = "final"; // "draft" | "final"
    public List<GraphNodeDto> Nodes { get; set; } = [];
    public List<GraphEdgeDto> Edges { get; set; } = [];
}

public class AddCommentRequest
{
    public required string Text { get; set; }
}

public class UpdatePostGroupRequest
{
    public int? GroupId { get; set; }
}

public class ReactionResponse
{
    public bool Active { get; set; }
    public int Count { get; set; }
}

public class SendNoteEmailRequest
{
    public required string To { get; set; }
    public string? Message { get; set; }
}
