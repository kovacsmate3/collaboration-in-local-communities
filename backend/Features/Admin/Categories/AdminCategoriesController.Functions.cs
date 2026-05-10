using Backend.Application.Categories;
using Backend.Domain.Entities;
using Backend.Features.Categories;
using Backend.Infrastructure.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Admin.Categories;

public sealed partial class AdminCategoriesController
{
    private static ConflictObjectResult DuplicateCodeConflict(string code)
    {
        return new ConflictObjectResult(new ProblemDetails
        {
            Title = "Duplicate category code",
            Detail = $"Category code '{code}' already exists.",
            Status = StatusCodes.Status409Conflict
        });
    }

    private static ConflictObjectResult CategoryInUseConflict(Category category)
    {
        return new ConflictObjectResult(new ProblemDetails
        {
            Title = "Category in use",
            Detail =
                $"Category '{category.Name}' (code '{category.Code}') cannot be deleted "
                + "because one or more tasks still reference it. Deactivate it instead "
                + "to hide it from the task creation flow without affecting existing tasks.",
            Status = StatusCodes.Status409Conflict
        });
    }

    private bool ValidateIcon(string fieldName, string icon)
    {
        if (!FieldValidator.ValidateRequired(ModelState, fieldName, icon))
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
