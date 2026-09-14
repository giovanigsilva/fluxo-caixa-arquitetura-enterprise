import React, { FormEvent, useEffect, useRef, useState } from "react"
import { createRoot } from "react-dom/client"
import { QueryClient, QueryClientProvider, useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts"
import { Activity, Banknote, Building2, CircleDollarSign, FileText, Gauge, LayoutDashboard, LogIn, Plus, RefreshCw, ShieldCheck, Users } from "lucide-react"
import "./styles.css"

type Account = { id: string; name: string; currency: string }
type Customer = { id: string; legalName: string; tradeName?: string; version: number }
type EntryType = "Credit" | "Debit"
type Entry = { id: string; accountId: string; type: EntryType; amount: string; businessDate: string; description: string; reversalEntryId?: string }
type EntryFormPayload = { accountId: string; type: EntryType; amount: string; businessDate: string; description: string; customerId: string | null; categoryId: null; costCenterId: null }
type DailyResponse = { lag: { outboxPending: number }, rows: Array<{ businessDate: string; credits: number; debits: number; dayMovement: number; entryCount: number }> }
type ScenarioSample = { timestamp: string; scenarioId: string; metrics: { readRps: number; writeRps: number; p50Ms: number; p95Ms: number; p99Ms: number; outboxPending: number; rabbitReady: number; projectedEntries: number; errorBudgetRemaining: number | null } }
type RecaptchaConfigResponse = { provider: "google-recaptcha-v2-checkbox"; enabled: boolean; siteKey?: string }
type LoginSessionResponse = { status: "approved"; pendingSessionId: string; userId: string; displayName: string; expiresAt: string }
type LoginSession = { pendingSessionId: string; userId: string; displayName: string; expiresAt: string }

declare global {
  interface Window {
    grecaptcha?: {
      ready(callback: () => void): void
      render(container: HTMLElement, parameters: { sitekey: string }): number
      getResponse(widgetId?: number): string
      reset(widgetId?: number): void
    }
  }
}

const queryClient = new QueryClient()
const brl = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" })
const tenantHeaders = { "X-Tenant-Id": "org-alpha", "X-User-Id": "user-admin-alpha" }
const environmentName = window.location.hostname === "vertx.dwilon.com" ? "Produção" : "UAT"
const sessionStorageKey = "vertx.cashflow.session"
const recaptchaScriptUrl = "https://www.google.com/recaptcha/api.js?render=explicit&hl=pt-BR"
let recaptchaScriptPromise: Promise<void> | null = null

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
  const [recaptchaConfig, setRecaptchaConfig] = useState<RecaptchaConfigResponse | null>(null)
  const [captchaReady, setCaptchaReady] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const captchaNode = useRef<HTMLDivElement | null>(null)
  const captchaWidgetId = useRef<number | null>(null)

  useEffect(() => {
    let active = true
    request<RecaptchaConfigResponse>("/bff/login/recaptcha/config")
      .then(config => {
        if (active) {
          setRecaptchaConfig(config)
        }
      })
      .catch(() => {
        if (active) {
          setError("reCAPTCHA não configurado.")
        }
      })

    return () => {
      active = false
    }
  }, [])

  useEffect(() => {
    let active = true
    if (!recaptchaConfig?.enabled || !recaptchaConfig.siteKey || !captchaNode.current || captchaWidgetId.current !== null) {
      return () => {
        active = false
      }
    }

    loadRecaptchaScript()
      .then(() => {
        window.grecaptcha?.ready(() => {
          if (active && captchaNode.current && recaptchaConfig.siteKey && captchaWidgetId.current === null) {
            captchaWidgetId.current = window.grecaptcha?.render(captchaNode.current, { sitekey: recaptchaConfig.siteKey }) ?? null
            setCaptchaReady(captchaWidgetId.current !== null)
          }
        })
      })
      .catch(() => {
        if (active) {
          setError("Não foi possível carregar o reCAPTCHA.")
        }
      })

    return () => {
      active = false
    }
  }, [recaptchaConfig])

  async function startLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)

    const recaptchaToken = window.grecaptcha?.getResponse(captchaWidgetId.current ?? undefined) ?? ""
    if (!recaptchaToken) {
      setError("Confirme o reCAPTCHA.")
      return
    }

    try {
      const approved = await request<LoginSessionResponse>("/bff/login/start", {
        method: "POST",
        body: JSON.stringify({ email, password, recaptchaToken })
      })
      onAuthenticated({
        pendingSessionId: approved.pendingSessionId,
        userId: approved.userId,
        displayName: approved.displayName,
        expiresAt: approved.expiresAt
      })
    } catch (failure) {
      window.grecaptcha?.reset(captchaWidgetId.current ?? undefined)
      setError(readError(failure))
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

        <form className="login-form" onSubmit={startLogin}>
          <label className="field">
            <span>Login</span>
            <input value={email} onChange={event => setEmail(event.target.value)} type="email" autoComplete="username" required />
          </label>
          <label className="field">
            <span>Senha</span>
            <input value={password} onChange={event => setPassword(event.target.value)} type="password" autoComplete="current-password" required />
          </label>
          <div className="recaptcha-box" ref={captchaNode} />
          <button type="submit" disabled={!recaptchaConfig?.enabled || !captchaReady}>
            <LogIn size={18} /> Entrar
          </button>
        </form>

        {error && <p className="error">{error}</p>}
      </section>
    </main>
  )
}

function readError(failure: unknown) {
  if (failure instanceof Error) {
    try {
      const parsed = JSON.parse(failure.message) as { detail?: string; title?: string }
      return parsed.detail ?? parsed.title ?? "Operação recusada."
    } catch {
      return failure.message || "Operação recusada."
    }
  }

  return "Operação recusada."
}

