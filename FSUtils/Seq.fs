namespace FSUtils

open System
open System.Collections.Generic
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
