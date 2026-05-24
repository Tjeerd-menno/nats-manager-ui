# Phase 0 Research: Application Permission Exposure

## Decision: Expose the manifest with a dedicated root-level Minimal API endpoint

Use a new `PermissionManifestEndpoints` mapper registered from `Program.cs` with `app.MapGet("/.well-known/permissions", ...)`. The endpoint is intentionally outside existing `/api` and environment-scoped route groups because the feature contract requires a well-known discovery location independent of NATS environment context.

**Rationale**: Existing endpoints are grouped by application area or environment. A well-known discovery endpoint is an application-level contract and should not inherit environment route parameters. ASP.NET Core Minimal API guidance supports `MapGet` handlers returning typed results for JSON and alternate status outcomes, which fits the 200/401/503 contract.

**Alternatives considered**:

- Add the route under `/api`: rejected because it breaks the well-known discovery contract.
- Serve a static JSON file: rejected because validation, exposure policy, last-valid fallback, logging, and application version resolution are application behavior.
- Put the endpoint into an existing access-control group: rejected because the feature publishes application-owned permissions and is not an IAM management workflow.

## Decision: Keep the manifest source code-owned in the application layer

Create a `Permissions` module in `NatsManager.Application` with manifest models, a registry for the active permission set, validation, and publication logic.

**Rationale**: The spec says the publishing application owns the permission set. A code-owned registry keeps permission changes reviewable, testable, versioned with the application, and independent of database migrations. It also avoids coupling this feature to IAM import or assignment behavior.

**Alternatives considered**:

- Store permissions in SQLite: rejected because the feature does not require runtime editing and would add persistence, migrations, and admin UI scope.
- Generate permissions from current roles only: rejected because roles are assignment groupings, while the manifest contract describes permission capabilities using `{aggregate-resource}:{action}` names.
- Read a JSON file at request time: rejected because the request path should not depend on filesystem I/O and last-valid fallback needs explicit publication state.

## Decision: Validate on publication and serve the last valid manifest on current-source failure

Use `PermissionManifestPublisher` as a singleton publication service with current validation status and the last valid `PermissionManifest`. Startup validation initializes the publication state. Retrieval returns the last valid manifest when current validation fails; if no prior valid manifest exists, retrieval fails safely.

**Rationale**: This directly implements the clarification that invalid current manifests should not expose malformed data and should preserve discovery availability when a prior valid manifest exists. Keeping the last valid manifest in memory keeps retrieval fast and deterministic.

**Alternatives considered**:

- Fail every retrieval while the current manifest is invalid: rejected because it reduces availability and contradicts the selected clarification.
- Return the invalid manifest with warnings: rejected because the spec forbids malformed or unvalidated successful manifests.
- Fail application startup on invalid manifest: rejected because this endpoint should not take down unrelated NATS Manager functionality; the endpoint can fail safely and log loudly.

## Decision: Make exposure mode explicit through validated deployment options

Add `PermissionManifestOptions` in the web layer with an `ExposureMode` enum. `Public` mode returns the manifest without authentication. `Restricted` mode requires a configured service access key sent with the request and compared using constant-time validation against a stored hash or configured secret representation.

**Rationale**: The spec requires each environment to explicitly declare public or restricted exposure. The existing session-auth UI model is not appropriate for external service discovery, and no IAM import workflow is in scope. A narrow service-key policy is testable, deployable behind gateway controls, and keeps the endpoint self-contained.

**Alternatives considered**:

- Reuse browser session authorization: rejected because external discovery consumers are service systems, not interactive users.
- Rely only on upstream gateway policy: rejected because the spec says the application must enforce approved access controls when restricted.
- Add OAuth2/client credentials in this feature: rejected as too broad and outside application-owned publication behavior.

## Decision: Use application version only for change tracking

The manifest may include `application.version`, and no separate manifest version is planned.

**Rationale**: Clarification selected application version as the only version field. This keeps the payload shape close to the original contract and avoids confusing consumers with two independent version concepts.

**Alternatives considered**:

- Add `manifest.version`: rejected by clarification.
- Require a version field: rejected because the spec marks version as optional and existing application version metadata may not always be available in local builds.

## Decision: Publish active permissions only

The registry exposes only active permissions. Deprecated or inactive permissions are excluded from the successful manifest.

**Rationale**: Clarification selected active-only publication. Rename behavior is represented by adding the new active permission and removing the old permission when it is no longer active.

**Alternatives considered**:

- Include deprecated permissions with status: rejected by clarification.
- Include replacement metadata: rejected by clarification and would expand lifecycle scope.

## Decision: No frontend implementation is required

Do not add React components, hooks, navigation, or UI tests for this feature.

**Rationale**: The user-facing value is endpoint discovery by external consumers. Administrative UI grouping is supported through the manifest `category` field, but no in-app administration view is required by the specification.

**Alternatives considered**:

- Add an admin page to preview permissions: rejected as out of scope and unnecessary for endpoint contract validation.

## Decision: Test at application and HTTP contract boundaries

Use application unit tests for validation and publication state, and web tests for route, status codes, headers, access modes, and JSON shape.

**Rationale**: This matches the constitution: unit tests for validation/business logic, contract tests for API boundaries, and no frontend/E2E tests when no UI behavior changes.

**Alternatives considered**:

- Only web tests: rejected because validation rules and fallback state are business logic.
- Full E2E browser tests: rejected because there is no browser workflow.
