namespace Requisite.Tests

open System
open System.Globalization
open Requisite
open Xunit

module ConfidenceTests =
    let private expectConfident probability value =
        match Confident.create probability value with
        | Ok confident -> confident
        | Error error -> failwithf "unexpected invalid confidence: %O" error

    [<Fact>]
    let ``confidence rejects non-finite and out-of-range values`` () =
        let invalid = [ Double.NaN; Double.PositiveInfinity; -0.01; 1.01 ]

        for value in invalid do
            match Confidence.create value with
            | Error error ->
                if Double.IsNaN value then
                    Assert.True(Double.IsNaN error.Value)
                else
                    Assert.Equal(value, error.Value)
            | Ok _ -> failwithf "expected %A to be rejected" value

    [<Fact>]
    let ``negative zero is normalized`` () =
        let confidence =
            match Confidence.create -0.0 with
            | Ok confidence -> confidence
            | Error error -> failwithf "unexpected invalid confidence: %O" error

        Assert.Equal(0L, BitConverter.DoubleToInt64Bits(Confidence.value confidence))

    [<Fact>]
    let ``validated scalar values retain structural equality`` () =
        let confidence value =
            match Confidence.create value with
            | Ok confidence -> confidence
            | Error error -> failwithf "unexpected invalid confidence: %O" error

        Assert.True(confidence 0.5 = confidence 0.5)

        let thresholds =
            match Thresholds.create 0.60 0.95 with
            | Ok thresholds -> thresholds
            | Error error -> failwithf "unexpected invalid thresholds: %O" error

        Assert.True(Thresholds.defaultValue = thresholds)

    [<Fact>]
    let ``confidence error formatting is invariant and readable`` () =
        let message = { InvalidConfidence.Value = Double.PositiveInfinity }.ToString()

        Assert.Equal(
            "Confidence must be finite and between 0.0 and 1.0 inclusive; received positive infinity.",
            message
        )

    [<Fact>]
    let ``threshold error formatting uses concise invariant numbers`` () =
        let previousCulture = CultureInfo.CurrentCulture

        try
            CultureInfo.CurrentCulture <- CultureInfo.GetCultureInfo("fr-FR")

            let error =
                match Thresholds.create 0.5 0.94 with
                | Error error -> error
                | Ok _ -> failwith "expected thresholds to be rejected"

            Assert.Equal(
                "Confidence thresholds are invalid: certain must be at least 0.95; received likely=0.5, certain=0.94.",
                error.ToString()
            )
        finally
            CultureInfo.CurrentCulture <- previousCulture

    [<Fact>]
    let ``default gate classifies all confidence tiers`` () =
        let classify probability =
            match expectConfident probability "signal" |> Confident.gate with
            | Gate.HighConfidence(_, value) -> $"certain:{value}"
            | Gate.Likely value -> $"likely:{value}"
            | Gate.Unsure value -> $"unsure:{value}"

        Assert.Equal("certain:signal", classify 0.95)
        Assert.Equal("likely:signal", classify 0.60)
        Assert.Equal("unsure:signal", classify 0.59)

    [<Fact>]
    let ``highest gate issues the token required by a sensitive action`` () =
        let authorize (_: Certain) value = $"authorized:{value}"

        match expectConfident 0.99 "charge" |> Confident.gate with
        | Gate.HighConfidence(proof, value) -> Assert.Equal("authorized:charge", authorize proof value)
        | Gate.Likely _ -> failwith "expected the certain tier, got likely"
        | Gate.Unsure _ -> failwith "expected the certain tier, got unsure"

    [<Fact>]
    let ``custom thresholds are validated and applied`` () =
        let thresholds =
            match Thresholds.create 0.75 0.99 with
            | Ok thresholds -> thresholds
            | Error error -> failwithf "unexpected invalid thresholds: %O" error

        let result =
            match expectConfident 0.98 "forecast" |> Confident.gateWith thresholds with
            | Gate.HighConfidence _ -> "certain"
            | Gate.Likely _ -> "likely"
            | Gate.Unsure _ -> "unsure"

        Assert.Equal("likely", result)

    [<Fact>]
    let ``invalid threshold relationships have typed reasons`` () =
        let cases =
            [ Double.NaN, 0.95, ThresholdError.InvalidLikely
              0.60, Double.PositiveInfinity, ThresholdError.InvalidCertain
              0.50, 0.94, ThresholdError.CertainBelowMinimum
              0.99, 0.99, ThresholdError.LikelyNotBelowCertain ]

        for likely, certain, expectedReason in cases do
            match Thresholds.create likely certain with
            | Error error -> Assert.Equal(expectedReason, error.Reason)
            | Ok _ -> failwithf "expected thresholds (%A, %A) to be rejected" likely certain
