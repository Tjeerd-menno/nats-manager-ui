import { useCallback, useEffect, useState, type ReactNode } from 'react';
import { EnvironmentContext } from './EnvironmentContext';
import { writeSelectedEnvironment } from '../../read-model/sync/environment-status-sync';

const STORAGE_KEY = 'nats-admin:selectedEnvironmentId';

export function EnvironmentProvider({ children }: { children: ReactNode }) {
  const [selectedEnvironmentId, setSelectedEnvironmentId] = useState<string | null>(() => sessionStorage.getItem(STORAGE_KEY));

  useEffect(() => {
    writeSelectedEnvironment(selectedEnvironmentId, new Date().toISOString());
  }, [selectedEnvironmentId]);

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
