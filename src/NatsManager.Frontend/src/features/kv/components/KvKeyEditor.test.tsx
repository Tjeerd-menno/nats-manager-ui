import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../test-utils';
import { KvKeyEditor } from './KvKeyEditor';

vi.mock('../hooks/useKv', () => ({
  usePutKvKey: vi.fn(() => ({ mutateAsync: vi.fn(), isPending: false })),
}));

vi.mock('../../environments/EnvironmentContext', () => ({
  useEnvironmentContext: vi.fn(() => ({
    selectedEnvironmentId: 'env-1',
    selectEnvironment: vi.fn(),
  })),
}));

// Mantine Textarea Autosize has a jsdom limitation with ref.addEventListener.
// Mock @mantine/core Textarea with a plain textarea to avoid this.
vi.mock('@mantine/core', async () => {
  const actual = await vi.importActual<typeof import('@mantine/core')>('@mantine/core');
  return {
    ...actual,
    Textarea: (props: Record<string, unknown>) => {
      const { label, ...rest } = props;
      return (
        <div>
          {label && <label>{String(label)}</label>}
          <textarea aria-label={String(label ?? '')} {...rest} />
        </div>
      );
    },
  };
});

describe('KvKeyEditor', () => {
  const defaultProps = {
    opened: true,
    onClose: vi.fn(),
    bucketName: 'config',
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders key editor form when opened', () => {
    renderWithProviders(<KvKeyEditor {...defaultProps} />);
    expect(screen.getByText('Key')).toBeInTheDocument();
    expect(screen.getByText('Value')).toBeInTheDocument();
  });

  it('does not render when closed', () => {
    renderWithProviders(<KvKeyEditor {...defaultProps} opened={false} />);
    expect(screen.queryByText('Key')).not.toBeInTheDocument();
  });
});
