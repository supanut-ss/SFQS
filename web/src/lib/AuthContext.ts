import { createContext } from 'react'

export type Role = 'Sale' | 'Operation' | 'Admin'

export interface AuthUser {
  id: number
  email: string
  role: Role
}

export interface AuthContextValue {
  user: AuthUser | null
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)
