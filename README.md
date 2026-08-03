# Requisite for F#

Requisite makes data-handling requirements visible in ordinary F# types:

- `Tainted<'T, Untrusted>` must pass an application sanitizer before a sink can
  require `Tainted<'T, Trusted>`.
- `Confidence`, `Thresholds`, and the exhaustive `Gate<'T>` DU validate
  probabilities and classify actions.
- `Fresh<'T>` checks a per-value TTL against a monotonic clock at every read and
  preserves stale values for typed recovery.

The package uses FSharp.Core and no runtime helper library.

## Install and compatibility

```sh
dotnet add package Requisite.FSharp
```

The package targets `netstandard2.0`, `net8.0`, and `net10.0`. Its explicit
FSharp.Core dependency floor is `8.0.403`; restore locks pin the repository's
development dependencies.

## Trust transitions

```fsharp
open Requisite

let loadCustomer (id: Tainted<int64, Trusted>) =
    Tainted.value id

let parseId (text: string) =
    match System.Int64.TryParse text with
    | true, value -> Ok value
    | false, _ -> Error "invalid customer id"

let result =
    Tainted.fromInput "42"
    |> Tainted.trySanitize parseId
    |> Result.map loadCustomer
```

`Tainted.inspect` observes without promotion. `Tainted.widen` explicitly
lowers a trusted value to an untrusted requirement.

## Confidence gates

```fsharp
let wakeOperator (_: Certain) reason =
    printfn "wake: %s" reason

match Confident.create 0.97 "strong aurora" |> Result.map Confident.gate with
| Error error -> printfn "%O" error
| Ok(Gate.HighConfidence(proof, reason)) -> wakeOperator proof reason
| Ok(Gate.Likely reason) -> printfn "notify: %s" reason
| Ok(Gate.Unsure reason) -> printfn "log: %s" reason
```

Default boundaries are `0.60` and `0.95`. Custom thresholds may raise, but not
lower, the meaning of `Certain`.

`Certain` has no public representation or constructor in ordinary checked F#.
It is not linear: callers can retain and reuse a token, and .NET escape hatches
such as reflection, emitted IL, or `Unchecked.defaultof` can bypass the
compile-time contract. A token records that some gate reached the certain tier;
it is not cryptographically bound to a value or action.

F# reports a non-exhaustive `Gate` match as warning `FS0025`. Applications that
rely on exhaustive handling should promote it to an error:

```xml
<WarningsAsErrors>$(WarningsAsErrors);25</WarningsAsErrors>
```

## Freshness

```fsharp
open System

match Fresh.create (TimeSpan.FromSeconds 30.0) price with
| Error creationError -> handleConfigurationError creationError
| Ok quote ->
    match Fresh.tryGetWithRecovery quote with
    | Ok current -> charge current
    | Error expired ->
        audit expired.Value
        scheduleRefresh expired.Stale
```

Freshness is checked by `tryGet`, `tryGetWithRecovery`, `age`, and `remaining`.
Timestamps use `Stopwatch.GetTimestamp`, are process-local, and must not be
persisted. Extreme test offsets and elapsed durations saturate instead of
overflowing.

The Rust crate's `Live`/`with_live` API is intentionally omitted because F#/.NET
cannot reproduce its non-escaping lifetime guarantee.

## Deliberate F# differences

- F# cannot encode Rust's `#[must_use]`. Results and wrappers can still be
  ignored; code review and warning policy remain important.
- `tryGetWithRecovery` does not consume its wrapper because F# has no move
  semantics.
- Invariant-bearing wrappers and `Gate` intentionally do not support F#
  structural equality or comparison. `Confidence` and `Thresholds` support
  equality, but not ordering.
- Private reference representations are preferred over struct wrappers whose
  default values would create additional invalid states.

## Build and test

The pinned SDK is in
[`global.json`](https://github.com/slepp/requisite-fsharp/blob/main/global.json).

```sh
dotnet tool restore
dotnet restore Requisite.sln --locked-mode
dotnet fantomas src tests/Requisite.Tests/*.fs tests/compiler-contracts examples --check
dotnet build Requisite.sln --configuration Release --no-restore
dotnet test tests/Requisite.Tests/Requisite.Tests.fsproj -f net8.0 --configuration Release --no-build
dotnet test tests/Requisite.Tests/Requisite.Tests.fsproj -f net10.0 --configuration Release --no-build
dotnet pack src/Requisite/Requisite.fsproj --configuration Release --no-build
dotnet run --project examples/PaymentFlow/PaymentFlow.fsproj -f net8.0 --configuration Release
```

Compiler-contract tests include exact diagnostic codes, a positive control,
private-construction failures, disabled gate equality, and `FS0025` promoted to
an error.

## Guides

- [API and design notes](https://github.com/slepp/requisite-fsharp/blob/main/docs/api.md)
- [Trust](https://github.com/slepp/requisite-fsharp/blob/main/docs/trust.md)
- [Confidence](https://github.com/slepp/requisite-fsharp/blob/main/docs/confidence.md)
- [Freshness](https://github.com/slepp/requisite-fsharp/blob/main/docs/freshness.md)
- [Payment-flow example](https://github.com/slepp/requisite-fsharp/blob/main/examples/PaymentFlow/Program.fs)
- [Changelog](https://github.com/slepp/requisite-fsharp/blob/main/CHANGELOG.md)
- [Releasing](https://github.com/slepp/requisite-fsharp/blob/main/RELEASING.md)

## License

Licensed under either Apache License 2.0 or the MIT license, at your option.
