import { BrowserRouter, Navigate, Route, Routes } from 'react-router'
import { AuthProvider, useAuth } from './auth'
import { Loading, ToastProvider } from './components'
import { Layout } from './Layout'
import { LoginPage, MfaSetupPage, SetupPage } from './pages/AuthPages'
import { ImportPage } from './pages/ImportPage'
import { ChecksPage, LoansPage } from './pages/CashDetailPages'
import { PartyDetailPage, PartyListPage } from './pages/PartyPages'
import { CashFlowPage } from './pages/CashFlowPage'
import { CompaniesPage, UsersPage } from './pages/ManagementPages'
import { AuditPage, CashPage, CatalogPage, DashboardPage, DocumentsPage, LedgerListPage, PendingPage, ReportsPage } from './pages/WorkPages'

function Gate() {
  const { me, needsSetup, isAdmin, isGroupAdmin, can } = useAuth()
  if (needsSetup === null || me === null) return <Loading label="UU Sport açılıyor…" />
  if (needsSetup) return <SetupPage />
  if (!me.authenticated) return <LoginPage />
  // Iki adimli dogrulama acilmadan hicbir veri ekrani acilmaz (guvenlik standardi 2.1).
  if (!me.mfaEnabled) return <MfaSetupPage />
  return (
    <Routes>
      <Route element={<Layout />}>
        {/* Genel bakis hesap hareketi yetkisi ister; yoksa kullanici izinli ilk ekrana gider (menu ile ayni kural). */}
        <Route index element={can('ledger.read') ? <DashboardPage /> : <Navigate to={can('reports.summary') ? '/raporlar' : can('parties.read') ? '/cariler' : '/raporlar'} replace />} />
        <Route path="cariler" element={<PartyListPage />} />
        <Route path="cariler/:id" element={<PartyDetailPage />} />
        <Route path="bekleyen" element={<PendingPage />} />
        <Route path="faturalar" element={<DocumentsPage />} />
        <Route path="hesap-hareketleri" element={<LedgerListPage />} />
        <Route path="katalog" element={<CatalogPage />} />
        <Route path="kasa-banka" element={<CashPage />} />
        <Route path="krediler" element={<LoansPage />} />
        <Route path="cekler" element={<ChecksPage />} />
        <Route path="aktarim" element={<ImportPage />} />
        <Route path="raporlar" element={<ReportsPage />} />
        {can('cash.read') && can('reports.summary') ? <Route path="nakit-akisi" element={<CashFlowPage />} /> : null}
        {isAdmin ? <Route path="yonetim/kullanicilar" element={<UsersPage />} /> : null}
        {isGroupAdmin ? <Route path="yonetim/sirketler" element={<CompaniesPage />} /> : null}
        {isAdmin && can('audit.read') ? <Route path="yonetim/denetim" element={<AuditPage />} /> : null}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <ToastProvider>
          <Gate />
        </ToastProvider>
      </AuthProvider>
    </BrowserRouter>
  )
}
