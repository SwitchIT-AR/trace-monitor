import { useMemo } from 'react'
import { Center, Group, Loader, Paper, Stack, Text } from '@mantine/core'
import { api } from '../api/client'
import { usePolling } from '../hooks/usePolling'
import type { TargetSummary, TraceRun } from '../api/types'
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

  const routes: MapRoute[] = useMemo(() => {
    if (!runsByTarget) return []
    return targets.map((t, i) => ({
      id: t.id,
      name: t.name,
      color: PALETTE[i % PALETTE.length],
      hops: runsByTarget[t.id]?.hops ?? [],
    }))
  }, [targets, runsByTarget])

  if (loading) {
    return (
      <Center h={420}>
        <Loader />
      </Center>
    )
  }

  return (
    <Paper withBorder p="md">
      <Stack gap="sm">
        <Group justify="space-between">
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
        <MultiRouteMap routes={routes} height={420} />
      </Stack>
    </Paper>
  )
}
