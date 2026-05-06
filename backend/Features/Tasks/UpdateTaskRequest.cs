using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Tasks;

public sealed record UpdateTaskRequest(
    [StringLength(160)]
    string? Title,

    [StringLength(3000)]
    string? Description,

    Guid? CategoryId,

    [StringLength(32)]
    string? CompensationType,

    [Range(0, 9_999_999_999.99)]
    decimal? CompensationAmount,

    [StringLength(300)]
    string? LocationText,

    [Range(-90, 90)]
    double? Latitude,

    [Range(-180, 180)]
    double? Longitude,

    [StringLength(32)]
    string? Status,

    [StringLength(1000)]
    string? CancellationReason);
