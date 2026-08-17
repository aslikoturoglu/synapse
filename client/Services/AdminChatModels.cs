namespace Client.Services;

public class AdminChatRequest
{
    public string? ThreadId { get; set; }
    public required string Question { get; set; }
}

public class AdminChatResponse
{
    public string Answer { get; set; } = "";
    public string ThreadId { get; set; } = "";
}
