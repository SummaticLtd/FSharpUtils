namespace FSUtils

open System.Collections.Generic

module Array =
    /// Returns distinct items ignoring the object's
    /// overridden GetHashCode and Equals implementation.
    /// This is equivalent to creating a set by true reference equality.
    let distinctPhysical<'a when 'a: not struct>(items: IEnumerable<'a>) =
        Seq.distinctPhysical items |> Array.ofSeq
