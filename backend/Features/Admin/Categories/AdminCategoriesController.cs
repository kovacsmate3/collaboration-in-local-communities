using Backend.Application.Categories;
using Backend.Domain.Entities;
using Backend.Features.Categories;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Backend.Features.Admin.Categories;

// [Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/categories")]
public sealed class AdminCategoriesController(
    AppDbContext db,
    IOutputCacheStore outputCacheStore)
    : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .FirstOrDefaultAsync(category => category.Id == id, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(category));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var code = Normalize(request.Code);
        var name = Normalize(request.Name);
        var icon = Normalize(request.Icon);
        var description = NormalizeOptional(request.Description);

        if (!ValidateRequired(nameof(request.Code), code) ||
            !ValidateRequired(nameof(request.Name), name) ||
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

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateCategoryCode(exception))
        {
            return DuplicateCodeConflict(code);
        }

        await EvictCategoryListAsync(cancellationToken);

        var response = ToResponse(category);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = category.Id }, response);
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

        var name = Normalize(request.Name);
        var icon = Normalize(request.Icon);
        var description = NormalizeOptional(request.Description);

        if (!ValidateRequired(nameof(request.Name), name) ||
            !ValidateIcon(nameof(request.Icon), icon))
        {
            return ValidationProblem(ModelState);
        }

        category.Name = name;
        category.Icon = icon;
        category.Description = description;
        category.SortOrder = request.SortOrder;
        category.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await EvictCategoryListAsync(cancellationToken);

        return Ok(ToResponse(category));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .FirstOrDefaultAsync(category => category.Id == id, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        if (!category.IsActive)
        {
            return NoContent();
        }

        category.IsActive = false;
        category.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await EvictCategoryListAsync(cancellationToken);

        return NoContent();
    }

    private static AdminCategoryResponse ToResponse(Category category)
    {
        return new AdminCategoryResponse(
            category.Id,
            category.Code,
            category.Name,
            category.Icon,
            category.Description,
            category.SortOrder,
            category.IsActive);
    }

    private static string Normalize(string value)
    {
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static ConflictObjectResult DuplicateCodeConflict(string code)
    {
        return new ConflictObjectResult(new ProblemDetails
        {
            Title = "Duplicate category code",
            Detail = $"Category code '{code}' already exists.",
            Status = StatusCodes.Status409Conflict
        });
    }

    private static bool IsDuplicateCategoryCode(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException &&
            postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
            postgresException.ConstraintName == "ux_categories_code";
    }

    private bool ValidateRequired(string fieldName, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        ModelState.AddModelError(fieldName, $"{fieldName} is required.");
        return false;
    }

    private bool ValidateIcon(string fieldName, string icon)
    {
        if (!ValidateRequired(fieldName, icon))
        {
            return false;
        }

        if (AllowedCategoryIcons.Values.Contains(icon))
        {
            return true;
        }

        ModelState.AddModelError(fieldName, $"{fieldName} is not an allowed category icon.");
        return false;
    }

    private ValueTask EvictCategoryListAsync(CancellationToken cancellationToken)
    {
        return outputCacheStore.EvictByTagAsync(CategoriesCache.Tag, cancellationToken);
    }
}
