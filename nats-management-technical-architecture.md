# Technical Architecture and Integration Design  

## NATS Management Application

## 1. Purpose

This document defines the technical architecture and integration design for the NATS Management Application.

The application shall:

- Manage Core NATS
- Manage JetStream streams and consumers
- Manage JetStream Key-Value Store
- Manage JetStream Object Store
- Discover and interact with NATS Services
- Run as a single lightweight container
- Be deployable on Docker, Podman, and Kubernetes

This architecture is aligned with:

- Domain-Driven Design (DDD)
- Clean Architecture
- Hexagonal Architecture (Ports & Adapters)
- CQRS

---

## 2. Architectural Drivers

The architecture is driven by the following needs:

- A single deployable unit that is easy to operate
- Clear separation of domain logic from infrastructure and UI concerns
- A model that preserves native NATS concepts
- Safe administrative behavior with auditability
- Support for multiple NATS environments
- Low operational footprint
- A front end and API that can evolve independently inside one deployable unit
- Suitability for containerized deployment

---

## 3. Technology Stack

### Backend

- .NET 10
- ASP.NET Core 10
- Minimal APIs
- EF Core 10
- SQLite

### Frontend

- React
- TypeScript
- Mantine
- Mantine UI
- Recharts

### Packaging and Hosting

- Single container image
- Docker-compatible OCI image
- Podman-compatible OCI image
- Kubernetes deployable

---

## 4. Architectural Style

The solution shall use a layered and modular architecture with explicit domain boundaries.

### 4.1 Architectural Combination

The architecture combines the following approaches:

#### Domain-Driven Design

DDD shall be used to model the core business concepts of the application around NATS resource management.

#### Clean Architecture

The solution shall separate domain and application logic from infrastructure and delivery mechanisms.

#### Hexagonal Architecture

All external dependencies shall be accessed through ports and adapters so that the core application remains independent of infrastructure details.

#### CQRS

Read and write responsibilities shall be separated at the application layer to improve clarity, safety, and maintainability.

---

## 5. High-Level System Shape

The application shall be implemented as a modular monolith packaged into a single deployable container.

This means:

- One deployment unit
- One backend process
- One web frontend
- One persistence store
- Multiple internal modules with strict boundaries

This approach keeps deployment lightweight while still allowing the internals to be organized around bounded contexts.

---

## 6. Deployment Model

## 6.1 Single Lightweight Container

The solution shall be publishable as one container image containing:

- The ASP.NET Core application
- The Minimal API endpoints
- The application modules
- The React frontend assets
- SQLite database storage mounted through a persistent volume or bind mount

The frontend shall be served by the backend from static assets so that the complete application can run as one service.

## 6.2 Runtime Targets

The container image shall be runnable in:

- Docker
- Podman
- Kubernetes

## 6.3 Persistence in Containerized Environments

SQLite shall be persisted outside the container writable layer through mounted storage.

The design shall assume:

- Ephemeral containers
- Persistent data volume for SQLite
- Explicit backup/restore responsibility outside the application process

---

## 7. Bounded Contexts and Modules

Each bounded context shall be implemented as an internal module with its own domain model, application logic, and adapters.

### Core modules

1. Environment Management
2. Core NATS
3. JetStream
4. KV Store
5. Object Store
6. Services
7. Monitoring
8. Identity & Access
9. Audit

Each module shall own:

- Domain concepts
- Commands
- Queries
- Validation rules
- Policies
- Persistence mappings where required
- Integration adapters where required

---

## 8. Solution Structure

A suitable solution structure is:

```text
src/
  NatsManager.Web/
  NatsManager.Application/
  NatsManager.Domain/
  NatsManager.Infrastructure/
  NatsManager.Frontend/
tests/
  NatsManager.Domain.Tests/
  NatsManager.Application.Tests/
  NatsManager.Infrastructure.Tests/
  NatsManager.Web.Tests/
```

---

## 9. Layer Responsibilities

## 9.1 Domain Layer

The Domain layer shall contain:

- Aggregates
- Entities
- Value objects
- Domain policies
- Domain services
- Domain events
- Invariants

The Domain layer shall not depend on:

- ASP.NET Core
- EF Core
- SQLite
- NATS client libraries
- React
- HTTP concepts

The Domain layer represents the business meaning of:

- Managed NATS environments
- Registered resources
- User intent
- Administrative rules
- Audit-relevant behavior
- Safety constraints

---

