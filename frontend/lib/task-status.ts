/**
 * Normalises a task status string to a stable snake_case form, e.g.
 * "InProgress" -> "in_progress", "PendingApproval" -> "pending_approval".
 * The regex MUST run before lowercasing — lowercasing first removes the
 * uppercase letters the regex looks for and would silently collapse
 * multi-word statuses into a single word.
 */
export function normalizeTaskStatus(status: string): string {
  return status.replace(/([a-z])([A-Z])/g, "$1_$2").toLowerCase()
}
