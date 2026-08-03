# Freshness

`Fresh<'T>` stores a value, a process-local monotonic timestamp, and a TTL.
Each read performs a new elapsed-time check.

```fsharp
match Fresh.tryGetWithRecovery quote with
| Ok current -> useQuote current
| Error expired ->
    archive expired.Value
    refresh expired.Stale
```

Negative TTLs and future fetch timestamps are creation errors. Use
`MonotonicTimestamp.offsetBy` with an earlier timestamp for deterministic
stale tests instead of sleeping. Extreme offsets saturate at the representable
timestamp bounds; elapsed conversion saturates at `TimeSpan.MaxValue`.

`StaleValue.ToString()` includes only stale timing and does not call
`ToString()` on the payload. The payload is still intentionally public for
recovery, so `%A` debug formatting can reveal it.

The Rust `Live`/`with_live` feature has no F# port because .NET generics cannot
express its non-escaping lifetime brand.
