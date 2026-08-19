using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Dtos;
using Server.Services;

namespace Server.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Admin,Moderator")]
public class DashboardController(DashboardService dashboardService) : ControllerBase
{
    // range: "total" | "6h" | "24h" | "1w" | "1m" | "6m" | "1y" | "custom" (custom reads from/to).
    [HttpGet]
    public async Task<ActionResult<DashboardStatsDto>> Get([FromQuery] string range = "24h", [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var (start, end) = DashboardService.ResolveRange(range, from, to);
        return Ok(await dashboardService.GetStatsAsync(start, end));
    }
}
