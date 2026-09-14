import React, { FormEvent, useEffect, useRef, useState } from "react"
import { createRoot } from "react-dom/client"
import { QueryClient, QueryClientProvider, useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Area, AreaChart, Bar, BarChart, CartesianGrid, Legend, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts"
import { Activity, AlertTriangle, Banknote, Bell, Bot, Building2, CheckCircle2, CircleDollarSign, Database, FileText, Gauge, LayoutDashboard, LineChart as LineChartIcon, LogIn, MessageCircle, Mic, Phone, Play, Plus, RefreshCw, Send, Server, ShieldCheck, Users, X, Zap } from "lucide-react"
import "./styles.css"

type Account = { id: string; name: string; currency: string }
type Customer = { id: string; legalName: string; tradeName?: string; version: number }
type EntryType = "Credit" | "Debit"
type Entry = { id: string; accountId: string; type: EntryType; amount: string; businessDate: string; description: string; reversalEntryId?: string }
type EntryFormPayload = { accountId: string; type: EntryType; amount: string; businessDate: string; description: string; customerId: string | null; categoryId: null; costCenterId: null }
type DailyResponse = { lag: { outboxPending: number }, rows: Array<{ businessDate: string; credits: number; debits: number; dayMovement: number; entryCount: number }> }
type ScenarioMetrics = {
  readRps: number
  writeRps: number
  p50Ms: number
  p95Ms: number
  p99Ms: number
  errors4xx: number
  errors5xx: number
  errors429: number
  outboxPending: number
  rabbitReady: number
  projectedEntries: number
  duplicatesIgnored: number
  errorBudgetRemaining: number | null
  unit: string
}
type ScenarioSample = { timestamp: string; scenarioId: string; health: Record<string, string>; metrics: ScenarioMetrics }
type RecaptchaConfigResponse = { provider: "google-recaptcha-v2-checkbox"; enabled: boolean; siteKey?: string }
type LoginSessionResponse = { status: "approved"; pendingSessionId: string; userId: string; displayName: string; expiresAt: string }
type LoginSession = { pendingSessionId: string; userId: string; displayName: string; expiresAt: string }
type AlertMetricKey = "p95Ms" | "errors5xx" | "errors429" | "outboxPending" | "dbRps" | "errorBudgetRemaining"
type AlertRule = { id: string; label: string; metric: AlertMetricKey; threshold: number; unit: string; compare: "above" | "below"; enabled: boolean; severity: "warning" | "critical" }
type AlertEvaluation = AlertRule & { active: boolean; value: number; displayValue: string }
type AgentMode = "chat" | "local" | "call"
type AgentMessage = { id: string; role: "agent" | "user"; text: string }

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
const integer = new Intl.NumberFormat("pt-BR")
const percent = new Intl.NumberFormat("pt-BR", { style: "percent", maximumFractionDigits: 0 })
const tenantHeaders = { "X-Tenant-Id": "org-alpha", "X-User-Id": "user-admin-alpha" }
const environmentName = window.location.hostname === "vertx.dwilon.com" ? "Produção" : "UAT"
const sessionStorageKey = "vertx.cashflow.session"
const recaptchaScriptUrl = "https://www.google.com/recaptcha/api.js?render=explicit&hl=pt-BR"
let recaptchaScriptPromise: Promise<void> | null = null

const defaultScenarios = ["NORMAL", "LOAD_50", "LOAD_100", "SPIKE_200", "WORKER_DOWN", "BROKER_DOWN", "READ_DB_DOWN", "REDIS_DOWN", "DUPLICATE_EVENT", "RECOVERY"]
const scenarioLabels: Record<string, string> = {
  NORMAL: "Normal",
  LOAD_50: "Carga 50",
  LOAD_100: "Carga 100",
  SPIKE_200: "Pico 200",
  WORKER_DOWN: "Worker down",
  BROKER_DOWN: "Broker down",
  READ_DB_DOWN: "Banco leitura down",
  REDIS_DOWN: "Redis down",
  DUPLICATE_EVENT: "Evento duplicado",
  RECOVERY: "Recuperação"
}
const defaultAlertRules: AlertRule[] = [
  { id: "latency", label: "Latência p95", metric: "p95Ms", threshold: 220, unit: "ms", compare: "above", enabled: true, severity: "warning" },
  { id: "outbox", label: "Outbox pendente", metric: "outboxPending", threshold: 25, unit: "eventos", compare: "above", enabled: true, severity: "warning" },
  { id: "db-rps", label: "Banco req/s", metric: "dbRps", threshold: 120, unit: "req/s", compare: "above", enabled: true, severity: "warning" },
  { id: "server-errors", label: "Erros 5xx", metric: "errors5xx", threshold: 1, unit: "erros", compare: "above", enabled: true, severity: "critical" },
  { id: "rate-limit", label: "Rate limit 429", metric: "errors429", threshold: 1, unit: "erros", compare: "above", enabled: true, severity: "warning" },
  { id: "budget", label: "Error budget", metric: "errorBudgetRemaining", threshold: 0.5, unit: "", compare: "below", enabled: true, severity: "critical" }
]
const initialAgentMessages: AgentMessage[] = [
  { id: "agent-welcome", role: "agent", text: "Olá, eu sou o agente Vertx. Posso ajudar com lançamentos, dashboard, alertas, Swagger e teste de carga." }
]

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
        <a className="manual-link" href="/manual.html" target="_blank" rel="noreferrer">
          <FileText size={16} /> Manual de uso
        </a>

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
  const shellClient = useQueryClient()
  const [selectedScenario, setSelectedScenario] = useState("NORMAL")
  const [alertRules, setAlertRules] = useState<AlertRule[]>(defaultAlertRules)
  const accounts = useQuery({ queryKey: ["accounts"], queryFn: () => request<Account[]>("/api/entries/accounts") })
  const customers = useQuery({ queryKey: ["customers"], queryFn: () => request<Customer[]>("/api/entries/customers") })
  const entries = useQuery({ queryKey: ["entries"], queryFn: () => request<Entry[]>("/api/entries/entries") })
  const daily = useQuery({ queryKey: ["daily"], queryFn: () => request<DailyResponse>("/api/consolidated/daily") })
  const scenarios = useQuery({ queryKey: ["observability", "scenarios"], queryFn: () => request<string[]>("/api/observability/scenarios") })
  const sample = useQuery({ queryKey: ["scenario", selectedScenario], queryFn: () => request<ScenarioSample>(`/api/observability/samples?scenario=${encodeURIComponent(selectedScenario)}`), refetchInterval: 2000 })
  const scenarioMutation = useMutation({
    mutationFn: (scenario: string) => request<ScenarioSample>(`/api/observability/scenarios/${encodeURIComponent(scenario)}`, {
      method: "POST",
      body: JSON.stringify({ environment: "uat" })
    }),
    onSuccess: async (_sample, scenario) => {
      setSelectedScenario(scenario)
      await shellClient.invalidateQueries({ queryKey: ["scenario", scenario] })
    }
  })

  const credits = entries.data?.filter(item => item.type === "Credit").reduce((sum, item) => sum + Number(item.amount), 0) ?? 0
  const debits = entries.data?.filter(item => item.type === "Debit").reduce((sum, item) => sum + Number(item.amount), 0) ?? 0
  const metrics = sample.data?.metrics
  const dbRps = (metrics?.readRps ?? 0) + (metrics?.writeRps ?? 0)
  const activeAlerts = evaluateAlertRules(alertRules, sample.data).filter(alert => alert.enabled && alert.active)
  const scenarioNames = scenarios.data?.length ? scenarios.data : defaultScenarios

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <CircleDollarSign size={24} />
          <div>
            <strong>Fluxo de Caixa</strong>
            <span>{environmentName}</span>
          </div>
        </div>
        <nav className="side-nav">
          <a href="#dashboard"><LayoutDashboard size={18} /><span>Dashboard</span><small>Visão executiva</small></a>
          <a href="#monitor"><Gauge size={18} /><span>Monitoramento</span><small>Sistema e banco</small></a>
          <a href="#alerts"><Bell size={18} /><span>Alertas</span><small>Controle operacional</small></a>
          <a href="#loadtest"><Zap size={18} /><span>Teste de carga</span><small>Cenários sintéticos</small></a>
          <a href="#entries"><Banknote size={18} /><span>Lançamentos</span><small>Débito e crédito</small></a>
          <a href="#customers"><Building2 size={18} /><span>Clientes</span><small>Cadastro UAT</small></a>
          <a href="/manual.html" target="_blank" rel="noreferrer"><FileText size={18} /><span>Manual</span><small>Instruções completas</small></a>
        </nav>
        <div className="sidebar-status">
          <span><CheckCircle2 size={16} /> Sistema ready</span>
          <strong>{formatRps(dbRps)}</strong>
          <small>{selectedScenarioLabel(selectedScenario)}</small>
        </div>
      </aside>
      <main>
        <header className="topbar">
          <div>
            <span className="crumb">{environmentName} / Organização Alfa</span>
            <h1>Centro de comando financeiro</h1>
          </div>
          <div className="topbar-actions">
            <span className="badge"><Database size={16} /> Banco {formatRps(dbRps)}</span>
            <span className={activeAlerts.length ? "badge danger" : "badge"}><Bell size={16} /> {activeAlerts.length} alertas</span>
            <span className="badge"><ShieldCheck size={16} /> {session.displayName}</span>
            <button className="ghost-button" type="button" onClick={onLogout}>Sair</button>
          </div>
        </header>

        <section id="dashboard" className="metrics dashboard-metrics">
          <Metric title="Créditos" value={brl.format(credits)} icon={<Banknote />} />
          <Metric title="Débitos" value={brl.format(debits)} icon={<RefreshCw />} />
          <Metric title="Saldo projetado" value={brl.format(credits - debits)} icon={<Activity />} />
          <Metric title="Banco total req/s" value={formatRps(dbRps)} icon={<Database />} tone={dbRps > 120 ? "warning" : "normal"} />
          <Metric title="p95 API" value={`${metrics?.p95Ms ?? 0} ms`} icon={<LineChartIcon />} tone={(metrics?.p95Ms ?? 0) > 220 ? "warning" : "normal"} />
          <Metric title="Alertas ativos" value={String(activeAlerts.length)} icon={<Bell />} tone={activeAlerts.length ? "critical" : "normal"} />
        </section>

        <section className="analytics-grid">
          <ChartPanel title="Fluxo diário" badge="BRL">
            <ResponsiveContainer width="100%" height={260}>
              <AreaChart data={daily.data?.rows ?? []}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="businessDate" />
                <YAxis />
                <Tooltip formatter={(value) => brl.format(Number(value))} />
                <Legend />
                <Area type="monotone" name="Créditos" dataKey="credits" stroke="#0891b2" fill="#a5f3fc" />
                <Area type="monotone" name="Débitos" dataKey="debits" stroke="#be123c" fill="#fecdd3" />
              </AreaChart>
            </ResponsiveContainer>
          </ChartPanel>

          <ChartPanel title="Banco req/s" badge={selectedScenarioLabel(selectedScenario)}>
            <ResponsiveContainer width="100%" height={260}>
              <LineChart data={buildRequestSeries(sample.data)}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="point" />
                <YAxis />
                <Tooltip />
                <Legend />
                <Line type="monotone" name="Leitura" dataKey="read" stroke="#2563eb" strokeWidth={2} dot={false} />
                <Line type="monotone" name="Escrita" dataKey="write" stroke="#0f766e" strokeWidth={2} dot={false} />
                <Line type="monotone" name="Total" dataKey="total" stroke="#f59e0b" strokeWidth={3} dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </ChartPanel>

          <ChartPanel title="Latência" badge="p50 / p95 / p99">
            <ResponsiveContainer width="100%" height={240}>
              <LineChart data={buildLatencySeries(sample.data)}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="point" />
                <YAxis />
                <Tooltip formatter={(value) => `${value} ms`} />
                <Legend />
                <Line type="monotone" name="p50" dataKey="p50" stroke="#0f766e" strokeWidth={2} dot={false} />
                <Line type="monotone" name="p95" dataKey="p95" stroke="#f59e0b" strokeWidth={2} dot={false} />
                <Line type="monotone" name="p99" dataKey="p99" stroke="#be123c" strokeWidth={2} dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </ChartPanel>

          <ChartPanel title="Filas e projeção" badge="Outbox">
            <ResponsiveContainer width="100%" height={240}>
              <BarChart data={buildQueueSeries(sample.data)}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="name" />
                <YAxis />
                <Tooltip />
                <Bar name="Quantidade" dataKey="value" fill="#0f766e" radius={[6, 6, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </ChartPanel>
        </section>

        <section className="workspace">
          <EntryPanel accounts={accounts.data ?? []} customers={customers.data ?? []} />
          <LoadTestPanel
            metrics={metrics}
            scenarioNames={scenarioNames}
            selectedScenario={selectedScenario}
            isChanging={scenarioMutation.isPending}
            onSelectScenario={(scenario) => scenarioMutation.mutate(scenario)}
          />
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
          <div className="panel-title">Monitoramento do sistema <span className="badge">Telemetria sintética</span></div>
          <div className="monitor-grid">
            <Metric title="RPS leitura" value={formatRps(metrics?.readRps ?? 0)} icon={<Database />} />
            <Metric title="RPS escrita" value={formatRps(metrics?.writeRps ?? 0)} icon={<Server />} />
            <Metric title="Outbox pendente" value={integer.format(metrics?.outboxPending ?? 0)} icon={<FileText />} tone={(metrics?.outboxPending ?? 0) > 25 ? "warning" : "normal"} />
            <Metric title="Error budget" value={metrics?.errorBudgetRemaining == null ? "sem dados" : percent.format(metrics.errorBudgetRemaining)} icon={<ShieldCheck />} tone={(metrics?.errorBudgetRemaining ?? 1) < 0.5 ? "critical" : "normal"} />
          </div>
          <div className="health-grid">
            {buildHealthItems(sample.data).map(item => (
              <div className={`health-item ${item.tone}`} key={item.label}>
                <span>{item.label}</span>
                <strong>{item.status}</strong>
              </div>
            ))}
          </div>
        </section>

        <section id="alerts" className="panel">
          <div className="panel-title">Controle de alertas <span className={activeAlerts.length ? "badge danger" : "badge"}>{activeAlerts.length ? "Incidente simulado" : "Normal"}</span></div>
          <div className="alert-rules">
            {evaluateAlertRules(alertRules, sample.data).map(alert => (
              <label className={`alert-rule ${alert.active && alert.enabled ? alert.severity : ""}`} key={alert.id}>
                <input checked={alert.enabled} onChange={() => setAlertRules(rules => rules.map(rule => rule.id === alert.id ? { ...rule, enabled: !rule.enabled } : rule))} type="checkbox" />
                <span>
                  <strong>{alert.label}</strong>
                  <small>{alert.displayValue} / limite {formatAlertThreshold(alert)}</small>
                </span>
                <em>{alert.enabled ? (alert.active ? "ativo" : "ok") : "silenciado"}</em>
              </label>
            ))}
          </div>
          <div className="alert-feed">
            {(activeAlerts.length ? activeAlerts : [{ id: "none", label: "Sem alerta ativo", displayValue: "0", severity: "warning" as const }]).map(alert => (
              <div className={`alert-event ${alert.severity}`} key={alert.id}>
                <AlertTriangle size={18} />
                <span>{alert.label}</span>
                <strong>{alert.displayValue}</strong>
              </div>
            ))}
          </div>
        </section>
      </main>
      <FloatingAgent />
    </div>
  )
}

function FloatingAgent() {
  const [menuOpen, setMenuOpen] = useState(false)
  const [mode, setMode] = useState<AgentMode | null>(null)
  const [messages, setMessages] = useState<AgentMessage[]>(initialAgentMessages)
  const [draft, setDraft] = useState("")
  const [phoneNumber, setPhoneNumber] = useState("")
  const feedRef = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    if (mode === "chat" && feedRef.current) {
      feedRef.current.scrollTop = feedRef.current.scrollHeight
    }
  }, [messages, mode])

  function openMode(nextMode: AgentMode) {
    setMode(nextMode)
    setMenuOpen(false)
  }

  function sendMessage(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const text = draft.trim()
    if (!text) {
      return
    }

    const userMessage: AgentMessage = { id: crypto.randomUUID(), role: "user", text }
    const agentMessage: AgentMessage = { id: crypto.randomUUID(), role: "agent", text: buildAgentReply(text) }
    setMessages(current => [...current, userMessage, agentMessage])
    setDraft("")
  }

  return (
    <div className="agent-widget">
      {mode && (
        <section className="agent-panel" aria-label="Agente Vertx">
          <header className="agent-panel-header">
            <div>
              <span><Bot size={18} /> Agente Vertx</span>
              <small>{agentModeLabel(mode)}</small>
            </div>
            <button aria-label="Fechar agente" className="agent-icon-button" onClick={() => setMode(null)} type="button"><X size={18} /></button>
          </header>

          <div className="agent-mode-switch" role="tablist" aria-label="Modo do agente">
            <button className={mode === "chat" ? "active" : ""} onClick={() => openMode("chat")} type="button"><MessageCircle size={16} /> Chat</button>
            <button className={mode === "local" ? "active" : ""} onClick={() => openMode("local")} type="button"><Mic size={16} /> Local</button>
            <button className={mode === "call" ? "active" : ""} onClick={() => openMode("call")} type="button"><Phone size={16} /> Ligação</button>
          </div>

          {mode === "chat" && (
            <>
              <div className="agent-chat-feed" ref={feedRef}>
                {messages.map(message => (
                  <div className={`agent-message ${message.role}`} key={message.id}>
                    <span>{message.text}</span>
                  </div>
                ))}
              </div>
              <form className="agent-chat-form" onSubmit={sendMessage}>
                <input aria-label="Mensagem para o agente" value={draft} onChange={event => setDraft(event.target.value)} placeholder="Digite sua pergunta" />
                <button aria-label="Enviar mensagem" type="submit"><Send size={18} /></button>
              </form>
            </>
          )}

          {mode === "local" && (
            <div className="agent-voice-preview">
              <div className="agent-pulse"><Mic size={32} /></div>
              <strong>Conversa local</strong>
              <span>Visual pronto para voz local.</span>
              <button disabled type="button">Ativar microfone</button>
            </div>
          )}

          {mode === "call" && (
            <div className="agent-call-preview">
              <label className="field">
                <span>Número de telefone</span>
                <input value={phoneNumber} onChange={event => setPhoneNumber(event.target.value)} inputMode="tel" placeholder="(31) 99999-9999" />
              </label>
              <button disabled type="button"><Phone size={18} /> Ligar com agente</button>
              <small>Etapa visual preparada; integração telefônica será detalhada depois.</small>
            </div>
          )}
        </section>
      )}

      {menuOpen && !mode && (
        <div className="agent-menu" role="menu">
          <button onClick={() => openMode("chat")} role="menuitem" type="button"><MessageCircle size={18} /> Conversar por chat</button>
          <button onClick={() => openMode("local")} role="menuitem" type="button"><Mic size={18} /> Conversar local</button>
          <button onClick={() => openMode("call")} role="menuitem" type="button"><Phone size={18} /> Conversar por ligação</button>
        </div>
      )}

      <button className="agent-launcher" aria-label="Abrir agente Vertx" onClick={() => mode ? setMode(null) : setMenuOpen(open => !open)} type="button">
        <Bot size={28} />
        <span>Agente</span>
      </button>
    </div>
  )
}

