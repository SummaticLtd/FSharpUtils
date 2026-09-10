module Tests.DisposableTests

open System
open System.Collections.Generic
open SimpleTests
open FSUtils

/// Captures what is logged, so that the default sink, which breaks into the debugger, never runs.
type private CapturingLogger() =
    let errors = ResizeArray<string>()
    member _.Errors = errors
    interface ISLogger.ISLogger with
        member _.LogDiagnostic(_: string) = ()
        member _.LogEvent(_: string, _: IDictionary<string, string>) = ()
        member _.LogError(_: string, message: string) = errors.Add message
        member _.LogException(_: exn, _: string, message: string) = errors.Add message

/// Installed for the whole run, so no test can reach the default sink.
let private capturing = CapturingLogger()
do Log.Set capturing

/// Returns what f logged.
let private logged(f: unit -> unit) =
    let alreadyLogged = capturing.Errors.Count
    f()
    List.ofSeq(Seq.skip alreadyLogged capturing.Errors)

/// A disposable that appends its label when disposed.
let private tracked(log: ResizeArray<string>, label: string) =
    { new IDisposable with member _.Dispose() = log.Add label }

/// A disposable that fails instead of recording, so that one bad item can be tested.
let private failing(label: string) =
    { new IDisposable with member _.Dispose() = failwith label }

let DisposableTestList =
    TestList("Disposable", [
        Test.Sync("SimpleCD disposes what it holds, most recently added first", fun () ->
            let log = ResizeArray<string>()
            let cd = new SimpleCD()
            cd.Add(tracked(log, "first"))
            cd.Add(tracked(log, "second"))
            Assert.Equal(0, log.Count, "nothing is disposed before the CD is")
            cd.Dispose()
            Assert.Equal(2, log.Count)
            Assert.Equal("second", log.[0], "the last added is disposed first")
            Assert.Equal("first", log.[1]))

        Test.Sync("SimpleCD.Adding returns what it was given", fun () ->
            let cd = new SimpleCD()
            let d = new SimpleCD()
            Assert.True(Object.ReferenceEquals(d, cd.Adding d), "the same instance comes back")
            cd.Dispose())

        Test.Sync("SimpleCD reports that disposal was requested", fun () ->
            let cd = new SimpleCD()
            Assert.True(not cd.DisposedRequested, "before disposal")
            cd.Dispose()
            Assert.True(cd.DisposedRequested, "after disposal"))

        Test.Sync("an addition to a disposed SimpleCD is disposed at once and reported", fun () ->
            let log = ResizeArray<string>()
            let cd = new SimpleCD()
            cd.Dispose()
            let errors = logged(fun () -> cd.Add(tracked(log, "late")))
            Assert.Equal(1, log.Count, "the late addition is disposed rather than held")
            Assert.Equal(1, errors.Length, "and the mistake is reported"))

        Test.Sync("disposing a SimpleCD twice is reported", fun () ->
            let cd = new SimpleCD()
            cd.Dispose()
            let errors = logged(fun () -> cd.Dispose())
            Assert.Equal(1, errors.Length))

        Test.Sync("SimpleCD releases the rest when one item throws", fun () ->
            let log = ResizeArray<string>()
            let cd = new SimpleCD()
            cd.Add(tracked(log, "bottom"))
            cd.Add(failing "boom")
            cd.Add(tracked(log, "top"))
            Assert.Throws((fun () -> cd.Dispose()), "the failure reaches the caller")
            Assert.Equal(2, log.Count, "the items either side of the failure were still released")
            Assert.Equal("top", log.[0])
            Assert.Equal("bottom", log.[1]))

        Test.Sync("SimpleCD disposal carries every failure", fun () ->
            let cd = new SimpleCD()
            cd.Add(failing "first")
            cd.Add(failing "second")
            let messages =
                try
                    cd.Dispose()
                    []
                with :? AggregateException as ex -> [ for e in ex.InnerExceptions -> e.Message ]
            Assert.Equal(2, messages.Length, "neither failure is dropped")
            Assert.True(List.contains "first" messages && List.contains "second" messages))

        Test.Sync("SerialDisposable disposes the previous value when switched", fun () ->
            let log = ResizeArray<string>()
            let sd = new SerialDisposable()
            sd.Switch(tracked(log, "first"))
            Assert.Equal(0, log.Count, "the first is held")
            sd.Switch(tracked(log, "second"))
            Assert.Equal(1, log.Count, "switching disposes the one it replaces")
            Assert.Equal("first", log.[0])
            sd.Dispose()
            Assert.Equal(2, log.Count, "disposal releases the last one"))

        Test.Sync("a switch after disposal is disposed at once", fun () ->
            let log = ResizeArray<string>()
            let sd = new SerialDisposable()
            sd.Dispose()
            sd.Switch(tracked(log, "late"))
            Assert.Equal(1, log.Count))

        Test.Sync("a SerialDisposable is left disposed even when its inner value throws", fun () ->
            let log = ResizeArray<string>()
            let sd = new SerialDisposable()
            sd.Switch(failing "boom")
            Assert.Throws((fun () -> sd.Dispose()), "the failure reaches the caller")
            sd.Switch(tracked(log, "late"))
            Assert.Equal(1, log.Count, "a later switch is still disposed at once"))

        Test.Sync("a CancellationScope cancels its token on disposal", fun () ->
            let scope = new CancellationScope()
            Assert.True(not scope.Token.IsCancellationRequested, "before disposal")
            Assert.True(not scope.IsCancelled)
            scope.Dispose()
            Assert.True(scope.Token.IsCancellationRequested, "after disposal")
            Assert.True(scope.IsCancelled))

        Test.Sync("a CancellationScope tolerates repeated disposal", fun () ->
            let scope = new CancellationScope()
            let token = scope.Token
            scope.Dispose()
            scope.Dispose()
            Assert.True(token.IsCancellationRequested, "a token taken earlier is still readable"))

        Test.Sync("a CancellationScope held by a SimpleCD is cancelled with it", fun () ->
            let cd = new SimpleCD()
            let scope = new CancellationScope() |> cd.Adding
            cd.Dispose()
            Assert.True(scope.IsCancelled))

        Test.Sync("switching a CancellationScope cancels the one it replaces", fun () ->
            let sd = new SerialDisposable()
            let first = new CancellationScope() |> sd.Switching
            let second = new CancellationScope() |> sd.Switching
            Assert.True(first.IsCancelled, "the replaced scope is cancelled")
            Assert.True(not second.IsCancelled, "the current one is not")
            sd.Dispose()
            Assert.True(second.IsCancelled, "and it goes with the SerialDisposable"))
    ])
