"use client"

import { useAuth } from "@/lib/auth-context"
import { useNotificationHub } from "@/lib/notification-hub"

export function NotificationHubMount() {
  const { user } = useAuth()
  useNotificationHub(user?.profileId)
  return null
}
