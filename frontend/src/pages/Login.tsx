import { useState } from 'react'
import { Alert, Button, Center, Paper, PasswordInput, Stack, TextInput, Title } from '@mantine/core'
import { api } from '../api/client'
import type { MeDto } from '../api/types'

export default function Login({ onLoggedIn }: { onLoggedIn: (me: MeDto) => void }) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function submit(e: React.FormEvent) {
    e.preventDefault()
    setLoading(true)
    setError(null)
    try {
      const me = await api.login(username, password)
      onLoggedIn(me)
    } catch {
      setError('Usuario o contraseña incorrectos')
    } finally {
      setLoading(false)
    }
  }

  return (
    <Center h="100vh">
      <Paper withBorder p="xl" w={360}>
        <form onSubmit={submit}>
          <Stack gap="md">
            <Title order={3} ta="center">
              Trace Monitor
            </Title>
            {error && (
              <Alert color="red" title="No se pudo iniciar sesion">
                {error}
              </Alert>
            )}
            <TextInput
              label="Usuario"
              value={username}
              onChange={(e) => setUsername(e.currentTarget.value)}
              required
              autoFocus
            />
            <PasswordInput
              label="Contraseña"
              value={password}
              onChange={(e) => setPassword(e.currentTarget.value)}
              required
            />
            <Button type="submit" loading={loading} fullWidth>
              Ingresar
            </Button>
          </Stack>
        </form>
      </Paper>
    </Center>
  )
}
