# Copilot Instructions

## Build & Test

```sh
# Build
dotnet build -c Release

# Run all tests
dotnet test -c Release

# Run a single test class
dotnet test --filter "FullyQualifiedName~FbClientTests"

# Run a single test method
dotnet test --filter "DisplayName~GetVariation"
```

The test project targets `net6.0`. The SDK itself is multi-targeted: `netstandard2.0`, `netstandard2.1`, `netcoreapp3.1`, `net6.0`, `net462`.

## Architecture

```
FbClient (public API, singleton)
├── IMemoryStore (DefaultMemoryStore) — in-memory flag/segment data
├── IEvaluator (Evaluator) — local flag evaluation against the store
├── IDataSynchronizer
│   ├── WebSocketDataSynchronizer — keeps store current via WebSocket push from FeatBit server
│   └── NullDataSynchronizer — used in offline mode
├── IEventProcessor
│   ├── DefaultEventProcessor — flushes eval + metric events to FeatBit server via HTTP
│   └── NullEventProcessor — used when offline or DisableEvents=true
└── IBootstrapProvider — populates store from JSON in offline mode
```

**Data flow:** The FeatBit server pushes flag/segment data over WebSocket → `WebSocketDataSynchronizer` deserializes a `DataSet` and writes it to `IMemoryStore` → `Evaluator` reads from the store on every variation call and evaluates locally (no network per-evaluation). Evaluation results emit `EvalEvent`s that `DefaultEventProcessor` batches and sends back for analytics/A/B testing.

**Evaluation order** (in `Evaluator.cs`):
1. Flag disabled → return `DisabledVariation`
2. User in `TargetUsers` → return targeted variation
3. First matching `Rule` → rollout dispatch on rule's `DispatchKey`
4. Fallthrough → rollout dispatch on fallthrough's `DispatchKey`

All flag variation values are stored and evaluated as `string` internally. Type conversion at the API boundary is handled by `ValueConverter<TValue>` delegates in `ValueConverters.cs`.

## Key Conventions

**Store keys** use string prefixes defined in `StoreKeys`:
- Feature flags: `ff_<flagKey>`
- Segments: `segment_<segmentId>`

**`Evaluator` is a partial class** split across multiple files:
- `Evaluator.cs` — main evaluation logic
- `Evaluator.ConditionMatcher.cs` — condition matching
- `Evaluator.RuleMatcher.cs` — rule matching
- `Evaluator.SegmentMatcher.cs` — segment matching

**`FbClient` constructor never throws.** Initialization failures are logged; check `client.Initialized` or `client.Status` after construction.

**`EvalDetail<T>`** is the return type of all `*VariationDetail` methods. It contains `Key`, `Value`, `Kind` (`ReasonKind` enum: `Fallthrough`, `RuleMatch`, `TargetMatch`, `FlagOff`, `Error`, `ClientNotReady`, `WrongType`), and `Reason` (human-readable string).

**Testing infrastructure** uses an in-process `WebApplicationFactory<TestStartup>` (`TestApp`) that runs a real WebSocket server to simulate the FeatBit streaming endpoint. Tests that need a live client use `[Collection(nameof(TestApp))]` and inject the shared `TestApp` fixture. The internal `FbClient` constructor accepts `IMemoryStore`, `IDataSynchronizer`, and `IEventProcessor` for dependency injection in tests.

**`FbOptionsBuilder`** is the only way to construct `FbOptions`; `FbOptions` has no public setters (except when used via DI via `AddFeatBit` which sets properties directly).

**C# language version is `10`** (set in `FeatBit.ServerSdk.csproj`). Do not use features from C# 11+.

**`System.Text.Json`** is the only JSON library used. It is a built-in runtime package for `netcoreapp3.1`/`net6.0`; it is added as a NuGet dependency for `netstandard2.x` and `net462`.
