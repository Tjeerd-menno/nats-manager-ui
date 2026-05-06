import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../../../test-utils';
import { OpenRelationshipMapButton } from './OpenRelationshipMapButton';

const navigate = vi.fn();

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: vi.fn(() => navigate),
  };
});

describe('OpenRelationshipMapButton', () => {
  beforeEach(() => {
    navigate.mockReset();
  });

  it('navigates to the environment-scoped relationship map for the selected resource', async () => {
    const user = userEvent.setup();

    renderWithProviders(
      <OpenRelationshipMapButton
        environmentId="env-1"
        resourceType="Stream"
        resourceId="orders"
      />,
    );

    await user.click(screen.getByRole('button', { name: 'View Relationships' }));

    expect(navigate).toHaveBeenCalledWith(
      '/environments/env-1/relationships?resourceType=Stream&resourceId=orders',
    );
  });
});
