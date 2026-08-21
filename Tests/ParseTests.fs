module Tests.ParseTests

open System.Numerics
open SimpleTests
open FSUtils
open Tests

let ParseTestList =
    TestList("Parse", [
        Test.Sync("toInt accepts integers and rejects the rest", fun () ->
            Assert.Equal(ValueSome 42, Parse.toInt "42")
            Assert.Equal(ValueSome -42, Parse.toInt "-42")
            Assert.Equal(ValueNone, Parse.toInt "42.0")
            Assert.Equal(ValueNone, Parse.toInt "")
            Assert.Equal(ValueNone, Parse.toInt "2147483648"))
        Test.Sync("toFloat is invariant-culture", fun () ->
            Assert.Equal(ValueSome 1.5, Parse.toFloat "1.5")
            Assert.Equal(ValueSome -0.5, Parse.toFloat "-0.5")
            Assert.Equal(ValueSome 1500.0, Parse.toFloat "1.5e3")
            Assert.Equal(ValueNone, Parse.toFloat "1,5"))
        Test.Sync("toGuid round-trips a guid", fun () ->
            Assert.Equal(ValueSome(System.Guid "12345678-1234-1234-1234-1234567890AB"), Parse.toGuid "12345678-1234-1234-1234-1234567890AB")
            Assert.Equal(ValueNone, Parse.toGuid "not-a-guid"))
        Test.Sync("toAbsoluteUri takes absolute URIs only", fun () ->
            Assert.Equal(ValueSome(System.Uri "https://summatic.co.uk/a?b=1"), Parse.toAbsoluteUri "https://summatic.co.uk/a?b=1")
            Assert.Equal(ValueNone, Parse.toAbsoluteUri "/a?b=1")
            Assert.Equal(ValueNone, Parse.toAbsoluteUri ""))
        Test.Sync("toComplex reads reals and imaginaries", fun () ->
            Assert.Equal(ValueSome(Complex(3.0, 0.0)), Parse.toComplex "3")
            Assert.Equal(ValueSome(Complex(0.0, 4.0)), Parse.toComplex "4i")
            Assert.Equal(ValueSome(Complex(0.0, 1.0)), Parse.toComplex "i")
            Assert.Equal(ValueSome(Complex(0.0, -2.5)), Parse.toComplex "-2.5i"))
        Test.Sync("toComplex reads both parts", fun () ->
            Assert.Equal(ValueSome(Complex(3.0, 4.0)), Parse.toComplex "3+4i")
            Assert.Equal(ValueSome(Complex(3.0, -4.0)), Parse.toComplex "3-4i")
            Assert.Equal(ValueSome(Complex(3.0, 4.0)), Parse.toComplex "4i+3"))
        Test.Sync("toComplex rejects nonsense", fun () ->
            Assert.Equal(ValueNone, Parse.toComplex "")
            Assert.Equal(ValueNone, Parse.toComplex "3+")
            Assert.Equal(ValueNone, Parse.toComplex "abc"))
    ])
