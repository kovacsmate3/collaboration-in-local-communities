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

        if (current is null)
        {
            return NotFound();
        }

        return Ok(KpiCurrentResponse.FromReadModel(current));
    }

    [HttpGet("charts/task-status")]
    public async Task<IActionResult> GetTaskStatusChartAsync(CancellationToken cancellationToken)
    {
        var groups = await db.Tasks.AsNoTracking()
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var total = groups.Sum(g => g.Count);
        var entries = groups
            .OrderByDescending(g => g.Count)
            .Select(g => new ChartEntry(
                g.Status.ToString(),
                g.Count,
                total > 0 ? Math.Round(g.Count * 100.0 / total, 1) : 0))
            .ToList();

        return Ok(new ChartDataResponse(entries));
    }

    [HttpGet("charts/category-demand")]
    public async Task<IActionResult> GetCategoryDemandChartAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);

        var groups = await db.Tasks.AsNoTracking()
            .Where(t => t.CreatedAt >= cutoff)
            .GroupBy(t => new { t.CategoryId, t.Category.Name })
            .Select(g => new { g.Key.Name, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(6)
            .ToListAsync(cancellationToken);

        var maxCount = groups.Count > 0 ? groups[0].Count : 0;
        var entries = groups
            .Select(g => new ChartEntry(
                g.Name,
                g.Count,
                maxCount > 0 ? Math.Round(g.Count * 100.0 / maxCount, 1) : 0))
            .ToList();

        return Ok(new ChartDataResponse(entries));
    }

    [HttpGet("charts/compensation-mix")]
    public async Task<IActionResult> GetCompensationMixChartAsync(CancellationToken cancellationToken)
    {
        var groups = await db.Tasks.AsNoTracking()
            .GroupBy(t => t.CompensationType)
            .Select(g => new { CompensationType = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var total = groups.Sum(g => g.Count);
        var entries = groups
            .OrderByDescending(g => g.Count)
            .Select(g => new ChartEntry(
                g.CompensationType.ToString(),
                g.Count,
                total > 0 ? Math.Round(g.Count * 100.0 / total, 1) : 0))
            .ToList();

        return Ok(new ChartDataResponse(entries));
    }

    [HttpGet("charts/task-application-status")]
    public async Task<IActionResult> GetTaskApplicationStatusChartAsync(CancellationToken cancellationToken)
    {
        var groups = await db.TaskApplications.AsNoTracking()
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var total = groups.Sum(g => g.Count);
        var entries = groups
            .OrderByDescending(g => g.Count)
            .Select(g => new ChartEntry(
                g.Status.ToString(),
                g.Count,
                total > 0 ? Math.Round(g.Count * 100.0 / total, 1) : 0))
            .ToList();

        return Ok(new ChartDataResponse(entries));
    }
}
