import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { ActionIcon, Alert, Badge, Center, Group, Loader, Paper, Select, SegmentedControl, Stack, Text, Title, Tooltip } from '@mantine/core'
import { IconArrowLeft, IconChevronLeft, IconChevronRight } from '@tabler/icons-react'
import { api } from '../api/client'
import { usePolling } from '../hooks/usePolling'
import HopTable from '../components/HopTable'
import LatencyChart from '../components/LatencyChart'
import RouteMap from '../components/RouteMap'
import RouteTimeline from '../components/RouteTimeline'
import { formatRelativeTime } from '../utils/format'

const RANGE_HOURS: Record<string, number> = { '1h': 1, '24h': 24, '7d': 24 * 7 }

export default function TargetDetail() {
  const { id } = useParams()
  const navigate = useNavigate()
  const targetId = Number(id)
  const [range, setRange] = useState('24h')
  const [selectedAgentId, setSelectedAgentId] = useState<number | null>(null)

  const { data: targets } = usePolling(() => api.getTargets(), 30_000)
  const { data: run, error, loading } = usePolling(() => api.getLatestRun(targetId), 15_000)
  const { data: history } = usePolling(() => api.getRunHistory(targetId, RANGE_HOURS[range]), 30_000, [targetId, range])
  const { data: agentRuns } = usePolling(() => api.getLatestRunsByAgent(targetId), 30_000, [targetId])
  const { data: office } = usePolling(() => api.getOffice(), 300_000)
  const { data: agents } = usePolling(() => api.getAgents(), 30_000)

  const effectiveAgentId = selectedAgentId ?? agentRuns?.[0]?.agentId ?? null
  const effectiveAgent = agents?.find((a) => a.id === effectiveAgentId)

  const target = targets?.find((t) => t.id === targetId)
  const currentIndex = targets?.findIndex((t) => t.id === targetId) ?? -1
  const prevTarget = targets && currentIndex > 0 ? targets[currentIndex - 1] : null
  const nextTarget = targets && currentIndex >= 0 && currentIndex < targets.length - 1 ? targets[currentIndex + 1] : null

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
      <Group justify="space-between" wrap="wrap">
        <Group gap="xs" wrap="nowrap">
          <ActionIcon variant="subtle" onClick={() => navigate(-1)} aria-label="Volver">
            <IconArrowLeft size={18} />
          </ActionIcon>
          <div style={{ minWidth: 0, flex: 1 }}>
            <Title order={3} lineClamp={1}>
              {target?.name ?? `Target #${targetId}`}
            </Title>
            <Text c="dimmed" size="sm" truncate="end">
              {target?.provider} &middot; {target?.destinationHost}
            </Text>
            {effectiveAgent && (
              <Text c="dimmed" size="xs" truncate="end">
                {effectiveAgent.location} → {target?.name}
              </Text>
            )}
          </div>
        </Group>
        <Group gap="xs" wrap="nowrap">
          <Tooltip label={prevTarget ? prevTarget.name : 'Sin destino anterior'}>
            <ActionIcon variant="subtle" disabled={!prevTarget} onClick={() => prevTarget && navigate(`/targets/${prevTarget.id}`)}>
              <IconChevronLeft size={18} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label={nextTarget ? nextTarget.name : 'Sin destino siguiente'}>
            <ActionIcon variant="subtle" disabled={!nextTarget} onClick={() => nextTarget && navigate(`/targets/${nextTarget.id}`)}>
              <IconChevronRight size={18} />
            </ActionIcon>
          </Tooltip>
          <Badge variant="light">ultima traza {formatRelativeTime(run.startedAtUtc)}</Badge>
        </Group>
      </Group>

      <Paper withBorder p="md">
        <Group justify="space-between" mb="sm" wrap="wrap">
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
        <Group justify="space-between" mb="sm" wrap="wrap">
          <Text fw={600}>Rutas</Text>
          {agentRuns && agentRuns.length > 1 && (
            <Select
              value={String(effectiveAgentId)}
              onChange={(v) => v && setSelectedAgentId(Number(v))}
              w={{ base: '100%', xs: 200 }}
              data={agentRuns.map((a) => ({ value: String(a.agentId), label: a.agentName }))}
            />
          )}
        </Group>
        {target && effectiveAgentId !== null ? (
          <RouteTimeline targetId={targetId} agentId={effectiveAgentId} target={target} office={office ?? null} />
        ) : (
          <Text c="dimmed" size="sm">
            Todavia no hay datos de agentes para este destino.
          </Text>
        )}
      </Paper>
    </Stack>
  )
}
