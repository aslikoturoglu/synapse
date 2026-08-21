using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Server.Data;
using Server.Dtos;
using Server.Models;
using Server.Services.AiFoundry;

namespace Server.Services;

public enum PostOpResult { Success, NotFound, Forbidden, GroupNotFound }

public class PostService(AppDbContext db, NoteChatAiService chatAiService, NoteMapAiService mapAiService, EmailService email)
{
    // The public feed only ever shows posts their author has chosen to share — a freshly
    // created post is visible to its author (see GetMineAsync) but nobody else until then.
    public async Task<List<PostDto>> GetFeedAsync(int userId) =>
        await QueryDto(userId).Where(p => p.IsShared)
            .OrderByDescending(p => p.CreatedDate).ThenByDescending(p => p.Id)
            .ToListAsync();

    public async Task<List<PostDto>> GetMineAsync(int userId) =>
        await QueryDto(userId).Where(p => p.AuthorId == userId)
            .OrderByDescending(p => p.CreatedDate).ThenByDescending(p => p.Id)
            .ToListAsync();

    // Admin-only: same shape as GetMineAsync, but for a target author distinct from the
    // viewer — lets an admin see another user's full note collection (shared and unshared),
    // mirroring what that user sees on their own All Notes page. Gated to Admin by
    // UsersController's class-level [Authorize(Roles = "Admin")], not here.
    public async Task<List<PostDto>> GetAllByAuthorIdAsync(int viewerId, int authorId) =>
        await QueryDto(viewerId).Where(p => p.AuthorId == authorId)
            .OrderByDescending(p => p.CreatedDate).ThenByDescending(p => p.Id)
            .ToListAsync();

    // Shared posts a given user has reposted — for the "Reposts" tab on their public profile,
    // not to be confused with GetMineAsync (posts they authored). RepostedByUsers isn't
    // projectable straight into QueryDto's PostDto shape, so the matching ids are resolved
    // first and fed back in as a filter.
    public async Task<List<PostDto>> GetRepostsByUserAsync(int viewerId, int targetUserId)
    {
        var repostedIds = await db.Posts
            .Where(p => p.RepostedByUsers.Any(u => u.Id == targetUserId))
            .Select(p => p.Id)
            .ToListAsync();

        return await QueryDto(viewerId)
            .Where(p => p.IsShared && repostedIds.Contains(p.Id))
            .OrderByDescending(p => p.CreatedDate).ThenByDescending(p => p.Id)
            .ToListAsync();
    }

    public async Task<PostDto?> CreateAsync(int userId, CreatePostRequest request)
    {
        var author = await db.Users.FindAsync(userId);
        if (author is null)
            return null;

        var post = new Post
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            MiniDescription = request.MiniDescription.Trim(),
            CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            AuthorId = userId,
            DocumentKnowledgeBase = request.DocumentKnowledgeBase,
            SynthesizedDocumentMarkdown = request.SynthesizedDocumentMarkdown,
            GraphJson = request.FinalGraph is null ? null : System.Text.Json.JsonSerializer.Serialize(request.FinalGraph),
        };

        foreach (var file in request.Files)
            post.Files.Add(new NoteFile { FileName = file.FileName, FileUrl = file.FileUrl, PageCount = file.PageCount });

        // Already fully generated (by the real AI pipeline) by the time the wizard reaches
        // this point — persisted as-is, not regenerated server-side.
        foreach (var page in request.Pages)
            post.Pages.Add(new Models.NotePage { Number = page.Number, Heading = page.Heading, Body = page.Body });

        foreach (var keyword in request.Keywords)
        {
            post.Keywords.Add(new BrainMapKeyword
            {
                Text = keyword.Text,
                Count = keyword.Count,
                Status = Enum.Parse<KeywordStatus>(keyword.Status),
            });
        }

        db.Posts.Add(post);
        await db.SaveChangesAsync();

        await email.SendAsync(author.Email, "Your note is ready",
            EmailService.Paragraphs(
                $"Hi {EmailService.Encode(author.Name)},",
                $"Your note \"<strong>{EmailService.Encode(post.Title)}</strong>\" has finished processing and is ready to view in Synapse."));

