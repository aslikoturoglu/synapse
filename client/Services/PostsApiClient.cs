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

    public async Task<bool> DeleteAsync(int postId) =>
        (await http.DeleteAsync($"api/posts/{postId}")).IsSuccessStatusCode;

    public Task<ReactionResponse?> ToggleLikeAsync(int postId) => PostForReactionAsync($"api/posts/{postId}/like");

    public Task<ReactionResponse?> ToggleFavoriteAsync(int postId) => PostForReactionAsync($"api/posts/{postId}/favorite");

    public Task<ReactionResponse?> ToggleRepostAsync(int postId) => PostForReactionAsync($"api/posts/{postId}/repost");

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
