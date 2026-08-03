namespace Requisite.Tests

open System
open Requisite
open Xunit

module TrustTests =
    [<Fact>]
    let ``sanitize can change the value type`` () =
        let trusted =
            Tainted.fromInput " 42 "
            |> Tainted.sanitize (fun value -> Int64.Parse(value.Trim()))

        Assert.Equal(42L, Tainted.value trusted)

    [<Fact>]
    let ``trySanitize composes with Result`` () =
        let parse (value: string) =
            match Int32.TryParse value with
            | true, parsed -> Ok parsed
            | false, _ -> Error "not an integer"

        let result =
            Tainted.fromInput "17" |> Tainted.trySanitize parse |> Result.map Tainted.value

        Assert.Equal<Result<int, string>>(Ok 17, result)

    [<Fact>]
    let ``failed sanitizer preserves its error`` () =
        let result =
            Tainted.fromInput "customer"
            |> Tainted.trySanitize (fun _ -> Error "invalid customer id")

        Assert.Equal<Result<Tainted<int, Trusted>, string>>(Error "invalid customer id", result)

    [<Fact>]
    let ``trusted values can satisfy a lower trust requirement`` () =
        let trusted =
            Tainted.fromInput "  customer-42 "
            |> Tainted.sanitize (fun value -> value.Trim())

        let untrustedAgain = Tainted.widen trusted
        Assert.Equal("customer-42", Tainted.inspect untrustedAgain)
