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

    // Lifetime sum of TokenUsageLogs for this user (UserService.GetAllAdminAsync).
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
