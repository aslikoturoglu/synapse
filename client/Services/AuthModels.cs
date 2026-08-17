namespace Client.Services;

public class SignupRequest
{
    public required string Name { get; set; }
    public required string Surname { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public string JobTitle { get; set; } = "";
    public required string Password { get; set; }
}

public class LoginRequest
{
    public required string UsernameOrEmail { get; set; }
    public required string Password { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Surname { get; set; } = "";
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public string Role { get; set; } = "User";
    public bool IsDeactivated { get; set; }
    public string? DeactivationReason { get; set; }
}

public class AuthResponseDto
{
    public string Token { get; set; } = "";
    public UserDto User { get; set; } = new();
}
