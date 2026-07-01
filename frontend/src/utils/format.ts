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
