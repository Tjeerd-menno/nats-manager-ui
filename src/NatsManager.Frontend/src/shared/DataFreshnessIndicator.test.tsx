import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';
import { DataFreshnessIndicator } from './DataFreshnessIndicator';

describe('DataFreshnessIndicator', () => {
  it('renders the freshness label', () => {
    renderWithProviders(<DataFreshnessIndicator freshness="live" />);

    expect(screen.getByText('live')).toBeInTheDocument();
  });

  it('adds last updated details when a timestamp is supplied', () => {
    const timestamp = '2026-01-02T03:04:05Z';

    renderWithProviders(<DataFreshnessIndicator freshness="stale" timestamp={timestamp} />);

    expect(screen.getByTitle(`Last updated: ${new Date(timestamp).toLocaleString()}`)).toBeInTheDocument();
  });
});
