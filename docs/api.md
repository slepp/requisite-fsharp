# API and design notes

The public namespace is `Requisite`. `.fsi` files define the supported surface
and hide representations that carry invariants.

## `Tainted`

| API | Purpose |
|---|---|
| `Tainted.fromInput value` | create `Tainted<_, Untrusted>` at an input edge |
| `Tainted.inspect tagged` | observe without changing the trust state |
| `Tainted.sanitize policy tagged` | perform an infallible trusted transition |
| `Tainted.trySanitize policy tagged` | perform a `Result`-returning transition |
| `Tainted.value trusted` | extract only from `Trusted` |
| `Tainted.widen trusted` | lower a trusted value to an untrusted requirement |

Trust means “passed this sink's policy,” not “universally safe.” Put the
destination-specific policy next to the transition and require the trusted
wrapper at the sink.

`Tainted` has neither structural equality nor structural comparison. This
avoids accidental reliance on an invariant wrapper as though it were only its
payload.

## Confidence

`Confidence.create` validates finite values from `0.0` to `1.0`, inclusive.
`Thresholds.create` additionally requires `likely < certain` and
`certain >= Thresholds.MinimumCertain`.

`Confident.gate` and `Confident.gateWith` return the exhaustive DU:

```fsharp
type Gate<'T> =
    | HighConfidence of Certain * 'T
    | Likely of 'T
    | Unsure of 'T
```

`Gate` deliberately has neither equality nor comparison. In particular,
separately issued high-confidence gates do not compare tokens by reference.

`Certain` has no public representation or constructor in ordinary checked F#,
so sensitive functions can require it in their call contract. The guarantee is
qualified: tokens can be retained and reused, are not tied to one value or
action, and reflection, `Unchecked.defaultof`, emitted IL, or equivalent .NET
escape hatches can bypass the abstraction.

An incomplete DU match is warning `FS0025`, not an error by default. Consumers
that depend on exhaustive handling should use:

```xml
<WarningsAsErrors>$(WarningsAsErrors);25</WarningsAsErrors>
```

## Freshness

`Fresh.create` and `Fresh.fetchedAt` validate non-negative TTLs.
`Fresh.tryGet` returns `Result<'T, Stale>`.
`Fresh.tryGetWithRecovery` returns `Result<'T, StaleValue<'T>>`, preserving the
expired value in the error branch.

Every operation measures elapsed time with the platform monotonic clock. A
`MonotonicTimestamp` is process-local; `offsetBy` exists for deterministic
tests and adapters, not persistence. Extreme offsets saturate at the timestamp
bounds, and elapsed conversion saturates at `TimeSpan.MaxValue`.

F# has no ownership-consuming return, so `tryGetWithRecovery` does not claim to
consume the wrapper. Call freshness checks immediately before the protected
operation.

`StaleValue.ToString()` reports timing only and omits the payload. Its fields
remain public for recovery; debug formatting such as `%A` can therefore include
the payload.

## Representation choices

`Tainted`, `Confident`, `Gate`, `Certain`, `Fresh`, `StaleValue`, and monotonic
timestamps do not expose structural equality or comparison. Validated scalar
`Confidence` and `Thresholds` expose equality but not comparison, matching the
Rust API's lack of total ordering.

The invariant wrappers are not structs. Avoiding default-initialized struct
states is more important here than eliminating small wrapper allocations.

F# has no direct equivalent of Rust's `#[must_use]`; callers can ignore a
wrapper or `Result`. This library keeps transitions explicit but cannot force
consumption.
