module Tests.CborTests

open System
open System.Collections.Immutable
open System.Formats.Cbor
open SimpleTests
open FSUtils
open FSUtils.Cbor

let private hex(bytes: byte[]) = Convert.ToHexString bytes
let private ofHex(s: string) = ReadOnlyMemory(Convert.FromHexString s)
let private roundTrip(σ, δ, x) = Cbor.decode(δ, ReadOnlyMemory(Cbor.encode(σ, x)))
let private assertRoundTrip(σ, δ, x) = Assert.Equal(x, roundTrip(σ, δ, x))
let private throwsContent(f: unit -> unit) =
    try f(); false
    with :? CborContentException -> true

let rec private δNested(r: CborReader) : int = δArray(r, fun () -> if isArray r then δNested r + 1 else 0)
let private nested(depth: int) = ofHex(String.replicate (depth - 1) "81" + "80")

let CborTestList =
    TestList("Cbor", [
        Test.Sync("primitives round-trip", fun () ->
            assertRoundTrip(σString, δString, "héllo")
            assertRoundTrip(σChar, δChar, 'é')
            assertRoundTrip(σInt, δInt, -7)
            assertRoundTrip(σByte, δByte, 255uy)
            assertRoundTrip(σInt16, δInt16, -300s)
            assertRoundTrip(σInt64, δInt64, Int64.MinValue)
            assertRoundTrip(σUInt, δUInt, UInt32.MaxValue)
            assertRoundTrip(σFloat, δFloat, 1.5)
            assertRoundTrip(σFloat32, δFloat32, 1.5f)
            assertRoundTrip(σBool, δBool, true)
            assertRoundTrip(σEnum, δEnum, DayOfWeek.Friday)
            assertRoundTrip(σTimeSpan, δTimeSpan, TimeSpan.FromMinutes 3.0)
            assertRoundTrip(σComplex, δComplex, Numerics.Complex(1.0, -2.0))
            let i = Instant.Now
            Assert.Equal(i.DT, roundTrip(σInstant, δInstant, i).DT))
        Test.Sync("the format is fixed", fun () ->
            Assert.Equal("6161", hex(Cbor.encode(σString, "a")))
            Assert.Equal("F6", hex(Cbor.encode((fun (w, x) -> σOpt(w, x, σInt)), None)))
            Assert.Equal("82F403", hex(Cbor.encode((fun (w, x) -> σChoice(w, x, σString, σInt)), Choice2Of2 3)))
            Assert.Equal("8401020506", hex(Cbor.encode((fun (w, x) -> σImmA2D(w, x, σInt)), ImmA2D(1, 2, ImmutableArray.Create(5, 6)))))
            Assert.Equal("D8255000112233445566778899AABBCCDDEEFF", hex(Cbor.encode(σGuid, Guid "00112233-4455-6677-8899-aabbccddeeff"))))
        Test.Sync("a whole-number float reads as a float", fun () ->
            Assert.Equal(4.0, Cbor.decode(δFloat, ofHex "04"))
            Assert.Equal(-1.0f, Cbor.decode(δFloat32, ofHex "20")))
        Test.Sync("δChar and δByte reject out-of-range values", fun () ->
            Assert.Throws((fun () -> Cbor.decode(δChar, ofHex "20") |> ignore), "-1 is not a char")
            Assert.Throws((fun () -> Cbor.decode(δByte, ofHex "190100") |> ignore), "256 is not a byte"))
        Test.Sync("options and collections round-trip", fun () ->
            Assert.True(roundTrip((fun (w, x) -> σOpt(w, x, σInt)), δOpt δInt, Some 3) = Some 3, "Some")
            assertRoundTrip((fun (w, x) -> σVOpt(w, x, σInt)), δVOpt δInt, ValueNone)
            let ia = ImmutableArray.Create(1, 2, 3)
            Assert.Equal([1; 2; 3], List.ofSeq(roundTrip((fun (w, x) -> σImmArray(w, x, σInt)), δImmArray δInt, ia)))
            Assert.Equal<int list>([1; 2], List.ofArray(roundTrip((fun (w, x) -> σArrayOf(w, x, σInt)), δArrayOf δInt, [| 1; 2 |])))
            assertRoundTrip((fun (w, x) -> σList(w, x, σString)), δList δString, ["a"; "b"])
            let d = ImmutableDictionary.CreateRange([ Collections.Generic.KeyValuePair("a", 1); Collections.Generic.KeyValuePair("b", 2) ])
            let d2 = roundTrip((fun (w, x) -> σImmDict(w, x, σString, σInt)), δImmDict(δString, δInt), d)
            Assert.Equal(2, d2.Count)
            Assert.Equal(2, d2.["b"])
            assertRoundTrip((fun (w, x) -> σResult(w, x, σInt, σString)), δResult(δInt, δString), Error "e")
            assertRoundTrip((fun (w, x) -> σChoice(w, x, σString, σInt)), δChoice(δString, δInt), Choice1Of2 "x"))
        Test.Sync("collections read back from a writer that keeps indefinite lengths", fun () ->
            let w = CborWriter()
            σList(w, [1; 2], σInt)
            Assert.Equal([1; 2], Cbor.decode(δList δInt, ReadOnlyMemory(w.Encode()))))
        Test.Sync("an empty ImmA2D keeps its column count", fun () ->
            let m = ImmA2D<int>(0, 3, ImmutableArray.Empty)
            let m2 = roundTrip((fun (w, x) -> σImmA2D(w, x, σInt)), δImmA2D δInt, m)
            Assert.Equal(3, m2.Cols))
        Test.Sync("δArray skips trailing fields and δTrailing defaults missing ones", fun () ->
            Assert.Equal(1, Cbor.decode((fun r -> δArray(r, fun () -> δInt r)), ofHex "83010203"))
            Assert.Equal((1, 9), Cbor.decode((fun r -> δArray(r, fun () -> δInt r, δTrailing(r, δInt, 9))), ofHex "8101")))
        Test.Sync("nesting deeper than 500 is rejected", fun () ->
            Assert.Equal(499, Cbor.decode(δNested, nested 500))
            Assert.True(throwsContent(fun () -> Cbor.decode(δNested, nested 501) |> ignore), "501 levels"))
        Test.Sync("a declared length beyond the input is rejected before allocating", fun () ->
            Assert.True(throwsContent(fun () -> Cbor.decode(δImmArray δInt, ofHex "9A7FFFFFFF") |> ignore), "array length")
            Assert.True(throwsContent(fun () -> Cbor.decode(δImmA2D δInt, ofHex "831A000100001A00010000") |> ignore), "ImmA2D dimensions")
            Assert.True(throwsContent(fun () -> Cbor.decode(δImmA2D δInt, ofHex "832001") |> ignore), "negative rows"))
        Test.Sync("compressed round-trip, including through the C# wrapper", fun () ->
            let xs = ImmutableArray.CreateRange(Seq.replicate 1000 "repeated")
            let bytes = Cbor.encodeCompressed((fun (w, x) -> σImmArray(w, x, σString)), xs)
            Assert.True(bytes.Length < 100, "compresses")
            Assert.True(Seq.forall2 (=) xs (Cbor.decodeCompressed(δImmArray δString, bytes)), "decodes")
            let viaCSharp = CompressedCbor.Encode(Action<CborWriter, string>(fun w s -> σString(w, s)), "x")
            Assert.Equal("x", CompressedCbor.Decode(Func<CborReader, string>(δString), viaCSharp)))
    ])
