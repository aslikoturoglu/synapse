namespace Client.Services;

public class AdminUserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Surname { get; set; } = "";
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public string Role { get; set; } = "User";
    public DateTime CreatedAt { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public bool IsDeactivated { get; set; }
    public string? DeactivationReason { get; set; }
    public int TokenUsage { get; set; }

    public string FullName => $"{Name} {Surname}";
}

public class DeactivateUserRequest
{
    public required string Reason { get; set; }
}

public class ResetPasswordResponse
{
    public string NewPassword { get; set; } = "";
}

public class ErrorResponse
{
    public string Error { get; set; } = "";
}
