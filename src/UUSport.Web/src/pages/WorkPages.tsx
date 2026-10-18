import { useState } from 'react'
import { Link } from 'react-router'
import { api, post, put, type CashAccount, type DocumentRow, type Item, type LedgerListRow, type PartyListRow, type PaymentRow, type PendingWork } from '../api'
import { useAuth } from '../auth'
import { CompanyTag, Dialog, Empty, ErrorBox, Field, Loading, ReasonDialog, StatusBadge, Tabs, WriteScope, errorText, useAsync, useToast } from '../components'
import { date, dateTime, docTypeName, money, normalizeNumber, percent, quantity, today } from '../format'
import { LineTable, PaymentTable } from './PartyPages'

// =====================================================================
// Genel bakis
// =====================================================================

type Dashboard = {
  receiptsToday: string; receiptsThisMonth: string; openReceivables: string; customerAdvances: string
  salesAwaitingInvoice: number; purchasesAwaitingInvoice: number; unallocatedPayments: number; pendingOpeningBalances: number
  recentPayments: PaymentRow[]
  totalsByCompany: { company: string; receiptsToday: string; receiptsThisMonth: string; openReceivables: string; customerAdvances: string }[]
}

export function DashboardPage() {
  const { me, canWrite, consolidated } = useAuth()
  const { data, error } = useAsync(() => api<Dashboard>('/api/dashboard'), [])
  const pendingTotal = data ? data.salesAwaitingInvoice + data.purchasesAwaitingInvoice + data.unallocatedPayments + data.pendingOpeningBalances : 0
  return (
    <>
      <div className="page-head">
        <div>
          <h1>Günaydın{me?.fullName ? `, ${me.fullName.split(' ')[0]}` : ''}</h1>
          <p className="sub">{new Date().toLocaleDateString('tr-TR', { dateStyle: 'full' })} · Bugünkü durum</p>
        </div>
        <div className="actions">
          {canWrite ? <Link className="btn" to="/aktarim">Excel'den Aktar</Link> : null}
          <Link className="btn primary" to="/cariler">Cariye Git</Link>
        </div>
      </div>
      <ErrorBox message={error} />
      {!data ? <Loading /> : (
        <>
          <section className="cards" aria-label="Özet">
            <div className="card"><div className="label">Bugün tahsil edilen</div><div className="value">{money(data.receiptsToday)}</div><div className="note">Nakit + banka + POS</div></div>
            <div className="card"><div className="label">Bu ay tahsil edilen</div><div className="value">{money(data.receiptsThisMonth)}</div><div className="note">İptaller düşülmüş</div></div>
            <div className="card"><div className="label">Açık alacak</div><div className="value">{money(data.openReceivables)}</div><div className="note">Borçlu carilerin toplamı</div></div>
            <div className={`card${pendingTotal ? ' attention' : ''}`}>
              <div className="label">Muhasebe bekleyen</div><div className="value">{pendingTotal} iş</div>
              <div className="note">{data.salesAwaitingInvoice} satış faturası · {data.unallocatedPayments} dağıtım · {data.pendingOpeningBalances} açılış onayı</div>
            </div>
          </section>
          {consolidated ? (
            <div className="panel" style={{ marginBottom: 16 }}>
              <div className="panel-head"><h2>Şirket kırılımı</h2><span className="muted small">Yukarıdaki kartlar seçili şirketlerin toplamıdır.</span></div>
              <div className="table-wrap">
                <table>
                  <thead><tr><th>Şirket</th><th className="num">Bugün tahsil</th><th className="num">Bu ay tahsil</th><th className="num">Açık alacak</th><th className="num">Müşteri avansı</th></tr></thead>
                  <tbody>{data.totalsByCompany.map((t) => (
                    <tr key={t.company}><td><CompanyTag code={t.company} /></td><td className="num">{money(t.receiptsToday)}</td><td className="num">{money(t.receiptsThisMonth)}</td>
                      <td className="num">{money(t.openReceivables)}</td><td className="num">{money(t.customerAdvances)}</td></tr>
                  ))}</tbody>
                </table>
              </div>
            </div>
          ) : null}
          {pendingTotal ? (
            <div className="alert warn" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 12 }}>
              <span><strong>Bugün tamamlanması gereken {pendingTotal} iş var.</strong> Faturası girilmemiş satışlar ve dağıtılmamış tahsilatlar unutulmasın.</span>
              <Link className="btn small" to="/bekleyen">Bekleyenleri Gör</Link>
            </div>
          ) : null}
          <PaymentTable rows={data.recentPayments} title="Bu ayın son tahsilat ve ödemeleri" showParty />
        </>
      )}
    </>
  )
}

