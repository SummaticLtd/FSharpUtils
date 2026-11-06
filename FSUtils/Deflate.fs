namespace FSUtils

open System.IO
open System.IO.Compression

module Deflate =
    let compress(data: byte[]) =
        use output = new MemoryStream()
        do  use deflate = new DeflateStream(output, CompressionLevel.Optimal)
            deflate.Write(data, 0, data.Length)
        output.ToArray()

    let decompress(data: byte[]) =
        use input = new MemoryStream(data)
        use output = new MemoryStream()
        do  use deflate = new DeflateStream(input, CompressionMode.Decompress)
            deflate.CopyTo(output)
        output.ToArray()
