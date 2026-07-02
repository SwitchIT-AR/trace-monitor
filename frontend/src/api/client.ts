import type { OfficeLocation, PathChangeEvent, RunHistoryPoint, TargetSummary, TraceRun } from './types'

const API_BASE = '/api'

async function getJson<T>(path: string): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`)
  if (!res.ok) throw new Error(`GET ${path} -> ${res.status}`)
  return res.json() as Promise<T>
}

export const api = {
  getTargets: () => getJson<TargetSummary[]>('/targets'),
  getLatestRun: (targetId: number) => getJson<TraceRun>(`/targets/${targetId}/latest`),
  getRunHistory: (targetId: number, sinceHours: number) => {
    const from = new Date(Date.now() - sinceHours * 3600_000).toISOString()
    return getJson<RunHistoryPoint[]>(`/targets/${targetId}/runs?from=${encodeURIComponent(from)}`)
  },
  getTargetEvents: (targetId: number) => getJson<PathChangeEvent[]>(`/targets/${targetId}/events`),
  getAllEvents: () => getJson<PathChangeEvent[]>('/events'),
  getOffice: () => getJson<OfficeLocation>('/office'),
}
