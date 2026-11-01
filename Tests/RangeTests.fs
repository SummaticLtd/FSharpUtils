module Tests.RangeTests

open SimpleTests
open FSUtils

let RangeTestList =
    TestList("Range", [
        Test.Sync("exists checks 0 .. n - 1", fun () ->
            Assert.Equal(true, 5 |> Range.exists(fun i -> i = 4))
            Assert.Equal(false, 4 |> Range.exists(fun i -> i = 4))
            Assert.Equal(false, 0 |> Range.exists(fun _ -> true)))
        Test.Sync("forall checks 0 .. n - 1", fun () ->
            Assert.Equal(true, 4 |> Range.forall(fun i -> i < 4))
            Assert.Equal(false, 5 |> Range.forall(fun i -> i < 4))
            Assert.Equal(true, 0 |> Range.forall(fun _ -> false)))
        Test.Sync("exists and forall stop at the first decisive i", fun () ->
            let mutable calls = 0
            Assert.Equal(true, 10 |> Range.exists(fun i -> calls <- calls + 1; i = 2))
            Assert.Equal(3, calls)
            calls <- 0
            Assert.Equal(false, 10 |> Range.forall(fun i -> calls <- calls + 1; i < 2))
            Assert.Equal(3, calls))
    ])
