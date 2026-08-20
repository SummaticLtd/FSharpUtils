module Tests.EntryPoint

open SimpleTests

let testFolders = [
    TestFolder("Tests", [
        Tests.CoreTests.CoreTestList
        Tests.SeqTests.SeqTestList
        Tests.ArrayTests.ArrayTestList
        Tests.ImmArrayTests.ImmArrayTestList
        Tests.ImmA2DTests.ImmA2DTestList
        Tests.ParseTests.ParseTestList
        Tests.MeasureTests.MeasureTestList
        Tests.InstantTests.InstantTestList
        Tests.CombinatorTests.CombinatorTestList
        Tests.JsonTests.JsonTestList
        Tests.BuildersTests.BuildersTestList
    ])
]

[<EntryPoint>]
let main (args: string array) : int =
    Runner.Run(args, testFolders)
