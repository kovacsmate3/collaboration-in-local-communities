import { useMutation } from "@tanstack/react-query"

import { apiClient } from "@/lib/api/client"

export interface AcceptTermsRequest {
  termsVersionId: string
}

export function acceptTermsVersion(termsVersionId: string): Promise<void> {
  return apiClient.post<void>("/terms/accept", {
    termsVersionId,
  } satisfies AcceptTermsRequest)
}

export function useAcceptTerms() {
  return useMutation({
    mutationFn: acceptTermsVersion,
  })
}
