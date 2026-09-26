import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'
import { api, type Me } from './api'

type AuthState = {
  me: Me | null
  needsSetup: boolean | null
  refresh: () => Promise<void>
  isAdmin: boolean
}

const AuthContext = createContext<AuthState>({ me: null, needsSetup: null, refresh: async () => {}, isAdmin: false })
export const useAuth = () => useContext(AuthContext)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [me, setMe] = useState<Me | null>(null)
  const [needsSetup, setNeedsSetup] = useState<boolean | null>(null)

  const refresh = useCallback(async () => {
    const setup = await api<{ needsSetup: boolean }>('/api/setup/status')
    setNeedsSetup(setup.needsSetup)
    setMe(setup.needsSetup ? { authenticated: false } : await api<Me>('/api/auth/me'))
  }, [])

  useEffect(() => {
    refresh().catch(() => setMe({ authenticated: false }))
    const onUnauthorized = () => setMe({ authenticated: false })
    window.addEventListener('uusport:unauthorized', onUnauthorized)
    return () => window.removeEventListener('uusport:unauthorized', onUnauthorized)
  }, [refresh])

  const isAdmin = !!me?.roles?.includes('Yonetici')
  return <AuthContext.Provider value={{ me, needsSetup, refresh, isAdmin }}>{children}</AuthContext.Provider>
}
