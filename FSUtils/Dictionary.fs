namespace FSUtils

open System.Collections.Generic

[<RequireQualifiedAccess>]
module Dictionary =
    /// ValueSome the value if the key is present, else ValueNone.
    let tryFind<'a, 'b when 'a: not null> (key: 'a) (d: Dictionary<'a, 'b>) =
        let found, v = d.TryGetValue key
        if found then ValueSome v else ValueNone
    let addOrReplace<'a, 'b when 'a: not null>(d: Dictionary<'a, 'b>, key: 'a, value: 'b) =
        let found, _ = d.TryGetValue key
        if found then d.[key] <- value else d.Add(key, value)
