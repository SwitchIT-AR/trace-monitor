import { useEffect, useState } from 'react'
import {
  ActionIcon,
  Alert,
  Badge,
  Button,
  Card,
  Center,
  CopyButton,
  Group,
  Loader,
  Modal,
  NumberInput,
  SimpleGrid,
  Stack,
  Text,
  TextInput,
  Title,
  Tooltip,
} from '@mantine/core'
import { IconCheck, IconCopy, IconMapPinOff, IconPencil, IconPlus, IconServer2, IconTrash } from '@tabler/icons-react'
import { notifications } from '@mantine/notifications'
import { api } from '../api/client'
import { usePolling } from '../hooks/usePolling'
import type { Agent, AgentCreated } from '../api/types'
import { formatRelativeTime } from '../utils/format'

const STALE_MINUTES = 10

function AgentStatusBadge({ agent }: { agent: Agent }) {
  if (!agent.isActive) return <Badge color="gray">inactivo</Badge>

  if (!agent.lastSeenAtUtc) {
    return (
      <Badge color="gray" variant="light">
        sin datos aun
      </Badge>
    )
  }

  const staleMs = Date.now() - new Date(agent.lastSeenAtUtc + 'Z').getTime()
  if (staleMs > STALE_MINUTES * 60_000) {
    return <Badge color="orange">sin novedades</Badge>
  }
  return <Badge color="teal">en linea</Badge>
}

function NewAgentModal({
  opened,
  onClose,
  onCreated,
}: {
  opened: boolean
  onClose: () => void
  onCreated: () => void
}) {
  const [name, setName] = useState('')
  const [location, setLocation] = useState('')
  const [provider, setProvider] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [created, setCreated] = useState<AgentCreated | null>(null)

  const handleClose = () => {
    setName('')
    setLocation('')
    setProvider('')
    setCreated(null)
    onClose()
  }

  const handleSubmit = async () => {
    if (!name.trim() || !location.trim() || !provider.trim()) return
    setSubmitting(true)
    try {
      const result = await api.createAgent({ name, location, provider })
      setCreated(result)
      onCreated()
    } catch (err) {
      notifications.show({ color: 'red', title: 'No se pudo crear el agente', message: (err as Error).message })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal opened={opened} onClose={handleClose} title={created ? 'Agente creado' : 'Nuevo agente'} centered>
      {created ? (
        <Stack>
          <Alert color="yellow" title="Guarda esta clave ahora">
            No se vuelve a mostrar. Es el valor de <code>AGENT_API_KEY</code> en el <code>.env</code> del agente
            remoto.
          </Alert>
          <Group gap="xs" wrap="nowrap">
            <TextInput value={created.apiKey} readOnly flex={1} styles={{ input: { fontFamily: 'monospace' } }} />
            <CopyButton value={created.apiKey}>
              {({ copied, copy }) => (
                <Tooltip label={copied ? 'Copiada' : 'Copiar'}>
                  <ActionIcon variant="light" onClick={copy} color={copied ? 'teal' : undefined}>
                    {copied ? <IconCheck size={16} /> : <IconCopy size={16} />}
                  </ActionIcon>
                </Tooltip>
              )}
            </CopyButton>
          </Group>
          <Button onClick={handleClose}>Listo</Button>
        </Stack>
      ) : (
        <Stack>
          <TextInput
            label="Nombre"
            placeholder="Sucursal X"
            value={name}
            onChange={(e) => setName(e.currentTarget.value)}
            required
          />
          <TextInput
            label="Ubicacion"
            placeholder="Ciudad, Provincia"
            value={location}
            onChange={(e) => setLocation(e.currentTarget.value)}
            required
          />
          <TextInput
            label="Proveedor (ISP)"
            placeholder="Fibertel"
            value={provider}
            onChange={(e) => setProvider(e.currentTarget.value)}
            required
          />
          <Text size="xs" c="dimmed">
            El marcador de origen en el mapa se completa solo (por IP) en el primer reporte del
            agente — no hace falta cargarlo aca.
          </Text>
          <Button
            onClick={handleSubmit}
            loading={submitting}
            disabled={!name.trim() || !location.trim() || !provider.trim()}
          >
            Crear
          </Button>
        </Stack>
      )}
    </Modal>
  )
}

function EditAgentModal({
  agent,
  onClose,
  onSaved,
}: {
  agent: Agent | null
  onClose: () => void
  onSaved: () => void
}) {
  const [name, setName] = useState('')
  const [location, setLocation] = useState('')
  const [provider, setProvider] = useState('')
  const [lat, setLat] = useState<number | string>('')
  const [lon, setLon] = useState<number | string>('')
  const [address, setAddress] = useState('')
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    setName(agent?.name ?? '')
    setLocation(agent?.location ?? '')
    setProvider(agent?.provider ?? '')
    setLat(agent?.lat ?? '')
    setLon(agent?.lon ?? '')
    setAddress(agent?.address ?? '')
  }, [agent])

  const handleSubmit = async () => {
    if (!agent || !name.trim() || !location.trim() || !provider.trim()) return
    setSubmitting(true)
    try {
      await api.updateAgent(agent.id, {
        name,
        location,
        provider,
        lat: lat === '' ? null : Number(lat),
        lon: lon === '' ? null : Number(lon),
        address: address.trim() || null,
      })
      onSaved()
      onClose()
    } catch (err) {
      notifications.show({ color: 'red', title: 'No se pudo actualizar el agente', message: (err as Error).message })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal opened={agent !== null} onClose={onClose} title={`Editar ${agent?.name ?? ''}`} centered>
      <Stack>
        <TextInput label="Nombre" value={name} onChange={(e) => setName(e.currentTarget.value)} required />
        <TextInput
          label="Ubicacion"
          value={location}
          onChange={(e) => setLocation(e.currentTarget.value)}
          required
        />
        <TextInput
          label="Proveedor (ISP)"
          value={provider}
          onChange={(e) => setProvider(e.currentTarget.value)}
          required
        />
        <Text size="sm" c="dimmed" mt="xs">
          Coordenadas de origen en el mapa de rutas. Se completan solas con la IP del primer
          reporte; dejalas vacias y guarda para sacar el marcador.
        </Text>
        <NumberInput label="Latitud" placeholder="-34.6037" value={lat} onChange={setLat} decimalScale={6} />
        <NumberInput label="Longitud" placeholder="-58.3816" value={lon} onChange={setLon} decimalScale={6} />
        <TextInput
          label="Direccion (opcional)"
          placeholder="Av. Corrientes 1234, CABA"
          value={address}
          onChange={(e) => setAddress(e.currentTarget.value)}
        />
        <Button
          onClick={handleSubmit}
          loading={submitting}
          disabled={!name.trim() || !location.trim() || !provider.trim()}
        >
          Guardar
        </Button>
      </Stack>
    </Modal>
  )
}

