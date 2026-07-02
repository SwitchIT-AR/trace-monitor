import { useMemo } from 'react'
import { Center, Group, Loader, Paper, Text } from '@mantine/core'
import type { Agent, AgentTraceRun, TargetSummary } from '../api/types'
import { buildRoutePoints } from '../utils/routePoints'
import MultiRouteMap, { type MapRoute } from './MultiRouteMap'

const PALETTE = ['#22b8cf', '#fd7e14', '#12b886', '#e64980', '#fab005', '#7950f2']

export default function OverviewMap({
  targets,
  runsByTarget,
  agents,
  selectedAgentId,
}: {
  targets: TargetSummary[]
  runsByTarget: Record<number, AgentTraceRun[]> | null
  agents: Agent[]
  selectedAgentId: number | null
}) {
  const routes: MapRoute[] = useMemo(() => {
    if (!runsByTarget) return []
    const agentById = new Map(agents.map((a) => [a.id, a]))

    return targets.flatMap((t, i) => {
      const color = PALETTE[i % PALETTE.length]
      const readings = (runsByTarget[t.id] ?? []).filter(
        (r) => selectedAgentId === null || r.agentId === selectedAgentId,
      )

      return readings.map((r) => {
        const agent = agentById.get(r.agentId)
        const origin =
          agent?.lat != null && agent?.lon != null
            ? { lat: agent.lat, lon: agent.lon, address: agent.address ?? agent.location }
            : null

        return {
          id: `${t.id}-${r.agentId}`,
          name: readings.length > 1 ? `${t.name} (${r.agentName})` : t.name,
          color,
          dashed: !r.agentIsBuiltIn,
          points: buildRoutePoints(t, r.hops, origin, r.agentName),
        }
      })
    })
  }, [targets, runsByTarget, agents, selectedAgentId])

  if (!runsByTarget) {
    return (
      <Center h="100%">
        <Loader />
      </Center>
    )
  }

  return (
    <Paper withBorder p="md" h="100%" style={{ display: 'flex', flexDirection: 'column' }}>
      <Group justify="space-between" mb="sm">
        <Text fw={600}>Mapa de rutas</Text>
        <Group gap="lg">
          {routes.map((r) => (
            <Group key={r.id} gap={6}>
              <div style={{ width: 10, height: 10, borderRadius: '50%', background: r.color }} />
              <Text size="sm" c="dimmed">
                {r.name}
              </Text>
            </Group>
          ))}
        </Group>
      </Group>
      <div style={{ flex: 1, minHeight: 0 }}>
        <MultiRouteMap routes={routes} height="100%" />
      </div>
    </Paper>
  )
}
