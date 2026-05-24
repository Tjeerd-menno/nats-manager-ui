# API Contract: Application Permission Exposure

## GET `/.well-known/permissions`

Returns the current validated permission manifest for the application, or the last valid published manifest when the current manifest source is invalid and a prior valid manifest exists.

### Request

```http
GET /.well-known/permissions HTTP/1.1
Host: example.internal
Accept: application/json
```

Restricted deployments require a service access key header:

```http
X-Permission-Manifest-Key: <service-access-key>
```

The application validates this header against a configured base64-encoded SHA-256 hash of the service access key.

Public deployments do not require authentication for this endpoint.

### Success Response: 200 OK

Headers:

```http
Content-Type: application/json; charset=utf-8
X-Permission-Manifest-Source: current | last-valid
```

Body:

```json
{
  "application": {
    "id": "nats-manager",
    "name": "NATS Manager",
    "version": "1.0.0"
  },
  "permissions": [
    {
      "name": "environments:read",
      "description": "Allows reading registered NATS environments",
      "category": "Environments"
    },
    {
      "name": "jetstream-streams:write",
      "description": "Allows creating, updating, or deleting JetStream streams",
      "category": "JetStream"
    }
  ]
}
```

Permission names use `{aggregate-resource}:{action}` with kebab-case resource segments so consumers that support wildcard policies can derive scopes such as `environments:*` or `jetstream-streams:*` from concrete manifest permissions.

### Safe Failure: 503 Service Unavailable

Returned when no valid manifest has ever been published and the current manifest cannot be validated or retrieved.

Headers:

```http
Content-Type: application/problem+json
```

Body:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.4",
  "title": "Permission manifest unavailable",
  "status": 503,
  "detail": "No valid permission manifest is currently available."
}
```

### Restricted Access Failure: 401 Unauthorized

Returned when deployment exposure mode is restricted and the request omits or provides an invalid service access key.

Headers:

```http
Content-Type: application/problem+json
```

Body:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401,
  "detail": "A valid permission manifest access key is required."
}
```

## JSON Schema

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "Permission Manifest",
  "type": "object",
  "additionalProperties": true,
  "properties": {
    "application": {
      "type": "object",
      "additionalProperties": true,
      "properties": {
        "id": {
          "type": "string",
          "minLength": 1
        },
        "name": {
          "type": "string",
          "minLength": 1
        },
        "version": {
          "type": "string",
          "minLength": 1
        }
      },
      "required": ["id", "name"]
    },
    "permissions": {
      "type": "array",
      "items": {
        "type": "object",
        "additionalProperties": true,
        "properties": {
          "name": {
            "type": "string",
            "minLength": 1
          },
          "description": {
            "type": "string",
            "minLength": 1
          },
          "category": {
            "type": "string",
            "minLength": 1
          }
        },
        "required": ["name", "description"]
      }
    }
  },
  "required": ["application", "permissions"]
}
```

## Contract Test Matrix

| Scenario | Expected |
|----------|----------|
| Public mode with valid current manifest | `200`, `X-Permission-Manifest-Source: current`, required JSON shape |
| Restricted mode without key | `401`, no permission data |
| Restricted mode with invalid key | `401`, no permission data |
| Restricted mode with valid key | `200`, required JSON shape |
| Current manifest invalid with last valid manifest present | `200`, `X-Permission-Manifest-Source: last-valid`, last valid body |
| Current manifest invalid with no prior valid manifest | `503`, problem details, no permission data |
| Manifest contains duplicate permission names | validation failure before successful publication |
| Manifest contains deprecated/inactive permission | permission excluded from successful manifest |
