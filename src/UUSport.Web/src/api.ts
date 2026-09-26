// API istemcisi. Para, miktar ve oran sunucudan METIN olarak gelir (ADR-002);
// istemci otoriter para hesabi yapmaz, yalniz gosterir.

export type Money = string

export class ApiError extends Error {
  readonly status: number
  readonly code: string
  constructor(status: number, code: string, message: string) {
    super(message)
    this.status = status
    this.code = code
  }
}

type Options = { method?: string; body?: unknown; form?: FormData }

export async function api<T>(path: string, options: Options = {}): Promise<T> {
  const headers: Record<string, string> = { 'X-UUSport-Request': '1' }
  let body: BodyInit | undefined
  if (options.form) body = options.form
  else if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json'
    body = JSON.stringify(options.body)
  }
  const res = await fetch(path, {
    method: options.method ?? (body ? 'POST' : 'GET'),
    headers,
    body,
    credentials: 'same-origin',
  })
  const text = await res.text()
  let data: unknown = null
  try {
    data = text ? JSON.parse(text) : null
  } catch {
    data = null
  }
  if (!res.ok) {
    const d = (data ?? {}) as { code?: string; message?: string }
    if (res.status === 401) window.dispatchEvent(new Event('uusport:unauthorized'))
    if (res.status === 403 && !d.code) window.dispatchEvent(new Event('uusport:forbidden'))
    const fallback =
      res.status === 403 ? 'Bu işlem için yetkiniz yok.'
        : res.status === 429 ? 'Çok fazla deneme yapıldı. Bir dakika sonra tekrar deneyin.'
          : res.status === 401 ? 'Oturum süresi doldu. Yeniden giriş yapın.'
            : 'İstek tamamlanamadı.'
    throw new ApiError(res.status, d.code ?? 'error', d.message ?? fallback)
  }
  return data as T
}

export const post = <T>(path: string, body?: unknown) => api<T>(path, { method: 'POST', body: body ?? {} })
export const put = <T>(path: string, body: unknown) => api<T>(path, { method: 'PUT', body })

// ---------------- tipler ----------------

export type Me = {
  authenticated: boolean
  userName?: string
  fullName?: string
  roles?: string[]
  maxDiscountPercent?: Money
  mfaEnabled?: boolean
}

export type PartyKind = 'customer' | 'supplier' | 'both'

export type PartyListRow = {
  id: string; code: string; name: string; kind: PartyKind; phone: string | null; email: string | null
  isActive: boolean; balance: Money; athletes: string[]
}

export type PartySummary = {
  partyId: string; balance: Money; documentedSalesGross: Money; pendingSalesGross: Money
  unallocatedReceipts: Money; pendingLineCount: number; unallocatedPaymentCount: number
}

export type PartyDetail = {
  party: {
    id: string; code: string; name: string; kind: PartyKind; phone: string | null; email: string | null
    taxNumber: string | null; notes: string | null; accountCode: string | null; isActive: boolean
  }
  athletes: { id: string; firstName: string; lastName: string; groupName: string | null; birthYear: number | null; isActive: boolean }[]
  summary: PartySummary
}

export type LineRow = {
  id: string; partyId: string; partyName: string; occurredOn: string; direction: 'sale' | 'purchase'
  itemId: string; itemName: string; itemKind: 'stock' | 'service'
  quantity: Money; unitPrice: Money; discountPercent: Money; vatRate: Money
  netAmount: Money; vatAmount: Money; grossAmount: Money
  documentedQuantity: Money; pendingQuantity: Money; allocatedAmount: Money; openAmount: Money
  status: 'pending_accounting' | 'partially_documented' | 'documented' | 'reversed' | 'reversal'
  reversesLineId: string | null; description: string | null; documents: string[]
}

export type LedgerRow = {
  id: string; occurredOn: string; sourceType: string; sourceId: string; description: string
  debit: Money; credit: Money; balance: Money
}

export type PaymentRow = {
  id: string; partyId: string; partyName: string; occurredOn: string; direction: 'receipt' | 'disbursement'
  method: string; accountName: string; amount: Money; allocatedAmount: Money; unallocatedAmount: Money
  reference: string | null; description: string | null; isReversal: boolean; isReversed: boolean
}

export type DocumentRow = {
  id: string; partyId: string; partyName: string; direction: 'sale' | 'purchase'; type: string
  documentNo: string; documentDate: string; netTotal: Money; vatTotal: Money; grossTotal: Money
  status: 'issued' | 'cancelled'; cancelReason: string | null; linkCount: number
}

export type Item = {
  id: string; code: string; name: string; kind: 'stock' | 'service'; unit: string; unitPrice: Money
  priceIncludesVat: boolean; vatRate: Money; accountCode: string | null; isActive: boolean; stockOnHand: Money | null
}

export type CashAccountKind = 'cash' | 'bank' | 'pos' | 'credit_card' | 'loan'

export type CashAccount = {
  id: string; name: string; kind: CashAccountKind; isActive: boolean; balance: Money
  parentAccountId: string | null; parentAccountName: string | null
}

export type PendingWork = {
  salesAwaitingInvoice: LineRow[]
  purchasesAwaitingInvoice: LineRow[]
  unallocatedPayments: PaymentRow[]
  pendingOpeningBalances: { id: string; partyId: string; partyName: string; amount: Money; note: string | null; importBatchId: string | null }[]
}

// ---------------- surum 2 hazirlik notu: kredi ve cek ----------------

export type LoanRow = {
  id: string; partyId: string; partyName: string; cashAccountId: string; cashAccountName: string; name: string
  principalAmount: Money; startDate: string; isActive: boolean; installmentCount: number; paidCount: number
  remainingPrincipal: Money; nextDueDate: string | null
}

export type LoanInstallmentRow = {
  id: string; installmentNo: number; dueDate: string; principalAmount: Money; interestAmount: Money
  feeAmount: Money; totalAmount: Money; isPaid: boolean; paymentId: string | null
}

export type CheckDirection = 'received' | 'issued'
export type CheckStatus = 'in_portfolio' | 'submitted_for_collection' | 'collected' | 'endorsed' | 'bounced'

export type CheckRow = {
  id: string; partyId: string; partyName: string; direction: CheckDirection; checkNo: string; bankName: string
  dueDate: string; amount: Money; status: CheckStatus; batchId: string; paymentId: string | null; statusNote: string | null
}

export type CheckSeriesRow = { checkNo: string; dueDate: string; amount: Money }
