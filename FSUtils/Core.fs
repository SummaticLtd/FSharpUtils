namespace FSUtils

open System
open System.Threading
open System.Threading.Tasks
open System.Collections.Generic
open System.Collections.Immutable

module NonGenericWorkaround =
    // Work around F# failure to use generic equality and comparison https://github.com/dotnet/fsharp/issues/9398 by type-testing and throwing error on type mismatches.
    // TODO: remove when the issues is addressed.
    /// obj types compared and are incompatible. Need for this results from non-generic equality or comparison used by F# https://github.com/dotnet/fsharp/issues/9398
    let inline private failIncompatibleTypes<'a>(): 'a =
        raise <| ArgumentException("different types equated or compared")
    [<RequiresExplicitTypeArguments>]
    let inline equals<'a when 'a :> IEquatable<'a> and 'a: not null>(x: 'a, yObj: obj | null) =
        match yObj with
        | :? 'a as y -> (x :> IEquatable<'a>).Equals(y)
        | _ -> failIncompatibleTypes()
    [<RequiresExplicitTypeArguments>]
    let inline compareTo<'a when 'a :> IComparable<'a> and 'a: not null>(x: 'a, yObj: obj | null) =
        match yObj with
        | :? 'a as y -> (x :> IComparable<'a>).CompareTo(y)
        | _ -> failIncompatibleTypes()
[<AutoOpen>]
module Locking =
    /// Runs f while holding the lock, using Lock.EnterScope so the Lock type's fast path is engaged.
    /// F#'s built-in `lock` takes a Monitor lock and does not special-case Lock (dotnet/fsharp#17287).
    let inline withLock<'T>(lock: Lock, f: unit -> 'T) : 'T =
        let mutable scope = lock.EnterScope()
        try f()
        finally scope.Dispose()

[<AutoOpen>]
module Extensions =

    type ImmutableArray<'a> with
        member t.Range(startInd:int, endInd:int) = t.Slice(startInd, max(endInd - startInd + 1) 0)

    type Async with
        static member AwaitTask (t : Task<'T>, timeoutMilliseconds: int) =
            async {
                use cts = new CancellationTokenSource()
                use timer = Task.Delay(timeoutMilliseconds, cts.Token)
                let! completed = Async.AwaitTask <| Task.WhenAny(t, timer)
                if completed = (t :> Task) then
                    cts.Cancel ()
                    let! result = Async.AwaitTask t
                    return Some result
                else return None
            }

    type Control.AsyncBuilder with
        member _.Bind(t:Task<'T>, f) = async.Bind(Async.AwaitTask t, f)

    type private Collections.Generic.Dictionary<'a,'b when 'a : not null>  with
        member t.tryFind key =
            let found, v = t.TryGetValue key
            if found then ValueSome v else ValueNone
        member t.AddOrReplace(key, value) =
            let found, _ = t.TryGetValue key
            if found
                then t.[key] <- value
                else t.Add(key, value)

    type System.Guid with
        static member FromInt(i:int) =
            Guid(i, 0s, 0s, 0uy, 0uy, 0uy, 0uy, 0uy, 0uy, 0uy, 0uy)
        member t.IsPredefined =
            t.ToByteArray() |> Array.skip 4 |> Array.forall ((=) 0uy)

    module Result =
        /// Gets v if Ok v, else fails
        let OkValue(r:Result<'a,'b>) =
            match r with Ok v -> v | Error e -> failwith("Error does not have OkValue. Error: " + e.ToString())
        /// Gets v if Error v, else fails
        let ErrorValue(r:Result<'a,'b>) =
            match r with Error v -> v | Ok _ -> failwith "Ok does not have ErrorValue"
        let TryOkValue(r:Result<'a,'b>) =
            match r with Ok v -> ValueSome v | Error _ -> ValueNone
        let ignoreOk(r:Result<'a,'b>) =
            match r with Ok _ -> Ok() | Error e -> Error e
        let isOk(r:Result<'a,'b>) =
            match r with Ok _ -> true | Error _ -> false
        let isError(r:Result<'a,'b>) =
            not (Result.isOk r)

    type Dictionary<'Key, 'Value when 'Key : not null> with
        member t.TryGetValueSafe(k: 'Key) =
            let (b, v) = t.TryGetValue(k)
            if b then ValueSome v else ValueNone
