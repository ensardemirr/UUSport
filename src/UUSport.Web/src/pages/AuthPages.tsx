import { useEffect, useState, type FormEvent } from 'react'
import QRCode from 'qrcode'
import { api, post } from '../api'
import { useAuth } from '../auth'
import { ErrorBox, Field, errorText } from '../components'

function Brand() {
  return (
    <div className="brand">
      <div className="brand-mark" aria-hidden="true">UU</div>
      <div>
        <div className="brand-name">UU Sport</div>
        <div className="muted small">Akademi yönetimi ve ön muhasebe</div>
      </div>
    </div>
  )
}

export function SetupPage() {
  const { refresh } = useAuth()
  const [form, setForm] = useState({ organizationName: '', fullName: '', userName: '', password: '', confirm: '', token: '' })
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const set = (k: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) => setForm({ ...form, [k]: e.target.value })

  async function submit(e: FormEvent) {
    e.preventDefault()
    if (form.password !== form.confirm) { setError('Parolalar aynı değil.'); return }
    setBusy(true)
    setError(null)
    try {
      await post('/api/setup', form)
      await post('/api/auth/login', { userName: form.userName, password: form.password })
      await refresh()
    } catch (err) { setError(errorText(err)) } finally { setBusy(false) }
  }

  return (
    <main className="auth-wrap">
      <form className="auth-card" onSubmit={submit}>
        <Brand />
        <div>
          <h1>İlk kurulum</h1>
          <p className="muted" style={{ marginTop: 6 }}>Kurum bilgisini ve ilk yönetici hesabını oluşturun. Varsayılan parola yoktur.</p>
        </div>
        <ErrorBox message={error} />
        <Field label="Kurum adı"><input className="input" value={form.organizationName} onChange={set('organizationName')} required /></Field>
        <Field label="Ad soyad"><input className="input" value={form.fullName} onChange={set('fullName')} required autoComplete="name" /></Field>
        <Field label="Kullanıcı adı" hint="küçük harf, rakam, . _ -">
          <input className="input" value={form.userName} onChange={set('userName')} required autoComplete="username" pattern="[a-z0-9._\-]+" />
        </Field>
        <Field label="Parola" hint="en az 12 karakter, en az bir rakam">
          <input className="input" type="password" value={form.password} onChange={set('password')} required minLength={12} autoComplete="new-password" />
        </Field>
        <Field label="Parola (tekrar)"><input className="input" type="password" value={form.confirm} onChange={set('confirm')} required autoComplete="new-password" /></Field>
        <Field label="Kurulum jetonu" hint="Sunucu makinesinde bir kez oluşur; sunucu günlüğünde görünür">
          <input className="input" value={form.token} onChange={set('token')} required autoComplete="off" />
        </Field>
        <button className="btn primary" type="submit" disabled={busy}>Kurulumu Tamamla</button>
      </form>
    </main>
  )
}

export function LoginPage() {
  const { refresh } = useAuth()
  const [step, setStep] = useState<'password' | 'code' | 'recovery'>('password')
  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [code, setCode] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      if (step === 'password') {
        const r = await post<{ status: string }>('/api/auth/login', { userName, password })
        if (r.status === 'mfa_required') { setStep('code'); return }
        await refresh()
      } else if (step === 'code') {
        await post('/api/auth/login/mfa', { code })
        await refresh()
      } else {
        await post('/api/auth/login/mfa/recovery', { code })
        await refresh()
      }
    } catch (err) { setError(errorText(err)) } finally { setBusy(false) }
  }

  return (
    <main className="auth-wrap">
      <form className="auth-card" onSubmit={submit}>
        <Brand />
        <h1>{step === 'password' ? 'Giriş' : step === 'code' ? 'Doğrulama kodu' : 'Kurtarma kodu'}</h1>
        <ErrorBox message={error} />
        {step === 'password' ? (
          <>
            <Field label="Kullanıcı adı"><input className="input" value={userName} onChange={(e) => setUserName(e.target.value)} required autoComplete="username" autoFocus /></Field>
            <Field label="Parola"><input className="input" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required autoComplete="current-password" /></Field>
            <button className="btn primary" type="submit" disabled={busy}>Giriş Yap</button>
          </>
        ) : step === 'code' ? (
          <>
            <p className="muted">Telefonunuzdaki doğrulayıcı uygulamada görünen 6 haneli kodu girin.</p>
            <Field label="Kod"><input className="input" value={code} onChange={(e) => setCode(e.target.value)} inputMode="numeric" autoComplete="one-time-code" required autoFocus maxLength={8} /></Field>
            <button className="btn primary" type="submit" disabled={busy}>Doğrula</button>
            <button className="btn ghost" type="button" onClick={() => { setStep('recovery'); setCode(''); setError(null) }}>Telefonum yanımda değil, kurtarma kodu kullanacağım</button>
            <button className="btn ghost" type="button" onClick={() => { setStep('password'); setCode(''); setError(null) }}>Geri dön</button>
          </>
        ) : (
          <>
            <p className="muted">Kurulumda size verilen kurtarma kodlarından birini girin. Her kod yalnız bir kez kullanılır.</p>
            <Field label="Kurtarma kodu"><input className="input" value={code} onChange={(e) => setCode(e.target.value)} autoComplete="one-time-code" required autoFocus /></Field>
            <button className="btn primary" type="submit" disabled={busy}>Doğrula</button>
            <button className="btn ghost" type="button" onClick={() => { setStep('code'); setCode(''); setError(null) }}>Geri dön</button>
          </>
        )}
      </form>
    </main>
  )
}

