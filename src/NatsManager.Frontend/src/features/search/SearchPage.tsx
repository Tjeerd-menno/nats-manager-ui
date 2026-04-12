import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  TextInput,
  Text,
  Group,
  Badge,
  Stack,
  ActionIcon,
  UnstyledButton,
  Popover,
  ScrollArea,
  Loader,
} from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { useSearch, useBookmarks, useDeleteBookmark } from './hooks/useSearch';

export function GlobalSearch() {
  const navigate = useNavigate();
  const [query, setQuery] = useState('');
  const [opened, setOpened] = useState(false);
  const [debouncedQuery] = useDebouncedValue(query, 300);
  const { data: results, isLoading } = useSearch(debouncedQuery);

  const handleSelect = (url: string) => {
    setOpened(false);
    setQuery('');
    navigate(url);
  };

  return (
    <Popover opened={opened && query.length >= 2} onChange={setOpened} width={400} position="bottom-start">
      <Popover.Target>
        <TextInput
          placeholder="Search resources..."
          value={query}
          onChange={(e) => {
            setQuery(e.currentTarget.value);
            setOpened(true);
          }}
          onFocus={() => query.length >= 2 && setOpened(true)}
          rightSection={isLoading ? <Loader size="xs" /> : undefined}
          size="sm"
          style={{ width: 300 }}
        />
      </Popover.Target>
      <Popover.Dropdown>
        <ScrollArea.Autosize mah={400}>
          {results && results.length > 0 ? (
            <Stack gap="xs">
              {results.map((r, i) => (
                <UnstyledButton
                  key={`${r.resourceType}-${r.resourceId}-${i}`}
                  onClick={() => handleSelect(r.navigationUrl)}
                  p="xs"
                  style={{ borderRadius: 4 }}
                >
                  <Group justify="space-between">
                    <div>
                      <Text size="sm" fw={500}>{r.name}</Text>
                      {r.description && <Text size="xs" c="dimmed">{r.description}</Text>}
                    </div>
                    <Badge size="sm" variant="light">{r.resourceType}</Badge>
                  </Group>
                </UnstyledButton>
              ))}
            </Stack>
          ) : (
            <Text size="sm" c="dimmed" ta="center" py="md">
              {debouncedQuery.length >= 2 ? 'No results found' : 'Type to search...'}
            </Text>
          )}
        </ScrollArea.Autosize>
      </Popover.Dropdown>
    </Popover>
  );
}

export function BookmarkList() {
  const navigate = useNavigate();
  const { data: bookmarks, isLoading } = useBookmarks();
  const deleteBookmark = useDeleteBookmark();

  if (isLoading) return <Loader size="sm" />;

  if (!bookmarks || bookmarks.length === 0) {
    return <Text size="sm" c="dimmed">No bookmarks yet</Text>;
  }

  return (
    <Stack gap="xs">
      {bookmarks.map((b) => (
        <Group key={b.id} justify="space-between">
          <UnstyledButton onClick={() => navigate(b.navigationUrl)}>
            <Group gap="xs">
              <Text size="sm">{b.displayName}</Text>
              <Badge size="xs" variant="light">{b.resourceType}</Badge>
            </Group>
          </UnstyledButton>
          <ActionIcon
            size="sm"
            variant="subtle"
            color="red"
            onClick={() => deleteBookmark.mutate(b.id)}
            loading={deleteBookmark.isPending}
          >
            ✕
          </ActionIcon>
        </Group>
      ))}
    </Stack>
  );
}

export default function SearchPage() {
  return <GlobalSearch />;
}
