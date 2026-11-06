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
let private throwsContent(f: unit -> unit) =
    try f(); false
    with :? CborContentException -> true

let rec private δNested(r: CborReader) : int = δArray(r, fun () -> if isArray r then δNested r + 1 else 0)
let private nested(depth: int) = ofHex(String.replicate (depth - 1) "81" + "80")

let CborTestList =
    TestList("Cbor", [
        Test.Sync("primitives round-trip", fun () ->
            Assert.Equal("héllo", roundTrip(σString, δString, "héllo"))
            Assert.Equal('é', roundTrip(σChar, δChar, 'é'))
            Assert.Equal(-7, roundTrip(σInt, δInt, -7))
            Assert.Equal(255uy, roundTrip(σByte, δByte, 255uy))
            Assert.Equal(-300s, roundTrip(σInt16, δInt16, -300s))
            Assert.Equal(Int64.MinValue, roundTrip(σInt64, δInt64, Int64.MinValue))
            Assert.Equal(UInt32.MaxValue, roundTrip(σUInt, δUInt, UInt32.MaxValue))
            Assert.Equal(1.5, roundTrip(σFloat, δFloat, 1.5))
            Assert.Equal(1.5f, roundTrip(σFloat32, δFloat32, 1.5f))
            Assert.Equal(true, roundTrip(σBool, δBool, true))
            Assert.Equal(DayOfWeek.Friday, roundTrip(σEnum, δEnum, DayOfWeek.Friday))
            Assert.Equal(TimeSpan.FromMinutes 3.0, roundTrip(σTimeSpan, δTimeSpan, TimeSpan.FromMinutes 3.0))
            Assert.Equal(Numerics.Complex(1.0, -2.0), roundTrip(σComplex, δComplex, Numerics.Complex(1.0, -2.0)))
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
            Assert.Equal(ValueNone, roundTrip((fun (w, x) -> σVOpt(w, x, σInt)), δVOpt δInt, ValueNone))
            let ia = ImmutableArray.Create(1, 2, 3)
            Assert.Equal([1; 2; 3], List.ofSeq(roundTrip((fun (w, x) -> σImmArray(w, x, σInt)), δImmArray δInt, ia)))
            Assert.Equal<int list>([1; 2], List.ofArray(roundTrip((fun (w, x) -> σArrayOf(w, x, σInt)), δArrayOf δInt, [| 1; 2 |])))
            Assert.Equal(["a"; "b"], roundTrip((fun (w, x) -> σList(w, x, σString)), δList δString, ["a"; "b"]))
            let d = ImmutableDictionary.CreateRange([ Collections.Generic.KeyValuePair("a", 1); Collections.Generic.KeyValuePair("b", 2) ])
            let d2 = roundTrip((fun (w, x) -> σImmDict(w, x, σString, σInt)), δImmDict(δString, δInt), d)
            Assert.Equal(2, d2.Count)
            Assert.Equal(2, d2.["b"])
            Assert.Equal(Error "e", roundTrip((fun (w, x) -> σResult(w, x, σInt, σString)), δResult(δInt, δString), Error "e"))
            Assert.Equal(Choice1Of2 "x", roundTrip((fun (w, x) -> σChoice(w, x, σString, σInt)), δChoice(δString, δInt), Choice1Of2 "x")))
        Test.Sync("an empty ImmA2D keeps its column count", fun () ->
            let m = ImmA2D<int>(0, 3, ImmutableArray.Empty)
            let m2 = roundTrip((fun (w, x) -> σImmA2D(w, x, σInt)), δImmA2D δInt, m)
            Assert.Equal(3, m2.Cols))
        Test.Sync("collection readers reject indefinite-length arrays", fun () ->
            let bytes = ofHex "9F0102FF"
            Assert.True(throwsContent(fun () -> Cbor.decode(δImmArray δInt, bytes) |> ignore), "δImmArray")
            Assert.True(throwsContent(fun () -> Cbor.decode(δArrayOf δInt, bytes) |> ignore), "δArrayOf")
            Assert.True(throwsContent(fun () -> Cbor.decode(δImmDict(δInt, δInt), ofHex "9F820102FF") |> ignore), "δImmDict"))
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
