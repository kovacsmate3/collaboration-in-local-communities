using JetBrains.Annotations;

namespace Backend.Features.Categories;

[PublicAPI]
public sealed record CategoryResponse(
    Guid Id,
    string Code,
    string Name,
    string Icon,
    string? Description);
