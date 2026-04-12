import { AppShell, NavLink, Group, Text, ActionIcon, Divider, Burger, Box, useMantineColorScheme, useComputedColorScheme } from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import {
  IconLayoutDashboard,
  IconServer,
  IconBolt,
  IconKey,
  IconPackage,
  IconSettings,
  IconBroadcast,
  IconFileText,
  IconUser,
  IconLogout,
  IconSun,
  IconMoon,
} from '@tabler/icons-react';
import { useAuth } from '../features/auth/useAuth';
import { EnvironmentSelector } from '../features/environments/components/EnvironmentSelector';
import { useEnvironmentContext } from '../features/environments/EnvironmentContext';
import { GlobalSearch } from '../features/search/SearchPage';

const navItems = [
  { label: 'Dashboard', path: '/dashboard', icon: IconLayoutDashboard },
  { label: 'Environments', path: '/environments', icon: IconServer },
  { label: 'JetStream', path: '/jetstream/streams', icon: IconBolt },
  { label: 'Key-Value', path: '/kv/buckets', icon: IconKey },
  { label: 'Object Store', path: '/objectstore/buckets', icon: IconPackage },
  { label: 'Services', path: '/services', icon: IconSettings },
  { label: 'Core NATS', path: '/core-nats', icon: IconBroadcast },
  { label: 'Audit Log', path: '/audit', icon: IconFileText },
];

const adminNavItems = [
  { label: 'Users', path: '/admin/users', icon: IconUser },
];

export function AppLayout() {
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout, hasRole } = useAuth();
  const { selectedEnvironmentId, selectEnvironment } = useEnvironmentContext();
  const { setColorScheme } = useMantineColorScheme();
  const computedColorScheme = useComputedColorScheme('light', { getInitialValueInEffect: true });
  const [opened, { toggle, close }] = useDisclosure();
  const canViewAudit = hasRole('Administrator') || hasRole('Auditor');
  const visibleNavItems = canViewAudit ? navItems : navItems.filter((item) => item.path !== '/audit');

  const handleNavigate = (path: string) => {
    navigate(path);
    close();
  };

  return (
    <AppShell
      navbar={{ width: 260, breakpoint: 'sm', collapsed: { mobile: !opened } }}
      header={{ height: 60 }}
      padding="md"
    >
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between">
          <Group>
            <Burger opened={opened} onClick={toggle} hiddenFrom="sm" size="sm" aria-label="Toggle navigation" />
            <Text size="lg" fw={700} c="indigo">NATS Manager</Text>
          </Group>
          <Group>
            <GlobalSearch />
            <Text size="sm" c="dimmed">{user?.displayName}</Text>
            <ActionIcon
              variant="subtle"
              color="gray"
              aria-label="Toggle color scheme"
              onClick={() => setColorScheme(computedColorScheme === 'light' ? 'dark' : 'light')}
            >
              {computedColorScheme === 'dark' ? <IconSun size={18} /> : <IconMoon size={18} />}
            </ActionIcon>
            <ActionIcon variant="subtle" color="gray" aria-label="Logout" onClick={() => void logout()}>
              <IconLogout size={18} />
            </ActionIcon>
          </Group>
        </Group>
      </AppShell.Header>

      <AppShell.Navbar p="xs">
        <Box mb="xs">
          <EnvironmentSelector
            selectedId={selectedEnvironmentId}
            onSelect={selectEnvironment}
          />
        </Box>
        <Divider my="xs" />
        {visibleNavItems.map((item) => (
          <NavLink
            key={item.path}
            label={item.label}
            leftSection={<item.icon size={18} stroke={1.5} />}
            active={location.pathname.startsWith(item.path)}
            onClick={() => handleNavigate(item.path)}
          />
        ))}

        {hasRole('Administrator') && (
          <>
            <Divider my="xs" label="Admin" labelPosition="left" />
            {adminNavItems.map((item) => (
              <NavLink
                key={item.path}
                label={item.label}
                leftSection={<item.icon size={18} stroke={1.5} />}
                active={location.pathname.startsWith(item.path)}
                onClick={() => handleNavigate(item.path)}
              />
            ))}
          </>
        )}
      </AppShell.Navbar>

      <AppShell.Main>
        <Outlet />
      </AppShell.Main>
    </AppShell>
  );
}
