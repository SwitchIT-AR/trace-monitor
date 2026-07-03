import type {
  AccessPair,
  Agent,
  AgentCreated,
  AgentTraceRun,
  AiAnalysisReportSummary,
  AiAnalysisResult,
  CreateAgentRequest,
  CreateUserRequest,
  HopLoss,
  LossSummary,
  MaskedKey,
  MeDto,
  OfficeLocation,
  PathChangeEvent,
  RouteTimelineData,
  RunHistoryPoint,
  TargetSummary,
  TraceRun,
  UpdateAgentRequest,
  UserSummary,
} from './types'

const API_BASE = '/api'

async function getJson<T>(path: string): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, { credentials: 'include' })
  if (!res.ok) throw new Error(`GET ${path} -> ${res.status}`)
  return res.json() as Promise<T>
}

async function postJson<T>(path: string, body: unknown): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  if (!res.ok) throw new Error(`POST ${path} -> ${res.status}`)
  return res.json() as Promise<T>
}

async function del(path: string): Promise<void> {
  const res = await fetch(`${API_BASE}${path}`, { method: 'DELETE', credentials: 'include' })
  if (!res.ok) throw new Error(`DELETE ${path} -> ${res.status}`)
}

async function postVoid(path: string): Promise<void> {
  const res = await fetch(`${API_BASE}${path}`, { method: 'POST', credentials: 'include' })
  if (!res.ok) throw new Error(`POST ${path} -> ${res.status}`)
}

async function putJson(path: string, body: unknown): Promise<void> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'PUT',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  if (!res.ok) throw new Error(`PUT ${path} -> ${res.status}`)
}

export const api = {
  getTargets: () => getJson<TargetSummary[]>('/targets'),
  getLatestRun: (targetId: number) => getJson<TraceRun>(`/targets/${targetId}/latest`),
  getLatestRunsByAgent: (targetId: number) => getJson<AgentTraceRun[]>(`/targets/${targetId}/latest-by-agent`),
  getRunHistory: (targetId: number, sinceHours: number) => {
    const from = new Date(Date.now() - sinceHours * 3600_000).toISOString()
    return getJson<RunHistoryPoint[]>(`/targets/${targetId}/runs?from=${encodeURIComponent(from)}`)
  },
  getAllEvents: () => getJson<PathChangeEvent[]>('/events'),
  getOffice: () => getJson<OfficeLocation>('/office'),
  getAgents: () => getJson<Agent[]>('/agents'),
  createAgent: (request: CreateAgentRequest) => postJson<AgentCreated>('/agents', request),
  deactivateAgent: (id: number) => del(`/agents/${id}`),
  updateAgent: (id: number, request: UpdateAgentRequest) => putJson(`/agents/${id}`, request),
  getLossSummary: (hours: number) => getJson<LossSummary[]>(`/stats/loss-summary?hours=${hours}`),
  getHopLoss: (targetId: number, agentId: number, hours: number) =>
    getJson<HopLoss[]>(`/targets/${targetId}/hop-loss?agentId=${agentId}&hours=${hours}`),
  getRouteTimeline: (targetId: number, agentId: number, hours: number) =>
    getJson<RouteTimelineData>(`/targets/${targetId}/route-timeline?agentId=${agentId}&hours=${hours}`),
  getRunHops: (targetId: number, runId: number) => getJson<TraceRun>(`/targets/${targetId}/runs/${runId}/hops`),
  runAiAnalysis: () => postJson<AiAnalysisResult>('/ai/analyze', {}),
  getAiReports: (limit = 20) => getJson<AiAnalysisReportSummary[]>(`/ai/reports?limit=${limit}`),
  getAiReport: (id: number) => getJson<AiAnalysisResult>(`/ai/reports/${id}`),
  login: (username: string, password: string) => postJson<MeDto>('/auth/login', { username, password }),
  logout: () => postVoid('/auth/logout'),
  getMe: () => getJson<MeDto>('/auth/me'),
  getAnthropicKey: () => getJson<MaskedKey>('/settings/anthropic-key'),
  setAnthropicKey: (value: string) => putJson('/settings/anthropic-key', { value }),
  getUsers: () => getJson<UserSummary[]>('/users'),
  createUser: (request: CreateUserRequest) => postJson<UserSummary>('/users', request),
  resetUserPassword: (id: number, newPassword: string) => putJson(`/users/${id}/password`, { newPassword }),
  deactivateUser: (id: number) => del(`/users/${id}`),
  getUserAccess: (id: number) => getJson<AccessPair[]>(`/users/${id}/access`),
  setUserAccess: (id: number, pairs: AccessPair[]) => putJson(`/users/${id}/access`, pairs),
}
