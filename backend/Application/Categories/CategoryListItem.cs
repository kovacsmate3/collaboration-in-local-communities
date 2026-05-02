namespace Backend.Application.Categories;

public sealed record CategoryListItem(
    Guid Id,
    string Code,
    string Name,
    string? Description);
