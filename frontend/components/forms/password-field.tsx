"use client"

import { useState } from "react"
import type { FieldValues } from "react-hook-form"
import { ViewIcon, ViewOffSlashIcon } from "@hugeicons/core-free-icons"
import { HugeiconsIcon } from "@hugeicons/react"

import { TextField, type TextFieldProps } from "@/components/forms/text-field"

type PasswordFieldProps<TFieldValues extends FieldValues> = Omit<
  TextFieldProps<TFieldValues>,
  "type" | "rightElement"
>

export function PasswordField<TFieldValues extends FieldValues>(
  props: PasswordFieldProps<TFieldValues>
) {
  const [showPassword, setShowPassword] = useState(false)

  return (
    <TextField
      type={showPassword ? "text" : "password"}
      rightElement={
        <button
          type="button"
          onClick={() => setShowPassword((prev) => !prev)}
          disabled={props.disabled}
          aria-label={showPassword ? "Hide password" : "Show password"}
          aria-pressed={showPassword}
          className="rounded-sm p-1 text-muted-foreground hover:text-foreground focus-visible:ring-1 focus-visible:ring-ring focus-visible:outline-none disabled:pointer-events-none disabled:opacity-50"
        >
          <HugeiconsIcon
            icon={showPassword ? ViewOffSlashIcon : ViewIcon}
            className="size-4"
            strokeWidth={2}
          />
        </button>
      }
      {...props}
    />
  )
}
