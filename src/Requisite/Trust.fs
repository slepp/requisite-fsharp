namespace Requisite

type Untrusted = private | Untrusted

type Trusted = private | Trusted

[<NoEquality; NoComparison>]
type Tainted<'T, 'Trust> = private TaintedValue of 'T

[<RequireQualifiedAccess>]
module Tainted =
    let fromInput value : Tainted<'T, Untrusted> = TaintedValue value

    let inspect (TaintedValue value) = value

    let value (trusted: Tainted<'T, Trusted>) =
        let (TaintedValue value) = trusted
        value

    let sanitize sanitizer (TaintedValue value: Tainted<'T, Untrusted>) : Tainted<'U, Trusted> =
        TaintedValue(sanitizer value)

    let trySanitize sanitizer (TaintedValue value: Tainted<'T, Untrusted>) =
        sanitizer value
        |> Result.map (fun sanitized -> TaintedValue sanitized: Tainted<'U, Trusted>)

    let widen (trusted: Tainted<'T, Trusted>) : Tainted<'T, Untrusted> =
        let (TaintedValue value) = trusted
        TaintedValue value
