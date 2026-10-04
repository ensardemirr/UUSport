import { useState } from 'react'
import { Link } from 'react-router'
import { api, post, type CashAccount, type CheckRow, type CheckSeriesRow, type LoanInstallmentRow, type LoanRow, type PartyListRow } from '../api'
import { Dialog, Empty, ErrorBox, Field, Loading, ReasonDialog, Tabs, errorText, useAsync, useToast } from '../components'
import { date, money, normalizeNumber, today } from '../format'

// =====================================================================
// Krediler (surum 2 hazirlik notu madde 3)
// =====================================================================

export function LoansPage() {
  const toast = useToast()
  const { data, error, reload } = useAsync(() => api<LoanRow[]>('/api/loans'), [])
  const [creating, setCreating] = useState(false)
  const [detail, setDetail] = useState<LoanRow | null>(null)

  return (
    <>
      <div className="page-head">
        <div><h1>Krediler</h1><p className="sub">Taksit tablosu ve ödeme takibi. Taksit ödemesi ilgili banka hesabından yapılır.</p></div>
        <div className="actions">
          <Link className="btn" to="/kasa-banka">Kasa ve Banka</Link>
          <button className="btn primary" type="button" onClick={() => setCreating(true)}>Yeni Kredi</button>
        </div>
      </div>
      <ErrorBox message={error} />
      <div className="panel">
        {!data ? <Loading /> : data.length === 0 ? (
          <Empty title="Kredi tanımlı değil">Önce Kasa ve Banka ekranından bir banka hesabının altına “Kredi” türünde hesap açın, sonra buradan krediyi tanımlayın.</Empty>
        ) : (
          <table>
            <thead><tr><th>Kredi</th><th>Veren</th><th className="num">Ana Para</th><th className="num">Kalan</th><th>Taksit</th><th>Sıradaki Vade</th><th><span className="sr-only">İşlemler</span></th></tr></thead>
            <tbody>{data.map((l) => (
              <tr key={l.id}>
                <td>{l.name}<span className="sub">{l.cashAccountName}</span></td>
                <td>{l.partyName}</td>
                <td className="num">{money(l.principalAmount)}</td>
                <td className="num">{money(l.remainingPrincipal)}</td>
                <td>{l.paidCount}/{l.installmentCount}</td>
                <td>{l.nextDueDate ? date(l.nextDueDate) : <span className="badge done">Tamamlandı</span>}</td>
                <td className="num"><button className="btn small ghost" type="button" onClick={() => setDetail(l)}>Taksitler</button></td>
              </tr>
            ))}</tbody>
          </table>
        )}
      </div>
      <CreateLoanDialog open={creating} onClose={() => setCreating(false)} onSaved={() => { setCreating(false); toast('Kredi oluşturuldu.'); reload() }} />
      <LoanInstallmentsDialog loan={detail} onClose={() => setDetail(null)} onPaid={() => { toast('Taksit ödendi.'); reload() }} />
    </>
  )
}

