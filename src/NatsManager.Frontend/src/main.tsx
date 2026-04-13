import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { MantineProvider } from '@mantine/core'
import { ModalsProvider } from '@mantine/modals'
import { QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from './features/auth/AuthProvider'
import { EnvironmentProvider } from './features/environments/EnvironmentProvider'
import { queryClient } from './api/queryClient'
import { ErrorBoundary } from './shared/ErrorBoundary'
import { NotificationProvider } from './shared/NotificationProvider'
import { theme } from './theme'
import App from './App'
import '@mantine/core/styles.css'
import '@mantine/notifications/styles.css'

const getResolvedColorScheme = (): 'light' | 'dark' => {
  if (typeof window === 'undefined') {
    return 'light'
  }

  try {
    const storedColorScheme = window.localStorage.getItem('mantine-color-scheme-value')

    if (storedColorScheme === 'light' || storedColorScheme === 'dark') {
      return storedColorScheme
    }
  } catch {
    // Ignore storage access failures in privacy-restricted contexts.
  }

  try {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
  } catch {
    return 'light'
  }
}

const resolvedColorScheme = getResolvedColorScheme()

if (typeof document !== 'undefined') {
  document.documentElement.setAttribute('data-mantine-color-scheme', resolvedColorScheme)
}
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <MantineProvider theme={theme} defaultColorScheme="auto">
      <ModalsProvider>
        <NotificationProvider />
        <QueryClientProvider client={queryClient}>
          <BrowserRouter>
            <ErrorBoundary>
              <AuthProvider>
                <EnvironmentProvider>
                  <App />
                </EnvironmentProvider>
              </AuthProvider>
            </ErrorBoundary>
          </BrowserRouter>
        </QueryClientProvider>
      </ModalsProvider>
    </MantineProvider>
  </StrictMode>,
)