function loadRecaptchaScript() {
  if (window.grecaptcha) {
    return Promise.resolve()
  }

  recaptchaScriptPromise ??= new Promise<void>((resolve, reject) => {
    const existing = document.querySelector<HTMLScriptElement>(`script[src="${recaptchaScriptUrl}"]`)
    if (existing) {
      existing.addEventListener("load", () => resolve(), { once: true })
      existing.addEventListener("error", () => reject(new Error("recaptcha.load_failed")), { once: true })
      return
    }

    const script = document.createElement("script")
    script.src = recaptchaScriptUrl
    script.async = true
    script.defer = true
    script.addEventListener("load", () => resolve(), { once: true })
    script.addEventListener("error", () => reject(new Error("recaptcha.load_failed")), { once: true })
    document.head.appendChild(script)
  })

  return recaptchaScriptPromise
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
  const [entryType, setEntryType] = useState<EntryType>("Credit")
  const [accountId, setAccountId] = useState("")
  const [customerId, setCustomerId] = useState("")
  const [amount, setAmount] = useState("")
  const [businessDate, setBusinessDate] = useState(() => new Date().toISOString().slice(0, 10))
  const [description, setDescription] = useState("")
  const [formError, setFormError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  useEffect(() => {
    if (!accountId && accounts.length) {
      setAccountId(accounts[0].id)
    }
  }, [accountId, accounts])

  const mutation = useMutation({
    mutationFn: (payload: EntryFormPayload) => request<Entry>("/api/entries/entries", {
      method: "POST",
      headers: { "Idempotency-Key": crypto.randomUUID() },
      body: JSON.stringify(payload)
    }),
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: ["entries"] })
      await client.invalidateQueries({ queryKey: ["daily"] })
      setAmount("")
      setDescription("")
      setFormError(null)
      setSuccess("Lançamento registrado.")
    }
  })

  function submitEntry(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSuccess(null)
    setFormError(null)

    const normalizedAmount = normalizeAmount(amount)
    if (!accountId) {
      setFormError("Selecione uma conta.")
      return
    }

    if (!normalizedAmount) {
      setFormError("Informe um valor positivo com até duas casas decimais.")
      return
    }

    if (!businessDate) {
      setFormError("Informe a data do lançamento.")
      return
    }

    const cleanDescription = description.trim()
    if (!cleanDescription) {
      setFormError("Informe a descrição do lançamento.")
      return
    }

    mutation.mutate({
      accountId,
      type: entryType,
      amount: normalizedAmount,
      businessDate,
      description: cleanDescription,
      customerId: customerId || null,
      categoryId: null,
      costCenterId: null
    })
  }

  return (
    <div className="panel">
      <div className="panel-title">Novo lançamento</div>
      <form className="entry-form" onSubmit={submitEntry}>
        <div className="segmented" role="radiogroup" aria-label="Tipo de lançamento">
          <button aria-pressed={entryType === "Credit"} className={entryType === "Credit" ? "segment-button active" : "segment-button"} type="button" onClick={() => setEntryType("Credit")}>
            <Banknote size={18} /> Crédito
          </button>
          <button aria-pressed={entryType === "Debit"} className={entryType === "Debit" ? "segment-button active" : "segment-button"} type="button" onClick={() => setEntryType("Debit")}>
            <RefreshCw size={18} /> Débito
          </button>
        </div>

        <div className="form-grid">
          <label className="field full">
            <span>Conta</span>
            <select value={accountId} onChange={event => setAccountId(event.target.value)} required>
              {accounts.map(account => (
                <option key={account.id} value={account.id}>{account.name} · {account.currency}</option>
              ))}
            </select>
          </label>

          <label className="field">
            <span>Valor</span>
            <input value={amount} onChange={event => setAmount(event.target.value)} inputMode="decimal" placeholder="150,00" required />
          </label>

          <label className="field">
            <span>Data</span>
            <input value={businessDate} onChange={event => setBusinessDate(event.target.value)} type="date" required />
          </label>

          <label className="field full">
            <span>Descrição</span>
            <input value={description} onChange={event => setDescription(event.target.value)} placeholder="Ex.: pagamento de fornecedor" required />
          </label>

          <label className="field full">
            <span>Cliente</span>
            <select value={customerId} onChange={event => setCustomerId(event.target.value)}>
              <option value="">Sem cliente vinculado</option>
              {customers.map(customer => (
                <option key={customer.id} value={customer.id}>{customer.tradeName || customer.legalName}</option>
              ))}
            </select>
          </label>
        </div>

        <button type="submit" disabled={!accounts.length || mutation.isPending}>
          <Plus size={18} /> {mutation.isPending ? "Registrando" : "Registrar lançamento"}
        </button>
      </form>
      {formError && <p className="error">{formError}</p>}
      {mutation.isError && <p className="error">{readError(mutation.error)}</p>}
      {success && <p className="success">{success}</p>}
    </div>
  )
}

function normalizeAmount(raw: string): string | null {
  const compact = raw.trim().replace(/\s/g, "")
  if (!compact) {
    return null
  }

  const normalized = compact.includes(",") ? compact.replace(/\./g, "").replace(",", ".") : compact
  if (!/^\d+(\.\d{1,2})?$/.test(normalized)) {
    return null
  }

  const value = Number(normalized)
  return Number.isFinite(value) && value > 0 ? value.toFixed(2) : null
}

createRoot(document.getElementById("root")!).render(<App />)
