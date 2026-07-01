import { AppShell, Group, Text, Tabs } from '@mantine/core'
import { IconRoute, IconTimeline } from '@tabler/icons-react'
import { Route, Routes, useLocation, useNavigate } from 'react-router-dom'
import Dashboard from './pages/Dashboard'
import TargetDetail from './pages/TargetDetail'
import EventsLog from './pages/EventsLog'

function TopNav() {
  const location = useLocation()
  const navigate = useNavigate()
  const active = location.pathname.startsWith('/events') ? 'events' : 'dashboard'

  return (
    <Tabs
      value={active}
      onChange={(value) => navigate(value === 'events' ? '/events' : '/')}
    >
      <Tabs.List>
        <Tabs.Tab value="dashboard" leftSection={<IconRoute size={16} />}>
          Dashboard
        </Tabs.Tab>
        <Tabs.Tab value="events" leftSection={<IconTimeline size={16} />}>
          Eventos
        </Tabs.Tab>
      </Tabs.List>
    </Tabs>
  )
}

function App() {
  return (
    <AppShell header={{ height: 60 }} padding="md">
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between">
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
        </Routes>
      </AppShell.Main>
    </AppShell>
  )
}

export default App
