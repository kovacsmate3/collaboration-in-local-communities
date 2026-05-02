namespace Backend.Features.Admin.Categories;

public sealed record AdminCategoryResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    int SortOrder,
    bool IsActive);
