import { AppShell, NavLink, Group, Text, ActionIcon, Divider } from '@mantine/core';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../features/auth/useAuth';
import { EnvironmentSelector } from '../features/environments/components/EnvironmentSelector';
import { useEnvironmentContext } from '../features/environments/EnvironmentContext';
import { GlobalSearch } from '../features/search/SearchPage';

const navItems = [
  { label: 'Dashboard', path: '/dashboard', icon: '📊' },
  { label: 'Environments', path: '/environments', icon: '🖥️' },
  { label: 'JetStream', path: '/jetstream/streams', icon: '⚡' },
  { label: 'Key-Value', path: '/kv/buckets', icon: '🔑' },
  { label: 'Object Store', path: '/objectstore/buckets', icon: '📦' },
  { label: 'Services', path: '/services', icon: '🔧' },
  { label: 'Core NATS', path: '/core-nats', icon: '📡' },
  { label: 'Audit Log', path: '/audit', icon: '📝' },
];

const adminNavItems = [
  { label: 'Users', path: '/admin/users', icon: '👤' },
];

export function AppLayout() {
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout, hasRole } = useAuth();
  const { selectedEnvironmentId, selectEnvironment } = useEnvironmentContext();
  const canViewAudit = hasRole('Administrator') || hasRole('Auditor');
  const visibleNavItems = canViewAudit ? navItems : navItems.filter((item) => item.path !== '/audit');

  return (
    <AppShell
      navbar={{ width: 250, breakpoint: 'sm' }}
      header={{ height: 60 }}
      padding="md"
    >
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between">
          <Text size="lg" fw={700}>NATS Manager</Text>
          <Group>
            <GlobalSearch />
            <Text size="sm">{user?.displayName}</Text>
            <ActionIcon variant="subtle" aria-label="Logout" onClick={() => void logout()}>
              ↪
            </ActionIcon>
          </Group>
        </Group>
      </AppShell.Header>

      <AppShell.Navbar p="xs">
        <EnvironmentSelector
          selectedId={selectedEnvironmentId}
          onSelect={selectEnvironment}
        />
        <Divider my="sm" />
        {visibleNavItems.map((item) => (
          <NavLink
            key={item.path}
            label={item.label}
            leftSection={item.icon}
            active={location.pathname.startsWith(item.path)}
            onClick={() => navigate(item.path)}
          />
        ))}

        {hasRole('Administrator') && (
          <>
            <NavLink label="Admin" leftSection="⚙️" defaultOpened>
              {adminNavItems.map((item) => (
                <NavLink
                  key={item.path}
                  label={item.label}
                  leftSection={item.icon}
                  active={location.pathname.startsWith(item.path)}
                  onClick={() => navigate(item.path)}
                />
              ))}
            </NavLink>
          </>
        )}
      </AppShell.Navbar>

      <AppShell.Main>
        <Outlet />
      </AppShell.Main>
    </AppShell>
  );
}
