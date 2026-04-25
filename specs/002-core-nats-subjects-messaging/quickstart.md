# Quickstart: Core NATS Subjects Browser, Expanded Publishing & Message Viewer

**Branch**: `002-core-nats-subjects-messaging`
**Date**: 2026-04-25

---

## Prerequisites

Same as the base application:
- .NET 10 SDK
- Node.js 22 LTS (with npm)
- Docker (for Aspire-managed NATS container)
- .NET Aspire workload installed

---

## 1. Checkout the Branch

```bash
git checkout 002-core-nats-subjects-messaging
```

---

## 2. Start Everything with Aspire

```bash
cd src/NatsManager.AppHost
dotnet run
```

The Aspire AppHost starts NATS with the monitoring port exposed (`8222`). Open the Aspire dashboard URL printed to the console and verify all resources are green.

Navigate to **Core NATS** in the sidebar to see the new subjects browser.

---

## 3. NATS Monitoring Port

Subject discovery calls `http://<host>:8222/subsz`. The default monitoring port (`8222`) is configured in `appsettings.json`:

```json
{
  "CoreNats": {
    "Monitoring": {
      "DefaultPort": 8222,
      "HttpTimeout": "00:00:03"
    }
  }
}
```

If your NATS server uses a different monitoring port, update this value. If the monitoring endpoint is unreachable, the subjects section shows an informational placeholder — the rest of the page continues to function.

---

## 4. Verify Subject Browser

1. Open the Core NATS page with an environment selected.
2. The **Active Subjects** section appears below the server info cards.
3. If no clients are subscribed, the section shows "No active subscriptions found."
4. Start a NATS subscriber on any subject (e.g., `nats sub 'test.>'`) and watch the table populate on the next 15-second refresh, or click **Refresh**.

---

## 5. Verify Expanded Publish

1. Click **Publish Message**.
2. The form now shows:
   - **Subject** (required)
   - **Payload Format** selector: Plain Text / JSON / Hex Bytes
   - **Payload** text area
   - **+ Add Header** button — adds key/value rows
   - **Reply-To** field (optional)
3. Select **JSON**, enter `{"event": "test"}`, and click **Publish**.
4. Entering invalid JSON (e.g., `{bad`) should disable the Publish button with an inline error.
5. On success, a green notification appears; form fields remain for the next message.

---

## 6. Verify Live Message Viewer

1. In the **Live Message Viewer** panel, enter `test.>` in the subject pattern field.
2. Click **Subscribe** — the connection badge turns green.
3. Open another terminal and publish: `nats pub test.event '{"hello":"world"}'`
4. The message appears in the viewer within ~1 second.
5. Click on the message row to expand and see the full payload (pretty-printed JSON) and any headers.
6. Click **Pause** — new messages stop appearing.
7. Publish another message — the **Pending** counter increments.
8. Click **Resume** — pending messages flush into the list.
9. Navigate away from the page — the backend subscription is automatically closed.

---

## 7. Run Tests

```bash
# All backend tests
dotnet test

# Frontend unit tests
cd src/NatsManager.Frontend && npm test

# E2E tests (requires Aspire stack running)
dotnet test tests/NatsManager.E2E.Tests
```

---

## 8. Troubleshooting

| Symptom | Likely Cause | Resolution |
|---------|-------------|------------|
| Subjects section shows "Subject discovery unavailable" | NATS monitoring port not open or wrong port | Check `CoreNats:Monitoring:DefaultPort` in `appsettings.json` |
| Live viewer shows "Disconnected" immediately | NATS subject has invalid characters (spaces) | Use valid NATS subject pattern |
| Publish fails with "Invalid hex string" | Payload contains non-hex characters when Hex Bytes selected | Use even-length hex strings (e.g., `48656c6c6f`) |
| Publish fails with "Invalid JSON" | JSON payload is malformed | Fix the JSON — the button should be disabled client-side; check browser console |
