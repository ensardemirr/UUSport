import { useState } from 'react'
import { api, post, put, type CompanyRole } from '../api'
import { useAuth } from '../auth'
import { CompanyTag, Dialog, Empty, ErrorBox, Field, Loading, errorText, useAsync, useToast } from '../components'
import { normalizeNumber, percent } from '../format'

// =====================================================================
// Kullanicilar ve sirket uyelikleri (ADR-012 2.2, 2.4)
// =====================================================================

const ROLES: { id: CompanyRole; label: string; hint: string }[] = [
  { id: 'Yonetici', label: 'Yönetici', hint: 'Her şeyi görür ve yapar; kullanıcı yönetir.' },
  { id: 'Muhasebe', label: 'Muhasebe', hint: 'Günlük iş: cari, tahsilat, fatura, rapor.' },
  { id: 'Ortak', label: 'Ortak', hint: 'Yalnız okur; ne göreceğini aşağıdaki kutular belirler.' },
]

const PERMISSION_LABEL: Record<string, string> = {
  'parties.read': 'Carileri görür',
  'ledger.read': 'Hesap hareketlerini görür',
  'documents.read': 'Faturaları görür',
  'catalog.read': 'Ürün ve hizmetleri görür',
  'cash.read': 'Kasa, banka, kredi, çek görür',
  'reports.summary': 'Özet raporları görür',
  'reports.detail': 'Ayrıntılı raporları görür',
  'audit.read': 'Denetim kaydını görür',
}

type Membership = {
  tenantId: string; code: string; name: string; role: CompanyRole
  grantedKeys: string[]; deniedKeys: string[]; effective: string[]
}

type UserRow = {
  id: string; userName: string; fullName: string; maxDiscountPercent: string; isActive: boolean
  isGroupAdmin: boolean; mfaEnabled: boolean; locked: boolean; memberships: Membership[]
}

type PermissionInfo = { all: string[]; grantable: string[]; templates: Record<string, string[]> }

/** Bir sirketteki form durumu: rol bos ise kullanicinin o sirkette uyeligi yoktur. */
type MembershipDraft = { role: CompanyRole | ''; keys: Set<string> }

