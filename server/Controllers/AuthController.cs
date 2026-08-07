using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Dtos;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("signup")]
    public async Task<ActionResult<AuthResponse>> Signup(SignupRequest request)
    {
        var result = await authService.SignupAsync(request);
        if (!result.Succeeded)
            return Conflict(new { error = result.Error });

        return CreatedAtAction(nameof(Signup), result.Response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        if (!result.Succeeded)
            return Unauthorized(new { error = result.Error });

        return Ok(result.Response);
    }
}
