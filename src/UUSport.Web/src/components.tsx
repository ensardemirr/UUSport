import { createContext, useCallback, useContext, useEffect, useId, useRef, useState, type ReactNode } from 'react'
import { ApiError } from './api'

// ---------------- veri yukleme ----------------

export function useAsync<T>(load: () => Promise<T>, deps: unknown[]) {
  const [data, setData] = useState<T | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [version, setVersion] = useState(0)
  useEffect(() => {
    let alive = true
    setLoading(true)
    load()
      .then((d) => { if (alive) { setData(d); setError(null) } })
      .catch((e: unknown) => { if (alive) setError(e instanceof Error ? e.message : 'Veri yüklenemedi.') })
      .finally(() => { if (alive) setLoading(false) })
    return () => { alive = false }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, version])
  const reload = useCallback(() => setVersion((v) => v + 1), [])
  return { data, error, loading, reload }
}

export function errorText(e: unknown) {
  if (e instanceof ApiError) return e.message
  if (e instanceof Error) return e.message
  return 'Beklenmeyen bir hata oluştu.'
}

// ---------------- bildirim ----------------

const ToastContext = createContext<(message: string) => void>(() => {})
export const useToast = () => useContext(ToastContext)

export function ToastProvider({ children }: { children: ReactNode }) {
  const [message, setMessage] = useState<string | null>(null)
  const timer = useRef<number | undefined>(undefined)
  const show = useCallback((m: string) => {
    setMessage(m)
    window.clearTimeout(timer.current)
    timer.current = window.setTimeout(() => setMessage(null), 4500)
  }, [])
  return (
    <ToastContext.Provider value={show}>
      {children}
      <div role="status" aria-live="polite" className={message ? 'toast' : 'sr-only'}>{message}</div>
    </ToastContext.Provider>
  )
}

// ---------------- temel parcalar ----------------

export function ErrorBox({ message }: { message: string | null | undefined }) {
  if (!message) return null
  return <div className="alert error" role="alert">{message}</div>
}

export function Loading({ label = 'Yükleniyor…' }: { label?: string }) {
  return <div className="loading" role="status">{label}</div>
}

export function Empty({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="empty">
      <strong>{title}</strong>
      {children}
    </div>
  )
}

export function Field({ label, hint, children, full }: { label: string; hint?: string; children: ReactNode; full?: boolean }) {
  return (
    <label className={`field${full ? ' full' : ''}`}>
      <span>{label}{hint ? <span className="hint"> — {hint}</span> : null}</span>
      {children}
    </label>
  )
}

const statusText: Record<string, [string, string]> = {
  pending_accounting: ['Muhasebe bekliyor', 'pending'],
  partially_documented: ['Kısmen belgelendi', 'partial'],
  documented: ['Belgelendi', 'done'],
  reversed: ['İade edildi', 'neutral'],
  reversal: ['İade', 'neutral'],
  issued: ['Kayıtlı', 'done'],
  cancelled: ['İptal', 'danger'],
  Previewed: ['Önizlendi', 'pending'],
  Applied: ['Uygulandı', 'done'],
  Reverted: ['Geri alındı', 'neutral'],
}

export function StatusBadge({ status }: { status: string }) {
  const [text, cls] = statusText[status] ?? [status, 'neutral']
  return <span className={`badge ${cls}`}>{text}</span>
}

// ---------------- sekmeler (WAI-ARIA tab kalibi) ----------------

export type TabDef = { id: string; label: string; count?: number }

export function Tabs({ tabs, value, onChange, label, children }: {
  tabs: TabDef[]; value: string; onChange: (id: string) => void; label: string; children: ReactNode
}) {
  const base = useId()
  const refs = useRef<(HTMLButtonElement | null)[]>([])
  const index = tabs.findIndex((t) => t.id === value)
  const move = (next: number) => {
    const i = (next + tabs.length) % tabs.length
    onChange(tabs[i].id)
    refs.current[i]?.focus()
  }
  return (
    <>
      <div role="tablist" aria-label={label} className="tabs"
        onKeyDown={(e) => {
          if (e.key === 'ArrowRight') { e.preventDefault(); move(index + 1) }
          if (e.key === 'ArrowLeft') { e.preventDefault(); move(index - 1) }
          if (e.key === 'Home') { e.preventDefault(); move(0) }
          if (e.key === 'End') { e.preventDefault(); move(tabs.length - 1) }
        }}>
        {tabs.map((t, i) => (
          <button key={t.id} ref={(el) => { refs.current[i] = el }} type="button" role="tab" className="tab"
            id={`${base}-tab-${t.id}`} aria-controls={`${base}-panel`} aria-selected={t.id === value}
            tabIndex={t.id === value ? 0 : -1} onClick={() => onChange(t.id)}>
            {t.label}{t.count !== undefined ? <span className="count">{t.count}</span> : null}
          </button>
        ))}
      </div>
      <div role="tabpanel" id={`${base}-panel`} aria-labelledby={`${base}-tab-${value}`} tabIndex={0}>
        {children}
      </div>
    </>
  )
}

// ---------------- dialog ----------------

export function Dialog({ open, title, description, onClose, children, footer, wide }: {
  open: boolean; title: string; description?: string; onClose: () => void; children: ReactNode; footer?: ReactNode; wide?: boolean
}) {
  const ref = useRef<HTMLDialogElement>(null)
  const titleId = useId()
  useEffect(() => {
    const d = ref.current
    if (!d) return
    if (open && !d.open) d.showModal()
    if (!open && d.open) d.close()
  }, [open])
  return (
    <dialog ref={ref} className={wide ? 'wide' : undefined} aria-labelledby={titleId}
      onCancel={(e) => { e.preventDefault(); onClose() }}>
      {open ? (
        <>
          <div className="dialog-head">
            <div>
              <h2 id={titleId}>{title}</h2>
              {description ? <p>{description}</p> : null}
            </div>
            <button type="button" className="btn ghost" onClick={onClose} aria-label="Pencereyi kapat">✕</button>
          </div>
          <div className="dialog-body">{children}</div>
          {footer ? <div className="dialog-foot">{footer}</div> : null}
        </>
      ) : null}
    </dialog>
  )
}

/**
 * Tehlikeli veya geri alinamayan islem icin onay. Gerekce zorunluysa bos gecilemez.
 */
export function ReasonDialog({ open, title, description, confirmLabel, onClose, onConfirm }: {
  open: boolean; title: string; description: string; confirmLabel: string
  onClose: () => void; onConfirm: (reason: string) => Promise<void>
}) {
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  useEffect(() => { if (open) { setReason(''); setError(null) } }, [open])
  return (
    <Dialog open={open} title={title} description={description} onClose={onClose}
      footer={<>
        <button type="button" className="btn" onClick={onClose}>Vazgeç</button>
        <button type="button" className="btn danger" disabled={busy}
          onClick={async () => {
            if (!reason.trim()) { setError('Gerekçe yazılmalı.'); return }
            setBusy(true)
            try { await onConfirm(reason.trim()); onClose() } catch (e) { setError(errorText(e)) } finally { setBusy(false) }
          }}>{confirmLabel}</button>
      </>}>
      <ErrorBox message={error} />
      <Field label="Gerekçe">
        <textarea className="input" value={reason} onChange={(e) => setReason(e.target.value)} autoFocus />
      </Field>
    </Dialog>
  )
}
