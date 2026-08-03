open Requisite

let describe gate =
    match gate with
    | Gate.HighConfidence(_, value) -> value
