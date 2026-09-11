namespace FSUtils

open System
open System.Numerics
open System.Collections.Immutable

[<RequireQualifiedAccess>]
module Numeric =

    /// Round to the nearest int (to even on .5).
    let inline toInt(x: float) = Convert.ToInt32 x

    /// Clamps x between l and r, where l <= r
    let clampFloat32(x:float32, l:float32, r:float32) = Math.Min(Math.Max(x, l), r)

    /// Clamps x between l and r, where l <= r
    let clampFloat(x:float, l:float, r:float) = Math.Min(Math.Max(x, l), r)

    /// Clamps x between l and r, where l <= r
    let clampInt(x:int, l:int, r:int) = Math.Min(Math.Max(x, l), r)

    /// max of f on x0 .. x1
    let inline maximum<'a when 'a:comparison>(i0: int, i1: int, [<InlineIfLambda>] f: int -> 'a, emptyResult: 'a) =
        let mutable m = emptyResult
        for i = i0 to i1 do
            let v = f i
            if v > m then m <- v
        m

    let inline sum<'a when 'a :> INumber<'a>>(i0: int, i1: int, [<InlineIfLambda>] f: int -> 'a) : 'a =
        let mutable acc = 'a.Zero
        for i = i0 to i1 do
            acc <- 'a.op_CheckedAddition(acc, f i)
        acc

    /// A correct modulus function for m > 0 (n % m sometimes returns a negative number)
    let modulus(n:int, m:int) = ((n % m) + m) % m

    /// Throws for gcd(Int32.MinValue, 0)
    let gcd(a:int, b:int) =
        let mutable x = a
        let mutable y = b
        while y <> 0 do
            let r = x % y
            x <- y
            y <- r
        Math.Abs x

    let isCoprime(x:int, y:int) = gcd(x, y) = 1

    /// ValueNone above 12!, which overflows int
    let rec fact(n:int) =
        if n < 0 then raise <| ArgumentOutOfRangeException(nameof n, "negative factorial input")
        elif n = 0 then ValueSome 1
        elif n >= 13 then ValueNone
        else fact(n-1) |> ValueOption.map (fun f -> n * f)

    let isPrime(i:int) =
        i > 1 &&
        let bound = int (sqrt (float i))
        let mutable d = 2
        let mutable found = false
        while d <= bound && not found do
            if i % d = 0 then found <- true
            d <- d+1
        not found

    let hasFactorOfOrder(order:int, i:int) =
        let i = abs i
        let rounded = toInt (Math.Pow(float i, 1./float order))
        // rounding can land one above the floor, which would overflow pown in the loop
        let bound = if pown (int64 rounded) order > int64 i then rounded - 1 else rounded
        let mutable d = 2
        let mutable found = false
        while d <= bound && not found do
            if i % (pown d order) = 0 then found <- true
            d <- d+1
        found

    /// the rth root of n, for r > 0
    let rec tryIntegerRoot(n:int, r:int): int voption =
        if n < 0 then
            if r % 2 = 0 then ValueNone
            elif n = Int32.MinValue then ValueNone // negating it wraps to itself, so the recursion below would never end
            else tryIntegerRoot(-n, r) |> ValueOption.map (~-)
        else
            let tryRoot = toInt ((float n) ** (1./float r))
            if pown (int64 tryRoot) r = int64 n then ValueSome tryRoot else ValueNone

    /// the first 40 primes
    let smallPrimes =
        ImmutableArray.Create(
            2, 3, 5, 7, 11, 13, 17, 19, 23, 29,
            31, 37, 41, 43, 47, 53, 59, 61, 67, 71,
            73, 79, 83, 89, 97, 101, 103, 107, 109, 113,
            127, 131, 137, 139, 149, 151, 157, 163, 167, 173
        )

    let private romans =
        ImmutableArray.Create(
            ("m", 1000), ("cm", 900), ("d", 500), ("cd", 400),
            ("c", 100), ("xc", 90), ("l", 50), ("xl", 40),
            ("x", 10), ("ix", 9), ("v", 5), ("iv", 4), ("i", 1)
        )

    /// Lowercase Roman numerals, empty below 1
    let rec toRoman(x:int) =
        match romans |> ImmArray.tryFind (fun (_, n) -> x >= n) with
        | ValueSome (init, n) -> init + toRoman(x-n)
        | ValueNone -> ""

    /// Spreadsheet column names: 1 is "a", 27 is "aa", empty below 1
    let rec toAlphabets(x:int) =
        if x <= 0 then ""
        else
            let num = x - 1 // 1 => a, not 0 => a
            let rem = num % 26
            toAlphabets ((num - rem) / 26) + string ('a' + char rem)

    /// "1st", "2nd", "3rd", "11th". Wrong for negatives.
    let ordinalStr(i:int) =
        let suffix =
            let i100 = i % 100
            if 11 <= i100 && i100 <= 13 then "th"
            else
                match i % 10 with
                | 1 -> "st"
                | 2 -> "nd"
                | 3 -> "rd"
                | _ -> "th"
        (string i) + suffix
