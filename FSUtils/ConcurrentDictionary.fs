namespace FSUtils

open System.Collections.Concurrent

[<RequireQualifiedAccess>]
module ConcurrentDictionary =
    /// ValueSome the value if the key is present, else ValueNone.
    let tryFind<'a, 'b when 'a: not null> (key: 'a) (d: ConcurrentDictionary<'a, 'b>) =
        let found, v = d.TryGetValue key
        if found then ValueSome v else ValueNone
    /// Removes the key, returning ValueSome its value if it was present, else ValueNone.
    let tryRemove<'a, 'b when 'a: not null> (key: 'a) (d: ConcurrentDictionary<'a, 'b>) =
        let found, v = d.TryRemove key
        if found then ValueSome v else ValueNone
