import React, { FormEvent, useEffect, useState } from "react"
import { createRoot } from "react-dom/client"
import { QueryClient, QueryClientProvider, useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import QRCode from "qrcode"
import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts"
import { Activity, Banknote, Building2, CircleDollarSign, Download, FileText, Gauge, KeyRound, LayoutDashboard, LogIn, QrCode, RefreshCw, ShieldCheck, Smartphone, Users } from "lucide-react"
import "./styles.css"

type Account = { id: string; name: string; currency: string }
type Customer = { id: string; legalName: string; tradeName?: string; version: number }
type Entry = { id: string; accountId: string; type: "Credit" | "Debit"; amount: string; businessDate: string; description: string; reversalEntryId?: string }
type DailyResponse = { lag: { outboxPending: number }, rows: Array<{ businessDate: string; credits: number; debits: number; dayMovement: number; entryCount: number }> }
type ScenarioSample = { timestamp: string; scenarioId: string; metrics: { readRps: number; writeRps: number; p50Ms: number; p95Ms: number; p99Ms: number; outboxPending: number; rabbitReady: number; projectedEntries: number; errorBudgetRemaining: number | null } }
type LoginStartResponse = { challengeId: string; pendingSessionId: string; matchCode: string; approvalUrl: string; totpUri: string; totpIssuer: string; displayName: string; expiresAt: string }
type LoginApproveResponse = { status: "approved"; pendingSessionId: string; userId: string; displayName: string; expiresAt: string }
type LoginSession = { pendingSessionId: string; userId: string; displayName: string; expiresAt: string }

const queryClient = new QueryClient()
const brl = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" })
const tenantHeaders = { "X-Tenant-Id": "org-alpha", "X-User-Id": "user-admin-alpha" }
const environmentName = window.location.hostname === "vertx.dwilon.com" ? "Produção" : "UAT"
const sessionStorageKey = "vertx.cashflow.session"

function request<T>(path: string, init?: RequestInit): Promise<T> {
  return fetch(path, {
    ...init,
    headers: {
      ...tenantHeaders,
      "Content-Type": "application/json",
      ...(init?.headers ?? {})
    }
  }).then(async response => {
    if (!response.ok) {
      throw new Error(await response.text())
    }
    return response.json() as Promise<T>
  })
}

function App() {
  const [session, setSession] = useState<LoginSession | null>(() => readStoredSession())

  function authenticate(nextSession: LoginSession) {
    window.sessionStorage.setItem(sessionStorageKey, JSON.stringify(nextSession))
    setSession(nextSession)
  }

  function logout() {
    window.sessionStorage.removeItem(sessionStorageKey)
    queryClient.clear()
    setSession(null)
  }

  return (
    <QueryClientProvider client={queryClient}>
      {session ? <Shell session={session} onLogout={logout} /> : <LoginScreen onAuthenticated={authenticate} />}
    </QueryClientProvider>
  )
}

function readStoredSession(): LoginSession | null {
  try {
    const raw = window.sessionStorage.getItem(sessionStorageKey)
    if (!raw) {
      return null
    }

    const parsed = JSON.parse(raw) as LoginSession
    return new Date(parsed.expiresAt).getTime() > Date.now() ? parsed : null
  } catch {
    window.sessionStorage.removeItem(sessionStorageKey)
    return null
  }
}

function LoginScreen({ onAuthenticated }: { onAuthenticated: (session: LoginSession) => void }) {
  const [email, setEmail] = useState("")
  const [password, setPassword] = useState("")
  const [totpCode, setTotpCode] = useState("")
  const [challenge, setChallenge] = useState<LoginStartResponse | null>(null)
  const [qrDataUrl, setQrDataUrl] = useState("")
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!challenge) {
      setQrDataUrl("")
      return
    }

    let active = true
    QRCode.toDataURL(challenge.totpUri, {
      errorCorrectionLevel: "M",
      margin: 2,
      width: 236,
      color: { dark: "#0f172a", light: "#ffffff" }
    })
      .then(dataUrl => {
        if (active) {
          setQrDataUrl(dataUrl)
        }
      })
      .catch(() => {
        if (active) {
          setError("Não foi possível gerar o QR Code.")
        }
      })

    return () => {
      active = false
    }
  }, [challenge])

  async function startLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setTotpCode("")

    try {
      const nextChallenge = await request<LoginStartResponse>("/bff/login/start", {
        method: "POST",
        body: JSON.stringify({ email, password })
      })
      setChallenge(nextChallenge)
    } catch (failure) {
      setChallenge(null)
      setError(readError(failure))
    }
  }

  async function approveLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!challenge) {
      return
    }

    setError(null)
    try {
      const approved = await request<LoginApproveResponse>("/bff/login/approve", {
        method: "POST",
        body: JSON.stringify({ challengeId: challenge.challengeId, totpCode })
      })
      onAuthenticated({
        pendingSessionId: approved.pendingSessionId,
        userId: approved.userId,
        displayName: approved.displayName,
        expiresAt: approved.expiresAt
      })
    } catch (failure) {
      setError(readError(failure))
      setChallenge(null)
      setTotpCode("")
    }
  }

  return (
    <main className="login-shell">
      <section className="login-panel" aria-labelledby="login-title">
        <div className="login-brand">
          <CircleDollarSign size={28} />
          <div>
            <span>Fluxo de Caixa</span>
            <strong>{environmentName}</strong>
          </div>
        </div>

        <div className="login-copy">
          <span className="badge"><ShieldCheck size={16} /> Entrada protegida</span>
          <h1 id="login-title">Acesso operacional</h1>
        </div>

        {!challenge ? (
          <form className="login-form" onSubmit={startLogin}>
            <label className="field">
              <span>Login</span>
              <input value={email} onChange={event => setEmail(event.target.value)} type="email" autoComplete="username" required />
            </label>
            <label className="field">
              <span>Senha</span>
              <input value={password} onChange={event => setPassword(event.target.value)} type="password" autoComplete="current-password" required />
            </label>
            <button type="submit">
              <LogIn size={18} /> Entrar
            </button>
          </form>
        ) : (
          <form className="login-form" onSubmit={approveLogin}>
            <div className="qr-zone">
              <div className="qr-frame">
                {qrDataUrl ? <img src={qrDataUrl} alt="QR Code TOTP" /> : <QrCode size={64} />}
              </div>
              <div className="qr-meta">
                <span><Smartphone size={16} /> {challenge.totpIssuer}</span>
                <strong>{challenge.displayName}</strong>
              </div>
            </div>
            <label className="field">
              <span>Código do Google Authenticator</span>
              <input value={totpCode} onChange={event => setTotpCode(event.target.value.replace(/\D/g, "").slice(0, 6))} inputMode="numeric" pattern="[0-9]{6}" autoComplete="one-time-code" required />
            </label>
            <button type="submit" disabled={totpCode.length !== 6}>
              <KeyRound size={18} /> Validar QR
            </button>
            <button className="text-button" type="button" onClick={() => setChallenge(null)}>
              Trocar login
            </button>
          </form>
        )}

        {error && <p className="error">{error}</p>}
      </section>
    </main>
  )
}

