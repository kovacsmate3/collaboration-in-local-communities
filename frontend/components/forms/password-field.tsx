"use client"

import type { FieldValues } from "react-hook-form"

import { TextField, type TextFieldProps } from "@/components/forms/text-field"

type PasswordFieldProps<TFieldValues extends FieldValues> = Omit<
  TextFieldProps<TFieldValues>,
  "type"
>

export function PasswordField<TFieldValues extends FieldValues>(
  props: PasswordFieldProps<TFieldValues>
) {
  return <TextField type="password" {...props} />
}
