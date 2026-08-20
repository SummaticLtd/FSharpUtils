module Tests.SeqTests

open SimpleTests
open FSUtils
open Tests

let SeqTestList =
    TestList("Seq", [
        Test.Sync("countWhere counts matches", fun () ->
            Assert.Equal(2, [ 1; 2; 3; 4 ] |> Seq.countWhere(fun x -> x % 2 = 0))
            Assert.Equal(0, [] |> Seq.countWhere(fun x -> x > 0)))
        Test.Sync("maxWithSafe falls back to the minimum on an empty sequence", fun () ->
            Assert.Equal(0, Seq.maxWithSafe(Seq.empty<int>, 0, id))
            Assert.Equal(9, Seq.maxWithSafe([ 1; 9; 5 ], 0, id)))
        Test.Sync("maxWithSafe never returns below the minimum", fun () ->
            Assert.Equal(0, Seq.maxWithSafe([ -5; -1 ], 0, id)))
        Test.Sync("distinctPhysical is by reference, not by value", fun () ->
            let a, b = ref 1, ref 1
            Assert.Equal(2, Seq.distinctPhysical [ a; b ] |> Seq.length)
            Assert.Equal(1, Seq.distinctPhysical [ a; a ] |> Seq.length))
    ])
