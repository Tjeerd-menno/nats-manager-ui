# B1 — Split `JetStreamAdapter` into read and write adapters

## Context

- The ports `IJetStreamAdapter` (read) and `IJetStreamWriteAdapter` (write)
  already exist and are separate in
  `src/NatsManager.Application/Modules/JetStream/Ports/`.
- The concrete class
  `src/NatsManager.Infrastructure/Nats/JetStreamAdapter.cs` currently
  implements **both** interfaces as a single ~322-line `sealed partial class`.
- DI registration in `src/NatsManager.Web/Program.cs` registers the concrete
  type for `IJetStreamAdapter` and then reuses the same instance for
  `IJetStreamWriteAdapter`.
- All Application-layer consumers already depend on the narrow interface they
  need: queries (`GetStreamsQuery`, `GetStreamDetailQuery`,
  `GetConsumersQuery`, `GetConsumerDetailQuery`, `GetStreamMessagesQuery`,
  `GetDashboardQuery`) take `IJetStreamAdapter`; commands
  (`CreateStreamCommand`, `UpdateStreamCommand`, `DeleteStreamCommand`,
  `ConsumerCommands`) take `IJetStreamWriteAdapter`.

## Goal

Make the physical class boundary match the port boundary: one class per
interface, so future changes to read logic cannot accidentally touch write
logic and vice versa. This also removes the slightly awkward DI factory that
casts between the two interface registrations.

## Scope

### In scope

1. Introduce two concrete classes in
   `src/NatsManager.Infrastructure/Nats/`:
   - `JetStreamReadAdapter` — implements `IJetStreamAdapter`.
   - `JetStreamWriteAdapter` — implements `IJetStreamWriteAdapter`.
2. Move the existing method bodies from `JetStreamAdapter.cs` into the
   appropriate new class. The `MapStreamInfo` / `MapConsumerInfo` helpers
   and any other shared mappers should move to an internal static helper
   class (for example `JetStreamModelMapper`) so both adapters can use them
   without inheritance.
3. Delete the old combined `JetStreamAdapter` class **and** its file.
4. Update DI in `src/NatsManager.Web/Program.cs` so each interface resolves
   directly to its own concrete type (no factory/cast).
5. Preserve the current logger category names by typing each
   `ILogger<JetStreamReadAdapter>` / `ILogger<JetStreamWriteAdapter>`
   respectively. If structured-log templates reference the old type, grep
   for them and update.
6. Update any xUnit tests in `tests/NatsManager.Integration.Tests` and
   `tests/NatsManager.Infrastructure.Tests` that reference the old class by
   name.

### Out of scope

- No public API or behaviour changes.
- No signature changes on the two interfaces.
- No changes to Application, Domain, Web, or Frontend code beyond DI wiring.

## Files expected to change

- `src/NatsManager.Infrastructure/Nats/JetStreamAdapter.cs` — **deleted**.
- `src/NatsManager.Infrastructure/Nats/JetStreamReadAdapter.cs` — **new**.
- `src/NatsManager.Infrastructure/Nats/JetStreamWriteAdapter.cs` — **new**.
- `src/NatsManager.Infrastructure/Nats/JetStreamModelMapper.cs` — **new** (internal static).
- `src/NatsManager.Web/Program.cs` — DI registrations.
- Any `tests/NatsManager.Integration.Tests/**` or
  `tests/NatsManager.Infrastructure.Tests/**` referring to
  `JetStreamAdapter` by name.

## Acceptance criteria

- [ ] Two classes exist, each implementing exactly one port interface.
- [ ] No class inherits from the other; shared mapping lives in a static
      helper.
- [ ] `dotnet build NatsManager.slnx -c Debug` succeeds with 0 warnings.
- [ ] All existing xUnit tests pass unchanged (except for name references
      that must be updated).
- [ ] `Program.cs` registers both adapters without a factory cast.
- [ ] A search for `JetStreamAdapter` (the old combined name) across `src/`
      returns zero hits.

## Test plan

1. Run the full backend test suite:
   `dotnet test NatsManager.slnx -c Debug`.
2. Run the Aspire stack end-to-end smoke: `aspire run` then exercise the
   JetStream page (create stream → publish → list messages → create
   consumer → delete stream) to confirm nothing regressed.
3. E2E tests under `tests/NatsManager.E2E.Tests` that cover JetStream flows
   should pass without modification.

## Risks / notes

- Because both adapters use the same `INatsConnectionFactory` cache, they
  will share the underlying `NatsConnection`. Register both as the same
  lifetime the combined class currently uses (check `Program.cs` — today
  it's `AddSingleton`). Keep that.
- If any method is hard to classify as read vs write (for example a peek
  that subscribes briefly), prefer placing it on the read adapter and note
  the decision in the PR description.
- The file is a `partial class` today. Verify no other partial files exist
  for it before deleting — `grep "partial class JetStreamAdapter"` over
  `src/NatsManager.Infrastructure`.

## Reference instructions

- `.github/instructions/infrastructure.instructions.md`
- `.github/instructions/backend.instructions.md`
- `.github/instructions/backend-tests.instructions.md`
