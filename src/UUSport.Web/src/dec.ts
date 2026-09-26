// Kesin ondalik aritmetik (ADR-002 bolum 2.5): JavaScript sayilari ikili kayan noktalidir.
// Degerler 10^-6 hassasiyetli BigInt olarak tutulur; yuvarlama "yarim yukari".
// Yalniz on doldurma icindir: kaydedilen her tutar sunucunun hesabidir.

const SCALE = 1_000_000n

export function toMicro(value: string | number | null | undefined): bigint {
  if (value === null || value === undefined || value === '') return 0n
  const s = String(value).trim()
  const neg = s.startsWith('-')
  const [intPart, fracPart = ''] = s.replace('-', '').split('.')
  const frac = (fracPart + '000000').slice(0, 6)
  const micro = BigInt(intPart || '0') * SCALE + BigInt(frac)
  return neg ? -micro : micro
}

function divRound(a: bigint, b: bigint): bigint {
  const neg = (a < 0n) !== (b < 0n)
  const x = a < 0n ? -a : a
  const y = b < 0n ? -b : b
  const q = (x * 2n + y) / (2n * y)
  return neg ? -q : q
}

/** value * num / den, sonuc micro birimde */
export function ratio(value: string, num: string, den: string): bigint {
  const d = toMicro(den)
  if (d === 0n) return 0n
  return divRound(toMicro(value) * toMicro(num), d)
}

/** micro -> 2 haneli metin ("1234.56"), yarim yukari */
export function toMoney(micro: bigint): string {
  const cents = divRound(micro, 10_000n)
  const neg = cents < 0n
  const abs = neg ? -cents : cents
  const s = `${abs / 100n}.${String(abs % 100n).padStart(2, '0')}`
  return neg ? `-${s}` : s
}

export const sumMicro = (values: bigint[]) => values.reduce((a, b) => a + b, 0n)