export function UsersPage() {
  const toast = useToast()
  const { data, error, reload } = useAsync(() => api<UserRow[]>('/api/users'), [])
  const keys = useAsync(() => api<PermissionInfo>('/api/users/permission-keys'), [])
  const [editing, setEditing] = useState<UserRow | 'new' | null>(null)
  const [resetFor, setResetFor] = useState<UserRow | null>(null)
  return (
    <>
      <div className="page-head">
        <div>
          <h1>Kullanıcılar</h1>
          <p className="sub">Şirket bazında rol ve yetkiler. Her kullanıcı iki adımlı doğrulama kurmadan veriye erişemez.</p>
        </div>
        <button className="btn primary" type="button" onClick={() => setEditing('new')} disabled={!keys.data}>Yeni Kullanıcı</button>
      </div>
      <ErrorBox message={error ?? keys.error} />
      <div className="panel">
        {!data ? <Loading /> : data.length === 0 ? <Empty title="Kullanıcı yok" /> : (
          <div className="table-wrap">
            <table>
              <thead><tr><th>Kullanıcı</th><th>Şirket ve rol</th><th className="num">İskonto sınırı</th><th>Güvenlik</th><th><span className="sr-only">İşlemler</span></th></tr></thead>
              <tbody>{data.map((u) => (
                <tr key={u.id}>
                  <td>{u.fullName}<span className="sub">{u.userName}{!u.isActive ? ' · kapalı' : ''}</span></td>
                  <td>
                    <div className="row" style={{ gap: 6 }}>
                      {u.isGroupAdmin ? <span className="badge partial">Grup yöneticisi</span> : null}
                      {u.memberships.map((m) => (
                        <span key={m.tenantId} className="row" style={{ gap: 4 }}>
                          <CompanyTag code={m.code} /><span className="small">{ROLES.find((r) => r.id === m.role)?.label ?? m.role}</span>
                        </span>
                      ))}
                      {!u.isGroupAdmin && u.memberships.length === 0 ? <span className="muted small">Şirket yok</span> : null}
                    </div>
                  </td>
                  <td className="num">{percent(u.maxDiscountPercent)}</td>
                  <td>
                    {u.mfaEnabled ? <span className="badge done">İki adım açık</span> : <span className="badge pending">İki adım kurulmadı</span>}
                    {u.locked ? <span className="badge danger" style={{ marginLeft: 6 }}>Kilitli</span> : null}
                  </td>
                  <td className="num">
                    <div className="actions" style={{ justifyContent: 'flex-end' }}>
                      <button className="btn small ghost" type="button" onClick={() => setEditing(u)}>Düzenle</button>
                      <button className="btn small ghost" type="button" onClick={() => setResetFor(u)}>Parola</button>
                      {u.mfaEnabled ? <button className="btn small ghost" type="button" onClick={async () => {
                        if (!window.confirm(`${u.fullName} için iki adımlı doğrulama sıfırlansın mı? Kullanıcı bir sonraki girişte yeniden kurar.`)) return
                        try { await post(`/api/users/${u.id}/reset-mfa`); toast('İki adımlı doğrulama sıfırlandı.'); reload() } catch (e) { toast(errorText(e)) }
                      }}>Doğrulamayı Sıfırla</button> : null}
                    </div>
                  </td>
                </tr>
              ))}</tbody>
            </table>
          </div>
        )}
      </div>
      {keys.data ? (
        <UserDialog user={editing} info={keys.data} onClose={() => setEditing(null)}
          onSaved={() => { setEditing(null); toast('Kullanıcı kaydedildi.'); reload() }} />
      ) : null}
      <PasswordDialog user={resetFor} onClose={() => setResetFor(null)} onSaved={() => { setResetFor(null); toast('Parola değiştirildi.') }} />
    </>
  )
}

