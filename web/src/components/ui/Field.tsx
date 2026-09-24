import { useId, type ReactNode } from 'react'

export interface FieldProps {
  label: string
  help?: string
  error?: string
  required?: boolean
  children: (fieldProps: { id: string; 'aria-describedby'?: string; 'aria-invalid'?: boolean }) => ReactNode
}

/** Label/help/error wrapper shared by Input and Select — design-system-spec.md's
 * `.field-block`. Wires aria-describedby/aria-invalid itself so callers can't forget it. */
export function Field({ label, help, error, required, children }: FieldProps) {
  const id = useId()
  const helpId = help ? `${id}-help` : undefined
  const errorId = error ? `${id}-error` : undefined
  const describedBy = [helpId, errorId].filter(Boolean).join(' ') || undefined

  return (
    <div className={`field-block${error ? ' has-error' : ''}`}>
      <label htmlFor={id}>
        {label}
        {required ? ' *' : ''}
      </label>
      {children({ id, 'aria-describedby': describedBy, 'aria-invalid': error ? true : undefined })}
      {help && !error && (
        <span id={helpId} className="field-help">
          {help}
        </span>
      )}
      {error && (
        <span id={errorId} className="field-error" role="alert">
          {error}
        </span>
      )}
    </div>
  )
}
