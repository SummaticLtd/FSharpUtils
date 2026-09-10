module Tests.SignalTests

open System
open System.Collections.Immutable
open SimpleTests
open FSUtils

/// Builds a signal whose sources are reachable only through it, and a weak handle on one source.
let private mappedFromSourcesThatGoOutOfScope() =
    let m = Mutable.create 1
    let sources = ImmutableArray.Create<ISignal<int>>(m.AsSignal)
    WeakReference m, Signal.mapFromImmArray (fun (a: ImmutableArray<int>) -> a.[0] * 10) sources

let SignalTestList =
    TestList("Signal", [
        Test.Sync("a Mutable reports a change only when the value differs", fun () ->
            let m = Mutable.create 1
            let seen = ResizeArray<int>()
            use _sub = m.AsSignal |> Signal.subscribe seen.Add
            m.Value <- 1
            Assert.Equal(0, seen.Count, "setting the same value is not a change")
            m.Value <- 2
            Assert.Equal(1, seen.Count)
            Assert.Equal(2, seen.[0]))

        Test.Sync("disposing a subscription stops it receiving", fun () ->
            let m = Mutable.create 1
            let seen = ResizeArray<int>()
            let sub = m.AsSignal |> Signal.subscribe seen.Add
            m.Value <- 2
            sub.Dispose()
            m.Value <- 3
            Assert.Equal(1, seen.Count, "only the change before disposal arrived")
            Assert.Equal(2, seen.[0]))

        Test.Sync("map projects the current value and follows changes", fun () ->
            let m = Mutable.create 2
            let mapped = m.AsSignal |> Signal.map(fun x -> x * 10)
            Assert.Equal(20, mapped.Value, "projected on creation")
            m.Value <- 3
            Assert.Equal(30, mapped.Value, "and again when the source changes"))

        Test.Sync("map2 follows either source", fun () ->
            let a = Mutable.create 1
            let b = Mutable.create 2
            let sum = Signal.map2 (fun (x, y) -> x + y) (a.AsSignal, b.AsSignal)
            Assert.Equal(3, sum.Value)
            a.Value <- 10
            Assert.Equal(12, sum.Value, "after the first changed")
            b.Value <- 20
            Assert.Equal(30, sum.Value, "after the second changed"))

        Test.Sync("a constant signal keeps its value", fun () ->
            let c = Signal.constant 7
            let seen = ResizeArray<int>()
            use _sub = c |> Signal.subscribe seen.Add
            Assert.Equal(7, c.Value)
            Assert.Equal(0, seen.Count, "it never reports a change"))

        Test.Sync("bind runs immediately and on every change", fun () ->
            let m = Mutable.create 1
            let seen = ResizeArray<int>()
            use _sub = m.AsSignal |> Signal.bind seen.Add
            Assert.Equal(1, seen.Count, "the current value is delivered at once")
            Assert.Equal(1, seen.[0])
            m.Value <- 2
            Assert.Equal(2, seen.Count)
            Assert.Equal(2, seen.[1]))

        Test.Sync("dispMap disposes each value's scope when the next arrives", fun () ->
            let disposedFor = ResizeArray<int>()
            let cd = new SimpleCD()
            let m = Mutable.create 1
            let mapped =
                m.AsSignal
                |> Signal.dispMap cd (fun (scd, v) ->
                    { new IDisposable with member _.Dispose() = disposedFor.Add v } |> scd.Add
                    v * 10)
            Assert.Equal(10, mapped.Value)
            Assert.Equal(0, disposedFor.Count, "the first scope is still live")
            m.Value <- 2
            Assert.Equal(20, mapped.Value)
            Assert.Equal(1, disposedFor.Count, "the scope for the previous value was released")
            Assert.Equal(1, disposedFor.[0])
            cd.Dispose()
            Assert.Equal(2, disposedFor.Count, "and the last one goes with the owning CD"))

        Test.Sync("mapFromImmArray keeps its sources alive, as map does", fun () ->
            let source, mapped = mappedFromSourcesThatGoOutOfScope()
            GC.Collect()
            GC.WaitForPendingFinalizers()
            GC.Collect()
            Assert.True(source.IsAlive, "a source reachable only through the mapped signal is still rooted")
            Assert.Equal(10, mapped.Value))
    ])
