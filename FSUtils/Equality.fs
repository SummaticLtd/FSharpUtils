namespace FSUtils

open System
open System.Collections.Immutable

[<RequireQualifiedAccess>]
module Hash =
    // method taken from https://github.com/dotnet/runtime/blob/22068a8f96d6d1c01b26db70fd24a433e1fdd5ef/src/libraries/System.Private.CoreLib/src/System/Array.cs#L755
    let immArray<'a>(ia:ImmutableArray<'a>) =
        let h = HashCode()
        let start = if ia.Length >= 8 then ia.Length - 8 else 0
        for i = start to ia.Length - 1 do
            h.Add(ia.[i])
        h.ToHashCode()
    let immArrayBy<'a, 'b> (f:'a -> int) (ia:ImmutableArray<'a>) =
        let h = HashCode()
        let start = if ia.Length >= 8 then ia.Length - 8 else 0
        for i = start to ia.Length - 1 do
            h.Add(f ia.[i])
        h.ToHashCode()
    // Taken from most common implementation of combineHash in FSharp.Core
    let inline private combineHash(x:int, y:int) =
        (x <<< 1) + y + 631
    let combine2(x:int, y:int) = combineHash(x, y)
    let combine3(a1:int, a2:int, a3:int) =
        let acc1 = combineHash(a1, a2)
        combineHash(acc1, a3)
    let combine4(a1:int, a2:int, a3:int, a4:int) =
        let acc1 = combineHash(a1, a2)
        let acc2 = combineHash(acc1, a3)
        combineHash(acc2, a4)

module Equals =
    /// Equate two IEquatable<'a> values
    let inline ie<'a when 'a:> IEquatable<'a>>(x: 'a, y: 'a) =
        (x:>IEquatable<'a>).Equals(y)
    let immArray<'a when 'a:>IEquatable<'a>>(ia1:ImmutableArray<'a>, ia2:ImmutableArray<'a>) =
        if ia1.Length <> ia2.Length then false
        else
            let mutable equals = true
            for i = 0 to ia1.Length - 1 do
                if equals then
                    equals <- (ia1.[i] :> IEquatable<'a>).Equals(ia2.[i])
            equals
    let immArrayBy<'a>(isEqual:struct('a * 'a) -> bool) (ia1:ImmutableArray<'a>, ia2:ImmutableArray<'a>) =
        if ia1.Length <> ia2.Length then false
        else
            let mutable equals = true
            for i = 0 to ia1.Length - 1 do
                if equals then
                    equals <- isEqual(ia1.[i], ia2.[i])
            equals
    let ia2D<'a when 'a: equality and 'a:>IEquatable<'a>>(a1:ImmA2D<'a>, a2:ImmA2D<'a>) =
        if a1.Rows <> a2.Rows || a1.Cols <> a2.Cols then false
        else
            let mutable equals = true
            for i = 0 to a1.Rows - 1 do
                for j = 0 to a1.Cols - 1 do
                    if equals then
                        equals <- (a1.[i, j] :> IEquatable<'a>).Equals(a2.[i, j])
            equals
    let tuple2<'a, 'b when 'a:> IEquatable<'a> and 'b:> IEquatable<'b>>(struct((xA: 'a, xB: 'b), (yA: 'a, yB: 'b))) =
        (xA :> IEquatable<'a>).Equals(yA) && (xB :> IEquatable<'b>).Equals(yB)
    let tuple3<'a, 'b, 'c when 'a:> IEquatable<'a> and 'b:> IEquatable<'b> and 'c:> IEquatable<'c>>(struct((xA: 'a, xB: 'b, xC: 'c), (yA: 'a, yB: 'b, yC: 'c))) =
        (xA :> IEquatable<'a>).Equals(yA) && (xB :> IEquatable<'b>).Equals(yB) && (xC :> IEquatable<'c>).Equals(yC)
    let tuple4<'a, 'b, 'c, 'd when 'a:> IEquatable<'a> and 'b:> IEquatable<'b> and 'c:> IEquatable<'c> and 'd:> IEquatable<'d>>(struct((xA: 'a, xB: 'b, xC: 'c, xD: 'd), (yA: 'a, yB: 'b, yC: 'c, yD: 'd))) =
        (xA :> IEquatable<'a>).Equals(yA) && (xB :> IEquatable<'b>).Equals(yB) && (xC :> IEquatable<'c>).Equals(yC) && (xD :> IEquatable<'d>).Equals(yD)