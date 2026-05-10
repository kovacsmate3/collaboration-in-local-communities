using JetBrains.Annotations;

namespace Backend.Application.Categories;

[PublicAPI]
public sealed record CategoryListItem(
    Guid Id,
    string Code,
    string Name,
    string Icon,
    string? Description);
