import { Badge, Grid, Group, Text, Timeline } from '@mantine/core'
import { IconGitBranch } from '@tabler/icons-react'
import type { HopSnapshot, PathChangeEvent } from '../api/types'
import { formatDateTime } from '../utils/format'

function hopLabel(hop: HopSnapshot): string {
  if (!hop.ip) return '(sin respuesta)'
  return hop.hostname ?? hop.ip
}

function HopList({ hops, changedIndexes }: { hops: HopSnapshot[]; changedIndexes: Set<number> }) {
  return (
    <Text component="div" size="sm">
      {hops.map((h) => (
        <div key={h.hopIndex} style={{ color: changedIndexes.has(h.hopIndex) ? 'var(--mantine-color-orange-5)' : undefined }}>
          #{h.hopIndex} {hopLabel(h)}
        </div>
      ))}
    </Text>
  )
}

function diffIndexes(previous: HopSnapshot[], next: HopSnapshot[]): Set<number> {
  const prevByIndex = new Map(previous.map((h) => [h.hopIndex, h.ip]))
  const changed = new Set<number>()
  for (const hop of next) {
    if (prevByIndex.get(hop.hopIndex) !== hop.ip) changed.add(hop.hopIndex)
  }
  for (const hop of previous) {
    if (!next.some((n) => n.hopIndex === hop.hopIndex)) changed.add(hop.hopIndex)
  }
  return changed
}

export default function PathTimeline({ events, showTarget = false }: { events: PathChangeEvent[]; showTarget?: boolean }) {
  if (events.length === 0) {
    return (
      <Text c="dimmed" size="sm">
        Todavia no se detectaron cambios de ruta.
      </Text>
    )
  }

  return (
    <Timeline active={events.length} bulletSize={22} lineWidth={2}>
      {events.map((event) => {
        const previous: HopSnapshot[] = JSON.parse(event.previousHopsJson)
        const next: HopSnapshot[] = JSON.parse(event.newHopsJson)
        const changed = diffIndexes(previous, next)

        return (
          <Timeline.Item key={event.id} bullet={<IconGitBranch size={12} />} title={
            <Group gap="xs">
              <Text fw={600} size="sm">
                {showTarget ? event.targetName : 'Cambio de ruta'}
              </Text>
              <Badge variant="light" color="orange" size="sm">
                {formatDateTime(event.detectedAtUtc)}
              </Badge>
            </Group>
          }>
            <Grid mt="xs">
              <Grid.Col span={{ base: 12, sm: 6 }}>
                <Text size="xs" c="dimmed" mb={4}>
                  Antes
                </Text>
                <HopList hops={previous} changedIndexes={changed} />
              </Grid.Col>
              <Grid.Col span={{ base: 12, sm: 6 }}>
                <Text size="xs" c="dimmed" mb={4}>
                  Ahora
                </Text>
                <HopList hops={next} changedIndexes={changed} />
              </Grid.Col>
            </Grid>
          </Timeline.Item>
        )
      })}
    </Timeline>
  )
}
