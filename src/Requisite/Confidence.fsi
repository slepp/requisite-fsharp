namespace Requisite

/// A finite probability in the closed interval 0.0 through 1.0.
[<NoComparison>]
type Confidence

/// Describes a rejected probability.
type InvalidConfidence =
    { Value: float }

    override ToString: unit -> string

/// Operations for validated probabilities.
[<RequireQualifiedAccess>]
module Confidence =
    /// Validates a finite probability in the closed interval 0.0 through 1.0.
    val create: value: float -> Result<Confidence, InvalidConfidence>

    /// Returns the validated probability.
    val value: confidence: Confidence -> float

/// Identifies why a pair of confidence thresholds was rejected.
[<RequireQualifiedAccess>]
type ThresholdError =
    | InvalidLikely
    | InvalidCertain
    | CertainBelowMinimum
    | LikelyNotBelowCertain

/// Describes rejected confidence thresholds.
type InvalidThresholds =
    { Likely: float
      Certain: float
      Reason: ThresholdError }

    override ToString: unit -> string

/// Valid confidence boundaries for Confidence.gateWith.
[<NoComparison>]
type Thresholds

/// Operations for constructing and reading confidence thresholds.
[<RequireQualifiedAccess>]
module Thresholds =
    /// The minimum probability represented by a Certain token.
    [<Literal>]
    val MinimumCertain: float = 0.95

    /// The default boundaries: likely at 0.60 and certain at 0.95.
    val defaultValue: Thresholds

    /// Validates likely and certain boundaries.
    ///
    /// Both values must be valid probabilities, likely must be lower than
    /// certain, and certain must be at least MinimumCertain.
    val create: likely: float -> certain: float -> Result<Thresholds, InvalidThresholds>

    /// Returns the likely boundary.
    val likely: thresholds: Thresholds -> Confidence

    /// Returns the certain boundary.
    val certain: thresholds: Thresholds -> Confidence

/// Proof that a confidence gate selected its highest tier.
///
/// Ordinary checked F# code cannot construct the representation. This is not a
/// linear capability: a token can be retained and reused, and .NET escape
/// hatches such as reflection or Unchecked.defaultof can bypass the guarantee.
[<NoEquality; NoComparison>]
type Certain

/// A value classified into one of the complete confidence tiers.
[<RequireQualifiedAccess; NoEquality; NoComparison>]
type Gate<'T> =
    | HighConfidence of proof: Certain * value: 'T
    | Likely of value: 'T
    | Unsure of value: 'T

/// A value paired with a validated confidence.
[<NoEquality; NoComparison>]
type Confident<'T>

/// Operations for confidence-tagged values.
[<RequireQualifiedAccess>]
module Confident =
    /// Validates a probability and attaches it to a value.
    ///
    /// The argument order supports: value |> Confident.create probability.
    val create: probability: float -> value: 'T -> Result<Confident<'T>, InvalidConfidence>

    /// Attaches an already validated confidence to a value.
    val attach: confidence: Confidence -> value: 'T -> Confident<'T>

    /// Returns the validated confidence.
    val confidence: confident: Confident<'T> -> Confidence

    /// Classifies a value using Thresholds.defaultValue.
    val gate: confident: Confident<'T> -> Gate<'T>

    /// Classifies a value using application-defined boundaries.
    val gateWith: thresholds: Thresholds -> confident: Confident<'T> -> Gate<'T>
