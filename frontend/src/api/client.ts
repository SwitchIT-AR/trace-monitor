import type {
  Agent,
  AgentCreated,
  AgentTraceRun,
  CreateAgentRequest,
  HopLoss,
  LossSummary,
  OfficeLocation,
  PathChangeEvent,
  RunHistoryPoint,
  TargetSummary,
  TraceRun,
  UpdateAgentLocationRequest,
} from './types'

const API_BASE = '/api'

async function getJson<T>(path: string): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`)
  if (!res.ok) throw new Error(`GET ${path} -> ${res.status}`)
  return res.json() as Promise<T>
}

async function postJson<T>(path: string, body: unknown): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  if (!res.ok) throw new Error(`POST ${path} -> ${res.status}`)
  return res.json() as Promise<T>
}

async function del(path: string): Promise<void> {
  const res = await fetch(`${API_BASE}${path}`, { method: 'DELETE' })
  if (!res.ok) throw new Error(`DELETE ${path} -> ${res.status}`)
}

async function putJson(path: string, body: unknown): Promise<void> {
  const res = await fetch(`${API_BASE}${path}`, {
    method: 'PUT',
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
  getTargetEvents: (targetId: number) => getJson<PathChangeEvent[]>(`/targets/${targetId}/events`),
  getAllEvents: () => getJson<PathChangeEvent[]>('/events'),
  getOffice: () => getJson<OfficeLocation>('/office'),
  getAgents: () => getJson<Agent[]>('/agents'),
  createAgent: (request: CreateAgentRequest) => postJson<AgentCreated>('/agents', request),
  deactivateAgent: (id: number) => del(`/agents/${id}`),
  updateAgentLocation: (id: number, request: UpdateAgentLocationRequest) => putJson(`/agents/${id}/location`, request),
  getLossSummary: (hours: number) => getJson<LossSummary[]>(`/stats/loss-summary?hours=${hours}`),
  getHopLoss: (targetId: number, agentId: number, hours: number) =>
    getJson<HopLoss[]>(`/targets/${targetId}/hop-loss?agentId=${agentId}&hours=${hours}`),
}
