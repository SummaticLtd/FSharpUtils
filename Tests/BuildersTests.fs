module Tests.BuildersTests

open SimpleTests
open Builders
open Tests

let BuildersTestList =
    TestList("Builders", [
        Test.Sync("maybe binds through Some and short-circuits on None", fun () ->
            Assert.Equal(Some 3, maybe { let! a = Some 1
                                         let! b = Some 2
                                         return a + b })
            Assert.Equal(None, maybe { let! a = Some 1
                                       let! b = None
                                       return a + b }))
        Test.Sync("maybe.Return of null is None", fun () ->
            Assert.Equal(None, maybe { return (null: string | null) }))
        Test.Sync("vmaybe binds through ValueSome and short-circuits on ValueNone", fun () ->
            Assert.Equal(ValueSome 3, vmaybe { let! a = ValueSome 1
                                               let! b = ValueSome 2
                                               return a + b })
            Assert.Equal(ValueNone, vmaybe { let! a = ValueSome 1
                                             let! b = ValueNone
                                             return a + b }))
        Test.Sync("result binds through Ok and short-circuits on the first Error", fun () ->
            Assert.Equal(Ok 3, result { let! a = Ok 1
                                        let! b = Ok 2
                                        return a + b })
            Assert.Equal(Error "bad", result { let! a = Ok 1
                                               let! b = Error "bad"
                                               return a + b }))
    ])