function CreateLoanDialog({ open, onClose, onSaved }: { open: boolean; onClose: () => void; onSaved: () => void }) {
  const { data: parties } = useAsync(() => open ? api<PartyListRow[]>('/api/parties?kind=supplier&includeInactive=false') : Promise.resolve([]), [open])
  const { data: accounts } = useAsync(() => open ? api<CashAccount[]>('/api/cash-accounts') : Promise.resolve([]), [open])
  const loanAccounts = (accounts ?? []).filter((a) => a.kind === 'loan' && a.isActive)

  const [partyId, setPartyId] = useState('')
  const [cashAccountId, setCashAccountId] = useState('')
  const [name, setName] = useState('')
  const [principal, setPrincipal] = useState('')
  const [startDate, setStartDate] = useState(today())
  const [count, setCount] = useState('12')
  const [firstDue, setFirstDue] = useState(today())
  const [interestEach, setInterestEach] = useState('0')
  const [feeEach, setFeeEach] = useState('0')
  const [rows, setRows] = useState<{ no: number; due: string; principal: string; interest: string; fee: string }[]>([])
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  function generateRows() {
    const n = Math.max(1, Math.round(Number(normalizeNumber(count)) || 1))
    const total = Number(normalizeNumber(principal)) || 0
    const each = Math.floor((total / n) * 100) / 100
    const list = Array.from({ length: n }, (_, i) => {
      const due = new Date(firstDue)
      due.setMonth(due.getMonth() + i)
      const isLast = i === n - 1
      const principalPart = isLast ? Math.round((total - each * (n - 1)) * 100) / 100 : each
      return { no: i + 1, due: due.toISOString().slice(0, 10), principal: String(principalPart).replace('.', ','), interest: interestEach, fee: feeEach }
    })
    setRows(list)
  }

  const sumPrincipal = rows.reduce((s, r) => s + (Number(normalizeNumber(r.principal)) || 0), 0)

  return (
    <Dialog open={open} wide onClose={onClose} title="Yeni Kredi"
      description="Taksit tablosunu oluşturun; ana para toplamı kredi tutarına eşit olmalı."
      footer={<>
        <button type="button" className="btn" onClick={onClose}>Vazgeç</button>
        <button type="button" className="btn primary" disabled={busy} onClick={async () => {
          setError(null); setBusy(true)
          try {
            await post('/api/loans', {
              partyId, cashAccountId, name, principalAmount: normalizeNumber(principal), startDate, notes: null,
              installments: rows.map((r) => ({
                installmentNo: r.no, dueDate: r.due, principalAmount: normalizeNumber(r.principal),
                interestAmount: normalizeNumber(r.interest) || '0', feeAmount: normalizeNumber(r.fee) || '0',
              })),
            })
            onSaved()
          } catch (e) { setError(errorText(e)) } finally { setBusy(false) }
        }}>Krediyi Kaydet</button>
      </>}>
      <ErrorBox message={error} />
      <div className="form-grid">
        <Field label="Kredi veren (tedarikçi)">
          <select className="input" value={partyId} onChange={(e) => setPartyId(e.target.value)}>
            <option value="">— seçin —</option>
            {(parties ?? []).map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
          </select>
        </Field>
        <Field label="Kredi hesabı" hint={loanAccounts.length === 0 ? 'önce Kasa ve Banka\'dan açın' : undefined}>
          <select className="input" value={cashAccountId} onChange={(e) => setCashAccountId(e.target.value)}>
            <option value="">— seçin —</option>
            {loanAccounts.map((a) => <option key={a.id} value={a.id}>{a.name}</option>)}
          </select>
        </Field>
        <Field label="Kredi adı" full><input className="input" value={name} onChange={(e) => setName(e.target.value)} placeholder="ör. İşletme Kredisi 2026" /></Field>
        <Field label="Ana para (TL)"><input className="input" inputMode="decimal" value={principal} onChange={(e) => setPrincipal(e.target.value)} /></Field>
        <Field label="Başlangıç tarihi"><input className="input" type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} /></Field>
      </div>

      <div className="panel">
        <div className="panel-head"><h3>Taksit tablosu oluştur</h3></div>
        <div className="panel-body">
          <div className="form-grid">
            <Field label="Taksit adedi"><input className="input" inputMode="numeric" value={count} onChange={(e) => setCount(e.target.value)} /></Field>
            <Field label="İlk vade"><input className="input" type="date" value={firstDue} onChange={(e) => setFirstDue(e.target.value)} /></Field>
            <Field label="Taksit başına faiz (TL)"><input className="input" inputMode="decimal" value={interestEach} onChange={(e) => setInterestEach(e.target.value)} /></Field>
            <Field label="Taksit başına masraf (TL)"><input className="input" inputMode="decimal" value={feeEach} onChange={(e) => setFeeEach(e.target.value)} /></Field>
          </div>
          <button type="button" className="btn" style={{ marginTop: 10 }} onClick={generateRows} disabled={!principal}>
            Eşit Taksitlere Böl
          </button>
        </div>
      </div>

      {rows.length > 0 ? (
        <div className="table-wrap">
          <table>
            <thead><tr><th>#</th><th>Vade</th><th className="num">Ana Para</th><th className="num">Faiz</th><th className="num">Masraf</th></tr></thead>
            <tbody>{rows.map((r, i) => (
              <tr key={r.no}>
                <td>{r.no}</td>
                <td><input className="input" type="date" value={r.due} onChange={(e) => setRows(rows.map((x, j) => j === i ? { ...x, due: e.target.value } : x))} /></td>
                <td className="num"><input className="input" inputMode="decimal" value={r.principal} onChange={(e) => setRows(rows.map((x, j) => j === i ? { ...x, principal: e.target.value } : x))} /></td>
                <td className="num"><input className="input" inputMode="decimal" value={r.interest} onChange={(e) => setRows(rows.map((x, j) => j === i ? { ...x, interest: e.target.value } : x))} /></td>
                <td className="num"><input className="input" inputMode="decimal" value={r.fee} onChange={(e) => setRows(rows.map((x, j) => j === i ? { ...x, fee: e.target.value } : x))} /></td>
              </tr>
            ))}</tbody>
            <tfoot><tr><td colSpan={2}>Ana para toplamı</td><td className="num" colSpan={3}>
              {money(sumPrincipal)} {principal && Math.abs(sumPrincipal - (Number(normalizeNumber(principal)) || 0)) > 0.01 ? <span className="badge danger">kredi tutarıyla uyuşmuyor</span> : null}
            </td></tr></tfoot>
          </table>
        </div>
      ) : null}
    </Dialog>
  )
}

