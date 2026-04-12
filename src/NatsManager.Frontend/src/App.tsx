import { lazy, Suspense } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { AppLayout } from './shared/AppLayout';
import { ProtectedRoute } from './features/auth/ProtectedRoute';
import { LoginPage } from './features/auth/LoginPage';
import { LoadingState } from './shared/LoadingState';

const Dashboard = lazy(() => import('./features/dashboard/DashboardPage'));
const Environments = lazy(() => import('./features/environments/EnvironmentsPage'));
const JetStream = lazy(() => import('./features/jetstream/JetStreamPage'));
const Kv = lazy(() => import('./features/kv/KvPage'));
const ObjectStore = lazy(() => import('./features/objectstore/ObjectStorePage'));
const Services = lazy(() => import('./features/services/ServicesPage'));
const CoreNats = lazy(() => import('./features/corenats/CoreNatsPage'));
const Audit = lazy(() => import('./features/audit/AuditPage'));
const Users = lazy(() => import('./features/admin/UsersPage'));

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }
      >
        <Route
          path="/dashboard"
          element={
            <Suspense fallback={<LoadingState />}>
              <Dashboard />
            </Suspense>
          }
        />
        <Route
          path="/environments"
          element={
            <Suspense fallback={<LoadingState />}>
              <Environments />
            </Suspense>
          }
        />
        <Route
          path="/environments/:id"
          element={
            <Suspense fallback={<LoadingState />}>
              <Environments />
            </Suspense>
          }
        />
        <Route
          path="/jetstream/streams"
          element={
            <Suspense fallback={<LoadingState />}>
              <JetStream />
            </Suspense>
          }
        />
        <Route
          path="/jetstream/streams/:streamName"
          element={
            <Suspense fallback={<LoadingState />}>
              <JetStream />
            </Suspense>
          }
        />
        <Route
          path="/jetstream/streams/:streamName/consumers/:consumerName"
          element={
            <Suspense fallback={<LoadingState />}>
              <JetStream />
            </Suspense>
          }
        />
        <Route
          path="/kv/buckets"
          element={
            <Suspense fallback={<LoadingState />}>
              <Kv />
            </Suspense>
          }
        />
        <Route
          path="/kv/buckets/:bucketName"
          element={
            <Suspense fallback={<LoadingState />}>
              <Kv />
            </Suspense>
          }
        />
        <Route
          path="/kv/buckets/:bucketName/keys/:keyName"
          element={
            <Suspense fallback={<LoadingState />}>
              <Kv />
            </Suspense>
          }
        />
        <Route
          path="/objectstore/buckets"
          element={
            <Suspense fallback={<LoadingState />}>
              <ObjectStore />
            </Suspense>
          }
        />
        <Route
          path="/objectstore/buckets/:bucketName"
          element={
            <Suspense fallback={<LoadingState />}>
              <ObjectStore />
            </Suspense>
          }
        />
        <Route
          path="/objectstore/buckets/:bucketName/objects/:objectName"
          element={
            <Suspense fallback={<LoadingState />}>
              <ObjectStore />
            </Suspense>
          }
        />
        <Route
          path="/services"
          element={
            <Suspense fallback={<LoadingState />}>
              <Services />
            </Suspense>
          }
        />
        <Route
          path="/services/:serviceName"
          element={
            <Suspense fallback={<LoadingState />}>
              <Services />
            </Suspense>
          }
        />
        <Route
          path="/core-nats"
          element={
            <Suspense fallback={<LoadingState />}>
              <CoreNats />
            </Suspense>
          }
        />
        <Route
          path="/audit"
          element={
            <ProtectedRoute requiredRoles={['Administrator', 'Auditor']}>
              <Suspense fallback={<LoadingState />}>
                <Audit />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/users"
          element={
            <ProtectedRoute requiredRoles={['Administrator']}>
              <Suspense fallback={<LoadingState />}>
                <Users />
              </Suspense>
            </ProtectedRoute>
          }
        />
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
      </Route>
    </Routes>
  );
}

export default App;
