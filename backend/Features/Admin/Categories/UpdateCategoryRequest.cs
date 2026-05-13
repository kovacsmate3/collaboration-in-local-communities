using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Backend.Features.Admin.Categories;

[PublicAPI]
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