function agentModeLabel(mode: AgentMode) {
  if (mode === "chat") {
    return "Conversar por chat"
  }

  if (mode === "local") {
    return "Conversar local"
  }

  return "Conversar por ligação"
}

function buildAgentReply(text: string) {
  const normalized = text.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase()
  if (normalized.includes("lanc") || normalized.includes("debito") || normalized.includes("credito")) {
    return "Para registrar, use o painel Novo lançamento: escolha crédito ou débito, conta, valor, data e descrição. Depois o dashboard e o consolidado são atualizados."
  }

  if (normalized.includes("saldo") || normalized.includes("consolid")) {
    return "O consolidado diário vem do read model. A Entries API grava o lançamento e o worker projeta os saldos por data de negócio."
  }

  if (normalized.includes("alert") || normalized.includes("monitor") || normalized.includes("carga") || normalized.includes("k6")) {
    return "O dashboard mostra telemetria sintética para operação visual. O requisito real de 50 RPS foi validado com k6 e está documentado em docs/testing."
  }

  if (normalized.includes("swagger") || normalized.includes("rota") || normalized.includes("api")) {
    return "As rotas documentadas estão no Swagger em /swagger. O OpenAPI JSON fica em /openapi/v1.json."
  }

  if (normalized.includes("senha") || normalized.includes("login") || normalized.includes("acesso")) {
    return "O acesso usa login, senha e Google reCAPTCHA v2 validado pelo BFF antes de liberar a sessão."
  }

  if (normalized.includes("telefone") || normalized.includes("ligacao") || normalized.includes("ligar")) {
    return "A experiência de ligação já tem o campo visual de telefone. A etapa de discagem real será conectada depois, quando definirmos o provedor e o fluxo seguro."
  }

  return "Posso te orientar pelo fluxo de caixa, lançamentos, consolidado, dashboard, alertas, Swagger ou teste de carga."
}

