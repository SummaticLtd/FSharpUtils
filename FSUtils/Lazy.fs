namespace FSUtils

// A simple implementation of lazy evaluation, used because F#'s built-in Lazy<T> is not trimmable.

type SimpleLazy<'T>(f: unit -> 'T) =
    let mutable valueOpt: voption<'T> = ValueNone
    member _.Value =
        match valueOpt with
        | ValueSome v -> v
        | ValueNone ->
            let v = f()
            valueOpt <- ValueSome v
            v
    member _.IsValueCreated = valueOpt.IsSome