"use client"

import {
  createContext,
  useCallback,
  useContext,
  useState,
  type ReactNode,
} from "react"

import type { RegistrationStep } from "@/lib/auth/functions"
import type { RegisterFormValues } from "@/lib/auth/schemas"

interface RegisterDraft {
  values: RegisterFormValues
  step: RegistrationStep
}

interface RegisterDraftContextValue {
  draft: RegisterDraft | null
  saveDraft: (values: RegisterFormValues, step: RegistrationStep) => void
  clearDraft: () => void
}

const RegisterDraftContext = createContext<RegisterDraftContextValue | null>(
  null
)

export function RegisterDraftProvider({ children }: { children: ReactNode }) {
  const [draft, setDraft] = useState<RegisterDraft | null>(null)

  const saveDraft = useCallback(
    (values: RegisterFormValues, step: RegistrationStep) => {
      setDraft({ values, step })
    },
    []
  )

  const clearDraft = useCallback(() => {
    setDraft(null)
  }, [])

  return (
    <RegisterDraftContext.Provider value={{ draft, saveDraft, clearDraft }}>
      {children}
    </RegisterDraftContext.Provider>
  )
}

export function useRegisterDraft(): RegisterDraftContextValue {
  const ctx = useContext(RegisterDraftContext)
  if (!ctx)
    throw new Error("useRegisterDraft used outside RegisterDraftProvider")
  return ctx
}
