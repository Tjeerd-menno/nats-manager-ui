import { useState } from 'react';
import { Title, Table, Badge, Stack, Group, Button, TextInput, PasswordInput, Modal, Loader, Center, ActionIcon, Select, Text } from '@mantine/core';
import { useForm } from '@mantine/form';
import { useUsers, useRoles, useUserRoles, useCreateUser, useDeactivateUser, useAssignRole, useRevokeRole } from './hooks/useAdmin';
import type { Role } from './types';
import { formatDate, formatDateTime } from '../../shared/formatting';
import { validatePassword } from '../../shared/validation';

export default function UsersPage() {
  const { data: users, isLoading } = useUsers();
  const { data: roles } = useRoles();
  const createMutation = useCreateUser();
  const deactivateMutation = useDeactivateUser();
  const [createOpen, setCreateOpen] = useState(false);
  const [selectedUserId, setSelectedUserId] = useState<string | undefined>();
  const createForm = useForm({
    initialValues: {
      username: '',
      displayName: '',
      password: '',
    },
    validate: {
      username: (value) => {
        const trimmed = value.trim();
        if (trimmed.length === 0) return 'Username is required';
        if (trimmed.length > 100) return 'Username must be 100 characters or fewer';
        return null;
      },
      displayName: (value) => {
        const trimmed = value.trim();
        if (trimmed.length === 0) return 'Display name is required';
        if (trimmed.length > 200) return 'Display name must be 200 characters or fewer';
        return null;
      },
      password: validatePassword,
    },
  });

  if (isLoading) return <Center h={200}><Loader /></Center>;

  const handleCreate = createForm.onSubmit((values) => {
    createMutation.mutate({ username: values.username, displayName: values.displayName, password: values.password }, {
      onSuccess: () => { setCreateOpen(false); createForm.reset(); },
    });
  });

  return (
    <Stack>
      <Group justify="space-between">
        <Title order={2}>Users</Title>
        <Button onClick={() => setCreateOpen(true)}>Create User</Button>
      </Group>

      <Table striped highlightOnHover>
        <Table.Thead>
          <Table.Tr>
            <Table.Th>Username</Table.Th>
            <Table.Th>Display Name</Table.Th>
            <Table.Th>Status</Table.Th>
            <Table.Th>Last Login</Table.Th>
            <Table.Th>Actions</Table.Th>
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {users?.map((user) => (
            <Table.Tr key={user.id} style={{ cursor: 'pointer' }} onClick={() => setSelectedUserId(user.id)}>
              <Table.Td>{user.username}</Table.Td>
              <Table.Td>{user.displayName}</Table.Td>
              <Table.Td><Badge color={user.isActive ? 'green' : 'red'}>{user.isActive ? 'Active' : 'Inactive'}</Badge></Table.Td>
              <Table.Td>{formatDateTime(user.lastLoginAt, 'Never')}</Table.Td>
              <Table.Td>
                {user.isActive && <ActionIcon color="red" variant="subtle" onClick={(e) => { e.stopPropagation(); deactivateMutation.mutate(user.id); }}>🚫</ActionIcon>}
              </Table.Td>
            </Table.Tr>
          ))}
        </Table.Tbody>
      </Table>

      {selectedUserId && <UserRoleManager userId={selectedUserId} roles={roles ?? []} onClose={() => setSelectedUserId(undefined)} />}

      <Modal opened={createOpen} onClose={() => setCreateOpen(false)} title="Create User">
        <form onSubmit={handleCreate}>
          <Stack>
            <TextInput label="Username" required {...createForm.getInputProps('username')} />
            <TextInput label="Display Name" required {...createForm.getInputProps('displayName')} />
            <PasswordInput label="Password" required {...createForm.getInputProps('password')} />
            <Button type="submit" loading={createMutation.isPending}>Create</Button>
          </Stack>
        </form>
      </Modal>
    </Stack>
  );
}

function UserRoleManager({ userId, roles, onClose }: { userId: string; roles: Role[]; onClose: () => void }) {
  const { data: userRoles, isLoading } = useUserRoles(userId);
  const assignMutation = useAssignRole();
  const revokeMutation = useRevokeRole();
  const [selectedRole, setSelectedRole] = useState<string | null>(null);

  const handleAssign = () => {
    if (!selectedRole) return;
    assignMutation.mutate({ userId, roleId: selectedRole }, {
      onSuccess: () => setSelectedRole(null),
    });
  };

  return (
    <Modal opened onClose={onClose} title="Manage User Roles" size="lg">
      <Stack>
        {isLoading ? (
          <Center><Loader /></Center>
        ) : (
          <>
            <Table>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>Role</Table.Th>
                  <Table.Th>Scope</Table.Th>
                  <Table.Th>Assigned</Table.Th>
                  <Table.Th>Actions</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {userRoles?.map((userRole) => (
                  <Table.Tr key={userRole.assignmentId}>
                    <Table.Td><Badge>{userRole.roleName}</Badge></Table.Td>
                    <Table.Td>{userRole.environmentId ? `Env: ${userRole.environmentId.slice(0, 8)}...` : 'Global'}</Table.Td>
                    <Table.Td>{formatDate(userRole.assignedAt)}</Table.Td>
                    <Table.Td><ActionIcon color="red" variant="subtle" onClick={() => revokeMutation.mutate({ userId, assignmentId: userRole.assignmentId })}>🗑️</ActionIcon></Table.Td>
                  </Table.Tr>
                ))}
                {userRoles?.length === 0 && <Table.Tr><Table.Td colSpan={4}><Text c="dimmed" ta="center">No roles assigned</Text></Table.Td></Table.Tr>}
              </Table.Tbody>
            </Table>
            <Group>
              <Select placeholder="Select role" data={roles.map(role => ({ value: role.id, label: role.name }))} value={selectedRole} onChange={setSelectedRole} style={{ flex: 1 }} />
              <Button onClick={handleAssign} disabled={!selectedRole} loading={assignMutation.isPending}>Assign</Button>
            </Group>
          </>
        )}
      </Stack>
    </Modal>
  );
}
