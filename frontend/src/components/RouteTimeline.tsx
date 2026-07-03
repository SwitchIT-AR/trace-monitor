import { useState } from 'react'
import { Badge, Group, Loader, Select, Stack, Tabs, Text, Timeline } from '@mantine/core'
import { IconArrowDown } from '@tabler/icons-react'
import { api } from '../api/client'
import { usePolling } from '../hooks/usePolling'
import HopTable from './HopTable'
import RouteMap from './RouteMap'
import { formatDateTime, formatDuration } from '../utils/format'
import type { OfficeLocation, TargetSummary } from '../api/types'

const RANGE_OPTIONS = [
  { value: '24', label: 'Ultimas 24h' },
  { value: '168', label: 'Ultimos 7 dias' },
  { value: '720', label: 'Ultimos 30 dias' },
]

export default function RouteTimeline({
  targetId,
  agentId,
  target,
  office,
}: {
  targetId: number
  agentId: number
  target: TargetSummary
  office: OfficeLocation | null
}) {
  const [hours, setHours] = useState('168')
  const [selectedRunId, setSelectedRunId] = useState<number | null>(null)
  const [localTab, setLocalTab] = useState('resumen')

  const { data: timeline, loading } = usePolling(
    () => api.getRouteTimeline(targetId, agentId, Number(hours)),
    30_000,
    [targetId, agentId, hours],
  )

  const { data: selectedRun } = usePolling(
    () => (selectedRunId !== null ? api.getRunHops(targetId, selectedRunId) : Promise.resolve(null)),
    30_000,
    [targetId, selectedRunId],
  )

  function openSegment(runId: number) {
    setSelectedRunId(runId)
    setLocalTab('detalle')
  }

  if (loading && !timeline) {
    return <Loader size="sm" />
  }

  if (!timeline || timeline.segments.length === 0) {
    return (
      <Text c="dimmed" size="sm">
        Todavia no hay suficiente historial de rutas para este rango.
      </Text>
    )
  }

  return (
    <Tabs value={localTab} onChange={(v) => v && setLocalTab(v)}>
      <Tabs.List>
        <Tabs.Tab value="resumen">Resumen</Tabs.Tab>
        <Tabs.Tab value="detalle" disabled={selectedRunId === null}>
          Detalle
        </Tabs.Tab>
      </Tabs.List>

      <Tabs.Panel value="resumen" pt="sm">
        <Group justify="space-between" mb="sm" wrap="wrap">
          <Text size="sm" c="dimmed">
            {timeline.distinctRouteCount} ruta{timeline.distinctRouteCount === 1 ? '' : 's'} distinta
            {timeline.distinctRouteCount === 1 ? '' : 's'} &mdash; {timeline.flapCount} cambio
            {timeline.flapCount === 1 ? '' : 's'} en el rango
          </Text>
          <Select value={hours} onChange={(v) => v && setHours(v)} w={{ base: '100%', xs: 160 }} data={RANGE_OPTIONS} />
        </Group>
        <Timeline bulletSize={20} lineWidth={2}>
          {timeline.segments.map((s, i) => (
            <Timeline.Item
              key={`${s.representativeRunId}-${i}`}
              bullet={<IconArrowDown size={12} />}
              title={
                <Group gap="xs" wrap="wrap" style={{ cursor: 'pointer' }} onClick={() => openSegment(s.representativeRunId)}>
                  <Text fw={600} size="sm">
                    {s.label}
                  </Text>
                  <Badge size="sm" color={s.isPrimary ? 'teal' : 'orange'} variant="light">
                    {s.isPrimary ? 'primaria' : 'alternativa'}
                  </Badge>
                  <Text size="xs" c="dimmed">
                    {formatDuration(s.startedAtUtc, s.endedAtUtc)}
                    {s.endedAtUtc === null ? ' (en curso)' : ''}
                  </Text>
                </Group>
              }
            >
              <Text size="xs" c="dimmed">
                {formatDateTime(s.startedAtUtc)} &rarr; {s.endedAtUtc ? formatDateTime(s.endedAtUtc) : 'ahora'}
              </Text>
            </Timeline.Item>
          ))}
        </Timeline>
      </Tabs.Panel>

      <Tabs.Panel value="detalle" pt="sm">
        {selectedRun ? (
          <Stack gap="md">
            <RouteMap target={target} hops={selectedRun.hops} office={office} />
            <HopTable hops={selectedRun.hops} />
          </Stack>
        ) : (
          <Loader size="sm" />
        )}
      </Tabs.Panel>
    </Tabs>
  )
}
