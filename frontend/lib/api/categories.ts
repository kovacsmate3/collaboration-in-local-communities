import { useQuery } from "@tanstack/react-query"

import { apiClient } from "@/lib/api/client"

export interface ApiCategory {
  id: string
  code: string
  name: string
  icon: string
  description?: string
}

export const publicCategoryKeys = {
  all: ["categories"] as const,
  list: () => [...publicCategoryKeys.all, "list"] as const,
}

export function useCategories() {
  return useQuery({
    queryKey: publicCategoryKeys.list(),
    queryFn: () => apiClient.get<ApiCategory[]>("/categories"),
    staleTime: 5 * 60 * 1000,
  })
}
