import { useState } from 'react'
import { createPortal } from 'react-dom'
import { Link, useNavigate, useParams } from 'react-router'
import { api, post, put, type DocumentRow, type LedgerRow, type LineRow, type PartyDetail, type PartyKind, type PartyListRow, type PaymentRow } from '../api'
import { Dialog, Empty, ErrorBox, Field, Loading, ReasonDialog, StatusBadge, Tabs, errorText, useAsync, useToast } from '../components'
import { date, docTypeName, kindName, money, quantity } from '../format'
import { AllocateDialog, DocumentDialog, InvoicePrepDialog, OpeningBalanceDialog, PaymentDialog, ReverseDialog, TradeLineDialog } from './Dialogs'

// =====================================================================
// Cari listesi
// =====================================================================

export function PartyListPage() {
  const [q, setQ] = useState('')
  const [kind, setKind] = useState('')
  const [creating, setCreating] = useState(false)
  const navigate = useNavigate()
  const { data, error, loading } = useAsync(
    () => api<PartyListRow[]>(`/api/parties?q=${encodeURIComponent(q)}&kind=${kind}`), [q, kind])

  return (
    <>
      <div className="page-head">
        <div>
          <h1>Cariler</h1>
          <p className="sub">Veliler, müşteriler ve tedarikçiler. Aynı kişi için tek cari.</p>
        </div>
        <button className="btn primary" type="button" onClick={() => setCreating(true)}>Yeni Cari</button>
      </div>
      <div className="panel">
        <div className="panel-head">
          <div className="row">
            <input className="input search" type="search" placeholder="Ad, kod, telefon veya sporcu adı" aria-label="Carilerde ara"
              value={q} onChange={(e) => setQ(e.target.value)} />
            <select className="input" style={{ width: 200 }} aria-label="Cari türü" value={kind} onChange={(e) => setKind(e.target.value)}>
              <option value="">Tümü</option>
              <option value="customer">Müşteri / veli</option>
              <option value="supplier">Tedarikçi</option>
            </select>
          </div>
          <span className="muted small">{data ? `${data.length} cari` : ''}</span>
        </div>
        <ErrorBox message={error} />
        {loading && !data ? <Loading /> : data && data.length === 0 ? (
          <Empty title="Cari bulunamadı">Yeni cari ekleyin veya Excel aktarımını kullanın.</Empty>
        ) : (
          <div className="table-wrap">
            <table>
              <thead><tr><th>Cari</th><th>Tür</th><th>Sporcular</th><th>Telefon</th><th className="num">Bakiye</th></tr></thead>
              <tbody>
                {data?.map((p) => (
                  <tr key={p.id}>
                    <td><Link className="row-link" to={`/cariler/${p.id}`}>{p.name}</Link><span className="sub">{p.code}</span></td>
                    <td>{kindName(p.kind)}</td>
                    <td className="small">{p.athletes.join(', ') || <span className="muted">—</span>}</td>
                    <td>{p.phone ?? '—'}</td>
                    <td className="num"><Balance value={p.balance} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
      <PartyEditDialog open={creating} onClose={() => setCreating(false)} onSaved={(id) => { setCreating(false); navigate(`/cariler/${id}`) }} />
    </>
  )
}

function Balance({ value }: { value: string }) {
  const n = Number(value)
  if (n === 0) return <span className="muted">{money(0)}</span>
  return <span>{money(Math.abs(n))} <span className="small muted">{n > 0 ? 'borçlu' : 'alacaklı'}</span></span>
}

export function PartyEditDialog({ open, party, onClose, onSaved }: {
  open: boolean; party?: PartyDetail['party']; onClose: () => void; onSaved: (id: string) => void
}) {
  const [form, setForm] = useState({ name: '', kind: 'customer' as PartyKind, code: '', phone: '', email: '', taxNumber: '', notes: '', accountCode: '', isActive: true })
  const [error, setError] = useState<string | null>(null)
  const [loadedFor, setLoadedFor] = useState<string | null>(null)
  const key = open ? (party?.id ?? 'new') : null
  if (key !== loadedFor) {
    setLoadedFor(key)
    setError(null)
    setForm(party ? {
      name: party.name, kind: party.kind, code: party.code, phone: party.phone ?? '', email: party.email ?? '',
      taxNumber: party.taxNumber ?? '', notes: party.notes ?? '', accountCode: party.accountCode ?? '', isActive: party.isActive,
    } : { name: '', kind: 'customer', code: '', phone: '', email: '', taxNumber: '', notes: '', accountCode: '', isActive: true })
  }
  const set = (k: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => setForm({ ...form, [k]: e.target.value })
  return (
    <Dialog open={open} onClose={onClose} title={party ? 'Cariyi düzenle' : 'Yeni cari'}
      footer={<>
        <button type="button" className="btn" onClick={onClose}>Vazgeç</button>
        <button type="button" className="btn primary" onClick={async () => {
          setError(null)
          try {
            if (party) { await put(`/api/parties/${party.id}`, form); onSaved(party.id) }
            else { const r = await post<{ id: string }>('/api/parties', form); onSaved(r.id) }
          } catch (e) { setError(errorText(e)) }
        }}>Kaydet</button>
      </>}>
      <ErrorBox message={error} />
      <div className="form-grid">
        <Field label="Ad soyad / unvan" full><input className="input" value={form.name} onChange={set('name')} autoFocus /></Field>
        <Field label="Tür">
          <select className="input" value={form.kind} onChange={set('kind')}>
            <option value="customer">Müşteri / veli</option>
            <option value="supplier">Tedarikçi</option>
            <option value="both">Müşteri ve tedarikçi</option>
          </select>
        </Field>
        <Field label="Cari kodu" hint="boş bırakılırsa verilir"><input className="input" value={form.code} onChange={set('code')} /></Field>
        <Field label="Telefon"><input className="input" value={form.phone} onChange={set('phone')} inputMode="tel" /></Field>
        <Field label="E-posta"><input className="input" value={form.email} onChange={set('email')} type="email" /></Field>
        <Field label="Vergi / TC no"><input className="input" value={form.taxNumber} onChange={set('taxNumber')} inputMode="numeric" /></Field>
        <Field label="Hesap kodu" hint="mali müşavirin mizanından; boş bırakılabilir">
          <input className="input" value={form.accountCode} onChange={set('accountCode')} />
        </Field>
        {party ? (
          <Field label="Durum">
            <select className="input" value={form.isActive ? '1' : '0'} onChange={(e) => setForm({ ...form, isActive: e.target.value === '1' })}>
              <option value="1">Kullanımda</option><option value="0">Kullanım dışı</option>
            </select>
          </Field>
        ) : null}
        <Field label="Not" full><textarea className="input" value={form.notes} onChange={set('notes')} /></Field>
      </div>
    </Dialog>
  )
}

// =====================================================================
// Cari detay: uc gorunum (ADR-004 bolum 2.4)
// =====================================================================

type Modal =
  | { kind: 'none' } | { kind: 'edit' } | { kind: 'trade'; direction: 'sale' | 'purchase' }
  | { kind: 'payment'; direction: 'receipt' | 'disbursement' }
  | { kind: 'document'; direction: 'sale' | 'purchase'; preselect: string[] }
  | { kind: 'prep'; line: LineRow } | { kind: 'reverse'; line: LineRow } | { kind: 'allocate'; payment: PaymentRow }
  | { kind: 'reversePayment'; payment: PaymentRow } | { kind: 'opening' } | { kind: 'athlete' } | { kind: 'statement' }

export function PartyDetailPage() {
  const { id = '' } = useParams()
  const toast = useToast()
  const [view, setView] = useState('all')
  const [modal, setModal] = useState<Modal>({ kind: 'none' })
  const detail = useAsync(() => api<PartyDetail>(`/api/parties/${id}`), [id])
  const lines = useAsync(() => api<LineRow[]>(`/api/parties/${id}/lines`), [id])
  const ledger = useAsync(() => api<LedgerRow[]>(`/api/parties/${id}/ledger`), [id])
  const payments = useAsync(() => api<PaymentRow[]>(`/api/parties/${id}/payments`), [id])
  const documents = useAsync(() => api<DocumentRow[]>(`/api/parties/${id}/documents`), [id])

  const reloadAll = () => { detail.reload(); lines.reload(); ledger.reload(); payments.reload(); documents.reload() }
  const close = () => setModal({ kind: 'none' })
  const done = (message: string) => { close(); toast(message); reloadAll() }

  if (detail.error) return <ErrorBox message={detail.error} />
  if (!detail.data) return <Loading />
  const { party, summary, athletes } = detail.data
  const isCustomer = party.kind !== 'supplier'
  const isSupplier = party.kind !== 'customer'
  const allLines = lines.data ?? []
  const official = allLines.filter((l) => Number(l.documentedQuantity) !== 0)
  const pendingLines = allLines.filter((l) => Number(l.pendingQuantity) > 0)
  const openPayments = (payments.data ?? []).filter((p) => Number(p.unallocatedAmount) > 0)
  const openSaleLines = allLines.filter((l) => l.direction === 'sale' && !l.reversesLineId && Number(l.openAmount) > 0)
  const openPurchaseLines = allLines.filter((l) => l.direction === 'purchase' && !l.reversesLineId && Number(l.openAmount) > 0)
  const balance = Number(summary.balance)

  return (
    <>
      <div className="page-head">
        <div>
          <div className="eyebrow">{kindName(party.kind)}</div>
          <h1>{party.name}</h1>
          <p className="sub">
            {party.code}{party.phone ? ` · ${party.phone}` : ''}{athletes.length ? ` · ${athletes.filter((a) => a.isActive).length} bağlı sporcu` : ''}
            {!party.isActive ? ' · kullanım dışı' : ''}
          </p>
        </div>
        <div className="actions">
          <button className="btn ghost" type="button" onClick={() => setModal({ kind: 'statement' })}>Ekstre Al</button>
          {isCustomer ? <>
            <button className="btn" type="button" onClick={() => setModal({ kind: 'trade', direction: 'sale' })}>Stok veya Hizmet Çıkışı</button>
            <button className="btn primary" type="button" onClick={() => setModal({ kind: 'payment', direction: 'receipt' })}>Tahsilat Al</button>
            <button className="btn" type="button" onClick={() => setModal({ kind: 'document', direction: 'sale', preselect: [] })}>Kesilen Faturayı Gir</button>
          </> : null}
          {isSupplier ? <>
            <button className="btn" type="button" onClick={() => setModal({ kind: 'trade', direction: 'purchase' })}>Alış Girişi</button>
            <button className="btn" type="button" onClick={() => setModal({ kind: 'document', direction: 'purchase', preselect: [] })}>Alış Faturası Gir</button>
            <button className={isCustomer ? 'btn' : 'btn primary'} type="button" onClick={() => setModal({ kind: 'payment', direction: 'disbursement' })}>Ödeme Yap</button>
          </> : null}
        </div>
      </div>

      <section className="cards" aria-label="Cari özeti">
        <div className="card balance">
          <div className="label">Toplam cari bakiye</div>
          <div className="value">{money(Math.abs(balance))}</div>
          <div className="note">{balance > 0 ? 'Cari bize borçlu' : balance < 0 ? 'Cari alacaklı / avans' : 'Hesap kapalı'}</div>
        </div>
        <div className="card">
          <div className="label">Belgeli satış</div>
          <div className="value">{money(summary.documentedSalesGross)}</div>
          <div className="note">Faturası girilmiş satış tutarı</div>
        </div>
        <div className={`card${summary.pendingLineCount ? ' attention' : ''}`}>
          <div className="label">Faturası girilmemiş</div>
          <div className="value">{money(summary.pendingSalesGross)}</div>
          <div className="note">{summary.pendingLineCount} satır muhasebe bekliyor</div>
        </div>
        <div className={`card${summary.unallocatedPaymentCount ? ' attention' : ''}`}>
          <div className="label">Dağıtılmamış tahsilat</div>
          <div className="value">{money(summary.unallocatedReceipts)}</div>
          <div className="note">{summary.unallocatedPaymentCount} tahsilat borca bağlanmadı</div>
        </div>
      </section>

      <div className="panel">
        <div className="panel-body">
          <Tabs label="Cari görünümü" value={view} onChange={setView} tabs={[
            { id: 'all', label: 'Toplam', count: ledger.data?.length },
            { id: 'official', label: 'Belgeli', count: official.length },
            { id: 'pending', label: 'Muhasebe Bekleyen', count: pendingLines.length + openPayments.length },
          ]}>
            <div style={{ marginTop: 12 }}>
              {view === 'all' ? <LedgerTable rows={ledger.data} /> : null}
              {view === 'official' ? (
                <LineTable rows={official} emptyTitle="Belgelenmiş satır yok"
                  onReverse={(l) => setModal({ kind: 'reverse', line: l })} />
              ) : null}
              {view === 'pending' ? (
                <div className="stack">
                  <LineTable rows={pendingLines} emptyTitle="Faturası beklenen satır yok"
                    onReverse={(l) => setModal({ kind: 'reverse', line: l })}
                    onInvoice={(l) => setModal({ kind: 'document', direction: l.direction, preselect: [l.id] })} />
                  <PaymentTable rows={openPayments} title="Dağıtılmamış tahsilat / ödeme"
                    onAllocate={(p) => setModal({ kind: 'allocate', payment: p })}
                    onReverse={(p) => setModal({ kind: 'reversePayment', payment: p })} />
                </div>
              ) : null}
            </div>
          </Tabs>
        </div>
        <p className="small muted" style={{ padding: '10px 16px' }}>
          Toplam görünüm tüm hareketleri ve yürüyen bakiyeyi gösterir. Fatura satışın belgesidir; bakiyeye ikinci kez eklenmez.
        </p>
      </div>

      <div className="panel" style={{ marginTop: 16 }}>
        <div className="panel-head">
          <h2>Tüm satırlar</h2>
        </div>
        <LineTable rows={allLines} emptyTitle="Henüz satış veya alış yok" onReverse={(l) => setModal({ kind: 'reverse', line: l })} />
      </div>
      <div className="grid-2" style={{ marginTop: 16 }}>
        <div className="stack">
          <PaymentTable rows={payments.data ?? []} title="Tahsilat ve ödemeler"
            onAllocate={(p) => setModal({ kind: 'allocate', payment: p })} onReverse={(p) => setModal({ kind: 'reversePayment', payment: p })} />
          <DocumentTable rows={documents.data ?? []} />
        </div>
        <div className="stack">
          <div className="panel">
            <div className="panel-head">
              <h2>Sporcular</h2>
              <button className="btn small" type="button" onClick={() => setModal({ kind: 'athlete' })}>Sporcu Ekle</button>
            </div>
            {athletes.length === 0 ? <Empty title="Bağlı sporcu yok" /> : (
              <table>
                <thead><tr><th>Ad soyad</th><th>Grup</th><th>Doğum yılı</th></tr></thead>
                <tbody>{athletes.map((a) => (
                  <tr key={a.id}><td>{a.firstName} {a.lastName}{!a.isActive ? <span className="sub">kullanım dışı</span> : null}</td><td>{a.groupName ?? '—'}</td><td>{a.birthYear ?? '—'}</td></tr>
                ))}</tbody>
              </table>
            )}
          </div>
          <div className="actions">
            <button className="btn" type="button" onClick={() => setModal({ kind: 'edit' })}>Cariyi Düzenle</button>
            <button className="btn" type="button" onClick={() => setModal({ kind: 'opening' })}>Açılış Bakiyesi</button>
          </div>
        </div>
      </div>

      {/* ---- pencereler ---- */}
      <PartyEditDialog open={modal.kind === 'edit'} party={party} onClose={close} onSaved={() => done('Cari güncellendi.')} />
      <TradeLineDialog open={modal.kind === 'trade'} direction={modal.kind === 'trade' ? modal.direction : 'sale'} partyId={id} onClose={close}
        onSaved={async (lineId, isOfficial) => {
          reloadAll()
          if (isOfficial) {
            const fresh = await api<LineRow[]>(`/api/parties/${id}/lines`)
            const line = fresh.find((l) => l.id === lineId)
            if (line) { setModal({ kind: 'prep', line }); return }
          }
          done(modal.kind === 'trade' && modal.direction === 'purchase' ? 'Alış kaydedildi.' : 'Çıkış kaydedildi ve Muhasebe Bekleyen listesine düştü.')
        }} />
      <PaymentDialog open={modal.kind === 'payment'} direction={modal.kind === 'payment' ? modal.direction : 'receipt'} partyId={id}
        openLines={modal.kind === 'payment' && modal.direction === 'disbursement' ? openPurchaseLines : openSaleLines}
        onClose={close} onSaved={() => done('Kaydedildi.')} />
      <DocumentDialog open={modal.kind === 'document'} direction={modal.kind === 'document' ? modal.direction : 'sale'} partyId={id}
        lines={allLines} preselect={modal.kind === 'document' ? modal.preselect : []} onClose={close} onSaved={done} />
      <InvoicePrepDialog open={modal.kind === 'prep'} line={modal.kind === 'prep' ? modal.line : null} partyName={party.name}
        onClose={() => done('Çıkış kaydedildi; fatura girilene kadar Muhasebe Bekleyen listesinde.')}
        onEnterInvoice={() => modal.kind === 'prep' && setModal({ kind: 'document', direction: 'sale', preselect: [modal.line.id] })} />
      <ReverseDialog open={modal.kind === 'reverse'} line={modal.kind === 'reverse' ? modal.line : null} onClose={close} onSaved={() => done('İade kaydedildi.')} />
      <AllocateDialog open={modal.kind === 'allocate'} payment={modal.kind === 'allocate' ? modal.payment : null}
        openLines={modal.kind === 'allocate' && modal.payment.direction === 'disbursement' ? openPurchaseLines : openSaleLines}
        onClose={close} onSaved={() => done('Dağıtım kaydedildi.')} />
      <ReasonDialog open={modal.kind === 'reversePayment'} title="Kaydı iptal et" confirmLabel="İptal Et"
        description="Kayıt silinmez; ters kayıt oluşur ve cari bakiye düzelir. Bu kaydın dağıtımları da geri alınır."
        onClose={close} onConfirm={async (reason) => {
          if (modal.kind !== 'reversePayment') return
          await post(`/api/payments/${modal.payment.id}/reverse`, { reason })
          toast('Kayıt iptal edildi.'); reloadAll()
        }} />
      <OpeningBalanceDialog open={modal.kind === 'opening'} partyId={id} onClose={close} onSaved={() => done('Açılış bakiyesi onaya gönderildi.')} />
      <AthleteDialog open={modal.kind === 'athlete'} partyId={id} onClose={close} onSaved={() => done('Sporcu eklendi.')} />
      <StatementDialog open={modal.kind === 'statement'} party={party} rows={ledger.data} onClose={close} />
    </>
  )
}

// =====================================================================
// Ekstre (surum 2 hazirlik notu madde 2): ayri pencere, "Muhasebe Gorunumu" filtresi,
// ekranda gorundugu sekilde yazdirma/PDF (tarayicinin "PDF olarak kaydet" ciktisi).
// Gecici: muhasebe gorunumunde yuruyen bakiye gosterilmez (toplam defterden geldigi icin tutarsizdi).
// Kalici cozum M2/S-21: sunucuda kendi devri ve yuruyen bakiyesiyle ekstre.
// =====================================================================

function StatementDialog({ open, party, rows, onClose }: {
  open: boolean; party: PartyDetail['party']; rows: LedgerRow[] | null; onClose: () => void
}) {
  const [officialOnly, setOfficialOnly] = useState(false)
  const all = rows ?? []
  const shown = officialOnly ? all.filter((r) => r.isOfficial) : all
  const totalDebit = shown.reduce((s, r) => s + Number(r.debit), 0)
  const totalCredit = shown.reduce((s, r) => s + Number(r.credit), 0)
  const currentBalance = all.length ? all[all.length - 1].balance : '0'
  const net = totalDebit - totalCredit

  const content = (
    <div className="statement-print">
      <div className="statement-head">
        <div>
          <strong>UU Sport</strong>
          <div className="muted small">{party.name}{party.code ? ` · ${party.code}` : ''}</div>
          <div className="muted small">Ekstre tarihi: {date(new Date().toISOString().slice(0, 10))}</div>
          <div className="small statement-scope">
            {officialOnly ? 'Muhasebe görünümü — yalnız belgelenmiş hareketler; tüm hareketleri içermez' : 'Tüm hareketler'}
          </div>
        </div>
        <label className="statement-toggle">
          <input type="checkbox" checked={officialOnly} onChange={(e) => setOfficialOnly(e.target.checked)} />
          Muhasebe Görünümü
        </label>
      </div>
      {!rows ? <Loading /> : shown.length === 0 ? (
        <Empty title={officialOnly ? 'Muhasebe görünümünde hareket yok' : 'Cari hareket yok'} />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Tarih</th><th>Açıklama</th>{!officialOnly ? <th>Muhasebe</th> : null}
                <th className="num">Borç</th><th className="num">Alacak</th>{!officialOnly ? <th className="num">Bakiye</th> : null}
              </tr>
            </thead>
            <tbody>
              {shown.map((r) => (
                <tr key={r.id}>
                  <td>{date(r.occurredOn)}</td>
                  <td>{r.description}</td>
                  {!officialOnly ? <td>{r.isOfficial ? <span className="badge done">Belgelendi</span> : <span className="badge pending">Belge bekliyor</span>}</td> : null}
                  <td className="num">{Number(r.debit) ? money(r.debit) : '—'}</td>
                  <td className="num">{Number(r.credit) ? money(r.credit) : '—'}</td>
                  {!officialOnly ? <td className="num"><Balance value={r.balance} /></td> : null}
                </tr>
              ))}
            </tbody>
            <tfoot>
              {officialOnly ? (
                <tr>
                  <td colSpan={2}>Toplam (net: <Balance value={String(net)} />)</td>
                  <td className="num">{money(totalDebit)}</td>
                  <td className="num">{money(totalCredit)}</td>
                </tr>
              ) : (
                <tr>
                  <td colSpan={3}>Toplam</td>
                  <td className="num">{money(totalDebit)}</td>
                  <td className="num">{money(totalCredit)}</td>
                  <td className="num"><Balance value={currentBalance} /></td>
                </tr>
              )}
            </tfoot>
          </table>
        </div>
      )}
      <p className="small muted statement-footnote">
        {officialOnly
          ? 'Belgelenmiş hareketler dökümüdür; belge bekleyen hareketleri içermez.'
          : '"Muhasebe" sütunu, hareketin belgelenip belgelenmediğini gösterir.'}
      </p>
    </div>
  )

  return (
    <>
      <Dialog open={open} onClose={onClose} wide title={`Cari Ekstresi — ${party.name}`}
        footer={<>
          <button type="button" className="btn" onClick={onClose}>Kapat</button>
          <button type="button" className="btn primary" onClick={() => window.print()}>Yazdır / PDF Kaydet</button>
        </>}>
        {content}
      </Dialog>
      {/* <dialog> yerlesik "ust katman"da calisir; tarayicilarin yazdirma/PDF cikisi bu katmani
          guvenilir sekilde basmiyor. Bu yuzden ayni icerik, pencere acikken document.body'ye
          ayri bir kopya olarak da eklenir; ekranda gizli kalir, yalniz @media print'te gorunur. */}
      {open ? createPortal(<div className="statement-print-portal">{content}</div>, document.body) : null}
    </>
  )
}

function LedgerTable({ rows }: { rows: LedgerRow[] | null }) {
  if (!rows) return <Loading />
  if (rows.length === 0) return <Empty title="Cari hareket yok" />
  return (
    <div className="table-wrap">
      <table>
        <thead><tr><th>Tarih</th><th>Açıklama</th><th className="num">Borç</th><th className="num">Alacak</th><th className="num">Bakiye</th></tr></thead>
        <tbody>
          {rows.map((r) => (
            <tr key={r.id}>
              <td>{date(r.occurredOn)}</td>
              <td>{r.description}</td>
              <td className="num">{Number(r.debit) ? money(r.debit) : '—'}</td>
              <td className="num">{Number(r.credit) ? money(r.credit) : '—'}</td>
              <td className="num"><Balance value={r.balance} /></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

export function LineTable({ rows, emptyTitle, onReverse, onInvoice, compact, showParty }: {
  rows: LineRow[]; emptyTitle: string; onReverse?: (l: LineRow) => void; onInvoice?: (l: LineRow) => void; compact?: boolean; showParty?: boolean
}) {
  if (rows.length === 0) return <Empty title={emptyTitle} />
  return (
    <div className="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Tarih</th>{showParty ? <th>Cari</th> : null}<th>İşlem</th>{!compact ? <th className="num">Miktar</th> : null}
            <th>Belge</th><th className="num">Tutar</th><th>Durum</th>{onReverse || onInvoice ? <th><span className="sr-only">İşlemler</span></th> : null}
          </tr>
        </thead>
        <tbody>
          {rows.map((l) => (
            <tr key={l.id}>
              <td>{date(l.occurredOn)}</td>
              {showParty ? <td><Link className="row-link" to={`/cariler/${l.partyId}`}>{l.partyName}</Link></td> : null}
              <td>
                {l.reversesLineId ? 'İade: ' : l.direction === 'purchase' ? 'Alış: ' : ''}{l.itemName}
                <span className="sub">{l.itemKind === 'stock' ? 'Stok' : 'Hizmet'}{compact ? ` · ${quantity(l.quantity)}` : ''}{l.description ? ` · ${l.description}` : ''}</span>
              </td>
              {!compact ? <td className="num">{quantity(l.quantity)}{Number(l.pendingQuantity) > 0 && Number(l.documentedQuantity) !== 0 ? <span className="sub">{quantity(l.pendingQuantity)} bekliyor</span> : null}</td> : null}
              <td>{l.documents.length ? l.documents.join(', ') : <span className="muted">—</span>}</td>
              <td className="num">{money(l.grossAmount)}</td>
              <td><StatusBadge status={l.status} /></td>
              {onReverse || onInvoice ? (
                <td className="num">
                  <div className="actions" style={{ justifyContent: 'flex-end' }}>
                    {onInvoice && Number(l.pendingQuantity) > 0 ? <button type="button" className="btn small" onClick={() => onInvoice(l)}>Fatura Gir</button> : null}
                    {onReverse && !l.reversesLineId && l.status !== 'reversed' ? <button type="button" className="btn small ghost" onClick={() => onReverse(l)}>İade</button> : null}
                  </div>
                </td>
              ) : null}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

export function PaymentTable({ rows, title, onAllocate, onReverse, showParty }: {
  rows: PaymentRow[]; title: string; onAllocate?: (p: PaymentRow) => void; onReverse?: (p: PaymentRow) => void; showParty?: boolean
}) {
  return (
    <div className="panel">
      <div className="panel-head"><h2>{title}</h2></div>
      {rows.length === 0 ? <Empty title="Kayıt yok" /> : (
        <div className="table-wrap">
          <table>
            <thead><tr><th>Tarih</th>{showParty ? <th>Cari</th> : null}<th>Araç / hesap</th><th className="num">Tutar</th><th className="num">Dağıtılmamış</th><th><span className="sr-only">İşlemler</span></th></tr></thead>
            <tbody>
              {rows.map((p) => (
                <tr key={p.id}>
                  <td>{date(p.occurredOn)}</td>
                  {showParty ? <td><Link className="row-link" to={`/cariler/${p.partyId}`}>{p.partyName}</Link></td> : null}
                  <td>
                    {p.isReversal ? 'İptal: ' : ''}{p.direction === 'receipt' ? 'Tahsilat' : 'Ödeme'} · {p.method}
                    <span className="sub">{p.accountName}{p.reference ? ` · ${p.reference}` : ''}{p.isReversed ? ' · iptal edildi' : ''}</span>
                  </td>
                  <td className="num">{money(p.amount)}</td>
                  <td className="num">{Number(p.unallocatedAmount) > 0 ? <span className="badge pending">{money(p.unallocatedAmount)}</span> : '—'}</td>
                  <td className="num">
                    <div className="actions" style={{ justifyContent: 'flex-end' }}>
                      {onAllocate && Number(p.unallocatedAmount) > 0 ? <button type="button" className="btn small" onClick={() => onAllocate(p)}>Dağıt</button> : null}
                      {onReverse && !p.isReversal && !p.isReversed ? <button type="button" className="btn small ghost" onClick={() => onReverse(p)}>İptal</button> : null}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

function DocumentTable({ rows }: { rows: DocumentRow[] }) {
  return (
    <div className="panel">
      <div className="panel-head"><h2>Faturalar</h2><Link className="btn small ghost" to="/faturalar">Tüm faturalar</Link></div>
      {rows.length === 0 ? <Empty title="Fatura girilmemiş" /> : (
        <table>
          <thead><tr><th>Tarih</th><th>No</th><th className="num">Toplam</th><th>Durum</th></tr></thead>
          <tbody>{rows.map((d) => (
            <tr key={d.id}><td>{date(d.documentDate)}</td><td>{d.documentNo}<span className="sub">{docTypeName(d.type)}{d.direction === 'purchase' ? ' · alış' : ''}</span></td>
              <td className="num">{money(d.grossTotal)}</td><td><StatusBadge status={d.status} /></td></tr>
          ))}</tbody>
        </table>
      )}
    </div>
  )
}

function AthleteDialog({ open, partyId, onClose, onSaved }: { open: boolean; partyId: string; onClose: () => void; onSaved: () => void }) {
  const [form, setForm] = useState({ firstName: '', lastName: '', groupName: '', birthYear: '' })
  const [error, setError] = useState<string | null>(null)
  return (
    <Dialog open={open} onClose={onClose} title="Sporcu ekle"
      footer={<>
        <button type="button" className="btn" onClick={onClose}>Vazgeç</button>
        <button type="button" className="btn primary" onClick={async () => {
          setError(null)
          try {
            await post(`/api/parties/${partyId}/athletes`, { ...form, birthYear: form.birthYear ? Number(form.birthYear) : null })
            setForm({ firstName: '', lastName: '', groupName: '', birthYear: '' }); onSaved()
          } catch (e) { setError(errorText(e)) }
        }}>Kaydet</button>
      </>}>
      <ErrorBox message={error} />
      <div className="form-grid">
        <Field label="Ad"><input className="input" value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} autoFocus /></Field>
        <Field label="Soyad"><input className="input" value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} /></Field>
        <Field label="Grup / branş"><input className="input" value={form.groupName} onChange={(e) => setForm({ ...form, groupName: e.target.value })} /></Field>
        <Field label="Doğum yılı"><input className="input" inputMode="numeric" value={form.birthYear} onChange={(e) => setForm({ ...form, birthYear: e.target.value })} /></Field>
      </div>
    </Dialog>
  )
}
