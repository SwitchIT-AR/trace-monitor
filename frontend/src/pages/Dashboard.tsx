import { Alert, Center, Loader, SimpleGrid, Title } from '@mantine/core'
import { api } from '../api/client'
import { usePolling } from '../hooks/usePolling'
import TargetCard from '../components/TargetCard'

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
    <>
      <Title order={3} mb="md">
        Destinos monitoreados
      </Title>
      <SimpleGrid cols={{ base: 1, sm: 2, lg: 4 }}>
        {targets?.map((t) => (
          <TargetCard key={t.id} target={t} />
        ))}
      </SimpleGrid>
    </>
  )
}
