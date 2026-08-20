module Tests.ImmArrayTests

open System.Collections.Immutable
open SimpleTests
open FSUtils
open Tests

let private ia (xs: int list) = xs.ToImmutableArray()

let ImmArrayTestList =
    TestList("ImmArray", [
        Test.Sync("map preserves order and length", fun () ->
            Assert.Equal(ia [ 2; 4; 6 ], ia [ 1; 2; 3 ] |> ImmArray.map ((*) 2)))
        Test.Sync("chooseV drops ValueNone", fun () ->
            Assert.Equal(ia [ 2; 4 ], ia [ 1; 2; 3; 4 ] |> ImmArray.chooseV(fun x -> if x % 2 = 0 then ValueSome x else ValueNone)))
        Test.Sync("partition splits on the predicate, keeping order", fun () ->
            let evens, odds = ia [ 1; 2; 3; 4; 5 ] |> ImmArray.partition(fun x -> x % 2 = 0)
            Assert.Equal(ia [ 2; 4 ], evens)
            Assert.Equal(ia [ 1; 3; 5 ], odds))
        Test.Sync("mapFold threads the state and returns it", fun () ->
            let mapped, total = ia [ 1; 2; 3 ] |> ImmArray.mapFold (fun acc x -> (acc + x, acc + x)) 0
            Assert.Equal(ia [ 1; 3; 6 ], mapped)
            Assert.Equal(6, total))
        Test.Sync("fold accumulates left to right", fun () ->
            Assert.Equal("abc", ia [ 1; 2; 3 ] |> ImmArray.fold (fun acc x -> acc + string (char (96 + x))) ""))
        Test.Sync("groupBy keys the groups", fun () ->
            let groups = ia [ 1; 2; 3; 4 ] |> ImmArray.groupBy(fun x -> x % 2)
            Assert.Equal(2, groups.Length)
            Assert.Equal(ia [ 1; 3 ], groups |> ImmArray.find(fun (k, _) -> k = 1) |> snd))
        Test.Sync("countBy counts occurrences", fun () ->
            let counts = ia [ 1; 1; 2 ] |> ImmArray.countBy id
            Assert.Equal(2, counts.Length)
            Assert.Equal(2, counts |> ImmArray.find(fun (k, _) -> k = 1) |> snd))
        Test.Sync("uniqueBy keeps the first of each equivalence class", fun () ->
            Assert.Equal(ia [ 1; 2 ], ia [ 1; 3; 2; 4 ] |> ImmArray.uniqueBy(fun (x, y) -> x % 2 = y % 2)))
        Test.Sync("zip pairs elementwise", fun () ->
            Assert.Equal(ImmutableArray.Create((1, 'a'), (2, 'b')), ImmArray.zip(ia [ 1; 2 ], ImmutableArray.Create('a', 'b'))))
        Test.Sync("zip rejects length mismatch", fun () ->
            Assert.Throws((fun () -> ImmArray.zip(ia [ 1 ], ia [ 1; 2 ]) |> ignore), "lengths differ"))
        Test.Sync("truncate caps at the length and never throws", fun () ->
            Assert.Equal(ia [ 1 ], ia [ 1; 2 ] |> ImmArray.truncate 1)
            Assert.Equal(ia [ 1; 2 ], ia [ 1; 2 ] |> ImmArray.truncate 5)
            Assert.Equal(ia [], ia [ 1; 2 ] |> ImmArray.truncate 0)
            Assert.Equal(ia [], ia [ 1; 2 ] |> ImmArray.truncate -1))
        Test.Sync("tryFindIndexBack finds the last match", fun () ->
            Assert.Equal(ValueSome 3, ia [ 1; 2; 1; 2 ] |> ImmArray.tryFindIndexBack(fun x -> x = 2))
            Assert.Equal(ValueNone, ia [ 1 ] |> ImmArray.tryFindIndexBack(fun x -> x = 2)))
        Test.Sync("toVoption allows at most one element", fun () ->
            Assert.Equal(ValueSome 1, ia [ 1 ] |> ImmArray.toVoption)
            Assert.Equal(ValueNone, ia [] |> ImmArray.toVoption)
            Assert.Throws((fun () -> ia [ 1; 2 ] |> ImmArray.toVoption |> ignore), "more than one element"))
        Test.Sync("sortBy orders by the key", fun () ->
            Assert.Equal(ia [ 3; 2; 1 ], ia [ 1; 2; 3 ] |> ImmArray.sortByDescending id))
        Test.Sync("prepend puts the head first", fun () ->
            Assert.Equal(ia [ 0; 1; 2 ], ImmArray.prepend(0, ia [ 1; 2 ])))
        Test.Sync("sumBy over an empty array is the zero", fun () ->
            Assert.Equal(0, ImmArray.sumBy(ia [], 0, id))
            Assert.Equal(6, ImmArray.sumBy(ia [ 1; 2; 3 ], 0, id)))
        Test.Sync("maxWithSafe and minWithSafe fall back on an empty array", fun () ->
            Assert.Equal(0, ImmArray.maxWithSafe(ia [], 0, id))
            Assert.Equal(0, ImmArray.minWithSafe(ia [], 0, id))
            Assert.Equal(9, ImmArray.maxWithSafe(ia [ 1; 9; 5 ], 0, id))
            Assert.Equal(1, ImmArray.minWithSafe(ia [ 1; 9; 5 ], 100, id)))
        Test.Sync("maxWithSafe and minWithSafe clamp to the bound given", fun () ->
            Assert.Equal(0, ImmArray.maxWithSafe(ia [ -5; -1 ], 0, id))
            Assert.Equal(0, ImmArray.minWithSafe(ia [ 5; 1 ], 0, id)))
        Test.Sync("Range is an inclusive slice", fun () ->
            Assert.Equal(ia [ 2; 3 ], (ia [ 1; 2; 3; 4 ]).Range(1, 2))
            Assert.Equal(ia [], (ia [ 1; 2 ]).Range(1, 0)))
        Test.Sync("distinctPhysical compares by reference, distinct by value", fun () ->
            let a, b = ref 1, ref 1
            Assert.Equal(2, (ImmutableArray.Create(a, b) |> ImmArray.distinctPhysical).Length)
            Assert.Equal(ia [ 1 ], ia [ 1; 1 ] |> ImmArray.distinct))
    ])
