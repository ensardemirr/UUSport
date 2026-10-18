import { useState } from 'react'
import { api, type Money } from '../api'
import { useAuth } from '../auth'
import { CompanyTag, Empty, ErrorBox, Field, Loading, Tabs, useAsync } from '../components'
import { date, money, normalizeNumber } from '../format'

// =====================================================================
// Nakit akisi tahmini (M4 on calismasi). Para sunucuda hesaplanir (ADR-002 2.5);
// istemci yalniz gosterir ve varsayimlari sorgu olarak gonderir.
// =====================================================================

type Bucket = {
  key: string; label: string; isOverdue: boolean
  checksIn: Money; receivablesIn: Money; scenarioIn: Money
  loansOut: Money; checksOut: Money; payablesOut: Money; scenarioOut: Money
  totalIn: Money; totalOut: Money; net: Money; closing: Money
}
type CompanyForecast = {
  company: string; opening: Money; openReceivables: Money; openPayables: Money; creditCardDebt: Money
  buckets: Bucket[]; firstShortfall: string | null
}
type Item = { company: string; dueDate: string; kind: 'loan' | 'check_in' | 'check_out'; description: string; partyName: string; amount: Money; isInflow: boolean; isOverdue: boolean }
type Forecast = {
  asOf: string
  assumptions: { months: number; monthlyIncome: Money; monthlyExpense: Money; receivableRate: Money; payableRate: Money }
  companies: CompanyForecast[]; total: CompanyForecast; items: Item[]
}

type RowDef = { key: keyof Bucket; label: string; kind: 'in' | 'out' | 'sum' | 'net' | 'closing'; assumption?: boolean }

const ROWS: RowDef[] = [
  { key: 'checksIn', label: 'Alınan çeklerin vadesi', kind: 'in' },
  { key: 'receivablesIn', label: 'Açık alacak tahsilatı', kind: 'in', assumption: true },
  { key: 'scenarioIn', label: 'Aylık beklenen tahsilat', kind: 'in', assumption: true },
  { key: 'totalIn', label: 'Toplam giriş', kind: 'sum' },
  { key: 'loansOut', label: 'Kredi taksitleri', kind: 'out' },
  { key: 'checksOut', label: 'Verilen çeklerin vadesi', kind: 'out' },
  { key: 'payablesOut', label: 'Açık borç ödemesi', kind: 'out', assumption: true },
  { key: 'scenarioOut', label: 'Aylık sabit gider', kind: 'out', assumption: true },
  { key: 'totalOut', label: 'Toplam çıkış', kind: 'sum' },
  { key: 'net', label: 'Net akış', kind: 'net' },
  { key: 'closing', label: 'Dönem sonu nakit', kind: 'closing' },
]

const ITEM_KIND: Record<Item['kind'], string> = { loan: 'Kredi taksiti', check_in: 'Alınan çek', check_out: 'Verilen çek' }

type Inputs = { months: string; monthlyIncome: string; monthlyExpense: string; receivableRate: string; payableRate: string }

