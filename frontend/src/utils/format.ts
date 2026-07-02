export function formatRelativeTime(iso: string | null): string {
  if (!iso) return 'sin datos'
  const diffMs = Date.now() - new Date(iso + (iso.endsWith('Z') ? '' : 'Z')).getTime()
  const minutes = Math.round(diffMs / 60_000)
  if (minutes < 1) return 'recien'
  if (minutes < 60) return `hace ${minutes}m`
  const hours = Math.round(minutes / 60)
  if (hours < 24) return `hace ${hours}h`
  return `hace ${Math.round(hours / 24)}d`
}

export function lossColor(lossPct: number): string {
  if (lossPct <= 0) return 'teal'
  if (lossPct <= 5) return 'yellow'
  return 'red'
}

export function formatDateTime(iso: string): string {
  const d = new Date(iso.endsWith('Z') ? iso : iso + 'Z')
  return d.toLocaleString('es-AR')
}

export function formatDuration(startIso: string, endIso: string | null): string {
  const start = new Date(startIso.endsWith('Z') ? startIso : startIso + 'Z').getTime()
  const end = endIso ? new Date(endIso.endsWith('Z') ? endIso : endIso + 'Z').getTime() : Date.now()
  const totalMinutes = Math.max(0, Math.round((end - start) / 60_000))
  const days = Math.floor(totalMinutes / 1440)
  const hours = Math.floor((totalMinutes % 1440) / 60)
  const minutes = totalMinutes % 60
  if (days > 0) return `${days}d ${hours}h`
  if (hours > 0) return `${hours}h ${minutes}m`
  return `${minutes}m`
}
