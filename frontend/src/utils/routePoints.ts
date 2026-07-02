import type { Hop, OfficeLocation, TargetSummary } from '../api/types'

export type RoutePointKind = 'office' | 'hop' | 'verified'

export type RoutePoint = {
  kind: RoutePointKind
  lat: number
  lon: number
  label: string
  ip?: string | null
  hostname?: string | null
  city?: string | null
  country?: string | null
  asn?: string | null
  hopIndex?: number
}

const EARTH_RADIUS_KM = 6371

function haversineKm(lat1: number, lon1: number, lat2: number, lon2: number): number {
  const toRad = (deg: number) => (deg * Math.PI) / 180
  const dLat = toRad(lat2 - lat1)
  const dLon = toRad(lon2 - lon1)
  const a =
    Math.sin(dLat / 2) ** 2 + Math.cos(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.sin(dLon / 2) ** 2
  return 2 * EARTH_RADIUS_KM * Math.asin(Math.sqrt(a))
}

/** Hop points whose IP-geolocation differs from the verified address by more than this are kept
 * as a separate "ISP node" point rather than merged into the verified destination. */
const DIFFERENT_LOCATION_THRESHOLD_KM = 2

export function buildRoutePoints(
  target: TargetSummary,
  hops: Hop[],
  origin: OfficeLocation | null,
  originLabel = 'Oficina',
): RoutePoint[] {
  const points: RoutePoint[] = []

  if (origin) {
    points.push({ kind: 'office', lat: origin.lat, lon: origin.lon, label: originLabel, city: origin.address })
  }

  const geoHops = hops.filter((h): h is Hop & { lat: number; lon: number } => h.lat !== null && h.lon !== null)
  for (const h of geoHops) {
    points.push({
      kind: 'hop',
      lat: h.lat,
      lon: h.lon,
      label: h.hostname ?? h.ip ?? `hop ${h.hopIndex}`,
      ip: h.ip,
      hostname: h.hostname,
      city: h.city,
      country: h.country,
      asn: h.asn,
      hopIndex: h.hopIndex,
    })
  }

  if (target.verifiedLat !== null && target.verifiedLon !== null) {
    const lastGeoHop = geoHops.at(-1)
    const distanceKm = lastGeoHop
      ? haversineKm(lastGeoHop.lat, lastGeoHop.lon, target.verifiedLat, target.verifiedLon)
      : Infinity

    if (distanceKm > DIFFERENT_LOCATION_THRESHOLD_KM) {
      // The IP-based hop resolves to the ISP's node/POP, not the customer's real address —
      // keep both: the ISP node (already added above) and the verified destination.
      points.push({
        kind: 'verified',
        lat: target.verifiedLat,
        lon: target.verifiedLon,
        label: target.name,
        city: target.verifiedAddress,
      })
    }
  }

  return points
}
