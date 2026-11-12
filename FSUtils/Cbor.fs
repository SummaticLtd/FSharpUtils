namespace FSUtils

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.Formats.Cbor

/// σX writes an X to a CborWriter; δX reads one from a CborReader.
module Cbor =

    let encode<'a>(σ: CborWriter * 'a -> unit, x: 'a) : byte[] =
        // Arrays are written without a length, which is filled in when each closes.
        let w = CborWriter(CborConformanceMode.Lax, convertIndefiniteLengthEncodings = true)
        σ(w, x)
        w.Encode()

    let decode<'a>(δ: CborReader -> 'a, bytes: ReadOnlyMemory<byte>) : 'a =
        // Deeper input would overflow the stack of the recursive readers.
        δ(CborReader(bytes, CborReaderOptions(ConformanceMode = CborConformanceMode.Lax, MaxDepth = 500)))

    let encodeCompressed<'a>(σ: CborWriter * 'a -> unit, x: 'a) = Deflate.compress(encode(σ, x))

    let decodeCompressed<'a>(δ: CborReader -> 'a, data: byte[]) = decode(δ, ReadOnlyMemory(Deflate.decompress data))

    let readStartArray(r: CborReader) = r.ReadStartArray()

    let private readStartDefiniteArray(r: CborReader) =
        let n = readStartArray r
        if n.HasValue then n.Value else raise(CborContentException "Expected a definite-length array")

    /// Writes the values written by body as one array.
    let inline σArray(w: CborWriter, [<InlineIfLambda>] body: unit -> unit) =
        w.WriteStartArray(Nullable())
        body()
        w.WriteEndArray()

    /// Reads an array with body, skipping trailing fields added by newer writers.
    let inline δArray<'a>(r: CborReader, [<InlineIfLambda>] body: unit -> 'a) : 'a =
        readStartArray r |> ignore
        let x = body()
        while r.PeekState() <> CborReaderState.EndArray do
            r.SkipValue()
        r.ReadEndArray()
        x

    /// Reads a field that older writers omitted.
    let δTrailing<'a>(r: CborReader, δ: CborReader -> 'a, ifAbsent: 'a) : 'a =
        if r.PeekState() = CborReaderState.EndArray then ifAbsent else δ r

    let isArray(r: CborReader) = r.PeekState() = CborReaderState.StartArray

    let σString(w: CborWriter, s: string) = w.WriteTextString s
    let δString(r: CborReader) = r.ReadTextString()

    let σChar(w: CborWriter, c: char) = w.WriteInt32(int c)
    let δChar(r: CborReader) = Checked.char(r.ReadInt32())

    let σInt(w: CborWriter, i: int) = w.WriteInt32 i
    let δInt(r: CborReader) = r.ReadInt32()

    let σByte(w: CborWriter, b: byte) = w.WriteInt32(int b)
    let δByte(r: CborReader) = Checked.byte(r.ReadInt32())

    let σInt16(w: CborWriter, i: int16) = w.WriteInt32(int i)
    let δInt16(r: CborReader) = Checked.int16(r.ReadInt32())

    let σInt64(w: CborWriter, i: int64) = w.WriteInt64 i
    let δInt64(r: CborReader) = r.ReadInt64()

    let σUInt(w: CborWriter, i: uint32) = w.WriteUInt32 i
    let δUInt(r: CborReader) = r.ReadUInt32()

    let σFloat(w: CborWriter, f: float) = w.WriteDouble f
    let δFloat(r: CborReader) = r.ReadDouble()

    let σFloat32(w: CborWriter, f: float32) = w.WriteSingle f
    let δFloat32(r: CborReader) = r.ReadSingle()

    let σBool(w: CborWriter, b: bool) = w.WriteBoolean b
    let δBool(r: CborReader) = r.ReadBoolean()

    let private guidTag = LanguagePrimitives.EnumOfValue<uint64, CborTag> 37UL
    /// IANA-registered UUID tag, in RFC 9562 byte order; System.Formats.Cbor leaves registered tags beyond RFC 8949 to applications.
    let σGuid(w: CborWriter, g: Guid) =
        w.WriteTag guidTag
        w.WriteByteString(ReadOnlySpan(g.ToByteArray(true)))
    let δGuid(r: CborReader) =
        if r.ReadTag() <> guidTag then raise(CborContentException "Expected Guid tag 37")
        Guid(ReadOnlySpan(r.ReadByteString()), true)

    let σByteArr(w: CborWriter, b: byte[]) = w.WriteByteString b
    let δByteArr(r: CborReader) = r.ReadByteString()

    let σEnum<'e when 'e: enum<int>>(w: CborWriter, e: 'e) = w.WriteInt32(LanguagePrimitives.EnumToValue e)
    let δEnum<'e when 'e: enum<int>>(r: CborReader) : 'e = LanguagePrimitives.EnumOfValue(r.ReadInt32())

    let σInstant(w: CborWriter, i: Instant) = w.WriteInt64(i.DT.ToBinary())
    let δInstant(r: CborReader) = Instant.FromUTCDateTime(DateTime.FromBinary(r.ReadInt64()))

    let σTimeSpan(w: CborWriter, ts: TimeSpan) = w.WriteInt64 ts.Ticks
    let δTimeSpan(r: CborReader) = TimeSpan.FromTicks(r.ReadInt64())

    let σComplex(w: CborWriter, c: Numerics.Complex) = σArray(w, fun () -> σFloat(w, c.Real); σFloat(w, c.Imaginary))
    let δComplex(r: CborReader) = δArray(r, fun () -> Numerics.Complex(δFloat r, δFloat r))

    /// Serializes the Some value unwrapped, so do not use with nested options.
    let σOpt<'a>(w: CborWriter, x: 'a option, σ: CborWriter * 'a -> unit) =
        match x with
        | Some v -> σ(w, v)
        | None -> w.WriteNull()
    let δOpt<'a> (δ: CborReader -> 'a) (r: CborReader) : 'a option =
        if r.PeekState() = CborReaderState.Null then
            r.ReadNull()
            None
        else Some(δ r)

    /// Serializes the ValueSome value unwrapped, so do not use with nested voptions.
    let σVOpt<'a>(w: CborWriter, x: 'a voption, σ: CborWriter * 'a -> unit) =
        match x with
        | ValueSome v -> σ(w, v)
        | ValueNone -> w.WriteNull()
    let δVOpt<'a> (δ: CborReader -> 'a) (r: CborReader) : 'a voption =
        if r.PeekState() = CborReaderState.Null then
            r.ReadNull()
            ValueNone
        else ValueSome(δ r)

    let inline private σDefinite(w: CborWriter, n: int, [<InlineIfLambda>] body: unit -> unit) =
        w.WriteStartArray(Nullable n)
        body()
        w.WriteEndArray()

    let σImmArray<'a>(w: CborWriter, xs: ImmutableArray<'a>, σ: CborWriter * 'a -> unit) =
        σDefinite(w, xs.Length, fun () -> for x in xs do σ(w, x))
    let δImmArray<'a> (δ: CborReader -> 'a) (r: CborReader) : ImmutableArray<'a> =
        let n = readStartDefiniteArray r
        let b = ImmutableArray.CreateBuilder<'a>(n)
        for _ in 1 .. n do
            b.Add(δ r)
        r.ReadEndArray()
        b.MoveToImmutable()

    let σArrayOf<'a>(w: CborWriter, xs: 'a[], σ: CborWriter * 'a -> unit) =
        σDefinite(w, xs.Length, fun () -> for x in xs do σ(w, x))
    let δArrayOf<'a> (δ: CborReader -> 'a) (r: CborReader) : 'a[] =
        let n = readStartDefiniteArray r
        let a = Array.zeroCreate<'a> n
        for i in 0 .. n - 1 do
            a.[i] <- δ r
        r.ReadEndArray()
        a

    let σList<'a>(w: CborWriter, xs: 'a list, σ: CborWriter * 'a -> unit) =
        σDefinite(w, List.length xs, fun () -> for x in xs do σ(w, x))
    let δList<'a> (δ: CborReader -> 'a) (r: CborReader) : 'a list =
        List.ofArray(δArrayOf δ r)

    let σSeq<'a>(w: CborWriter, xs: IReadOnlyCollection<'a>, σ: CborWriter * 'a -> unit) =
        σDefinite(w, xs.Count, fun () -> for x in xs do σ(w, x))

    let σImmDict<'k, 'v when 'k: not null and 'k: equality>(w: CborWriter, d: ImmutableDictionary<'k, 'v>, σKey: CborWriter * 'k -> unit, σValue: CborWriter * 'v -> unit) =
        σSeq(w, d, fun (w, kv) -> σArray(w, fun () -> σKey(w, kv.Key); σValue(w, kv.Value)))
    let δImmDict<'k, 'v when 'k: not null and 'k: equality> (δKey: CborReader -> 'k, δValue: CborReader -> 'v) (r: CborReader) =
        let b = ImmutableDictionary.CreateBuilder<'k, 'v>()
        let n = readStartDefiniteArray r
        for _ in 1 .. n do
            δArray(r, fun () -> b.Add(δKey r, δValue r))
        r.ReadEndArray()
        b.ToImmutable()

    // Rows and cols are stored explicitly, so an array with 0 rows keeps its column count.
    let σImmA2D<'a when 'a: equality>(w: CborWriter, m: ImmA2D<'a>, σ: CborWriter * 'a -> unit) =
        σArray(w, fun () ->
            σInt(w, m.Rows)
            σInt(w, m.Cols)
            for x in m.Elements do // row-major
                σ(w, x))
    /// Reads an ImmA2D from an array already opened.
    let δImmA2DFields<'a when 'a: equality>(δ: CborReader -> 'a, r: CborReader) =
        let rows = δInt r
        let cols = δInt r
        // Each element takes at least a byte, so this bounds the allocation by the input size.
        if rows < 0 || cols < 0 || int64 rows * int64 cols > int64 r.BytesRemaining then
            raise(CborContentException("Invalid ImmA2D dimensions " + rows.ToString() + " x " + cols.ToString()))
        let b = ImmutableArray.CreateBuilder<'a>(rows * cols)
        for _ in 1 .. rows * cols do
            b.Add(δ r)
        ImmA2D(rows, cols, b.MoveToImmutable())
    let δImmA2D<'a when 'a: equality> (δ: CborReader -> 'a) (r: CborReader) = δArray(r, fun () -> δImmA2DFields(δ, r))

    let σChoice<'a, 'b>(w: CborWriter, c: Choice<'a, 'b>, σA: CborWriter * 'a -> unit, σB: CborWriter * 'b -> unit) =
        σArray(w, fun () ->
            match c with
            | Choice1Of2 a -> σBool(w, true); σA(w, a)
            | Choice2Of2 b -> σBool(w, false); σB(w, b))
    let δChoice<'a, 'b> (δA: CborReader -> 'a, δB: CborReader -> 'b) (r: CborReader) =
        δArray(r, fun () -> if δBool r then Choice1Of2(δA r) else Choice2Of2(δB r))

    let σResult<'ok, 'error>(w: CborWriter, result: Result<'ok, 'error>, σOk: CborWriter * 'ok -> unit, σError: CborWriter * 'error -> unit) =
        σArray(w, fun () ->
            match result with
            | Ok v -> σBool(w, true); σOk(w, v)
            | Error e -> σBool(w, false); σError(w, e))
    let δResult<'ok, 'error> (δOk: CborReader -> 'ok, δError: CborReader -> 'error) (r: CborReader) : Result<'ok, 'error> =
        δArray(r, fun () -> if δBool r then Ok(δOk r) else Error(δError r))

/// For C#, whose method groups convert to delegates but not to F# functions.
[<AbstractClass; Sealed>]
type CompressedCbor =
    static member Encode<'a>(σ: Action<CborWriter, 'a>, x: 'a) = Cbor.encodeCompressed((fun (w, x) -> σ.Invoke(w, x)), x)
    static member Decode<'a>(δ: Func<CborReader, 'a>, data: byte[]) = Cbor.decodeCompressed(δ.Invoke, data)
