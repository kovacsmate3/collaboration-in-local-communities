using Backend.Domain.Entities;

namespace Backend.Features.Admin.Categories;

public sealed record AdminCategoryResponse(
    Guid Id,
    string Code,
    string Name,
    string Icon,
    string? Description,
    int SortOrder,
    bool IsActive)
{
    /// <summary>
    /// Creates an <see cref="AdminCategoryResponse"/> from a <see cref="Category"/> entity.
    /// </summary>
    public static AdminCategoryResponse FromCategory(Category category)
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
}
