using Backend.Domain.Entities;
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
    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var code = Normalize(request.Code);
        var name = Normalize(request.Name);
        var description = NormalizeOptional(request.Description);

        if (!ValidateRequired(nameof(request.Code), code) ||
            !ValidateRequired(nameof(request.Name), name))
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
            Code = code,
            Name = name,
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
        return Created($"/api/admin/categories/{category.Id}", response);
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
        var description = NormalizeOptional(request.Description);

        if (!ValidateRequired(nameof(request.Name), name))
        {
            return ValidationProblem(ModelState);
        }

        category.Name = name;
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

    private ValueTask EvictCategoryListAsync(CancellationToken cancellationToken)
    {
        return outputCacheStore.EvictByTagAsync(Backend.Features.Categories.CategoriesCache.Tag, cancellationToken);
    }
}
