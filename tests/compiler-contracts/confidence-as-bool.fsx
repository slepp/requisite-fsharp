open Requisite

match Confident.create 0.80 "signal" with
| Ok confident ->
    if confident then
        printfn "accepted"
| Error _ -> ()
