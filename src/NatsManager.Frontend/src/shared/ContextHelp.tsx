import { Tooltip, ActionIcon } from '@mantine/core';

interface ContextHelpProps {
  term: string;
  description: string;
}

const NATS_CONCEPTS: Record<string, string> = {
  'retention-policy': 'Determines how messages are retained: Limits (size/count/age), Interest (while consumers exist), or WorkQueue (deleted after ack).',
  'ack-policy': 'Controls acknowledgment: None (no ack needed), All (ack implies all prior), Explicit (each message individually).',
  'delivery-policy': 'Where to start delivering: All, Last, New, ByStartSequence, ByStartTime, LastPerSubject.',
  'replay-policy': 'Instant (as fast as possible) or Original (at original publish rate).',
  'storage-type': 'File (persistent to disk) or Memory (faster, not persisted).',
  'discard-policy': 'What to do when stream limits are reached: Old (remove oldest) or New (reject new).',
  'max-deliver': 'Maximum number of delivery attempts before message is considered undeliverable.',
  'ack-wait': 'Time server waits for acknowledgment before redelivery.',
  'kv-bucket': 'A NATS Key-Value store backed by a JetStream stream for persistent key-value data.',
  'object-store': 'A NATS Object Store backed by JetStream for storing large binary objects in chunks.',
};

export function ContextHelp({ term, description }: ContextHelpProps) {
  const tooltip = NATS_CONCEPTS[term] ?? description;

  return (
    <Tooltip label={tooltip} multiline w={300} withArrow>
      <ActionIcon size="xs" variant="subtle" color="gray" aria-label={`Help: ${term}`}>
        ?
      </ActionIcon>
    </Tooltip>
  );
}
