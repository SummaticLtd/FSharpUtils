namespace ISLogger

open System
open System.Diagnostics
open System.Collections.Generic

type ISLogger =
    abstract member LogDiagnostic : string -> unit
    abstract member LogEvent : string * IDictionary<string, string> -> unit
    // summary is the stable one-line description (used to form titles/dedup keys); message is the full text to log.
    abstract member LogError : summary: string * message: string -> unit
    abstract member LogException : Exception * summary: string * message: string -> unit

type Combine(x:ISLogger, y:ISLogger) =
    interface ISLogger with
        member _.LogDiagnostic s = x.LogDiagnostic s; y.LogDiagnostic s
        member _.LogEvent (s, parameters) = x.LogEvent(s, parameters); y.LogEvent(s, parameters)
        member _.LogError (summary, message) = x.LogError(summary, message); y.LogError(summary, message)
        member _.LogException (ex, summary, message) = x.LogException(ex, summary, message); y.LogException(ex, summary, message)

module Utils =
    let ParamsToStr(parameters:IDictionary<string, string>) =
        String.Join(", ", parameters |> Seq.map(fun kvp -> kvp.Key + "=" + kvp.Value))

type Console() =
    interface ISLogger with
        member _.LogDiagnostic s = Console.WriteLine("Diagnostic: " + s)
        member _.LogEvent (s, parameters) = Console.WriteLine("Event: " + s + ". " + Utils.ParamsToStr(parameters))
        member _.LogError (_, message) = Console.WriteLine("Error: " + message)
        member _.LogException (ex, _, message) = Console.WriteLine("Exception: " + message + "\n" + ex.ToString())

type Diagnostics() =
    interface ISLogger with
        member _.LogDiagnostic s = Diagnostics.Debug.WriteLine("Diagnostic: " + s)
        member _.LogEvent (s, parameters) = Diagnostics.Debug.WriteLine("Event: " + s + ". " + Utils.ParamsToStr(parameters))
        member _.LogError (_, message) = Diagnostics.Debug.WriteLine("Error: " + message); Debugger.Break()
        member _.LogException (ex, _, message) = Diagnostics.Debug.WriteLine("Exception: " + ex.Message + " " + message + "\n" + ex.StackTrace); Debugger.Break()