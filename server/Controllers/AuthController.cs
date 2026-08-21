using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Dtos;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(AuthService authService, UserService userService) : ControllerBase
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

    // Always returns the same generic response whether or not the email exists — otherwise
    // this endpoint becomes a way to check which emails are registered.
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        await userService.ForgotPasswordAsync(request.Email.Trim());
        return Ok(new { message = "If that email is registered, a new password has been sent to it." });
    }
}
