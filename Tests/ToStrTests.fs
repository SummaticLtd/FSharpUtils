module Tests.ToStrTests

open SimpleTests
open FSUtils

let ToStrTestList =
    TestList("ToStr", [
        Test.Sync("each shape wraps its contents", fun () ->
            Assert.Equal("Some(3)", ToStr.typeSimple("Some", 3))
            Assert.Equal("Pair(3, a)", ToStr.typeProps("Pair", [ 3; "a" ]))
            Assert.Equal("Pair(3, a)", ToStr.typeStrs("Pair", ["3"; "a"]))
            Assert.Equal("Pair(3, 4)", ToStr.typePropsG("Pair", [3; 4]))
            Assert.Equal("[3; 4]", ToStr.seq [3; 4])
            Assert.Equal("Empty()", ToStr.typeStrs("Empty", []))
            Assert.Equal("[]", ToStr.seq ([]: int list)))
    ])
