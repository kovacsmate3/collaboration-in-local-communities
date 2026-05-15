using System.Security.Claims;
using System.Text.Json;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Admin.Skills;

[ApiController]
[Route("api/admin/skills")]
[Authorize(Roles = "Admin")]
public sealed class AdminSkillsController(AppDbContext db) : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    [HttpGet]
    public async Task<IActionResult> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        [FromQuery] string? status = null,
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

        var query = db.Skills.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<SkillStatus>(status, ignoreCase: true, out var parsedStatus))
            {
                ModelState.AddModelError(nameof(status), $"Invalid status '{status}'. Allowed values: Pending, Approved.");
                return ValidationProblem(ModelState);
            }

            query = query.Where(skill => skill.Status == parsedStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var skills = await query
            .OrderByDescending(skill => skill.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var response = new AdminSkillPagedResponse(
            skills.Select(AdminSkillResponse.FromEntity).ToList(),
            totalCount,
            page,
            pageSize,
            totalPages);

        return Ok(response);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> PatchAsync(
        Guid id,
        PatchSkillRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.Action, "Approve", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(request.Action), $"Invalid action '{request.Action}'. Allowed: 'Approve'.");
            return ValidationProblem(ModelState);
        }

        var skill = await db.Skills
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (skill is null)
        {
            return NotFound();
        }

        if (skill.Status == SkillStatus.Approved)
        {
            return Ok(AdminSkillResponse.FromEntity(skill));
        }

        skill.Status = SkillStatus.Approved;
        skill.ApprovedAt = DateTimeOffset.UtcNow;
        skill.UpdatedAt = DateTimeOffset.UtcNow;

        AddAuditEvent(GetActorUserId(), "admin.skill_approved", "Skill", skill.Id, new { skill.Name, skill.Code });

        await db.SaveChangesAsync(cancellationToken);

        return Ok(AdminSkillResponse.FromEntity(skill));
    }

    private Guid? GetActorUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private void AddAuditEvent(Guid? actorUserId, string eventType, string? entityType, Guid? entityId, object? payload)
    {
        db.AuditEvents.Add(new AuditEvent
        {
            ActorUserId = actorUserId,
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            Payload = payload is null ? null : JsonSerializer.Serialize(payload),
            CreatedAt = DateTimeOffset.UtcNow,
        });
    }
}
