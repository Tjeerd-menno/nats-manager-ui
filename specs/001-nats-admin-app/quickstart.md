# Quickstart: NATS Admin Application

**Branch**: `001-nats-admin-app`
**Date**: 2026-04-06

---

## Prerequisites

- .NET 10 SDK
- Node.js 22 LTS (with npm)
- Docker (for Aspire-managed NATS container)
- .NET Aspire workload (`dotnet workload install aspire`)

---

## 1. Clone and Setup

```bash
git checkout 001-nats-admin-app
```

## 2. Start Everything with Aspire (Recommended)

```bash
cd src/NatsManager.AppHost
dotnet run
```

This starts the Aspire AppHost which orchestrates:
- **NATS** container with JetStream enabled (port 4222)
- **Backend** ASP.NET Core API (port auto-assigned)
- **Frontend** Vite dev server (port auto-assigned)
- **Aspire Dashboard** for monitoring (opens automatically in browser)

The Aspire Dashboard provides real-time resource status, structured logs, traces, and metrics.

## 3. Manual Start (Without Aspire)

If you prefer to start components individually:

### Backend

```bash
cd src/NatsManager.Web
dotnet restore
dotnet ef database update --project ../NatsManager.Infrastructure
dotnet run
```

The API will be available at `http://localhost:5000`.

### Frontend

```bash
cd src/NatsManager.Frontend
npm install
npm run dev
```

The Vite dev server runs at `http://localhost:5173` with HMR.

### NATS (manual container)

```bash
docker run -d --name nats-dev \
  -p 4222:4222 -p 8222:8222 \
  nats:latest -js -m 8222
```

## 4. Run Tests

```bash
# Backend unit tests
dotnet test tests/NatsManager.Domain.Tests
dotnet test tests/NatsManager.Application.Tests

# Backend integration tests (requires running NATS)
dotnet test tests/NatsManager.Infrastructure.Tests
dotnet test tests/NatsManager.Web.Tests

# Frontend tests (Vitest, colocated with source)
cd src/NatsManager.Frontend
npm test

# Frontend tests with coverage
npm run test:coverage
```

## 5. Build Container Image (Production)

```bash
docker build -t nats-admin:dev -f src/NatsManager.Web/Dockerfile .
docker run -d -p 8080:8080 -v nats-admin-data:/data nats-admin:dev
```

The application will be available at `http://localhost:8080`.

## 6. First Steps

1. Start via Aspire (`dotnet run` in AppHost) — the dashboard opens automatically
2. Open the frontend URL shown in the Aspire Dashboard
3. Log in with the default admin account (created during first migration)
4. Navigate to **Environments** and register your NATS server (`nats://localhost:4222`)
5. Test the connection — you should see a green "Available" status
6. Select the environment and browse JetStream streams, KV buckets, etc.

## Key Development URLs

| URL | Purpose |
|-----|---------|
| Aspire Dashboard | Resource monitoring, logs, traces, metrics (auto-opens) |
| `http://localhost:5000/swagger` | OpenAPI documentation (manual start) |
| `http://localhost:5000/health` | Health check endpoint |
| `http://localhost:5173` | Vite dev server (manual start) |
| `http://localhost:5173` | Frontend dev server |
| `http://localhost:8222` | NATS monitoring (dev) |
