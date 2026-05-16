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
        var action = request.Action;

        if (!string.Equals(action, "Approve", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(action, "Deactivate", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(action, "Activate", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(request.Action), $"Invalid action '{action}'. Allowed: 'Approve', 'Deactivate', 'Activate'.");
            return ValidationProblem(ModelState);
        }

        var skill = await db.Skills
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (skill is null)
        {
            return NotFound();
        }

        if (string.Equals(action, "Approve", StringComparison.OrdinalIgnoreCase))
        {
            if (skill.Status != SkillStatus.Approved)
            {
                skill.Status = SkillStatus.Approved;
                skill.ApprovedAt = DateTimeOffset.UtcNow;
                skill.UpdatedAt = DateTimeOffset.UtcNow;

                AddAuditEvent(GetActorUserId(), "admin.skill_approved", "Skill", skill.Id, new { skill.Name, skill.Code });

                await db.SaveChangesAsync(cancellationToken);
            }
        }
        else if (string.Equals(action, "Deactivate", StringComparison.OrdinalIgnoreCase))
        {
            if (skill.IsActive)
            {
                skill.IsActive = false;
                skill.UpdatedAt = DateTimeOffset.UtcNow;

                AddAuditEvent(GetActorUserId(), "admin.skill_deactivated", "Skill", skill.Id, new { skill.Name, skill.Code });

                await db.SaveChangesAsync(cancellationToken);
            }
        }
        else if (string.Equals(action, "Activate", StringComparison.OrdinalIgnoreCase))
        {
            if (!skill.IsActive)
            {
                skill.IsActive = true;
                skill.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        return Ok(AdminSkillResponse.FromEntity(skill));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var skill = await db.Skills.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (skill is null)
        {
            return NotFound();
        }

        bool isLinked = await db.ProfileSkills.AnyAsync(ps => ps.SkillId == id, cancellationToken);

        if (isLinked)
        {
            return Conflict("This skill is linked to user profiles and cannot be deleted. Deactivate it instead.");
        }

        db.Skills.Remove(skill);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
