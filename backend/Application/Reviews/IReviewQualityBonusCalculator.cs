namespace Backend.Application.Reviews;

/// <summary>
/// Calculates the extra points a helper earns when the seeker rates their completed work.
/// Server-owned and deterministic; the bonus is additive on top of the completion reward
/// and is never negative, so a poor review simply yields no bonus rather than a penalty.
/// </summary>
public interface IReviewQualityBonusCalculator
{
    /// <summary>
    /// Returns the points to award the helper for a seeker review of the given star rating.
    /// </summary>
    /// <param name="rating">The seeker's star rating of the helper (1-5).</param>
    /// <returns>A non-negative bonus; <c>0</c> means no bonus entry should be written.</returns>
    int Calculate(int rating);
}
