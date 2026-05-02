using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Admin.Categories;

public sealed record UpdateCategoryRequest(
    [Required]
    [StringLength(120)]
    string Name,
    [Required]
    [StringLength(64)]
    string Icon,
    [StringLength(500)]
    string? Description,
    int SortOrder);
