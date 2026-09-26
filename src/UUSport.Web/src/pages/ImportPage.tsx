import { useState } from 'react'
import { api, post } from '../api'
import { Empty, ErrorBox, Field, Loading, StatusBadge, errorText, useAsync, useToast } from '../components'
import { dateTime } from '../format'

type FieldDef = { key: string; label: string; required: boolean }
type Column = { index: number; letter: string; header: string }
type Inspect = { sheets: string[]; selected: { name: string; columns: Column[]; dataRowCount: number } | null; studentSheet: string }
type BatchRow = { rowNumber: number; data: Record<string, string | null>; isValid: boolean; errors: string | null; warnings: string | null }
type BatchDetail = {
  batch: { id: string; kind: string; fileName: string; sheetName: string; status: string; rowsRead: number; rowsValid: number; rowsError: number }
  rows: BatchRow[]; fields: FieldDef[]
}
type BatchListRow = { id: string; kind: string; fileName: string; sheetName: string; status: string; rowsRead: number; rowsValid: number; rowsError: number; createdAt: string }

const kinds = [
  { id: 'ogrenci', label: 'Öğrenci listesi', note: 'Veli carisi + sporcu + isteğe bağlı açılış bakiyesi. Yalnız “Sayfa1 (2)” sayfası okunur.' },
  { id: 'cari', label: 'Cari listesi', note: 'Müşteri / tedarikçi carileri ve isteğe bağlı açılış bakiyesi.' },
  { id: 'urun', label: 'Ürün ve hizmet listesi', note: 'Katalog kalemleri, fiyat ve KDV oranı.' },
]

function guess(field: FieldDef, columns: Column[]): number | undefined {
  const norm = (s: string) => s.toLocaleLowerCase('tr-TR').replace(/[^a-zçğıöşü0-9]/g, '')
  const hints: Record<string, string[]> = {
    veli_adi: ['veli', 'anne', 'baba', 'velıadı'], veli_telefon: ['telefon', 'tel', 'gsm', 'cep'],
    ogrenci_adi: ['öğrenci', 'ogrenci', 'sporcu', 'adı', 'ad'], ogrenci_soyadi: ['soyad'], grup: ['grup', 'branş', 'brans', 'sınıf'],
    dogum_yili: ['doğum', 'dogum', 'yaş'], acilis_bakiyesi: ['bakiye', 'borç', 'kalan'],
    cari_adi: ['cari', 'unvan', 'ad'], cari_kodu: ['kod'], tur: ['tür', 'tip'], telefon: ['telefon', 'tel'], eposta: ['posta', 'mail'],
    vergi_no: ['vergi', 'tc', 'vkn'], ad: ['ad', 'ürün', 'hizmet'], kod: ['kod'], birim: ['birim'], birim_fiyat: ['fiyat', 'tutar'],
    kdv_orani: ['kdv'], kdv_dahil: ['dahil'],
  }
  const list = hints[field.key] ?? []
  return columns.find((c) => list.some((h) => norm(c.header).includes(norm(h))))?.index
}

