import type { QueryClient, QueryKey } from "@tanstack/react-query"

const cacheRoots = {
  adminAnalytics: ["admin", "analytics"] as const,
  adminAuditLog: ["admin", "audit-log"] as const,
  adminCategories: ["admin", "categories"] as const,
  adminSkills: ["admin", "skills"] as const,
  categories: ["categories"] as const,
  conversations: ["conversations"] as const,
  profiles: ["profiles"] as const,
  skills: ["skills"] as const,
  tasks: ["tasks"] as const,
}

function invalidate(qc: QueryClient, queryKey: QueryKey) {
  void qc.invalidateQueries({ queryKey })
}

export function invalidateAdminAuditLog(qc: QueryClient) {
  invalidate(qc, cacheRoots.adminAuditLog)
}

export function invalidateAdminAnalytics(qc: QueryClient) {
  invalidate(qc, cacheRoots.adminAnalytics)
}

export function invalidateCategoryData(qc: QueryClient) {
  invalidate(qc, cacheRoots.adminCategories)
  invalidate(qc, cacheRoots.categories)
  invalidate(qc, cacheRoots.tasks)
  invalidateAdminAnalytics(qc)
  invalidateAdminAuditLog(qc)
}

export function invalidateConversationData(qc: QueryClient) {
  invalidate(qc, cacheRoots.conversations)
}

export function invalidateProfileData(qc: QueryClient) {
  invalidate(qc, cacheRoots.profiles)
}

export function invalidateSkillData(qc: QueryClient) {
  invalidate(qc, cacheRoots.adminSkills)
  invalidate(qc, cacheRoots.skills)
  invalidateProfileData(qc)
  invalidateAdminAuditLog(qc)
}

export function invalidateTaskData(qc: QueryClient) {
  invalidate(qc, cacheRoots.tasks)
  invalidateAdminAnalytics(qc)
}

export function invalidateTaskParticipantData(qc: QueryClient) {
  invalidateTaskData(qc)
  invalidateProfileData(qc)
}
