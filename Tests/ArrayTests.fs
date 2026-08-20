module Tests.ArrayTests

open SimpleTests
open FSUtils
open Tests

let ArrayTestList =
    TestList("Array", [
        Test.Sync("distinctPhysical is by reference and returns an array", fun () ->
            let a, b = ref 1, ref 1
            Assert.Equal(2, (Array.distinctPhysical [ a; b ]).Length)
            Assert.Equal(1, (Array.distinctPhysical [ a; a ]).Length))
    ])
