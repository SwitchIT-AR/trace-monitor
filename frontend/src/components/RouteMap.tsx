import type { Hop } from '../api/types'
import MultiRouteMap from './MultiRouteMap'

export default function RouteMap({ hops }: { hops: Hop[] }) {
  return <MultiRouteMap routes={[{ id: 0, name: 'Ruta actual', color: '#22b8cf', hops }]} />
}
