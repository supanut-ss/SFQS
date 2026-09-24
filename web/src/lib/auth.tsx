import { useEffect, useState, type ReactNode } from 'react'
import { api } from './api'
import { AuthContext, type AuthUser } from './AuthContext'

/** T8's JWT is an httpOnly cookie, so the frontend never touches the token itself — it just
 * asks /api/auth/me on load and after login/logout to know who's signed in. */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    api
      .get<AuthUser>('/api/auth/me')
      .then(setUser)
      .catch(() => setUser(null))
      .finally(() => setLoading(false))
  }, [])

  const login = async (email: string, password: string) => {
    const result = await api.post<AuthUser>('/api/auth/login', { email, password })
    setUser(result)
  }

  const logout = async () => {
    await api.post('/api/auth/logout')
    setUser(null)
  }

  return <AuthContext.Provider value={{ user, loading, login, logout }}>{children}</AuthContext.Provider>
}
