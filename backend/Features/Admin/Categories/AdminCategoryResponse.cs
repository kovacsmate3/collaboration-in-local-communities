namespace Backend.Features.Admin.Categories;

public sealed record AdminCategoryResponse(
    Guid Id,
    string Code,
    string Name,
    string Icon,
    string? Description,
    int SortOrder,
    bool IsActive);
