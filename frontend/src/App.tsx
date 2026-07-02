import { AppShell, Group, Text, Tabs } from '@mantine/core'
import { IconRoute, IconServer2, IconSparkles, IconTimeline } from '@tabler/icons-react'
import { Route, Routes, useLocation, useNavigate } from 'react-router-dom'
import Dashboard from './pages/Dashboard'
import TargetDetail from './pages/TargetDetail'
import EventsLog from './pages/EventsLog'
import Agents from './pages/Agents'
import AiAnalysis from './pages/AiAnalysis'

const ROUTES_BY_TAB: Record<string, string> = { dashboard: '/', events: '/events', agents: '/agents', ai: '/ai' }

function TopNav() {
  const location = useLocation()
  const navigate = useNavigate()
  const active = location.pathname.startsWith('/events')
    ? 'events'
    : location.pathname.startsWith('/agents')
      ? 'agents'
      : location.pathname.startsWith('/ai')
        ? 'ai'
        : 'dashboard'

  return (
    <Tabs value={active} onChange={(value) => value && navigate(ROUTES_BY_TAB[value])}>
      <Tabs.List>
        <Tabs.Tab value="dashboard" leftSection={<IconRoute size={16} />}>
          Dashboard
        </Tabs.Tab>
        <Tabs.Tab value="events" leftSection={<IconTimeline size={16} />}>
          Eventos
        </Tabs.Tab>
        <Tabs.Tab value="agents" leftSection={<IconServer2 size={16} />}>
          Agentes
        </Tabs.Tab>
        <Tabs.Tab value="ai" leftSection={<IconSparkles size={16} />}>
          IA
        </Tabs.Tab>
      </Tabs.List>
    </Tabs>
  )
}

function App() {
  return (
    <AppShell header={{ height: { base: 90, sm: 60 } }} padding="md">
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between" wrap="wrap">
          <Text fw={700} size="lg">
            Trace Monitor
          </Text>
          <TopNav />
        </Group>
      </AppShell.Header>

      <AppShell.Main>
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/targets/:id" element={<TargetDetail />} />
          <Route path="/events" element={<EventsLog />} />
          <Route path="/agents" element={<Agents />} />
          <Route path="/ai" element={<AiAnalysis />} />
        </Routes>
      </AppShell.Main>
    </AppShell>
  )
}

export default App
