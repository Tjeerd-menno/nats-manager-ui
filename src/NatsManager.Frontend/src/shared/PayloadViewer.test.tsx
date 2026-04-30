import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';
import { PayloadViewer } from './PayloadViewer';

describe('PayloadViewer', () => {
  it('shows an empty payload message when data is missing', () => {
    renderWithProviders(<PayloadViewer data={null} />);

    expect(screen.getByText('No payload data')).toBeInTheDocument();
  });

  it('detects json payloads and redacts sensitive values', () => {
    const { container } = renderWithProviders(
      <PayloadViewer data='{"username":"demo","password":"secret","api_key":"abc123"}' />,
    );

    expect(screen.getByText('json')).toBeInTheDocument();
    expect(container).toHaveTextContent('"password": "***REDACTED***"');
    expect(container).toHaveTextContent('"api_key": "***REDACTED***"');
    expect(container).not.toHaveTextContent('secret');
    expect(container).not.toHaveTextContent('abc123');
  });

  it('uses the provided content type and shows truncation details', () => {
    renderWithProviders(<PayloadViewer data="hello world" contentType="text/plain" maxLength={5} />);

    expect(screen.getByText('text/plain')).toBeInTheDocument();
    expect(screen.getByText('Truncated (11 bytes)')).toBeInTheDocument();
    expect(screen.getByText('hello')).toBeInTheDocument();
    expect(screen.queryByText('hello world')).not.toBeInTheDocument();
  });
});
