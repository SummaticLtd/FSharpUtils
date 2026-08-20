module Base64Url

open System

// Base64url encoding and decoding, avoiding a dependency on a whole library for the job.

let Decode(base64urlstr:string) =
    let ss = base64urlstr.Replace('-','+').Replace('_','/')
    ss.PadRight(ss.Length + (4 - ss.Length % 4) % 4, '=')
    |> Convert.FromBase64String

let DecodeToString(base64urlstr:string) =
    let ba = Decode(base64urlstr)
    System.Text.Encoding.UTF8.GetString(ba, 0, ba.Length)

let Encode(bytes:byte[]) =
    Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_")
