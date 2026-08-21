module Builders

[<Sealed>]
type VMaybeBuilder() =
    member _.Bind(a:'a voption, f:'a->('b voption)) = ValueOption.bind f a
    member _.Return(a:'a) = ValueSome a
    member _.ReturnFrom(x:'a voption) = x
    member _.Zero () = ValueNone

/// Maybe monad, over voption.
let vmaybe = VMaybeBuilder()

[<Sealed>]
type ResultBuilder() =
    member _.Bind(m:Result<'d,'e>, f:'d->Result<'c,'e>) =
        Result.bind f m
    member _.Return(x:'b) = Ok x
    member _.ReturnFrom(m: Result<_, _>) = m
    member _.Zero () = Ok ()

let result = ResultBuilder()