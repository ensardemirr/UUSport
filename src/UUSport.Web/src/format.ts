// Gosterim bicimleri (tr-TR). Hesap yapilmaz; yalniz sunucudan gelen degerler gosterilir.

const tl = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' })
const qty = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 4 })
const pct = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 2 })

export const money = (v: string | number | null | undefined) =>
  v === null || v === undefined || v === '' ? '—' : tl.format(Number(v))

export const quantity = (v: string | number | null | undefined) =>
  v === null || v === undefined || v === '' ? '—' : qty.format(Number(v))

export const percent = (v: string | number | null | undefined) =>
  v === null || v === undefined || v === '' ? '—' : `%${pct.format(Number(v))}`

export const date = (iso: string | null | undefined) => {
  if (!iso) return '—'
  const [y, m, d] = iso.slice(0, 10).split('-')
  return `${d}.${m}.${y}`
}

export const dateTime = (iso: string | null | undefined) =>
  iso ? new Date(iso).toLocaleString('tr-TR', { dateStyle: 'short', timeStyle: 'short' }) : '—'

export const today = () => {
  const d = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

/**
 * Kullanicinin yazdigi sayiyi sunucuya gidecek bicime cevirir:
 * "1.234,56" -> "1234.56", "1.500" -> "1500", "250,5" -> "250.5".
 */
export function normalizeNumber(input: string): string {
  const t = input.trim().replace(/\s|TL|₺/gi, '')
  if (t === '') return ''
  if (t.includes(',')) return t.replace(/\./g, '').replace(',', '.')
  if (/^-?\d{1,3}(\.\d{3})+$/.test(t)) return t.replace(/\./g, '')
  return t
}

export const isNumber = (input: string) => /^-?\d+(\.\d+)?$/.test(normalizeNumber(input))

export const kindName = (k: string) =>
  ({ customer: 'Müşteri / veli', supplier: 'Tedarikçi', both: 'Müşteri ve tedarikçi' })[k] ?? k

export const docTypeName = (t: string) =>
  ({ e_arsiv: 'e-Arşiv', e_fatura: 'e-Fatura', kagit: 'Kağıt fatura', fis: 'Fiş' })[t] ?? t

/** Sunucudan gelen '1.5000' / '3454.55' degerini giris kutusu icin '1,5' / '3454,55' yapar. */
export const toInput = (v: string) => {
  const t = v.includes('.') ? v.replace(/0+$/, '').replace(/\.$/, '') : v
  return t.replace('.', ',')
}
