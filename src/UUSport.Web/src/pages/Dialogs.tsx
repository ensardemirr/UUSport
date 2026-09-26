import { useEffect, useMemo, useState } from 'react'
import { api, post, type CashAccount, type Item, type LineRow, type PaymentRow } from '../api'
import { useAuth } from '../auth'
import { Dialog, ErrorBox, Field, errorText } from '../components'
import { ratio, sumMicro, toMicro, toMoney } from '../dec'
import { date, money, normalizeNumber, percent, quantity, toInput, today } from '../format'

type Direction = 'sale' | 'purchase'

// =====================================================================
// Stok veya hizmet cikisi / alis girisi
// =====================================================================

type Preview = { net: string; vat: string; gross: string; vatRate: string; priceIncludesVat: boolean; unitPrice: string; maxDiscountPercent: string; discountExceeded: boolean }

export function TradeLineDialog({ open, direction, partyId, onClose, onSaved }: {
  open: boolean; direction: Direction; partyId: string; onClose: () => void
  onSaved: (lineId: string, official: boolean) => void
}) {
  const sale = direction === 'sale'
  const [items, setItems] = useState<Item[]>([])
  const [kind, setKind] = useState<'stock' | 'service'>('stock')
  const [search, setSearch] = useState('')
  const [itemId, setItemId] = useState('')
  const [qty, setQty] = useState('1')
  const [price, setPrice] = useState('')
  const [discount, setDiscount] = useState('0')
  const [on, setOn] = useState(today())
  const [description, setDescription] = useState('')
  const [official, setOfficial] = useState(false)
  const [preview, setPreview] = useState<Preview | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (!open) return
    setItemId(''); setQty('1'); setPrice(''); setDiscount('0'); setOn(today()); setDescription(''); setOfficial(false); setError(null); setPreview(null); setSearch('')
    api<Item[]>('/api/items').then(setItems).catch((e) => setError(errorText(e)))
  }, [open])

  const visible = useMemo(() => {
    const key = search.toLocaleLowerCase('tr-TR')
    return items.filter((i) => i.kind === kind && (!key || `${i.name} ${i.code}`.toLocaleLowerCase('tr-TR').includes(key)))
  }, [items, kind, search])
  const item = items.find((i) => i.id === itemId)

  useEffect(() => {
    if (!visible.some((i) => i.id === itemId)) setItemId(visible[0]?.id ?? '')
  }, [visible, itemId])

  useEffect(() => {
    if (!open || !itemId) { setPreview(null); return }
    const q = normalizeNumber(qty)
    const d = normalizeNumber(discount) || '0'
    const p = normalizeNumber(price)
    if (!q) { setPreview(null); return }
    const t = window.setTimeout(() => {
      post<Preview>('/api/trade-lines/preview', { itemId, quantity: q, unitPrice: p || null, discountPercent: d })
        .then(setPreview).catch(() => setPreview(null))
    }, 250)
    return () => window.clearTimeout(t)
  }, [open, itemId, qty, price, discount])

  const exceeded = sale && preview?.discountExceeded

  async function save() {
    setError(null)
    if (!itemId) { setError(sale ? 'Stok veya hizmet seçilmeli.' : 'Alınan kalem seçilmeli.'); return }
    if (exceeded) { setError(`İskonto yetki sınırını aşıyor. En fazla ${percent(preview?.maxDiscountPercent)} uygulayabilirsiniz.`); return }
    setBusy(true)
    try {
      const r = await post<{ id: string }>('/api/trade-lines', {
        partyId, itemId, direction, occurredOn: on, quantity: normalizeNumber(qty),
        unitPrice: normalizeNumber(price) || null, discountPercent: normalizeNumber(discount) || '0', description,
      })
      onSaved(r.id, official)
    } catch (e) { setError(errorText(e)) } finally { setBusy(false) }
  }

  return (
    <Dialog open={open} wide onClose={onClose}
      title={sale ? 'Stok veya Hizmet Çıkışı' : 'Alış Girişi'}
      description={sale ? 'Cariye borç yazar; stoklu kalemde stok düşer. Fatura ayrıca girilir.' : 'Tedarikçiye alacak yazar; stoklu kalemde stok girer.'}
      footer={<>
        <button type="button" className="btn" onClick={onClose}>Vazgeç</button>
        <button type="button" className="btn primary" onClick={save} disabled={busy}>
          {sale && official ? 'Kaydet ve Fatura Özetine Geç' : sale ? 'Çıkışı Kaydet' : 'Alışı Kaydet'}
        </button>
      </>}>
      <ErrorBox message={error} />
      <div className="row">
        <div className="tabs" role="radiogroup" aria-label="Kalem türü">
          {(['stock', 'service'] as const).map((k) => (
            <button key={k} type="button" role="radio" aria-checked={kind === k} className="tab" aria-selected={kind === k}
              onClick={() => { setKind(k); setItemId('') }}>{k === 'stock' ? 'Stok' : 'Hizmet'}</button>
          ))}
        </div>
        <input className="input search" placeholder={kind === 'stock' ? 'Stoklarda ara' : 'Hizmetlerde ara'}
          aria-label="Kalem ara" value={search} onChange={(e) => setSearch(e.target.value)} />
      </div>
      <Field label={kind === 'stock' ? 'Stok kalemi' : 'Hizmet'}>
        <select className="input" value={itemId} onChange={(e) => { setItemId(e.target.value); setPrice('') }} size={Math.min(6, Math.max(3, visible.length))}>
          {visible.map((i) => (
            <option key={i.id} value={i.id}>
              {i.name} — {money(i.unitPrice)}{i.priceIncludesVat ? ' (KDV dahil)' : ' + KDV'}
              {i.kind === 'stock' ? ` · mevcut ${quantity(i.stockOnHand)} ${i.unit}` : ''}
            </option>
          ))}
        </select>
      </Field>
      {visible.length === 0 ? <p className="muted small">Bu türde kalem yok. Önce “Ürün ve Hizmetler” ekranından tanımlayın.</p> : null}
      <div className="form-grid">
        <Field label="Miktar"><input className="input" inputMode="decimal" value={qty} onChange={(e) => setQty(e.target.value)} /></Field>
        <Field label="Birim fiyat" hint={item ? `katalog: ${money(item.unitPrice)}` : undefined}>
          <input className="input" inputMode="decimal" value={price} placeholder={item ? String(item.unitPrice).replace('.', ',') : ''} onChange={(e) => setPrice(e.target.value)} />
        </Field>
        {sale ? (
          <Field label="İskonto (%)" hint={preview ? `sınırınız ${percent(preview.maxDiscountPercent)}` : undefined}>
            <input className="input" inputMode="decimal" value={discount} aria-invalid={exceeded || undefined}
              aria-describedby="discount-note" onChange={(e) => setDiscount(e.target.value)} />
          </Field>
        ) : null}
        <Field label={sale ? 'Teslim tarihi' : 'Alış tarihi'}><input className="input" type="date" value={on} onChange={(e) => setOn(e.target.value)} /></Field>
        <Field label="Açıklama" full><input className="input" value={description} onChange={(e) => setDescription(e.target.value)} /></Field>
      </div>
      <div id="discount-note" role={exceeded ? 'alert' : undefined} className={exceeded ? 'alert error' : 'sr-only'}>
        {exceeded ? `İskonto yetki sınırını aşıyor. Bu kullanıcı en fazla ${percent(preview?.maxDiscountPercent)} iskonto uygulayabilir.` : ''}
      </div>
      {preview ? (
        <div className="calc" aria-live="polite">
          <div><span>Matrah</span><strong>{money(preview.net)}</strong></div>
          <div><span>KDV {percent(preview.vatRate)}</span><strong>{money(preview.vat)}</strong></div>
          <div className="total"><span>{sale ? 'Cariye yazılacak' : 'Tedarikçiye yazılacak'}</span><span>{money(preview.gross)}</span></div>
        </div>
      ) : null}
      {sale ? (
        <label className="check">
          <input type="checkbox" checked={official} onChange={(e) => setOfficial(e.target.checked)} />
          <span><strong>Resmî çıkış</strong><br />
            <span className="muted small">Kayıttan sonra fatura hazırlık özeti açılır. Faturayı GİB portalında kesip numarasını buraya girersiniz. Kapalıysa çıkış Muhasebe Bekleyen listesine düşer.</span>
          </span>
        </label>
      ) : null}
    </Dialog>
  )
}

