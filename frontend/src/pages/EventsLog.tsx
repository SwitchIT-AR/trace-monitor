import { Alert, Center, Loader, Paper, Title } from '@mantine/core'
import { api } from '../api/client'
import { usePolling } from '../hooks/usePolling'
import PathTimeline from '../components/PathTimeline'

export default function EventsLog() {
  const { data: events, error, loading } = usePolling(() => api.getAllEvents(), 30_000)

  if (loading) {
    return (
      <Center h={200}>
        <Loader />
      </Center>
    )
  }

  if (error) {
    return <Alert color="red" title="No se pudo cargar los eventos">{error.message}</Alert>
  }

  return (
    <>
      <Title order={3} mb="md">
        Cambios de ruta detectados
      </Title>
      <Paper withBorder p="md">
        <PathTimeline events={events ?? []} showTarget />
      </Paper>
    </>
  )
}