function readError(failure: unknown) {
  if (failure instanceof Error) {
    return failure.message || "Operação recusada."
  }

  return "Operação recusada."
}

function Shell({ session, onLogout }: { session: LoginSession; onLogout: () => void }) {
  const accounts = useQuery({ queryKey: ["accounts"], queryFn: () => request<Account[]>("/api/entries/accounts") })
  const customers = useQuery({ queryKey: ["customers"], queryFn: () => request<Customer[]>("/api/entries/customers") })
  const entries = useQuery({ queryKey: ["entries"], queryFn: () => request<Entry[]>("/api/entries/entries") })
  const daily = useQuery({ queryKey: ["daily"], queryFn: () => request<DailyResponse>("/api/consolidated/daily") })
  const sample = useQuery({ queryKey: ["scenario", "NORMAL"], queryFn: () => request<ScenarioSample>("/api/observability/samples?scenario=NORMAL"), refetchInterval: 3000 })

  const credits = entries.data?.filter(item => item.type === "Credit").reduce((sum, item) => sum + Number(item.amount), 0) ?? 0
  const debits = entries.data?.filter(item => item.type === "Debit").reduce((sum, item) => sum + Number(item.amount), 0) ?? 0

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand"><CircleDollarSign size={24} /> Fluxo de Caixa</div>
        <nav>
          <a href="#dashboard"><LayoutDashboard size={18} /> Visão geral</a>
          <a href="#entries"><Banknote size={18} /> Lançamentos</a>
          <a href="#customers"><Building2 size={18} /> Clientes</a>
          <a href="#users"><Users size={18} /> Usuários e perfis</a>
          <a href="#monitor"><Gauge size={18} /> Monitoramento</a>
        </nav>
      </aside>
      <main>
        <header className="topbar">
          <div>
            <span className="crumb">{environmentName} / Organização Alfa</span>
            <h1>Operação financeira</h1>
          </div>
          <div className="topbar-actions">
            <span className="badge"><ShieldCheck size={16} /> {session.displayName}</span>
            <button className="ghost-button" type="button" onClick={onLogout}>Sair</button>
          </div>
        </header>

        <section id="dashboard" className="metrics">
          <Metric title="Créditos" value={brl.format(credits)} icon={<Banknote />} />
          <Metric title="Débitos" value={brl.format(debits)} icon={<RefreshCw />} />
          <Metric title="Saldo projetado" value={brl.format(credits - debits)} icon={<Activity />} />
          <Metric title="Outbox pendente" value={String(daily.data?.lag.outboxPending ?? 0)} icon={<FileText />} />
        </section>

        <section className="workspace">
          <EntryPanel accounts={accounts.data ?? []} customers={customers.data ?? []} />
          <div className="panel">
            <div className="panel-title">Consolidado diário</div>
            <ResponsiveContainer width="100%" height={260}>
              <AreaChart data={daily.data?.rows ?? []}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="businessDate" />
                <YAxis />
                <Tooltip />
                <Area type="monotone" dataKey="credits" stroke="#0891b2" fill="#a5f3fc" />
                <Area type="monotone" dataKey="debits" stroke="#be123c" fill="#fecdd3" />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </section>

        <section id="entries" className="panel">
          <div className="panel-title">Lançamentos</div>
          <table>
            <thead><tr><th>Data</th><th>Descrição</th><th>Tipo</th><th className="money">Valor</th></tr></thead>
            <tbody>
              {(entries.data ?? []).map(entry => (
                <tr key={entry.id}>
                  <td>{entry.businessDate}</td>
                  <td>{entry.description}</td>
                  <td><span className={entry.type === "Credit" ? "pill credit" : "pill debit"}>{entry.type === "Credit" ? "Crédito" : "Débito"}</span></td>
                  <td className="money">{brl.format(Number(entry.amount))}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>

        <section id="customers" className="panel">
          <div className="panel-title">Clientes</div>
          <div className="grid-list">
            {(customers.data ?? []).map(customer => <div className="row-card" key={customer.id}>{customer.legalName}<span>v{customer.version}</span></div>)}
          </div>
        </section>

        <section id="monitor" className="panel">
          <div className="panel-title">Monitoramento <span className="badge">Telemetria sintética</span></div>
          <div className="monitor-grid">
            <Metric title="RPS leitura" value={String(sample.data?.metrics.readRps ?? 0)} icon={<Gauge />} />
            <Metric title="p95" value={`${sample.data?.metrics.p95Ms ?? 0} ms`} icon={<Activity />} />
            <Metric title="Rabbit ready" value={String(sample.data?.metrics.rabbitReady ?? 0)} icon={<FileText />} />
            <Metric title="Error budget" value={sample.data?.metrics.errorBudgetRemaining == null ? "sem dados" : `${Math.round(sample.data.metrics.errorBudgetRemaining * 100)}%`} icon={<ShieldCheck />} />
          </div>
        </section>
      </main>
    </div>
  )
}

function Metric({ title, value, icon }: { title: string; value: string; icon: React.ReactNode }) {
  return <div className="metric"><span>{icon}</span><small>{title}</small><strong>{value}</strong></div>
}

function EntryPanel({ accounts, customers }: { accounts: Account[]; customers: Customer[] }) {
  const client = useQueryClient()
  const mutation = useMutation({
    mutationFn: () => request<Entry>("/api/entries/entries", {
      method: "POST",
      headers: { "Idempotency-Key": crypto.randomUUID() },
      body: JSON.stringify({
        accountId: accounts[0]?.id,
        type: "Credit",
        amount: "150.00",
        businessDate: new Date().toISOString().slice(0, 10),
        description: `Recebimento operacional ${environmentName}`,
        customerId: customers[0]?.id,
        categoryId: null,
        costCenterId: null
      })
    }),
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: ["entries"] })
      await client.invalidateQueries({ queryKey: ["daily"] })
    }
  })

  return (
    <div className="panel">
      <div className="panel-title">Novo lançamento rápido</div>
      <p>Cria um crédito real no backend {environmentName} com idempotência e outbox.</p>
      <button type="button" onClick={() => mutation.mutate()} disabled={!accounts.length || mutation.isPending}>
        <Download size={18} /> Registrar crédito BRL 150,00
      </button>
      {mutation.isError && <p className="error">Falha ao registrar lançamento.</p>}
    </div>
  )
}

createRoot(document.getElementById("root")!).render(<App />)
