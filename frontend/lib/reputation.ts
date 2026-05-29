/**
 * Tier identifiers shown next to the reputation score. Order matters —
 * `tierForScore` walks the list and returns the highest tier the score
 * qualifies for.
 */
export type ReputationTier = "newcomer" | "bronze" | "silver" | "gold"

/** Weight applied to each completed task in the reputation score formula. */
export const COMPLETED_TASK_WEIGHT = 10
/** Weight applied to (averageRating × reviewCount) in the reputation score formula. */
export const REVIEW_QUALITY_WEIGHT = 2

interface TierDefinition {
  id: ReputationTier
  label: string
  /** Minimum score (inclusive) required to reach this tier. */
  minScore: number
}

/**
 * Tier thresholds. Picked to match the score formula:
 *   bronze ≈ a few completed tasks or a handful of decent reviews,
 *   silver ≈ established helper with ~20 completions,
 *   gold   ≈ power user with sustained high ratings.
 */
export const REPUTATION_TIERS: readonly TierDefinition[] = [
  { id: "newcomer", label: "Newcomer", minScore: 0 },
  { id: "bronze", label: "Bronze", minScore: 50 },
  { id: "silver", label: "Silver", minScore: 250 },
  { id: "gold", label: "Gold", minScore: 750 },
]

export function tierForScore(score: number): TierDefinition {
  const safe = Number.isFinite(score) ? Math.max(0, score) : 0
  let current = REPUTATION_TIERS[0]
  for (const tier of REPUTATION_TIERS) {
    if (safe >= tier.minScore) {
      current = tier
    }
  }
  return current
}

export function nextTierForScore(score: number): TierDefinition | null {
  const safe = Number.isFinite(score) ? Math.max(0, score) : 0
  return REPUTATION_TIERS.find((tier) => safe < tier.minScore) ?? null
}

export interface ReputationScoreBreakdown {
  averageRating: number
  reviewCount: number
  completedTaskCount: number
  /** Contribution from completed tasks: completed × COMPLETED_TASK_WEIGHT. */
  completedComponent: number
  /** Contribution from review quality: rating × reviewCount × REVIEW_QUALITY_WEIGHT. */
  reviewComponent: number
  /** Final rounded score. */
  total: number
}

export function reputationScoreBreakdown(input: {
  score: number
  averageRating: number
  reviewCount: number
  completedTaskCount: number
}): ReputationScoreBreakdown {
  const rating = Number.isFinite(input.averageRating)
    ? Math.max(0, Math.min(5, input.averageRating))
    : 0
  const reviews = Math.max(0, input.reviewCount)
  const completed = Math.max(0, input.completedTaskCount)
  const completedComponent = completed * COMPLETED_TASK_WEIGHT
  const rawReviewComponent = rating * reviews * REVIEW_QUALITY_WEIGHT
  const total = Math.max(0, Math.round(completedComponent + rawReviewComponent))

  return {
    averageRating: rating,
    reviewCount: reviews,
    completedTaskCount: completed,
    completedComponent,
    reviewComponent: total - completedComponent,
    total,
  }
}
