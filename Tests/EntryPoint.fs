module Tests.EntryPoint

open SimpleTests

let testFolders = [
    TestFolder("Tests", [
        Tests.ImmArrayTests.ImmArrayTestList
        Tests.ImmA2DTests.ImmA2DTestList
        Tests.ParseTests.ParseTestList
        Tests.InstantTests.InstantTestList
        Tests.CombinatorTests.CombinatorTestList
        Tests.JsonTests.JsonTestList
    ])
]

[<EntryPoint>]
let main (args: string array) : int =
    Runner.Run(args, testFolders)
