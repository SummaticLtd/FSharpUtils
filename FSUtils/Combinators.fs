namespace FSUtils

open System
open System.Threading.Tasks
open System.Collections.Immutable

[<RequireQualifiedAccess>]
module Result =
    let partition<'Ok, 'Error>(l:ImmutableArray<Result<'Ok, 'Error>>) =
        l |> ImmArray.chooseV(function Ok x -> ValueSome x | Error _ -> ValueNone),
        l |> ImmArray.chooseV(function Ok _ -> ValueNone | Error e -> ValueSome e)

    let ofArray<'a, 'b when 'b: not null>(results:Result<'a, 'b>[]) =
        match results |> Array.tryFind(fun r -> Result.isError r) with
        | None ->
            results |> Array.map(fun r -> Result.OkValue r) |> Ok
        | Some r ->
            Error(Result.ErrorValue r)
    let ofImmArray<'a, 'b when 'b: not null>(results:ImmutableArray<Result<'a, 'b>>) =
        match results |> ImmArray.tryFind(fun r -> Result.isError r) with
        | ValueNone ->
            results |> ImmArray.map(fun r -> Result.OkValue r) |> Ok
        | ValueSome r ->
            Error(Result.ErrorValue r)
    /// Returns Ok(the Ok values of the mapped list) if all outputs are Ok, else the first Error. Short-circuits if an Error is encountered.
    let ofImmArrayMap<'a, 'Ok, 'Error> (f: 'a -> Result<'Ok, 'Error>) (l:ImmutableArray<'a>) =
        let b = ImmutableArray.CreateBuilder<'Ok>(l.Length)
        let mutable errorEncountered = ValueNone: 'Error voption
        for i = 0 to l.Length - 1 do
            if errorEncountered.IsNone then
                match f l.[i] with
                | Ok x -> b.Add x
                | Error e -> errorEncountered <- ValueSome e
        match errorEncountered with
        | ValueNone -> Ok(b.MoveToImmutable())
        | ValueSome e -> Error e
    let rec ofListMap<'a, 'Ok, 'Error> (f: 'a -> Result<'Ok, 'Error>) (l: 'a list) =
        match l with
        | [] -> Ok []
        | x :: t ->
            match f x with
            | Ok y -> ofListMap f t |> Result.map(fun t' -> y :: t')
            | Error e -> Error e

[<RequireQualifiedAccess>]
module Tuple =
    let map2<'u, 'v> (f:'u->'v) (a:'u, b:'u) = (f a, f b)
    let map3<'u, 'v> (f:'u->'v) (a:'u, b:'u, c:'u) = (f a, f b, f c)
    let map4<'u, 'v> (f:'u->'v) (a:'u, b:'u, c:'u, d:'u) = (f a, f b, f c, f d)
[<RequireQualifiedAccess>]
module Async =
    let map (f:'a->'b) (ax:Async<'a>) =
        async {
            let! x = ax
            return f x
        }
    let bind (f:'a -> Async<'b>) (ax:Async<'a>) =
        async {
            let! x = ax
            return! f x
        }

[<RequireQualifiedAccess>]
module Task =
    let Return<'a>(value: 'a) = value |> Task.FromResult

    let bind<'a, 'b>(f : 'a -> Task<'b>) (x : Task<'a>) =
        task {
            let! x = x
            return! f x
        }

    let map<'a, 'b>(f: 'a -> 'b) (x: Task<'a>) =
        task {
            let! x = x
            return f x
        }
    let Ignore<'a>(x: Task<'a>) =
        task {
            let! _ = x
            return ()
        }
    let sequential<'a>(tasks: ImmutableArray<unit -> Task<'a>>) =
        task {
            let results = Array.create tasks.Length (Unchecked.defaultof<'a>)
            for i = 0 to tasks.Length - 1 do
                let! result = tasks.[i]()
                results.[i] <- result
            return results.ToImmutableArray()
        }
    let sequentialDo(tasks: ImmutableArray<unit -> Task<unit>>) =
        task {
            for task in tasks do
                do! task()
        }

module ValueOption =
    let ofOption(opt:'a option) =
        match opt with
        | Some x -> ValueSome x
        | None -> ValueNone
    /// Returns ValueSome(the values of the mapped list) if all outputs are ValueSome, else ValueNone. Short-circuits if ValueNone is encountered.
    let rec ofListMap<'a, 'b> (f: 'a -> 'b voption) (l: 'a list): 'b list voption =
        match l with
        | [] -> ValueSome []
        | x :: t -> f x |> ValueOption.bind(fun y -> ofListMap f t |> ValueOption.map(fun t' -> y :: t'))
    let ofImmArray<'a>(opts:ImmutableArray<'a voption>) =
        if opts |> ImmArray.forall ValueOption.isSome
        then opts |> ImmArray.map ValueOption.get |> ValueSome
        else ValueNone
    /// Returns ValueSome(the values of the mapped immarray) if all outputs are ValueSome, else ValueNone. Short-circuits if ValueNone is encountered.
    let ofImmArrayMap<'a, 'b> (f: 'a -> 'b voption) (l:ImmutableArray<'a>) =
        let b = ImmutableArray.CreateBuilder<'b>(l.Length)
        let mutable noneEncountered = false
        for i = 0 to l.Length - 1 do
            if not noneEncountered then
                match f l.[i] with
                | ValueSome x -> b.Add x
                | ValueNone -> noneEncountered <- true
        if noneEncountered then ValueNone else ValueSome(b.MoveToImmutable())
    let toImmArray<'a>(opt: 'a voption) =
        match opt with
        | ValueSome x -> ImmArray.singleton x
        | ValueNone -> ImmArray.empty<'a>