## 9.2 Application Layer

The Application layer shall orchestrate use cases.

It shall contain:

- Commands
- Queries
- Command handlers
- Query handlers
- DTOs
- Application services
- Authorization checks
- Transaction boundaries
- Use-case-specific validation
- Ports for outbound dependencies

Examples of application use cases:

- Register environment
- Refresh environment state
- List streams
- Inspect consumer details
- Create KV entry
- Delete object
- Invoke service endpoint
- Publish message
- Subscribe to subject for inspection
- Record audit event

---

## 9.3 Infrastructure Layer

The Infrastructure layer shall contain adapters for:

- EF Core persistence
- SQLite
- NATS connectivity
- JetStream resource access
- KV access
- Object Store access
- Service discovery and invocation
- Authentication integration
- Authorization persistence
- Audit persistence
- Static file hosting support
- Logging and telemetry integration

Infrastructure shall implement application ports but shall not own business rules.

---

## 9.4 Web/API Layer

The Web layer shall expose:

- Minimal API endpoints
- Authentication endpoints or integration points
- OpenAPI metadata
- Static frontend hosting
- Health endpoints
- Administrative API surface
- Query API surface
- Command API surface

The Web layer shall:

- Translate HTTP requests into commands and queries
- Translate application results into HTTP responses
- Remain thin
- Avoid domain logic

---

## 9.5 Frontend Layer

The Frontend layer shall provide:

- Resource overviews
- Detail screens
- Search and filtering
- Action dialogs
- Confirmation workflows
- Audit and monitoring views

The frontend shall consume the backend API and remain separate from server-side domain logic.

---

## 10. Hexagonal Architecture

## 10.1 Inbound Ports

Inbound ports represent application use cases.

Examples:

- ManageEnvironment
- InspectCoreNats
- ManageStream
- ManageConsumer
- ManageKvBucket
- ManageObjectBucket
- DiscoverServices
- InvokeService
- ManageUsersAndRoles
- ReviewAuditHistory

## 10.2 Outbound Ports

Outbound ports represent dependencies on external systems.

Examples:

- IEnvironmentRepository
- IUserRepository
- IRoleRepository
- IAuditEventRepository
- INatsConnectionFactory
- ICoreNatsInspector
- IJetStreamInspector
- IKvStoreGateway
- IObjectStoreGateway
- IServiceDiscoveryGateway
- IServiceInvocationGateway
- IClock
- ICurrentUser
- IUnitOfWork

## 10.3 Adapters

Adapters implement the ports.

Examples:

- SQLite repositories using EF Core
- NATS client adapter
- JetStream administration adapter
- KV adapter
- Object Store adapter
- Service discovery adapter
- JWT or cookie authentication adapter
- Web API adapter
- React UI adapter

---

## 11. CQRS Design

CQRS shall be applied at the application boundary.

## 11.1 Command Side

The command side shall handle state-changing intent.

Examples:

- Add environment
- Update environment definition
- Remove environment
- Create stream
- Delete stream
- Create consumer
- Put KV key
- Delete KV key
- Upload object
- Delete object
- Trigger service test invocation
- Assign role
- Revoke permission

Command handling shall:

- Validate intent
- Enforce authorization
- Enforce safety rules
- Persist internal state when needed
- Emit audit events
- Trigger integration actions against NATS where appropriate

## 11.2 Query Side

The query side shall provide read models optimized for inspection.

Examples:

- Stream summary view
- Consumer backlog dashboard
- KV bucket browser
- Object metadata browser
- Service catalog
- Subject activity view
- Audit history
- Environment overview

Read models may differ from command models in order to support fast and clear UI composition.

## 11.3 Separation Rule

Commands shall not return rich query graphs.
Queries shall not perform state changes.

---

## 12. Domain-Driven Design Model

## 12.1 Core Domain

The core domain is safe and understandable administration of NATS environments.

Key domain concepts include:

- Environment
- Stream
- Consumer
- Subject
- KV Bucket
- Object Bucket
- Service Descriptor
- Access Policy
- Audit Event

## 12.2 Aggregates

Potential aggregates include:

- Environment Aggregate
- Access Policy Aggregate
- User/Role Assignment Aggregate
- Audit Trail Aggregate
- Saved View or Bookmark Aggregate

External NATS resources such as streams and consumers may be represented as managed resources and/or observed resources depending on whether the application owns their lifecycle or only inspects them.

## 12.3 Domain Events

Examples:

