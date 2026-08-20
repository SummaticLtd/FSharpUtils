namespace FSUtils

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Linq

module Seq =
    /// An equality comparer that works on the base object
    type private ReferenceComparer<'T when 'T : not struct>() =
        interface IEqualityComparer<'T> with
            member _.Equals(a, b) = Object.ReferenceEquals(a, b)
            member _.GetHashCode(a) = LanguagePrimitives.PhysicalHash a

    /// Returns distinct items ignoring the object's
    /// overridden GetHashCode and Equals implementation.
    /// This is equivalent to creating a set by true reference equality.
    let distinctPhysical(items:IEnumerable<'a>) =
        let referenceComparer = ReferenceComparer<_>()
        Enumerable.Distinct(items, referenceComparer)

    let inline countWhere<'a> ([<InlineIfLambda>] f:'a->bool) (s:seq<'a>) =
        let mutable n = 0
        s |> Seq.iter (fun item -> if f item then n <- n+1)
        n
    /// Returns the largest output f(x) for x in arr, or minimum, whichever is higher
    let inline maxWithSafe<'a,'b when 'b:comparison> (arr: 'a seq, minimum: 'b, [<InlineIfLambda>] f:'a -> 'b) =
        let mutable m = minimum
        for item in arr do
            let fItem = f item
            if fItem > m then m <- fItem
        m

module Array =
    /// Returns distinct items ignoring the object's
    /// overridden GetHashCode and Equals implementation.
    /// This is equivalent to creating a set by true reference equality.
    let distinctPhysical<'a when 'a: not struct>(items: IEnumerable<'a>) =
        Seq.distinctPhysical items |> Array.ofSeq

module ImmArray =
    let inline mapFromIROC<'a, 'b> ([<InlineIfLambda>] f:'a -> 'b) (l:IReadOnlyCollection<'a>) =
        let builder = ImmutableArray.CreateBuilder<'b>(l.Count)
        for x in l do
            builder.Add(f x)
        builder.MoveToImmutable()
    let inline map<'a, 'b> ([<InlineIfLambda>] f:'a -> 'b) (arr:ImmutableArray<'a>) =
        let builder = ImmutableArray.CreateBuilder<'b>(arr.Length)
        for i = 0 to arr.Length - 1 do
            builder.Add(f(arr.[i]))
        builder.MoveToImmutable()
    let inline mapi<'a, 'b> ([<InlineIfLambda>] f: int -> 'a -> 'b) (arr:ImmutableArray<'a>) =
        let builder = ImmutableArray.CreateBuilder<'b>(arr.Length)
        for i = 0 to arr.Length - 1 do
            builder.Add(f i (arr.[i]))
        builder.MoveToImmutable()
    let inline chooseV<'a, 'b> ([<InlineIfLambda>] f:'a -> 'b voption) (arr: ImmutableArray<'a>) =
        let builder = ImmutableArray.CreateBuilder<'b>(arr.Length)
        for i = 0 to arr.Length - 1 do
            f(arr.[i]) |> ValueOption.iter builder.Add
        builder.ToImmutable()
    let inline concat<'a>(s:IReadOnlyCollection<ImmutableArray<'a>>) =
        let count = s |> Seq.sumBy (fun ia -> ia.Length)
        let builder = ImmutableArray.CreateBuilder<'a>(count)
        for ia in s do
            builder.AddRange(ia)
        builder.MoveToImmutable()
    let inline init<'a> (count:int) ([<InlineIfLambda>] f:int -> 'a) =
        let b = ImmutableArray.CreateBuilder<'a>(count)
        for i = 0 to count-1 do
            b.Add(f i)
        b.MoveToImmutable()
    let inline create<'a> (n:int) (v:'a) =
        init n (fun _ -> v)
    let inline iter<'a> ([<InlineIfLambda>] f:'a -> unit) (arr:ImmutableArray<'a>) =
        for i = 0 to arr.Length - 1 do
            f (arr.[i])
    let inline iter2<'a, 'b> ([<InlineIfLambda>] f:('a * 'b) -> unit) (arr1:ImmutableArray<'a>, arr2:ImmutableArray<'b>) =
        for i = 0 to arr1.Length - 1 do
            f (arr1.[i], arr2.[i])
    let inline iteri<'a> ([<InlineIfLambda>] f:int -> 'a -> unit) (arr:ImmutableArray<'a>) =
        for i = 0 to arr.Length - 1 do
            f i (arr.[i])
    let indexed<'a>(arr:ImmutableArray<'a>) =
        let b = ImmutableArray.CreateBuilder<struct(int * 'a)>(arr.Length)
        arr |> iteri (fun i x -> b.Add(i, x))
        b.MoveToImmutable()
    let singleton<'a>(x:'a) = ImmutableArray.Create(x)
    let empty<'a> = ImmutableArray.Create<'a>()
    let inline exists<'a> ([<InlineIfLambda>] f:'a -> bool) (ia:ImmutableArray<'a>) =
        let mutable exists = false
        for i = 0 to ia.Length - 1 do
            if not exists then
                exists <- f(ia.[i])
        exists
    let contains<'a when 'a: equality> (x: 'a) (ia:ImmutableArray<'a>) =
        let mutable exists = false
        for i = 0 to ia.Length - 1 do
            if not exists then
                exists <- ia.[i] = x
        exists
    let inline forall<'a> ([<InlineIfLambda>] f:'a -> bool) (ia:ImmutableArray<'a>) =
        let mutable sofar = true
        for i = 0 to ia.Length - 1 do
            if sofar then
                sofar <- f(ia.[i])
        sofar
    let pairwise<'a>(coll:ImmutableArray<'a>) =
        if coll.Length < 2 then ImmutableArray.Create<struct('a*'a)>()
        else
            init (coll.Length - 1) (fun i -> coll.[i], coll.[i+1])
    let inline map2<'a, 'b, 'c> ([<InlineIfLambda>] f:'a -> 'b -> 'c) (ia1: ImmutableArray<'a>) (ia2: ImmutableArray<'b>) =
        if ia1.Length <> ia2.Length then failwith "the immutable arrays are of different lengths"
        else init ia1.Length (fun i -> f (ia1.[i]) (ia2.[i]))
    let inline map3<'a, 'b, 'c, 'd> ([<InlineIfLambda>] f:'a -> 'b -> 'c -> 'd) (ia1: ImmutableArray<'a>) (ia2: ImmutableArray<'b>) (ia3: ImmutableArray<'c>) =
        if ia1.Length <> ia2.Length || ia1.Length <> ia3.Length then failwith "the immutable arrays are of different lengths"
        else init ia1.Length (fun i -> f (ia1.[i]) (ia2.[i]) (ia3.[i]))
    let inline mapi2<'a, 'b, 'c> ([<InlineIfLambda>] f: int -> 'a -> 'b -> 'c) (ia1: ImmutableArray<'a>) (ia2: ImmutableArray<'b>) =
        if ia1.Length <> ia2.Length then failwith "the immutable arrays are of different lengths"
        else init ia1.Length (fun i -> f i (ia1.[i]) (ia2.[i]))
    let inline filter<'a> ([<InlineIfLambda>] f:'a->bool) (ia:ImmutableArray<'a>) =
        let b = ImmutableArray.CreateBuilder<'a>(ia.Length)
        for x in ia do
            if f x then b.Add x
        b.ToImmutable()
    let sortBy<'a, 'Key when 'Key:>IComparable<'Key>> (p:'a->'Key) (ia:ImmutableArray<'a>) =
        let comparer = Comparison<'a>(fun x y -> (p x:>IComparable<'Key>).CompareTo(p y))
        ia.Sort(comparer)
    let sortByDescending<'a, 'Key when 'Key:>IComparable<'Key>> (p:'a->'Key) (ia:ImmutableArray<'a>) =
        let comparer = Comparison<'a>(fun x y -> (p y:>IComparable<'Key>).CompareTo(p x))
        ia.Sort(comparer)
    /// Returns ValueSome(the unique element of an immutableArray) or ValueNone if empty; throws if there is more than one element.
    /// ValueNone if empty and the element if there is exactly one; throws if there is more than one.
    let toVoption<'a> (ia:ImmutableArray<'a>) =
        if ia.Length = 0 then ValueNone
        elif ia.Length = 1 then ValueSome(ia.[0])
        else failwith "ImmutableArray has more than one element"
    /// Returns the unique element of an immutableArray; throws if there is a different number of elements.
    let toSingle<'a> (ia:ImmutableArray<'a>) =
        if ia.Length = 1 then ia.[0]
        else failwith("ImmutableArray expected to have one element but has: " + ia.Length.ToString())
    let tryLastV<'a> (ia:ImmutableArray<'a>) =
        if ia.Length = 0 then ValueNone
        else ValueSome(ia.[ia.Length - 1])
    let tryFirstV<'a> (ia:ImmutableArray<'a>) =
        if ia.Length = 0 then ValueNone
        else ValueSome(ia.[0])
    let toArray<'a> (ia:ImmutableArray<'a>) =
        Array.init ia.Length (fun i -> ia.[i])
    /// Returns an array for which the function evaluates to true, and an array for which the function evaluates to false
    let inline partition<'a> ([<InlineIfLambda>] f:'a -> bool) (ia:ImmutableArray<'a>) =
        let bTrue = ImmutableArray.CreateBuilder<'a>(ia.Length)
        let bFalse = ImmutableArray.CreateBuilder<'a>(ia.Length)
        for x in ia do
            if f x then bTrue.Add x else bFalse.Add x
        bTrue.ToImmutable(), bFalse.ToImmutable()
    let inline groupBy<'a,'Key when 'Key:equality> ([<InlineIfLambda>] f:'a -> 'Key) (ia:ImmutableArray<'a>) =
        Array.groupBy f (ia |> toArray) |> Array.map (fun (k, x) -> (k, x.ToImmutableArray()))
        |> ImmutableArray.ToImmutableArray
    /// <summary>Applies a key-generating function to each element of an immarray and returns an immarray yielding unique
    /// keys and their number of occurrences in the original array.</summary>
    /// <remarks>This is an O(n) operation, where n is the length of the immarray.</remarks>
    let inline countBy<'a, 'Key when 'Key: struct and 'Key: equality and 'Key: not null> ([<InlineIfLambda>] projection: 'a -> 'Key) (ia: ImmutableArray<'a>) =
        let dict = Dictionary<'Key, int>(HashIdentity.Structural)
        for x in ia do
            let key = projection x
            let mutable prev = 0
            if dict.TryGetValue(key, &prev) then
                dict.[key] <- prev + 1
            else
                dict.[key] <- 1
        let b = ImmutableArray.CreateBuilder<'Key * int>(dict.Count)
        for group in dict do
            b.Add(group.Key, group.Value)
        b.MoveToImmutable()
    let rev<'a> (ia:ImmutableArray<'a>) =
        let b = ImmutableArray.CreateBuilder<'a>(ia.Length)
        for i = ia.Length - 1 downto 0 do
            b.Add(ia.[i])
        b.MoveToImmutable()
    let inline takeWhile<'a> ([<InlineIfLambda>] f:'a -> bool) (ia:ImmutableArray<'a>) =
        let mutable count = 0
        while count < ia.Length && f ia.[count] do
            count <- count + 1
        ia.Range(0, count - 1)
    let truncate<'a> (count:int) (ia:ImmutableArray<'a>) =
        let count' = min count ia.Length
        ia.Range(0, count' - 1)
    let distinct<'a when 'a:equality> (ia:ImmutableArray<'a>) =
        (ia |> toArray |> Array.distinct).ToImmutableArray()
    /// Returns distinct items, ignoring the object's overridden GetHashCode and Equals implementation.
    /// This is equivalent to creating a set by true reference equality.
    let distinctPhysical<'a when 'a: not struct> (ia:ImmutableArray<'a>) =
        (Seq.distinctPhysical ia).ToImmutableArray()
    let inline distinctBy<'a,'Key when 'Key:equality> ([<InlineIfLambda>] projection:'a -> 'Key) (ia:ImmutableArray<'a>) =
        (ia |> toArray |> Array.distinctBy projection).ToImmutableArray()
    let tryItem<'a> (index:int) (ia:ImmutableArray<'a>) =
        if 0 <= index && index < ia.Length
        then ValueSome(ia.[index])
        else ValueNone
    let inline tryFindIndex<'T> ([<InlineIfLambda>] f:'T->bool) (ia: ImmutableArray<'T>) =
        let mutable found: int voption = ValueNone
        for ind = 0 to ia.Length - 1 do
            if found.IsNone && f(ia.[ind]) then found <- ValueSome ind
        found
    let inline findIndex<'T> ([<InlineIfLambda>] f:'T->bool) (ia: ImmutableArray<'T>) =
        let mutable found: int voption = ValueNone
        for ind = 0 to ia.Length - 1 do
            if found.IsNone && f(ia.[ind]) then found <- ValueSome ind
        found.Value
    let inline tryFindIndexBack<'a> ([<InlineIfLambda>] f:'a->bool) (ia:ImmutableArray<'a>) =
        let mutable found:int voption = ValueNone
        for ind in ia.Length - 1 .. -1 .. 0 do
            if found.IsNone && f(ia.[ind]) then found <- ValueSome ind
        found
    let inline tryFind<'a> ([<InlineIfLambda>] f:'a->bool) (ia:ImmutableArray<'a>) =
        let mutable found:'a voption = ValueNone
        for ind = 0 to ia.Length - 1 do
            if found.IsNone && f(ia.[ind]) then found <- ValueSome ia.[ind]
        found
    let inline find<'a> ([<InlineIfLambda>] f:'a->bool) (ia:ImmutableArray<'a>) = tryFind f ia |> ValueOption.get
    let inline tryFindBack<'a> ([<InlineIfLambda>] f:'a->bool) (ia:ImmutableArray<'a>) =
        let mutable found:'a voption = ValueNone
        for ind in ia.Length - 1 .. -1 .. 0 do
            if found.IsNone && f(ia.[ind]) then found <- ValueSome ia.[ind]
        found
    let inline tryPick<'a, 'b> ([<InlineIfLambda>] f:'a->'b voption) (ia:ImmutableArray<'a>) =
        let mutable found:'b voption = ValueNone
        for ind = 0 to ia.Length - 1 do
            if found.IsNone then
                found <- f(ia.[ind])
        found
    let inline collect<'T, 'U> ([<InlineIfLambda>] mapping: 'T -> ImmutableArray<'U>) (l:IReadOnlyList<'T>) =
        let b = ImmutableArray.CreateBuilder<'U>()
        for ind = 0 to l.Count - 1 do
            b.AddRange(mapping(l.[ind]))
        b.ToImmutable()
    // https://github.com/dotnet/fsharp/blob/cb106cf3182ff218f0a0e42780815dba94b60013/src/Compiler/Utilities/ImmutableArray.fs
    let fold<'a, 'b> (folder: 'a -> 'b -> 'a) (state: 'a) (arr: ImmutableArray<'b>) =
        let f = OptimizedClosures.FSharpFunc<'a, 'b, 'a>.Adapt (folder)
        let mutable state = state
        for i = 0 to arr.Length - 1 do
            state <- f.Invoke(state, arr.[i])
        state
    // https://github.com/dotnet/fsharp/blob/605486e79ca5e6c1dd4c3194c03809b906d7ccfe/src/FSharp.Core/local.fs#L1045
    let mapFold<'a, 'b, 'c> (folder: 'a -> 'b -> 'c * 'a) (state: 'a) (arr: ImmutableArray<'b>): ImmutableArray<'c> * 'a =
        let f = OptimizedClosures.FSharpFunc<_, _, _>.Adapt (folder)
        let mutable state = state
        let b = ImmutableArray.CreateBuilder<'c>(arr.Length)
        for i = 0 to arr.Length - 1 do
            let res, newState = f.Invoke(state, arr.[i])
            state <- newState
            b.Add res
        b.MoveToImmutable(), state
    /// Returns the largest output f(x) for x in arr, or minimum, whichever is higher
    let inline maxWithSafe<'a,'b when 'b:comparison> (arr: ImmutableArray<'a>, minimum: 'b, [<InlineIfLambda>] f:'a -> 'b) =
        let mutable m = minimum
        for item in arr do
            let fItem = f item
            if fItem > m then m <- fItem
        m
    /// Returns the smallest output f(x) for x in arr, or maximum, whichever is lower
    let inline minWithSafe<'a,'b when 'b:comparison> (arr: ImmutableArray<'a>, maximum: 'b, [<InlineIfLambda>] f:'a -> 'b) =
        let mutable m = maximum
        for item in arr do
            let fItem = f item
            if fItem < m then m <- fItem
        m
    let compare<'a when 'a:>IComparable<'a>>(ia1: ImmutableArray<'a>, ia2:ImmutableArray<'a>) =
        if ia1.Length <> ia2.Length then ia1.Length.CompareTo(ia2.Length)
        else
            let mutable compare = 0
            for i = 0 to min ia1.Length ia2.Length - 1 do
                if compare = 0 then
                    compare <- (ia1.[i] :> IComparable<'a>).CompareTo(ia2.[i])
            compare
    let prepend<'a>(h: 'a, t: ImmutableArray<'a>) =
        let b = ImmutableArray.CreateBuilder<'a>(t.Length + 1)
        b.Add h
        b.AddRange t
        b.MoveToImmutable()
    /// Takes the first count elements of the array, or the whole array if it is shorter
    /// The first count elements, or all of them if there are fewer. Unlike Array.take, this does not throw.
    let take (count:int) (ia:ImmutableArray<'a>) =
        ia.Slice(0, min count ia.Length)

    let inline sumBy<'a, 'b when 'b : (static member (+) : 'b * 'b -> 'b)>(ia: ImmutableArray<'a>, zero: 'b, [<InlineIfLambda>] f: 'a -> 'b) =
        let mutable acc = zero
        for i = 0 to ia.Length - 1 do
            acc <- acc + f(ia.[i])
        acc
    let inline sum<'b when 'b : (static member (+) : 'b * 'b -> 'b)>(ia: ImmutableArray<'b>, zero: 'b) =
        let mutable acc = zero
        for i = 0 to ia.Length - 1 do
            acc <- acc + ia.[i]
        acc
    let inline productBy<'a, 'b when 'b : (static member (*) : 'b * 'b -> 'b)>(ia: ImmutableArray<'a>, one: 'b, [<InlineIfLambda>] f: 'a -> 'b) =
        let mutable acc = one
        for i = 0 to ia.Length - 1 do
            acc <- acc * f(ia.[i])
        acc
    let inline countWhere<'a> ([<InlineIfLambda>] f:'a->bool) (s:ImmutableArray<'a>) =
        let mutable n = 0
        s |> iter (fun item -> if f item then n <- n+1)
        n
    /// Returns the initial set, minus any duplicated elements, decided by the comparer
    let uniqueBy(comparer:'a*'a -> bool) (l:ImmutableArray<'a>) =
        let unique = ImmutableArray.CreateBuilder<'a>()
        for f in l do
            if unique |> Seq.exists(fun g -> comparer(f, g)) |> not
            then unique.Add f
        unique.ToImmutable()
    let zip<'a, 'b>(ia1: ImmutableArray<'a>, ia2: ImmutableArray<'b>) =
        if ia1.Length <> ia2.Length then failwith "the immutable arrays are of different lengths"
        else
            init ia1.Length (fun i -> (ia1.[i], ia2.[i]))
