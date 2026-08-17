namespace Server.Dtos;

public class AdminUserDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required string Surname { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public required string JobTitle { get; set; }
    public required string Role { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public bool IsDeactivated { get; set; }
    public string? DeactivationReason { get; set; }

    // Real per-user AI token metering isn't wired up yet (FoundryAgentClient doesn't capture
    // usage from the Responses API) — always 0 for now rather than a fabricated number.
    public int TokenUsage { get; set; }
}

public class DeactivateUserRequest
{
    public required string Reason { get; set; }
}

public class ResetPasswordResponse
{
    public required string NewPassword { get; set; }
}