export function ImportPage() {
  const toast = useToast()
  const [kind, setKind] = useState('ogrenci')
  const [file, setFile] = useState<File | null>(null)
  const [inspect, setInspect] = useState<Inspect | null>(null)
  const [sheet, setSheet] = useState('')
  const [fields, setFields] = useState<FieldDef[]>([])
  const [mapping, setMapping] = useState<Record<string, number>>({})
  const [batch, setBatch] = useState<BatchDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const history = useAsync(() => api<BatchListRow[]>('/api/imports'), [batch?.batch.status])

  const step = batch ? (batch.batch.status === 'Previewed' ? 3 : 4) : inspect ? 2 : 1

  function reset() { setFile(null); setInspect(null); setBatch(null); setMapping({}); setError(null) }

  async function doInspect(f: File, forSheet?: string) {
    setBusy(true); setError(null)
    try {
      const form = new FormData(); form.append('file', f)
      const params = new URLSearchParams({ kind }); if (forSheet) params.set('sheet', forSheet)
      const r = await api<Inspect>(`/api/imports/inspect?${params}`, { method: 'POST', form })
      const kindDefs = await api<{ kind: string; fields: FieldDef[] }[]>('/api/imports/kinds')
      const defs = kindDefs.find((k) => k.kind === kind)?.fields ?? []
      setInspect(r); setSheet(r.selected?.name ?? ''); setFields(defs)
      const m: Record<string, number> = {}
      for (const d of defs) { const g = guess(d, r.selected?.columns ?? []); if (g !== undefined) m[d.key] = g }
      setMapping(m)
    } catch (e) { setError(errorText(e)); setInspect(null) } finally { setBusy(false) }
  }

  async function doPreview() {
    if (!file) return
    setBusy(true); setError(null)
    try {
      const form = new FormData(); form.append('file', file)
      const params = new URLSearchParams({ kind, sheet, mapping: JSON.stringify(mapping) })
      const r = await api<{ id: string }>(`/api/imports/preview?${params}`, { method: 'POST', form })
      setBatch(await api<BatchDetail>(`/api/imports/${r.id}`))
    } catch (e) { setError(errorText(e)) } finally { setBusy(false) }
  }

  async function doApply() {
    if (!batch) return
    setBusy(true); setError(null)
    try {
      await post(`/api/imports/${batch.batch.id}/apply`)
      setBatch(await api<BatchDetail>(`/api/imports/${batch.batch.id}`))
      toast('Aktarım uygulandı. Açılış bakiyeleri Muhasebe Bekleyen listesinde onay bekliyor.')
    } catch (e) { setError(errorText(e)) } finally { setBusy(false) }
  }

  const kindInfo = kinds.find((k) => k.id === kind)!

  return (
    <>
      <div className="page-head">
        <div><h1>Excel Aktarımı</h1><p className="sub">Önce kontrol edin, sonra kayıtları oluşturun. Kaynak dosya değiştirilmez; aynı dosya ikinci kez uygulanamaz.</p></div>
        {step > 1 ? <button className="btn" type="button" onClick={reset}>Yeni Aktarım</button> : null}
      </div>
      <ol className="steps" aria-label="Aktarım adımları">
        {['Dosya seçimi', 'Sütunları eşleştir', 'Satırları kontrol et', 'Tamamlandı'].map((s, i) => (
          <li key={s} aria-current={step === i + 1 ? 'step' : undefined} className={step > i + 1 ? 'done' : undefined}>{i + 1} · {s}</li>
        ))}
      </ol>
      <ErrorBox message={error} />

      {step === 1 ? (
        <div className="panel"><div className="panel-body stack">
          <Field label="Aktarım türü">
            <select className="input" value={kind} onChange={(e) => setKind(e.target.value)}>
              {kinds.map((k) => <option key={k.id} value={k.id}>{k.label}</option>)}
            </select>
          </Field>
          <p className="muted">{kindInfo.note}</p>
          <div className="row">
            <a className="btn" href={`/api/imports/template?kind=${kind}`}>Şablon İndir</a>
            <span className="muted small">Zorunlu alanlar kalın işaretlidir; 2. satırdaki örnek değeri silip verinizi yazın.</span>
          </div>
          <Field label="Excel dosyası (.xlsx)">
            <input className="input" type="file" accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
              onChange={(e) => { const f = e.target.files?.[0] ?? null; setFile(f); if (f) void doInspect(f) }} />
          </Field>
          {busy ? <Loading label="Dosya okunuyor…" /> : null}
          <div className="alert info">Açılış bakiyeleri cariye doğrudan yazılmaz; muhasebe onayından sonra yazılır. Aynı veli birden çok satırda geçiyorsa tek cari oluşur.</div>
        </div></div>
      ) : null}

      {step === 2 && inspect ? (
        <div className="panel">
          <div className="panel-head">
            <h2>{file?.name}</h2>
            <span className="muted small">{inspect.sheets.length} sayfa · okunacak: <strong>{sheet}</strong> ({inspect.selected?.dataRowCount ?? 0} satır)</span>
          </div>
          <div className="panel-body stack">
            {kind === 'ogrenci' ? (
              <div className="alert info">Öğrenci aktarımı yalnız <strong>{inspect.studentSheet}</strong> sayfasından yapılır. Dosyadaki diğer sayfalar ({inspect.sheets.filter((s) => s !== inspect.studentSheet).join(', ') || 'yok'}) okunmaz ve birleştirilmez.</div>
            ) : (
              <Field label="Sayfa">
                <select className="input" value={sheet} onChange={(e) => { setSheet(e.target.value); if (file) void doInspect(file, e.target.value) }}>
                  {inspect.sheets.map((s) => <option key={s} value={s}>{s}</option>)}
                </select>
              </Field>
            )}
            <div className="form-grid">
              {fields.map((f) => (
                <Field key={f.key} label={`${f.label}${f.required ? ' *' : ''}`}>
                  <select className="input" value={mapping[f.key] ?? ''} aria-required={f.required}
                    onChange={(e) => { const m = { ...mapping }; if (e.target.value) m[f.key] = Number(e.target.value); else delete m[f.key]; setMapping(m) }}>
                    <option value="">— aktarma —</option>
                    {inspect.selected?.columns.map((c) => <option key={c.index} value={c.index}>{c.letter}: {c.header}</option>)}
                  </select>
                </Field>
              ))}
            </div>
            <div className="actions" style={{ justifyContent: 'flex-end' }}>
              <button className="btn primary" type="button" onClick={doPreview} disabled={busy}>Satırları Kontrol Et</button>
            </div>
          </div>
        </div>
      ) : null}

      {batch ? <BatchView detail={batch} busy={busy} onApply={doApply} /> : null}

      <div className="panel" style={{ marginTop: 20 }}>
        <div className="panel-head"><h2>Aktarım geçmişi</h2></div>
        {!history.data ? <Loading /> : history.data.length === 0 ? <Empty title="Henüz aktarım yapılmadı" /> : (
          <table>
            <thead><tr><th>Zaman</th><th>Dosya</th><th>Tür</th><th className="num">Satır</th><th>Durum</th><th><span className="sr-only">İşlemler</span></th></tr></thead>
            <tbody>{history.data.map((b) => (
              <tr key={b.id}>
                <td className="small">{dateTime(b.createdAt)}</td>
                <td>{b.fileName}<span className="sub">{b.sheetName}</span></td>
                <td>{kinds.find((k) => k.id === b.kind)?.label ?? b.kind}</td>
                <td className="num">{b.rowsValid}/{b.rowsRead}{b.rowsError ? <span className="sub">{b.rowsError} hatalı</span> : null}</td>
                <td><StatusBadge status={b.status} /></td>
                <td className="num">
                  <div className="actions" style={{ justifyContent: 'flex-end' }}>
                    <button className="btn small ghost" type="button" onClick={async () => setBatch(await api<BatchDetail>(`/api/imports/${b.id}`))}>Aç</button>
                    {b.status === 'Applied' ? <button className="btn small ghost" type="button" onClick={async () => {
                      if (!window.confirm('Bu aktarım geri alınsın mı? Oluşan cari, sporcu ve ürünler silinmez, kullanım dışı bırakılır; onay bekleyen açılış bakiyeleri reddedilir.')) return
                      try { await post(`/api/imports/${b.id}/revert`); toast('Aktarım geri alındı.'); history.reload() } catch (e) { setError(errorText(e)) }
                    }}>Geri Al</button> : null}
                  </div>
                </td>
              </tr>
            ))}</tbody>
          </table>
        )}
      </div>
    </>
  )
}

