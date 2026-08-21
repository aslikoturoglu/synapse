namespace Server.Dtos;

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

public class ForgotPasswordRequest
{
    public required string Email { get; set; }
}

public class LoginRequest
{
    public required string UsernameOrEmail { get; set; }
    public required string Password { get; set; }
}

public class AuthResponse
{
    public required string Token { get; set; }
    public required UserDto User { get; set; }
}
