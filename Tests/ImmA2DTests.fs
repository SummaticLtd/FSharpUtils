module Tests.ImmA2DTests

open SimpleTests
open FSUtils
open Tests

let private grid = ImmA2D.fromJagged [ [ 1; 2; 3 ]; [ 4; 5; 6 ] ]

let ImmA2DTestList =
    TestList("ImmA2D", [
        Test.Sync("fromJagged is row-major", fun () ->
            Assert.Equal(2, grid.Rows)
            Assert.Equal(3, grid.Cols)
            Assert.Equal(2, grid.[0, 1])
            Assert.Equal(4, grid.[1, 0]))
        Test.Sync("fromJagged of no rows is empty, not an error", fun () ->
            let empty = ImmA2D.fromJagged ([]: int list list)
            Assert.Equal(0, empty.Rows)
            Assert.Equal(0, empty.Cols))
        Test.Sync("fromJagged rejects ragged rows", fun () ->
            Assert.Throws((fun () -> ImmA2D.fromJagged [ [ 1; 2 ]; [ 3 ] ] |> ignore), "ragged rows"))
        Test.Sync("fromJaggedWithKnownCols rejects a wrong row width", fun () ->
            Assert.Throws((fun () -> ImmA2D.fromJaggedWithKnownCols 2 [ [ 1; 2; 3 ] ] |> ignore), "wrong width"))
        Test.Sync("indexing out of bounds throws", fun () ->
            Assert.Throws((fun () -> grid.[2, 0] |> ignore), "row out of range")
            Assert.Throws((fun () -> grid.[0, 3] |> ignore), "column out of range"))
        Test.Sync("mapi sees row then column", fun () ->
            let indices = grid |> ImmA2D.mapi(fun r c _ -> (r, c))
            Assert.Equal((0, 2), indices.[0, 2])
            Assert.Equal((1, 0), indices.[1, 0]))
        Test.Sync("iteri visits every cell in row-major order", fun () ->
            let visited = ResizeArray()
            grid |> ImmA2D.iteri(fun r c v -> visited.Add(r, c, v))
            Assert.Equal(6, visited.Count)
            Assert.Equal((0, 0, 1), visited.[0])
            Assert.Equal((1, 2, 6), visited.[5]))
        Test.Sync("equality is structural", fun () ->
            Assert.Equal(grid, ImmA2D.fromJagged [ [ 1; 2; 3 ]; [ 4; 5; 6 ] ])
            Assert.False((grid = ImmA2D.fromJagged [ [ 1; 2; 3 ] ]), "different shapes are unequal"))
        Test.Sync("shape is part of identity", fun () ->
            Assert.False((ImmA2D.fromJagged [ [ 1; 2 ]; [ 3; 4 ] ] = ImmA2D.fromJagged [ [ 1; 2; 3; 4 ] ]), "same elements, different shape"))
        Test.Sync("A2D.init fills row-major", fun () ->
            let a = A2D.init 2 3 (fun r c -> r * 10 + c)
            Assert.Equal(12, a.[1, 2]))
    ])
