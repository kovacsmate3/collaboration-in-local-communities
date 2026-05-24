using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Admin.AuditLog;

[ApiController]
[Route("api/admin/audit-log")]
[Authorize(Roles = "Admin")]
public sealed class AdminAuditLogController(AppDbContext db) : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    [HttpGet]
    public async Task<IActionResult> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        [FromQuery] string? eventType = null,
        [FromQuery] Guid? actorUserId = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            ModelState.AddModelError(nameof(page), "Page must be at least 1.");
            return ValidationProblem(ModelState);
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            ModelState.AddModelError(nameof(pageSize), $"PageSize must be between 1 and {MaxPageSize}.");
            return ValidationProblem(ModelState);
        }

        var query = db.AuditEvents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(e => e.EventType == eventType);
        }

        if (actorUserId.HasValue)
        {
            query = query.Where(e => e.ActorUserId == actorUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(e => e.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e => e.EventType.Contains(search));
        }

        if (from.HasValue)
        {
            query = query.Where(e => e.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.CreatedAt <= to.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (page - 1) * pageSize;

        var events = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var actorIds = events
            .Where(e => e.ActorUserId.HasValue)
            .Select(e => e.ActorUserId!.Value)
            .Distinct()
            .ToList();

        var emailByUserId = await db.Users
            .Where(u => actorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);

        var nameLookup = await db.Profiles
            .Where(p => actorIds.Contains(p.UserId))
            .Select(p => new { p.UserId, p.DisplayName })
            .ToDictionaryAsync(p => p.UserId, p => p.DisplayName, cancellationToken);

        var items = events.Select(e => new AuditLogEntryResponse(
            e.Id,
            e.ActorUserId,
            e.ActorUserId.HasValue ? emailByUserId.GetValueOrDefault(e.ActorUserId.Value) : null,
            e.ActorUserId.HasValue ? nameLookup.GetValueOrDefault(e.ActorUserId.Value) : null,
            e.EventType,
            e.EntityType,
            e.EntityId,
            e.Payload,
            e.CreatedAt)).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return Ok(new AuditLogPagedResponse(items, totalCount, page, pageSize, totalPages));
    }
}