function LoanInstallmentsDialog({ loan, onClose, onPaid }: { loan: LoanRow | null; onClose: () => void; onPaid: () => void }) {
  const { data, reload } = useAsync(() => loan ? api<LoanInstallmentRow[]>(`/api/loans/${loan.id}/installments`) : Promise.resolve([]), [loan?.id])
  const [paying, setPaying] = useState<LoanInstallmentRow | null>(null)

  return (
    <>
      <Dialog open={!!loan} wide onClose={onClose} title={loan ? `Taksitler — ${loan.name}` : ''}>
        {!data ? <Loading /> : (
          <table>
            <thead><tr><th>#</th><th>Vade</th><th className="num">Ana Para</th><th className="num">Faiz</th><th className="num">Masraf</th><th className="num">Toplam</th><th>Durum</th><th><span className="sr-only">İşlemler</span></th></tr></thead>
            <tbody>{data.map((i) => (
              <tr key={i.id}>
                <td>{i.installmentNo}</td><td>{date(i.dueDate)}</td>
                <td className="num">{money(i.principalAmount)}</td><td className="num">{money(i.interestAmount)}</td><td className="num">{money(i.feeAmount)}</td>
                <td className="num">{money(i.totalAmount)}</td>
                <td>{i.isPaid ? <span className="badge done">Ödendi</span> : <span className="badge pending">Bekliyor</span>}</td>
                <td className="num">{!i.isPaid ? <button className="btn small" type="button" onClick={() => setPaying(i)}>Öde</button> : null}</td>
              </tr>
            ))}</tbody>
          </table>
        )}
      </Dialog>
      <PayInstallmentDialog loan={loan} installment={paying} onClose={() => setPaying(null)}
        onSaved={() => { setPaying(null); reload(); onPaid() }} />
    </>
  )
}

function PayInstallmentDialog({ loan, installment, onClose, onSaved }: {
  loan: LoanRow | null; installment: LoanInstallmentRow | null; onClose: () => void; onSaved: () => void
}) {
  const { data: accounts } = useAsync(() => installment ? api<CashAccount[]>('/api/cash-accounts') : Promise.resolve([]), [installment?.id])
  const payable = (accounts ?? []).filter((a) => (a.kind === 'bank' || a.kind === 'cash') && a.isActive)
  const [accountId, setAccountId] = useState('')
  const [method, setMethod] = useState<'transfer' | 'cash'>('transfer')
  const [occurredOn, setOccurredOn] = useState(today())
  const [reference, setReference] = useState('')
  const [error, setError] = useState<string | null>(null)

  return (
    <Dialog open={!!installment} onClose={onClose} title={installment ? `Taksit ${installment.installmentNo} ödemesi` : ''}
      description={installment ? `Tutar: ${money(installment.totalAmount)}` : undefined}
      footer={<>
        <button type="button" className="btn" onClick={onClose}>Vazgeç</button>
        <button type="button" className="btn primary" onClick={async () => {
          setError(null)
          if (!accountId) { setError('Ödemenin çıkacağı hesap seçilmeli.'); return }
          try {
            await post(`/api/loans/${loan!.id}/installments/${installment!.installmentNo}/pay`, { cashAccountId: accountId, method, occurredOn, reference })
            onSaved()
          } catch (e) { setError(errorText(e)) }
        }}>Ödemeyi Kaydet</button>
      </>}>
      <ErrorBox message={error} />
      <Field label="Ödemenin çıkacağı hesap">
        <select className="input" value={accountId} onChange={(e) => setAccountId(e.target.value)}>
          <option value="">— seçin —</option>
          {payable.map((a) => <option key={a.id} value={a.id}>{a.name}</option>)}
        </select>
      </Field>
      <Field label="Yöntem">
        <select className="input" value={method} onChange={(e) => setMethod(e.target.value as typeof method)}>
          <option value="transfer">Havale/EFT</option><option value="cash">Nakit</option>
        </select>
      </Field>
      <Field label="Tarih"><input className="input" type="date" value={occurredOn} onChange={(e) => setOccurredOn(e.target.value)} /></Field>
      <Field label="Referans"><input className="input" value={reference} onChange={(e) => setReference(e.target.value)} /></Field>
    </Dialog>
  )
}

