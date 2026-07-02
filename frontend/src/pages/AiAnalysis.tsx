import { useState } from 'react'
import { Alert, Badge, Button, Group, Loader, Paper, Stack, Text, Title } from '@mantine/core'
import { IconSparkles } from '@tabler/icons-react'
import { api } from '../api/client'
import type { AiAnalysisResult } from '../api/types'
import { formatDateTime } from '../utils/format'

export default function AiAnalysis() {
  const [result, setResult] = useState<AiAnalysisResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<Error | null>(null)

  async function runAnalysis() {
    setLoading(true)
    setError(null)
    try {
      setResult(await api.runAiAnalysis())
    } catch (e) {
      setError(e as Error)
    } finally {
      setLoading(false)
    }
  }

  return (
    <Stack gap="lg">
      <Title order={3}>Analisis con IA</Title>
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
    </Stack>
  )
}
