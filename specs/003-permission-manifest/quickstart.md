# Quickstart: Application Permission Exposure

## Prerequisites

- .NET SDK from `global.json`
- Existing repository dependencies restored
- Feature branch: `003-permission-manifest`

## Configure Exposure Mode

For local public discovery testing:

```json
{
  "PermissionManifest": {
    "ExposureMode": "Public",
    "ApplicationId": "nats-manager",
    "ApplicationName": "NATS Manager"
  }
}
```

For restricted discovery testing:

```json
{
  "PermissionManifest": {
    "ExposureMode": "Restricted",
    "RestrictedAccessKeyHash": "<base64-sha256-service-access-key-hash>",
    "ApplicationId": "nats-manager",
    "ApplicationName": "NATS Manager"
  }
}
```

`RestrictedAccessKeyHash` is the base64-encoded SHA-256 hash of the service access key sent through `X-Permission-Manifest-Key`.

## Run Focused Tests

Application-level validation and publication behavior:

```powershell
dotnet test tests/NatsManager.Application.Tests/NatsManager.Application.Tests.csproj -- --filter-namespace NatsManager.Application.Tests.Modules.Permissions
```

HTTP endpoint contract behavior:

```powershell
dotnet test tests/NatsManager.Web.Tests/NatsManager.Web.Tests.csproj -- --filter-namespace NatsManager.Web.Tests.Endpoints --filter-class '*PermissionManifestEndpointTests'
```

HTTP option validation behavior:

```powershell
dotnet test tests/NatsManager.Web.Tests/NatsManager.Web.Tests.csproj -- --filter-namespace NatsManager.Web.Tests.Configuration --filter-class '*PermissionManifestOptionsTests'
```

## Run Full Backend Verification

```powershell
dotnet test
```

```powershell
dotnet format --verify-no-changes
```

## Manual Endpoint Check

Start the web project:

```powershell
dotnet run --project src/NatsManager.Web/NatsManager.Web.csproj
```

Fetch the manifest:

```powershell
Invoke-RestMethod -Uri "https://localhost:5001/.well-known/permissions" -Headers @{ Accept = "application/json" }
```

Restricted mode:

```powershell
Invoke-RestMethod -Uri "https://localhost:5001/.well-known/permissions" -Headers @{
  Accept = "application/json"
  "X-Permission-Manifest-Key" = "<service-access-key>"
}
```

## Expected Results

- Successful responses include `application.id`, `application.name`, optional `application.version`, and an array of active permissions.
- Each permission has a unique `{aggregate-resource}:{action}` `name` and non-empty `description`.
- Consumers can derive wildcard scopes such as `environments:*` from concrete manifest permissions when their IAM solution supports them.
- Deprecated or inactive permissions are absent.
- Invalid current manifests return the last valid manifest when available.
- If no valid manifest exists, the endpoint returns a safe problem response with no permission data.
