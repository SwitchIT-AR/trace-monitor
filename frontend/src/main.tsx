import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { MantineProvider, createTheme } from '@mantine/core'
import { Notifications } from '@mantine/notifications'

import '@mantine/core/styles.css'
import '@mantine/charts/styles.css'
import '@mantine/notifications/styles.css'
import 'leaflet/dist/leaflet.css'
import './index.css'

import App from './App.tsx'

const theme = createTheme({
  primaryColor: 'cyan',
  defaultRadius: 'md',
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <MantineProvider theme={theme} defaultColorScheme="dark">
      <Notifications position="top-right" />
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </MantineProvider>
  </StrictMode>,
)
