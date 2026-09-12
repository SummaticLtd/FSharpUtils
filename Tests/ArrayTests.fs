module Tests.ArrayTests

open SimpleTests
open FSUtils

let ArrayTestList =
    TestList("Array", [
        Test.Sync("distinctPhysical is by reference and returns an array", fun () ->
            let a, b = ref 1, ref 1
            Assert.Equal(2, (Array.distinctPhysical [ a; b ]).Length)
            Assert.Equal(1, (Array.distinctPhysical [ a; a ]).Length))
        Test.Sync("tryFindIndexV returns the first match", fun () ->
            Assert.Equal(ValueSome 1, [| 1; 4; 6 |] |> Array.tryFindIndexV(fun x -> x % 2 = 0))
            Assert.Equal(ValueNone, [| 1; 3 |] |> Array.tryFindIndexV(fun x -> x % 2 = 0))
            Assert.Equal(ValueNone, [||] |> Array.tryFindIndexV(fun (_: int) -> true)))
    ])