- EnvironmentRegistered
- EnvironmentUpdated
- StreamManagementRequested
- SensitiveActionConfirmed
- UserRoleAssigned
- AuditEventRecorded

Domain events shall represent meaningful business facts inside the application.

---

## 13. Persistence Design

## 13.1 Persistence Scope

SQLite shall be used for application-owned data such as:

- Registered environments
- User preferences
- Saved filters or bookmarks
- Local authorization metadata
- Audit records
- Cached metadata where appropriate
- Application configuration
- Job or refresh state if needed

SQLite shall not be treated as the source of truth for NATS-managed runtime resources such as:

- Live subscriptions
- Live stream state
- Live consumer delivery state
- Live KV values
- Live object contents
- Live service availability

Those shall be observed from NATS and related APIs/gateways.

## 13.2 EF Core Usage

EF Core shall be used for:

- Mapping application-owned entities
- Managing migrations
- Enforcing persistence boundaries
- Querying persisted read models where applicable

## 13.3 Persistence Strategy

The architecture shall distinguish between:

- Application-owned persistent state in SQLite
- External operational state in NATS

This prevents duplication of authority and keeps the source of truth clear.

---

## 14. Integration Design

## 14.1 NATS Integration

The application shall integrate with one or more external NATS environments.

Integration responsibilities include:

- Connect to NATS servers or clusters
- Inspect Core NATS state where observable
- Manage JetStream resources
- Access KV buckets
- Access Object Store buckets
- Discover services
- Invoke diagnostic service requests

## 14.2 Integration Boundary

All NATS interactions shall be isolated behind application ports and infrastructure adapters.

This ensures that:

- The domain remains independent
- The application layer expresses intent rather than protocol details
- NATS client concerns do not leak into business logic

## 14.3 Environment Isolation

Every integration action shall execute within an explicit environment context.

The design shall prevent accidental cross-environment actions by requiring all operations to be scoped to a selected environment.

## 14.4 Authentication to Managed NATS Environments

The architecture shall support per-environment connection configuration and credentials or tokens as required by the target NATS environment.

Secrets shall be treated as infrastructure concerns, not domain concerns.

## 14.5 Caching and Refresh

The architecture may cache selected observed data for performance and usability, but shall clearly distinguish:

- Live state
- Refreshed state
- Cached state

The design shall ensure users can understand data freshness.

---

## 15. API Design Approach

## 15.1 API Style

The backend shall expose HTTP APIs using ASP.NET Core Minimal APIs.

The API shall include:

- Query endpoints for dashboards and inspection
- Command endpoints for administrative actions
- Authentication and session endpoints where needed
- Health endpoints
- OpenAPI description

## 15.2 Endpoint Organization

Endpoints should be organized by bounded context, for example:

```text
/api/environments
/api/core
/api/jetstream/streams
/api/jetstream/consumers
/api/kv/buckets
/api/objects/buckets
/api/services
/api/audit
/api/access
```

## 15.3 Result Semantics

The API shall:

- Return clear domain-relevant responses
- Distinguish validation failures from authorization failures
- Distinguish not found from unavailable
- Distinguish safe read actions from state-changing actions

## 15.4 Validation

Input validation shall be performed at the API and application boundaries.
Business rule validation shall remain in the application/domain layers.

---

## 16. Frontend Architecture

## 16.1 Frontend Shape

The frontend shall be a React single-page application served by the ASP.NET Core host.

## 16.2 Frontend Responsibilities

The React application shall provide:

- Environment selection
- Resource navigation
- Dashboard views
- Inspection views
- Action forms
- Confirmation workflows
- Audit history views
- Permission-aware UI behavior

## 16.3 Frontend Module Boundaries

The UI should be organized around feature modules aligned with backend bounded contexts:

- Environments
- Core NATS
- JetStream
- KV
- Object Store
- Services
- Monitoring
- Access
- Audit

## 16.4 UI Interaction Model

The UI shall treat:

- Queries as read-only fetches
- Commands as explicit user actions
- Destructive actions as confirmation-based flows

---

## 17. Security Architecture

## 17.1 Security Boundaries

The application shall secure:

- User authentication
- Authorization for actions
- Access to sensitive payloads
- Access to environment connection definitions
- Access to audit history
- Access to destructive operations

## 17.2 Authorization Model

Authorization shall be policy- or role-based and shall support:

- Read-only users
- Operators
- Administrators
- Auditors

