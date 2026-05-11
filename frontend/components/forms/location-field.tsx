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
      render={({ field }) => (
        <FormItem className="gap-3">
          <LocationInput
            id={field.name}
            label={label}
            required={required}
            value={field.value}
            onChange={field.onChange}
            placeholder={placeholder}
          />
          {description ? (
            <FormDescription>{description}</FormDescription>
          ) : null}
          <FormMessage />
        </FormItem>
      )}
    />
  )
}
