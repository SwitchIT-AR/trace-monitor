import { Badge, Card, Group, Stack, Text } from '@mantine/core'
import { IconAlertTriangle, IconCheck } from '@tabler/icons-react'
import { useNavigate } from 'react-router-dom'
import type { TargetSummary } from '../api/types'
import { formatRelativeTime, lossColor } from '../utils/format'

const RECENT_CHANGE_MINUTES = 30

export default function TargetCard({ target }: { target: TargetSummary }) {
  const navigate = useNavigate()

  const changedRecently =
    target.lastPathChangeAtUtc !== null &&
    Date.now() - new Date(target.lastPathChangeAtUtc + 'Z').getTime() < RECENT_CHANGE_MINUTES * 60_000

  return (
    <Card
      withBorder
      shadow="sm"
      padding="lg"
      style={{ cursor: 'pointer' }}
      onClick={() => navigate(`/targets/${target.id}`)}
    >
      <Stack gap="xs">
        <Group justify="space-between">
          <Text fw={600}>{target.name}</Text>
          <Badge variant="light">{target.provider}</Badge>
        </Group>
        <Text size="sm" c="dimmed">
          {target.destinationHost}
        </Text>

        <Group gap="xl" mt="xs">
          <div>
            <Text size="xs" c="dimmed">
              Loss
            </Text>
            <Text fw={700} c={target.lastLossPct !== null ? lossColor(target.lastLossPct) : 'dimmed'}>
              {target.lastLossPct !== null ? `${target.lastLossPct.toFixed(1)}%` : '-'}
            </Text>
          </div>
          <div>
            <Text size="xs" c="dimmed">
              Avg RTT
            </Text>
            <Text fw={700}>
              {target.lastAvgRttMs !== null ? `${target.lastAvgRttMs.toFixed(1)} ms` : '-'}
            </Text>
          </div>
        </Group>

        <Group justify="space-between" mt="xs">
          <Text size="xs" c="dimmed">
            ultima traza {formatRelativeTime(target.lastRunAtUtc)}
          </Text>
          {changedRecently ? (
            <Badge color="orange" leftSection={<IconAlertTriangle size={12} />}>
              ruta cambio {formatRelativeTime(target.lastPathChangeAtUtc)}
            </Badge>
          ) : (
            <Badge color="teal" variant="light" leftSection={<IconCheck size={12} />}>
              ruta estable
            </Badge>
          )}
        </Group>
      </Stack>
    </Card>
  )
}
