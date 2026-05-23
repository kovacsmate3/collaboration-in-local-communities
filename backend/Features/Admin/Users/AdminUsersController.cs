using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Infrastructure.Identity;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Admin.Users;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public sealed class AdminUsersController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    [HttpGet]
    public async Task<IActionResult> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        CancellationToken ct = default)
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

        var query =
            from user in db.Users.AsNoTracking()
            join profile in db.Profiles.AsNoTracking()
                on user.Id equals profile.UserId into profileJoin
            from profile in profileJoin.DefaultIfEmpty()
            select new { UserId = user.Id, user.Email, user.EmailConfirmed, profile };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var escaped = search.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
            var pattern = $"%{escaped}%";

            // ReSharper disable EntityFramework.ClientSideDbFunctionCall
            query = query.Where(x =>
                (x.Email != null && EF.Functions.ILike(x.Email, pattern, @"\")) ||
                (x.profile != null && EF.Functions.ILike(x.profile.DisplayName, pattern, @"\")));

            // ReSharper restore EntityFramework.ClientSideDbFunctionCall
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleIds = db.Roles.Where(r => r.Name == role).Select(r => r.Id);
            var userIdsWithRole = db.UserRoles
                .Where(ur => roleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId);
            query = query.Where(x => userIdsWithRole.Contains(x.UserId));
        }

        var totalCount = await query.CountAsync(ct);

        var pageUsers = await query
            .OrderByDescending(x => x.profile != null ? (DateTimeOffset?)x.profile.CreatedAt : null)
            .ThenBy(x => x.UserId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var userIds = pageUsers.Select(x => x.UserId).ToList();

        var rolesLookup = await (
            from ur in db.UserRoles
            where userIds.Contains(ur.UserId)
            join r in db.Roles on ur.RoleId equals r.Id
            select new { ur.UserId, r.Name }).ToListAsync(ct);

        var rolesMap = rolesLookup
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.Select(x => x.Name ?? string.Empty).ToList());

        var items = pageUsers.Select(x => new AdminUserResponse(
            x.UserId,
            x.Email ?? string.Empty,
            x.EmailConfirmed,
            x.profile?.DisplayName,
            x.profile?.PhotoUrl,
            x.profile?.IsProfileCompleted ?? false,
            rolesMap.TryGetValue(x.UserId, out var roles) ? roles : [],
            x.profile?.CreatedAt)).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return Ok(new AdminUserPagedResponse(items, totalCount, page, pageSize, totalPages));
    }

    [HttpPost("{id:guid}/make-admin")]
    public async Task<IActionResult> MakeAdminAsync(Guid id, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
        {
            return NotFound();
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Contains(ApplicationRoleNames.Admin))
        {
            return Ok(await BuildResponseAsync(user, currentRoles, ct));
        }

        var addResult = await userManager.AddToRoleAsync(user, ApplicationRoleNames.Admin);
        if (!addResult.Succeeded)
        {
            foreach (var error in addResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        db.AuditEvents.Add(new AuditEvent
        {
            ActorUserId = GetCurrentUserId(),
            EventType = "admin.user_promoted_to_admin",
            EntityType = "ApplicationUser",
            EntityId = id,
            Payload = null,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);

        var updatedRoles = await userManager.GetRolesAsync(user);
        return Ok(await BuildResponseAsync(user, updatedRoles, ct));
    }

    [HttpDelete("{id:guid}/make-admin")]
    public async Task<IActionResult> RevokeAdminAsync(Guid id, CancellationToken ct)
    {
        if (id == GetCurrentUserId())
        {
            return BadRequest("Cannot demote yourself.");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
        {
            return NotFound();
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(ApplicationRoleNames.Admin))
        {
            return Ok(await BuildResponseAsync(user, currentRoles, ct));
        }

        var removeResult = await userManager.RemoveFromRoleAsync(user, ApplicationRoleNames.Admin);
        if (!removeResult.Succeeded)
        {
            foreach (var error in removeResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        db.AuditEvents.Add(new AuditEvent
        {
            ActorUserId = GetCurrentUserId(),
            EventType = "admin.user_demoted_from_admin",
            EntityType = "ApplicationUser",
            EntityId = id,
            Payload = null,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);

        var updatedRoles = await userManager.GetRolesAsync(user);
        return Ok(await BuildResponseAsync(user, updatedRoles, ct));
    }

    private async Task<AdminUserResponse> BuildResponseAsync(
        ApplicationUser user,
        IList<string> roles,
        CancellationToken ct)
    {
        var profile = await db.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == user.Id, ct);

        return new AdminUserResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.EmailConfirmed,
            profile?.DisplayName,
            profile?.PhotoUrl,
            profile?.IsProfileCompleted ?? false,
            roles.ToList(),
            profile?.CreatedAt);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim?.Value, out var id) ? id : null;
    }
}
