import { Badge, Card, Divider, Group, Stack, Text } from '@mantine/core'
import { IconAlertTriangle, IconCheck } from '@tabler/icons-react'
import { useNavigate } from 'react-router-dom'
import type { AgentTraceRun, TargetSummary } from '../api/types'
import { formatRelativeTime, lossColor } from '../utils/format'

const RECENT_CHANGE_MINUTES = 30

function isRecentChange(iso: string | null): boolean {
  return iso !== null && Date.now() - new Date(iso + 'Z').getTime() < RECENT_CHANGE_MINUTES * 60_000
}

function AgentReading({ reading, showAgentName }: { reading: AgentTraceRun; showAgentName: boolean }) {
  const changedRecently = isRecentChange(reading.lastPathChangeAtUtc)

  return (
    <Stack gap={4}>
      {showAgentName && (
        <Text size="xs" fw={600} c="dimmed">
          {reading.agentName}
        </Text>
      )}
      <Group gap="xl">
        <div>
          <Text size="xs" c="dimmed">
            Loss
          </Text>
          <Text fw={700} c={lossColor(reading.overallLossPct)}>
            {reading.overallLossPct.toFixed(1)}%
          </Text>
        </div>
        <div>
          <Text size="xs" c="dimmed">
            Avg RTT
          </Text>
          <Text fw={700}>{reading.overallAvgRttMs.toFixed(1)} ms</Text>
        </div>
      </Group>
      <Group justify="space-between">
        <Text size="xs" c="dimmed">
          ultima traza {formatRelativeTime(reading.startedAtUtc)}
        </Text>
        {changedRecently ? (
          <Badge size="sm" color="orange" leftSection={<IconAlertTriangle size={12} />}>
            ruta cambio {formatRelativeTime(reading.lastPathChangeAtUtc)}
          </Badge>
        ) : (
          <Badge size="sm" color="teal" variant="light" leftSection={<IconCheck size={12} />}>
            ruta estable
          </Badge>
        )}
      </Group>
    </Stack>
  )
}

export default function TargetCard({ target, readings }: { target: TargetSummary; readings: AgentTraceRun[] }) {
  const navigate = useNavigate()

  return (
    <Card withBorder shadow="sm" padding="lg" style={{ cursor: 'pointer' }} onClick={() => navigate(`/targets/${target.id}`)}>
      <Stack gap="xs">
        <Group justify="space-between">
          <Text fw={600}>{target.name}</Text>
          <Badge variant="light">{target.provider}</Badge>
        </Group>
        <Text size="sm" c="dimmed">
          {target.destinationHost}
        </Text>

        {readings.length === 0 ? (
          <Text size="sm" c="dimmed" mt="xs">
            Sin datos todavia
          </Text>
        ) : (
          readings.map((r, i) => (
            <div key={r.agentId}>
              {i > 0 && <Divider my={6} />}
              <AgentReading reading={r} showAgentName={readings.length > 1} />
            </div>
          ))
        )}
      </Stack>
    </Card>
  )
}
