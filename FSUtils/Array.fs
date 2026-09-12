namespace FSUtils

open System.Collections.Generic

module Array =
    /// Returns distinct items ignoring the object's
    /// overridden GetHashCode and Equals implementation.
    /// This is equivalent to creating a set by true reference equality.
    let distinctPhysical<'a when 'a: not struct>(items: IEnumerable<'a>) =
        Seq.distinctPhysical items |> Array.ofSeq
    let inline tryFindIndexV<'T> ([<InlineIfLambda>] f: 'T -> bool) (arr: 'T[]) : int voption =
        let mutable i = 0
        while i < arr.Length && not (f arr.[i]) do
            i <- i + 1
        if i < arr.Length then ValueSome i else ValueNone
