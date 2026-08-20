namespace FSUtils

open System

[<RequireQualifiedAccess>]
module Measure =
    let toFloat32<[<Measure>] 'm> (f:float<'m>) =
        LanguagePrimitives.Float32WithMeasure<'m> (float32 f)
    let intToFloat(f:int) = float f
    let intToFloat32(f:int) = float32 f
    let toInt(f:float) = int f
    [<RequiresExplicitTypeArguments>]
    let removeFloat32Unit<[<Measure>] 'm> (f:float32<'m>) = float32 f
    [<RequiresExplicitTypeArguments>]
    let removeFloatUnit<[<Measure>] 'm> (f:float<'m>) = float f
    let float32ToFloat<[<Measure>] 'm> (f:float32<'m>) =
        LanguagePrimitives.FloatWithMeasure<'m> (float f)
    let max32<[<Measure>] 'm>(x:float32<'m>, y:float32<'m>) =
        LanguagePrimitives.Float32WithMeasure<'m>(Math.Max(removeFloat32Unit<'m> x, removeFloat32Unit<'m> y))
    let min32<[<Measure>] 'm>(x:float32<'m>, y:float32<'m>) =
        LanguagePrimitives.Float32WithMeasure<'m>(Math.Min(removeFloat32Unit<'m> x, removeFloat32Unit<'m> y))
    let withFloat32Unit<[<Measure>] 'm> (f:float32) = LanguagePrimitives.Float32WithMeasure<'m>(f)
    let withFloatUnit<[<Measure>] 'm> (f:float) = LanguagePrimitives.FloatWithMeasure<'m>(f)
