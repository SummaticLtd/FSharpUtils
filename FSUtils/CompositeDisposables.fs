namespace FSUtils

open System
open System.Threading
open System.Runtime.CompilerServices
open System.Collections.Generic

/// A simple CompositeDisposable, where additions should not be performed after disposal, and disposal should only be done once. Disposal blocks until everything is released, then raises an AggregateException carrying any failures, so it is unsuitable for a tight loop (e.g. a slider update).
[<Sealed>]
type SimpleCD([<CallerFilePath>] ?invokedFromFile : string, [<CallerLineNumber>] ?invokedFromLine : int) =
    let disps = Stack<IDisposable>()
    let mutable disposeRequested = false
    let origin() = String.Format("{0} line {1}", defaultArg invokedFromFile "", defaultArg invokedFromLine -1)
    member _.Add(d:IDisposable) =
        if disposeRequested then
            Log.Error("Additions should not occur after disposal. From " + origin())
            d.Dispose()
        else disps.Push d
    member t.Adding (d:'a when 'a:>IDisposable) = t.Add d; d
    member _.Dispose() =
        if disposeRequested then
            Log.Error("Disposal should not occur after disposal. From " + origin())
        else
            disposeRequested <- true
            let failures = ResizeArray<exn>()
            // Pop inside the try, so one failing item cannot strand the ones below it
            while disps.Count > 0 do
                try disps.Pop().Dispose() with ex -> failures.Add ex
            if failures.Count > 0 then raise (AggregateException("SimpleCD disposal, from " + origin(), failures))
    member _.DisposedRequested = disposeRequested
#if DEBUG
    override _.Finalize() =
        if not disposeRequested then
            Log.Error("Finalize is called before Dispose, from " + origin())
#endif
    interface IDisposable with member t.Dispose() = t.Dispose()

module Disposable =
    let empty = { new IDisposable with member _.Dispose() = () }
    let dispose (disposable: IDisposable) = disposable.Dispose()

/// Represents a disposable whose underlying disposable
/// can be replaced by another disposable,
/// causing automatic disposal of the previous underlying disposable.
// If a switch happens after disposal, immediately dispose of it
[<Sealed>]
#if DEBUG
type SerialDisposable([<CallerFilePath>] ?invokedFromFile : string, [<CallerLineNumber>] ?invokedFromLine : int) =
#else
type SerialDisposable() =
#endif
    let mutable disposable = Disposable.empty
    let mutable isDisposed = false

    member _.Switch(newDisp:IDisposable) =
        if isDisposed then
            newDisp.Dispose()
        else
            let current = Volatile.Read &disposable
            Volatile.Write (&disposable, newDisp)
            current.Dispose()

    member t.Switching(newDisp:'a when 'a:>IDisposable) = t.Switch newDisp; newDisp

    member t.Dispose() =
        // finally, so a failing inner disposable still leaves this marked disposed
        try t.Switch(Disposable.empty)
        finally isDisposed <- true
    member t.DisposeInner() = t.Switch(Disposable.empty)

#if DEBUG
    override _.Finalize() =
        if not isDisposed then
            Log.Error(String.Format("Finalize is called before Dispose, from {0} Line {1}", defaultArg invokedFromFile "", defaultArg invokedFromLine -1))
#endif

    interface IDisposable with member t.Dispose() = t.Dispose()

/// Cancels its token when disposed. Add it to a CD to tie async work to that CD's lifetime.
[<Sealed>]
type CancellationScope() =
    let cts = new CancellationTokenSource()

    /// Pass to any async operation that should stop when this scope is disposed
    member _.Token = cts.Token
    member _.IsCancelled = cts.IsCancellationRequested

    // Cancels without disposing the source, so repeated disposal stays a no-op and the token stays readable
    member _.Dispose() = cts.Cancel()

    interface IDisposable with member t.Dispose() = t.Dispose()
