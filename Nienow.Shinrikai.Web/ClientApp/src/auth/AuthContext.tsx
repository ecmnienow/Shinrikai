import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, getToken, post, setToken, setUnauthorizedHandler } from '../api/client'
import type { AuthenticatedUser, UserInfo } from '../api/types'

interface AuthState {
  user: UserInfo | null
  /** Administrator ou Teacher ([Authorize(Roles = "Administrator, Teacher")] do original). */
  isEditor: boolean
  loading: boolean
  login: (username: string, password: string) => Promise<void>
  logout: () => Promise<void>
  /** Atualiza o usuário exibido (nome, nível) depois de editar o perfil. */
  updateUser: (user: UserInfo) => void
}

const AuthContext = createContext<AuthState | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserInfo | null>(null)
  const [loading, setLoading] = useState(() => getToken() !== null)

  useEffect(() => {
    setUnauthorizedHandler(() => setUser(null))
    if (!getToken()) return
    api<UserInfo>('/auth/me')
      .then(setUser)
      .catch(() => setToken(null))
      .finally(() => setLoading(false))
  }, [])

  const login = useCallback(async (username: string, password: string) => {
    const auth = await post<AuthenticatedUser>('/auth/login', { username, password })
    setToken(auth.token)
    setUser(auth.user)
  }, [])

  const logout = useCallback(async () => {
    // Só registra a interação; se falhar (token expirado, rede), sai do mesmo jeito.
    await post('/auth/logout').catch(() => {})
    setToken(null)
    setUser(null)
  }, [])

  const value = useMemo(
    () => ({ user, isEditor: user?.role === 'Administrator' || user?.role === 'Teacher', loading, login, logout, updateUser: setUser }),
    [user, loading, login, logout],
  )
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth fora de <AuthProvider>')
  return ctx
}
