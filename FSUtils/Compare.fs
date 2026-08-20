namespace FSUtils

open System
open System.Collections.Generic
open System.Collections.Immutable

[<RequireQualifiedAccess>]
module Compare =
    let roc<'a when 'a:> IComparable<'a>>(xs1:IReadOnlyCollection<'a>, xs2:IReadOnlyCollection<'a>) =
        let c1 = xs1.Count.CompareTo(xs2.Count)
        if c1 = 0 then
            Seq.fold2(fun acc x y ->
                if acc = 0 then (x :> IComparable<'a>).CompareTo(y) else acc) 0 xs1 xs2
        else c1
    let rocBy<'a> (f: struct('a * 'a) -> int) (xs1:IReadOnlyCollection<'a>, xs2:IReadOnlyCollection<'a>) =
        let c1 = xs1.Count.CompareTo(xs2.Count)
        if c1 = 0 then
            Seq.fold2(fun acc x y ->
                if acc = 0 then f(x, y) else acc) 0 xs1 xs2
        else c1
    /// Compare two IComparable<'a> values
    let inline ic<'a when 'a:> IComparable<'a>>(x: 'a, y: 'a) =
        (x:>IComparable<'a>).CompareTo(y)
    let tuple2<'a, 'b when 'a:> IComparable<'a> and 'b:> IComparable<'b>>(struct((xA: 'a, xB: 'b), (yA: 'a, yB: 'b))) =
        let cA = (xA :> IComparable<'a>).CompareTo(yA)
        if cA <> 0 then cA
        else (xB :> IComparable<'b>).CompareTo(yB)
    let tuple3<'a, 'b, 'c when 'a:> IComparable<'a> and 'b:> IComparable<'b> and 'c:> IComparable<'c>>
            (struct(xA: 'a, xB: 'b, xC: 'c), struct(yA: 'a, yB: 'b, yC: 'c)) =
        let cA = (xA :> IComparable<'a>).CompareTo(yA)
        if cA <> 0 then cA
        else
            let cB = (xB :> IComparable<'b>).CompareTo(yB)
            if cB <> 0 then cB
            else (xC :> IComparable<'c>).CompareTo(yC)
    let tuple4<'a, 'b, 'c, 'd when 'a:> IComparable<'a> and 'b:> IComparable<'b> and 'c:> IComparable<'c> and 'd:> IComparable<'d>>
            (struct(xA: 'a, xB: 'b, xC: 'c, xD: 'd), struct(yA: 'a, yB: 'b, yC: 'c, yD: 'd)) =
        let cA = (xA :> IComparable<'a>).CompareTo(yA)
        if cA <> 0 then cA
        else
            let cB = (xB :> IComparable<'b>).CompareTo(yB)
            if cB <> 0 then cB
            else
                let cC = (xC :> IComparable<'c>).CompareTo(yC)
                if cC <> 0 then cC
                else (xD :> IComparable<'d>).CompareTo(yD)
    let immArray<'a when 'a:> IComparable<'a>>(xs1:ImmutableArray<'a>, xs2:ImmutableArray<'a>) =
        let c1 = xs1.Length.CompareTo(xs2.Length)
        if c1 <> 0 then c1
        else
            let mutable c = 0
            for i = 0 to xs1.Length - 1 do
                if c = 0 then c <- (xs1.[i] :> IComparable<'a>).CompareTo(xs2.[i])
            c
    let immArrayBy<'a>(f: struct('a * 'a) -> int) (xs1:ImmutableArray<'a>, xs2:ImmutableArray<'a>) =
        let c1 = xs1.Length.CompareTo(xs2.Length)
        if c1 <> 0 then c1
        else
            let mutable c = 0
            for i = 0 to xs1.Length - 1 do
                if c = 0 then c <- f(xs1.[i], xs2.[i])
            c
