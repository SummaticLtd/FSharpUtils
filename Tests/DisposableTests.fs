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

/// Runs f with a capturing sink installed and returns what it logged.
let private logged(f: unit -> unit) =
    let logger = CapturingLogger()
    Log.Set logger
    f()
    List.ofSeq logger.Errors

/// A disposable that appends its label when disposed.
let private tracked(log: ResizeArray<string>, label: string) =
    { new IDisposable with member _.Dispose() = log.Add label }

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

        Test.Sync("an addition to a disposed ConcurrentCD is disposed at once", fun () ->
            let log = ResizeArray<string>()
            let cd = new ConcurrentCD()
            cd.Dispose()
            cd.Add(tracked(log, "late"))
            Assert.Equal(1, log.Count)
            Assert.True(cd.DisposedRequested, "disposal was recorded"))

        Test.Sync("a ConcurrentCD cancels its token on disposal", fun () ->
            let cd = new ConcurrentCD()
            Assert.True(not cd.CancellationToken.IsCancellationRequested, "before disposal")
            cd.Dispose()
            Assert.True(cd.CancellationToken.IsCancellationRequested, "after disposal"))
    ])
