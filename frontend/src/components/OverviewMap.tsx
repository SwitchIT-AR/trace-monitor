import { useMemo } from 'react'
import { Center, Group, Loader, Paper, Text } from '@mantine/core'
import { api } from '../api/client'
import { usePolling } from '../hooks/usePolling'
import type { TargetSummary, TraceRun } from '../api/types'
import { buildRoutePoints } from '../utils/routePoints'
import MultiRouteMap, { type MapRoute } from './MultiRouteMap'

const PALETTE = ['#22b8cf', '#fd7e14', '#12b886', '#e64980', '#fab005', '#7950f2']

function useLatestRuns(targets: TargetSummary[]) {
  return usePolling(async () => {
    const entries = await Promise.all(
      targets.map(async (t) => {
        try {
          return [t.id, await api.getLatestRun(t.id)] as const
        } catch {
          return [t.id, null] as const
        }
      }),
    )
    return Object.fromEntries(entries) as Record<number, TraceRun | null>
  }, 30_000)
}

export default function OverviewMap({ targets }: { targets: TargetSummary[] }) {
  const { data: runsByTarget, loading } = useLatestRuns(targets)
  const { data: office } = usePolling(() => api.getOffice(), 300_000)

  const routes: MapRoute[] = useMemo(() => {
    if (!runsByTarget) return []
    return targets.map((t, i) => ({
      id: t.id,
      name: t.name,
      color: PALETTE[i % PALETTE.length],
      points: buildRoutePoints(t, runsByTarget[t.id]?.hops ?? [], office ?? null),
    }))
  }, [targets, runsByTarget, office])

  if (loading) {
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
