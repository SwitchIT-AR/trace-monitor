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
  // routes: one polyline per (target, agent) reading, for the map itself. legendEntries: deduped
  // to one entry per target — the legend groups by destination only, agents on the same target
  // just share that color (solid = built-in "Oficina", dashed = everyone else).
  const { routes, legendEntries } = useMemo(() => {
    const routes: MapRoute[] = []
    const legendEntries: { name: string; color: string }[] = []
    if (!runsByTarget) return { routes, legendEntries }

    const agentById = new Map(agents.map((a) => [a.id, a]))

    targets.forEach((t, i) => {
      const color = PALETTE[i % PALETTE.length]
      const readings = (runsByTarget[t.id] ?? []).filter(
        (r) => selectedAgentId === null || r.agentId === selectedAgentId,
      )
      if (readings.length === 0) return

      legendEntries.push({ name: t.name, color })

      for (const r of readings) {
        const agent = agentById.get(r.agentId)
        const origin =
          agent?.lat != null && agent?.lon != null
            ? { lat: agent.lat, lon: agent.lon, address: agent.address ?? agent.location }
            : null

        routes.push({
          id: `${t.id}-${r.agentId}`,
          name: `${t.name} (${r.agentName})`,
          color,
          dashed: !r.agentIsBuiltIn,
          points: buildRoutePoints(t, r.hops, origin, r.agentName),
        })
      }
    })

    return { routes, legendEntries }
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
      <Group justify="space-between" mb="sm" wrap="wrap">
        <Text fw={600}>Mapa de rutas</Text>
        <Group gap="lg" wrap="wrap">
          {legendEntries.map((e) => (
            <Group key={e.name} gap={6}>
              <div style={{ width: 10, height: 10, borderRadius: '50%', background: e.color }} />
              <Text size="sm" c="dimmed">
                {e.name}
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
