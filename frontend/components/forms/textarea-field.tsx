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
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import { Textarea } from "@/components/ui/textarea"

interface TextareaFieldProps<TFieldValues extends FieldValues> extends Omit<
  React.ComponentProps<typeof Textarea>,
  "defaultValue" | "name" | "onBlur" | "onChange" | "value"
> {
  name: FieldPathByValue<TFieldValues, string>
  label: React.ReactNode
  description?: React.ReactNode
  optional?: boolean
}

export function TextareaField<TFieldValues extends FieldValues>({
  name,
  label,
  description,
  optional = false,
  ...textareaProps
}: TextareaFieldProps<TFieldValues>) {
  const { control } = useFormContext<TFieldValues>()

  return (
    <FormField
      control={control}
      name={name}
      render={({ field }) => (
        <FormItem className="gap-3">
          <FormLabel>
            {label}
            {optional ? (
              <>
                {" "}
                <span className="font-normal text-muted-foreground">
                  (optional)
                </span>
              </>
            ) : null}
          </FormLabel>
          <FormControl>
            <Textarea {...textareaProps} {...field} />
          </FormControl>
          {description ? (
            <FormDescription>{description}</FormDescription>
          ) : null}
          <FormMessage />
        </FormItem>
      )}
    />
  )
}
