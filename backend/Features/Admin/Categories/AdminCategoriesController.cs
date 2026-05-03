using Backend.Common;
using Backend.Domain.Entities;
using Backend.Infrastructure.Persistence;
using Backend.Infrastructure.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Admin.Categories;

// [Authorize(Roles = "Admin")]
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
        var categories = await db.Categories
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => AdminCategoryResponse.FromCategory(category))
            .ToListAsync(cancellationToken);

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

        await db.SaveChangesAsync(cancellationToken);
        await EvictCategoryListAsync(cancellationToken);

        return Ok(AdminCategoryResponse.FromCategory(category));
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
}
