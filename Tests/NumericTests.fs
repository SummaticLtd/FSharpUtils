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
        Test.Sync("clamps hold the bounds", fun () ->
            Assert.Equal(1.0, Numeric.clampFloat(0.5, 1.0, 2.0))
            Assert.Equal(2.0, Numeric.clampFloat(2.5, 1.0, 2.0))
            Assert.Equal(1.5, Numeric.clampFloat(1.5, 1.0, 2.0))
            Assert.Equal(1.0f, Numeric.clampFloat32(0.5f, 1.0f, 2.0f))
            Assert.Equal(2, Numeric.clampInt(7, -2, 2))
            Assert.Equal(-2, Numeric.clampInt(-7, -2, 2)))
        Test.Sync("maximum falls back to emptyResult", fun () ->
            Assert.Equal(9, Numeric.maximum(1, 3, (fun i -> i * i), 0))
            Assert.Equal(100, Numeric.maximum(1, 3, (fun i -> i * i), 100))
            Assert.Equal(0, Numeric.maximum(1, 0, (fun i -> i * i), 0)))
        Test.Sync("sum covers an inclusive range", fun () ->
            Assert.Equal(6, Numeric.sum(1, 3, id))
            Assert.Equal(0, Numeric.sum(1, 0, id))
            Assert.Equal(1.5, Numeric.sum(1, 3, fun i -> 0.5 * float i - 0.5)))
        Test.Sync("sum throws on overflow", fun () ->
            Assert.Throws(fun () -> Numeric.sum(1, 2, fun _ -> System.Int32.MaxValue) |> ignore))
        Test.Sync("modulus is never negative", fun () ->
            Assert.Equal(2, Numeric.modulus(-7, 3))
            Assert.Equal(1, Numeric.modulus(7, 3))
            Assert.Equal(0, Numeric.modulus(-9, 3)))
        Test.Sync("gcd and isCoprime", fun () ->
            Assert.Equal(6, Numeric.gcd(12, 18))
            Assert.Equal(6, Numeric.gcd(-12, 18))
            Assert.Equal(5, Numeric.gcd(5, 0))
            Assert.True(Numeric.isCoprime(9, 28))
            Assert.True(not <| Numeric.isCoprime(9, 27)))
        Test.Sync("fact stops where int overflows", fun () ->
            Assert.Equal(ValueSome 1, Numeric.fact 0)
            Assert.Equal(ValueSome 3628800, Numeric.fact 10)
            Assert.Equal(ValueSome 479001600, Numeric.fact 12)
            Assert.Equal(ValueNone, Numeric.fact 13))
        Test.Sync("isPrime", fun () ->
            Assert.True(not <| Numeric.isPrime 1)
            Assert.True(Numeric.isPrime 2)
            Assert.True(Numeric.isPrime 3)
            Assert.True(not <| Numeric.isPrime 4)
            Assert.True(not <| Numeric.isPrime 49)
            Assert.True(Numeric.isPrime 173))
        Test.Sync("smallPrimes are the first 40 primes", fun () ->
            Assert.Equal(40, Numeric.smallPrimes.Length)
            Assert.True(Numeric.smallPrimes |> Seq.forall Numeric.isPrime)
            Assert.Equal(2, Numeric.smallPrimes.[0])
            Assert.Equal(173, Numeric.smallPrimes.[39]))
        Test.Sync("hasFactorOfOrder finds repeated factors", fun () ->
            Assert.True(Numeric.hasFactorOfOrder(2, 18))
            Assert.True(not <| Numeric.hasFactorOfOrder(2, 30))
            Assert.True(Numeric.hasFactorOfOrder(3, -54))
            Assert.True(not <| Numeric.hasFactorOfOrder(3, 18)))
        Test.Sync("tryIntegerRoot only accepts exact roots", fun () ->
            Assert.Equal(ValueSome 3, Numeric.tryIntegerRoot(81, 4))
            Assert.Equal(ValueNone, Numeric.tryIntegerRoot(80, 4))
            Assert.Equal(ValueSome -3, Numeric.tryIntegerRoot(-27, 3))
            Assert.Equal(ValueNone, Numeric.tryIntegerRoot(-16, 4)))
        Test.Sync("toRoman", fun () ->
            Assert.Equal("", Numeric.toRoman 0)
            Assert.Equal("iv", Numeric.toRoman 4)
            Assert.Equal("xlii", Numeric.toRoman 42)
            Assert.Equal("mcmxcix", Numeric.toRoman 1999))
        Test.Sync("toAlphabets counts like spreadsheet columns", fun () ->
            Assert.Equal("", Numeric.toAlphabets 0)
            Assert.Equal("a", Numeric.toAlphabets 1)
            Assert.Equal("z", Numeric.toAlphabets 26)
            Assert.Equal("aa", Numeric.toAlphabets 27)
            Assert.Equal("ab", Numeric.toAlphabets 28))
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
