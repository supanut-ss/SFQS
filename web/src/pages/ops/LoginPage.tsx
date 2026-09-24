import { useState, type FormEvent } from 'react'
import { Button, Input, useToast } from '../../components/ui'
import { useAuth } from '../../lib/useAuth'
import { ApiError } from '../../lib/api'

/** ui-plan.md IA page 8 — the only entry point for Sale/Operation/Admin (no self-signup). */
export function LoginPage() {
  const { login } = useAuth()
  const toast = useToast()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setSubmitting(true)
    try {
      await login(email, password)
    } catch (err) {
      toast.show(err instanceof ApiError ? 'Invalid email or password' : 'Login failed', 'destructive')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form className="quote-form" onSubmit={handleSubmit} style={{ maxWidth: 360 }}>
      <h2 className="font-heading text-lg font-semibold mb-4">Sign in</h2>
      <div className="flex flex-col gap-4">
        <Input label="Email" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
        <Input label="Password" type="password" required value={password} onChange={(e) => setPassword(e.target.value)} />
        <Button type="submit" disabled={submitting}>
          {submitting ? 'Signing in…' : 'Sign in'}
        </Button>
      </div>
    </form>
  )
}
