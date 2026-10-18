import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, setCompanySelection, type CompanyInfo, type Me } from './api'

type AuthState = {
  me: Me | null
  needsSetup: boolean | null
  refresh: () => Promise<void>
  /** Secili her sirkette Yonetici (veya grup yoneticisi). */
  isAdmin: boolean
  isGroupAdmin: boolean
  /** Kayit yapabilir: tek sirket secili ve o sirkette yazma yetkisi var. */
  canWrite: boolean
  /** Secili her sirkette bu yetki anahtari var mi (sunucu kurali; arayuz yalniz menuyu/dugmeyi ayarlar). */
  can: (key: string) => boolean
  companies: CompanyInfo[]
  selectedIds: string[]
  /** Birden cok sirket secili: yalniz okuma, satirlarda sirket sutunu. */
  consolidated: boolean
  selectCompanies: (ids: string[]) => Promise<void>
  /** Sirket secimi her degistiginde artar; sayfalar bununla yeniden yuklenir. */
  selectionVersion: number
}

const AuthContext = createContext<AuthState>({
  me: null, needsSetup: null, refresh: async () => {}, isAdmin: false, isGroupAdmin: false, canWrite: false,
  can: () => false, companies: [], selectedIds: [], consolidated: false, selectCompanies: async () => {}, selectionVersion: 0,
})
export const useAuth = () => useContext(AuthContext)

const SELECTION_KEY = 'uusport.companies'

function readStoredSelection(): string[] {
  try {
    const raw = localStorage.getItem(SELECTION_KEY)
    const parsed = raw ? (JSON.parse(raw) as unknown) : []
    return Array.isArray(parsed) ? parsed.filter((x): x is string => typeof x === 'string') : []
  } catch {
    return []
  }
}

function storeSelection(ids: string[]) {
  try {
    localStorage.setItem(SELECTION_KEY, JSON.stringify(ids))
  } catch {
    // yoksayilir; bu yalniz goruntu tercihidir
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [me, setMe] = useState<Me | null>(null)
  const [needsSetup, setNeedsSetup] = useState<boolean | null>(null)
  const [selectionVersion, setSelectionVersion] = useState(0)

  /** Sunucudan /me alir; kayitli secim izinli sirketlerle kesisiyorsa onu uygular (sunucu yine kesistirir). */
  const loadMe = useCallback(async (wanted?: string[]) => {
    let result = await api<Me>('/api/auth/me')
    if (!result.authenticated) {
      setCompanySelection([])
      return result
    }
    const allowed = new Set((result.companies ?? []).map((c) => c.id))
    const preferred = (wanted ?? readStoredSelection()).filter((id) => allowed.has(id))
    const current = result.selectedCompanyIds ?? []
    const target = preferred.length > 0 ? preferred : current
    setCompanySelection(target)
    if (target.length !== current.length || target.some((id) => !current.includes(id))) {
      result = await api<Me>('/api/auth/me')
    }
    return result
  }, [])

  const refresh = useCallback(async () => {
    const setup = await api<{ needsSetup: boolean }>('/api/setup/status')
    setNeedsSetup(setup.needsSetup)
    setMe(setup.needsSetup ? { authenticated: false } : await loadMe())
  }, [loadMe])

  useEffect(() => {
    refresh().catch(() => setMe({ authenticated: false }))
    const onUnauthorized = () => setMe({ authenticated: false })
    window.addEventListener('uusport:unauthorized', onUnauthorized)
    return () => window.removeEventListener('uusport:unauthorized', onUnauthorized)
  }, [refresh])

  const selectCompanies = useCallback(async (ids: string[]) => {
    if (ids.length === 0) return
    storeSelection(ids)
    setCompanySelection(ids)
    setMe(await loadMe(ids))
    setSelectionVersion((v) => v + 1)
  }, [loadMe])

  const value = useMemo<AuthState>(() => {
    const permissions = new Set(me?.permissions ?? [])
    const selectedIds = me?.selectedCompanyIds ?? []
    return {
      me, needsSetup, refresh, selectCompanies, selectionVersion, selectedIds,
      isAdmin: !!me?.isAdmin,
      isGroupAdmin: !!me?.isGroupAdmin,
      canWrite: !!me?.canWrite,
      can: (key: string) => permissions.has(key),
      companies: me?.companies ?? [],
      consolidated: selectedIds.length > 1,
    }
  }, [me, needsSetup, refresh, selectCompanies, selectionVersion])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
