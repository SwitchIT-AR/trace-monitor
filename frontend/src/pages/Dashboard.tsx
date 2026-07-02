import { Alert, Center, Loader } from '@mantine/core'
import { api } from '../api/client'
import { usePolling } from '../hooks/usePolling'
import TargetCard from '../components/TargetCard'
import OverviewMap from '../components/OverviewMap'

const SIDEBAR_WIDTH = 300

export default function Dashboard() {
  const { data: targets, error, loading } = usePolling(() => api.getTargets(), 30_000)

  if (loading) {
    return (
      <Center h={200}>
        <Loader />
      </Center>
    )
  }

  if (error) {
    return <Alert color="red" title="No se pudo cargar el dashboard">{error.message}</Alert>
  }

  return (
    <div style={{ display: 'flex', gap: 16, height: 'calc(100vh - 92px)' }}>
      <div style={{ width: SIDEBAR_WIDTH, flexShrink: 0, display: 'flex', flexDirection: 'column', gap: 12 }}>
        {targets?.map((t) => (
          <div key={t.id} style={{ flex: 1, minHeight: 0 }}>
            <TargetCard target={t} />
          </div>
        ))}
      </div>
      <div style={{ flex: 1, minWidth: 0 }}>
        <OverviewMap targets={targets ?? []} />
      </div>
    </div>
  )
}
