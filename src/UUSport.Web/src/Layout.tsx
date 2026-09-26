import { useEffect, useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router'
import { api, post } from './api'
import { useAuth } from './auth'

type Counts = { salesAwaitingInvoice: number; purchasesAwaitingInvoice: number; unallocatedPayments: number; pendingOpeningBalances: number }

export function Layout() {
  const { me, isAdmin, refresh } = useAuth()
  const location = useLocation()
  const [pending, setPending] = useState<number | null>(null)

  useEffect(() => {
    api<Counts>('/api/dashboard')
      .then((c) => setPending(c.salesAwaitingInvoice + c.purchasesAwaitingInvoice + c.unallocatedPayments + c.pendingOpeningBalances))
      .catch(() => setPending(null))
  }, [location.pathname])

  const link = (to: string, label: string, extra?: React.ReactNode) => (
    <NavLink to={to} end={to === '/'}>{label}{extra}</NavLink>
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
        <div className="nav-label">Günlük iş</div>
        <nav className="nav" aria-label="Günlük iş">
          {link('/', 'Genel Bakış')}
          {link('/cariler', 'Cariler')}
          {link('/bekleyen', 'Muhasebe Bekleyen', pending ? <span className="count" aria-label={`${pending} bekleyen iş`}>{pending}</span> : null)}
          {link('/faturalar', 'Faturalar')}
        </nav>
        <div className="nav-label">Tanımlar</div>
        <nav className="nav" aria-label="Tanımlar">
          {link('/katalog', 'Ürün ve Hizmetler')}
          {link('/kasa-banka', 'Kasa ve Banka')}
          {link('/aktarim', 'Excel Aktarımı')}
        </nav>
        <div className="nav-label">Rapor</div>
        <nav className="nav" aria-label="Rapor">
          {link('/raporlar', 'Raporlar')}
        </nav>
        {isAdmin ? (
          <>
            <div className="nav-label">Yönetim</div>
            <nav className="nav" aria-label="Yönetim">
              {link('/yonetim/kullanicilar', 'Kullanıcılar')}
              {link('/yonetim/denetim', 'Denetim Kaydı')}
            </nav>
          </>
        ) : null}
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
        <main className="content" id="icerik">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
