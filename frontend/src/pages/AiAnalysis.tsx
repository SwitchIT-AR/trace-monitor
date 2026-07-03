import { useState } from 'react'
import { Alert, Badge, Button, Group, Loader, Paper, Stack, Tabs, Text, Title } from '@mantine/core'
import { IconSparkles } from '@tabler/icons-react'
import { api } from '../api/client'
import { usePolling } from '../hooks/usePolling'
import type { AiAnalysisReportSummary, AiAnalysisResult } from '../api/types'
import { formatDateTime } from '../utils/format'

function NewAnalysisTab({ onGenerated }: { onGenerated: () => void }) {
  const [result, setResult] = useState<AiAnalysisResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<Error | null>(null)

  async function runAnalysis() {
    setLoading(true)
    setError(null)
    try {
      setResult(await api.runAiAnalysis())
      onGenerated()
    } catch (e) {
      setError(e as Error)
    } finally {
      setLoading(false)
    }
  }

  return (
    <Paper withBorder p="md">
      <Group justify="space-between" mb="sm" wrap="wrap">
        <Text c="dimmed" size="sm">
          Le pide a Claude que revise el estado de la red (perdida, estabilidad de rutas, cambios recientes) como
          un ingeniero de redes y ciberseguridad.
        </Text>
        <Button leftSection={<IconSparkles size={16} />} onClick={runAnalysis} loading={loading}>
          Analizar red
        </Button>
      </Group>

      {loading && (
        <Group justify="center" py="lg">
          <Loader size="sm" />
        </Group>
      )}

      {error && (
        <Alert color="red" title="No se pudo generar el analisis">
          {error.message}
        </Alert>
      )}

      {result && (
        <Stack gap="xs">
          <Badge variant="light" size="sm" style={{ alignSelf: 'flex-start' }}>
            {result.modelUsed} &middot; {formatDateTime(result.generatedAtUtc)}
          </Badge>
          <Text style={{ whiteSpace: 'pre-wrap' }}>{result.analysisText}</Text>
        </Stack>
      )}
    </Paper>
  )
}

function HistoryTab({
  reports,
  loading,
  error,
  refetch,
}: {
  reports: AiAnalysisReportSummary[] | null
  loading: boolean
  error: Error | null
  refetch: () => void
}) {
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const { data: selected } = usePolling(
    () => (selectedId !== null ? api.getAiReport(selectedId) : Promise.resolve(null)),
    60_000,
    [selectedId],
  )

  return (
    <Stack gap="md">
      <Paper withBorder p="md">
        {loading ? (
          <Group justify="center" py="lg">
            <Loader size="sm" />
          </Group>
        ) : error ? (
          <Alert color="red" title="No se pudo cargar el historial">
            {error.message}
          </Alert>
        ) : !reports || reports.length === 0 ? (
          <Text c="dimmed" size="sm">
            Todavia no hay analisis generados.
          </Text>
        ) : (
          <Stack gap={4}>
            {reports.map((r) => (
              <Group
                key={r.id}
                justify="space-between"
                wrap="wrap"
                p="xs"
                style={{
                  cursor: 'pointer',
                  borderRadius: 4,
                  background: selectedId === r.id ? 'var(--mantine-color-dark-5)' : undefined,
                }}
                onClick={() => setSelectedId(r.id)}
              >
                <Text size="sm">{formatDateTime(r.generatedAtUtc)}</Text>
                <Badge variant="light" size="sm">
                  {r.modelUsed}
                </Badge>
              </Group>
            ))}
          </Stack>
        )}
        <Button variant="subtle" size="xs" mt="sm" onClick={() => refetch()}>
          Actualizar lista
        </Button>
      </Paper>

      {selectedId !== null && (
        <Paper withBorder p="md">
          {selected ? (
            <Stack gap="xs">
              <Badge variant="light" size="sm" style={{ alignSelf: 'flex-start' }}>
                {selected.modelUsed} &middot; {formatDateTime(selected.generatedAtUtc)}
              </Badge>
              <Text style={{ whiteSpace: 'pre-wrap' }}>{selected.analysisText}</Text>
            </Stack>
          ) : (
            <Group justify="center" py="lg">
              <Loader size="sm" />
            </Group>
          )}
        </Paper>
      )}
    </Stack>
  )
}

export default function AiAnalysis() {
  const [tab, setTab] = useState('nuevo')
  const { data: reports, loading, error, refetch } = usePolling(() => api.getAiReports(20), 60_000)

  return (
    <Stack gap="lg">
      <Title order={3}>Analisis con IA</Title>
      <Tabs value={tab} onChange={(v) => v && setTab(v)}>
        <Tabs.List>
          <Tabs.Tab value="nuevo">Nuevo analisis</Tabs.Tab>
          <Tabs.Tab value="historial">Historial</Tabs.Tab>
        </Tabs.List>

        <Tabs.Panel value="nuevo" pt="md">
          <NewAnalysisTab onGenerated={refetch} />
        </Tabs.Panel>

        <Tabs.Panel value="historial" pt="md">
          <HistoryTab reports={reports} loading={loading} error={error} refetch={refetch} />
        </Tabs.Panel>
      </Tabs>
    </Stack>
  )
}
