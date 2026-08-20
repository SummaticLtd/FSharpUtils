module Tests.JsonTests

open System.Collections.Immutable
open SimpleTests
open FSUtils
open Tests

let private parse(s: string) = Json.tryParse s |> Result.OkValue

let JsonTestList =
    TestList("Json", [
        Test.Sync("tryParse reports invalid JSON rather than throwing", fun () ->
            Assert.True(Json.tryParse "{}" |> Result.isOk, "valid object")
            Assert.True(Json.tryParse "{" |> Result.isError, "truncated object"))
        Test.Sync("the parsed element outlives the document", fun () ->
            let root = parse """{"a":1}"""
            System.GC.Collect()
            Assert.Equal(Ok 1, Json.tryGetPropInt root "a"))
        Test.Sync("property accessors check the type", fun () ->
            let root = parse """{"s":"x","n":1,"b":true}"""
            Assert.Equal(Ok "x", Json.tryGetPropStr root "s")
            Assert.Equal(Ok 1, Json.tryGetPropInt root "n")
            Assert.Equal(Ok true, Json.tryGetPropBool root "b")
            Assert.True(Json.tryGetPropInt root "s" |> Result.isError, "string is not an int"))
        Test.Sync("a missing property is an error", fun () ->
            Assert.True(Json.tryGetProp (parse "{}") "nope" |> Result.isError, "absent property"))
        Test.Sync("reading a property of a non-object is an error, not an exception", fun () ->
            Assert.True(Json.tryGetProp (parse "[1]") "nope" |> Result.isError, "array is not an object")
            Assert.True(Json.tryGetProp (parse "3") "nope" |> Result.isError, "number is not an object"))
        Test.Sync("tryGetStringArray requires every element to be a string", fun () ->
            Assert.Equal(Ok(ImmutableArray.Create("a", "b")), Json.tryGetStringArray (parse """["a","b"]"""))
            Assert.True(Json.tryGetStringArray (parse """["a",1]""") |> Result.isError, "mixed array"))
        Test.Sync("JsonNode builders produce the expected document", fun () ->
            let node = JsonNode.jobj [ "n", JsonNode.int32 1; "xs", JsonNode.arr [ JsonNode.str "a" ] ]
            Assert.Equal("""{"n":1,"xs":["a"]}""", node.ToJsonString()))
    ])
