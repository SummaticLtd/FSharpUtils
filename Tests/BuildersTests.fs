module Tests.BuildersTests

open SimpleTests
open Builders
open Tests

let BuildersTestList =
    TestList("Builders", [
        Test.Sync("maybe binds through Some", fun () ->
            let sum =
                maybe {
                    let! a = Some 1
                    let! b = Some 2
                    return a + b
                }
            Assert.Equal(Some 3, sum))
        Test.Sync("maybe short-circuits on None", fun () ->
            let sum =
                maybe {
                    let! a = Some 1
                    let! b = None
                    return a + b
                }
            Assert.Equal(None, sum))
        Test.Sync("maybe turns a null return into None", fun () ->
            let s =
                maybe {
                    return (null: string | null)
                }
            Assert.Equal(None, s))
        Test.Sync("vmaybe binds through ValueSome", fun () ->
            let sum =
                vmaybe {
                    let! a = ValueSome 1
                    let! b = ValueSome 2
                    return a + b
                }
            Assert.Equal(ValueSome 3, sum))
        Test.Sync("vmaybe short-circuits on ValueNone", fun () ->
            let sum =
                vmaybe {
                    let! a = ValueSome 1
                    let! b = ValueNone
                    return a + b
                }
            Assert.Equal(ValueNone, sum))
        Test.Sync("result binds through Ok", fun () ->
            let sum =
                result {
                    let! a = Ok 1
                    let! b = Ok 2
                    return a + b
                }
            Assert.Equal(Ok 3, sum))
        Test.Sync("result short-circuits on the first Error", fun () ->
            let sum =
                result {
                    let! a = Ok 1
                    let! b = Error "bad"
                    let! c = Error "worse"
                    return a + b + c
                }
            Assert.Equal(Error "bad", sum))
    ])