function Metric({ title, value, icon, tone = "normal" }: { title: string; value: string; icon: React.ReactNode; tone?: "normal" | "warning" | "critical" }) {
  return <div className={`metric ${tone}`}><span>{icon}</span><small>{title}</small><strong>{value}</strong></div>
}

function ChartPanel({ title, badge, children }: { title: string; badge: string; children: React.ReactNode }) {
  return (
    <div className="panel chart-panel">
      <div className="panel-title">{title}<span className="badge muted">{badge}</span></div>
      {children}
    </div>
  )
}

function LoadTestPanel({
  metrics,
  scenarioNames,
  selectedScenario,
  isChanging,
  onSelectScenario
}: {
  metrics?: ScenarioMetrics
  scenarioNames: string[]
  selectedScenario: string
  isChanging: boolean
  onSelectScenario: (scenario: string) => void
}) {
  const loadScenarios = scenarioNames.filter(scenario => scenario.includes("LOAD") || scenario.includes("SPIKE") || scenario === "NORMAL" || scenario === "RECOVERY")
  const dbRps = (metrics?.readRps ?? 0) + (metrics?.writeRps ?? 0)

  return (
    <div className="panel load-panel" id="loadtest">
      <div className="panel-title">Teste de carga <span className="badge">{selectedScenarioLabel(selectedScenario)}</span></div>
      <div className="scenario-grid">
        {loadScenarios.map(scenario => (
          <button className={scenario === selectedScenario ? "scenario-button active" : "scenario-button"} disabled={isChanging} key={scenario} onClick={() => onSelectScenario(scenario)} type="button">
            <Play size={16} />
            <span>{selectedScenarioLabel(scenario)}</span>
          </button>
        ))}
      </div>
      <div className="load-summary">
        <Metric title="Leitura banco" value={formatRps(metrics?.readRps ?? 0)} icon={<Database />} />
        <Metric title="Escrita banco" value={formatRps(metrics?.writeRps ?? 0)} icon={<Server />} />
        <Metric title="Total banco" value={formatRps(dbRps)} icon={<Zap />} tone={dbRps > 120 ? "warning" : "normal"} />
        <Metric title="Erros 5xx" value={integer.format(metrics?.errors5xx ?? 0)} icon={<AlertTriangle />} tone={(metrics?.errors5xx ?? 0) > 0 ? "critical" : "normal"} />
      </div>
    </div>
  )
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

function selectedScenarioLabel(scenario: string) {
  return scenarioLabels[scenario] ?? scenario
}

function formatRps(value: number) {
  return `${integer.format(Math.round(value))} req/s`
}

function buildRequestSeries(sample?: ScenarioSample) {
  const metrics = sample?.metrics
  const read = metrics?.readRps ?? 0
  const write = metrics?.writeRps ?? 0
  const factors = [0.82, 0.94, 1.05, 0.97, 1.1, 1.02, 0.96, 1]

  return factors.map((factor, index) => {
    const readValue = Math.round(read * factor)
    const writeValue = Math.round(write * (factor > 1 ? factor - 0.04 : factor + 0.03))
    return {
      point: `-${(factors.length - index - 1) * 5}s`,
      read: readValue,
      write: writeValue,
      total: readValue + writeValue
    }
  })
}

function buildLatencySeries(sample?: ScenarioSample) {
  const metrics = sample?.metrics
  const p50 = metrics?.p50Ms ?? 0
  const p95 = metrics?.p95Ms ?? 0
  const p99 = metrics?.p99Ms ?? 0
  const factors = [0.88, 0.96, 1.04, 0.98, 1.08, 1.02, 0.95, 1]

  return factors.map((factor, index) => ({
    point: `-${(factors.length - index - 1) * 5}s`,
    p50: Math.round(p50 * factor),
    p95: Math.round(p95 * factor),
    p99: Math.round(p99 * factor)
  }))
}

function buildQueueSeries(sample?: ScenarioSample) {
  const metrics = sample?.metrics
  return [
    { name: "Outbox", value: metrics?.outboxPending ?? 0 },
    { name: "Rabbit", value: metrics?.rabbitReady ?? 0 },
    { name: "Projetados", value: metrics?.projectedEntries ?? 0 },
    { name: "Duplicados", value: metrics?.duplicatesIgnored ?? 0 }
  ]
}

function buildHealthItems(sample?: ScenarioSample) {
  const health = sample?.health ?? {}
  return [
    { label: "Entries API", status: health.entriesApi ?? "sem amostra", tone: healthTone(health.entriesApi) },
    { label: "Read DB", status: health.consolidationApi ?? "sem amostra", tone: healthTone(health.consolidationApi) },
    { label: "RabbitMQ", status: health.rabbitMq ?? "sem amostra", tone: healthTone(health.rabbitMq) },
    { label: "Redis", status: health.redisCache ?? "sem amostra", tone: healthTone(health.redisCache) },
    { label: "Observability", status: health.observabilityStack ?? "sem amostra", tone: healthTone(health.observabilityStack) },
    { label: "AI boundary", status: health.aiAssistant ?? "sem amostra", tone: healthTone(health.aiAssistant) }
  ]
}

function healthTone(status?: string): "normal" | "warning" | "critical" {
  const normalized = status?.toLowerCase() ?? ""
  if (normalized.includes("down")) {
    return "critical"
  }

  if (normalized.includes("degraded") || normalized.includes("disabled")) {
    return "warning"
  }

  return "normal"
}

function evaluateAlertRules(rules: AlertRule[], sample?: ScenarioSample): AlertEvaluation[] {
  return rules.map(rule => {
    const value = alertMetricValue(rule.metric, sample)
    const active = rule.compare === "above" ? value >= rule.threshold : value <= rule.threshold
    return {
      ...rule,
      active,
      value,
      displayValue: formatAlertValue(rule, value)
    }
  })
}

function alertMetricValue(metric: AlertMetricKey, sample?: ScenarioSample) {
  const metrics = sample?.metrics
  if (!metrics) {
    return metric === "errorBudgetRemaining" ? 1 : 0
  }

  switch (metric) {
    case "dbRps":
      return metrics.readRps + metrics.writeRps
    case "errorBudgetRemaining":
      return metrics.errorBudgetRemaining ?? 1
    default:
      return metrics[metric]
  }
}

function formatAlertThreshold(rule: AlertRule) {
  return formatAlertValue(rule, rule.threshold)
}

function formatAlertValue(rule: Pick<AlertRule, "metric" | "unit">, value: number) {
  if (rule.metric === "errorBudgetRemaining") {
    return percent.format(value)
  }

  if (rule.unit === "req/s") {
    return formatRps(value)
  }

  if (rule.unit === "ms") {
    return `${integer.format(Math.round(value))} ms`
  }

  return rule.unit ? `${integer.format(Math.round(value))} ${rule.unit}` : integer.format(Math.round(value))
}

createRoot(document.getElementById("root")!).render(<App />)
