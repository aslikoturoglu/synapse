namespace Client.Services;

public class UserRequestDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string RequesterName { get; set; } = "";
    public string UserEmail { get; set; } = "";
    public string Username { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Subject { get; set; } = "";
    public string Content { get; set; } = "";
    public string? RequestedRole { get; set; }
    public string Status { get; set; } = "Unseen";
    public string Decision { get; set; } = "Pending";
    public string? Reply { get; set; }
    public DateTime? HandledAt { get; set; }
}

public class CreateUserRequestRequest
{
    public required string Subject { get; set; }
    public required string Content { get; set; }
}

public class RespondToRequestRequest
{
    public string? Reply { get; set; }
    public string? Decision { get; set; }
}
