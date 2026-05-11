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
import { Input } from "@/components/ui/input"

export interface TextFieldProps<TFieldValues extends FieldValues> extends Omit<
  React.ComponentProps<typeof Input>,
  "defaultValue" | "name" | "onBlur" | "onChange" | "value"
> {
  name: FieldPathByValue<TFieldValues, string>
  label: React.ReactNode
  description?: React.ReactNode
  labelAction?: React.ReactNode
  optional?: boolean
}

export function TextField<TFieldValues extends FieldValues>({
  name,
  label,
  description,
  labelAction,
  optional = false,
  ...inputProps
}: TextFieldProps<TFieldValues>) {
  const { control } = useFormContext<TFieldValues>()

  return (
    <FormField
      control={control}
      name={name}
      render={({ field }) => (
        <FormItem className="gap-3">
          {labelAction ? (
            <div className="flex items-center justify-between">
              <FieldLabel label={label} optional={optional} />
              {labelAction}
            </div>
          ) : (
            <FieldLabel label={label} optional={optional} />
          )}
          <FormControl>
            <Input {...inputProps} {...field} />
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

function FieldLabel({
  label,
  optional,
}: {
  label: React.ReactNode
  optional: boolean
}) {
  return (
    <FormLabel>
      {label}
      {optional ? (
        <>
          {" "}
          <span className="font-normal text-muted-foreground">(optional)</span>
        </>
      ) : null}
    </FormLabel>
  )
}
