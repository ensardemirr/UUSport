import { useEffect, useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router'
import { api, post } from './api'
import { useAuth } from './auth'
import { CompanyTag } from './components'

type Counts = { salesAwaitingInvoice: number; purchasesAwaitingInvoice: number; unallocatedPayments: number; pendingOpeningBalances: number }

const ROLE_LABEL: Record<string, string> = { Yonetici: 'Yönetici', Muhasebe: 'Muhasebe', Ortak: 'Ortak' }

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

/**
 * Sirket secici (ADR-012 2.9). Her sirket bir anahtar dugmedir (aria-pressed); "Tumu" izinli tum sirketleri
 * secer. Birden cok sirket secilince arayuz yalniz okuma moduna gecer (sunucu yazmayi zaten reddeder).
 */
function CompanySwitcher() {
  const { companies, selectedIds, selectCompanies } = useAuth()
  if (companies.length === 0) return null
  if (companies.length === 1) return <div className="company-switch"><CompanyTag code={companies[0].code} /><span>{companies[0].name}</span></div>
  const allSelected = companies.every((c) => selectedIds.includes(c.id))
  const toggle = (id: string) => {
    const next = selectedIds.includes(id) ? selectedIds.filter((x) => x !== id) : [...selectedIds, id]
    if (next.length > 0) void selectCompanies(next)
  }
  return (
    <div className="company-switch" role="group" aria-label="Çalışılan şirketler">
      {companies.map((c) => (
        <button key={c.id} type="button" className="co-chip" aria-pressed={selectedIds.includes(c.id)} title={c.name}
          onClick={() => toggle(c.id)}>
          <CompanyTag code={c.code} /><span className="co-name">{c.name}</span>
        </button>
      ))}
      <button type="button" className="co-chip co-all" aria-pressed={allSelected} onClick={() => void selectCompanies(companies.map((c) => c.id))}>
        Tümü
      </button>
    </div>
  )
}

export function Layout() {
  const { me, isAdmin, isGroupAdmin, can, canWrite, consolidated, selectionVersion, refresh } = useAuth()
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
    if (!can('ledger.read')) { setPending(null); return }
    api<Counts>('/api/dashboard')
      .then((c) => setPending(c.salesAwaitingInvoice + c.purchasesAwaitingInvoice + c.unallocatedPayments + c.pendingOpeningBalances))
      .catch(() => setPending(null))
  }, [location.pathname, selectionVersion, can])

  const link = (to: string, label: string, extra?: React.ReactNode) => (
    <NavLink to={to} end={to === '/'}>{label}{extra}</NavLink>
  )
  // Menu yalniz kisayoldur; asil yetki sunucudadir. Yetkisiz olunan baglantilar gosterilmez.
  const linkIf = (key: string, to: string, label: string, extra?: React.ReactNode) =>
    can(key) ? link(to, label, extra) : null

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
          {linkIf('ledger.read', '/', 'Genel Bakış')}
        </nav>
        {group('Günlük iş', (
          <>
            {linkIf('parties.read', '/cariler', 'Cariler')}
            {linkIf('ledger.read', '/bekleyen', 'Muhasebe Bekleyen', pending ? <span className="count" aria-label={`${pending} bekleyen iş`}>{pending}</span> : null)}
            {linkIf('documents.read', '/faturalar', 'Faturalar')}
            {linkIf('ledger.read', '/hesap-hareketleri', 'Hesap Hareketleri')}
          </>
        ))}
        {group('Tanımlar', (
          <>
            {linkIf('catalog.read', '/katalog', 'Ürün ve Hizmetler')}
            {linkIf('cash.read', '/kasa-banka', 'Kasa ve Banka')}
            {linkIf('cash.read', '/krediler', 'Krediler')}
            {linkIf('cash.read', '/cekler', 'Çek Portföyü')}
            {canWrite ? link('/aktarim', 'Excel Aktarımı') : null}
          </>
        ))}
        {group('Rapor', linkIf('reports.summary', '/raporlar', 'Raporlar'))}
        {isAdmin || isGroupAdmin ? group('Yönetim', (
          <>
            {isAdmin ? link('/yonetim/kullanicilar', 'Kullanıcılar') : null}
            {isGroupAdmin ? link('/yonetim/sirketler', 'Şirketler') : null}
            {isAdmin && can('audit.read') ? link('/yonetim/denetim', 'Denetim Kaydı') : null}
          </>
        )) : null}
        <div className="sidebar-foot">Sürüm 0.1 · Faz 1 (yerel ağ)</div>
      </aside>
      <div className="main">
        <header className="topbar">
          <CompanySwitcher />
          <div className="who">
            <span className="muted small env-note">Geliştirme ortamı — yalnız sentetik veri</span>
            <span>{me?.fullName} <span className="muted small">({me?.isGroupAdmin ? 'Grup yöneticisi' : (me?.roles ?? []).map((r) => ROLE_LABEL[r] ?? r).join(' / ')})</span></span>
            <button type="button" className="btn small" onClick={async () => { await post('/api/auth/logout'); await refresh() }}>Çıkış</button>
          </div>
        </header>
        {newVersion ? (
          <div className="version-banner" role="status">
            <span>Uygulamanın yeni sürümü yüklendi. Doğru çalışması için sayfayı yenileyin.</span>
            <button type="button" className="btn small primary" onClick={() => window.location.reload()}>Yenile</button>
          </div>
        ) : null}
        {isGroupAdmin && me?.onlyAdmin ? (
          <div className="version-banner" role="status">
            <span>Sistemde tek grup yöneticisi hesabı var. Bu hesap kilitlenirse kimse grup işlemi yapamaz; ikinci bir grup yöneticisi oluşturmanız önerilir.</span>
            <NavLink to="/yonetim/kullanicilar" className="btn small primary">Kullanıcılar</NavLink>
          </div>
        ) : null}
        {consolidated ? (
          <div className="mode-banner" role="status">
            <strong>Birleşik görünüm</strong>
            <span>Seçili şirketler birlikte gösteriliyor; yalnız okunur. Kayıt girmek için tek şirket seçin.</span>
          </div>
        ) : !canWrite ? (
          <div className="mode-banner" role="status">
            <strong>Salt okunur</strong>
            <span>Bu şirkette rolünüz kayıt girmeye izin vermiyor.</span>
          </div>
        ) : null}
        <main className="content" id="icerik" key={selectionVersion}>
          <Outlet />
        </main>
      </div>
    </div>
  )
}
