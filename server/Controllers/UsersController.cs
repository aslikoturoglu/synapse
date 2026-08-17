using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Dtos;
using Server.Services;

namespace Server.Controllers;

// Every action here exposes another user's contact info or lets the caller change/end their
// account — all of it is admin-only, not just "any authenticated user" like most other
// controllers in this app.
[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController(UserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AdminUserDto>>> GetAll() => Ok(await userService.GetAllAdminAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var user = await userService.GetByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, DeactivateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { error = "A reason is required." });

        return await userService.DeactivateAsync(id, request.Reason.Trim()) ? NoContent() : NotFound();
    }

    [HttpPost("{id:int}/reactivate")]
    public async Task<IActionResult> Reactivate(int id) =>
        await userService.ReactivateAsync(id) ? NoContent() : NotFound();

    [HttpPost("{id:int}/toggle-role")]
    public async Task<IActionResult> ToggleRole(int id) => await userService.ToggleRoleAsync(id) switch
    {
        ToggleRoleResult.Success => NoContent(),
        ToggleRoleResult.LastAdmin => BadRequest(new { error = "Can't change this user's role — they're the last remaining admin." }),
        _ => NotFound(),
    };

    [HttpPost("{id:int}/reset-password")]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword(int id)
    {
        var newPassword = await userService.ResetPasswordAsync(id);
        return newPassword is null ? NotFound() : Ok(new ResetPasswordResponse { NewPassword = newPassword });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        await userService.DeleteAsync(id) ? NoContent() : NotFound();
}