## 17.3 Secret Handling

Connection secrets shall not be embedded in frontend code or domain entities.

They shall be handled through infrastructure and deployment configuration.

---

## 18. Observability and Diagnostics

The application shall expose operational diagnostics for:

- API health
- Database availability
- NATS connectivity
- Background refresh health if present
- Integration failures
- Audit persistence health

The architecture should support:

- Structured logging
- Correlation of actions and audit events
- Operational health reporting

---

## 19. Background Processing

Where background processing is required, it shall remain inside the same deployable unit.

Examples:

- Refresh environment metadata
- Periodic resource discovery
- Cleanup of cache or temporary inspection data
- Health polling

Background processing shall not require a separate deployable service.

---

## 20. Containerization Design

## 20.1 Container Image

The application shall be packaged into one OCI-compatible image.

The image shall contain:

- Published ASP.NET Core application
- Static React assets
- Runtime dependencies only

## 20.2 Container Principles

The image should be:

- Small
- Stateless except for mounted persistent storage
- Configurable via environment variables and mounted files
- Suitable for local and orchestrated execution

## 20.3 Kubernetes Suitability

The application shall be deployable in Kubernetes as a single workload with:

- Persistent volume for SQLite
- Config-driven environment definitions
- Secret-driven runtime credentials
- Health probes
- Resource limits

---

## 21. Integration Scenarios

## 21.1 Register and Inspect Environment

1. User registers an environment
2. Application validates access
3. Environment metadata is stored in SQLite
4. User selects the environment
5. Application queries live NATS state through adapters
6. UI presents summaries and detailed views

## 21.2 Stream Administration

1. User opens stream detail
2. Query model presents current stream state
3. User submits update or delete command
4. Application validates authorization and safety
5. NATS adapter performs the external action
6. Audit event is recorded
7. Updated state is returned via the query side

## 21.3 KV Inspection and Mutation

1. User browses bucket and keys
2. Query side loads current bucket/key information
3. User submits put/delete action
4. Command side enforces policy and records audit
5. KV adapter executes the change
6. Query side refreshes the visible state

## 21.4 Service Diagnostic Invocation

1. User discovers a service endpoint
2. User initiates a diagnostic request
3. Application checks authorization and side-effect sensitivity
4. Service adapter issues the request
5. Response is returned for inspection
6. Audit trail records the invocation

---

## 22. Key Architectural Decisions

### Decision 1: Modular monolith instead of microservices

Because the product must run as one lightweight container and remain simple to deploy, the preferred architecture is a modular monolith.

### Decision 2: React SPA served by the ASP.NET Core host

Because the solution must be a single deployable unit, the frontend is packaged into the backend host rather than deployed separately.

### Decision 3: SQLite only for application-owned state

Because NATS remains the system of record for operational messaging resources, SQLite is limited to application state and metadata owned by this product.

### Decision 4: Ports and adapters for NATS integration

Because NATS is an external dependency and may evolve, all NATS-specific behavior is isolated behind adapters.

### Decision 5: CQRS at application boundary

Because read and write use cases differ significantly in shape and risk, commands and queries are separated.

---

## 23. Risks and Constraints

### 23.1 SQLite Constraint

SQLite is well suited for lightweight single-deployment persistence, but the design shall account for its limitations in concurrent write-heavy scenarios.

### 23.2 Single Instance Persistence Constraint

If the application is deployed with SQLite, active-write multi-instance scaling shall not be assumed without an alternative persistence strategy.

### 23.3 External State Volatility

Much of the displayed NATS state is externally owned and can change independently of the application.

The design shall therefore favor explicit refresh, freshness indicators, and resilient error handling.

---

## 24. Recommended Build and Packaging Model

The recommended build model is:

1. Build React frontend
2. Emit static assets
3. Publish ASP.NET Core backend
4. Include static assets in published output
5. Build one OCI image from the published application
6. Mount persistent storage for SQLite at runtime

---

## 25. Summary

The recommended architecture is a single-container modular monolith built with:

- .NET 10
- ASP.NET Core 10 Minimal APIs
- EF Core 10
- SQLite
- React

It applies:

- DDD for domain clarity
- Clean Architecture for separation of concerns
- Hexagonal Architecture for infrastructure isolation
- CQRS for clear use-case modeling

This architecture provides:

- Low operational complexity
- Strong modularity
- Safe NATS administration
- Clear evolution path
- Compatibility with Docker, Podman, and Kubernetes
