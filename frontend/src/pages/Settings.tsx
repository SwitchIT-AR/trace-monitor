import { useEffect, useState } from 'react'
import {
  ActionIcon,
  Alert,
  Badge,
  Button,
  Checkbox,
  Group,
  Loader,
  Modal,
  PasswordInput,
  Stack,
  Table,
  Text,
  TextInput,
  Title,
  Tooltip,
} from '@mantine/core'
import { IconKey, IconLock, IconPlus, IconTrash, IconUserPlus } from '@tabler/icons-react'
import { notifications } from '@mantine/notifications'
import { api } from '../api/client'
import { usePolling } from '../hooks/usePolling'
import type { AccessPair, UserSummary } from '../api/types'
import { formatDateTime } from '../utils/format'

function AccessMatrixModal({ user, onClose }: { user: UserSummary | null; onClose: () => void }) {
  const { data: targets } = usePolling(() => api.getTargets(), 60_000)
  const { data: agents } = usePolling(() => api.getAgents(), 60_000)
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!user) return
    setLoading(true)
    api
      .getUserAccess(user.id)
      .then((pairs) => setSelected(new Set(pairs.map((p) => `${p.targetId}:${p.agentId}`))))
      .finally(() => setLoading(false))
  }, [user])

  function toggle(targetId: number, agentId: number) {
    const key = `${targetId}:${agentId}`
    setSelected((prev) => {
      const next = new Set(prev)
      if (next.has(key)) next.delete(key)
      else next.add(key)
      return next
    })
  }

  async function save() {
    if (!user) return
    setSaving(true)
    try {
      const pairs: AccessPair[] = [...selected].map((key) => {
        const [targetId, agentId] = key.split(':').map(Number)
        return { targetId, agentId }
      })
      await api.setUserAccess(user.id, pairs)
      notifications.show({ color: 'teal', title: 'Listo', message: `Permisos de ${user.username} actualizados` })
      onClose()
    } catch (err) {
      notifications.show({ color: 'red', title: 'No se pudo guardar', message: (err as Error).message })
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal opened={user !== null} onClose={onClose} title={`Permisos de ${user?.username ?? ''}`} size="lg" centered>
      {loading || !targets || !agents ? (
        <Group justify="center" py="lg">
          <Loader size="sm" />
        </Group>
      ) : (
        <Stack>
          <Text size="sm" c="dimmed">
            Tildá los pares destino → agente que este usuario puede ver. Sin ningun tilde, no ve nada.
          </Text>
          <div style={{ overflowX: 'auto' }}>
            <Table verticalSpacing={4} fz="sm">
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>Destino</Table.Th>
                  {agents.map((a) => (
                    <Table.Th key={a.id} ta="center">
                      {a.name}
                    </Table.Th>
                  ))}
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {targets.map((t) => (
                  <Table.Tr key={t.id}>
                    <Table.Td>{t.name}</Table.Td>
                    {agents.map((a) => (
                      <Table.Td key={a.id} ta="center">
                        <Checkbox
                          checked={selected.has(`${t.id}:${a.id}`)}
                          onChange={() => toggle(t.id, a.id)}
                          aria-label={`${a.name} -> ${t.name}`}
                        />
                      </Table.Td>
                    ))}
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </div>
          <Button onClick={save} loading={saving}>
            Guardar permisos
          </Button>
        </Stack>
      )}
    </Modal>
  )
}

function ApiKeySection() {
  const { data: key, refetch } = usePolling(() => api.getAnthropicKey(), 60_000)
  const [modalOpen, setModalOpen] = useState(false)
  const [value, setValue] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function save() {
    if (!value.trim()) return
    setSubmitting(true)
    try {
      await api.setAnthropicKey(value.trim())
      setValue('')
      setModalOpen(false)
      await refetch()
      notifications.show({ color: 'teal', title: 'Listo', message: 'API key actualizada' })
    } catch (err) {
      notifications.show({ color: 'red', title: 'No se pudo guardar la key', message: (err as Error).message })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Stack gap="sm">
      <Title order={4}>Clave API de Anthropic</Title>
      <Group wrap="wrap">
        <TextInput readOnly value={key?.masked ?? '(sin configurar)'} styles={{ input: { fontFamily: 'monospace' } }} w={220} />
        <Button leftSection={<IconKey size={16} />} variant="light" onClick={() => setModalOpen(true)}>
          Actualizar
        </Button>
      </Group>

      <Modal opened={modalOpen} onClose={() => setModalOpen(false)} title="Actualizar API key" centered>
        <Stack>
          <PasswordInput
            label="Nueva API key de Anthropic"
            value={value}
            onChange={(e) => setValue(e.currentTarget.value)}
            placeholder="sk-ant-..."
            autoFocus
          />
          <Button onClick={save} loading={submitting} disabled={!value.trim()}>
            Guardar
          </Button>
        </Stack>
      </Modal>
    </Stack>
  )
}

function NewUserModal({ opened, onClose, onCreated }: { opened: boolean; onClose: () => void; onCreated: () => void }) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [isAdmin, setIsAdmin] = useState(false)
  const [submitting, setSubmitting] = useState(false)

  function handleClose() {
    setUsername('')
    setPassword('')
    setIsAdmin(false)
    onClose()
  }

  async function submit() {
    if (!username.trim() || !password.trim()) return
    setSubmitting(true)
    try {
      await api.createUser({ username: username.trim(), password, isAdmin })
      onCreated()
      handleClose()
    } catch (err) {
      notifications.show({ color: 'red', title: 'No se pudo crear el usuario', message: (err as Error).message })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal opened={opened} onClose={handleClose} title="Nuevo usuario" centered>
      <Stack>
        <TextInput label="Usuario" value={username} onChange={(e) => setUsername(e.currentTarget.value)} required autoFocus />
        <PasswordInput label="Contraseña" value={password} onChange={(e) => setPassword(e.currentTarget.value)} required />
        <Checkbox
          label="Administrador (ve todo, gestiona settings/usuarios)"
          checked={isAdmin}
          onChange={(e) => setIsAdmin(e.currentTarget.checked)}
        />
        <Button onClick={submit} loading={submitting} disabled={!username.trim() || !password.trim()}>
          Crear
        </Button>
      </Stack>
    </Modal>
  )
}

function ResetPasswordModal({ userId, onClose, onSaved }: { userId: number | null; onClose: () => void; onSaved: () => void }) {
  const [password, setPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function submit() {
    if (!userId || !password.trim()) return
    setSubmitting(true)
    try {
      await api.resetUserPassword(userId, password)
      setPassword('')
      onSaved()
      onClose()
    } catch (err) {
      notifications.show({ color: 'red', title: 'No se pudo cambiar la contraseña', message: (err as Error).message })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal opened={userId !== null} onClose={onClose} title="Restablecer contraseña" centered>
      <Stack>
        <PasswordInput label="Nueva contraseña" value={password} onChange={(e) => setPassword(e.currentTarget.value)} autoFocus />
        <Button onClick={submit} loading={submitting} disabled={!password.trim()}>
          Guardar
        </Button>
      </Stack>
    </Modal>
  )
}

function UsersSection() {
  const { data: users, refetch } = usePolling(() => api.getUsers(), 30_000)
  const [modalOpen, setModalOpen] = useState(false)
  const [resettingId, setResettingId] = useState<number | null>(null)
  const [deactivatingId, setDeactivatingId] = useState<number | null>(null)
  const [accessUser, setAccessUser] = useState<UserSummary | null>(null)

  async function handleDeactivate(id: number, username: string) {
    if (!window.confirm(`Desactivar el usuario "${username}"? No va a poder loguearse de nuevo.`)) return
    setDeactivatingId(id)
    try {
      await api.deactivateUser(id)
      await refetch()
    } catch (err) {
      notifications.show({ color: 'red', title: 'No se pudo desactivar', message: (err as Error).message })
    } finally {
      setDeactivatingId(null)
    }
  }

  return (
    <Stack gap="sm">
      <Group justify="space-between" wrap="wrap">
        <Title order={4}>Usuarios</Title>
        <Button leftSection={<IconUserPlus size={16} />} onClick={() => setModalOpen(true)}>
          Nuevo usuario
        </Button>
      </Group>

      <div style={{ overflowX: 'auto' }}>
        <Table verticalSpacing="xs" highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Usuario</Table.Th>
              <Table.Th>Rol</Table.Th>
              <Table.Th>Estado</Table.Th>
              <Table.Th>Creado</Table.Th>
              <Table.Th />
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {(users ?? []).map((u) => (
              <Table.Tr key={u.id}>
                <Table.Td>{u.username}</Table.Td>
                <Table.Td>
                  <Badge size="sm" variant="light" color={u.isAdmin ? 'grape' : 'blue'}>
                    {u.isAdmin ? 'admin' : 'usuario'}
                  </Badge>
                </Table.Td>
                <Table.Td>
                  <Badge size="sm" color={u.isActive ? 'teal' : 'gray'}>
                    {u.isActive ? 'activo' : 'inactivo'}
                  </Badge>
                </Table.Td>
                <Table.Td>{formatDateTime(u.createdAtUtc)}</Table.Td>
                <Table.Td>
                  <Group gap={4} justify="flex-end">
                    {!u.isAdmin && (
                      <Tooltip label="Permisos de destinos/agentes">
                        <ActionIcon variant="subtle" onClick={() => setAccessUser(u)}>
                          <IconLock size={16} />
                        </ActionIcon>
                      </Tooltip>
                    )}
                    <Tooltip label="Restablecer contraseña">
                      <ActionIcon variant="subtle" onClick={() => setResettingId(u.id)}>
                        <IconKey size={16} />
                      </ActionIcon>
                    </Tooltip>
                    <Tooltip label="Desactivar">
                      <ActionIcon
                        variant="subtle"
                        color="red"
                        loading={deactivatingId === u.id}
                        disabled={!u.isActive}
                        onClick={() => handleDeactivate(u.id, u.username)}
                      >
                        <IconTrash size={16} />
                      </ActionIcon>
                    </Tooltip>
                  </Group>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      </div>
      {(users ?? []).length === 0 && (
        <Text c="dimmed" size="sm">
          Todavia no hay usuarios cargados.
        </Text>
      )}

      <NewUserModal opened={modalOpen} onClose={() => setModalOpen(false)} onCreated={refetch} />
      <ResetPasswordModal userId={resettingId} onClose={() => setResettingId(null)} onSaved={refetch} />
      <AccessMatrixModal user={accessUser} onClose={() => setAccessUser(null)} />
    </Stack>
  )
}

export default function Settings() {
  return (
    <Stack gap="xl">
      <Title order={3}>Settings</Title>
      <Alert color="blue" variant="light" icon={<IconPlus size={16} />}>
        Solo visible para administradores.
      </Alert>
      <ApiKeySection />
      <UsersSection />
    </Stack>
  )
}
