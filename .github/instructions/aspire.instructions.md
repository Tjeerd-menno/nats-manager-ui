---
description: "Use when working with Aspire orchestration, the AppHost project, or service defaults. Covers app host configuration, resource wiring, and launching the stack."
applyTo: "src/NatsManager.AppHost/**,src/NatsManager.ServiceDefaults/**"
---
# Aspire Orchestration Instructions

## AppHost Structure

`NatsManager.AppHost` orchestrates the full stack:

```
NATS (container, JetStream enabled, persistent lifetime)
  └── Backend (NatsManager.Web, ASP.NET Core)
       └── Frontend (NatsManager.Frontend, npm dev server)
```

## Resource Configuration

```csharp
// NATS with JetStream
var nats = builder.AddNats("nats").WithArgs("-js").WithLifetime(ContainerLifetime.Persistent);

// Backend with NATS reference + secrets
var backend = builder.AddProject<Projects.NatsManager_Web>("backend")
    .WithReference(nats).WaitFor(nats)
    .WithEnvironment("BootstrapAdmin__Username", ...)
    .WithEnvironment("BootstrapAdmin__Password", ...)
    .WithEnvironment("Encryption__Key", ...);

// Frontend pointing to backend
builder.AddNpmApp("frontend", "../NatsManager.Frontend", "dev")
    .WithReference(backend).WaitFor(backend)
    .WithHttpEndpoint(env: "PORT");
```

## Parameters (Secrets)

Three parameters managed by Aspire:
- `bootstrap-admin-username` — initial admin user
- `bootstrap-admin-password` — initial admin password (secret)
- `backend-encryption-key` — 32-byte base64 encryption key (secret)

## Commands

```bash
aspire run          # Launch full stack with dashboard
aspire describe     # Show resource status
```

## Service Defaults

`NatsManager.ServiceDefaults` provides shared configuration for OpenTelemetry, service discovery, and HTTP resilience policies. All service projects reference this.
