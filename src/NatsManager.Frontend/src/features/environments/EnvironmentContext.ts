import { createContext, useContext } from 'react';

export interface EnvironmentContextValue {
  selectedEnvironmentId: string | null;
  selectEnvironment: (id: string | null) => void;
}

export const EnvironmentContext = createContext<EnvironmentContextValue | null>(null);

export function useEnvironmentContext() {
  const context = useContext(EnvironmentContext);
  if (!context) {
    throw new Error('useEnvironmentContext must be used within an EnvironmentProvider');
  }

  return context;
}
