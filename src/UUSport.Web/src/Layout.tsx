import { useEffect, useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router'
import { api, post } from './api'
import { useAuth } from './auth'

type Counts = { salesAwaitingInvoice: number; purchasesAwaitingInvoice: number; unallocatedPayments: number; pendingOpeningBalances: number }

const NAV_STATE_KEY = 'uusport.nav.open'

function readOpenGroups(): Record<string, boolean> {
  try {
    const raw = localStorage.getItem(NAV_STATE_KEY)
    if (raw) return JSON.parse(raw)
  } catch {
    // localStorage kullanilamiyorsa varsayilana don
  }
  return {}
}

function writeOpenGroups(state: Record<string, boolean>) {
  try {
    localStorage.setItem(NAV_STATE_KEY, JSON.stringify(state))
  } catch {
    // yoksayilir; bu yalniz goruntu tercihidir
  }
}

export function Layout() {
  const { me, isAdmin, refresh } = useAuth()
  const location = useLocation()
  const [pending, setPending] = useState<number | null>(null)
  const [openGroups, setOpenGroups] = useState<Record<string, boolean>>(() => readOpenGroups())
  const [newVersion, setNewVersion] = useState(false)

  useEffect(() => {
    const onNewVersion = () => setNewVersion(true)
    window.addEventListener('uusport:new-version', onNewVersion)
    return () => window.removeEventListener('uusport:new-version', onNewVersion)
  }, [])

  useEffect(() => {
    api<Counts>('/api/dashboard')
      .then((c) => setPending(c.salesAwaitingInvoice + c.purchasesAwaitingInvoice + c.unallocatedPayments + c.pendingOpeningBalances))
      .catch(() => setPending(null))
  }, [location.pathname])

  const link = (to: string, label: string, extra?: React.ReactNode) => (
    <NavLink to={to} end={to === '/'}>{label}{extra}</NavLink>
  )

  const toggleGroup = (name: string, open: boolean) => {
    setOpenGroups((prev) => {
      const next = { ...prev, [name]: open }
      writeOpenGroups(next)
      return next
    })
  }

  const isOpen = (name: string) => openGroups[name] ?? true

  const group = (name: string, items: React.ReactNode) => (
    <details className="nav-group" open={isOpen(name)} onToggle={(e) => toggleGroup(name, e.currentTarget.open)}>
      <summary>
        <span>{name}</span>
        <svg className="chevron" width="12" height="12" viewBox="0 0 12 12" aria-hidden="true">
          <path d="M3 2 L9 6 L3 10" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </summary>
      <nav className="nav" aria-label={name}>{items}</nav>
    </details>
  )

  return (
    <div className="shell">
      <aside className="sidebar" aria-label="Ana menü">
        <div className="brand">
          <div className="brand-mark" aria-hidden="true">UU</div>
          <div>
            <div className="brand-name">UU Sport</div>
            <div className="brand-sub">Ön muhasebe</div>
          </div>
        </div>
        <nav className="nav nav-top" aria-label="Ana Sayfa">
          {link('/', 'Genel Bakış')}
        </nav>
        {group('Günlük iş', (
          <>
            {link('/cariler', 'Cariler')}
            {link('/bekleyen', 'Muhasebe Bekleyen', pending ? <span className="count" aria-label={`${pending} bekleyen iş`}>{pending}</span> : null)}
            {link('/faturalar', 'Faturalar')}
            {link('/hesap-hareketleri', 'Hesap Hareketleri')}
          </>
        ))}
        {group('Tanımlar', (
          <>
            {link('/katalog', 'Ürün ve Hizmetler')}
            {link('/kasa-banka', 'Kasa ve Banka')}
            {link('/krediler', 'Krediler')}
            {link('/cekler', 'Çek Portföyü')}
            {link('/aktarim', 'Excel Aktarımı')}
          </>
        ))}
        {group('Rapor', link('/raporlar', 'Raporlar'))}
        {isAdmin ? group('Yönetim', (
          <>
            {link('/yonetim/kullanicilar', 'Kullanıcılar')}
            {link('/yonetim/denetim', 'Denetim Kaydı')}
          </>
        )) : null}
        <div className="sidebar-foot">Sürüm 0.1 · Faz 1 (yerel ağ)</div>
      </aside>
      <div className="main">
        <header className="topbar">
          <span className="muted">Geliştirme ortamı — yalnız sentetik veri</span>
          <div className="who">
            <span>{me?.fullName} <span className="muted small">({me?.roles?.includes('Yonetici') ? 'Yönetici' : 'Muhasebe'})</span></span>
            <button type="button" className="btn small" onClick={async () => { await post('/api/auth/logout'); await refresh() }}>Çıkış</button>
          </div>
        </header>
        {newVersion ? (
          <div className="version-banner" role="status">
            <span>Uygulamanın yeni sürümü yüklendi. Doğru çalışması için sayfayı yenileyin.</span>
            <button type="button" className="btn small primary" onClick={() => window.location.reload()}>Yenile</button>
          </div>
        ) : null}
        {isAdmin && me?.onlyAdmin ? (
          <div className="version-banner" role="status">
            <span>Sistemde tek yönetici hesabı var. Bu hesap kilitlenirse kimse yönetici işlemi yapamaz; ikinci bir yönetici oluşturmanız önerilir.</span>
            <NavLink to="/yonetim/kullanicilar" className="btn small primary">Kullanıcılar</NavLink>
          </div>
        ) : null}
        <main className="content" id="icerik">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