// =====================================================================
// Cek portfoyu (surum 2 hazirlik notu madde 3)
// =====================================================================

const checkStatusLabel: Record<string, string> = {
  in_portfolio: 'Portföyde', submitted_for_collection: 'Tahsile Verildi', collected: 'Tahsil Edildi',
  endorsed: 'Ciro Edildi', bounced: 'Karşılıksız',
}

export function ChecksPage() {
  const toast = useToast()
  const [status, setStatus] = useState('')
  const { data, error, reload } = useAsync(() => api<CheckRow[]>(`/api/checks${status ? `?status=${status}` : ''}`), [status])
  const [creating, setCreating] = useState(false)
  const [collecting, setCollecting] = useState<CheckRow | null>(null)
  const [bouncing, setBouncing] = useState<CheckRow | null>(null)

  return (
    <>
      <div className="page-head">
        <div><h1>Çek Portföyü</h1><p className="sub">Alınan ve verilen çekler. Tahsil edilen çek, cariye otomatik tahsilat olarak işlenir.</p></div>
        <button className="btn primary" type="button" onClick={() => setCreating(true)}>Çek Gir</button>
      </div>
      <ErrorBox message={error} />
      <div className="panel">
        <div className="panel-body">
          <Tabs label="Durum" value={status} onChange={setStatus} tabs={[
            { id: '', label: 'Tümü' }, { id: 'InPortfolio', label: 'Portföyde' },
            { id: 'SubmittedForCollection', label: 'Tahsile Verildi' }, { id: 'Collected', label: 'Tahsil Edildi' },
            { id: 'Bounced', label: 'Karşılıksız' },
          ]}>
            <div style={{ marginTop: 12 }}>
              {!data ? <Loading /> : data.length === 0 ? <Empty title="Çek yok" /> : (
                <div className="table-wrap">
                  <table>
                    <thead><tr><th>Cari</th><th>Yön</th><th>Banka / No</th><th>Vade</th><th className="num">Tutar</th><th>Durum</th><th><span className="sr-only">İşlemler</span></th></tr></thead>
                    <tbody>{data.map((c) => (
                      <tr key={c.id}>
                        <td>{c.partyName}</td>
                        <td>{c.direction === 'received' ? 'Alınan' : 'Verilen'}</td>
                        <td>{c.bankName}<span className="sub">{c.checkNo}</span></td>
                        <td>{date(c.dueDate)}</td>
                        <td className="num">{money(c.amount)}</td>
                        <td><span className={`badge ${c.status === 'collected' ? 'done' : c.status === 'bounced' ? 'danger' : c.status === 'in_portfolio' ? 'neutral' : 'partial'}`}>{checkStatusLabel[c.status]}</span>
                          {c.statusNote ? <span className="sub">{c.statusNote}</span> : null}</td>
                        <td className="num">
                          <div className="actions" style={{ justifyContent: 'flex-end' }}>
                            {c.status === 'in_portfolio' ? <>
                              <button className="btn small ghost" type="button" onClick={async () => {
                                try { await post(`/api/checks/${c.id}/submit-for-collection`); toast('Tahsile verildi.'); reload() } catch (e) { toast(errorText(e)) }
                              }}>Tahsile Ver</button>
                              <button className="btn small ghost" type="button" onClick={async () => {
                                try { await post(`/api/checks/${c.id}/endorse`, {}); toast('Ciro edildi.'); reload() } catch (e) { toast(errorText(e)) }
                              }}>Ciro Et</button>
                            </> : null}
                            {c.status === 'in_portfolio' || c.status === 'submitted_for_collection' ? (
                              <button className="btn small" type="button" onClick={() => setCollecting(c)}>Tahsil Et</button>
                            ) : null}
                            {c.status !== 'bounced' ? (
                              <button className="btn small ghost" type="button" onClick={() => setBouncing(c)}>Karşılıksız</button>
                            ) : null}
                          </div>
                        </td>
                      </tr>
                    ))}</tbody>
                  </table>
                </div>
              )}
            </div>
          </Tabs>
        </div>
      </div>
      <CreateCheckDialog open={creating} onClose={() => setCreating(false)} onSaved={(n) => { setCreating(false); toast(`${n} çek kaydedildi.`); reload() }} />
      <CollectCheckDialog check={collecting} onClose={() => setCollecting(null)} onSaved={() => { setCollecting(null); toast('Çek tahsil edildi.'); reload() }} />
      <ReasonDialog open={!!bouncing} title="Çek karşılıksız" confirmLabel="Karşılıksız Olarak İşaretle"
        description="Bu çek daha önce tahsil edilmişse ilgili tahsilat ters kayıtla geri alınır."
        onClose={() => setBouncing(null)} onConfirm={async (reason) => {
          await post(`/api/checks/${bouncing!.id}/bounce`, { reason }); toast('Çek karşılıksız olarak işaretlendi.'); reload()
        }} />
    </>
  )
}

