module Tests.NumericTests

open SimpleTests
open FSUtils

let NumericTestList =
    TestList("Numeric", [
        Test.Sync("toInt rounds to even on .5", fun () ->
            Assert.Equal(2, Numeric.toInt 2.5)
            Assert.Equal(4, Numeric.toInt 3.5)
            Assert.Equal(-2, Numeric.toInt -2.5)
            Assert.Equal(3, Numeric.toInt 2.6))
        Test.Sync("sum throws on overflow", fun () ->
            Assert.Throws(fun () -> Numeric.sum(1, 2, fun _ -> System.Int32.MaxValue) |> ignore))
        Test.Sync("modulus is never negative", fun () ->
            Assert.Equal(2, Numeric.modulus(-7, 3))
            Assert.Equal(1, Numeric.modulus(7, 3))
            Assert.Equal(0, Numeric.modulus(-9, 3)))
        Test.Sync("ordinalStr handles the teens", fun () ->
            Assert.Equal("1st", Numeric.ordinalStr 1)
            Assert.Equal("2nd", Numeric.ordinalStr 2)
            Assert.Equal("3rd", Numeric.ordinalStr 3)
            Assert.Equal("4th", Numeric.ordinalStr 4)
            Assert.Equal("11th", Numeric.ordinalStr 11)
            Assert.Equal("13th", Numeric.ordinalStr 13)
            Assert.Equal("21st", Numeric.ordinalStr 21)
            Assert.Equal("112th", Numeric.ordinalStr 112))
    ])
