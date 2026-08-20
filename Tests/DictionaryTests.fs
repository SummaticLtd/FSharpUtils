module Tests.DictionaryTests

open System.Collections.Generic
open System.Collections.Immutable
open SimpleTests
open FSUtils
open Tests

let DictionaryTestList =
    TestList("Dictionary", [
        Test.Sync("tryFind reports presence", fun () ->
            let d = Dictionary<string, int>()
            d.Add("a", 1)
            Assert.Equal(ValueSome 1, d |> Dictionary.tryFind "a")
            Assert.Equal(ValueNone, d |> Dictionary.tryFind "b"))
        Test.Sync("addOrReplace adds, then replaces without growing", fun () ->
            let d = Dictionary<string, int>()
            d |> Dictionary.addOrReplace "a" 1
            d |> Dictionary.addOrReplace "a" 2
            Assert.Equal(ValueSome 2, d |> Dictionary.tryFind "a")
            Assert.Equal(1, d.Count))
        Test.Sync("the immutable and mutable lookups agree", fun () ->
            let d = ImmutableDictionary.CreateRange([ KeyValuePair("a", 1) ])
            Assert.Equal(ValueSome 1, d |> ImmutableDictionary.tryFind "a")
            Assert.Equal(ValueNone, d |> ImmutableDictionary.tryFind "b"))
    ])
