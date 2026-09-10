namespace FSUtils

open System
open System.Threading
open System.Runtime.CompilerServices
open System.Collections.Generic
open System.Collections.Concurrent

/// A simple CompositeDisposable, where additions should not be performed after disposal, and disposal should only be done once.
// Should not be used where disposals need to happen in a tight loop (e.g. on a slider update),
// since disposal runs synchronously and will block the current thread
[<Sealed>]
type SimpleCD([<CallerFilePath>] ?invokedFromFile : string, [<CallerLineNumber>] ?invokedFromLine : int) =
    let disps = Stack<IDisposable>()
    let mutable disposeRequested = false
    member _.Add(d:IDisposable) =
        if disposeRequested then
            Log.Error(String.Format("Additions should not occur after disposals. From {0} line {1}", defaultArg invokedFromFile "", defaultArg invokedFromLine -1))
            d.Dispose()
        else disps.Push d
    member t.Adding (d:'a when 'a:>IDisposable) = t.Add d; d
    member _.Dispose() =
        try // *very* small chance of two threads disposing at the same time, but if so we don't want to crash
            if disposeRequested then
                Log.Error(String.Format("Disposal should not occur after disposals. From {0} line {1}", defaultArg invokedFromFile "", defaultArg invokedFromLine -1))
            else
                disposeRequested <- true
                while disps.Count > 0 do
                    let disp = disps.Pop()
                    disp.Dispose()
        with ex ->
            Log.Exception(ex, "SimpleCD Disposal exception, possibly from multiple disposals")
    member _.DisposedRequested = disposeRequested
#if DEBUG
    override _.Finalize() =
        if not disposeRequested then
            Log.Error(String.Format("Finalize is called before Dispose, from {0} Line {1}", defaultArg invokedFromFile "", defaultArg invokedFromLine -1))
#endif
    interface IDisposable with member t.Dispose() = t.Dispose()

/// A thread-safe CompositeDisposable, which handles additions after disposal
/// (in which case they are disposed immediately).
/// Also allows async operations to subscribe to a CancellationToken which is triggered on dispose.
// createdFrom is used to help with diagnosing the place a CD is called from if something goes wrong
[<Sealed>]
type ConcurrentCD([<CallerFilePath>] ?invokedFromFile : string, [<CallerLineNumber>] ?invokedFromLine : int) =
    let bc = new BlockingCollection<IDisposable>(ConcurrentStack<IDisposable>())
    let mutable startDisposingDt = ValueNone:DateTime voption
    let cts = new CancellationTokenSource()

    // Thread-safe check of disposal. Returns bool to indicate whether the current thread can dispose
    // If false, should be a no-op
    // If true, triggers the cancellation token and sets the blocking collection to complete
    let currentThreadCanDispose =
        let dispObjLock = Lock()
        fun () ->
            withLock(dispObjLock, fun () ->
                match startDisposingDt with
                | ValueNone -> startDisposingDt <- ValueSome DateTime.UtcNow; true
                | ValueSome _ -> false) // Indicates multiple disposals for an object, which isn't great

    member _.Add(d:IDisposable) =
        match startDisposingDt with
        | ValueSome dt ->
            d.Dispose()
            // A thread has called _.Add more than 100ms after disposal has started.
            // This is a potential efficiency, and one method to improve efficiency may be to use an async method using the cancellation token
            if DateTime.UtcNow > dt.AddMilliseconds(1000.) then Log.Error(String.Format("A thread has called _.Add after disposal has started, from {0} line {1}", defaultArg invokedFromFile "", defaultArg invokedFromLine -1))
        | ValueNone ->
            try bc.Add d
            with ex ->
                Log.Exception(ex, String.Format("Disposal Exception from {0} line {1}", defaultArg invokedFromFile "", defaultArg invokedFromLine -1))
                d.Dispose() // Dispose anyway, gives the best case for users to continue w/out memory leaks

    member t.Adding (d:'a when 'a:>IDisposable) = t.Add (d:>IDisposable); d

    /// Cancellation token to pass to any async operations that will be adding to the collection
    // On dispose, the cancellation token is triggered, so that all async threads will abort, allowing for disposal
    member _.CancellationToken = cts.Token
    member _.Dispose() =
        if currentThreadCanDispose() then
            cts.Cancel() // Send cancellation flag to all ongoing threads
            bc.CompleteAdding() // Disallows bc.Add after this point, all threads should accept the cancellation token
            async {
                // a more efficient way to do "for d in bc do d.Dispose()"
                // not typed (IDisposable | null) since TryTake needs a non-null byref out, and only reads on true (item assigned)
                do  let mutable currentItem = Unchecked.defaultof<IDisposable>
                    while bc.TryTake &currentItem do currentItem.Dispose()
                match startDisposingDt with
                | ValueSome t ->
                    if DateTime.UtcNow > t.AddMilliseconds(1000.) || bc.Count > 0 then // Disposal has taken too long
                        Log.Error(String.Format("Disposal is taking too long; check that completion is set. Items: {0} From {1} Line {2}", bc.Count, defaultArg invokedFromFile "", defaultArg invokedFromLine -1))
                | ValueNone -> failwith "shouldn't happen"
                cts.Dispose()
                bc.Dispose()
            } |> Async.Start
        else
            Log.Error(String.Format("Object has multiple disposals. From {0} line {1}", defaultArg invokedFromFile "", defaultArg invokedFromLine -1))
    member _.DisposedRequested = startDisposingDt.IsSome

#if DEBUG
    override _.Finalize() =
        if startDisposingDt.IsNone then
            Log.Error(String.Format("Finalize is called before Dispose, from {0} Line {1}, Items: {2}", defaultArg invokedFromFile "", defaultArg invokedFromLine -1, bc.Count))
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
        t.Switch(Disposable.empty)
        isDisposed <- true
    member t.DisposeInner() = t.Switch(Disposable.empty)

#if DEBUG
    override _.Finalize() =
        if not isDisposed then
            Log.Error(String.Format("Finalize is called before Dispose, from {0} Line {1}", defaultArg invokedFromFile "", defaultArg invokedFromLine -1))
#endif

    interface IDisposable with member t.Dispose() = t.Dispose()