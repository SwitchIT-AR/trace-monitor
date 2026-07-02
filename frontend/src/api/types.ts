export type TargetSummary = {
  id: number
  name: string
  provider: string
  destinationHost: string
  lastRunAtUtc: string | null
  lastLossPct: number | null
  lastAvgRttMs: number | null
  lastPathChangeAtUtc: string | null
  verifiedLat: number | null
  verifiedLon: number | null
  verifiedAddress: string | null
}

export type OfficeLocation = {
  lat: number
  lon: number
  address: string
}

export type Hop = {
  hopIndex: number
  ip: string | null
  hostname: string | null
  lossPct: number
  sent: number
  last: number
  avg: number
  best: number
  worst: number
  stDev: number
  lat: number | null
  lon: number | null
  city: string | null
  country: string | null
  asn: string | null
  isPrivate: boolean
}

export type TraceRun = {
  id: number
  startedAtUtc: string
  overallLossPct: number
  overallAvgRttMs: number
  hops: Hop[]
}

export type RunHistoryPoint = {
  id: number
  startedAtUtc: string
  overallLossPct: number
  overallAvgRttMs: number
}

export type PathChangeEvent = {
  id: number
  targetId: number
  targetName: string
  detectedAtUtc: string
  previousHopsJson: string
  newHopsJson: string
}

export type HopSnapshot = { hopIndex: number; ip: string | null; hostname: string | null }
