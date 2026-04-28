import { Stack, Text, Alert, Badge, Group } from '@mantine/core';
import { IconAlertTriangle } from '@tabler/icons-react';
import type { ClusterWarning } from '../types';

interface ClusterWarningListProps {
  warnings: ClusterWarning[];
}

const severityConfig: Record<ClusterWarning['severity'], { color: string }> = {
  Info: { color: 'blue' },
  Warning: { color: 'yellow' },
  Critical: { color: 'red' },
};

export function ClusterWarningList({ warnings }: ClusterWarningListProps) {
  if (warnings.length === 0) {
    return null;
  }

  return (
    <Stack gap="xs">
      {warnings.map((warning) => (
        <Alert
          key={warning.code}
          color={severityConfig[warning.severity].color}
          icon={<IconAlertTriangle size={16} />}
          title={
            <Group gap="xs">
              <Text size="sm" fw={600}>{warning.code}</Text>
              <Badge size="xs" color={severityConfig[warning.severity].color} variant="light">
                {warning.severity}
              </Badge>
            </Group>
          }
        >
          <Text size="sm">{warning.message}</Text>
          {warning.currentValue !== null && warning.thresholdValue !== null && (
            <Text size="xs" c="dimmed" mt={4}>
              Current: {warning.currentValue} / Threshold: {warning.thresholdValue}
            </Text>
          )}
        </Alert>
      ))}
    </Stack>
  );
}
