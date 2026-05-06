using Backend.Common;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Persistence;
using Backend.Infrastructure.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace Backend.Features.Tasks;

[ApiController]
[Route("api/tasks")]
[Authorize]
public sealed partial class TasksController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListAsync(
        [FromQuery] string? status,
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
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

        var tasks = await query
            .OrderByDescending(task => task.CreatedAt)
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
    public async Task<IActionResult> CreateAsync(
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await GetCurrentProfileAsync(cancellationToken);
        if (profile is null)
        {
            return Unauthorized();
        }

        if (!FieldValidator.ValidateTrimmedString(ModelState, nameof(request.Title), request.Title, 3, 160, out var title)
            || !FieldValidator.ValidateTrimmedString(ModelState, nameof(request.Description), request.Description, 10, 3000, out var description))
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
            CompensationAmount = request.CompensationAmount,
            Status = DomainTaskStatus.Open
        };

        db.Tasks.Add(task);
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

        if (task.Status is DomainTaskStatus.Completed or DomainTaskStatus.Cancelled)
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
            if (!FieldValidator.ValidateTrimmedString(ModelState, nameof(request.Description), request.Description, 10, 3000, out var description))
            {
                return ValidationProblem(ModelState);
            }

            task.Description = description;
            anyChange = true;
        }

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await db.Categories
                .AnyAsync(c => c.Id == request.CategoryId.Value && c.IsActive, cancellationToken);
            if (!categoryExists)
            {
                ModelState.AddModelError(nameof(request.CategoryId), "Category not found or inactive.");
                return ValidationProblem(ModelState);
            }

            task.CategoryId = request.CategoryId.Value;
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
            anyChange = true;
        }

        if (request.CompensationAmount is not null)
        {
            task.CompensationAmount = request.CompensationAmount;
            anyChange = true;
        }

        if (request.LocationText is not null)
        {
            task.LocationText = StringUtilities.Normalize(request.LocationText);
            anyChange = true;
        }

        if (request.Latitude.HasValue || request.Longitude.HasValue)
        {
            task.Location = BuildLocation(request.Latitude, request.Longitude);
            anyChange = true;
        }

        if (request.Status is not null)
        {
            if (request.Status != nameof(DomainTaskStatus.Cancelled))
            {
                ModelState.AddModelError(nameof(request.Status), "Status can only be changed to 'Cancelled'.");
                return ValidationProblem(ModelState);
            }

            task.Status = DomainTaskStatus.Cancelled;
            task.CancelledAt = DateTimeOffset.UtcNow;
            task.CancellationReason = StringUtilities.Normalize(request.CancellationReason);
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
