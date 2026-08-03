open Requisite

let left = Confident.create 0.99 "signal" |> Result.map Confident.gate
let right = Confident.create 0.99 "signal" |> Result.map Confident.gate
let same = left = right
