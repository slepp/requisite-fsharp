open System
open Requisite

let consumeCertain (_: Certain) value = value

let trusted =
    Tainted.fromInput "42"
    |> Tainted.trySanitize (fun value ->
        match Int32.TryParse value with
        | true, parsed -> Ok parsed
        | false, _ -> Error "invalid")

let gated =
    Confident.create 0.99 "signal"
    |> Result.map (fun confident ->
        match Confident.gate confident with
        | Gate.HighConfidence(proof, value) -> consumeCertain proof value
        | Gate.Likely value -> value
        | Gate.Unsure value -> value)

let fresh =
    match Fresh.create (TimeSpan.FromSeconds 1.0) "quote" with
    | Ok value -> Fresh.tryGet value
    | Error error -> failwithf "unexpected creation error: %A" error

match trusted, gated, fresh with
| Ok trustedValue, Ok "signal", Ok "quote" when Tainted.value trustedValue = 42 -> ()
| state -> failwithf "unexpected positive-control state: %A" state