export function MfaSetupPage() {
  const { refresh, me } = useAuth()
  const [setup, setSetup] = useState<{ sharedKey: string; authenticatorUri: string } | null>(null)
  const [qr, setQr] = useState<string | null>(null)
  const [code, setCode] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [recoveryCodes, setRecoveryCodes] = useState<string[] | null>(null)

  useEffect(() => {
    api<{ sharedKey: string; authenticatorUri: string }>('/api/auth/mfa/setup')
      .then(async (s) => { setSetup(s); setQr(await QRCode.toDataURL(s.authenticatorUri, { margin: 1, width: 200 })) })
      .catch((e) => setError(errorText(e)))
  }, [])

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      const r = await post<{ recoveryCodes: string[] }>('/api/auth/mfa/enable', { code })
      setRecoveryCodes(r.recoveryCodes)
    } catch (err) { setError(errorText(err)) }
  }

  if (recoveryCodes) {
    return (
      <main className="auth-wrap">
        <div className="auth-card">
          <Brand />
          <div>
            <h1>Kurtarma kodlarınız</h1>
            <p className="muted" style={{ marginTop: 6 }}>
              Telefonunuzu kaybederseniz bu kodlardan biriyle giriş yapabilirsiniz. Her kod yalnız bir kez
              kullanılır. Şimdi not edin veya yazdırın; bu ekran bir daha gösterilmeyecek.
            </p>
          </div>
          <div className="code" style={{ lineHeight: 1.8 }}>
            {recoveryCodes.map((c) => <div key={c}>{c}</div>)}
          </div>
          <button className="btn primary" type="button" onClick={refresh}>Kaydettim, devam et</button>
        </div>
      </main>
    )
  }

  return (
    <main className="auth-wrap">
      <form className="auth-card" onSubmit={submit}>
        <Brand />
        <div>
          <h1>İki adımlı doğrulama</h1>
          <p className="muted" style={{ marginTop: 6 }}>
            Muhasebe verisine erişim için iki adımlı doğrulama zorunludur. Telefonunuza Microsoft Authenticator,
            Google Authenticator veya benzeri bir uygulama kurun ve aşağıdaki kodu okutun.
          </p>
        </div>
        <ErrorBox message={error} />
        {qr ? <div className="qr"><img src={qr} alt="Doğrulayıcı uygulama için karekod" width={200} height={200} /></div> : null}
        {setup ? (
          <div>
            <div className="small muted">Karekod okutulamıyorsa bu anahtarı elle girin:</div>
            <div className="code">{setup.sharedKey}</div>
            <div className="small muted" style={{ marginTop: 4 }}>Hesap: {me?.userName}</div>
          </div>
        ) : null}
        <Field label="Uygulamadaki 6 haneli kod">
          <input className="input" value={code} onChange={(e) => setCode(e.target.value)} inputMode="numeric" autoComplete="one-time-code" required maxLength={8} />
        </Field>
        <button className="btn primary" type="submit">Doğrulamayı Aç</button>
        <button className="btn ghost" type="button" onClick={async () => { await post('/api/auth/logout'); await refresh() }}>Çıkış yap</button>
      </form>
    </main>
  )
}
