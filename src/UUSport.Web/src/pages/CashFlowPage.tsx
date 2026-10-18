import { useState } from 'react'
import { api, post, put, type Money } from '../api'
import { useAuth } from '../auth'
import { CompanyTag, Dialog, Empty, ErrorBox, Field, Loading, Tabs, WriteScope, errorText, useAsync, useToast } from '../components'
import { date, money, normalizeNumber } from '../format'

// =====================================================================
// Nakit akisi tahmini (M4 on calismasi). Para sunucuda hesaplanir (ADR-002 2.5);
// istemci yalniz gosterir ve varsayimlari sorgu olarak gonderir.
// =====================================================================

type Bucket = {
  key: string; label: string; isOverdue: boolean
  checksIn: Money; receivablesIn: Money; plannedIn: Money
  loansOut: Money; checksOut: Money; cardOut: Money; payablesOut: Money
  salaryOut: Money; sgkOut: Money; rentOut: Money; vatOut: Money; taxOut: Money; otherOut: Money
  totalIn: Money; totalOut: Money; net: Money; closing: Money
}
type CompanyForecast = {
  company: string; opening: Money; openReceivables: Money; openPayables: Money; creditCardDebt: Money
  buckets: Bucket[]; firstShortfall: string | null
}
type Item = {
  company: string; dueDate: string; kind: string; description: string; partyName: string; amount: Money
  isInflow: boolean; isOverdue: boolean; isEstimate: boolean
}
type Forecast = { asOf: string; companies: CompanyForecast[]; total: CompanyForecast; items: Item[] }

type RowDef = { key: keyof Bucket; label: string; kind: 'in' | 'out' | 'sum' | 'net' | 'closing'; note?: string }

const ROWS: RowDef[] = [
  { key: 'checksIn', label: 'Alınan çeklerin vadesi', kind: 'in' },
  { key: 'plannedIn', label: 'Beklenen tahsilat', kind: 'in', note: 'planlı' },
  { key: 'receivablesIn', label: 'Açık alacak tahsilatı', kind: 'in', note: 'varsayım' },
  { key: 'totalIn', label: 'Toplam giriş', kind: 'sum' },
  { key: 'salaryOut', label: 'Maaş', kind: 'out', note: 'planlı' },
  { key: 'sgkOut', label: 'SGK primi', kind: 'out', note: 'planlı' },
  { key: 'rentOut', label: 'Kira', kind: 'out', note: 'planlı' },
  { key: 'vatOut', label: 'KDV', kind: 'out', note: 'faturalardan / planlı' },
  { key: 'taxOut', label: 'Muhtasar ve diğer vergiler', kind: 'out', note: 'planlı' },
  { key: 'loansOut', label: 'Kredi taksitleri', kind: 'out' },
  { key: 'cardOut', label: 'Kredi kartı ödemesi', kind: 'out' },
  { key: 'checksOut', label: 'Verilen çeklerin vadesi', kind: 'out' },
  { key: 'otherOut', label: 'Diğer giderler', kind: 'out', note: 'planlı' },
  { key: 'payablesOut', label: 'Açık borç ödemesi', kind: 'out', note: 'varsayım' },
  { key: 'totalOut', label: 'Toplam çıkış', kind: 'sum' },
  { key: 'net', label: 'Net akış', kind: 'net' },
  { key: 'closing', label: 'Dönem sonu nakit', kind: 'closing' },
]

const KIND_LABEL: Record<string, string> = {
  loan: 'Kredi taksiti', check_in: 'Alınan çek', check_out: 'Verilen çek', card: 'Kredi kartı', vat_auto: 'KDV (faturalardan)',
  salary: 'Maaş', sgk: 'SGK primi', rent: 'Kira', vat: 'KDV (planlı)', withholding_tax: 'Muhtasar', provisional_tax: 'Geçici vergi',
  other_tax: 'Vergi', other_expense: 'Gider', other_income: 'Beklenen tahsilat',
}

