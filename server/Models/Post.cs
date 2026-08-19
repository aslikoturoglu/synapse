namespace Server.Models;

public class Post
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public string Description { get; set; } = "";
    public string MiniDescription { get; set; } = "";

    public DateOnly CreatedDate { get; set; }

    // CreatedDate (above) is day-only and drives the Notes list's date grouping — this is the
    // precise timestamp Dashboard's time-range filters (e.g. "Last 6 Hours") actually need.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // A newly-created post is saved for its author (shows up in their All Notes) but stays
    // invisible to everyone else until they explicitly share it — at which point it starts
    // appearing in the public feed too.
    public bool IsShared { get; set; }

    // Set (defaulting to true) in the share confirmation popup — whether a non-author viewer
    // is allowed to open that review page at all. The author can always reach both regardless
    // of these flags; see PostService.UpdateShareSettingsAsync. Map has no such flag — every
    // viewer of a shared post can always open it.
    public bool ShareBrainMap { get; set; } = true;
    public bool ShareProcess { get; set; } = true;

    public int? GroupId { get; set; }
    public Group? Group { get; set; }

    public int Sends { get; set; }
    public int Downloads { get; set; }

    public List<NoteFile> Files { get; } = [];
    public List<PostComment> Comments { get; } = [];

    public List<User> LikedByUsers { get; } = [];
    public List<User> FavoritedByUsers { get; } = [];
    public List<User> RepostedByUsers { get; } = [];

    // The generated note itself (Brain Map keywords, finished pages) plus what readers did
    // with it (highlights + AI chats, manual edit count). See NotePage/BrainMapKeyword/
    // NoteHighlight for why Keywords/Pages are shared across every viewer but Highlights
    // aren't.
    public List<NotePage> Pages { get; } = [];
    public List<BrainMapKeyword> Keywords { get; } = [];
    public List<NoteHighlight> Highlights { get; } = [];
    public int DocumentChangeCount { get; set; }

    // document-rag-agent's raw structured knowledge-base output, captured once at creation
    // time. Reused later (without re-reading the original files) whenever the Map view needs
    // to (re)generate its node/edge graph via brain-map-agent.
    public string? DocumentKnowledgeBase { get; set; }

    // topic-synthesizer-agent's raw Markdown output, captured once at creation time —
    // MarkdownPager only keeps the HTML-converted Pages, but brain-map-agent's final mode
    // needs the actual finalized document as its primary grounding source (per its own
    // spec), not a re-derived approximation. Null for posts created before this field
    // existed; RegenerateMapAsync falls back to Pages text for those.
    public string? SynthesizedDocumentMarkdown { get; set; }

    // Cached brain-map-agent output (serialized {nodes, edges}) so opening Map doesn't call
    // the agent again on every visit — only on first generation or an explicit Regenerate.
    public string? GraphJson { get; set; }
}
