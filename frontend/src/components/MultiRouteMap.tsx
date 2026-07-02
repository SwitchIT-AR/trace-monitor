import { Fragment, useEffect, useMemo } from 'react'
import { MapContainer, TileLayer, Marker, Polyline, Popup, useMap } from 'react-leaflet'
import L from 'leaflet'
import icon from 'leaflet/dist/images/marker-icon.png'
import iconShadow from 'leaflet/dist/images/marker-shadow.png'
import { Center, Text } from '@mantine/core'
import type { RoutePoint } from '../utils/routePoints'

L.Icon.Default.mergeOptions({
  iconUrl: icon,
  shadowUrl: iconShadow,
  iconRetinaUrl: icon,
})

// Free, no-API-key dark basemap so the map matches the rest of the dark UI.
const DARK_TILE_URL = 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png'
const DARK_TILE_ATTRIBUTION =
  '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>'

export type MapRoute = {
  id: string
  name: string
  color: string
  /** Distinguishes routes that share a color (same target, different agent). */
  dashed?: boolean
  points: RoutePoint[]
}

function FitToPoints({ points }: { points: [number, number][] }) {
  const map = useMap()

  useEffect(() => {
    if (points.length === 0) return
    if (points.length === 1) {
      map.setView(points[0], 11)
    } else {
      map.fitBounds(points, { padding: [30, 30] })
    }
  }, [map, points])

  return null
}

function hopIcon(color: string) {
  return L.divIcon({
    className: '',
    html: `<div style="width:12px;height:12px;border-radius:50%;background:${color};border:2px solid #1a1b1e;box-shadow:0 0 0 1px ${color}"></div>`,
    iconSize: [12, 12],
    iconAnchor: [6, 6],
  })
}

function verifiedIcon(color: string) {
  return L.divIcon({
    className: '',
    html: `<div style="width:16px;height:16px;border-radius:3px;background:${color};border:2px solid white;transform:rotate(45deg);box-shadow:0 0 0 1px ${color}"></div>`,
    iconSize: [16, 16],
    iconAnchor: [8, 8],
  })
}

const OFFICE_ICON = L.divIcon({
  className: '',
  html: `<div style="width:16px;height:16px;border-radius:50%;background:#e9ecef;border:3px solid #1a1b1e;box-shadow:0 0 0 1.5px #e9ecef"></div>`,
  iconSize: [16, 16],
  iconAnchor: [8, 8],
})

function PointPopup({ routeName, point }: { routeName: string; point: RoutePoint }) {
  if (point.kind === 'office') {
    return (
      <Popup>
        <strong>{point.label}</strong>
        <br />
        {point.city}
      </Popup>
    )
  }

  if (point.kind === 'verified') {
    return (
      <Popup>
        <strong>{point.label} — dirección verificada</strong>
        <br />
        {point.city}
        <br />
        <em>Según dato real provisto, no por geolocalización de IP.</em>
      </Popup>
    )
  }

  return (
    <Popup>
      <strong>
        {routeName} — #{point.hopIndex} {point.hostname ?? point.ip}
      </strong>
      <br />
      {point.ip}
      <br />
      {point.city ? `${point.city}, ` : ''}
      {point.country}
      {point.asn && (
        <>
          <br />
          {point.asn}
        </>
      )}
    </Popup>
  )
}

export default function MultiRouteMap({
  routes,
  height = 400,
}: {
  routes: MapRoute[]
  height?: number | string
}) {
  const allPoints = useMemo(
    () => routes.flatMap((r) => r.points.map((p) => [p.lat, p.lon] as [number, number])),
    [routes],
  )

  const officePoints = useMemo(() => {
    const seen = new Map<string, RoutePoint>()
    for (const route of routes) {
      for (const p of route.points) {
        if (p.kind === 'office') seen.set(`${p.lat},${p.lon}`, p)
      }
    }
    return [...seen.values()]
  }, [routes])

  if (allPoints.length === 0) {
    return (
      <Center h={height} style={{ border: '1px solid var(--mantine-color-default-border)', borderRadius: 8 }}>
        <Text c="dimmed">Todavia no hay saltos con IP publica geolocalizada.</Text>
      </Center>
    )
  }

  return (
    <MapContainer center={allPoints[0]} zoom={4} style={{ height, borderRadius: 8, background: '#1a1b1e' }}>
      <TileLayer attribution={DARK_TILE_ATTRIBUTION} url={DARK_TILE_URL} />
      <FitToPoints points={allPoints} />

      {officePoints.map((p) => (
        <Marker key={`office-${p.lat}-${p.lon}`} position={[p.lat, p.lon]} icon={OFFICE_ICON}>
          <PointPopup routeName="" point={p} />
        </Marker>
      ))}

      {routes.map((route) => (
        <Fragment key={route.id}>
          <Polyline
            positions={route.points.map((p) => [p.lat, p.lon])}
            color={route.color}
            weight={3}
            dashArray={route.dashed ? '6 6' : undefined}
          />
          {route.points
            .filter((p) => p.kind !== 'office')
            .map((p) => (
              <Marker
                key={`${route.id}-${p.kind}-${p.hopIndex ?? p.label}`}
                position={[p.lat, p.lon]}
                icon={p.kind === 'verified' ? verifiedIcon(route.color) : hopIcon(route.color)}
              >
                <PointPopup routeName={route.name} point={p} />
              </Marker>
            ))}
        </Fragment>
      ))}
    </MapContainer>
  )
}
