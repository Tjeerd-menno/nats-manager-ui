import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { TextInput, PasswordInput, Button, Paper, Title, Stack, Alert, Center, Text } from '@mantine/core';
import { IconAlertCircle, IconLogin } from '@tabler/icons-react';
import { useAuth } from './useAuth';
import { extractProblemDetails } from '../../api/client';

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      await login({ username, password });
      navigate('/');
    } catch (err) {
      const problem = extractProblemDetails(err);
      setError(problem?.detail ?? 'Login failed. Please check your credentials.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Center mih="100vh" bg="gray.0">
      <Paper shadow="lg" p="xl" radius="md" w={420} withBorder>
        <Stack align="center" mb="lg">
          <Title order={2} c="indigo">NATS Manager</Title>
          <Text size="sm" c="dimmed">Sign in to manage your NATS infrastructure</Text>
        </Stack>
        <form onSubmit={handleSubmit}>
          <Stack>
            {error && (
              <Alert color="red" icon={<IconAlertCircle size={16} />}>
                {error}
              </Alert>
            )}
            <TextInput
              label="Username"
              value={username}
              onChange={(e) => setUsername(e.currentTarget.value)}
              required
              autoFocus
            />
            <PasswordInput
              label="Password"
              value={password}
              onChange={(e) => setPassword(e.currentTarget.value)}
              required
            />
            <Button
              type="submit"
              loading={isSubmitting}
              fullWidth
              leftSection={<IconLogin size={16} />}
            >
              Sign in
            </Button>
          </Stack>
        </form>
      </Paper>
    </Center>
  );
}
