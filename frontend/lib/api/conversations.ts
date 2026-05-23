import {
  useInfiniteQuery,
  useMutation,
  useQuery,
  useQueryClient,
  type InfiniteData,
} from "@tanstack/react-query"

import { apiClient } from "@/lib/api/client"

// ── Types ─────────────────────────────────────────────────────────────────────

export interface ApiParticipant {
  profileId: string
  displayName: string
  photoUrl: string | null
}

export interface ApiConversation {
  id: string
  taskId: string
  taskTitle: string
  otherParticipant: ApiParticipant
  createdAt: string
}

export interface ApiConversationPreview {
  id: string
  taskId: string
  taskTitle: string
  otherParticipant: ApiParticipant
  lastMessageContent: string | null
  lastMessageAt: string | null
  hasUnread: boolean
  taskStatus: string
  pendingApplicationId: string | null
}

export interface ApiMessage {
  id: string
  content: string
  sentAt: string
  senderProfileId: string
  senderDisplayName: string
  isMine: boolean
}

export interface MessagesPage {
  messages: ApiMessage[]
  hasMore: boolean
}

// ── Query keys ────────────────────────────────────────────────────────────────

export const conversationKeys = {
  list: ["conversations"] as const,
  messages: (id: string) => ["conversations", id, "messages"] as const,
}

// ── Hooks ─────────────────────────────────────────────────────────────────────

export function useConversations(enabled = true) {
  return useQuery({
    queryKey: conversationKeys.list,
    queryFn: () => apiClient.get<ApiConversationPreview[]>("/conversations"),
    enabled,
    staleTime: 60_000,
  })
}

export function useConversationMessages(id: string) {
  return useInfiniteQuery({
    queryKey: conversationKeys.messages(id),
    queryFn: ({ pageParam }: { pageParam: string | undefined }) => {
      const params = new URLSearchParams({ limit: "50" })
      if (pageParam) params.set("before", pageParam)
      return apiClient.get<MessagesPage>(
        `/conversations/${id}/messages?${params.toString()}`
      )
    },
    initialPageParam: undefined as string | undefined,
    getNextPageParam: () => undefined,
    getPreviousPageParam: (firstPage) =>
      firstPage.hasMore ? firstPage.messages[0]?.sentAt : undefined,
    enabled: Boolean(id),
  })
}

export function useStartConversation() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (taskId: string) =>
      apiClient.post<ApiConversation>("/conversations", { taskId }),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: conversationKeys.list })
    },
  })
}

export function useMarkConversationRead() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (conversationId: string) =>
      apiClient.put<void>(`/conversations/${conversationId}/read`, {}),
    onSuccess: (_data, conversationId) => {
      qc.setQueryData<ApiConversationPreview[]>(conversationKeys.list, (prev) =>
        prev?.map((c) =>
          c.id === conversationId ? { ...c, hasUnread: false } : c
        )
      )
    },
  })
}

export function useUnreadCount(): number {
  const { data: conversations = [] } = useConversations()
  return conversations.filter((c) => c.hasUnread).length
}

export function useSendMessage(conversationId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (content: string) =>
      apiClient.post<ApiMessage>(`/conversations/${conversationId}/messages`, {
        content,
      }),
    onSuccess: (message) => {
      qc.setQueryData<InfiniteData<MessagesPage>>(
        conversationKeys.messages(conversationId),
        (prev) => {
          if (!prev) {
            return {
              pages: [{ messages: [message], hasMore: false }],
              pageParams: [undefined],
            }
          }
          const lastPage = prev.pages[prev.pages.length - 1]
          if (lastPage.messages.some((m) => m.id === message.id)) return prev
          return {
            ...prev,
            pages: [
              ...prev.pages.slice(0, -1),
              { ...lastPage, messages: [...lastPage.messages, message] },
            ],
          }
        }
      )
      void qc.invalidateQueries({ queryKey: conversationKeys.list })
    },
  })
}