const PLANNED_KINDS: { id: string; label: string; hint: string }[] = [
  { id: 'salary', label: 'Maaş', hint: 'net maaş toplamı' },
  { id: 'sgk', label: 'SGK primi', hint: 'genelde ayın 26\'sı' },
  { id: 'withholding_tax', label: 'Muhtasar', hint: 'genelde ayın 26\'sı' },
  { id: 'rent', label: 'Kira', hint: '' },
  { id: 'vat', label: 'KDV (gelecek aylar)', hint: 'ilk iki ay faturalardan hesaplanır' },
  { id: 'provisional_tax', label: 'Geçici vergi', hint: '3 ayda bir' },
  { id: 'other_tax', label: 'Diğer vergi', hint: '' },
  { id: 'other_expense', label: 'Diğer gider', hint: 'elektrik, su, yemek…' },
  { id: 'other_income', label: 'Beklenen tahsilat', hint: 'aidat, üyelik, bilet' },
]

type Inputs = { months: string; receivableRate: string; payableRate: string }

export function CashFlowPage() {
  const { consolidated } = useAuth()
  const blank: Inputs = { months: '6', receivableRate: '0', payableRate: '0' }
  const [form, setForm] = useState<Inputs>(blank)
  const [applied, setApplied] = useState<Inputs>(blank)
  const [version, setVersion] = useState(0)
  const [view, setView] = useState('total')
  const { data, error, loading } = useAsync(() => {
    const q = new URLSearchParams({ months: applied.months })
    for (const k of ['receivableRate', 'payableRate'] as const) {
      const v = normalizeNumber(applied[k])
      if (v) q.set(k, v)
    }
    return api<Forecast>(`/api/reports/cash-forecast?${q.toString()}`)
  }, [applied, version])

  return (
    <>
      <div className="page-head">
        <div>
          <h1>Nakit Akışı</h1>
          <p className="sub">Önümüzdeki aylarda kasaya ve bankaya girecek, çıkacak paranın tahmini. Yönetim raporudur; resmî beyan yerine geçmez.</p>
        </div>
        <button className="btn" type="button" onClick={() => window.print()}>Yazdır</button>
      </div>

      <form className="panel" style={{ marginBottom: 16 }} onSubmit={(e) => { e.preventDefault(); setApplied(form) }}>
        <div className="panel-body forecast-inputs">
          <Field label="Dönem">
            <select className="input" value={form.months} onChange={(e) => { const next = { ...form, months: e.target.value }; setForm(next); setApplied(next) }}>
              <option value="3">3 ay</option><option value="6">6 ay</option><option value="12">12 ay</option><option value="24">24 ay</option>
            </select>
          </Field>
          <Field label="Açık alacağın ilk ay tahsili (%)" hint="vadesi belli olmayan">
            <input className="input" inputMode="decimal" value={form.receivableRate} onChange={(e) => setForm({ ...form, receivableRate: e.target.value })} />
          </Field>
          <Field label="Açık borcun ilk ay ödemesi (%)" hint="vadesi belli olmayan">
            <input className="input" inputMode="decimal" value={form.payableRate} onChange={(e) => setForm({ ...form, payableRate: e.target.value })} />
          </Field>
          <div className="forecast-apply"><button className="btn primary" type="submit">Hesapla</button></div>
        </div>
      </form>

      <ErrorBox message={error} />
      {loading && !data ? <Loading /> : !data ? null : (
        <>
          {consolidated && data.companies.length > 1 ? (
            <div className="panel" style={{ marginBottom: 16 }}>
              <div className="panel-body">
                <Tabs label="Görünüm" value={view} onChange={setView} tabs={[{ id: 'total', label: 'Toplu' }, { id: 'companies', label: 'Şirket bazında' }]}>
                  <div style={{ marginTop: 12 }}>
                    {view === 'total' ? (
                      <ForecastBlock forecast={data.total} title="Seçili şirketlerin toplamı" />
                    ) : (
                      <div className="stack">{data.companies.map((c) => <ForecastBlock key={c.company} forecast={c} company={c.company} />)}</div>
                    )}
                  </div>
                </Tabs>
              </div>
            </div>
          ) : (
            <ForecastBlock forecast={data.total} />
          )}
          <PlannedItemsPanel onChanged={() => setVersion((v) => v + 1)} />
          <ItemsTable items={data.items} showCompany={consolidated} />
        </>
      )}
    </>
  )
}

