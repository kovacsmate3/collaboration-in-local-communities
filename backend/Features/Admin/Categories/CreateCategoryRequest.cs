using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Admin.Categories;

public sealed record CreateCategoryRequest(
    [property: Required]
    [property: StringLength(64)]
    string Code,
    [property: Required]
    [property: StringLength(120)]
    string Name,
    [property: Required]
    [property: StringLength(64)]
    string Icon,
    [property: StringLength(500)]
    string? Description,
    int SortOrder);
