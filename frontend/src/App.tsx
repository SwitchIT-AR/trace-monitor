import { useEffect, useState } from 'react'
import { ActionIcon, AppShell, Burger, Center, Drawer, Group, Loader, NavLink, Stack, Text, Tabs, Tooltip } from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import { IconLogout, IconRoute, IconServer2, IconSettings, IconSparkles, IconTimeline } from '@tabler/icons-react'
import { Route, Routes, useLocation, useNavigate } from 'react-router-dom'
import { api } from './api/client'
import type { MeDto } from './api/types'
import Dashboard from './pages/Dashboard'
import TargetDetail from './pages/TargetDetail'
import EventsLog from './pages/EventsLog'
import Agents from './pages/Agents'
import AiAnalysis from './pages/AiAnalysis'
import Settings from './pages/Settings'
import Login from './pages/Login'

const ROUTES_BY_TAB: Record<string, string> = {
  dashboard: '/',
  events: '/events',
  agents: '/agents',
  ai: '/ai',
  settings: '/settings',
}

function navItems(isAdmin: boolean) {
  return [
    { value: 'dashboard', label: 'Dashboard', icon: IconRoute },
    { value: 'events', label: 'Eventos', icon: IconTimeline },
    { value: 'agents', label: 'Agentes', icon: IconServer2 },
    { value: 'ai', label: 'IA', icon: IconSparkles },
    ...(isAdmin ? [{ value: 'settings', label: 'Settings', icon: IconSettings }] : []),
  ]
}

function useActiveTab(): string {
  const location = useLocation()
  return location.pathname.startsWith('/events')
    ? 'events'
    : location.pathname.startsWith('/agents')
      ? 'agents'
      : location.pathname.startsWith('/ai')
        ? 'ai'
        : location.pathname.startsWith('/settings')
          ? 'settings'
          : 'dashboard'
}

function TopNav({ isAdmin }: { isAdmin: boolean }) {
  const navigate = useNavigate()
  const active = useActiveTab()

  return (
    <Tabs value={active} onChange={(value) => value && navigate(ROUTES_BY_TAB[value])} visibleFrom="sm">
      <Tabs.List>
        {navItems(isAdmin).map((item) => (
          <Tabs.Tab key={item.value} value={item.value} leftSection={<item.icon size={16} />}>
            {item.label}
          </Tabs.Tab>
        ))}
      </Tabs.List>
    </Tabs>
  )
}

function MobileNav({ isAdmin }: { isAdmin: boolean }) {
  const navigate = useNavigate()
  const active = useActiveTab()
  const [opened, { open, close }] = useDisclosure(false)

  return (
    <>
      <Burger opened={opened} onClick={open} hiddenFrom="sm" aria-label="Abrir menu" />
      <Drawer opened={opened} onClose={close} title="Menu" hiddenFrom="sm">
        <Stack gap={4}>
          {navItems(isAdmin).map((item) => (
            <NavLink
              key={item.value}
              label={item.label}
              leftSection={<item.icon size={18} />}
              active={active === item.value}
              onClick={() => {
                navigate(ROUTES_BY_TAB[item.value])
                close()
              }}
            />
          ))}
        </Stack>
      </Drawer>
    </>
  )
}

function AppShellContent({ me, onLogout }: { me: MeDto; onLogout: () => void }) {
  return (
    <AppShell header={{ height: 60 }} padding="md">
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between" wrap="nowrap">
          <Group gap="sm" wrap="nowrap">
            <MobileNav isAdmin={me.isAdmin} />
            <Text fw={700} size="lg">
              Trace Monitor
            </Text>
          </Group>
          <Group gap="sm" wrap="nowrap">
            <TopNav isAdmin={me.isAdmin} />
            <Tooltip label="Cerrar sesion">
              <ActionIcon variant="subtle" onClick={onLogout} aria-label="Cerrar sesion">
                <IconLogout size={18} />
              </ActionIcon>
            </Tooltip>
          </Group>
        </Group>
      </AppShell.Header>

      <AppShell.Main>
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/targets/:id" element={<TargetDetail />} />
          <Route path="/events" element={<EventsLog />} />
          <Route path="/agents" element={<Agents />} />
          <Route path="/ai" element={<AiAnalysis />} />
          {me.isAdmin && <Route path="/settings" element={<Settings />} />}
        </Routes>
      </AppShell.Main>
    </AppShell>
  )
}

function App() {
  const [status, setStatus] = useState<'loading' | 'authed' | 'anon'>('loading')
  const [me, setMe] = useState<MeDto | null>(null)

  useEffect(() => {
    api
      .getMe()
      .then((m) => {
        setMe(m)
        setStatus('authed')
      })
      .catch(() => setStatus('anon'))
  }, [])

  async function handleLogout() {
    await api.logout()
    setMe(null)
    setStatus('anon')
  }

  if (status === 'loading') {
    return (
      <Center h="100vh">
        <Loader />
      </Center>
    )
  }

  if (status === 'anon' || !me) {
    return (
      <Login
        onLoggedIn={(loggedInMe) => {
          setMe(loggedInMe)
          setStatus('authed')
        }}
      />
    )
  }

  return <AppShellContent me={me} onLogout={handleLogout} />
}

export default App