function AgentCard({
  agent,
  onEdit,
  onDeactivate,
  deactivating,
}: {
  agent: Agent
  onEdit: () => void
  onDeactivate: () => void
  deactivating: boolean
}) {
  const missingLocation = !agent.isBuiltIn && agent.lat == null

  return (
    <Card withBorder shadow="sm" padding="lg">
      <Stack gap="xs">
        <Group justify="space-between" wrap="nowrap">
          <Group gap={6} wrap="nowrap">
            <IconServer2 size={16} opacity={0.6} />
            <Text fw={600}>{agent.name}</Text>
            {agent.isBuiltIn && (
              <Badge size="xs" variant="light">
                oficina
              </Badge>
            )}
          </Group>
          <Badge variant="light">{agent.provider}</Badge>
        </Group>

        <Text size="sm" c="dimmed">
          {agent.location}
        </Text>
        {agent.address && (
          <Text size="xs" c="dimmed">
            {agent.address}
          </Text>
        )}
        {missingLocation && (
          <Group gap={4}>
            <IconMapPinOff size={14} color="var(--mantine-color-orange-6)" />
            <Text size="xs" c="orange">
              todavia sin ubicacion: no muestra marcador de origen en el mapa
            </Text>
          </Group>
        )}

        <Group justify="space-between" mt="xs">
          <Text size="xs" c="dimmed">
            ultima vez visto {formatRelativeTime(agent.lastSeenAtUtc)}
          </Text>
          <AgentStatusBadge agent={agent} />
        </Group>

        {!agent.isBuiltIn && (
          <Group justify="flex-end" gap={4} mt="xs">
            <Tooltip label="Editar agente">
              <ActionIcon variant="subtle" onClick={onEdit}>
                <IconPencil size={16} />
              </ActionIcon>
            </Tooltip>
            <Tooltip label="Desactivar">
              <ActionIcon variant="subtle" color="red" loading={deactivating} onClick={onDeactivate}>
                <IconTrash size={16} />
              </ActionIcon>
            </Tooltip>
          </Group>
        )}
      </Stack>
    </Card>
  )
}

export default function Agents() {
  const { data: agents, error, loading, refetch } = usePolling(() => api.getAgents(), 15_000)
  const [modalOpen, setModalOpen] = useState(false)
  const [editingAgent, setEditingAgent] = useState<Agent | null>(null)
  const [deactivatingId, setDeactivatingId] = useState<number | null>(null)

  const handleDeactivate = async (agent: Agent) => {
    if (!window.confirm(`Desactivar el agente "${agent.name}"? Va a dejar de poder reportar trazas.`)) return

    setDeactivatingId(agent.id)
    try {
      await api.deactivateAgent(agent.id)
      await refetch()
    } catch (err) {
      notifications.show({ color: 'red', title: 'No se pudo desactivar', message: (err as Error).message })
    } finally {
      setDeactivatingId(null)
    }
  }

  if (loading) {
    return (
      <Center h={200}>
        <Loader />
      </Center>
    )
  }

  if (error) {
    return (
      <Alert color="red" title="No se pudo cargar los agentes">
        {error.message}
      </Alert>
    )
  }

  return (
    <Stack gap="lg">
      <Group justify="space-between">
        <div>
          <Title order={3}>Agentes</Title>
          <Text c="dimmed" size="sm">
            Sondas que corren mtr desde distintas ubicaciones/ISPs y reportan al backend central.
          </Text>
        </div>
        <Button leftSection={<IconPlus size={16} />} onClick={() => setModalOpen(true)}>
          Nuevo agente
        </Button>
      </Group>

      {agents?.length === 0 ? (
        <Text c="dimmed" size="sm" ta="center" py="lg">
          Todavia no hay agentes dados de alta.
        </Text>
      ) : (
        <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }}>
          {agents?.map((agent) => (
            <AgentCard
              key={agent.id}
              agent={agent}
              onEdit={() => setEditingAgent(agent)}
              onDeactivate={() => handleDeactivate(agent)}
              deactivating={deactivatingId === agent.id}
            />
          ))}
        </SimpleGrid>
      )}

      <NewAgentModal opened={modalOpen} onClose={() => setModalOpen(false)} onCreated={refetch} />
      <EditAgentModal agent={editingAgent} onClose={() => setEditingAgent(null)} onSaved={refetch} />
    </Stack>
  )
}
