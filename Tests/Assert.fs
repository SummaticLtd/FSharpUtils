namespace Tests

open System

/// Minimal assertions, so the test project needs nothing beyond SimpleTests.
type Assert =
    [<Diagnostics.DebuggerHidden>]
    static member Equal<'a when 'a: equality>(expected: 'a, actual: 'a) =
        if expected <> actual then
            failwith ("Expected: " + string expected + "\nBut was:  " + string actual)
    [<Diagnostics.DebuggerHidden>]
    static member True(condition: bool, message: string) =
        if not condition then failwith message
    [<Diagnostics.DebuggerHidden>]
    static member False(condition: bool, message: string) =
        if condition then failwith message
    /// Fails unless f raises an exception.
    [<Diagnostics.DebuggerHidden>]
    static member Throws(f: unit -> unit, message: string) =
        let mutable threw = false
        try f() with _ -> threw <- true
        if not threw then failwith ("Expected an exception: " + message)