function CreateCheckDialog({ open, onClose, onSaved }: { open: boolean; onClose: () => void; onSaved: (count: number) => void }) {
  const { data: parties } = useAsync(() => open ? api<PartyListRow[]>('/api/parties') : Promise.resolve([]), [open])
  const [partyId, setPartyId] = useState('')
  const [direction, setDirection] = useState<'received' | 'issued'>('received')
  const [bankName, setBankName] = useState('')
  const [isSeries, setIsSeries] = useState(false)
  const [checkNo, setCheckNo] = useState('')
  const [count, setCount] = useState('1')
  const [dueDate, setDueDate] = useState(today())
  const [interval, setInterval_] = useState('30')
  const [amount, setAmount] = useState('')
  const [rows, setRows] = useState<CheckSeriesRow[]>([])
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function preview() {
    setError(null)
    try {
      if (isSeries) {
        const r = await post<CheckSeriesRow[]>('/api/checks/preview-series', {
          startCheckNo: checkNo, count: Number(count) || 1, amount: normalizeNumber(amount), firstDueDate: dueDate, dueDayInterval: Number(interval) || 0,
        })
        setRows(r)
      } else {
        setRows([{ checkNo, dueDate, amount: normalizeNumber(amount) }])
      }
    } catch (e) { setError(errorText(e)) }
  }

  async function save() {
    setError(null); setBusy(true)
    try {
      const r = await post<{ count: number }>('/api/checks', {
        partyId, direction, bankName,
        checks: rows.map((c) => ({ checkNo: c.checkNo, dueDate: c.dueDate, amount: normalizeNumber(String(c.amount)) })),
      })
      onSaved(r.count)
      setRows([])
    } catch (e) { setError(errorText(e)) } finally { setBusy(false) }
  }

  return (
    <Dialog open={open} wide onClose={onClose} title="Çek Gir"
      description="Tek çek veya birden fazla ardışık numaralı çek (seri) girebilirsiniz."
      footer={rows.length > 0 ? <>
        <button type="button" className="btn" onClick={() => setRows([])}>Geri</button>
        <button type="button" className="btn primary" disabled={busy} onClick={save}>{rows.length} Çeki Kaydet</button>
      </> : <>
        <button type="button" className="btn" onClick={onClose}>Vazgeç</button>
        <button type="button" className="btn primary" onClick={preview} disabled={!partyId || !bankName || !checkNo || !amount}>Önizle</button>
      </>}>
      <ErrorBox message={error} />
      {rows.length === 0 ? (
        <div className="form-grid">
          <Field label="Cari" full>
            <select className="input" value={partyId} onChange={(e) => setPartyId(e.target.value)}>
              <option value="">— seçin —</option>
              {(parties ?? []).map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
            </select>
          </Field>
          <Field label="Yön">
            <select className="input" value={direction} onChange={(e) => setDirection(e.target.value as typeof direction)}>
              <option value="received">Alınan (müşteriden)</option><option value="issued">Verilen (tedarikçiye)</option>
            </select>
          </Field>
          <Field label="Banka"><input className="input" value={bankName} onChange={(e) => setBankName(e.target.value)} /></Field>
          <Field label="Çek numarası" hint={isSeries ? 'seri için ilk numara, rakamla bitmeli' : undefined}>
            <input className="input" value={checkNo} onChange={(e) => setCheckNo(e.target.value)} />
          </Field>
          <Field label="Tutar (TL)"><input className="input" inputMode="decimal" value={amount} onChange={(e) => setAmount(e.target.value)} /></Field>
          <Field label="Vade"><input className="input" type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} /></Field>
          <label className="check full" style={{ gridColumn: '1 / -1' }}>
            <input type="checkbox" checked={isSeries} onChange={(e) => setIsSeries(e.target.checked)} />
            <span><strong>Seri çek</strong><br />
              <span className="muted small">Açıksa ilk numaradan başlayarak ardışık numaralı birden fazla çek üretilir; her satırı kaydetmeden önce düzenleyebilirsiniz.</span>
            </span>
          </label>
          {isSeries ? (
            <>
              <Field label="Çek adedi"><input className="input" inputMode="numeric" value={count} onChange={(e) => setCount(e.target.value)} /></Field>
              <Field label="Vadeler arası gün" hint="0 = hepsi aynı vade"><input className="input" inputMode="numeric" value={interval} onChange={(e) => setInterval_(e.target.value)} /></Field>
            </>
          ) : null}
        </div>
      ) : (
        <div className="table-wrap">
          <table>
            <thead><tr><th>Çek No</th><th>Vade</th><th className="num">Tutar</th></tr></thead>
            <tbody>{rows.map((r, i) => (
              <tr key={i}>
                <td><input className="input" value={r.checkNo} onChange={(e) => setRows(rows.map((x, j) => j === i ? { ...x, checkNo: e.target.value } : x))} /></td>
                <td><input className="input" type="date" value={r.dueDate} onChange={(e) => setRows(rows.map((x, j) => j === i ? { ...x, dueDate: e.target.value } : x))} /></td>
                <td className="num"><input className="input" inputMode="decimal" value={String(r.amount)} onChange={(e) => setRows(rows.map((x, j) => j === i ? { ...x, amount: e.target.value } : x))} /></td>
              </tr>
            ))}</tbody>
          </table>
        </div>
      )}
    </Dialog>
  )
}