export function CashFlowPage() {
  const { consolidated } = useAuth()
  const blank: Inputs = { months: '6', monthlyIncome: '', monthlyExpense: '', receivableRate: '0', payableRate: '0' }
  const [form, setForm] = useState<Inputs>(blank)
  const [applied, setApplied] = useState<Inputs>(blank)
  const [view, setView] = useState('total')
  const { data, error, loading } = useAsync(() => {
    const q = new URLSearchParams({ months: applied.months })
    for (const k of ['monthlyIncome', 'monthlyExpense', 'receivableRate', 'payableRate'] as const) {
      const v = normalizeNumber(applied[k])
      if (v) q.set(k, v)
    }
    return api<Forecast>(`/api/reports/cash-forecast?${q.toString()}`)
  }, [applied])

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
        <div className="panel-head">
          <h2>Varsayımlar</h2>
          <span className="muted small">Kesin vadeli kalemler (çek, kredi taksiti) her zaman hesaba katılır. Aşağıdakiler sizin tahmininizdir.</span>
        </div>
        <div className="panel-body forecast-inputs">
          <Field label="Dönem">
            <select className="input" value={form.months} onChange={(e) => { const next = { ...form, months: e.target.value }; setForm(next); setApplied(next) }}>
              <option value="3">3 ay</option><option value="6">6 ay</option><option value="12">12 ay</option><option value="24">24 ay</option>
            </select>
          </Field>
          <Field label="Aylık beklenen tahsilat (TL)" hint="aidat, satış">
            <input className="input" inputMode="decimal" placeholder="0" value={form.monthlyIncome} onChange={(e) => setForm({ ...form, monthlyIncome: e.target.value })} />
          </Field>
          <Field label="Aylık sabit gider (TL)" hint="maaş, kira, SGK, vergi">
            <input className="input" inputMode="decimal" placeholder="0" value={form.monthlyExpense} onChange={(e) => setForm({ ...form, monthlyExpense: e.target.value })} />
          </Field>
          <Field label="Açık alacağın ilk ay tahsili (%)">
            <input className="input" inputMode="decimal" value={form.receivableRate} onChange={(e) => setForm({ ...form, receivableRate: e.target.value })} />
          </Field>
          <Field label="Açık borcun ilk ay ödemesi (%)">
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
                      <ForecastBlock forecast={data.total} title="Seçili şirketlerin toplamı" showScenario />
                    ) : (
                      <div className="stack">
                        <p className="muted small">Aylık tahsilat ve gider varsayımları şirketlere dağıtılmaz; yalnız toplu görünümde yer alır.</p>
                        {data.companies.map((c) => <ForecastBlock key={c.company} forecast={c} company={c.company} />)}
                      </div>
                    )}
                  </div>
                </Tabs>
              </div>
            </div>
          ) : (
            <ForecastBlock forecast={data.total} showScenario />
          )}
          <ItemsTable items={data.items} showCompany={consolidated} />
        </>
      )}
    </>
  )
}

function ForecastBlock({ forecast, company, title, showScenario }: { forecast: CompanyForecast; company?: string; title?: string; showScenario?: boolean }) {
  const buckets = forecast.buckets.filter((b) => !b.isOverdue || Number(b.totalIn) !== 0 || Number(b.totalOut) !== 0)
  const last = buckets[buckets.length - 1]
  const lowest = buckets.reduce<Bucket | null>((min, b) => (!min || Number(b.closing) < Number(min.closing) ? b : min), null)
  // Tum donemlerde sifir olan satirlar gosterilmez (toplam ve kapanis satirlari her zaman gorunur).
  const rows = ROWS.filter((r) => (r.kind !== 'in' && r.kind !== 'out') || buckets.some((b) => Number(b[r.key]) !== 0))
    .filter((r) => showScenario || (r.key !== 'scenarioIn' && r.key !== 'scenarioOut'))

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
          <div className="note">açık alacak · {money(forecast.openPayables)} açık borç</div>
        </div>
      </div>
      {forecast.firstShortfall ? (
        <div className="alert warn" role="status"><strong>Nakit açığı riski:</strong> tahmine göre nakit {forecast.firstShortfall} döneminde eksiye düşüyor.</div>
      ) : null}
      {Number(forecast.creditCardDebt) > 0 ? (
        <p className="muted small">Kredi kartı borcu {money(forecast.creditCardDebt)}: ekstre son ödeme tarihi sistemde olmadığı için tahmine katılmadı.</p>
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
                  <td>{r.label}{r.assumption ? <span className="sub">varsayım</span> : null}</td>
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

function ItemsTable({ items, showCompany }: { items: Item[]; showCompany: boolean }) {
  return (
    <div className="panel" style={{ marginTop: 16 }}>
      <div className="panel-head"><h2>Vadeli kalemler</h2><span className="muted small">Tahmine giren çek ve kredi taksitleri, vadeye göre</span></div>
      {items.length === 0 ? <Empty title="Dönem içinde vadesi gelen çek veya kredi taksiti yok" /> : (
        <div className="table-wrap">
          <table>
            <thead><tr><th>Vade</th>{showCompany ? <th>Şirket</th> : null}<th>Kalem</th><th>Cari</th><th className="num">Giriş</th><th className="num">Çıkış</th></tr></thead>
            <tbody>{items.map((it, i) => (
              <tr key={i}>
                <td>{date(it.dueDate)}{it.isOverdue ? <span className="sub"><span className="badge danger">vadesi geçti</span></span> : null}</td>
                {showCompany ? <td className="company-col"><CompanyTag code={it.company} /></td> : null}
                <td>{ITEM_KIND[it.kind]}<span className="sub">{it.description}</span></td>
                <td>{it.partyName}</td>
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
