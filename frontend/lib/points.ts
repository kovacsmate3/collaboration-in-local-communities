/**
 * Client-side mirror of the backend helper-points reward policy
 * (`Backend.Application.TaskCompletion.CategoryWeightedTaskCompletionPointsCalculator`
 * and `Backend.Application.Reviews.ReviewQualityBonusCalculator`).
 *
 * These constants exist only to drive explanatory UI copy — the server is the
 * source of truth for the amounts actually awarded. Keep them in sync if the
 * backend constants change.
 */

/** Base points for completing a task, before the category weight multiplier. */
export const POINTS_BASE_REWARD = 10

/** Extra points added when the task is voluntary (no other compensation). */
export const POINTS_VOLUNTARY_BONUS = 5

/** Highest review rating a seeker can give. */
const MAX_REVIEW_RATING = 5

/** Ratings at or below this earn no quality bonus. */
const REVIEW_NEUTRAL_RATING = 3

/** Bonus points per review star above the neutral rating. */
const POINTS_PER_REVIEW_STAR = 3

/** Smallest quality bonus that still awards points — a 4-star review. */
export const POINTS_REVIEW_BONUS_MIN = POINTS_PER_REVIEW_STAR

/** Largest quality bonus — a 5-star review. */
export const POINTS_REVIEW_BONUS_MAX =
  (MAX_REVIEW_RATING - REVIEW_NEUTRAL_RATING) * POINTS_PER_REVIEW_STAR
