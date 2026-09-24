import type { SelectHTMLAttributes } from 'react'
import { Field } from './Field'

export interface SelectOption {
  value: string
  label: string
}

export interface SelectProps extends Omit<SelectHTMLAttributes<HTMLSelectElement>, 'id'> {
  label: string
  help?: string
  error?: string
  options: SelectOption[]
  placeholder?: string
}

/** Native <select> styled per design-system-spec.md — kept native (not a custom listbox) for
 * free keyboard/screen-reader support, matching ui-plan.md's accessibility bar. */
export function Select({ label, help, error, required, options, placeholder, className = '', ...props }: SelectProps) {
  return (
    <Field label={label} help={help} error={error} required={required}>
      {(fieldProps) => (
        <select className={`select ${className}`.trim()} required={required} defaultValue="" {...fieldProps} {...props}>
          {placeholder && (
            <option value="" disabled>
              {placeholder}
            </option>
          )}
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      )}
    </Field>
  )
}
