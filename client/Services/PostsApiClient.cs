using System.Net.Http.Json;

namespace Client.Services;

public class PostsApiClient(HttpClient http)
{
    public async Task<List<PostDto>> GetFeedAsync() =>
        await http.GetFromJsonAsync<List<PostDto>>("api/posts") ?? [];

    public async Task<List<PostDto>> GetMineAsync() =>
        await http.GetFromJsonAsync<List<PostDto>>("api/posts/mine") ?? [];

    public async Task<PostDto?> CreateAsync(CreatePostRequest request)
    {
        var response = await http.PostAsJsonAsync("api/posts", request);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<PostDto>() : null;
    }

    public async Task<PostDetailDto?> GetDetailAsync(int postId) =>
        await http.GetFromJsonAsync<PostDetailDto>($"api/posts/{postId}");

    public async Task<byte[]?> GetPdfAsync(int postId)
    {
        var response = await http.GetAsync($"api/posts/{postId}/pdf");
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
    }

    public async Task<bool> ShareAsync(int postId) =>
        (await http.PostAsync($"api/posts/{postId}/share", null)).IsSuccessStatusCode;

    public async Task<bool> UnshareAsync(int postId) =>
        (await http.DeleteAsync($"api/posts/{postId}/share")).IsSuccessStatusCode;

    public async Task<BrainMapKeywordDto?> AddKeywordAsync(int postId, string text)
    {
        var response = await http.PostAsJsonAsync($"api/posts/{postId}/keywords", new AddKeywordRequest { Text = text });
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<BrainMapKeywordDto>() : null;
    }

    public async Task<bool> RemoveKeywordAsync(int postId, int keywordId) =>
        (await http.DeleteAsync($"api/posts/{postId}/keywords/{keywordId}")).IsSuccessStatusCode;

    public async Task<bool> RestoreKeywordAsync(int postId, int keywordId) =>
        (await http.PostAsync($"api/posts/{postId}/keywords/{keywordId}/restore", null)).IsSuccessStatusCode;

    public async Task<bool> UpdateTitleAsync(int postId, string title) =>
        (await http.PutAsJsonAsync($"api/posts/{postId}/title", new UpdatePostTitleRequest { Title = title })).IsSuccessStatusCode;

    public async Task<bool> UpdateDescriptionAsync(int postId, string description) =>
        (await http.PutAsJsonAsync($"api/posts/{postId}/description", new UpdatePostDescriptionRequest { Description = description })).IsSuccessStatusCode;

    public async Task<bool> UpdateShareSettingsAsync(int postId, bool showBrainMap, bool showProcess) =>
        (await http.PutAsJsonAsync($"api/posts/{postId}/share-settings", new UpdateShareSettingsRequest { ShareBrainMap = showBrainMap, ShareProcess = showProcess })).IsSuccessStatusCode;

    public async Task<bool> UpdatePageBodyAsync(int postId, int pageNumber, string body) =>
        (await http.PutAsJsonAsync($"api/posts/{postId}/pages/{pageNumber}", new UpdatePageBodyRequest { Body = body })).IsSuccessStatusCode;

    public async Task<bool> UpdatePageHeadingAsync(int postId, int pageNumber, string heading) =>
        (await http.PutAsJsonAsync($"api/posts/{postId}/pages/{pageNumber}/heading", new UpdatePageHeadingRequest { Heading = heading })).IsSuccessStatusCode;

    public async Task<bool> UpdatePersonalNotesAsync(int postId, string notes) =>
        (await http.PutAsJsonAsync($"api/posts/{postId}/personal-notes", new UpdatePersonalNotesRequest { Notes = notes })).IsSuccessStatusCode;

    public async Task<bool> IncrementDocumentChangeAsync(int postId) =>
        (await http.PostAsync($"api/posts/{postId}/document-change", null)).IsSuccessStatusCode;

    public async Task<bool> MarkAddedToDocumentAsync(int postId, Guid highlightId, int messageId) =>
        (await http.PostAsync($"api/posts/{postId}/highlights/{highlightId}/messages/{messageId}/add-to-document", null)).IsSuccessStatusCode;

    public async Task<NoteHighlightDto?> CreateHighlightAsync(int postId, CreateHighlightRequest request)
    {
        var response = await http.PostAsJsonAsync($"api/posts/{postId}/highlights", request);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<NoteHighlightDto>() : null;
    }

    public async Task<AiChatMessageDto?> AddHighlightMessageAsync(int postId, Guid highlightId, AddHighlightMessageRequest request)
    {
        var response = await http.PostAsJsonAsync($"api/posts/{postId}/highlights/{highlightId}/messages", request);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<AiChatMessageDto>() : null;
    }

    public async Task<AskAiResponse?> AskAsync(int postId, AskAiRequest request)
    {
        var response = await http.PostAsJsonAsync($"api/posts/{postId}/ask", request);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<AskAiResponse>() : null;
    }

    public async Task<GraphDto?> GetMapAsync(int postId) =>
        await PostForJsonAsync<GraphDto>($"api/posts/{postId}/map");

    public async Task<GraphDto?> RegenerateMapAsync(int postId) =>
        await PostForJsonAsync<GraphDto>($"api/posts/{postId}/map/regenerate");

    private async Task<T?> PostForJsonAsync<T>(string url)
    {
        var response = await http.PostAsync(url, null);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<T>() : default;
    }

    public async Task<bool> DeleteAsync(int postId) =>
        (await http.DeleteAsync($"api/posts/{postId}")).IsSuccessStatusCode;

    public async Task<bool> SetGroupAsync(int postId, int? groupId) =>
        (await http.PatchAsJsonAsync($"api/posts/{postId}/group", new UpdatePostGroupRequest { GroupId = groupId })).IsSuccessStatusCode;

    public Task<ReactionResponse?> ToggleLikeAsync(int postId) => PostForReactionAsync($"api/posts/{postId}/like");

    public Task<ReactionResponse?> ToggleFavoriteAsync(int postId) => PostForReactionAsync($"api/posts/{postId}/favorite");

    public Task<ReactionResponse?> ToggleRepostAsync(int postId) => PostForReactionAsync($"api/posts/{postId}/repost");

    public async Task<bool> SendByEmailAsync(int postId, string to, string? message) =>
        (await http.PostAsJsonAsync($"api/posts/{postId}/send", new SendNoteEmailRequest { To = to, Message = message })).IsSuccessStatusCode;

    public async Task<PostCommentDto?> AddCommentAsync(int postId, string text)
    {
        var response = await http.PostAsJsonAsync($"api/posts/{postId}/comments", new AddCommentRequest { Text = text });
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<PostCommentDto>() : null;
    }

    public async Task<bool> DeleteCommentAsync(int commentId) =>
        (await http.DeleteAsync($"api/posts/comments/{commentId}")).IsSuccessStatusCode;

    private async Task<ReactionResponse?> PostForReactionAsync(string url)
    {
        var response = await http.PostAsync(url, null);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<ReactionResponse>() : null;
    }
}
