namespace FSUtils

open System

[<RequireQualifiedAccess>]
module Parse =
    open System.Numerics
    let toInt(s:string) = match Int32.TryParse s with (true, r) -> ValueSome r | _ -> ValueNone
    let toIntSpan(s:ReadOnlySpan<char>) = match Int32.TryParse s with (true, r) -> ValueSome r | _ -> ValueNone
    let toInt16(s:string) = match Int16.TryParse s with (true, r) -> ValueSome r | _ -> ValueNone
    let toGuid(s:string) = match Guid.TryParse s with (true, r) -> ValueSome r | _ -> ValueNone
    let toAbsoluteUri(s:string) = match Uri.TryCreate(s, UriKind.Absolute) with (true, NonNull r) -> ValueSome r | _ -> ValueNone
    let toFloat(s:string) = match System.Double.TryParse(s, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture) with (true, r) -> ValueSome r | _ -> ValueNone
    let toFloatSpan(s:ReadOnlySpan<char>) = match System.Double.TryParse(s, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture) with (true, r) -> ValueSome r | _ -> ValueNone
    let toComplex(s:string) =
        let toImaginary(s:string) =
            let l = s.Length
            if l > 0 && s.[l-1] = 'i' then
                let bef = s.[.. l-2]
                if bef = "" then ValueSome 1. else bef |> toFloat
            else ValueNone
        let findImaginary() = toImaginary s |> ValueOption.map (fun im -> Complex(0., im))
        let findReal() = s |> toFloat |> ValueOption.map (fun re -> Complex(re, 0.))
        let findPlus() =
            match s |> Seq.tryFindIndexBack ((=) '+') with
            | Some plusInd ->
                let bef, aft = s.[.. plusInd-1], s.[plusInd+1 ..]
                match toFloat bef, toImaginary aft with
                | ValueSome re, ValueSome im -> ValueSome(Complex(re, im))
                | _ ->
                    match toImaginary bef, toFloat aft with
                    | ValueSome im, ValueSome re -> ValueSome(Complex(re, im))
                    | _ -> ValueNone
            | None -> ValueNone
        let findMinus() =
            match s |> Seq.tryFindIndexBack ((=) '-') with
            | Some minusInd ->
                let bef, aft = s.[.. minusInd-1], s.[minusInd+1 ..]
                match toFloat bef, toImaginary aft with
                | ValueSome re, ValueSome im when im >= 0. -> ValueSome(Complex(re, -im))
                | _ ->
                    match toImaginary bef, toFloat aft with
                    | ValueSome im, ValueSome re when re >= 0. -> ValueSome(Complex(re, im))
                    | _ -> ValueNone
            | None ->
                toFloat s |> ValueOption.map (fun re -> Complex(re, 0.))
                |> ValueOption.orElse (toImaginary s |> ValueOption.map (fun im -> Complex(0., im)))
        let mutable found = ValueNone: Complex voption
        for f in [findReal; findImaginary; findPlus; findMinus] do
            if found.IsNone then
                found <- f()
        found
