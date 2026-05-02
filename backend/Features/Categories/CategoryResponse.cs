namespace Backend.Features.Categories;

public sealed record CategoryResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    int SortOrder);