function BatchView({ detail, busy, onApply }: { detail: BatchDetail; busy: boolean; onApply: () => void }) {
  const [filter, setFilter] = useState<'all' | 'error' | 'warning'>('all')
  const { batch, rows, fields } = detail
  const visible = rows.filter((r) => filter === 'all' || (filter === 'error' ? !r.isValid : !!r.warnings))
  return (
    <div className="panel">
      <div className="panel-head">
        <div>
          <h2>{batch.fileName} · {batch.sheetName}</h2>
          <p className="muted small">{batch.rowsRead} satır okundu · {batch.rowsValid} uygun · {batch.rowsError} hatalı</p>
        </div>
        <div className="actions">
          <select className="input" style={{ width: 180 }} aria-label="Satırları süz" value={filter} onChange={(e) => setFilter(e.target.value as typeof filter)}>
            <option value="all">Tüm satırlar</option><option value="error">Yalnız hatalılar</option><option value="warning">Uyarılı olanlar</option>
          </select>
          {batch.status === 'Previewed' ? (
            <button className="btn primary" type="button" onClick={onApply} disabled={busy || batch.rowsValid === 0}>
              {batch.rowsValid} Uygun Satırı Aktar
            </button>
          ) : <StatusBadge status={batch.status} />}
        </div>
      </div>
      {batch.status === 'Previewed' && batch.rowsError > 0 ? (
        <div className="alert warn" style={{ margin: 12 }}>Hatalı {batch.rowsError} satır aktarılmaz. Excel'de düzeltip dosyayı yeniden yükleyebilirsiniz.</div>
      ) : null}
      <div className="table-wrap">
        <table>
          <thead><tr><th>Satır</th>{fields.map((f) => <th key={f.key}>{f.label}</th>)}<th>Kontrol</th></tr></thead>
          <tbody>{visible.map((r) => (
            <tr key={r.rowNumber}>
              <td>{r.rowNumber}</td>
              {fields.map((f) => <td key={f.key} className="small">{r.data[f.key] ?? <span className="muted">—</span>}</td>)}
              <td className="small">
                {r.isValid ? <span className="badge done">Uygun</span> : <span className="badge danger">Hatalı</span>}
                {r.errors ? <span className="sub" style={{ color: 'var(--danger)' }}>{r.errors}</span> : null}
                {r.warnings ? <span className="sub">{r.warnings}</span> : null}
              </td>
            </tr>
          ))}</tbody>
        </table>
      </div>
    </div>
  )
}
