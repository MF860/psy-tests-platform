import * as React from 'react'
import {
  Controller,
  type ControllerProps,
  type FieldPath,
  type FieldValues,
  FormProvider,
  useFormContext,
} from 'react-hook-form'

import { cn } from '@/lib/utils'

type FormFieldContextValue = {
  name: string
}

const FormFieldContext = React.createContext<FormFieldContextValue | undefined>(undefined)
const FormItemContext = React.createContext<{ id: string } | undefined>(undefined)

export const Form = FormProvider

export function FormField<TFieldValues extends FieldValues, TName extends FieldPath<TFieldValues>>(
  props: ControllerProps<TFieldValues, TName>,
) {
  const { name, render, ...controllerProps } = props

  return (
    <Controller
      {...controllerProps}
      name={name}
      render={(controllerRenderProps) => (
        <FormFieldContext.Provider value={{ name: name as string }}>
          {render(controllerRenderProps)}
        </FormFieldContext.Provider>
      )}
    />
  )
}

export const FormItem = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => {
    const id = React.useId()

    return (
      <FormItemContext.Provider value={{ id }}>
        <div ref={ref} className={cn('space-y-2', className)} {...props} />
      </FormItemContext.Provider>
    )
  },
)
FormItem.displayName = 'FormItem'

export const FormLabel = React.forwardRef<HTMLLabelElement, React.LabelHTMLAttributes<HTMLLabelElement>>(
  ({ className, ...props }, ref) => {
    const { formItemId } = useFormField()

    return (
      <label
        ref={ref}
        className={cn('text-sm font-medium text-gray-700', className)}
        htmlFor={formItemId}
        {...props}
      />
    )
  },
)
FormLabel.displayName = 'FormLabel'

export const FormDescription = React.forwardRef<HTMLParagraphElement, React.HTMLAttributes<HTMLParagraphElement>>(
  ({ className, ...props }, ref) => {
    const { formDescriptionId } = useFormField()

    return (
      <p
        ref={ref}
        id={formDescriptionId}
        className={cn('text-sm text-gray-500', className)}
        {...props}
      />
    )
  },
)
FormDescription.displayName = 'FormDescription'

export const FormMessage = React.forwardRef<HTMLParagraphElement, React.HTMLAttributes<HTMLParagraphElement>>(
  ({ className, children, ...props }, ref) => {
    const { formMessageId, error } = useFormField()
    const body = error?.message ?? children

    if (!body) {
      return null
    }

    return (
      <p
        ref={ref}
        id={formMessageId}
        className={cn('text-sm font-medium text-red-600', className)}
        {...props}
      >
        {body}
      </p>
    )
  },
)
FormMessage.displayName = 'FormMessage'

export const FormControl = React.forwardRef<any, { children: React.ReactElement }>(
  ({ children }, ref) => {
    const { formItemId, formDescriptionId, formMessageId, error } = useFormField()

    return React.cloneElement(children, {
      ref,
      id: children.props.id ?? formItemId,
      'aria-describedby': [children.props['aria-describedby'], formDescriptionId, formMessageId]
        .filter(Boolean)
        .join(' ') || undefined,
      'aria-invalid': error ? 'true' : children.props['aria-invalid'],
    })
  },
)
FormControl.displayName = 'FormControl'

function useFormField() {
  const fieldContext = React.useContext(FormFieldContext)
  const itemContext = React.useContext(FormItemContext)
  const { getFieldState, formState } = useFormContext()

  if (!fieldContext) {
    throw new Error('FormField must be used within a Form component')
  }

  const fieldState = getFieldState(fieldContext.name as FieldPath<FieldValues>, formState)
  const id = itemContext?.id ?? fieldContext.name

  return {
    name: fieldContext.name,
    formItemId: `${id}-form-item`,
    formDescriptionId: `${id}-form-item-description`,
    formMessageId: `${id}-form-item-message`,
    ...fieldState,
  }
}