// =====================================================================
// Muhasebe bekleyen
// =====================================================================

export function PendingPage() {
  const toast = useToast()
  const { consolidated } = useAuth()
  const [tab, setTab] = useState('sales')
  const { data, error, reload } = useAsync(() => api<PendingWork>('/api/pending-work'), [])
  if (error) return <ErrorBox message={error} />
  if (!data) return <Loading />
  return (
    <>
      <div className="page-head">
        <div>
          <h1>Muhasebe Bekleyen</h1>
          <p className="sub">Unutulmaması gereken işler: faturası girilmemiş satış ve alışlar, borca dağıtılmamış tahsilatlar, onay bekleyen açılış bakiyeleri.</p>
        </div>
      </div>
      <div className="panel">
        <div className="panel-body">
          <Tabs label="Bekleyen iş türü" value={tab} onChange={setTab} tabs={[
            { id: 'sales', label: 'Portalda kesilmesi bekleyen', count: data.salesAwaitingInvoice.length },
            { id: 'purchases', label: 'Alış faturası bekleyen', count: data.purchasesAwaitingInvoice.length },
            { id: 'payments', label: 'Dağıtılmamış tahsilat', count: data.unallocatedPayments.length },
            { id: 'openings', label: 'Açılış bakiyesi onayı', count: data.pendingOpeningBalances.length },
          ]}>
            <div style={{ marginTop: 12 }}>
              {tab === 'sales' ? <LineTable rows={data.salesAwaitingInvoice} emptyTitle="Faturası beklenen satış yok" showParty /> : null}
              {tab === 'purchases' ? <LineTable rows={data.purchasesAwaitingInvoice} emptyTitle="Faturası beklenen alış yok" showParty /> : null}
              {tab === 'payments' ? <PaymentTable rows={data.unallocatedPayments} title="Borca bağlanmamış kayıtlar" showParty /> : null}
              {tab === 'openings' ? (
                data.pendingOpeningBalances.length === 0 ? <Empty title="Onay bekleyen açılış bakiyesi yok" /> : (
                  <div className="table-wrap">
                    <table>
                      <thead><tr><th>Cari</th><th className="num">Tutar</th><th>Dayanak</th><th><span className="sr-only">İşlemler</span></th></tr></thead>
                      <tbody>
                        {data.pendingOpeningBalances.map((o) => (
                          <tr key={o.id}>
                            <td>{consolidated ? <><CompanyTag code={o.company} />{' '}</> : null}<Link className="row-link" to={`/cariler/${o.partyId}`}>{o.partyName}</Link></td>
                            <td className="num">{money(Math.abs(Number(o.amount)))} <span className="small muted">{Number(o.amount) > 0 ? 'borçlu' : 'alacaklı'}</span></td>
                            <td className="small">{o.note ?? '—'}</td>
                            <td className="num">
                              <div className="actions" style={{ justifyContent: 'flex-end' }}>
                                <WriteScope>
                                <button className="btn small primary" type="button" onClick={async () => {
                                  try { await post(`/api/opening-balances/${o.id}/decide`, { approve: true }); toast('Açılış bakiyesi onaylandı ve cariye yazıldı.'); reload() } catch (e) { toast(errorText(e)) }
                                }}>Onayla</button>
                                <button className="btn small ghost" type="button" onClick={async () => {
                                  try { await post(`/api/opening-balances/${o.id}/decide`, { approve: false }); toast('Açılış bakiyesi reddedildi.'); reload() } catch (e) { toast(errorText(e)) }
                                }}>Reddet</button>
                                </WriteScope>
                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )
              ) : null}
            </div>
          </Tabs>
        </div>
        <p className="small muted" style={{ padding: '10px 16px' }}>Fatura girmek veya tahsilatı dağıtmak için ilgili cariyi açın.</p>
      </div>
    </>
  )
}

// =====================================================================
// Faturalar
// =====================================================================

export function DocumentsPage() {
  const toast = useToast()
  const { consolidated, canWrite } = useAuth()
  const { data, error, reload } = useAsync(() => api<DocumentRow[]>('/api/documents'), [])
  const [cancel, setCancel] = useState<DocumentRow | null>(null)
  return (
    <>
      <div className="page-head">
        <div>
          <h1>Faturalar</h1>
          <p className="sub">GİB portalında kesilip sisteme girilen satış faturaları ve alış faturaları.</p>
        </div>
      </div>
      <ErrorBox message={error} />
      <div className="panel">
        {!data ? <Loading /> : data.length === 0 ? <Empty title="Henüz fatura girilmedi">Fatura, cari ekranındaki “Kesilen Faturayı Gir” ile girilir.</Empty> : (
          <div className="table-wrap">
            <table>
              <thead><tr><th>Tarih</th><th>Numara</th><th>Cari</th><th className="num">Matrah</th><th className="num">KDV</th><th className="num">Toplam</th><th>Durum</th><th><span className="sr-only">İşlemler</span></th></tr></thead>
              <tbody>
                {data.map((d) => (
                  <tr key={d.id}>
                    <td>{date(d.documentDate)}</td>
                    <td>{d.documentNo}<span className="sub">{docTypeName(d.type)} · {d.direction === 'sale' ? 'satış' : 'alış'} · {d.linkCount} satır</span></td>
                    <td>{consolidated ? <><CompanyTag code={d.company} />{' '}</> : null}<Link className="row-link" to={`/cariler/${d.partyId}`}>{d.partyName}</Link></td>
                    <td className="num">{money(d.netTotal)}</td><td className="num">{money(d.vatTotal)}</td><td className="num">{money(d.grossTotal)}</td>
                    <td><StatusBadge status={d.status} />{d.cancelReason ? <span className="sub">{d.cancelReason}</span> : null}</td>
                    <td className="num">{d.status === 'issued' && canWrite ? <button type="button" className="btn small ghost" onClick={() => setCancel(d)}>İptal</button> : null}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
      <ReasonDialog open={!!cancel} title={`Faturayı iptal et: ${cancel?.documentNo ?? ''}`} confirmLabel="Faturayı İptal Et"
        description="Fatura kaydı iptal edilir; satış ve stok değişmez, satırlar yeniden faturalanabilir hale gelir. Portaldaki iptal işlemini ayrıca yapın."
        onClose={() => setCancel(null)} onConfirm={async (reason) => {
          await post(`/api/documents/${cancel!.id}/cancel`, { reason }); toast('Fatura iptal edildi.'); reload()
        }} />
    </>
  )
}

// =====================================================================
// Hesap hareketleri (tum cariler, surum 2 hazirlik notu)
// =====================================================================

export function LedgerListPage() {
  const { consolidated } = useAuth()
  const [partyId, setPartyId] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [officialOnly, setOfficialOnly] = useState(false)
  const parties = useAsync(() => api<PartyListRow[]>('/api/parties'), [])
  const { data, error, loading } = useAsync(() => {
    const params = new URLSearchParams()
    if (partyId) params.set('partyId', partyId)
    if (from) params.set('from', from)
    if (to) params.set('to', to)
    return api<LedgerListRow[]>(`/api/ledger?${params.toString()}`)
  }, [partyId, from, to])
  const rows = officialOnly ? (data ?? []).filter((r) => r.isOfficial) : (data ?? [])

  return (
    <>
      <div className="page-head">
        <div>
          <h1>Hesap Hareketleri</h1>
          <p className="sub">Tüm carilerin hareketleri tek listede.</p>
        </div>
      </div>
      <div className="panel">
        <div className="panel-head">
          <div className="row">
            <select className="input" style={{ width: 220 }} aria-label="Cari" value={partyId} onChange={(e) => setPartyId(e.target.value)}>
              <option value="">Tüm cariler</option>
              {(parties.data ?? []).map((p) => <option key={p.id} value={p.id}>{consolidated && p.company ? `${p.company} · ${p.name}` : p.name}</option>)}
            </select>
            <Field label="Başlangıç"><input className="input" type="date" value={from} onChange={(e) => setFrom(e.target.value)} /></Field>
            <Field label="Bitiş"><input className="input" type="date" value={to} onChange={(e) => setTo(e.target.value)} /></Field>
            <label className="statement-toggle">
              <input type="checkbox" checked={officialOnly} onChange={(e) => setOfficialOnly(e.target.checked)} />
              Muhasebe Görünümü
            </label>
          </div>
          <span className="muted small">{data ? `${rows.length} hareket` : ''}</span>
        </div>
        <ErrorBox message={error} />
        {loading && !data ? <Loading /> : rows.length === 0 ? (
          <Empty title="Hareket bulunamadı">Seçtiğiniz filtreye uyan cari hareket yok.</Empty>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Tarih</th><th>Cari</th><th>Açıklama</th><th>Muhasebe</th>
                  <th className="num">Borç</th><th className="num">Alacak</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((r) => (
                  <tr key={r.id}>
                    <td>{date(r.occurredOn)}</td>
                    <td>{consolidated ? <><CompanyTag code={r.company} />{' '}</> : null}<Link className="row-link" to={`/cariler/${r.partyId}`}>{r.partyName}</Link></td>
                    <td>{r.description}</td>
                    <td>{r.isOfficial ? <span className="badge done">Belgelendi</span> : <span className="badge pending">Belge bekliyor</span>}</td>
                    <td className="num">{Number(r.debit) ? money(r.debit) : '—'}</td>
                    <td className="num">{Number(r.credit) ? money(r.credit) : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </>
  )
}

// =====================================================================
// Urun ve hizmetler
// =====================================================================

export function CatalogPage() {
  const { consolidated, canWrite } = useAuth()
  const [q, setQ] = useState('')
  const [editing, setEditing] = useState<Item | 'new' | null>(null)
  const toast = useToast()
  const { data, error, reload } = useAsync(() => api<Item[]>(`/api/items?includeInactive=true&q=${encodeURIComponent(q)}`), [q])
  return (
    <>
      <div className="page-head">
        <div><h1>Ürün ve Hizmetler</h1><p className="sub">Stoklu ürünler ve hizmet kalemleri. Fiyat, KDV oranı ve stok durumu.</p></div>
        <WriteScope><button className="btn primary" type="button" onClick={() => setEditing('new')}>Yeni Kalem</button></WriteScope>
      </div>
      <div className="panel">
        <div className="panel-head">
          <input className="input search" type="search" placeholder="Ad veya kod" aria-label="Kalemlerde ara" value={q} onChange={(e) => setQ(e.target.value)} />
        </div>
        <ErrorBox message={error} />
        {!data ? <Loading /> : data.length === 0 ? <Empty title="Kalem yok">Yeni kalem ekleyin veya Excel'den aktarın.</Empty> : (
          <div className="table-wrap">
            <table>
              <thead><tr>{consolidated ? <th>Şirket</th> : null}<th>Kalem</th><th>Tür</th><th className="num">Birim fiyat</th><th className="num">KDV</th><th className="num">Stok</th><th><span className="sr-only">İşlemler</span></th></tr></thead>
              <tbody>
                {data.map((i) => (
                  <tr key={i.id}>
                    {consolidated ? <td className="company-col"><CompanyTag code={i.company} /></td> : null}
                    <td>{i.name}<span className="sub">{i.code}{!i.isActive ? ' · kullanım dışı' : ''}</span></td>
                    <td>{i.kind === 'stock' ? 'Stok' : 'Hizmet'}</td>
                    <td className="num">{money(i.unitPrice)}<span className="sub">{i.priceIncludesVat ? 'KDV dahil' : 'KDV hariç'}</span></td>
                    <td className="num">{percent(i.vatRate)}</td>
                    <td className="num">{i.kind === 'stock' ? <span className={Number(i.stockOnHand) < 0 ? 'badge danger' : undefined}>{quantity(i.stockOnHand)} {i.unit}</span> : '—'}</td>
                    <td className="num">{canWrite ? <button type="button" className="btn small ghost" onClick={() => setEditing(i)}>Düzenle</button> : null}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
      <ItemDialog item={editing} onClose={() => setEditing(null)} onSaved={() => { setEditing(null); toast('Kalem kaydedildi.'); reload() }} />
    </>
  )
}

function ItemDialog({ item, onClose, onSaved }: { item: Item | 'new' | null; onClose: () => void; onSaved: () => void }) {
  const blank = { name: '', kind: 'service', code: '', unit: 'adet', unitPrice: '', priceIncludesVat: true, vatRate: '10', accountCode: '', isActive: true }
  const [form, setForm] = useState(blank)
  const [error, setError] = useState<string | null>(null)
  const [loadedFor, setLoadedFor] = useState<unknown>(null)
  if (item !== loadedFor) {
    setLoadedFor(item)
    setError(null)
    setForm(item && item !== 'new' ? {
      name: item.name, kind: item.kind, code: item.code, unit: item.unit, unitPrice: String(item.unitPrice).replace('.', ','),
      priceIncludesVat: item.priceIncludesVat, vatRate: String(Number(item.vatRate)), accountCode: item.accountCode ?? '', isActive: item.isActive,
    } : blank)
  }
  return (
    <Dialog open={!!item} onClose={onClose} title={item === 'new' ? 'Yeni kalem' : 'Kalemi düzenle'}
      footer={<>
        <button type="button" className="btn" onClick={onClose}>Vazgeç</button>
        <button type="button" className="btn primary" onClick={async () => {
          setError(null)
          const body = { ...form, unitPrice: normalizeNumber(form.unitPrice), vatRate: normalizeNumber(form.vatRate) }
          try { if (item === 'new') await post('/api/items', body); else await put(`/api/items/${(item as Item).id}`, body); onSaved() }
          catch (e) { setError(errorText(e)) }
        }}>Kaydet</button>
      </>}>
      <ErrorBox message={error} />
      <div className="form-grid">
        <Field label="Ad" full><input className="input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} autoFocus /></Field>
        <Field label="Tür">
          <select className="input" value={form.kind} onChange={(e) => setForm({ ...form, kind: e.target.value })}>
            <option value="service">Hizmet</option><option value="stock">Stoklu ürün</option>
          </select>
        </Field>
        <Field label="Kod" hint="boşsa verilir"><input className="input" value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} /></Field>
        <Field label="Birim fiyat (TL)"><input className="input" inputMode="decimal" value={form.unitPrice} onChange={(e) => setForm({ ...form, unitPrice: e.target.value })} /></Field>
        <Field label="Fiyat">
          <select className="input" value={form.priceIncludesVat ? '1' : '0'} onChange={(e) => setForm({ ...form, priceIncludesVat: e.target.value === '1' })}>
            <option value="1">KDV dahil</option><option value="0">KDV hariç</option>
          </select>
        </Field>
        <Field label="KDV oranı (%)" hint="eğitim %10, malzeme %20 — müşavirle teyit">
          <input className="input" inputMode="decimal" value={form.vatRate} onChange={(e) => setForm({ ...form, vatRate: e.target.value })} />
        </Field>
        <Field label="Birim"><input className="input" value={form.unit} onChange={(e) => setForm({ ...form, unit: e.target.value })} /></Field>
        <Field label="Hesap kodu" hint="mali müşavirin mizanından; boş bırakılabilir">
          <input className="input" value={form.accountCode} onChange={(e) => setForm({ ...form, accountCode: e.target.value })} />
        </Field>
        {item !== 'new' ? (
          <Field label="Durum">
            <select className="input" value={form.isActive ? '1' : '0'} onChange={(e) => setForm({ ...form, isActive: e.target.value === '1' })}>
              <option value="1">Kullanımda</option><option value="0">Kullanım dışı</option>
            </select>
          </Field>
        ) : null}
      </div>
    </Dialog>
  )
}

// =====================================================================
// Kasa ve banka
// =====================================================================

const cashKindName: Record<string, string> = { cash: 'Kasa', bank: 'Banka', pos: 'POS', credit_card: 'Kredi Kartı', loan: 'Kredi' }

export function CashPage() {
  const { isAdmin, canWrite, consolidated } = useAuth()
  const toast = useToast()
  const { data, error, reload } = useAsync(() => api<CashAccount[]>('/api/cash-accounts'), [])
  const [form, setForm] = useState({ name: '', kind: 'bank', parentAccountId: '' })
  const [formError, setFormError] = useState<string | null>(null)

  const banks = (data ?? []).filter((a) => a.kind === 'bank')
  const needsParent = form.kind === 'credit_card' || form.kind === 'loan'
  // Banka hesaplarini once, altlarindaki kredi karti/kredi hesaplarini hemen ardindan sirala (surum 2 hazirlik notu madde 3).
  const ordered = (data ?? []).filter((a) => !a.parentAccountId)
    .flatMap((top) => [top, ...(data ?? []).filter((c) => c.parentAccountId === top.id)])
    .concat((data ?? []).filter((a) => a.parentAccountId && !(data ?? []).some((p) => p.id === a.parentAccountId)))

  return (
    <>
      <div className="page-head">
        <div><h1>Kasa ve Banka</h1><p className="sub">Tahsilat ve ödemelerin hesaplara göre bakiyesi. Kredi kartı ve kredi hesapları bir banka hesabının altında tanımlanır.</p></div>
        <div className="actions">
          <Link className="btn" to="/krediler">Krediler</Link>
          <Link className="btn" to="/cekler">Çek Portföyü</Link>
        </div>
      </div>
      <ErrorBox message={error} />
      <div className="grid-2">
        <div className="panel">
          {!data ? <Loading /> : (
            <table>
              <thead><tr>{consolidated ? <th>Şirket</th> : null}<th>Hesap</th><th>Tür</th><th className="num">Bakiye</th></tr></thead>
              <tbody>{ordered.map((a) => (
                <tr key={a.id}>
                  {consolidated ? <td className="company-col"><CompanyTag code={a.company} /></td> : null}
                  <td style={{ paddingLeft: a.parentAccountId ? 28 : undefined }}>
                    {a.parentAccountId ? '↳ ' : ''}{a.name}{!a.isActive ? <span className="sub">kullanım dışı</span> : null}
                  </td>
                  <td>{cashKindName[a.kind] ?? a.kind}</td>
                  <td className="num">{money(a.balance)}</td>
                </tr>
              ))}</tbody>
            </table>
          )}
        </div>
        {isAdmin && canWrite ? (
          <div className="panel">
            <div className="panel-head"><h2>Yeni hesap</h2></div>
            <div className="panel-body stack">
              <ErrorBox message={formError} />
              <Field label="Hesap adı"><input className="input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} placeholder="ör. Ziraat Bankası" /></Field>
              <Field label="Tür">
                <select className="input" value={form.kind} onChange={(e) => setForm({ ...form, kind: e.target.value, parentAccountId: '' })}>
                  <option value="bank">Banka</option><option value="cash">Kasa</option><option value="pos">POS</option>
                  <option value="credit_card">Kredi Kartı (bir bankanın altında)</option>
                  <option value="loan">Kredi (bir bankanın altında)</option>
                </select>
              </Field>
              {needsParent ? (
                <Field label="Hangi banka hesabının altında">
                  <select className="input" value={form.parentAccountId} onChange={(e) => setForm({ ...form, parentAccountId: e.target.value })}>
                    <option value="">— seçin —</option>
                    {banks.map((b) => <option key={b.id} value={b.id}>{b.name}</option>)}
                  </select>
                </Field>
              ) : null}
              <button className="btn primary" type="button" onClick={async () => {
                setFormError(null)
                if (needsParent && !form.parentAccountId) { setFormError('Üst banka hesabı seçilmeli.'); return }
                try {
                  await post('/api/cash-accounts', { name: form.name, kind: form.kind, parentAccountId: needsParent ? form.parentAccountId : null })
                  setForm({ name: '', kind: 'bank', parentAccountId: '' }); toast('Hesap eklendi.'); reload()
                } catch (e) { setFormError(errorText(e)) }
              }}>Hesabı Ekle</button>
            </div>
          </div>
        ) : null}
      </div>
    </>
  )
}

// =====================================================================
// Raporlar
// =====================================================================

export function ReportsPage() {
  const [tab, setTab] = useState('collections')
  const [from, setFrom] = useState(() => `${today().slice(0, 8)}01`)
  const [to, setTo] = useState(today())
  return (
    <>
      <div className="page-head">
        <div><h1>Raporlar</h1><p className="sub">Yönetim ve ön muhasebe raporları. Resmî beyan yerine geçmez.</p></div>
        <button className="btn" type="button" onClick={() => window.print()}>Yazdır</button>
      </div>
      <div className="panel">
        <div className="panel-body">
          <Tabs label="Rapor" value={tab} onChange={setTab} tabs={[
            { id: 'collections', label: 'Günlük tahsilat' }, { id: 'balances', label: 'Açık bakiyeler' },
            { id: 'uninvoiced', label: 'Faturası girilmemiş' }, { id: 'unallocated', label: 'Dağıtılmamış tahsilat' }, { id: 'stock', label: 'Stok durumu' },
          ]}>
            <div style={{ marginTop: 12 }}>
              {tab === 'collections' ? <CollectionsReport from={from} to={to} setFrom={setFrom} setTo={setTo} /> : null}
              {tab === 'balances' ? <BalancesReport /> : null}
              {tab === 'uninvoiced' ? <UninvoicedReport /> : null}
              {tab === 'unallocated' ? <UnallocatedReport /> : null}
              {tab === 'stock' ? <StockReport /> : null}
            </div>
          </Tabs>
        </div>
      </div>
    </>
  )
}

function CollectionsReport({ from, to, setFrom, setTo }: { from: string; to: string; setFrom: (v: string) => void; setTo: (v: string) => void }) {
  type R = {
    rows: { company: string; date: string; method: string; account: string; count: number; gross: string; reversed: string; net: string }[]
    total: string; totalsByCompany: { company: string; total: string }[]
  }
  const { consolidated } = useAuth()
  const { data, error } = useAsync(() => api<R>(`/api/reports/daily-collections?from=${from}&to=${to}`), [from, to])
  return (
    <div className="stack">
      <div className="row">
        <Field label="Başlangıç"><input className="input" type="date" value={from} onChange={(e) => setFrom(e.target.value)} /></Field>
        <Field label="Bitiş"><input className="input" type="date" value={to} onChange={(e) => setTo(e.target.value)} /></Field>
      </div>
      <ErrorBox message={error} />
      {!data ? <Loading /> : data.rows.length === 0 ? <Empty title="Bu aralıkta tahsilat yok" /> : (
        <table>
          <thead><tr>{consolidated ? <th>Şirket</th> : null}<th>Tarih</th><th>Araç</th><th>Hesap</th><th className="num">Adet</th><th className="num">Tahsilat</th><th className="num">İptal</th><th className="num">Net</th></tr></thead>
          <tbody>{data.rows.map((r, i) => (
            <tr key={i}>{consolidated ? <td className="company-col"><CompanyTag code={r.company} /></td> : null}<td>{date(r.date)}</td><td>{r.method}</td><td>{r.account}</td><td className="num">{r.count}</td>
              <td className="num">{money(r.gross)}</td><td className="num">{Number(r.reversed) ? money(r.reversed) : '—'}</td><td className="num">{money(r.net)}</td></tr>
          ))}</tbody>
          <tfoot>
            {consolidated ? data.totalsByCompany.map((t) => (
              <tr key={t.company}><td colSpan={7}><CompanyTag code={t.company} /> toplamı</td><td className="num">{money(t.total)}</td></tr>
            )) : null}
            <tr><td colSpan={consolidated ? 7 : 6}>{consolidated ? 'Genel toplam (seçili şirketler)' : 'Toplam'}</td><td className="num">{money(data.total)}</td></tr>
          </tfoot>
        </table>
      )}
    </div>
  )
}

function BalancesReport() {
  type R = {
    rows: { company: string; id: string; code: string; name: string; kind: string; phone: string | null; balance: string }[]
    totalReceivable: string; totalPayable: string; totalsByCompany: { company: string; totalReceivable: string; totalPayable: string }[]
  }
  const { consolidated } = useAuth()
  const { data, error } = useAsync(() => api<R>('/api/reports/open-balances'), [])
  if (error) return <ErrorBox message={error} />
  if (!data) return <Loading />
  return data.rows.length === 0 ? <Empty title="Açık bakiye yok" /> : (
    <table>
      <thead><tr><th>Cari</th><th>Telefon</th><th className="num">Borçlu</th><th className="num">Alacaklı</th></tr></thead>
      <tbody>{data.rows.map((r) => (
        <tr key={r.id}><td>{consolidated ? <><CompanyTag code={r.company} />{' '}</> : null}<Link className="row-link" to={`/cariler/${r.id}`}>{r.name}</Link><span className="sub">{r.code}</span></td><td>{r.phone ?? '—'}</td>
          <td className="num">{Number(r.balance) > 0 ? money(r.balance) : '—'}</td><td className="num">{Number(r.balance) < 0 ? money(-Number(r.balance)) : '—'}</td></tr>
      ))}</tbody>
      <tfoot>
        {consolidated ? data.totalsByCompany.map((t) => (
          <tr key={t.company}><td colSpan={2}><CompanyTag code={t.company} /> toplamı</td><td className="num">{money(t.totalReceivable)}</td><td className="num">{money(t.totalPayable)}</td></tr>
        )) : null}
        <tr><td colSpan={2}>{consolidated ? 'Genel toplam (seçili şirketler)' : 'Toplam'}</td><td className="num">{money(data.totalReceivable)}</td><td className="num">{money(data.totalPayable)}</td></tr>
      </tfoot>
    </table>
  )
}

function UninvoicedReport() {
  type R = { rows: PendingWork['salesAwaitingInvoice']; totalPendingGross: string }
  const { data, error } = useAsync(() => api<R>('/api/reports/uninvoiced'), [])
  if (error) return <ErrorBox message={error} />
  if (!data) return <Loading />
  return (
    <div className="stack">
      <p><strong>Faturası girilmemiş toplam: {money(data.totalPendingGross)}</strong> <span className="muted small">— stoktan çıkmış veya hizmeti verilmiş, faturası kesilmemiş.</span></p>
      <LineTable rows={data.rows} emptyTitle="Faturası girilmemiş satış yok" showParty />
    </div>
  )
}

function UnallocatedReport() {
  const { data, error } = useAsync(() => api<{ rows: PaymentRow[]; total: string }>('/api/reports/unallocated'), [])
  if (error) return <ErrorBox message={error} />
  if (!data) return <Loading />
  return (
    <div className="stack">
      <p><strong>Dağıtılmamış toplam: {money(data.total)}</strong></p>
      <PaymentTable rows={data.rows} title="Borca bağlanmamış tahsilat ve ödemeler" showParty />
    </div>
  )
}

function StockReport() {
  type R = { company: string; id: string; code: string; name: string; unit: string; isActive: boolean; stockOnHand: string }[]
  const { consolidated } = useAuth()
  const { data, error } = useAsync(() => api<R>('/api/reports/stock'), [])
  if (error) return <ErrorBox message={error} />
  if (!data) return <Loading />
  return data.length === 0 ? <Empty title="Stoklu kalem yok" /> : (
    <table>
      <thead><tr><th>Kalem</th><th className="num">Mevcut</th></tr></thead>
      <tbody>{data.map((i) => (
        <tr key={i.id}><td>{consolidated ? <><CompanyTag code={i.company} />{' '}</> : null}{i.name}<span className="sub">{i.code}</span></td>
          <td className="num">{Number(i.stockOnHand) < 0 ? <span className="badge danger">{quantity(i.stockOnHand)} {i.unit} — eksi stok</span> : `${quantity(i.stockOnHand)} ${i.unit}`}</td></tr>
      ))}</tbody>
    </table>
  )
}

// =====================================================================
// Yonetim
// =====================================================================

export function AuditPage() {
  type Row = { id: string; occurredAt: string; userName: string | null; action: string; targetType: string; targetId: string | null; summary: string | null }
  const [action, setAction] = useState('')
  const { data, error } = useAsync(() => api<Row[]>(`/api/audit?take=300&action=${encodeURIComponent(action)}`), [action])
  return (
    <>
      <div className="page-head"><div><h1>Denetim Kaydı</h1><p className="sub">Kim, ne zaman, ne yaptı. Kayıtlar değiştirilemez ve silinemez.</p></div></div>
      <div className="panel">
        <div className="panel-head">
          <input className="input search" placeholder="İşlem türü (ör. payment, document)" aria-label="İşlem türüne göre süz" value={action} onChange={(e) => setAction(e.target.value)} />
        </div>
        <ErrorBox message={error} />
        {!data ? <Loading /> : data.length === 0 ? <Empty title="Kayıt yok" /> : (
          <div className="table-wrap">
            <table>
              <thead><tr><th>Zaman</th><th>Kullanıcı</th><th>İşlem</th><th>Ayrıntı</th></tr></thead>
              <tbody>{data.map((r) => (
                <tr key={r.id}><td className="small">{dateTime(r.occurredAt)}</td><td>{r.userName ?? '—'}</td><td><code>{r.action}</code></td>
                  <td className="small" style={{ maxWidth: 520, wordBreak: 'break-word' }}>{r.summary ?? '—'}</td></tr>
              ))}</tbody>
            </table>
          </div>
        )}
      </div>
    </>
  )
}
