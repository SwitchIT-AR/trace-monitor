import { useState } from 'react'
import { Alert, Center, Loader, SegmentedControl } from '@mantine/core'
import { api } from '../api/client'
import { usePolling } from '../hooks/usePolling'
import TargetCard from '../components/TargetCard'
import OverviewMap from '../components/OverviewMap'
import LossSummaryPanel from '../components/LossSummaryPanel'
import type { AgentTraceRun } from '../api/types'

const SIDEBAR_WIDTH = 300

export default function Dashboard() {
  const { data: targets, error, loading } = usePolling(() => api.getTargets(), 30_000)
  const { data: agents } = usePolling(() => api.getAgents(), 30_000)
  const [selectedAgentId, setSelectedAgentId] = useState<number | null>(null)

  const { data: runsByTarget } = usePolling(async () => {
    if (!targets || targets.length === 0) return {}
    const entries = await Promise.all(
      targets.map(async (t) => {
        try {
          return [t.id, await api.getLatestRunsByAgent(t.id)] as const
        } catch {
          return [t.id, []] as const
        }
      }),
    )
    return Object.fromEntries(entries) as Record<number, AgentTraceRun[]>
  }, 30_000, [targets])

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

  const activeAgents = (agents ?? []).filter((a) => a.isActive)

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      {activeAgents.length > 1 && (
        <SegmentedControl
          value={selectedAgentId === null ? 'all' : String(selectedAgentId)}
          onChange={(v) => setSelectedAgentId(v === 'all' ? null : Number(v))}
          data={[{ label: 'Todos', value: 'all' }, ...activeAgents.map((a) => ({ label: a.name, value: String(a.id) }))]}
          style={{ alignSelf: 'flex-start' }}
        />
      )}
      <div style={{ display: 'flex', gap: 16, height: '60vh', minHeight: 420 }}>
        <div
          style={{
            width: SIDEBAR_WIDTH,
            flexShrink: 0,
            display: 'flex',
            flexDirection: 'column',
            gap: 12,
            overflowY: 'auto',
          }}
        >
          {targets?.map((t) => {
            const readings = (runsByTarget?.[t.id] ?? []).filter(
              (r) => selectedAgentId === null || r.agentId === selectedAgentId,
            )
            return <TargetCard key={t.id} target={t} readings={readings} />
          })}
        </div>
        <div style={{ flex: 1, minWidth: 0 }}>
          <OverviewMap
            targets={targets ?? []}
            runsByTarget={runsByTarget}
            agents={agents ?? []}
            selectedAgentId={selectedAgentId}
          />
        </div>
      </div>
      <LossSummaryPanel selectedAgentId={selectedAgentId} />
    </div>
  )
}
