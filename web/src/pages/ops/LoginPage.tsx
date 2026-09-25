import { useState, type FormEvent } from 'react'
import Box from '@mui/material/Box'
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
    <Box
      component="form"
      className="quote-form"
      onSubmit={handleSubmit}
      sx={{ maxWidth: 360, width: '100%', display: 'flex', flexDirection: 'column', gap: 2 }}
    >
      <h2 className="font-heading text-lg font-semibold mb-1">Sign in</h2>
      <Box sx={{ display: 'grid', gridTemplateColumns: '1fr', gap: 2 }}>
        <Input label="Email" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
        <Input label="Password" type="password" required value={password} onChange={(e) => setPassword(e.target.value)} />
        <Button type="submit" disabled={submitting}>
          {submitting ? 'Signing in…' : 'Sign in'}
        </Button>
      </Box>
    </Box>
  )
}
