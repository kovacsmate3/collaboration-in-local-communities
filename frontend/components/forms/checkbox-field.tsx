"use client"

import type * as React from "react"
import {
  useFormContext,
  type FieldPathByValue,
  type FieldValues,
} from "react-hook-form"

import {
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormMessage,
} from "@/components/ui/form"

interface CheckboxFieldProps<TFieldValues extends FieldValues> extends Omit<
  React.ComponentProps<"input">,
  | "checked"
  | "defaultChecked"
  | "name"
  | "onBlur"
  | "onChange"
  | "type"
  | "value"
> {
  name: FieldPathByValue<TFieldValues, boolean>
  label: React.ReactNode
  description?: React.ReactNode
}

export function CheckboxField<TFieldValues extends FieldValues>({
  name,
  label,
  description,
  className = "mt-1 size-4 rounded border-input",
  ...inputProps
}: CheckboxFieldProps<TFieldValues>) {
  const { control } = useFormContext<TFieldValues>()

  return (
    <FormField
      control={control}
      name={name}
      render={({ field }) => (
        <FormItem>
          <label className="flex items-start gap-2 text-sm text-muted-foreground">
            <FormControl>
              <input
                {...inputProps}
                type="checkbox"
                checked={field.value}
                onChange={(event) => field.onChange(event.target.checked)}
                onBlur={field.onBlur}
                name={field.name}
                ref={field.ref}
                className={className}
              />
            </FormControl>
            <span>{label}</span>
          </label>
          {description ? (
            <FormDescription>{description}</FormDescription>
          ) : null}
          <FormMessage />
        </FormItem>
      )}
    />
  )
}
