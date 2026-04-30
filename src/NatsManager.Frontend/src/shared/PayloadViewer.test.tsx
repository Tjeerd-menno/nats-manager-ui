import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';
import { PayloadViewer } from './PayloadViewer';

describe('PayloadViewer', () => {
  it('shows an empty payload message when data is missing', () => {
    renderWithProviders(<PayloadViewer data={null} />);

    expect(screen.getByText('No payload data')).toBeInTheDocument();
  });

  it('detects json payloads and redacts sensitive values', () => {
    renderWithProviders(
      <PayloadViewer data='{"username":"demo","password":"secret","api_key":"abc123"}' />,
    );

    expect(screen.getByText('json')).toBeInTheDocument();
    expect(screen.getByText((_, element) =>
      element?.textContent?.includes('"password": "***REDACTED***"') ?? false,
    )).toBeInTheDocument();
    expect(screen.getByText((_, element) =>
      element?.textContent?.includes('"api_key": "***REDACTED***"') ?? false,
    )).toBeInTheDocument();
    expect(screen.queryByText('secret')).not.toBeInTheDocument();
    expect(screen.queryByText('abc123')).not.toBeInTheDocument();
  });

  it('uses the provided content type and shows truncation details', () => {
    renderWithProviders(<PayloadViewer data="hello world" contentType="text/plain" maxLength={5} />);

    expect(screen.getByText('text/plain')).toBeInTheDocument();
    expect(screen.getByText('Truncated (11 bytes)')).toBeInTheDocument();
    expect(screen.getByText('hello')).toBeInTheDocument();
    expect(screen.queryByText('hello world')).not.toBeInTheDocument();
  });
});
