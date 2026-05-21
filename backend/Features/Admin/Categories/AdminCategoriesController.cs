using Backend.Common;
using Backend.Domain.Entities;
using Backend.Infrastructure.Persistence;
using Backend.Infrastructure.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Admin.Categories;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/categories")]
public sealed partial class AdminCategoriesController(
    AppDbContext db,
    IOutputCacheStore outputCacheStore)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        var categoryEntities = await db.Categories
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .ToListAsync(cancellationToken);

        var categories = categoryEntities
            .Select(category => AdminCategoryResponse.FromCategory(category))
            .ToList();

        return Ok(categories);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .FirstOrDefaultAsync(category => category.Id == id, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        return Ok(AdminCategoryResponse.FromCategory(category));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var code = StringUtilities.Normalize(request.Code);
        var name = StringUtilities.Normalize(request.Name);
        var icon = StringUtilities.Normalize(request.Icon);
        var description = StringUtilities.Normalize(request.Description);

        if (!FieldValidator.ValidateRequired(ModelState, nameof(request.Code), code) ||
            !FieldValidator.ValidateRequired(ModelState, nameof(request.Name), name) ||
            !ValidateIcon(nameof(request.Icon), icon))
        {
            return ValidationProblem(ModelState);
        }

        var codeExists = await db.Categories
            .AnyAsync(category => category.Code == code, cancellationToken);
        if (codeExists)
        {
            return DuplicateCodeConflict(code);
        }

        var category = new Category
        {
            // Ensure the entity has an ID immediately so callers receive it
            // even before the DB generates values on insert.
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Icon = icon,
            Description = description,
            SortOrder = request.SortOrder,
            IsActive = true
        };

        db.Categories.Add(category);
        AddAuditEvent(GetActorUserId(), "admin.category_created", "Category", category.Id, new { category.Code, category.Name });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (PostgresExceptionHelpers.IsDuplicateCategoryCode(exception))
        {
            return DuplicateCodeConflict(code);
        }

        await EvictCategoryListAsync(cancellationToken);

        var response = AdminCategoryResponse.FromCategory(category);
        return CreatedAtAction("GetById", new { id = category.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .FirstOrDefaultAsync(category => category.Id == id, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        var name = StringUtilities.Normalize(request.Name);
        var icon = StringUtilities.Normalize(request.Icon);
        var description = StringUtilities.Normalize(request.Description);

        if (!FieldValidator.ValidateRequired(ModelState, nameof(request.Name), name) ||
            !ValidateIcon(nameof(request.Icon), icon))
        {
            return ValidationProblem(ModelState);
        }

        category.Name = name;
        category.Icon = icon;
        category.Description = description;
        category.SortOrder = request.SortOrder;
        category.UpdatedAt = DateTimeOffset.UtcNow;

        AddAuditEvent(GetActorUserId(), "admin.category_updated", "Category", category.Id, new { category.Code, category.Name });

        await db.SaveChangesAsync(cancellationToken);
        await EvictCategoryListAsync(cancellationToken);

        return Ok(AdminCategoryResponse.FromCategory(category));
    }

    /// <summary>
    /// Permanently removes a category. Returns 409 Conflict if any task still
    /// references it (the FK is configured with <c>OnDelete(Restrict)</c>);
    /// callers should deactivate instead in that case.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .FirstOrDefaultAsync(category => category.Id == id, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        AddAuditEvent(GetActorUserId(), "admin.category_deleted", "Category", category.Id, new { category.Code, category.Name });
        db.Categories.Remove(category);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (PostgresExceptionHelpers.IsForeignKeyViolation(exception))
        {
            return CategoryInUseConflict(category);
        }

        await EvictCategoryListAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Soft-deletes (deactivates) a category. Idempotent — calling on an
    /// already-inactive category is a no-op and returns 204.
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        return await SetActiveAsync(id, isActive: false, cancellationToken);
    }

    /// <summary>
    /// Reactivates a previously deactivated category. Idempotent — calling on
    /// an already-active category is a no-op and returns 204.
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateAsync(Guid id, CancellationToken cancellationToken)
    {
        return await SetActiveAsync(id, isActive: true, cancellationToken);
    }

    private async Task<IActionResult> SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .FirstOrDefaultAsync(category => category.Id == id, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        if (category.IsActive == isActive)
        {
            return NoContent();
        }

        category.IsActive = isActive;
        category.UpdatedAt = DateTimeOffset.UtcNow;

        var eventType = isActive ? "admin.category_activated" : "admin.category_deactivated";
        AddAuditEvent(GetActorUserId(), eventType, "Category", category.Id, new { category.Code, category.Name });

        await db.SaveChangesAsync(cancellationToken);
        await EvictCategoryListAsync(cancellationToken);

        return NoContent();
    }
}
