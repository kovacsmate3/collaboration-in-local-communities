"use client"

import type * as React from "react"
import {
  useFormContext,
  type FieldPathByValue,
  type FieldValues,
} from "react-hook-form"

import { LocationInput } from "@/components/shared/location-input"
import {
  FormDescription,
  FormField,
  FormItem,
  FormMessage,
} from "@/components/ui/form"
import type { LocationValue } from "@/lib/location"

interface LocationFieldProps<TFieldValues extends FieldValues> {
  name: FieldPathByValue<TFieldValues, LocationValue>
  label: string
  required?: boolean
  placeholder?: string
  description?: React.ReactNode
}

export function LocationField<TFieldValues extends FieldValues>({
  name,
  label,
  required,
  placeholder,
  description,
}: LocationFieldProps<TFieldValues>) {
  const { control } = useFormContext<TFieldValues>()

  return (
    <FormField
      control={control}
      name={name}
      render={({ field, fieldState }) => {
        const inputId = `${field.name}-form-item`
        const descriptionId = `${field.name}-form-item-description`
        const messageId = `${field.name}-form-item-message`
        const hasError = Boolean(fieldState.error)
        const describedBy = [
          description ? descriptionId : null,
          hasError ? messageId : null,
        ]
          .filter(Boolean)
          .join(" ")

        return (
          <FormItem className="gap-3">
            <LocationInput
              id={inputId}
              label={label}
              required={required}
              value={field.value}
              onChange={field.onChange}
              placeholder={placeholder}
              aria-describedby={describedBy || undefined}
              aria-invalid={hasError}
            />
            {description ? (
              <FormDescription id={descriptionId}>{description}</FormDescription>
            ) : null}
            <FormMessage id={messageId} />
          </FormItem>
        )
      }}
    />
  )
}