function UserDialog({ user, info, onClose, onSaved }: { user: UserRow | 'new' | null; info: PermissionInfo; onClose: () => void; onSaved: () => void }) {
  const { companies, isGroupAdmin } = useAuth()
  // Yonetilebilir sirketler: grup yoneticisi hepsini, sirket yoneticisi yalniz Yonetici oldugu sirketleri yonetir.
  const manageable = companies.filter((c) => isGroupAdmin || c.role === 'Yonetici')
  const blank = { userName: '', fullName: '', password: '', maxDiscountPercent: '10', isActive: true, isGroupAdmin: false }
  const [form, setForm] = useState(blank)
  const [drafts, setDrafts] = useState<Record<string, MembershipDraft>>({})
  const [error, setError] = useState<string | null>(null)
  const [loadedFor, setLoadedFor] = useState<unknown>(null)
  if (user !== loadedFor) {
    setLoadedFor(user); setError(null)
    const existing = user && user !== 'new' ? user : null
    setForm(existing ? {
      userName: existing.userName, fullName: existing.fullName, password: '', maxDiscountPercent: String(Number(existing.maxDiscountPercent)),
      isActive: existing.isActive, isGroupAdmin: existing.isGroupAdmin,
    } : blank)
    const next: Record<string, MembershipDraft> = {}
    for (const c of manageable) {
      const m = existing?.memberships.find((x) => x.tenantId === c.id)
      next[c.id] = m ? { role: m.role, keys: new Set(m.effective) } : { role: '', keys: new Set() }
    }
    setDrafts(next)
  }
  const isNew = user === 'new'

  const setRole = (companyId: string, role: CompanyRole | '') =>
    setDrafts((d) => ({ ...d, [companyId]: { role, keys: new Set(role ? info.templates[role] ?? [] : []) } }))
  const toggleKey = (companyId: string, key: string) =>
    setDrafts((d) => {
      const keys = new Set(d[companyId].keys)
      if (keys.has(key)) keys.delete(key); else keys.add(key)
      return { ...d, [companyId]: { ...d[companyId], keys } }
    })

  const save = async () => {
    setError(null)
    const memberships = manageable.flatMap((c) => {
      const d = drafts[c.id]
      if (!d || !d.role) return []
      const template = new Set(info.templates[d.role] ?? [])
      // Yalniz verilebilir anahtarlar sablondan sapabilir; yazma ve kullanici yonetimi role bagli kalir.
      const grantedKeys = info.grantable.filter((k) => d.keys.has(k) && !template.has(k))
      const deniedKeys = info.grantable.filter((k) => !d.keys.has(k) && template.has(k))
      return [{ tenantId: c.id, role: d.role, grantedKeys, deniedKeys }]
    })
    if (isNew && memberships.length === 0 && !form.isGroupAdmin) { setError('En az bir şirkette rol verin veya grup yöneticisi yapın.'); return }
    const discount = normalizeNumber(form.maxDiscountPercent)
    try {
      if (isNew) {
        await post('/api/users', {
          userName: form.userName, fullName: form.fullName, password: form.password, maxDiscountPercent: discount,
          memberships, isGroupAdmin: form.isGroupAdmin,
        })
      } else {
        await put(`/api/users/${(user as UserRow).id}`, {
          fullName: form.fullName, maxDiscountPercent: discount, isActive: form.isActive, memberships,
          isGroupAdmin: isGroupAdmin ? form.isGroupAdmin : null,
        })
      }
      onSaved()
    } catch (e) { setError(errorText(e)) }
  }

  return (
    <Dialog open={!!user} onClose={onClose} wide title={isNew ? 'Yeni kullanıcı' : 'Kullanıcıyı düzenle'}
      footer={<>
        <button type="button" className="btn" onClick={onClose}>Vazgeç</button>
        <button type="button" className="btn primary" onClick={save}>Kaydet</button>
      </>}>
      <ErrorBox message={error} />
      <div className="form-grid">
        <Field label="Ad soyad" full><input className="input" value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} /></Field>
        <Field label="Kullanıcı adı"><input className="input" value={form.userName} disabled={!isNew} onChange={(e) => setForm({ ...form, userName: e.target.value })} /></Field>
        {isNew ? <Field label="Geçici parola" hint="en az 12 karakter"><input className="input" type="password" autoComplete="new-password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} /></Field> : null}
        <Field label="İskonto sınırı (%)"><input className="input" inputMode="decimal" value={form.maxDiscountPercent} onChange={(e) => setForm({ ...form, maxDiscountPercent: e.target.value })} /></Field>
        {!isNew ? (
          <Field label="Erişim">
            <select className="input" value={form.isActive ? '1' : '0'} onChange={(e) => setForm({ ...form, isActive: e.target.value === '1' })}>
              <option value="1">Açık</option><option value="0">Kapalı (tüm oturumlar kapanır)</option>
            </select>
          </Field>
        ) : null}
        {isGroupAdmin ? (
          <label className="check full">
            <input type="checkbox" checked={form.isGroupAdmin} onChange={(e) => setForm({ ...form, isGroupAdmin: e.target.checked })} />
            <span><strong>Grup yöneticisi</strong><span className="sub">Tüm şirketleri görür ve yönetir; şirket ve kullanıcı açar. Yalnız güvendiğiniz kişi yapın.</span></span>
          </label>
        ) : null}
      </div>

      <h3>Şirketler ve roller</h3>
      {manageable.length === 0 ? <Empty title="Yönetebileceğiniz şirket yok" /> : manageable.map((c) => {
        const d = drafts[c.id] ?? { role: '', keys: new Set<string>() }
        return (
          <fieldset key={c.id} className={`membership${d.role ? '' : ' off'}`} style={{ margin: 0 }}>
            <legend className="sr-only">{c.name}</legend>
            <div className="membership-head">
              <span className="row" style={{ gap: 8 }}><CompanyTag code={c.code} /><strong>{c.name}</strong></span>
              <select className="input" style={{ width: 'auto' }} aria-label={`${c.name} rolü`} value={d.role}
                onChange={(e) => setRole(c.id, e.target.value as CompanyRole | '')}>
                <option value="">Erişimi yok</option>
                {ROLES.map((r) => <option key={r.id} value={r.id}>{r.label}</option>)}
              </select>
            </div>
            {d.role ? (
              <>
                <p className="muted small">{ROLES.find((r) => r.id === d.role)?.hint}</p>
                <div className="perm-grid">
                  {info.grantable.map((k) => (
                    <label key={k}>
                      <input type="checkbox" checked={d.keys.has(k)} onChange={() => toggleKey(c.id, k)} />
                      {PERMISSION_LABEL[k] ?? k}
                    </label>
                  ))}
                </div>
                <p className="muted small">
                  {d.role === 'Ortak' ? 'Ortak hiçbir koşulda kayıt giremez.' : d.role === 'Muhasebe' ? 'Kayıt girebilir; kullanıcı yönetemez.' : 'Kayıt girebilir ve kullanıcı yönetir.'}
                </p>
              </>
            ) : null}
          </fieldset>
        )
      })}
    </Dialog>
  )
}

