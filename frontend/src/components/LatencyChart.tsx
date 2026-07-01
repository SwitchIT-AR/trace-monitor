import { AreaChart } from '@mantine/charts'
import { Stack, Text } from '@mantine/core'
import type { RunHistoryPoint } from '../api/types'

export default function LatencyChart({ history }: { history: RunHistoryPoint[] }) {
  const data = history.map((p) => ({
    time: new Date(p.startedAtUtc.endsWith('Z') ? p.startedAtUtc : p.startedAtUtc + 'Z').toLocaleTimeString('es-AR', {
      hour: '2-digit',
      minute: '2-digit',
    }),
    avgRtt: Number(p.overallAvgRttMs.toFixed(1)),
    loss: Number(p.overallLossPct.toFixed(1)),
  }))

  return (
    <Stack gap="xl">
      <div>
        <Text size="sm" fw={600} mb="xs">
          Latencia promedio (ms)
        </Text>
        <AreaChart
          h={220}
          data={data}
          dataKey="time"
          series={[{ name: 'avgRtt', color: 'cyan.6', label: 'Avg RTT' }]}
          curveType="monotone"
          connectNulls
        />
      </div>
      <div>
        <Text size="sm" fw={600} mb="xs">
          Pérdida de paquetes (%)
        </Text>
        <AreaChart
          h={160}
          data={data}
          dataKey="time"
          series={[{ name: 'loss', color: 'red.6', label: 'Loss %' }]}
          curveType="monotone"
          connectNulls
        />
      </div>
    </Stack>
  )
}
