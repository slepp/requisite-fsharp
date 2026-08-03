# Trust

Create untrusted wrappers at input boundaries and make trusted sinks accept
`Tainted<'T, Trusted>`. Only `sanitize` and `trySanitize` produce that state.

```fsharp
let validate raw =
    if System.String.IsNullOrWhiteSpace raw then Error "empty"
    else Ok(raw.Trim())

let clean =
    Tainted.fromInput requestValue
    |> Tainted.trySanitize validate
```

The sanitizer may change the value type. `Tainted.inspect` does not promote
trust, and `Tainted.widen` only moves from trusted to untrusted.

