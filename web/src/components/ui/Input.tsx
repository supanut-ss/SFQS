import type { InputHTMLAttributes } from 'react'
import { Field } from './Field'

export interface InputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'id'> {
  label: string
  help?: string
  error?: string
}

/** Text input with the shared Field wrapper — design-system-spec.md's form primitives. */
export function Input({ label, help, error, required, className = '', ...props }: InputProps) {
  return (
    <Field label={label} help={help} error={error} required={required}>
      {(fieldProps) => (
        <input className={`input ${className}`.trim()} required={required} {...fieldProps} {...props} />
      )}
    </Field>
  )
}
