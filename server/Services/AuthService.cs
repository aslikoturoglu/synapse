using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Data;
using Server.Dtos;
using Server.Models;

namespace Server.Services;

public record AuthResult(bool Succeeded, string? Error, AuthResponse? Response)
{
    public static AuthResult Success(AuthResponse response) => new(true, null, response);
    public static AuthResult Failure(string error) => new(false, error, null);
}

public class AuthService(AppDbContext db, IConfiguration config)
{
    public async Task<AuthResult> SignupAsync(SignupRequest request)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim();

        if (await db.Users.AnyAsync(u => u.Username == username))
            return AuthResult.Failure("Username is already taken.");
        if (await db.Users.AnyAsync(u => u.Email == email))
            return AuthResult.Failure("Email is already registered.");

        var user = new User
        {
            Name = request.Name.Trim(),
            Surname = request.Surname.Trim(),
            Username = username,
            Email = email,
            PasswordHash = PasswordHasher.Hash(request.Password),
        };
        user.Settings = new UserSettings { User = user };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return AuthResult.Success(BuildResponse(user));
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var identifier = request.UsernameOrEmail.Trim();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == identifier || u.Email == identifier);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            return AuthResult.Failure("Invalid username/email or password.");

        return AuthResult.Success(BuildResponse(user));
    }

    private AuthResponse BuildResponse(User user) => new()
    {
        Token = GenerateToken(user),
        User = new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Surname = user.Surname,
            Username = user.Username,
            Email = user.Email,
            Phone = user.Phone,
            JobTitle = user.JobTitle,
        },
    };

    private string GenerateToken(User user)
    {
        var jwt = config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.")));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
        ];

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwt.GetValue<int>("ExpiryMinutes")),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