function PasswordDialog({ user, onClose, onSaved }: { user: UserRow | null; onClose: () => void; onSaved: () => void }) {
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  return (
    <Dialog open={!!user} onClose={onClose} title={`Parola değiştir: ${user?.fullName ?? ''}`}
      footer={<>
        <button type="button" className="btn" onClick={onClose}>Vazgeç</button>
        <button type="button" className="btn primary" onClick={async () => {
          setError(null)
          try { await post(`/api/users/${user!.id}/password`, { password }); setPassword(''); onSaved() } catch (e) { setError(errorText(e)) }
        }}>Parolayı Değiştir</button>
      </>}>
      <ErrorBox message={error} />
      <Field label="Yeni parola" hint="en az 12 karakter, en az bir rakam">
        <input className="input" type="password" autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} />
      </Field>
    </Dialog>
  )
}

// =====================================================================
// Sirketler (yalniz grup yoneticisi)
// =====================================================================

type CompanyRow = {
  id: string; code: string; name: string; legalName: string | null; taxNumber: string | null
  taxOffice: string | null; colorKey: string | null; sortOrder: number; isActive: boolean
}

export function CompaniesPage() {
  const toast = useToast()
  const { refresh } = useAuth()
  const { data, error, reload } = useAsync(() => api<CompanyRow[]>('/api/companies/all'), [])
  const [editing, setEditing] = useState<CompanyRow | 'new' | null>(null)
  return (
    <>
      <div className="page-head">
        <div>
          <h1>Şirketler</h1>
          <p className="sub">Her vergi kimlik numarası ayrı bir şirkettir. Şirketin verisi diğerlerinden yalıtılır; birleşik görünüm yalnız okunur.</p>
        </div>
        <button className="btn primary" type="button" onClick={() => setEditing('new')}>Yeni Şirket</button>
      </div>
      <ErrorBox message={error} />
      <div className="panel">
        {!data ? <Loading /> : data.length === 0 ? <Empty title="Şirket yok" /> : (
          <div className="table-wrap">
            <table>
              <thead><tr><th>Kod</th><th>Ad</th><th>Unvan</th><th>Vergi kimlik no</th><th>Vergi dairesi</th><th>Durum</th><th><span className="sr-only">İşlemler</span></th></tr></thead>
              <tbody>{data.map((c) => (
                <tr key={c.id}>
                  <td><CompanyTag code={c.code} /></td>
                  <td>{c.name}</td>
                  <td>{c.legalName ?? <span className="muted">—</span>}</td>
                  <td>{c.taxNumber ?? <span className="muted">—</span>}</td>
                  <td>{c.taxOffice ?? <span className="muted">—</span>}</td>
                  <td>{c.isActive ? <span className="badge done">Aktif</span> : <span className="badge neutral">Pasif</span>}</td>
                  <td className="num"><button className="btn small ghost" type="button" onClick={() => setEditing(c)}>Düzenle</button></td>
                </tr>
              ))}</tbody>
            </table>
          </div>
        )}
      </div>
      <CompanyDialog company={editing} onClose={() => setEditing(null)}
        onSaved={async () => { setEditing(null); toast('Şirket kaydedildi.'); reload(); await refresh() }} />
    </>
  )
}

