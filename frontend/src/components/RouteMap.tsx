import { useEffect, useMemo } from 'react'
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

type GeoHop = Hop & { lat: number; lon: number }

function FitToPoints({ points }: { points: GeoHop[] }) {
  const map = useMap()

  useEffect(() => {
    if (points.length === 1) {
      map.setView([points[0].lat, points[0].lon], 11)
    } else {
      map.fitBounds(
        points.map((p) => [p.lat, p.lon]),
        { padding: [30, 30] },
      )
    }
  }, [map, points])

  return null
}

export default function RouteMap({ hops }: { hops: Hop[] }) {
  const points = useMemo(
    () => hops.filter((h): h is GeoHop => h.lat !== null && h.lon !== null),
    [hops],
  )

  if (points.length === 0) {
    return (
      <Center h={300} style={{ border: '1px solid var(--mantine-color-default-border)', borderRadius: 8 }}>
        <Text c="dimmed">Todavia no hay saltos con IP publica geolocalizada para esta traza.</Text>
      </Center>
    )
  }

  const line = points.map((p) => [p.lat, p.lon] as [number, number])

  return (
    <MapContainer center={[points[0].lat, points[0].lon]} zoom={4} style={{ height: 400, borderRadius: 8 }}>
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      <FitToPoints points={points} />
      <Polyline positions={line} color="#22b8cf" />
      {points.map((p) => (
        <Marker key={p.hopIndex} position={[p.lat, p.lon]}>
          <Popup>
            <strong>
              #{p.hopIndex} {p.hostname ?? p.ip}
            </strong>
            <br />
            {p.ip}
            <br />
            {p.city ? `${p.city}, ` : ''}
            {p.country}
          </Popup>
        </Marker>
      ))}
    </MapContainer>
  )
}
