import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { Alert, Badge, Center, Group, Loader, Paper, SegmentedControl, Stack, Text, Title } from '@mantine/core'
import { api } from '../api/client'
import { usePolling } from '../hooks/usePolling'
import HopTable from '../components/HopTable'
import LatencyChart from '../components/LatencyChart'
import RouteMap from '../components/RouteMap'
import PathTimeline from '../components/PathTimeline'
import { formatRelativeTime } from '../utils/format'

const RANGE_HOURS: Record<string, number> = { '1h': 1, '24h': 24, '7d': 24 * 7 }

export default function TargetDetail() {
  const { id } = useParams()
  const targetId = Number(id)
  const [range, setRange] = useState('24h')

  const { data: targets } = usePolling(() => api.getTargets(), 30_000)
  const { data: run, error, loading } = usePolling(() => api.getLatestRun(targetId), 15_000)
  const { data: history } = usePolling(() => api.getRunHistory(targetId, RANGE_HOURS[range]), 30_000)
  const { data: events } = usePolling(() => api.getTargetEvents(targetId), 30_000)
  const { data: office } = usePolling(() => api.getOffice(), 300_000)

  const target = targets?.find((t) => t.id === targetId)

  if (loading) {
    return (
      <Center h={200}>
        <Loader />
      </Center>
    )
  }

  if (error || !run) {
    return (
      <Alert color="red" title="No se pudo cargar la traza">
        {error?.message ?? 'Sin datos todavia para este destino.'}
      </Alert>
    )
  }

  return (
    <Stack gap="lg">
      <Group justify="space-between">
        <div>
          <Title order={3}>{target?.name ?? `Target #${targetId}`}</Title>
          <Text c="dimmed" size="sm">
            {target?.provider} &middot; {target?.destinationHost}
          </Text>
        </div>
        <Badge variant="light">ultima traza {formatRelativeTime(run.startedAtUtc)}</Badge>
      </Group>

      <Paper withBorder p="md">
        <Group justify="space-between" mb="sm">
          <Text fw={600}>Historial</Text>
          <SegmentedControl value={range} onChange={setRange} data={['1h', '24h', '7d']} />
        </Group>
        {history && history.length > 0 ? (
          <LatencyChart history={history} />
        ) : (
          <Text c="dimmed" size="sm">
            Todavia no hay suficiente historial para este rango.
          </Text>
        )}
      </Paper>

      <Paper withBorder p="md">
        <Text fw={600} mb="sm">
          Mapa de la ruta actual
        </Text>
        {target && <RouteMap target={target} hops={run.hops} office={office ?? null} />}
      </Paper>

      <Paper withBorder p="md">
        <Text fw={600} mb="sm">
          Ultimo trace ({run.hops.length} saltos)
        </Text>
        <HopTable hops={run.hops} />
      </Paper>

      <Paper withBorder p="md">
        <Text fw={600} mb="sm">
          Cambios de ruta
        </Text>
        <PathTimeline events={events ?? []} />
      </Paper>
    </Stack>
  )
}
