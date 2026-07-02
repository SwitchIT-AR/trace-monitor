import { Fragment, useState } from 'react'
import { Alert, Center, Collapse, Group, Loader, Paper, SegmentedControl, Table, Text, Title } from '@mantine/core'
import { IconChevronDown, IconChevronRight } from '@tabler/icons-react'
import { api } from '../api/client'
import { usePolling } from '../hooks/usePolling'
import { lossColor } from '../utils/format'

const WINDOWS = [
  { label: '24hs', value: '24' },
  { label: '7 dias', value: '168' },
]

function HopLossRows({ targetId, agentId, hours }: { targetId: number; agentId: number; hours: number }) {
  const { data: hops, loading } = usePolling(
    () => api.getHopLoss(targetId, agentId, hours),
    30_000,
    [targetId, agentId, hours],
  )

  if (loading) {
    return (
      <Center py="sm">
        <Loader size="xs" />
      </Center>
    )
  }

  if (!hops || hops.length === 0) {
    return (
      <Text size="xs" c="dimmed" py="sm" ta="center">
        Sin saltos registrados en esta ventana.
      </Text>
    )
  }

  const worstLoss = Math.max(...hops.map((h) => h.avgLossPct))

  return (
    <Table verticalSpacing={4} fz="xs">
      <Table.Thead>
        <Table.Tr>
          <Table.Th>Salto</Table.Th>
          <Table.Th>IP / host</Table.Th>
          <Table.Th ta="right">Perdida prom.</Table.Th>
          <Table.Th ta="right">Muestras</Table.Th>
        </Table.Tr>
      </Table.Thead>
      <Table.Tbody>
        {hops.map((h) => (
          <Table.Tr key={`${h.hopIndex}-${h.ip ?? 'na'}`}>
            <Table.Td>{h.hopIndex}</Table.Td>
            <Table.Td>{h.hostname ?? h.ip ?? '???'}</Table.Td>
            <Table.Td ta="right">
              <Text
                span
                fw={h.avgLossPct === worstLoss && worstLoss > 0 ? 700 : 400}
                c={lossColor(h.avgLossPct)}
                style={{ fontVariantNumeric: 'tabular-nums' }}
              >
                {h.avgLossPct.toFixed(1)}%
              </Text>
            </Table.Td>
            <Table.Td ta="right" style={{ fontVariantNumeric: 'tabular-nums' }}>
              {h.sampleCount}
            </Table.Td>
          </Table.Tr>
        ))}
      </Table.Tbody>
    </Table>
  )
}

export default function LossSummaryPanel({ selectedAgentId }: { selectedAgentId: number | null }) {
  const [hours, setHours] = useState(24)
  const [expandedKey, setExpandedKey] = useState<string | null>(null)
  const { data: summary, error, loading } = usePolling(() => api.getLossSummary(hours), 30_000, [hours])

  const rows = (summary ?? []).filter((r) => selectedAgentId === null || r.agentId === selectedAgentId)

  return (
    <Paper withBorder p="md">
      <Group justify="space-between" mb="sm" wrap="wrap">
        <div>
          <Title order={4}>Estadisticas de perdida</Title>
          <Text size="xs" c="dimmed">
            Por agente y destino, ordenado de peor a mejor. Click en una fila para ver el detalle por salto.
          </Text>
        </div>
        <SegmentedControl size="xs" value={String(hours)} onChange={(v) => setHours(Number(v))} data={WINDOWS} />
      </Group>

      {loading ? (
        <Center py="lg">
          <Loader size="sm" />
        </Center>
      ) : error ? (
        <Alert color="red" title="No se pudo cargar las estadisticas">
          {error.message}
        </Alert>
      ) : rows.length === 0 ? (
        <Text c="dimmed" size="sm" ta="center" py="lg">
          Sin corridas registradas en esta ventana.
        </Text>
      ) : (
        <Table verticalSpacing="xs" highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th w={28} />
              <Table.Th>Agente</Table.Th>
              <Table.Th>Destino</Table.Th>
              <Table.Th ta="right">Perdida promedio</Table.Th>
              <Table.Th ta="right">Corridas</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {rows.map((r) => {
              const key = `${r.targetId}-${r.agentId}`
              const expanded = expandedKey === key
              return (
                <Fragment key={key}>
                  <Table.Tr style={{ cursor: 'pointer' }} onClick={() => setExpandedKey(expanded ? null : key)}>
                    <Table.Td>{expanded ? <IconChevronDown size={14} /> : <IconChevronRight size={14} />}</Table.Td>
                    <Table.Td>{r.agentName}</Table.Td>
                    <Table.Td>{r.targetName}</Table.Td>
                    <Table.Td ta="right">
                      <Text span fw={600} c={lossColor(r.avgLossPct)} style={{ fontVariantNumeric: 'tabular-nums' }}>
                        {r.avgLossPct.toFixed(1)}%
                      </Text>
                    </Table.Td>
                    <Table.Td ta="right" style={{ fontVariantNumeric: 'tabular-nums' }}>
                      {r.runCount}
                    </Table.Td>
                  </Table.Tr>
                  <Table.Tr>
                    <Table.Td colSpan={5} p={0}>
                      <Collapse expanded={expanded}>
                        <div style={{ padding: '8px 8px 8px 32px' }}>
                          {expanded && <HopLossRows targetId={r.targetId} agentId={r.agentId} hours={hours} />}
                        </div>
                      </Collapse>
                    </Table.Td>
                  </Table.Tr>
                </Fragment>
              )
            })}
          </Table.Tbody>
        </Table>
      )}
    </Paper>
  )
}
