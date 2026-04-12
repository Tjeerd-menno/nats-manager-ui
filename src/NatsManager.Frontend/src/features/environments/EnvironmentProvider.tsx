import { useCallback, useState, type ReactNode } from 'react';
import { EnvironmentContext } from './EnvironmentContext';

const STORAGE_KEY = 'nats-admin:selectedEnvironmentId';

export function EnvironmentProvider({ children }: { children: ReactNode }) {
  const [selectedEnvironmentId, setSelectedEnvironmentId] = useState<string | null>(() => {
    return sessionStorage.getItem(STORAGE_KEY);
  });

  const selectEnvironment = useCallback((id: string | null) => {
    setSelectedEnvironmentId(id);
    if (id) {
      sessionStorage.setItem(STORAGE_KEY, id);
    } else {
      sessionStorage.removeItem(STORAGE_KEY);
    }
  }, []);

  return (
    <EnvironmentContext.Provider value={{ selectedEnvironmentId, selectEnvironment }}>
      {children}
    </EnvironmentContext.Provider>
  );
}
