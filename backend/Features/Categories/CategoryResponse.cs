namespace Backend.Features.Categories;

public sealed record CategoryResponse(
    Guid Id,
    string Code,
    string Name,
    string Icon,
    string? Description);
