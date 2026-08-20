module Builders

[<Sealed>]
type MaybeBuilder() =
    member _.Bind(a:'a option, f:'a->('b option)) = Option.bind f a
    member _.Return(a:'a) =
        // condition will not be needed when C# nullables are used
        // unknown whether we are using it now
        if obj.ReferenceEquals(a, null) then None else Some a
    member _.ReturnFrom(x:'a option) = x
    member _.Zero () = None

/// Maybe monad.
let maybe = MaybeBuilder()

[<Sealed>]
type VMaybeBuilder() =
    member _.Bind(a:'a voption, f:'a->('b voption)) = ValueOption.bind f a
    member _.Return(a:'a) = ValueSome a
    member _.ReturnFrom(x:'a voption) = x
    member _.Zero () = ValueNone

/// Maybe monad.
let vmaybe = VMaybeBuilder()

[<Sealed>]
type ResultBuilder() =
    member _.Bind(m:Result<'d,'e>, f:'d->Result<'c,'e>) =
        Result.bind f m
    member _.Return(x:'b) = Ok x
    member _.ReturnFrom(m: Result<_, _>) = m
    member _.Zero () = Ok ()

let result = ResultBuilder()