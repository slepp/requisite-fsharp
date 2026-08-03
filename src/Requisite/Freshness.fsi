namespace Requisite

open System

/// A process-local timestamp from the platform monotonic clock.
///
/// Monotonic timestamps are suitable for elapsed-time checks, not persistence
/// or interchange between processes.
[<NoEquality; NoComparison>]
type MonotonicTimestamp

/// Operations for process-local monotonic timestamps.
[<RequireQualifiedAccess>]
module MonotonicTimestamp =
    /// Reads the current platform monotonic clock.
    val now: unit -> MonotonicTimestamp

    /// Returns a timestamp offset by a signed duration.
    ///
    /// This is primarily useful for adapting existing timestamps and writing
    /// deterministic tests. Extreme offsets saturate at the representable
    /// monotonic timestamp bounds.
    val offsetBy: duration: TimeSpan -> timestamp: MonotonicTimestamp -> MonotonicTimestamp

/// Identifies why a fresh value could not be created.
[<RequireQualifiedAccess>]
type FreshCreationError =
    | NegativeTtl of ttl: TimeSpan
    | FetchTimeInFuture of aheadBy: TimeSpan

/// Timing information returned when a value has exceeded its TTL.
type Stale =
    { Age: TimeSpan
      Ttl: TimeSpan }

    override ToString: unit -> string

/// A stale check that retains the expired value for typed recovery.
[<NoEquality; NoComparison>]
type StaleValue<'T> =
    { Value: 'T
      Stale: Stale }

    override ToString: unit -> string

/// A value with a process-local monotonic fetch time and a non-negative TTL.
[<NoEquality; NoComparison>]
type Fresh<'T>

/// Operations for creating and checking fresh values.
[<RequireQualifiedAccess>]
module Fresh =
    /// Records a value as fetched now with the supplied TTL.
    val create: ttl: TimeSpan -> value: 'T -> Result<Fresh<'T>, FreshCreationError>

    /// Records a value with an existing process-local monotonic fetch time.
    ///
    /// Negative TTLs and timestamps ahead of the current monotonic clock are
    /// rejected.
    val fetchedAt: fetchedAt: MonotonicTimestamp -> ttl: TimeSpan -> value: 'T -> Result<Fresh<'T>, FreshCreationError>

    /// Checks freshness at this call and returns the value while age <= TTL.
    val tryGet: fresh: Fresh<'T> -> Result<'T, Stale>

    /// Checks freshness and includes the value in the stale branch.
    ///
    /// F# does not have move semantics; the wrapper is not consumed. This
    /// operation exists to make stale-value recovery explicit and typed.
    val tryGetWithRecovery: fresh: Fresh<'T> -> Result<'T, StaleValue<'T>>

    /// Returns the age measured at this call.
    val age: fresh: Fresh<'T> -> TimeSpan

    /// Returns the remaining TTL measured at this call, saturating at zero.
    val remaining: fresh: Fresh<'T> -> TimeSpan
