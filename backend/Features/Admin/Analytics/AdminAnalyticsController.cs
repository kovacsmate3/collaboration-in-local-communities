using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Admin.Analytics;

[ApiController]
[Route("api/admin/analytics")]
[Authorize(Roles = "Admin")]
public sealed class AdminAnalyticsController(AppDbContext db) : ControllerBase
{
    [HttpGet("kpi")]
    public async Task<IActionResult> GetKpiAsync(CancellationToken cancellationToken)
    {
        var current = await db.KpiCurrent
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(KpiCurrentResponse.FromReadModel(current));
    }
}
