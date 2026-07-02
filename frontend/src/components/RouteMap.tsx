import type { Hop, OfficeLocation, TargetSummary } from '../api/types'
import { buildRoutePoints } from '../utils/routePoints'
import MultiRouteMap from './MultiRouteMap'

export default function RouteMap({
  target,
  hops,
  office,
}: {
  target: TargetSummary
  hops: Hop[]
  office: OfficeLocation | null
}) {
  const points = buildRoutePoints(target, hops, office)
  return <MultiRouteMap routes={[{ id: target.id, name: target.name, color: '#22b8cf', points }]} />
}
