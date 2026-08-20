namespace FSUtils

open System.Collections.Immutable

[<RequireQualifiedAccess>]
module ImmutableDictionary =
    /// ValueSome the value if the key is present, else ValueNone.
    let tryFind<'a, 'b when 'a: not null> (key: 'a) (d: ImmutableDictionary<'a, 'b>) =
        let found, v = d.TryGetValue key
        if found then ValueSome v else ValueNone
