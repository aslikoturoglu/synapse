namespace Server.Dtos;

public class UserRequestDto
{
    public required int Id { get; set; }
    public required int UserId { get; set; }

    // "RequesterName" (full name), not "UserName" — colliding with Username below under
    // case-insensitive JSON property matching ("userName" vs "username") crashed serialization.
    public required string RequesterName { get; set; }
    public required string UserEmail { get; set; }
    public required string Username { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required string Subject { get; set; }
    public required string Content { get; set; }
    public string? RequestedRole { get; set; }

    // Status/Decision are strings, same convention as UserDto.Role.
    public required string Status { get; set; }
    public required string Decision { get; set; }
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

    // "Approved" | "Rejected" — Enum.Parse<RequestDecision>, so must match a real value other
    // than "Pending"; null means "don't change the decision, just record the reply".
    public string? Decision { get; set; }
}
