module Tests.CombinatorTests

open System.Collections.Immutable
open SimpleTests
open FSUtils
open Tests

let private ia (xs: int list) = xs.ToImmutableArray()

let CombinatorTestList =
    TestList("Combinators", [
        Test.Sync("Result.ofImmArrayMap collects the Oks", fun () ->
            Assert.Equal(Ok(ia [ 2; 4 ]), ia [ 1; 2 ] |> Result.ofImmArrayMap(fun x -> Ok(x * 2))))
        Test.Sync("Result.ofImmArrayMap returns the first error and stops", fun () ->
            let seen = ResizeArray()
            let r = ia [ 1; 2; 3 ] |> Result.ofImmArrayMap(fun x -> seen.Add x; if x = 2 then Error "bad" else Ok x)
            Assert.Equal(Error "bad", r)
            Assert.Equal(2, seen.Count))
        Test.Sync("Result.partition splits Oks from Errors", fun () ->
            let oks, errors = Result.partition(ImmutableArray.Create(Ok 1, Error "a", Ok 2))
            Assert.Equal(ia [ 1; 2 ], oks)
            Assert.Equal(ImmutableArray.Create "a", errors))
        Test.Sync("ValueOption.ofImmArrayMap short-circuits on ValueNone", fun () ->
            let seen = ResizeArray()
            let r = ia [ 1; 2; 3 ] |> ValueOption.ofImmArrayMap(fun x -> seen.Add x; if x = 2 then ValueNone else ValueSome x)
            Assert.Equal(ValueNone, r)
            Assert.Equal(2, seen.Count))
        Test.Sync("ValueOption.ofImmArray requires every element", fun () ->
            Assert.Equal(ValueSome(ia [ 1; 2 ]), ValueOption.ofImmArray(ImmutableArray.Create(ValueSome 1, ValueSome 2)))
            Assert.Equal(ValueNone, ValueOption.ofImmArray(ImmutableArray.Create(ValueSome 1, ValueNone))))
        Test.Sync("Tuple.map applies to every slot", fun () ->
            Assert.Equal((2, 4), Tuple.map2 ((*) 2) (1, 2))
            Assert.Equal((2, 4, 6, 8), Tuple.map4 ((*) 2) (1, 2, 3, 4)))
        Test.Sync("Compare orders by length before contents", fun () ->
            Assert.True(Compare.immArray(ia [ 9 ], ia [ 1; 2 ]) < 0, "shorter sorts first")
            Assert.True(Compare.immArray(ia [ 1; 3 ], ia [ 1; 2 ]) > 0, "then by element"))
        Test.Sync("Compare.tuple2 is lexicographic", fun () ->
            Assert.True(Compare.tuple2(struct((1, 2), (1, 3))) < 0, "second slot breaks the tie")
            Assert.Equal(0, Compare.tuple2(struct((1, 2), (1, 2)))))
        Test.Sync("Equals.immArray needs matching lengths and elements", fun () ->
            Assert.True(Equals.immArray(ia [ 1; 2 ], ia [ 1; 2 ]), "equal arrays")
            Assert.False(Equals.immArray(ia [ 1; 2 ], ia [ 1 ]), "different lengths"))
        Test.Sync("Hash.immArray agrees for equal arrays", fun () ->
            Assert.Equal(Hash.immArray(ia [ 1; 2; 3 ]), Hash.immArray(ia [ 1; 2; 3 ])))
        Test.Sync("SimpleLazy evaluates once, on demand", fun () ->
            let mutable calls = 0
            let l = SimpleLazy(fun () -> calls <- calls + 1; 7)
            Assert.False(l.IsValueCreated, "not evaluated before use")
            Assert.Equal(7, l.Value)
            Assert.Equal(7, l.Value)
            Assert.Equal(1, calls)
            Assert.True(l.IsValueCreated, "evaluated after use"))
        Test.Sync("Base64Url round-trips and avoids URL-unsafe characters", fun () ->
            let bytes = [| 251uy; 255uy; 190uy; 1uy; 2uy |]
            let encoded = Base64Url.Encode bytes
            Assert.False(encoded.Contains '+' || encoded.Contains '/', "no + or / in the output")
            Assert.Equal(bytes, Base64Url.Decode encoded))
        Test.Sync("Base64Url decodes every padding length", fun () ->
            for n in 1 .. 6 do
                let bytes = Array.init n byte
                Assert.Equal(bytes, Base64Url.Decode(Base64Url.Encode bytes)))
        Test.Sync("Base64Url decodes unpadded input", fun () ->
            Assert.Equal("Hi", Base64Url.DecodeToString "SGk"))
    ])
