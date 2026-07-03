import { Table, Text } from '@mantine/core'
import type { Hop } from '../api/types'
import { lossColor } from '../utils/format'

export default function HopTable({ hops }: { hops: Hop[] }) {
  return (
    <div style={{ overflowX: 'auto' }}>
      <Table striped highlightOnHover>
        <Table.Thead>
          <Table.Tr>
            <Table.Th>#</Table.Th>
            <Table.Th>Host</Table.Th>
            <Table.Th>Loss%</Table.Th>
            <Table.Th>Snt</Table.Th>
            <Table.Th>Last</Table.Th>
            <Table.Th>Avg</Table.Th>
            <Table.Th>Best</Table.Th>
            <Table.Th>Wrst</Table.Th>
            <Table.Th>StDev</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {hops.map((hop) => (
            <Table.Tr key={hop.hopIndex}>
              <Table.Td>{hop.hopIndex}</Table.Td>
              <Table.Td>
                {hop.ip ? (
                  <>
                    <Text size="sm">{hop.hostname ?? hop.ip}</Text>
                    {hop.hostname && (
                      <Text size="xs" c="dimmed">
                        {hop.ip}
                      </Text>
                    )}
                  </>
                ) : (
                  <Text size="sm" c="dimmed">
                    (waiting for reply)
                  </Text>
                )}
              </Table.Td>
              <Table.Td>
                <Text c={lossColor(hop.lossPct)}>{hop.lossPct.toFixed(1)}</Text>
              </Table.Td>
              <Table.Td>{hop.sent}</Table.Td>
              <Table.Td>{hop.last.toFixed(1)}</Table.Td>
              <Table.Td>{hop.avg.toFixed(1)}</Table.Td>
              <Table.Td>{hop.best.toFixed(1)}</Table.Td>
              <Table.Td>{hop.worst.toFixed(1)}</Table.Td>
              <Table.Td>{hop.stDev.toFixed(1)}</Table.Td>
            </Table.Tr>
          ))}
        </Table.Tbody>
      </Table>
    </div>
  )
}
