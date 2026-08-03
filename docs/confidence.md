# Confidence

Probabilities are validated before use:

```fsharp
let forecast = Confident.create 0.82 predictedEvent
```

Handle all `Gate` cases near the resulting action. The high-confidence case
alone contains `Certain`, whose construction is hidden by the assembly's
signature.

`Gate` has no structural equality or comparison. This avoids the misleading
result where two independently issued high-confidence cases differ only
because their proof objects have different identities.

Custom thresholds are also validated:

```fsharp
Thresholds.create 0.75 0.99
```

The certain boundary cannot be lower than `0.95`, preserving one consistent
meaning for every `Certain` token.

## Exhaustiveness and proof limits

F# emits `FS0025` for an incomplete `Gate` match. It is only a warning unless
the consuming project promotes it:

```xml
<WarningsAsErrors>$(WarningsAsErrors);25</WarningsAsErrors>
```

`Certain` is an ordinary reusable .NET value, not a linear capability. It may
escape the match, be reused, and is not bound to a particular payload. Its
hidden representation prevents normal construction, but reflection,
`Unchecked.defaultof`, emitted IL, and similar escape hatches remain outside
the guarantee.