// =====================================================================
// Tahsilat al / odeme yap
// =====================================================================

const methods = [
  { id: 'transfer', label: 'Havale / EFT', account: 'bank' },
  { id: 'cash', label: 'Nakit', account: 'cash' },
  { id: 'card', label: 'Kredi kartı / POS', account: 'pos' },
  { id: 'check', label: 'Çek', account: 'bank' },
] as const

export function PaymentDialog({ open, direction, partyId, openLines, onClose, onSaved }: {
  open: boolean; direction: 'receipt' | 'disbursement'; partyId: string; openLines: LineRow[]
  onClose: () => void; onSaved: () => void
}) {
  const { me } = useAuth()
  const receipt = direction === 'receipt'
  const [accounts, setAccounts] = useState<CashAccount[]>([])
  const [method, setMethod] = useState<(typeof methods)[number]['id']>('transfer')
  const [accountId, setAccountId] = useState('')
  const [amount, setAmount] = useState('')
  const [on, setOn] = useState(today())
  const [reference, setReference] = useState('')
  const [description, setDescription] = useState('')
  const [alloc, setAlloc] = useState<Record<string, string>>({})
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (!open) return
    setMethod('transfer'); setAmount(''); setOn(today()); setReference(''); setDescription(''); setAlloc({}); setError(null)
    api<CashAccount[]>('/api/cash-accounts').then((a) => setAccounts(a.filter((x) => x.isActive))).catch((e) => setError(errorText(e)))
  }, [open])

  const wantedKind = methods.find((m) => m.id === method)!.account
  const usable = accounts.filter((a) => method === 'check' ? a.kind !== 'pos' : a.kind === wantedKind)
  useEffect(() => { setAccountId(usable[0]?.id ?? '') }, [method, accounts]) // eslint-disable-line react-hooks/exhaustive-deps

  const allocTotal = toMoney(sumMicro(Object.values(alloc).map((v) => toMicro(normalizeNumber(v)))))

  function fillOldestFirst() {
    // Varsayilan dagitim: en eski borctan (spesifikasyon O31); fazlasi avans kalir.
    let remaining = toMicro(normalizeNumber(amount))
    const next: Record<string, string> = {}
    for (const l of [...openLines].sort((a, b) => a.occurredOn.localeCompare(b.occurredOn))) {
      if (remaining <= 0n) break
      const open = toMicro(l.openAmount)
      const take = open < remaining ? open : remaining
      if (take > 0n) { next[l.id] = toInput(toMoney(take)); remaining -= take }
    }
    setAlloc(next)
  }

  async function save() {
    setError(null)
    if (!accountId) { setError('Kasa/banka hesabı seçilmeli.'); return }
    setBusy(true)
    try {
      const allocations = Object.entries(alloc)
        .map(([tradeLineId, v]) => ({ tradeLineId, amount: normalizeNumber(v) }))
        .filter((a) => a.amount && toMicro(a.amount) > 0n)
      await post('/api/payments', {
        partyId, direction, method, cashAccountId: accountId, amount: normalizeNumber(amount), occurredOn: on,
        reference, description, allocations,
      })
      onSaved()
    } catch (e) { setError(errorText(e)) } finally { setBusy(false) }
  }

  return (
    <Dialog open={open} wide onClose={onClose} title={receipt ? 'Tahsilat Al' : 'Ödeme Yap'}
      description={receipt ? 'Cariye alacak yazar. Borca dağıtılmayan tutar avans olarak kalır.' : 'Tedarikçiye yapılan ödemeyi kaydeder.'}
      footer={<>
        <button type="button" className="btn" onClick={onClose}>Vazgeç</button>
        <button type="button" className="btn primary" onClick={save} disabled={busy}>{receipt ? 'Tahsilatı Kaydet' : 'Ödemeyi Kaydet'}</button>
      </>}>
      <ErrorBox message={error} />
      <div className="form-grid">
        <Field label="Ödeme aracı">
          <select className="input" value={method} onChange={(e) => setMethod(e.target.value as typeof method)}>
            {methods.map((m) => <option key={m.id} value={m.id}>{m.label}</option>)}
          </select>
        </Field>
        <Field label="Kasa / banka hesabı">
          <select className="input" value={accountId} onChange={(e) => setAccountId(e.target.value)}>
            {usable.map((a) => <option key={a.id} value={a.id}>{a.name}</option>)}
          </select>
        </Field>
        <Field label="Tutar (TL)"><input className="input" inputMode="decimal" value={amount} onChange={(e) => setAmount(e.target.value)} autoFocus /></Field>
        <Field label="Tarih"><input className="input" type="date" value={on} onChange={(e) => setOn(e.target.value)} /></Field>
        <Field label="Referans" hint="dekont no, POS slip no"><input className="input" value={reference} onChange={(e) => setReference(e.target.value)} /></Field>
        <Field label={receipt ? 'Tahsil eden' : 'Ödeyen'}><input className="input" value={me?.fullName ?? ''} readOnly aria-readonly="true" /></Field>
        <Field label="Açıklama" full><input className="input" value={description} onChange={(e) => setDescription(e.target.value)} /></Field>
      </div>
      {openLines.length > 0 ? (
        <div className="panel">
          <div className="panel-head">
            <h3>{receipt ? 'Borçlara dağıt' : 'Alışlara dağıt'} <span className="muted small">(isteğe bağlı)</span></h3>
            <button type="button" className="btn small" onClick={fillOldestFirst} disabled={!amount}>En eski borçtan dağıt</button>
          </div>
          <div className="table-wrap">
            <table>
              <thead><tr><th>Tarih</th><th>Kalem</th><th className="num">Açık tutar</th><th className="num">Dağıtılacak</th></tr></thead>
              <tbody>
                {openLines.map((l) => (
                  <tr key={l.id}>
                    <td>{date(l.occurredOn)}</td>
                    <td>{l.itemName} <span className="sub">{quantity(l.quantity)} adet</span></td>
                    <td className="num">{money(l.openAmount)}</td>
                    <td className="num" style={{ width: 160 }}>
                      <input className="input" inputMode="decimal" aria-label={`${l.itemName} için dağıtım`}
                        value={alloc[l.id] ?? ''} onChange={(e) => setAlloc({ ...alloc, [l.id]: e.target.value })} />
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot><tr><td colSpan={3}>Dağıtılan toplam</td><td className="num">{money(allocTotal)}</td></tr></tfoot>
            </table>
          </div>
        </div>
      ) : null}
    </Dialog>
  )
}

// =====================================================================
// Kesilen faturayi gir (ADR-005: GIB portalinda kesilir, burada kaydedilir)
// =====================================================================

type VatRow = { rate: string; net: string; vat: string }

export function DocumentDialog({ open, direction, partyId, lines, preselect, onClose, onSaved }: {
  open: boolean; direction: Direction; partyId: string; lines: LineRow[]; preselect: string[]
  onClose: () => void; onSaved: (message: string) => void
}) {
  const sale = direction === 'sale'
  const candidates = lines.filter((l) => l.direction === direction && toMicro(l.pendingQuantity) > 0n)
  const [type, setType] = useState(sale ? 'e_arsiv' : 'e_fatura')
  const [number, setNumber] = useState('')
  const [docDate, setDocDate] = useState(today())
  const [selected, setSelected] = useState<Record<string, string>>({})
  const [vatRows, setVatRows] = useState<VatRow[]>([])
  const [gross, setGross] = useState('')
  const [note, setNote] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (!open) return
    setType(sale ? 'e_arsiv' : 'e_fatura'); setNumber(''); setDocDate(today()); setNote(''); setError(null)
    const init: Record<string, string> = {}
    for (const l of candidates) if (preselect.includes(l.id)) init[l.id] = toInput(l.pendingQuantity.replace('-', ''))
    setSelected(init)
  }, [open]) // eslint-disable-line react-hooks/exhaustive-deps

  // Sistem hesabi: secilen satir paylarinin oran bazinda toplami (kesin ondalik).
  const system = useMemo(() => {
    const byRate = new Map<string, { net: bigint; vat: bigint; gross: bigint }>()
    for (const l of candidates) {
      const q = selected[l.id]
      if (!q) continue
      const qn = normalizeNumber(q)
      const sign = l.quantity.startsWith('-') ? '-' : ''
      const signedQty = sign + qn.replace('-', '')
      const cur = byRate.get(l.vatRate) ?? { net: 0n, vat: 0n, gross: 0n }
      cur.net += ratio(l.netAmount, signedQty, l.quantity)
      cur.vat += ratio(l.vatAmount, signedQty, l.quantity)
      cur.gross += ratio(l.grossAmount, signedQty, l.quantity)
      byRate.set(l.vatRate, cur)
    }
    return byRate
  }, [selected, candidates])

  useEffect(() => {
    const rows: VatRow[] = []
    let total = 0n
    for (const [rate, v] of system) {
      const abs = (x: bigint) => (x < 0n ? -x : x)
      rows.push({ rate, net: toInput(toMoney(abs(v.net))), vat: toInput(toMoney(abs(v.vat))) })
      total += abs(v.gross)
    }
    setVatRows(rows)
    setGross(rows.length ? toInput(toMoney(total)) : '')
  }, [system])

  async function save() {
    setError(null)
    const links = Object.entries(selected).filter(([, q]) => q && toMicro(normalizeNumber(q)) !== 0n)
      .map(([tradeLineId, q]) => ({ tradeLineId, quantity: normalizeNumber(q).replace('-', '') }))
    if (!number.trim()) { setError('Fatura numarası yazılmalı.'); return }
    if (links.length === 0) { setError('Faturanın kapsadığı satırlar seçilmeli.'); return }
    setBusy(true)
    try {
      const r = await post<{ difference: string; systemGross: string }>('/api/documents', {
        partyId, direction, type, documentNo: number, documentDate: docDate, grossTotal: normalizeNumber(gross),
        vatLines: vatRows.map((v) => ({ vatRate: normalizeNumber(v.rate), netAmount: normalizeNumber(v.net), vatAmount: normalizeNumber(v.vat) })),
        links, note,
      })
      const diff = toMicro(r.difference)
      onSaved(diff === 0n ? 'Fatura kaydedildi. Tutarlar sistem hesabıyla aynı.'
        : `Fatura yazıldığı gibi kaydedildi. Sistem hesabıyla fark: ${money(r.difference)} (sistem: ${money(r.systemGross)}).`)
    } catch (e) { setError(errorText(e)) } finally { setBusy(false) }
  }

  return (
    <Dialog open={open} wide onClose={onClose}
      title={sale ? 'Kesilen Faturayı Gir' : 'Alış Faturası Gir'}
      description={sale ? 'GİB portalında kestiğiniz faturanın bilgilerini girin. Tutarlar faturada yazdığı gibi kaydedilir; cari bakiye değişmez.' : 'Tedarikçiden gelen faturayı yazdığı gibi girin; cari bakiye değişmez.'}
      footer={<>
        <button type="button" className="btn" onClick={onClose}>Vazgeç</button>
        <button type="button" className="btn primary" onClick={save} disabled={busy}>Faturayı Kaydet</button>
      </>}>
      <ErrorBox message={error} />
      <div className="form-grid">
        <Field label="Belge türü">
          <select className="input" value={type} onChange={(e) => setType(e.target.value)}>
            <option value="e_arsiv">e-Arşiv fatura</option>
            <option value="e_fatura">e-Fatura</option>
            <option value="kagit">Kağıt fatura</option>
            {!sale ? <option value="fis">Perakende fiş</option> : null}
          </select>
        </Field>
        <Field label="Fatura numarası"><input className="input" value={number} onChange={(e) => setNumber(e.target.value)} autoFocus /></Field>
        <Field label="Fatura tarihi"><input className="input" type="date" value={docDate} onChange={(e) => setDocDate(e.target.value)} /></Field>
        <Field label="Not"><input className="input" value={note} onChange={(e) => setNote(e.target.value)} /></Field>
      </div>

      <div className="panel">
        <div className="panel-head"><h3>Faturanın kapsadığı satırlar</h3></div>
        {candidates.length === 0 ? <p className="empty">Faturası girilmemiş satır yok.</p> : (
          <div className="table-wrap">
            <table>
              <thead><tr><th></th><th>Tarih</th><th>Kalem</th><th className="num">Belgelenmemiş</th><th className="num">Faturadaki miktar</th></tr></thead>
              <tbody>
                {candidates.map((l) => {
                  const checked = selected[l.id] !== undefined
                  return (
                    <tr key={l.id}>
                      <td><input type="checkbox" aria-label={`${l.itemName} satırını seç`} checked={checked}
                        onChange={(e) => {
                          const next = { ...selected }
                          if (e.target.checked) next[l.id] = toInput(l.pendingQuantity.replace('-', '')); else delete next[l.id]
                          setSelected(next)
                        }} /></td>
                      <td>{date(l.occurredOn)}</td>
                      <td>{l.itemName}{l.reversesLineId ? <span className="sub">İade satırı — iade faturası</span> : null}</td>
                      <td className="num">{quantity(l.pendingQuantity)}</td>
                      <td className="num" style={{ width: 150 }}>
                        <input className="input" inputMode="decimal" disabled={!checked} aria-label={`${l.itemName} fatura miktarı`}
                          value={selected[l.id] ?? ''} onChange={(e) => setSelected({ ...selected, [l.id]: e.target.value })} />
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {vatRows.length ? (
        <div className="panel">
          <div className="panel-head">
            <h3>Faturada yazan tutarlar</h3>
            <span className="muted small">Sistem hesabıyla dolduruldu; faturada farklıysa faturadakini yazın.</span>
          </div>
          <div className="table-wrap">
            <table>
              <thead><tr><th>KDV oranı</th><th className="num">Matrah</th><th className="num">KDV</th></tr></thead>
              <tbody>
                {vatRows.map((v, i) => (
                  <tr key={v.rate}>
                    <td>{percent(v.rate)}</td>
                    <td className="num"><input className="input" inputMode="decimal" aria-label={`%${v.rate} matrah`} value={v.net}
                      onChange={(e) => setVatRows(vatRows.map((x, j) => j === i ? { ...x, net: e.target.value } : x))} /></td>
                    <td className="num"><input className="input" inputMode="decimal" aria-label={`%${v.rate} KDV`} value={v.vat}
                      onChange={(e) => setVatRows(vatRows.map((x, j) => j === i ? { ...x, vat: e.target.value } : x))} /></td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <td colSpan={2}>Genel toplam (faturada yazan)</td>
                  <td className="num"><input className="input" inputMode="decimal" aria-label="Genel toplam" value={gross} onChange={(e) => setGross(e.target.value)} /></td>
                </tr>
              </tfoot>
            </table>
          </div>
        </div>
      ) : null}
    </Dialog>
  )
}

// =====================================================================
// Fatura hazirlik ozeti (resmi belge degildir)
// =====================================================================

export function InvoicePrepDialog({ open, line, partyName, onClose, onEnterInvoice }: {
  open: boolean; line: LineRow | null; partyName: string; onClose: () => void; onEnterInvoice: () => void
}) {
  return (
    <Dialog open={open} onClose={onClose} title="Fatura hazırlık özeti"
      description="Bu özet resmî belge değildir. Faturayı GİB portalında keserken kullanın, sonra numarasını girin."
      footer={<>
        <button type="button" className="btn" onClick={() => window.print()}>Özeti Yazdır</button>
        <button type="button" className="btn" onClick={onClose}>Sonra</button>
        <button type="button" className="btn primary" onClick={onEnterInvoice}>Portalda Kestim, Faturayı Gir</button>
      </>}>
      {line ? (
        <div className="calc">
          <div><span>Alıcı</span><strong>{partyName}</strong></div>
          <div><span>Kalem</span><strong>{line.itemName} × {quantity(line.quantity)}</strong></div>
          <div><span>Birim fiyat</span><span>{money(line.unitPrice)}</span></div>
          {toMicro(line.discountPercent) > 0n ? <div><span>İskonto</span><span>{percent(line.discountPercent)}</span></div> : null}
          <div><span>Matrah</span><span>{money(toMoney(toMicro(line.netAmount)))}</span></div>
          <div><span>KDV {percent(line.vatRate)}</span><span>{money(toMoney(toMicro(line.vatAmount)))}</span></div>
          <div className="total"><span>Genel toplam</span><span>{money(line.grossAmount)}</span></div>
        </div>
      ) : null}
    </Dialog>
  )
}

// =====================================================================
// Iade, dagitim, acilis bakiyesi
// =====================================================================

export function ReverseDialog({ open, line, onClose, onSaved }: { open: boolean; line: LineRow | null; onClose: () => void; onSaved: () => void }) {
  const [qty, setQty] = useState('')
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  useEffect(() => { if (open) { setQty(''); setReason(''); setError(null) } }, [open])
  return (
    <Dialog open={open} onClose={onClose} title="İade / Düzeltme"
      description="İlk kayıt değişmez; ters satır oluşur. Cari ve stok buna göre düzelir."
      footer={<>
        <button type="button" className="btn" onClick={onClose}>Vazgeç</button>
        <button type="button" className="btn danger" onClick={async () => {
          setError(null)
          try { await post(`/api/trade-lines/${line!.id}/reverse`, { quantity: normalizeNumber(qty), reason }); onSaved() }
          catch (e) { setError(errorText(e)) }
        }}>İadeyi Kaydet</button>
      </>}>
      <ErrorBox message={error} />
      {line ? <p>{line.itemName} · satılan {quantity(line.quantity)} · {money(line.grossAmount)}</p> : null}
      <Field label="İade miktarı"><input className="input" inputMode="decimal" value={qty} onChange={(e) => setQty(e.target.value)} autoFocus /></Field>
      <Field label="Gerekçe"><textarea className="input" value={reason} onChange={(e) => setReason(e.target.value)} /></Field>
    </Dialog>
  )
}

export function AllocateDialog({ open, payment, openLines, onClose, onSaved }: {
  open: boolean; payment: PaymentRow | null; openLines: LineRow[]; onClose: () => void; onSaved: () => void
}) {
  const [alloc, setAlloc] = useState<Record<string, string>>({})
  const [error, setError] = useState<string | null>(null)
  useEffect(() => { if (open) { setAlloc({}); setError(null) } }, [open])
  return (
    <Dialog open={open} wide onClose={onClose} title="Tahsilatı dağıt"
      description={payment ? `${date(payment.occurredOn)} · ${payment.method} · dağıtılmamış ${money(payment.unallocatedAmount)}` : undefined}
      footer={<>
        <button type="button" className="btn" onClick={onClose}>Vazgeç</button>
        <button type="button" className="btn primary" onClick={async () => {
          setError(null)
          const list = Object.entries(alloc).map(([tradeLineId, v]) => ({ tradeLineId, amount: normalizeNumber(v) })).filter((x) => x.amount)
          try { await post(`/api/payments/${payment!.id}/allocations`, list); onSaved() } catch (e) { setError(errorText(e)) }
        }}>Dağıtımı Kaydet</button>
      </>}>
      <ErrorBox message={error} />
      {openLines.length === 0 ? <p className="empty">Açık borç yok. Tutar avans olarak kalır.</p> : (
        <table>
          <thead><tr><th>Tarih</th><th>Kalem</th><th className="num">Açık tutar</th><th className="num">Dağıtılacak</th></tr></thead>
          <tbody>
            {openLines.map((l) => (
              <tr key={l.id}>
                <td>{date(l.occurredOn)}</td><td>{l.itemName}</td><td className="num">{money(l.openAmount)}</td>
                <td className="num" style={{ width: 160 }}><input className="input" inputMode="decimal" aria-label={`${l.itemName} için dağıtım`}
                  value={alloc[l.id] ?? ''} onChange={(e) => setAlloc({ ...alloc, [l.id]: e.target.value })} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </Dialog>
  )
}

export function OpeningBalanceDialog({ open, partyId, onClose, onSaved }: { open: boolean; partyId: string; onClose: () => void; onSaved: () => void }) {
  const [amount, setAmount] = useState('')
  const [side, setSide] = useState<'debit' | 'credit'>('debit')
  const [note, setNote] = useState('')
  const [error, setError] = useState<string | null>(null)
  useEffect(() => { if (open) { setAmount(''); setSide('debit'); setNote(''); setError(null) } }, [open])
  return (
    <Dialog open={open} onClose={onClose} title="Açılış bakiyesi"
      description="Açılış bakiyesi cariye doğrudan yazılmaz; Muhasebe Bekleyen listesinde onaylandığında yazılır."
      footer={<>
        <button type="button" className="btn" onClick={onClose}>Vazgeç</button>
        <button type="button" className="btn primary" onClick={async () => {
          setError(null)
          const n = normalizeNumber(amount).replace('-', '')
          try { await post('/api/opening-balances', { partyId, amount: side === 'debit' ? n : `-${n}`, note }); onSaved() }
          catch (e) { setError(errorText(e)) }
        }}>Onaya Gönder</button>
      </>}>
      <ErrorBox message={error} />
      <Field label="Yön">
        <select className="input" value={side} onChange={(e) => setSide(e.target.value as 'debit' | 'credit')}>
          <option value="debit">Cari bize borçlu</option>
          <option value="credit">Cari alacaklı (avans / bizim borcumuz)</option>
        </select>
      </Field>
      <Field label="Tutar (TL)"><input className="input" inputMode="decimal" value={amount} onChange={(e) => setAmount(e.target.value)} /></Field>
      <Field label="Dayanak / not"><input className="input" value={note} onChange={(e) => setNote(e.target.value)} placeholder="ör. 31.12.2025 mizan" /></Field>
    </Dialog>
  )
}
