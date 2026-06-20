# Stage 1 Implementation Plan: Capture And Storage

## Objective

Record one metadata-only event per MCP tool invocation and persist it durably in
a self-creating `analytics.db`, without changing any tool result, status, or
latency. This stage delivers the write path only; the read API is Stage 2.

Implements spec §4, §5, §6, §8, §9.

## Scope

- Capture at the single chokepoint `ToolInvocationLogger.InvokeAsync`.
- Non-blocking recorder plus background batched SQLite writer.
- Separate, self-creating SQLite analytics database.
- `DevContextMcp:Analytics` configuration and startup validation.

Out of scope: HTTP endpoints, aggregate queries, dashboard.

## Components

### Configuration

- Add `AnalyticsOptions` (`Enabled`, `DatabasePath`, `UserHeaderName`) under
  `DevContextMcp`, defaulting to enabled, `../../../../../database/analytics.db`,
  and `X-User-Name`.
- Validate on startup: when `Enabled`, `DatabasePath` and `UserHeaderName` must
  be non-empty. Resolve `DatabasePath` relative to the host base directory.
- When disabled, register no recorder, hosted service, or store.

### Event model and abstractions (`Server.Core`)

- `ToolInvocationRecord`: id, toolName, userName, startedAt (UTC), durationMs,
  status (`success`|`error`|`canceled`), errorType?, requestBytes?,
  responseBytes?.
- `IToolInvocationWriteStore.AppendAsync(IReadOnlyList<ToolInvocationRecord>,
  CancellationToken)`.

### SQLite write store (`Infrastructure`)

- `SqliteAnalyticsStore` implementing `IToolInvocationWriteStore`, mirroring the
  open/version pattern of `SqliteNuGetReadStore`.
- Self-creating: on first use create file, `tool_invocations` table, and the
  three indexes in `ReadWriteCreate` mode with WAL journaling.
- `AnalyticsSchema.Version = 1` stamped in `PRAGMA user_version`; refuse older.

### Recorder and drain (`Server`)

- `AnalyticsRecorder` singleton over a bounded `Channel<ToolInvocationRecord>`,
  drop-oldest with a drop counter. `Enqueue` never blocks and never throws.
- `IHostedService` drains the channel and writes in batches via the write store.
- All capture and persistence failures are swallowed.

### Capture hook (`Server`)

- Inject `AnalyticsRecorder` and `IHttpContextAccessor` into
  `ToolInvocationLogger`; enqueue a record on each outcome reusing the existing
  `Stopwatch` and status branches.
- `AnalyticsUserResolver`: configured header → `User.Identity.Name` → remote IP
  → `anonymous`.
- Payload sizes are best-effort: reuse a size already serialized for diagnostics,
  otherwise record null. Never serialize solely to measure.

## Implementation Sequence

1. Add `AnalyticsOptions`, bind it, validate on start, extend `appsettings.json`.
2. Add `ToolInvocationRecord` and `IToolInvocationWriteStore` in `Server.Core`.
3. Add `SqliteAnalyticsStore` write path + schema/version in `Infrastructure`.
4. Add `AnalyticsRecorder` + hosted drain in `Server`.
5. Add `AnalyticsUserResolver` and hook capture into `ToolInvocationLogger`.
6. Register everything in DI, gated on `Enabled`.
7. Add tests.

## Tests

- Options validation: enabled requires non-empty path and header; disabled skips.
- Status mapping: return → `success`, cancellation → `canceled`, fault → `error`
  with CLR error type.
- User resolution precedence: header, identity, IP, `anonymous`.
- Write store round-trip and schema/version creation against a temp database.
- Regression: tool results, status, and timing unchanged with analytics on/off.
- Architecture: `ProjectDependencyTests` stays green (no new project references).

## Completion Criteria

- Running the server self-creates `analytics.db` without touching `docs.db`.
- Each tool call writes exactly one metadata-only event with correct fields.
- Errored and canceled calls record the correct status and error type.
- Disabling analytics removes all capture with no behavior change.
- `dotnet build` and `dotnet test` pass.
