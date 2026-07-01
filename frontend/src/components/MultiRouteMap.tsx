import { Fragment, useEffect, useMemo } from 'react'
import { MapContainer, TileLayer, Marker, Polyline, Popup, useMap } from 'react-leaflet'
import L from 'leaflet'
import icon from 'leaflet/dist/images/marker-icon.png'
import iconShadow from 'leaflet/dist/images/marker-shadow.png'
import { Center, Text } from '@mantine/core'
import type { Hop } from '../api/types'

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
  id: number
  name: string
  color: string
  hops: Hop[]
}

type GeoHop = Hop & { lat: number; lon: number }

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

export default function MultiRouteMap({ routes, height = 400 }: { routes: MapRoute[]; height?: number }) {
  const geoRoutes = useMemo(
    () =>
      routes.map((r) => ({
        ...r,
        points: r.hops.filter((h): h is GeoHop => h.lat !== null && h.lon !== null),
      })),
    [routes],
  )

  const allPoints = useMemo(
    () => geoRoutes.flatMap((r) => r.points.map((p) => [p.lat, p.lon] as [number, number])),
    [geoRoutes],
  )

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
      {geoRoutes.map((route) => (
        <Fragment key={route.id}>
          <Polyline positions={route.points.map((p) => [p.lat, p.lon])} color={route.color} weight={3} />
          {route.points.map((p) => (
            <Marker
              key={`${route.id}-${p.hopIndex}`}
              position={[p.lat, p.lon]}
              icon={L.divIcon({
                className: '',
                html: `<div style="width:12px;height:12px;border-radius:50%;background:${route.color};border:2px solid #1a1b1e;box-shadow:0 0 0 1px ${route.color}"></div>`,
                iconSize: [12, 12],
                iconAnchor: [6, 6],
              })}
            >
              <Popup>
                <strong>
                  {route.name} — #{p.hopIndex} {p.hostname ?? p.ip}
                </strong>
                <br />
                {p.ip}
                <br />
                {p.city ? `${p.city}, ` : ''}
                {p.country}
                {p.asn && (
                  <>
                    <br />
                    {p.asn}
                  </>
                )}
              </Popup>
            </Marker>
          ))}
        </Fragment>
      ))}
    </MapContainer>
  )
}
