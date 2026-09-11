namespace FSUtils

[<RequireQualifiedAccess>]
module ToStr =
    /// Name and single contents
    let inline typeSimple(caseName:string, contents:'a) = caseName + "(" + (string contents) + ")"
    /// Name and multiple contents
    let typeProps(caseName:string, contents:seq<obj>) =
        caseName + "(" + (contents |> Seq.map string |> String.concat ", ") + ")"
    /// Name and multiple contents
    let typeStrs(caseName:string, contents:seq<string>) =
        caseName + "(" + (contents |> String.concat ", ") + ")"
    /// Generic
    let inline typePropsG(caseName:string, contents:seq<'a>) =
        caseName + "(" + (contents |> Seq.map string |> String.concat ", ") + ")"
    let seq(contents:seq<'a>) = "[" + (contents |> Seq.map string |> String.concat "; ") + "]"
