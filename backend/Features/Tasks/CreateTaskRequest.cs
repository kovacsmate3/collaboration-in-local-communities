using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Tasks;

public sealed record CreateTaskRequest(
    [Required]
    [StringLength(160, MinimumLength = 3)]
    string Title,

    [Required]
    [StringLength(3000, MinimumLength = 10)]
    string Description,

    [Required]
    Guid CategoryId,

    [Required]
    [StringLength(32)]
    string CompensationType,

    [Range(0, 9_999_999_999.99)]
    decimal? CompensationAmount,

    [StringLength(300)]
    string? LocationText,

    [Range(-90, 90)]
    double? Latitude,

    [Range(-180, 180)]
    double? Longitude);
