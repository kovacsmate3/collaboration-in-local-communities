import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"

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
}

export interface ApiMessage {
  id: string
  content: string
  sentAt: string
  senderProfileId: string
  senderDisplayName: string
  isMine: boolean
}

// ── Query keys ────────────────────────────────────────────────────────────────

export const conversationKeys = {
  list: ["conversations"] as const,
  messages: (id: string) => ["conversations", id, "messages"] as const,
}

// ── Hooks ─────────────────────────────────────────────────────────────────────

export function useConversations() {
  return useQuery({
    queryKey: conversationKeys.list,
    queryFn: () => apiClient.get<ApiConversationPreview[]>("/conversations"),
  })
}

export function useConversationMessages(id: string) {
  return useQuery({
    queryKey: conversationKeys.messages(id),
    queryFn: () => apiClient.get<ApiMessage[]>(`/conversations/${id}/messages`),
    enabled: !!id,
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

export function useSendMessage(conversationId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (content: string) =>
      apiClient.post<ApiMessage>(`/conversations/${conversationId}/messages`, {
        content,
      }),
    onSuccess: (message) => {
      qc.setQueryData<ApiMessage[]>(
        conversationKeys.messages(conversationId),
        (prev) => (prev ? [...prev, message] : [message])
      )
      void qc.invalidateQueries({ queryKey: conversationKeys.list })
    },
  })
}
