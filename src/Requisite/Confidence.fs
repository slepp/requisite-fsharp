namespace Requisite

open System
open System.Globalization

[<AutoOpen>]
module private ConfidenceInternals =
    [<Literal>]
    let MinimumCertain = 0.95

    let isFinite value =
        not (Double.IsNaN value || Double.IsInfinity value)

    let normalizeZero value = if value = 0.0 then 0.0 else value

    let formatFloat value =
        if Double.IsNaN value then
            "NaN"
        elif Double.IsPositiveInfinity value then
            "positive infinity"
        elif Double.IsNegativeInfinity value then
            "negative infinity"
        else
            value.ToString("R", CultureInfo.InvariantCulture)

[<StructuralEquality; NoComparison>]
type Confidence = private ConfidenceValue of float

type InvalidConfidence =
    { Value: float }

    override this.ToString() =
        $"Confidence must be finite and between 0.0 and 1.0 inclusive; received {formatFloat this.Value}."

[<RequireQualifiedAccess>]
module Confidence =
    let create value =
        if isFinite value && value >= 0.0 && value <= 1.0 then
            Ok(ConfidenceValue(normalizeZero value))
        else
            Error { Value = value }

    let value (ConfidenceValue value) = value

[<RequireQualifiedAccess>]
type ThresholdError =
    | InvalidLikely
    | InvalidCertain
    | CertainBelowMinimum
    | LikelyNotBelowCertain

type InvalidThresholds =
    { Likely: float
      Certain: float
      Reason: ThresholdError }

    override this.ToString() =
        let reason =
            match this.Reason with
            | ThresholdError.InvalidLikely -> "likely must be a valid confidence"
            | ThresholdError.InvalidCertain -> "certain must be a valid confidence"
            | ThresholdError.CertainBelowMinimum -> $"certain must be at least {formatFloat MinimumCertain}"
            | ThresholdError.LikelyNotBelowCertain -> "likely must be lower than certain"

        $"Confidence thresholds are invalid: {reason}; received likely={formatFloat this.Likely}, certain={formatFloat this.Certain}."

[<StructuralEquality; NoComparison>]
type Thresholds = private ThresholdValues of likely: Confidence * certain: Confidence

[<RequireQualifiedAccess>]
module Thresholds =
    [<Literal>]
    let MinimumCertain = ConfidenceInternals.MinimumCertain

    let defaultValue =
        ThresholdValues(ConfidenceValue 0.60, ConfidenceValue MinimumCertain)

    let private invalid likely certain reason =
        Error
            { Likely = likely
              Certain = certain
              Reason = reason }

    let create likely certain =
        match Confidence.create likely with
        | Error _ -> invalid likely certain ThresholdError.InvalidLikely
        | Ok validLikely ->
            match Confidence.create certain with
            | Error _ -> invalid likely certain ThresholdError.InvalidCertain
            | Ok _ when certain < MinimumCertain -> invalid likely certain ThresholdError.CertainBelowMinimum
            | Ok _ when likely >= certain -> invalid likely certain ThresholdError.LikelyNotBelowCertain
            | Ok validCertain -> Ok(ThresholdValues(validLikely, validCertain))

    let likely (ThresholdValues(likely, _)) = likely

    let certain (ThresholdValues(_, certain)) = certain

[<NoEquality; NoComparison>]
type Certain = internal | CertainToken

[<RequireQualifiedAccess; NoEquality; NoComparison>]
type Gate<'T> =
    | HighConfidence of proof: Certain * value: 'T
    | Likely of value: 'T
    | Unsure of value: 'T

[<NoEquality; NoComparison>]
type Confident<'T> = private ConfidentValue of value: 'T * confidence: Confidence

[<RequireQualifiedAccess>]
module Confident =
    let create probability value =
        Confidence.create probability
        |> Result.map (fun confidence -> ConfidentValue(value, confidence))

    let attach confidence value = ConfidentValue(value, confidence)

    let confidence (ConfidentValue(_, confidence)) = confidence

    let gateWith (ThresholdValues(likely, certain)) (ConfidentValue(value, confidence)) =
        let probability = Confidence.value confidence

        if probability >= Confidence.value certain then
            Gate.HighConfidence(CertainToken, value)
        elif probability >= Confidence.value likely then
            Gate.Likely value
        else
            Gate.Unsure value

    let gate confident =
        gateWith Thresholds.defaultValue confident