        return ToDto(post, author);
    }

    // Any signed-in viewer can read a shared post's full content; only the author can read
    // their own not-yet-shared draft. Highlights shown are always the post's author's — the
    // client only lets the author create highlights (NotePagesViewer's toolbar hides Ask AI
    // from everyone else), so a non-author has no highlights of their own to show anyway, and
    // filtering by the viewer's id here made Share Process show nothing to anyone but the
    // author, defeating its purpose.
    public async Task<PostDetailDto?> GetDetailAsync(int userId, int postId)
    {
        var post = await db.Posts
            .Include(p => p.Pages)
            .Include(p => p.Keywords)
            .Include(p => p.Highlights).ThenInclude(h => h.Messages)
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post is null || (!post.IsShared && post.AuthorId != userId))
            return null;

        var dto = ToDetailDto(await QueryDto(userId).FirstAsync(p => p.Id == postId));
        dto.DocumentChangeCount = post.DocumentChangeCount;
        dto.Pages = post.Pages.OrderBy(p => p.Number)
            .Select(p => new NotePageDto { Number = p.Number, Heading = p.Heading, Body = p.Body })
            .ToList();
        dto.Keywords = post.Keywords
            .Select(k => new BrainMapKeywordDto { Id = k.Id, Text = k.Text, Count = k.Count, Status = k.Status.ToString() })
            .ToList();
        dto.Highlights = post.Highlights.Where(h => h.UserId == post.AuthorId)
            .Select(h => new NoteHighlightDto
            {
                Id = h.Id,
                PageNumber = h.PageNumber,
                SelectedText = h.SelectedText,
                TargetsHeading = h.TargetsHeading,
                Messages = h.Messages.OrderBy(m => m.CreatedAt)
                    .Select(m => new AiChatMessageDto { Id = m.Id, Question = m.Question, Answer = m.Answer, CreatedAt = m.CreatedAt, AddedToDocument = m.AddedToDocument })
                    .ToList(),
            })
            .ToList();

        return dto;
    }

    public async Task<BrainMapKeywordDto?> AddKeywordAsync(int userId, int postId, string text)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null || post.AuthorId != userId)
            return null;

        var keyword = new BrainMapKeyword { PostId = postId, Text = text.Trim(), Status = KeywordStatus.UserAdded };
        db.BrainMapKeywords.Add(keyword);
        await db.SaveChangesAsync();

        return new BrainMapKeywordDto { Id = keyword.Id, Text = keyword.Text, Count = keyword.Count, Status = keyword.Status.ToString() };
    }

    // AI-originated keywords keep their history (shown red in the Brain Map history view) —
    // ones the user typed in themselves have no AI history worth keeping, so they're just
    // removed outright.
    public async Task<PostOpResult> RemoveKeywordAsync(int userId, int postId, int keywordId)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null)
            return PostOpResult.NotFound;
        if (post.AuthorId != userId)
            return PostOpResult.Forbidden;

        var keyword = await db.BrainMapKeywords.FirstOrDefaultAsync(k => k.Id == keywordId && k.PostId == postId);
        if (keyword is null)
            return PostOpResult.NotFound;

        if (keyword.Status == KeywordStatus.UserAdded)
            db.BrainMapKeywords.Remove(keyword);
        else
            keyword.Status = KeywordStatus.AiDeleted;

        await db.SaveChangesAsync();
        return PostOpResult.Success;
    }

    public async Task<PostOpResult> UpdatePageBodyAsync(int userId, int postId, int pageNumber, string body)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null)
            return PostOpResult.NotFound;
        if (post.AuthorId != userId)
            return PostOpResult.Forbidden;

        var page = await db.NotePages.FirstOrDefaultAsync(p => p.PostId == postId && p.Number == pageNumber);
        if (page is null)
            return PostOpResult.NotFound;

        page.Body = body;
        await db.SaveChangesAsync();
        return PostOpResult.Success;
    }

    public async Task<PostOpResult> UpdateTitleAsync(int userId, int postId, string title)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null)
            return PostOpResult.NotFound;
        if (post.AuthorId != userId)
            return PostOpResult.Forbidden;

        post.Title = title;
        await db.SaveChangesAsync();
        return PostOpResult.Success;
    }

    public async Task<PostOpResult> UpdateDescriptionAsync(int userId, int postId, string description)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null)
            return PostOpResult.NotFound;
        if (post.AuthorId != userId)
            return PostOpResult.Forbidden;

        post.Description = description;
        await db.SaveChangesAsync();
        return PostOpResult.Success;
    }

    // Set from the share confirmation popup, before the post is actually marked shared — see
    // Post.ShareBrainMap.
    public async Task<PostOpResult> UpdateShareSettingsAsync(int userId, int postId, bool showBrainMap, bool showProcess)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null)
            return PostOpResult.NotFound;
        if (post.AuthorId != userId)
            return PostOpResult.Forbidden;

        post.ShareBrainMap = showBrainMap;
        post.ShareProcess = showProcess;
        await db.SaveChangesAsync();
        return PostOpResult.Success;
    }

    public async Task<PostOpResult> UpdatePageHeadingAsync(int userId, int postId, int pageNumber, string heading)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null)
            return PostOpResult.NotFound;
        if (post.AuthorId != userId)
            return PostOpResult.Forbidden;

        var page = await db.NotePages.FirstOrDefaultAsync(p => p.PostId == postId && p.Number == pageNumber);
        if (page is null)
            return PostOpResult.NotFound;

        page.Heading = heading;
        await db.SaveChangesAsync();
        return PostOpResult.Success;
    }

    public async Task<PostOpResult> IncrementDocumentChangeAsync(int userId, int postId)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null)
            return PostOpResult.NotFound;
        if (post.AuthorId != userId)
            return PostOpResult.Forbidden;

        post.DocumentChangeCount++;
        await db.SaveChangesAsync();
        return PostOpResult.Success;
    }

    // Same "Add to Document" event as IncrementDocumentChangeAsync above, but also tags the
    // specific message that produced the edit — lets the Process view group/tag document
    // changes distinctly instead of lumping every "Add to Document" click into one bare count.
    public async Task<PostOpResult> MarkAddedToDocumentAsync(int userId, int postId, Guid highlightId, int messageId)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null)
            return PostOpResult.NotFound;
        if (post.AuthorId != userId)
            return PostOpResult.Forbidden;

        var message = await db.AiChatMessages.FirstOrDefaultAsync(m => m.Id == messageId && m.HighlightId == highlightId);
        if (message is not null)
            message.AddedToDocument = true;

        post.DocumentChangeCount++;
        await db.SaveChangesAsync();
        return PostOpResult.Success;
    }

    // Any viewer who can see the post (author, or anyone once it's shared) can highlight
    // their own copy of it and ask interactive-chat-agent-synapse about it — see
    // NoteHighlight.UserId. The agent needs a real answer before anything is saved, so this
    // is the first place a highlight's own Foundry thread gets created.
    public async Task<NoteHighlightDto?> CreateHighlightAsync(int userId, int postId, CreateHighlightRequest request)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null || (!post.IsShared && post.AuthorId != userId))
            return null;

        var sessionContext = BuildSessionContext(post, request.SelectedText);
        var (answer, threadId) = await chatAiService.AskAsync(null, sessionContext, request.Question);

        var highlight = new NoteHighlight
        {
            Id = request.Id,
            PostId = postId,
            UserId = userId,
            PageNumber = request.PageNumber,
            SelectedText = request.SelectedText,
            TargetsHeading = request.TargetsHeading,
            AgentThreadId = threadId,
        };
        var message = new AiChatMessage { HighlightId = request.Id, Question = request.Question, Answer = answer };
        highlight.Messages.Add(message);

        db.NoteHighlights.Add(highlight);
        await db.SaveChangesAsync();

        return new NoteHighlightDto
        {
            Id = highlight.Id,
            PageNumber = highlight.PageNumber,
            SelectedText = highlight.SelectedText,
            TargetsHeading = highlight.TargetsHeading,
            Messages = [new AiChatMessageDto { Id = message.Id, Question = message.Question, Answer = message.Answer, CreatedAt = message.CreatedAt }],
        };
    }

    public async Task<AiChatMessageDto?> AddHighlightMessageAsync(int userId, Guid highlightId, AddHighlightMessageRequest request)
    {
        var highlight = await db.NoteHighlights.FirstOrDefaultAsync(h => h.Id == highlightId);
        if (highlight is null || highlight.UserId != userId)
            return null;

        // AgentThreadId is always set by CreateHighlightAsync above — reused here so the
        // agent sees the real conversation history instead of a fresh, context-less prompt.
        var (answer, _) = await chatAiService.AskAsync(highlight.AgentThreadId, null, request.Question);

        var message = new AiChatMessage { HighlightId = highlightId, Question = request.Question, Answer = answer };
        db.AiChatMessages.Add(message);
        await db.SaveChangesAsync();

        return new AiChatMessageDto { Id = message.Id, Question = message.Question, Answer = message.Answer, CreatedAt = message.CreatedAt };
    }

    // General, not-tied-to-any-highlight question — grounded in the post's document-rag
    // knowledge base on the first call of a conversation (threadId null), then just continues
    // whatever thread the client already has for follow-ups. Never persisted server-side.
    public async Task<(string Answer, string ThreadId)?> AskGeneralAsync(int userId, int postId, string? threadId, string question)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null || (!post.IsShared && post.AuthorId != userId))
            return null;

        var sessionContext = threadId is null ? BuildSessionContext(post, selectedText: null) : null;
        return await chatAiService.AskAsync(threadId, sessionContext, question);
    }

    // interactive-chat-agent-synapse's spec grounds it in the finished document, the Document
    // RAG knowledge base, and the current Brain Map's node/edge relationships (to surface
    // "Bağlantılı Kavramlar" for whatever the user selected) — only built on the first turn
    // of a conversation, exactly like the old DocumentKnowledgeBase-only context was.
    private static string? BuildSessionContext(Post post, string? selectedText)
    {
        if (string.IsNullOrWhiteSpace(post.DocumentKnowledgeBase) && string.IsNullOrWhiteSpace(post.SynthesizedDocumentMarkdown) && string.IsNullOrWhiteSpace(post.GraphJson))
            return null;

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(selectedText))
            parts.Add($"Selected passage: \"{selectedText}\"");
        if (!string.IsNullOrWhiteSpace(post.SynthesizedDocumentMarkdown))
            parts.Add($"Finished document:\n{post.SynthesizedDocumentMarkdown}");
        if (!string.IsNullOrWhiteSpace(post.DocumentKnowledgeBase))
            parts.Add($"Document RAG knowledge base:\n{post.DocumentKnowledgeBase}");
        if (!string.IsNullOrWhiteSpace(post.GraphJson))
        {
            var graph = System.Text.Json.JsonSerializer.Deserialize<GraphDto>(post.GraphJson);
            if (graph is { Nodes.Count: > 0 })
            {
                var nodeLines = graph.Nodes.Select(n => $"- {n.Label} ({n.Type})");
                var edgeLines = graph.Edges.Select(e =>
                {
                    var source = graph.Nodes.FirstOrDefault(n => n.Id == e.Source)?.Label ?? e.Source;
                    var target = graph.Nodes.FirstOrDefault(n => n.Id == e.Target)?.Label ?? e.Target;
                    return $"- {source} {e.Label} {target}";
                });
                parts.Add("Brain Map — nodes:\n" + string.Join('\n', nodeLines) + "\n\nBrain Map — relationships:\n" + string.Join('\n', edgeLines));
            }
        }

        return string.Join("\n\n", parts);
    }

    // Cached: brain-map-agent-synapse is only called once, on first request; later opens of
    // the Map view reuse Post.GraphJson until the user explicitly asks to regenerate.
    public async Task<GraphDto?> GetOrGenerateMapAsync(int userId, int postId)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null || (!post.IsShared && post.AuthorId != userId))
            return null;

        if (!string.IsNullOrWhiteSpace(post.GraphJson))
            return System.Text.Json.JsonSerializer.Deserialize<GraphDto>(post.GraphJson);

        return await RegenerateMapAsync(post);
    }

    // Author-only, unlike GetOrGenerateMapAsync above: this always re-calls brain-map-agent and
    // overwrites the cached graph every other viewer sees, so only the post's own author can
    // trigger it — any shared-post viewer being able to blow away the author's Map for everyone
    // would be a real correctness bug, not just a permissions nicety.
    public async Task<GraphDto?> RegenerateMapAsync(int userId, int postId)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null || post.AuthorId != userId)
            return null;

        return await RegenerateMapAsync(post);
    }

    // Always final mode: by the time a Post exists it already has a finished document, so
    // Regenerate re-runs the enrichment pass against whatever the user's current (possibly
    // since-edited) keyword list is — never the draft, single-shot pass that only makes sense
    // before the document exists.
    private async Task<GraphDto> RegenerateMapAsync(Post post)
    {
        var keywords = await db.BrainMapKeywords
            .Where(k => k.PostId == post.Id && k.Status != KeywordStatus.AiDeleted)
            .Select(k => k.Text)
            .ToListAsync();

        var document = string.IsNullOrWhiteSpace(post.SynthesizedDocumentMarkdown)
            ? await FallbackDocumentFromPagesAsync(post)
            : post.SynthesizedDocumentMarkdown;

        var graph = await mapAiService.GenerateFinalGraphAsync(keywords, document, post.DocumentKnowledgeBase ?? "");
        post.GraphJson = System.Text.Json.JsonSerializer.Serialize(graph);
        await db.SaveChangesAsync();

        return graph;
    }

    // Posts created before SynthesizedDocumentMarkdown existed don't have the real
    // topic-synthesizer-agent output stored — best-effort reconstruction from the already-
    // persisted, HTML-converted Pages instead of failing Regenerate outright for them.
    private async Task<string> FallbackDocumentFromPagesAsync(Post post)
    {
        var pages = await db.NotePages
            .Where(p => p.PostId == post.Id)
            .OrderBy(p => p.Number)
            .ToListAsync();

        return string.Join("\n\n", pages.Select(p => $"## {p.Heading}\n{System.Text.RegularExpressions.Regex.Replace(p.Body, "<[^>]+>", "")}"));
    }

    // Same visibility rule as GetDetailAsync: any signed-in viewer can download a shared post,
    // only the author can download their own not-yet-shared draft.
    public async Task<(string Title, byte[] Bytes)?> GenerateNotePdfAsync(int userId, int postId)
    {
        var post = await db.Posts.Include(p => p.Pages).FirstOrDefaultAsync(p => p.Id == postId);
        if (post is null || (!post.IsShared && post.AuthorId != userId))
            return null;

        return (post.Title, BuildPdf(post.Title, post.Pages.OrderBy(p => p.Number).ToList()));
    }

    // One continuous flowing document — heading then body, page after page — mirroring the
    // print stylesheet's layout (see .note-print-pages in app.css), not the on-screen page-card
    // look. Body text is plain (same HTML-stripping FallbackDocumentFromPagesAsync already
    // uses above): QuestPDF has no HTML renderer, and the note editor's markup is just
    // <p>/highlight <span>s, not real rich-text formatting worth reproducing here.
    private static byte[] BuildPdf(string title, List<Models.NotePage> pages)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Content().Column(column =>
                {
                    column.Item().PaddingBottom(15).Text(title).FontSize(20).Bold();

                    foreach (var notePage in pages)
                    {
                        if (!string.IsNullOrWhiteSpace(notePage.Heading))
                            column.Item().PaddingTop(10).Text(notePage.Heading).FontSize(14).Bold();

                        column.Item().PaddingTop(4).Text(HtmlToPlainText(notePage.Body));
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    // Plain tag-stripping alone runs adjacent block elements together with no separator
    // (</p><p> becomes just two sentences glued end to end) and leaves entities like &#39;
    // literal — insert a line break at block boundaries first, then strip the rest and decode.
    private static string HtmlToPlainText(string html)
    {
        var withBreaks = System.Text.RegularExpressions.Regex.Replace(
            html, "</(p|li|div|h1|h2|h3|h4|h5|h6)>|<br\\s*/?>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var stripped = System.Text.RegularExpressions.Regex.Replace(withBreaks, "<[^>]+>", "");
        return System.Net.WebUtility.HtmlDecode(stripped).Trim();
    }

    public async Task<PostOpResult> ShareAsync(int userId, int postId)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null)
            return PostOpResult.NotFound;
        if (post.AuthorId != userId)
            return PostOpResult.Forbidden;

        if (!post.IsShared)
        {
            post.IsShared = true;
            await db.SaveChangesAsync();
        }

        return PostOpResult.Success;
    }

    // Only flips IsShared back off — likes/comments/CreatedDate are untouched, so pulling a
    // post back from the public feed doesn't lose any of its history, and re-sharing later
    // picks up right where it left off.
    public async Task<PostOpResult> UnshareAsync(int userId, int postId)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null)
            return PostOpResult.NotFound;
        if (post.AuthorId != userId)
            return PostOpResult.Forbidden;

        if (post.IsShared)
        {
            post.IsShared = false;
            await db.SaveChangesAsync();
        }

        return PostOpResult.Success;
    }

    public async Task<PostOpResult> DeleteAsync(int userId, int postId)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null)
            return PostOpResult.NotFound;
        if (post.AuthorId != userId)
            return PostOpResult.Forbidden;

        db.Posts.Remove(post);
        await db.SaveChangesAsync();
        return PostOpResult.Success;
    }

    public async Task<PostOpResult> SetGroupAsync(int userId, int postId, int? groupId)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null)
            return PostOpResult.NotFound;
        if (post.AuthorId != userId)
            return PostOpResult.Forbidden;

        if (groupId is int id)
        {
            var group = await db.Groups.FindAsync(id);
            if (group is null || group.OwnerId != userId)
                return PostOpResult.GroupNotFound;
        }

        post.GroupId = groupId;
        await db.SaveChangesAsync();
        return PostOpResult.Success;
    }

    public Task<(PostOpResult Status, bool Active, int Count)> ToggleLikeAsync(int userId, int postId) =>
        ToggleReactionAsync(userId, postId, nameof(Post.LikedByUsers), p => p.LikedByUsers);

    public Task<(PostOpResult Status, bool Active, int Count)> ToggleFavoriteAsync(int userId, int postId) =>
        ToggleReactionAsync(userId, postId, nameof(Post.FavoritedByUsers), p => p.FavoritedByUsers);

    public Task<(PostOpResult Status, bool Active, int Count)> ToggleRepostAsync(int userId, int postId) =>
        ToggleReactionAsync(userId, postId, nameof(Post.RepostedByUsers), p => p.RepostedByUsers);

    public async Task<PostCommentDto?> AddCommentAsync(int userId, int postId, string text)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post is null)
            return null;

        var author = await db.Users.FindAsync(userId);
        if (author is null)
            return null;

        var comment = new PostComment { PostId = postId, AuthorId = userId, Text = text.Trim() };
        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        return new PostCommentDto
        {
            Id = comment.Id,
            AuthorId = userId,
            AuthorName = $"{author.Name} {author.Surname}",
            Text = comment.Text,
            CreatedAt = comment.CreatedAt,
        };
    }

    public async Task<PostOpResult> DeleteCommentAsync(int userId, bool isAdmin, int commentId)
    {
        var comment = await db.Comments.FindAsync(commentId);
        if (comment is null)
            return PostOpResult.NotFound;
        if (comment.AuthorId != userId && !isAdmin)
            return PostOpResult.Forbidden;

        db.Comments.Remove(comment);
        await db.SaveChangesAsync();
        return PostOpResult.Success;
    }

    // Toggles the caller's membership in a post's Liked/Favorited/Reposted user list. Loading
    // both the post's reaction collection and the user through the same DbContext relies on
    // EF's identity map so `list.Remove(user)` finds the tracked instance by reference.
    private async Task<(PostOpResult Status, bool Active, int Count)> ToggleReactionAsync(
        int userId, int postId, string includePath, Func<Post, List<User>> reactionList)
    {
        var post = await db.Posts.Include(includePath).FirstOrDefaultAsync(p => p.Id == postId);
        if (post is null)
            return (PostOpResult.NotFound, false, 0);

        var user = await db.Users.FindAsync(userId);
        if (user is null)
            return (PostOpResult.NotFound, false, 0);

        var list = reactionList(post);
        bool active;
        if (list.Remove(user))
            active = false;
        else
        {
            list.Add(user);
            active = true;
        }

        await db.SaveChangesAsync();
        return (PostOpResult.Success, active, list.Count);
    }

    private IQueryable<PostDto> QueryDto(int currentUserId) =>
        db.Posts.Select(p => new PostDto
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            MiniDescription = p.MiniDescription,
            CreatedDate = p.CreatedDate,
            IsShared = p.IsShared,
            ShareBrainMap = p.ShareBrainMap,
            ShareProcess = p.ShareProcess,
            FirstPage = p.Pages.OrderBy(pg => pg.Number)
                .Select(pg => new NotePageDto { Number = pg.Number, Heading = pg.Heading, Body = pg.Body })
                .FirstOrDefault(),
            AuthorId = p.AuthorId,
            AuthorName = p.Author.Name + " " + p.Author.Surname,
            AuthorRole = p.Author.JobTitle,
            GroupId = p.GroupId,
            GroupName = p.Group != null ? p.Group.Name : null,
            Sends = p.Sends,
            Downloads = p.Downloads,
            Files = p.Files
                .Select(f => new NoteFileDto { FileName = f.FileName, FileUrl = f.FileUrl, PageCount = f.PageCount })
                .ToList(),
            GeneratedPageCount = p.Pages.Count,
            LikeCount = p.LikedByUsers.Count,
            FavoriteCount = p.FavoritedByUsers.Count,
            RepostCount = p.RepostedByUsers.Count,
            CommentCount = p.Comments.Count,
            LikedByMe = p.LikedByUsers.Any(u => u.Id == currentUserId),
            FavoritedByMe = p.FavoritedByUsers.Any(u => u.Id == currentUserId),
            RepostedByMe = p.RepostedByUsers.Any(u => u.Id == currentUserId),
            FollowedByMe = p.Author.FollowerLinks.Any(f => f.FollowerId == currentUserId),
            Comments = p.Comments
                .OrderBy(c => c.CreatedAt)
                .Select(c => new PostCommentDto
                {
                    Id = c.Id,
                    AuthorId = c.AuthorId,
                    AuthorName = c.Author.Name + " " + c.Author.Surname,
                    Text = c.Text,
                    CreatedAt = c.CreatedAt,
                })
                .ToList(),
        });

    private static PostDetailDto ToDetailDto(PostDto basic) => new()
    {
        Id = basic.Id,
        Title = basic.Title,
        Description = basic.Description,
        MiniDescription = basic.MiniDescription,
        CreatedDate = basic.CreatedDate,
        IsShared = basic.IsShared,
        AuthorId = basic.AuthorId,
        AuthorName = basic.AuthorName,
        AuthorRole = basic.AuthorRole,
        GroupId = basic.GroupId,
        GroupName = basic.GroupName,
        Sends = basic.Sends,
        Downloads = basic.Downloads,
        Files = basic.Files,
        GeneratedPageCount = basic.GeneratedPageCount,
        LikeCount = basic.LikeCount,
        FavoriteCount = basic.FavoriteCount,
        RepostCount = basic.RepostCount,
        CommentCount = basic.CommentCount,
        LikedByMe = basic.LikedByMe,
        FavoritedByMe = basic.FavoritedByMe,
        RepostedByMe = basic.RepostedByMe,
        FollowedByMe = basic.FollowedByMe,
        Comments = basic.Comments,
    };

    private static PostDto ToDto(Post post, User author) => new()
    {
        Id = post.Id,
        Title = post.Title,
        Description = post.Description,
        MiniDescription = post.MiniDescription,
        CreatedDate = post.CreatedDate,
        AuthorId = post.AuthorId,
        AuthorName = $"{author.Name} {author.Surname}",
        AuthorRole = author.JobTitle,
        GroupId = post.GroupId,
        Files = post.Files
            .Select(f => new NoteFileDto { FileName = f.FileName, FileUrl = f.FileUrl, PageCount = f.PageCount })
            .ToList(),
        GeneratedPageCount = post.Pages.Count,
    };
}
