import type { ReactNode } from 'react';
import { Center, Loader, Stack, Text, Title } from '@mantine/core';
import { Navigate } from 'react-router-dom';
import { useAuth } from './useAuth';

interface ProtectedRouteProps {
  children: ReactNode;
  requiredRoles?: string[];
}

export function ProtectedRoute({ children, requiredRoles }: ProtectedRouteProps) {
  const { isAuthenticated, isLoading, hasRole } = useAuth();

  if (isLoading) {
    return (
      <Center h="100vh">
        <Loader size="lg" />
      </Center>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (requiredRoles && !requiredRoles.some(hasRole)) {
    return (
      <Center h="100vh">
        <Stack gap="xs" align="center">
          <Title order={2}>Access denied</Title>
          <Text c="dimmed">Your account does not have permission to view this page.</Text>
        </Stack>
      </Center>
    );
  }

  return <>{children}</>;
}
