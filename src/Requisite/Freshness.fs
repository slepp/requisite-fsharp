namespace Requisite

open System
open System.Diagnostics

[<NoEquality; NoComparison>]
type MonotonicTimestamp = private MonotonicTicks of int64

[<RequireQualifiedAccess>]
module MonotonicTimestamp =
    let now () =
        MonotonicTicks(Stopwatch.GetTimestamp())

    let private clampInt64 value =
        if value > decimal Int64.MaxValue then Int64.MaxValue
        elif value < decimal Int64.MinValue then Int64.MinValue
        else Decimal.ToInt64 value

    let offsetBy (duration: TimeSpan) (MonotonicTicks timestamp) =
        let delta =
            decimal duration.Ticks * decimal Stopwatch.Frequency
            / decimal TimeSpan.TicksPerSecond

        let adjusted =
            decimal timestamp + Decimal.Round(delta, 0, MidpointRounding.AwayFromZero)

        MonotonicTicks(clampInt64 adjusted)

    let internal isAfter (MonotonicTicks left) (MonotonicTicks right) = left > right

    let internal elapsed (MonotonicTicks started) (MonotonicTicks finished) =
        let stopwatchTicks = decimal finished - decimal started

        if stopwatchTicks <= 0M then
            TimeSpan.Zero
        else
            let timeSpanTicks =
                stopwatchTicks * decimal TimeSpan.TicksPerSecond / decimal Stopwatch.Frequency

            if timeSpanTicks >= decimal TimeSpan.MaxValue.Ticks then
                TimeSpan.MaxValue
            else
                let rounded = Decimal.Round(timeSpanTicks, 0, MidpointRounding.AwayFromZero)
                TimeSpan.FromTicks(Decimal.ToInt64 rounded)

[<RequireQualifiedAccess>]
type FreshCreationError =
    | NegativeTtl of ttl: TimeSpan
    | FetchTimeInFuture of aheadBy: TimeSpan

type Stale =
    { Age: TimeSpan
      Ttl: TimeSpan }

    override this.ToString() =
        $"stale: age {this.Age} exceeds ttl {this.Ttl}"

[<NoEquality; NoComparison>]
type StaleValue<'T> =
    { Value: 'T
      Stale: Stale }

    override this.ToString() = this.Stale.ToString()

[<NoEquality; NoComparison>]
type Fresh<'T> = private FreshValue of value: 'T * fetchedAt: MonotonicTimestamp * ttl: TimeSpan

[<RequireQualifiedAccess>]
module Fresh =
    let fetchedAt fetchedAt ttl value =
        if ttl < TimeSpan.Zero then
            Error(FreshCreationError.NegativeTtl ttl)
        else
            let now = MonotonicTimestamp.now ()

            if MonotonicTimestamp.isAfter fetchedAt now then
                let aheadBy = MonotonicTimestamp.elapsed now fetchedAt
                Error(FreshCreationError.FetchTimeInFuture aheadBy)
            else
                Ok(FreshValue(value, fetchedAt, ttl))

    let create ttl value =
        fetchedAt (MonotonicTimestamp.now ()) ttl value

    let age (FreshValue(_, fetchedAt, _)) =
        MonotonicTimestamp.elapsed fetchedAt (MonotonicTimestamp.now ())

    let private staleAt age ttl = { Age = age; Ttl = ttl }

    let tryGet (FreshValue(value, _, ttl) as fresh) =
        let currentAge = age fresh

        if currentAge <= ttl then
            Ok value
        else
            Error(staleAt currentAge ttl)

    let tryGetWithRecovery (FreshValue(value, _, _) as fresh) =
        match tryGet fresh with
        | Ok current -> Ok current
        | Error stale -> Error { Value = value; Stale = stale }

    let remaining (FreshValue(_, _, ttl) as fresh) =
        let left = ttl - age fresh

        if left > TimeSpan.Zero then left else TimeSpan.Zero
