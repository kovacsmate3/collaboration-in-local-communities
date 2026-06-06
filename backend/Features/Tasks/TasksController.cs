using Backend.Common;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Domain.Tasks;
using Backend.Infrastructure.Persistence;
using Backend.Infrastructure.Security;
using Backend.Infrastructure.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace Backend.Features.Tasks;

[ApiController]
[Route("api/tasks")]
[Authorize]
[EnableRateLimiting(RateLimitingExtensions.TasksReadPolicy)]
public sealed partial class TasksController(AppDbContext db) : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;
    private const string SortRelevant = "relevant";

    [HttpGet]
    public async Task<IActionResult> ListAsync(
        [FromQuery] string? status,
        [FromQuery] Guid? categoryId,
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] double? radiusMeters,
        [FromQuery] string? q = null,
        [FromQuery] string? compensationType = null,
        [FromQuery] DateTimeOffset? createdAfter = null,
        [FromQuery] string? sort = null,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        Point? proximityOrigin = null;
        var shouldPage = page.HasValue || pageSize.HasValue;
        var effectivePage = page.GetValueOrDefault(1);
        var effectivePageSize = pageSize.GetValueOrDefault(DefaultPageSize);
        var skip = 0;

        if (shouldPage)
        {
            if (effectivePage < 1)
            {
                ModelState.AddModelError(nameof(page), "Page must be greater than or equal to 1.");
            }

            if (effectivePageSize is < 1 or > MaxPageSize)
            {
                ModelState.AddModelError(nameof(pageSize), $"PageSize must be between 1 and {MaxPageSize}.");
            }

            var offset = ((long)effectivePage - 1) * effectivePageSize;
            if (offset > int.MaxValue)
            {
                ModelState.AddModelError(nameof(page), "Page is too large for the requested page size.");
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            skip = (int)offset;
        }

        if (latitude.HasValue || longitude.HasValue || radiusMeters.HasValue)
        {
            if (!TryBuildProximityFilter(latitude, longitude, radiusMeters, ModelState, out proximityOrigin))
            {
                return ValidationProblem(ModelState);
            }
        }

        var query = db.Tasks
            .AsNoTracking()
            .Include(task => task.SeekerProfile)
            .Include(task => task.AcceptedHelperProfile)
            .Include(task => task.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TryParseTaskStatus(status, out var taskStatus))
            {
                ModelState.AddModelError(nameof(status), $"Invalid status value '{status}'.");
                return ValidationProblem(ModelState);
            }

            query = query.Where(task => task.Status == taskStatus);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(task => task.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(compensationType))
        {
            if (!TryParseCompensationType(compensationType, out var parsedCompensation))
            {
                ModelState.AddModelError(
                    nameof(compensationType),
                    $"Invalid compensation type '{compensationType}'.");
                return ValidationProblem(ModelState);
            }

            query = parsedCompensation is CompensationType.Points or CompensationType.Voluntary
                ? query.Where(task => task.CompensationType == CompensationType.Points
                    || task.CompensationType == CompensationType.Voluntary)
                : query.Where(task => task.CompensationType == parsedCompensation);
        }

        if (createdAfter.HasValue)
        {
            var cutoff = createdAfter.Value;
            query = query.Where(task => task.CreatedAt >= cutoff);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            // Escape PostgreSQL LIKE wildcards in the user input so they're
            // treated as literal characters. The escape char is backslash;
            // we escape \ first so it doesn't double-process the % and _
            // we add immediately after.
            var trimmed = q.Trim();
            var escaped = trimmed
                .Replace(@"\", @"\\", StringComparison.Ordinal)
                .Replace("%", @"\%", StringComparison.Ordinal)
                .Replace("_", @"\_", StringComparison.Ordinal);
            var pattern = $"%{escaped}%";
            query = query.Where(task =>
                EF.Functions.ILike(task.Title, pattern, @"\")
                || EF.Functions.ILike(task.Description, pattern, @"\"));
        }

        // Apply hard radius filter regardless of sort order.
        if (proximityOrigin is not null)
        {
            var radius = radiusMeters.GetValueOrDefault();
            query = query.Where(task => task.Location != null && task.Location.IsWithinDistance(proximityOrigin, radius));
        }

        IQueryable<CommunityTask> orderedQuery;
        var effectiveSort = sort?.ToLowerInvariant();
        if (effectiveSort == SortRelevant)
        {
            var userId = GetCurrentUserId();
            var userLocation = userId.HasValue
                ? (await db.Profiles
                    .AsNoTracking()
                    .Select(p => new { p.UserId, p.Location })
                    .FirstOrDefaultAsync(p => p.UserId == userId.Value, cancellationToken))?.Location
                : null;

            if (proximityOrigin is not null)
            {
                orderedQuery = query
                    .OrderBy(task => task.Location!.Distance(proximityOrigin))
                    .ThenByDescending(task => task.CreatedAt);
            }
            else if (userLocation is not null)
            {
                orderedQuery = query
                    .OrderBy(task => task.Location != null ? (double?)task.Location.Distance(userLocation) : null)
                    .ThenByDescending(task => task.CreatedAt);
            }
            else
            {
                orderedQuery = query.OrderByDescending(task => task.CreatedAt);
            }
        }
        else if (proximityOrigin is not null)
        {
            orderedQuery = query
                .OrderBy(task => task.Location!.Distance(proximityOrigin))
                .ThenByDescending(task => task.CreatedAt);
        }
        else
        {
            orderedQuery = query.OrderByDescending(task => task.CreatedAt);
        }

        if (shouldPage)
        {
            orderedQuery = orderedQuery
                .Skip(skip)
                .Take(effectivePageSize);
        }

        var tasks = await orderedQuery
            .ToListAsync(cancellationToken);

        return Ok(tasks.Select(TaskResponse.FromTask));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await db.Tasks
            .AsNoTracking()
            .Include(task => task.SeekerProfile)
            .Include(task => task.AcceptedHelperProfile)
            .Include(task => task.Category)
            .FirstOrDefaultAsync(task => task.Id == id, cancellationToken);

        if (task is null)
        {
            return NotFound();
        }

        return Ok(TaskResponse.FromTask(task));
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitingExtensions.TasksWritePolicy)]
    public async Task<IActionResult> CreateAsync(
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.HasClaim("email_verified", "true"))
        {
            return Problem(
                title: "Email Not Verified",
                detail: "Please verify your email address to post tasks.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var profile = await GetCurrentProfileAsync(cancellationToken);
        if (profile is null)
        {
            return Unauthorized();
        }

        if (!FieldValidator.ValidateTrimmedString(ModelState, nameof(request.Title), request.Title, 3, 160, out var title))
        {
            return ValidationProblem(ModelState);
        }

        var description = SanitizeDescription(request.Description);
        if (!ValidateDescriptionPlainText(description, nameof(request.Description)))
        {
            return ValidationProblem(ModelState);
        }

        var categoryExists = await db.Categories
            .AnyAsync(c => c.Id == request.CategoryId && c.IsActive, cancellationToken);
        if (!categoryExists)
        {
            ModelState.AddModelError(nameof(request.CategoryId), "Category not found or inactive.");
            return ValidationProblem(ModelState);
        }

        if (!TryParseCompensationType(request.CompensationType, out var compensationType))
        {
            ModelState.AddModelError(nameof(request.CompensationType), $"Invalid compensation type '{request.CompensationType}'.");
            return ValidationProblem(ModelState);
        }

        if (compensationType == CompensationType.Paid && request.CompensationAmount is null)
        {
            ModelState.AddModelError(nameof(request.CompensationAmount), "CompensationAmount is required for Paid tasks.");
            return ValidationProblem(ModelState);
        }

        if (compensationType != CompensationType.Paid && request.CompensationAmount is not null)
        {
            ModelState.AddModelError(
                nameof(request.CompensationAmount),
                "CompensationAmount only applies to Paid tasks.");
            return ValidationProblem(ModelState);
        }

        if (request.Latitude.HasValue != request.Longitude.HasValue)
        {
            ModelState.AddModelError("Location", "Both Latitude and Longitude must be provided together.");
            return ValidationProblem(ModelState);
        }

        var task = new CommunityTask
        {
            Id = Guid.NewGuid(),
            SeekerProfileId = profile.Id,
            CategoryId = request.CategoryId,
            Title = title,
            Description = description,
            Location = BuildLocation(request.Latitude, request.Longitude),
            LocationText = StringUtilities.Normalize(request.LocationText),
            CompensationType = compensationType,
            CompensationAmount = compensationType == CompensationType.Paid ? request.CompensationAmount : null,
            Status = DomainTaskStatus.Open
        };

        db.Tasks.Add(task);
        db.AddActivityEvent(
            profile.UserId,
            profile.Id,
            ActivityEventType.TaskPosted,
            nameof(CommunityTask),
            task.Id,
            new { task.CategoryId, task.CompensationType });
        await db.SaveChangesAsync(cancellationToken);

        var created = await db.Tasks
            .AsNoTracking()
            .Include(t => t.SeekerProfile)
            .Include(t => t.AcceptedHelperProfile)
            .Include(t => t.Category)
            .FirstAsync(t => t.Id == task.Id, cancellationToken);

        return CreatedAtAction("GetById", new { id = task.Id }, TaskResponse.FromTask(created));
    }

    [HttpPatch("{id:guid}")]
    [EnableRateLimiting(RateLimitingExtensions.TasksWritePolicy)]
    public async Task<IActionResult> UpdateAsync(
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await GetCurrentProfileAsync(cancellationToken);
        if (profile is null)
        {
            return Unauthorized();
        }

        var task = await db.Tasks
            .Include(t => t.SeekerProfile)
            .Include(t => t.AcceptedHelperProfile)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (task is null)
        {
            return NotFound();
        }

        if (task.SeekerProfileId != profile.Id)
        {
            return Forbid();
        }

        // A status change request is only ever a cancellation (enforced below).
        // Validate the transition against the FSM up front so an invalid
        // transition fails with 400 before any field-level edits are applied.
        if (request.Status is not null)
        {
            if (!string.Equals(request.Status, nameof(DomainTaskStatus.Cancelled), StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(request.Status), "Status can only be changed to 'Cancelled'.");
                return ValidationProblem(ModelState);
            }

            if (!TaskStatusTransitions.CanTransition(task.Status, DomainTaskStatus.Cancelled))
            {
                return Problem(
                    title: "Invalid task status transition",
                    detail: $"A task with status '{task.Status}' cannot be transitioned to 'Cancelled'.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }
        else if (task.Status is DomainTaskStatus.Completed or DomainTaskStatus.Cancelled)
        {
            return Problem(
                title: "Task cannot be modified",
                detail: "Completed or cancelled tasks cannot be updated.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var anyChange = false;

        if (request.Title is not null)
        {
            if (!FieldValidator.ValidateTrimmedString(ModelState, nameof(request.Title), request.Title, 3, 160, out var title))
            {
                return ValidationProblem(ModelState);
            }

            task.Title = title;
            anyChange = true;
        }

        if (request.Description is not null)
        {
            var description = SanitizeDescription(request.Description);
            if (!ValidateDescriptionPlainText(description, nameof(request.Description)))
            {
                return ValidationProblem(ModelState);
            }

            task.Description = description;
            anyChange = true;
        }

        if (request.CategoryId.HasValue)
        {
            var category = await db.Categories
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId.Value && c.IsActive, cancellationToken);
            if (category is null)
            {
                ModelState.AddModelError(nameof(request.CategoryId), "Category not found or inactive.");
                return ValidationProblem(ModelState);
            }

            task.Category = category;
            anyChange = true;
        }

        if (request.CompensationType is not null)
        {
            if (!TryParseCompensationType(request.CompensationType, out var compensationType))
            {
                ModelState.AddModelError(nameof(request.CompensationType), $"Invalid compensation type '{request.CompensationType}'.");
                return ValidationProblem(ModelState);
            }

            task.CompensationType = compensationType;
            if (compensationType != CompensationType.Paid)
            {
                task.CompensationAmount = null;
            }

            anyChange = true;
        }

        if (request.CompensationAmount is not null)
        {
            if (task.CompensationType != CompensationType.Paid)
            {
                ModelState.AddModelError(
                    nameof(request.CompensationAmount),
                    "CompensationAmount only applies to Paid tasks.");
                return ValidationProblem(ModelState);
            }

            task.CompensationAmount = request.CompensationAmount;
            anyChange = true;
        }

        if (task.CompensationType == CompensationType.Paid && task.CompensationAmount is null)
        {
            ModelState.AddModelError(nameof(request.CompensationAmount), "CompensationAmount is required for Paid tasks.");
            return ValidationProblem(ModelState);
        }

        if (request.LocationText is not null)
        {
            task.LocationText = StringUtilities.Normalize(request.LocationText);
            anyChange = true;
        }

        if (request.Latitude.HasValue || request.Longitude.HasValue)
        {
            if (request.Latitude.HasValue != request.Longitude.HasValue)
            {
                ModelState.AddModelError("Location", "Both Latitude and Longitude must be provided together.");
                return ValidationProblem(ModelState);
            }

            task.Location = BuildLocation(request.Latitude, request.Longitude);
            anyChange = true;
        }

        if (request.Status is not null)
        {
            // Target status and transition validity already verified above.
            var oldStatus = task.Status;
            var cancelledAt = DateTimeOffset.UtcNow;
            var cancellationReason = StringUtilities.Normalize(request.CancellationReason);

            task.Status = DomainTaskStatus.Cancelled;
            task.CancelledAt = cancelledAt;
            task.CancellationReason = cancellationReason;
            db.TaskStatusHistory.Add(new TaskStatusHistoryEntry
            {
                TaskId = task.Id,
                OldStatus = oldStatus,
                NewStatus = DomainTaskStatus.Cancelled,
                ChangedByProfileId = profile.Id,
                Reason = cancellationReason
            });
            db.AddActivityEvent(
                profile.UserId,
                profile.Id,
                ActivityEventType.TaskCancelled,
                nameof(CommunityTask),
                task.Id,
                new { OldStatus = oldStatus.ToString(), Reason = cancellationReason });
            db.AddAuditEvent(
                profile.UserId,
                "task.cancelled",
                nameof(CommunityTask),
                task.Id,
                new
                {
                    OldStatus = oldStatus.ToString(),
                    Reason = cancellationReason,
                    task.AcceptedHelperProfileId,
                    CancelledAt = cancelledAt
                });
            anyChange = true;
        }

        if (anyChange)
        {
            task.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(TaskResponse.FromTask(task));
    }
}
