using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Reviews;

public sealed class PostReviewRequest
{
    [Range(1, 5)]
    public int Rating { get; init; }

    [MaxLength(2000)]
    public string? Comment { get; init; }
}