function ForecastBlock({ forecast, company, title }: { forecast: CompanyForecast; company?: string; title?: string }) {
  const buckets = forecast.buckets.filter((b) => !b.isOverdue || Number(b.totalIn) !== 0 || Number(b.totalOut) !== 0)
  const last = buckets[buckets.length - 1]
  const lowest = buckets.reduce<Bucket | null>((min, b) => (!min || Number(b.closing) < Number(min.closing) ? b : min), null)
  // Tum donemlerde sifir olan kalem satirlari gosterilmez (toplam ve kapanis satirlari her zaman gorunur).
  const rows = ROWS.filter((r) => (r.kind !== 'in' && r.kind !== 'out') || buckets.some((b) => Number(b[r.key]) !== 0))

  return (
    <section className="stack" aria-label={company ? `${company} nakit akışı` : 'Nakit akışı'}>
      {company || title ? <h2 className="row" style={{ gap: 8 }}>{company ? <CompanyTag code={company} /> : null}{title ?? null}</h2> : null}
      <div className="cards">
        <div className="card balance"><div className="label">Bugünkü nakit</div><div className="value">{money(forecast.opening)}</div><div className="note">Kasa + banka + POS</div></div>
        {last ? (
          <div className={`card${Number(last.closing) < 0 ? ' attention' : ''}`}>
            <div className="label">{last.label} sonu tahmini</div><div className="value">{money(last.closing)}</div>
            <div className="note">Dönem boyunca net {money(Number(last.closing) - Number(forecast.opening))}</div>
          </div>
        ) : null}
        {lowest ? (
          <div className={`card${Number(lowest.closing) < 0 ? ' attention' : ''}`}>
            <div className="label">En düşük nokta</div><div className="value">{money(lowest.closing)}</div><div className="note">{lowest.label} sonu</div>
          </div>
        ) : null}
        <div className="card">
          <div className="label">Vadesi belirsiz</div><div className="value">{money(forecast.openReceivables)}</div>
          <div className="note">açık alacak · {money(forecast.openPayables)} açık borç · {money(forecast.creditCardDebt)} kart borcu</div>
        </div>
      </div>
      {forecast.firstShortfall ? (
        <div className="alert warn" role="status"><strong>Nakit açığı riski:</strong> tahmine göre nakit {forecast.firstShortfall} döneminde eksiye düşüyor.</div>
      ) : null}
      <div className="panel">
        <div className="table-wrap">
          <table className="forecast-table">
            <thead>
              <tr><th>Kalem</th>{buckets.map((b) => <th key={b.key} className="num">{b.label}</th>)}</tr>
            </thead>
            <tbody>
              <tr className="forecast-opening"><td>Dönem başı nakit</td>{buckets.map((b, i) => (
                <td key={b.key} className="num">{money(i === 0 ? forecast.opening : buckets[i - 1].closing)}</td>
              ))}</tr>
              {rows.map((r) => (
                <tr key={r.key} className={`forecast-${r.kind}`}>
                  <td>{r.label}{r.note ? <span className="sub">{r.note}</span> : null}</td>
                  {buckets.map((b) => {
                    const v = Number(b[r.key])
                    if (r.kind === 'closing') return <td key={b.key} className="num">{v < 0 ? <span className="badge danger">{money(v)}</span> : money(v)}</td>
                    if (v === 0 && r.kind !== 'net' && r.kind !== 'sum') return <td key={b.key} className="num muted">—</td>
                    return <td key={b.key} className="num">{r.kind === 'out' || r.key === 'totalOut' ? `−${money(v)}` : money(v)}</td>
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  )
}

// ---------------- planli kalemler (sirket basina, kalici) ----------------

type PlannedRow = {
  company: string; id: string; kind: string; name: string; amount: Money; dayOfMonth: number; intervalMonths: number
  startMonth: string; endMonth: string | null; isActive: boolean
}

const INTERVAL_LABEL: Record<number, string> = { 1: 'her ay', 2: '2 ayda bir', 3: '3 ayda bir', 6: '6 ayda bir', 12: 'yılda bir' }

function PlannedItemsPanel({ onChanged }: { onChanged: () => void }) {
  const toast = useToast()
  const { consolidated, canWrite } = useAuth()
  const { data, error, reload } = useAsync(() => api<PlannedRow[]>('/api/planned-cash-items'), [])
  const [editing, setEditing] = useState<PlannedRow | 'new' | null>(null)
  const done = (msg: string) => { setEditing(null); toast(msg); reload(); onChanged() }
  return (
    <div className="panel" style={{ marginTop: 16 }}>
      <div className="panel-head">
        <div>
          <h2>Planlı kalemler</h2>
          <p className="muted small">Maaş, SGK, kira, vergi gibi sistemde belgesi olmayan düzenli ödeme ve tahsilatlar. Yalnız tahmini etkiler; kasaya veya cariye yazmaz.</p>
        </div>
        <WriteScope><button className="btn primary" type="button" onClick={() => setEditing('new')}>Kalem Ekle</button></WriteScope>
      </div>
      <ErrorBox message={error} />
      {!data ? <Loading /> : data.length === 0 ? <Empty title="Planlı kalem yok">Maaş, SGK, kira ve vergileri ekleyin; tahmin gerçeğe yaklaşır.</Empty> : (
        <div className="table-wrap">
          <table>
            <thead><tr>{consolidated ? <th>Şirket</th> : null}<th>Kalem</th><th className="num">Tutar</th><th>Ne zaman</th><th>Durum</th><th><span className="sr-only">İşlemler</span></th></tr></thead>
            <tbody>{data.map((p) => (
              <tr key={p.id}>
                {consolidated ? <td className="company-col"><CompanyTag code={p.company} /></td> : null}
                <td>{p.name}<span className="sub">{KIND_LABEL[p.kind] ?? p.kind}</span></td>
                <td className="num">{p.kind === 'other_income' ? '+' : '−'}{money(p.amount)}</td>
                <td>Ayın {p.dayOfMonth}'i, {INTERVAL_LABEL[p.intervalMonths] ?? `${p.intervalMonths} ayda bir`}
                  <span className="sub">{date(p.startMonth)} itibarıyla{p.endMonth ? ` · ${date(p.endMonth)} ayına kadar` : ''}</span></td>
                <td>{p.isActive ? <span className="badge done">Hesaba katılıyor</span> : <span className="badge neutral">Kapalı</span>}</td>
                <td className="num">{canWrite ? <button className="btn small ghost" type="button" onClick={() => setEditing(p)}>Düzenle</button> : null}</td>
              </tr>
            ))}</tbody>
          </table>
        </div>
      )}
      <PlannedDialog item={editing} onClose={() => setEditing(null)} onSaved={() => done('Planlı kalem kaydedildi.')} />
    </div>
  )
}

function PlannedDialog({ item, onClose, onSaved }: { item: PlannedRow | 'new' | null; onClose: () => void; onSaved: () => void }) {
  const thisMonth = new Date().toISOString().slice(0, 7)
  const blank = { kind: 'salary', name: '', amount: '', dayOfMonth: '1', intervalMonths: '1', startMonth: thisMonth, endMonth: '', isActive: true }
  const [form, setForm] = useState(blank)
  const [error, setError] = useState<string | null>(null)
  const [loadedFor, setLoadedFor] = useState<unknown>(null)
  if (item !== loadedFor) {
    setLoadedFor(item); setError(null)
    setForm(item && item !== 'new' ? {
      kind: item.kind, name: item.name, amount: String(item.amount).replace('.', ','), dayOfMonth: String(item.dayOfMonth),
      intervalMonths: String(item.intervalMonths), startMonth: item.startMonth.slice(0, 7), endMonth: item.endMonth?.slice(0, 7) ?? '', isActive: item.isActive,
    } : blank)
  }
  const kind = PLANNED_KINDS.find((k) => k.id === form.kind)
  return (
    <Dialog open={!!item} onClose={onClose} title={item === 'new' ? 'Planlı kalem ekle' : 'Planlı kalemi düzenle'}
      description="Kalem seçili şirkete eklenir. Tutar her gerçekleşmede ödenecek/tahsil edilecek toplam tutardır."
      footer={<>
        <button type="button" className="btn" onClick={onClose}>Vazgeç</button>
        <button type="button" className="btn primary" onClick={async () => {
          setError(null)
          const body = {
            kind: form.kind, name: form.name || kind?.label || '', amount: normalizeNumber(form.amount),
            dayOfMonth: Number(form.dayOfMonth), intervalMonths: Number(form.intervalMonths),
            startMonth: `${form.startMonth}-01`, endMonth: form.endMonth ? `${form.endMonth}-01` : null, isActive: form.isActive,
          }
          try {
            if (item === 'new') await post('/api/planned-cash-items', body)
            else await put(`/api/planned-cash-items/${(item as PlannedRow).id}`, body)
            onSaved()
          } catch (e) { setError(errorText(e)) }
        }}>Kaydet</button>
      </>}>
      <ErrorBox message={error} />
      <div className="form-grid">
        <Field label="Tür" hint={kind?.hint || undefined}>
          <select className="input" value={form.kind} onChange={(e) => {
            const k = e.target.value
            setForm({ ...form, kind: k, intervalMonths: k === 'provisional_tax' ? '3' : form.intervalMonths, dayOfMonth: ['sgk', 'withholding_tax'].includes(k) ? '26' : k === 'vat' ? '28' : form.dayOfMonth })
          }}>
            {PLANNED_KINDS.map((k) => <option key={k.id} value={k.id}>{k.label}</option>)}
          </select>
        </Field>
        <Field label="Açıklama" hint="boşsa türü yazılır"><input className="input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></Field>
        <Field label="Tutar (TL)"><input className="input" inputMode="decimal" value={form.amount} onChange={(e) => setForm({ ...form, amount: e.target.value })} /></Field>
        <Field label="Ayın kaçında" hint="1-28"><input className="input" inputMode="numeric" value={form.dayOfMonth} onChange={(e) => setForm({ ...form, dayOfMonth: e.target.value })} /></Field>
        <Field label="Tekrar">
          <select className="input" value={form.intervalMonths} onChange={(e) => setForm({ ...form, intervalMonths: e.target.value })}>
            {[1, 2, 3, 6, 12].map((n) => <option key={n} value={n}>{INTERVAL_LABEL[n]}</option>)}
          </select>
        </Field>
        <Field label="İlk ay"><input className="input" type="month" value={form.startMonth} onChange={(e) => setForm({ ...form, startMonth: e.target.value })} /></Field>
        <Field label="Son ay" hint="boşsa süresiz"><input className="input" type="month" value={form.endMonth} onChange={(e) => setForm({ ...form, endMonth: e.target.value })} /></Field>
        {item !== 'new' ? (
          <Field label="Durum">
            <select className="input" value={form.isActive ? '1' : '0'} onChange={(e) => setForm({ ...form, isActive: e.target.value === '1' })}>
              <option value="1">Hesaba katılıyor</option><option value="0">Kapalı</option>
            </select>
          </Field>
        ) : null}
      </div>
    </Dialog>
  )
}

function ItemsTable({ items, showCompany }: { items: Item[]; showCompany: boolean }) {
  return (
    <div className="panel" style={{ marginTop: 16 }}>
      <div className="panel-head"><h2>Tahmine giren kalemler</h2><span className="muted small">Vadeye göre; “tahmin” işaretliler kesin değildir</span></div>
      {items.length === 0 ? <Empty title="Dönem içinde kalem yok" /> : (
        <div className="table-wrap">
          <table>
            <thead><tr><th>Tarih</th>{showCompany ? <th>Şirket</th> : null}<th>Kalem</th><th>Cari</th><th className="num">Giriş</th><th className="num">Çıkış</th></tr></thead>
            <tbody>{items.map((it, i) => (
              <tr key={i}>
                <td>{date(it.dueDate)}{it.isOverdue ? <span className="sub"><span className="badge danger">vadesi geçti</span></span> : null}</td>
                {showCompany ? <td className="company-col"><CompanyTag code={it.company} /></td> : null}
                <td>{KIND_LABEL[it.kind] ?? it.kind}{it.isEstimate ? <span className="badge neutral" style={{ marginLeft: 6 }}>tahmin</span> : null}<span className="sub">{it.description}</span></td>
                <td>{it.partyName || <span className="muted">—</span>}</td>
                <td className="num">{it.isInflow ? money(it.amount) : '—'}</td>
                <td className="num">{!it.isInflow ? money(it.amount) : '—'}</td>
              </tr>
            ))}</tbody>
          </table>
        </div>
      )}
    </div>
  )
}
