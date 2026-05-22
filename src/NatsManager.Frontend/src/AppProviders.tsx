import { useLocation } from 'react-router-dom'
import { AuthProvider } from './features/auth/AuthProvider'
import { EnvironmentProvider } from './features/environments/EnvironmentProvider'
import { ErrorBoundary } from './shared/ErrorBoundary'
import App from './App'

export function AppProviders() {
  const location = useLocation()
  const skipCurrentUserBootstrap = location.pathname === '/login'
  const authProviderKey = skipCurrentUserBootstrap ? 'public' : 'protected'

  return (
    <ErrorBoundary>
      <AuthProvider
        key={authProviderKey}
        skipCurrentUserBootstrap={skipCurrentUserBootstrap}
      >
        <EnvironmentProvider>
          <App />
        </EnvironmentProvider>
      </AuthProvider>
    </ErrorBoundary>
  )
}
