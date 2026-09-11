namespace FSUtils

open System
open System.Threading

[<Sealed>]
type DisposableTimer(getTimer:Func<Action, IDisposable>) =
    member _.StartTimer = getTimer
    static member SystemThreading(ts:TimeSpan) =
        let getTimer(f:Action) =
            let callback(_:obj | null) =
                f.Invoke()
            new Timer(callback, null, TimeSpan.Zero, ts) :>IDisposable
        new DisposableTimer(getTimer)
    /// Never ticks, so animated transitions stay at the values applied synchronously when the step is shown.
    static member Never =
        let getTimer(_:Action) =
            { new IDisposable with member _.Dispose() = () }
        new DisposableTimer(getTimer)
