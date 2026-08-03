namespace Requisite

/// Marks a value received from an external input boundary.
type Untrusted

/// Marks a value that passed through an application-defined sanitizer.
type Trusted

/// A value carrying a compile-time trust state.
///
/// The representation is hidden. Trusted values can only be produced by
/// Tainted.sanitize or Tainted.trySanitize.
[<NoEquality; NoComparison>]
type Tainted<'T, 'Trust>

/// Operations for creating, observing, and transitioning trust-tagged values.
[<RequireQualifiedAccess>]
module Tainted =
    /// Marks a value received at an input boundary as untrusted.
    val fromInput: value: 'T -> Tainted<'T, Untrusted>

    /// Observes the wrapped value without changing its trust state.
    ///
    /// Typed sinks should still require Tainted&lt;_, Trusted&gt; rather than a
    /// bare value.
    val inspect: tainted: Tainted<'T, 'Trust> -> 'T

    /// Extracts a value that has passed through a sanitizer.
    val value: trusted: Tainted<'T, Trusted> -> 'T

    /// Applies an infallible policy and marks the transformed value as trusted.
    val sanitize: sanitizer: ('T -> 'U) -> tainted: Tainted<'T, Untrusted> -> Tainted<'U, Trusted>

    /// Applies a fallible policy. A successful result is marked as trusted.
    val trySanitize:
        sanitizer: ('T -> Result<'U, 'Error>) -> tainted: Tainted<'T, Untrusted> -> Result<Tainted<'U, Trusted>, 'Error>

    /// Lowers a trusted value to an untrusted requirement.
    val widen: trusted: Tainted<'T, Trusted> -> Tainted<'T, Untrusted>