function CollectCheckDialog({ check, onClose, onSaved }: { check: CheckRow | null; onClose: () => void; onSaved: () => void }) {
  const { data: accounts } = useAsync(() => check ? api<CashAccount[]>('/api/cash-accounts') : Promise.resolve([]), [check?.id])
  const usable = (accounts ?? []).filter((a) => (a.kind === 'bank' || a.kind === 'cash') && a.isActive)
  const [accountId, setAccountId] = useState('')
  const [occurredOn, setOccurredOn] = useState(today())
  const [error, setError] = useState<string | null>(null)

  return (
    <Dialog open={!!check} onClose={onClose} title="Çeki Tahsil Et"
      description={check ? `${check.bankName} ${check.checkNo} · ${money(check.amount)} · vade ${date(check.dueDate)}` : undefined}
      footer={<>
        <button type="button" className="btn" onClick={onClose}>Vazgeç</button>
        <button type="button" className="btn primary" onClick={async () => {
          setError(null)
          if (!accountId) { setError('Paranın gireceği hesap seçilmeli.'); return }
          if (!occurredOn) { setError('Tahsilat tarihi girilmeli.'); return }
          try { await post(`/api/checks/${check!.id}/collect`, { cashAccountId: accountId, occurredOn: occurredOn || null }); onSaved() }
          catch (e) { setError(errorText(e)) }
        }}>Tahsil Et</button>
      </>}>
      <ErrorBox message={error} />
      <Field label="Paranın gireceği hesap">
        <select className="input" value={accountId} onChange={(e) => setAccountId(e.target.value)}>
          <option value="">— seçin —</option>
          {usable.map((a) => <option key={a.id} value={a.id}>{a.name}</option>)}
        </select>
      </Field>
      <Field label="Tahsilat tarihi" hint="paranın hesaba geçtiği gün">
        <input className="input" type="date" value={occurredOn} onChange={(e) => setOccurredOn(e.target.value)} required />
      </Field>
    </Dialog>
  )
}