function CompanyDialog({ company, onClose, onSaved }: { company: CompanyRow | 'new' | null; onClose: () => void; onSaved: () => void }) {
  const blank = { name: '', code: '', legalName: '', taxNumber: '', taxOffice: '', sortOrder: '0', isActive: true }
  const [form, setForm] = useState(blank)
  const [error, setError] = useState<string | null>(null)
  const [loadedFor, setLoadedFor] = useState<unknown>(null)
  if (company !== loadedFor) {
    setLoadedFor(company); setError(null)
    setForm(company && company !== 'new' ? {
      name: company.name, code: company.code, legalName: company.legalName ?? '', taxNumber: company.taxNumber ?? '',
      taxOffice: company.taxOffice ?? '', sortOrder: String(company.sortOrder), isActive: company.isActive,
    } : blank)
  }
  const isNew = company === 'new'
  return (
    <Dialog open={!!company} onClose={onClose} title={isNew ? 'Yeni şirket' : 'Şirketi düzenle'}
      footer={<>
        <button type="button" className="btn" onClick={onClose}>Vazgeç</button>
        <button type="button" className="btn primary" onClick={async () => {
          setError(null)
          const body = {
            name: form.name, code: form.code, legalName: form.legalName, taxNumber: form.taxNumber, taxOffice: form.taxOffice,
            colorKey: null, sortOrder: Number(normalizeNumber(form.sortOrder)) || 0, isActive: form.isActive,
          }
          try {
            if (isNew) await post('/api/companies', body)
            else await put(`/api/companies/${(company as CompanyRow).id}`, body)
            onSaved()
          } catch (e) { setError(errorText(e)) }
        }}>Kaydet</button>
      </>}>
      <ErrorBox message={error} />
      <div className="form-grid">
        <Field label="Şirket adı"><input className="input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></Field>
        <Field label="Kısa kod" hint={isNew ? 'sonradan değişmez' : 'değiştirilemez'}>
          <input className="input" value={form.code} disabled={!isNew} maxLength={8} onChange={(e) => setForm({ ...form, code: e.target.value.toUpperCase() })} />
        </Field>
        <Field label="Ticari unvan" full><input className="input" value={form.legalName} onChange={(e) => setForm({ ...form, legalName: e.target.value })} /></Field>
        <Field label="Vergi kimlik no"><input className="input" inputMode="numeric" value={form.taxNumber} onChange={(e) => setForm({ ...form, taxNumber: e.target.value })} /></Field>
        <Field label="Vergi dairesi"><input className="input" value={form.taxOffice} onChange={(e) => setForm({ ...form, taxOffice: e.target.value })} /></Field>
        <Field label="Sıra" hint="seçicide soldan sağa"><input className="input" inputMode="numeric" value={form.sortOrder} onChange={(e) => setForm({ ...form, sortOrder: e.target.value })} /></Field>
        {!isNew ? (
          <Field label="Durum">
            <select className="input" value={form.isActive ? '1' : '0'} onChange={(e) => setForm({ ...form, isActive: e.target.value === '1' })}>
              <option value="1">Aktif</option><option value="0">Pasif (seçiciden kalkar, veri silinmez)</option>
            </select>
          </Field>
        ) : null}
      </div>
      <p className="muted small">Geliştirme ortamında gerçek vergi kimlik numarası girmeyin; gerçek değerler yalnız canlıda girilir.</p>
    </Dialog>
  )
}
