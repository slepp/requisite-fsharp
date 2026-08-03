open System
open Requisite

type Money = { Cents: int64 }

let readCustomerId raw = Tainted.fromInput raw

let sanitizeCustomerId raw =
    let digits =
        raw
        |> Seq.filter Char.IsAsciiDigit
        |> Seq.toArray
        |> fun chars -> String(chars)

    match Int64.TryParse digits with
    | true, customerId -> Ok customerId
    | false, _ -> Error "customer id contained no digits"

let loadCustomer (customerId: Tainted<int64, Trusted>) = $"customer<{Tainted.value customerId}>"

let wakeOperator (_: Certain) reason =
    printfn "*** WAKE OPERATOR: %s ***" reason

let customerResult =
    readCustomerId "42; DROP TABLE customers"
    |> Tainted.trySanitize sanitizeCustomerId
    |> Result.map loadCustomer

match customerResult with
| Error error -> eprintfn "customer lookup refused: %s" error
| Ok customer ->
    printfn "resolved %s" customer

    match Fresh.create (TimeSpan.FromSeconds 30.0) { Cents = 499L } with
    | Error error -> eprintfn "quote rejected: %O" error
    | Ok quote ->
        match Fresh.tryGetWithRecovery quote with
        | Ok price -> printfn "charged %d cents" price.Cents
        | Error expired -> eprintfn "stale quote retained for audit: %O" expired

match Confident.create 0.97 "strong aurora" |> Result.map Confident.gate with
| Error error -> eprintfn "invalid forecast: %O" error
| Ok(Gate.HighConfidence(proof, reason)) -> wakeOperator proof reason
| Ok(Gate.Likely reason) -> printfn "silent notification: %s" reason
| Ok(Gate.Unsure reason) -> printfn "logged: %s" reason
