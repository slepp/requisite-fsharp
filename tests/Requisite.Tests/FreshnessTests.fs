namespace Requisite.Tests

open System
open Requisite
open Xunit

module FreshnessTests =
    let private expectFresh result =
        match result with
        | Ok fresh -> fresh
        | Error error -> failwithf "unexpected fresh-value creation error: %A" error

    [<Fact>]
    let ``fresh values are checked at each read`` () =
        let ttl = TimeSpan.FromMinutes 1.0
        let fresh = Fresh.create ttl 7 |> expectFresh

        Assert.Equal<Result<int, Stale>>(Ok 7, Fresh.tryGet fresh)
        Assert.InRange(Fresh.remaining fresh, TimeSpan.Zero, ttl)

    [<Fact>]
    let ``negative TTL is rejected`` () =
        match Fresh.create (TimeSpan.FromTicks(-1L)) "quote" with
        | Error(FreshCreationError.NegativeTtl ttl) -> Assert.Equal(TimeSpan.FromTicks(-1L), ttl)
        | Error(FreshCreationError.FetchTimeInFuture _) -> failwith "expected a negative TTL error"
        | Ok _ -> failwith "expected a negative TTL to be rejected"

    [<Fact>]
    let ``stale recovery retains the value and timing`` () =
        let fetched =
            MonotonicTimestamp.now ()
            |> MonotonicTimestamp.offsetBy (TimeSpan.FromSeconds(-2.0))

        let ttl = TimeSpan.FromSeconds 1.0
        let fresh = Fresh.fetchedAt fetched ttl "quote" |> expectFresh

        match Fresh.tryGetWithRecovery fresh with
        | Error expired ->
            Assert.Equal("quote", expired.Value)
            Assert.Equal(ttl, expired.Stale.Ttl)
            Assert.True(expired.Stale.Age >= TimeSpan.FromSeconds 2.0)
            Assert.Equal(TimeSpan.Zero, Fresh.remaining fresh)
        | Ok _ -> failwith "expected the value to be stale"

    [<Fact>]
    let ``future monotonic fetch times are rejected`` () =
        let future =
            MonotonicTimestamp.now ()
            |> MonotonicTimestamp.offsetBy (TimeSpan.FromSeconds 2.0)

        match Fresh.fetchedAt future (TimeSpan.FromSeconds 1.0) 7 with
        | Error(FreshCreationError.FetchTimeInFuture aheadBy) -> Assert.True(aheadBy > TimeSpan.FromSeconds 1.0)
        | Error(FreshCreationError.NegativeTtl _) -> failwith "expected a future fetch-time error"
        | Ok _ -> failwith "expected a future timestamp to be rejected"

    [<Fact>]
    let ``extreme monotonic offsets saturate without elapsed overflow`` () =
        let now = MonotonicTimestamp.now ()
        let earliest = now |> MonotonicTimestamp.offsetBy TimeSpan.MinValue
        let latest = now |> MonotonicTimestamp.offsetBy TimeSpan.MaxValue

        let old = Fresh.fetchedAt earliest TimeSpan.Zero "old" |> expectFresh
        Assert.InRange(Fresh.age old, TimeSpan.Zero, TimeSpan.MaxValue)

        match Fresh.fetchedAt latest TimeSpan.Zero "future" with
        | Error(FreshCreationError.FetchTimeInFuture aheadBy) ->
            Assert.InRange(aheadBy, TimeSpan.Zero, TimeSpan.MaxValue)
        | Error(FreshCreationError.NegativeTtl _) -> failwith "expected a future fetch-time error"
        | Ok _ -> failwith "expected the saturated future timestamp to be rejected"

    [<Fact>]
    let ``stale value ToString omits the payload`` () =
        let stale =
            { Value = "sensitive payload"
              Stale =
                { Age = TimeSpan.FromSeconds 2.0
                  Ttl = TimeSpan.FromSeconds 1.0 } }

        let text = stale.ToString()
        Assert.DoesNotContain("sensitive payload", text)
        Assert.Contains("stale:", text)
