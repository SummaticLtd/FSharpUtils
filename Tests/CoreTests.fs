module Tests.CoreTests

open System
open System.Collections.Generic
open System.Threading
open SimpleTests
open FSUtils
open Tests

let CoreTestList =
    TestList("Core", [
        Test.Sync("Dictionary.tryFind reports presence", fun () ->
            let d = Dictionary<string, int>()
            d.Add("a", 1)
            Assert.Equal(ValueSome 1, d.tryFind "a")
            Assert.Equal(ValueNone, d.tryFind "b"))
        Test.Sync("AddOrReplace adds then replaces", fun () ->
            let d = Dictionary<string, int>()
            d.AddOrReplace("a", 1)
            d.AddOrReplace("a", 2)
            Assert.Equal(ValueSome 2, d.tryFind "a")
            Assert.Equal(1, d.Count))
        Test.Sync("Guid.FromInt is predefined, a new guid is not", fun () ->
            Assert.True(Guid.FromInt(7).IsPredefined, "built from an int")
            Assert.False(Guid.NewGuid().IsPredefined, "randomly generated"))
        Test.Sync("Guid.FromInt is injective on the int", fun () ->
            Assert.Equal(Guid.FromInt 7, Guid.FromInt 7)
            Assert.False((Guid.FromInt 7 = Guid.FromInt 8), "different ints, different guids"))
        Test.Sync("withLock runs the body and returns its value", fun () ->
            let l = Lock()
            Assert.Equal(42, withLock(l, fun () -> 42)))
        Test.Sync("withLock releases the lock when the body throws", fun () ->
            let l = Lock()
            Assert.Throws((fun () -> withLock(l, fun () -> failwith "boom")), "the body throws")
            Assert.Equal(1, withLock(l, fun () -> 1)))
        Test.Sync("NonGenericWorkaround compares matching types", fun () ->
            Assert.True(NonGenericWorkaround.equals<string>("a", box "a"), "equal strings")
            Assert.False(NonGenericWorkaround.equals<string>("a", box "b"), "different strings")
            Assert.True(NonGenericWorkaround.compareTo<string>("a", box "b") < 0, "a sorts before b"))
        Test.Sync("NonGenericWorkaround throws on a type mismatch rather than returning false", fun () ->
            Assert.Throws((fun () -> NonGenericWorkaround.equals<string>("a", box 1) |> ignore), "string vs int")
            Assert.Throws((fun () -> NonGenericWorkaround.compareTo<string>("a", box 1) |> ignore), "string vs int"))
        Test.Sync("ImmutableDictionary.tryFind matches the Dictionary one", fun () ->
            let d = Collections.Immutable.ImmutableDictionary.CreateRange([ KeyValuePair("a", 1) ])
            Assert.Equal(ValueSome 1, d |> ImmutableDictionary.tryFind "a")
            Assert.Equal(ValueNone, d |> ImmutableDictionary.tryFind "b"))
    ])
