import type { ChatMessage, ChatPreview, Review, Task, User } from "@/lib/types"

/**
 * Static mock data for the skeleton UI.
 *
 * Once the backend is in place, replace each of these helpers with real
 * data-fetching functions (server actions, RSC fetches, or a query layer)
 * keeping the same return types so the UI does not need to change.
 */

export const currentUser: User = {
  id: "u_self",
  name: "Anna Kovács",
  avatarUrl: undefined,
  bio: "CS student. Happy to help with tech setup and tutoring around the dorms.",
  workplace: "ELTE",
  position: "Student",
  location: "Budapest, District XI",
  skills: ["JavaScript", "Math tutoring", "Tech support"],
  reputation: {
    points: 240,
    averageRating: 4.8,
    reviewCount: 12,
    completedTasks: 9,
  },
  verified: true,
  joinedAt: "2025-09-01T10:00:00Z",
}

const otherUsers: User[] = [
  {
    id: "u_marta",
    name: "Márta Szabó",
    bio: "Teacher and dog person.",
    workplace: "Local primary school",
    position: "Teacher",
    location: "Budapest, District V",
    skills: ["Pet sitting", "Childcare"],
    reputation: {
      points: 410,
      averageRating: 4.9,
      reviewCount: 31,
      completedTasks: 28,
    },
    verified: true,
    joinedAt: "2025-05-12T10:00:00Z",
  },
  {
    id: "u_david",
    name: "Dávid Nagy",
    bio: "Hobby mechanic, tools to share.",
    workplace: "Freelance",
    position: "Designer",
    location: "Budapest, District VII",
    skills: ["Tools", "Furniture assembly"],
    reputation: {
      points: 95,
      averageRating: 4.5,
      reviewCount: 6,
      completedTasks: 5,
    },
    verified: false,
    joinedAt: "2025-11-03T10:00:00Z",
  },
]

const toSeeker = (u: User) => ({
  id: u.id,
  name: u.name,
  avatarUrl: u.avatarUrl,
  reputation: u.reputation,
})

export const mockTasks: Task[] = [
  {
    id: "t_001",
    title: "Help moving a bookshelf to 3rd floor",
    description:
      "Need one extra pair of hands for ~30 minutes. The building has no elevator. Pizza included.",
    category: "moving",
    location: "Budapest, District XI",
    compensation: { type: "barter", barterOffer: "Pizza + a beer" },
    status: "open",
    createdAt: "2026-04-26T16:30:00Z",
    seeker: toSeeker(otherUsers[0]),
  },
  {
    id: "t_002",
    title: "Math tutoring for high-school finals",
    description:
      "Looking for help with calculus and linear algebra, two sessions before the exam.",
    category: "tutoring",
    location: "Online",
    compensation: { type: "paid", amount: 8000, currency: "HUF" },
    status: "open",
    createdAt: "2026-04-26T08:10:00Z",
    seeker: toSeeker(otherUsers[1]),
  },
  {
    id: "t_003",
    title: "Dog sitting this weekend",
    description: "Friendly border collie, two short walks per day. Sat–Sun.",
    category: "petcare",
    location: "Budapest, District V",
    compensation: { type: "voluntary" },
    status: "open",
    createdAt: "2026-04-25T19:00:00Z",
    seeker: toSeeker(otherUsers[0]),
  },
  {
    id: "t_004",
    title: "Borrow a power drill for an afternoon",
    description: "Just need it for 2-3 hours to hang shelves. Will return same day.",
    category: "tools",
    location: "Budapest, District VII",
    compensation: { type: "voluntary" },
    status: "in_progress",
    createdAt: "2026-04-24T11:45:00Z",
    seeker: toSeeker(otherUsers[1]),
    helper: toSeeker(currentUser),
  },
]

export const mockReviews: Review[] = [
  {
    id: "r_001",
    taskId: "t_old_1",
    authorId: "u_marta",
    authorName: "Márta Szabó",
    targetUserId: currentUser.id,
    rating: 5,
    comment: "Anna was super helpful and on time. Highly recommended.",
    createdAt: "2026-03-15T12:00:00Z",
  },
  {
    id: "r_002",
    taskId: "t_old_2",
    authorId: "u_david",
    authorName: "Dávid Nagy",
    targetUserId: currentUser.id,
    rating: 4,
    comment: "Solid help with the laptop setup, would ask again.",
    createdAt: "2026-02-04T17:20:00Z",
  },
]

export const mockChats: ChatPreview[] = [
  {
    id: "c_001",
    participant: {
      id: "u_marta",
      name: "Márta Szabó",
    },
    taskId: "t_001",
    taskTitle: "Help moving a bookshelf to 3rd floor",
    lastMessage: "Great, see you at 5!",
    lastMessageAt: "2026-04-27T09:14:00Z",
    unreadCount: 0,
  },
  {
    id: "c_002",
    participant: {
      id: "u_david",
      name: "Dávid Nagy",
    },
    taskId: "t_004",
    taskTitle: "Borrow a power drill for an afternoon",
    lastMessage: "I can drop it off tomorrow morning, does that work?",
    lastMessageAt: "2026-04-26T20:02:00Z",
    unreadCount: 2,
  },
]

export const mockMessages: ChatMessage[] = [
  {
    id: "m_001",
    author: "system",
    content: "Chat opened for: Help moving a bookshelf to 3rd floor",
    sentAt: "2026-04-26T17:00:00Z",
  },
  {
    id: "m_002",
    author: "other",
    content: "Hi! Thanks for accepting. Are you free around 5pm tomorrow?",
    sentAt: "2026-04-26T17:01:00Z",
  },
  {
    id: "m_003",
    author: "self",
    content: "Yep, that works. I'll bring gloves.",
    sentAt: "2026-04-26T17:05:00Z",
  },
  {
    id: "m_004",
    author: "other",
    content: "Great, see you at 5!",
    sentAt: "2026-04-27T09:14:00Z",
  },
]

export function getTaskById(id: string): Task | undefined {
  return mockTasks.find((t) => t.id === id)
}

export function getChatById(id: string): ChatPreview | undefined {
  return mockChats.find((c) => c.id === id)
}
