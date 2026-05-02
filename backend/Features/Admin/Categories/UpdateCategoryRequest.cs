using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Admin.Categories;

public sealed record UpdateCategoryRequest(
    [property: Required]
    [property: StringLength(120)]
    string Name,
    [property: StringLength(500)]
    string? Description,
    int SortOrder);
