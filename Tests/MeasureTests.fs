module Tests.MeasureTests

open SimpleTests
open FSUtils
open Tests

[<Measure>] type testUnit

let MeasureTestList =
    TestList("Measure", [
        Test.Sync("units survive a float32 round trip", fun () ->
            let px = Measure.WithFloatUnit<testUnit> 3.5
            Assert.Equal(3.5, Measure.removeFloatUnit<testUnit> px)
            Assert.Equal(3.5f, Measure.removeFloat32Unit<testUnit>(Measure.toFloat32 px))
            Assert.Equal(3.5, Measure.removeFloatUnit<testUnit>(Measure.float32ToFloat(Measure.toFloat32 px))))
        Test.Sync("Max32 and Min32 keep the unit", fun () ->
            let a, b = Measure.WithFloat32Unit<testUnit> 1.0f, Measure.WithFloat32Unit<testUnit> 2.0f
            Assert.Equal(2.0f, Measure.removeFloat32Unit<testUnit>(Measure.Max32(a, b)))
            Assert.Equal(1.0f, Measure.removeFloat32Unit<testUnit>(Measure.Min32(a, b))))
    ])
