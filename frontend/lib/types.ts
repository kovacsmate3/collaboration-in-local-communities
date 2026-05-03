/**
 * Domain types for the local-collaboration platform.
 *
 * Keep this file as the single source of truth for shared domain models.
 * Backend integration should later mirror these shapes (or be generated
 * from an OpenAPI schema and re-exported from here).
 */

export type UserRole = "seeker" | "helper"

/** Every user can act as both a Seeker and a Helper (US-06, dual-role default). */
export interface User {
  id: string
  name: string
  avatarUrl?: string
  bio?: string
  workplace?: string
  position?: string
  location?: string
  skills: string[]
  /** Aggregate reputation score derived from reviews. */
  reputation: Reputation
  verified: boolean
  joinedAt: string
}

export interface Reputation {
  points: number
  averageRating: number
  reviewCount: number
  completedTasks: number
}

export type TaskCategory =
  | "moving"
  | "tutoring"
  | "household"
  | "petcare"
  | "tools"
  | "tech"
  | "errands"
  | "other"

export interface Category {
  id: string
  code: string
  name: string
  description?: string | null
  /** Hugeicons identifier rendered through `getIconForKey`. */
  icon: string
}

export type CompensationType = "paid" | "voluntary" | "barter"

export interface Compensation {
  type: CompensationType
  /** Optional amount/details depending on the compensation type. */
  amount?: number
  currency?: string
  /** What the seeker offers in exchange for a barter. */
  barterOffer?: string
}

export type TaskStatus =
  | "open"
  | "in_progress"
  | "completed"
  | "reviewed"
  | "cancelled"

export interface Task {
  id: string
  title: string
  description: string
  category: TaskCategory
  /** Category icon identifier rendered through `getIconForKey`. */
  icon: string
  location: string
  compensation: Compensation
  status: TaskStatus
  createdAt: string
  /** The Seeker who posted the task. */
  seeker: Pick<User, "id" | "name" | "avatarUrl" | "reputation">
  /** Helper assigned once the task moves to in_progress. */
  helper?: Pick<User, "id" | "name" | "avatarUrl" | "reputation">
}

export interface Review {
  id: string
  taskId: string
  authorId: string
  authorName: string
  authorAvatarUrl?: string
  targetUserId: string
  rating: number
  comment: string
  createdAt: string
}

export interface ChatPreview {
  id: string
  participant: Pick<User, "id" | "name" | "avatarUrl">
  taskTitle: string
  taskId: string
  lastMessage: string
  lastMessageAt: string
  unreadCount: number
}

export type ChatMessageAuthor = "self" | "other" | "system"

export interface ChatMessage {
  id: string
  author: ChatMessageAuthor
  content: string
  sentAt: string
}
