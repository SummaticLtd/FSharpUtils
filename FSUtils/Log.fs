namespace FSUtils

open System
open System.Collections.Generic
open ISLogger

type Log private () =
    static let mutable logger = Diagnostics() :> ISLogger

    static let combine(summary: string, details: string) =
        if String.IsNullOrEmpty details then summary else summary + " " + details

    /// Set the sink receiving all logging. Until a host calls this, logging goes to Diagnostics, which writes to Debug and breaks into an attached debugger on errors.
    static member Set(l:ISLogger) = logger <- l

    static member Diag(s:string) = logger.LogDiagnostic s

    static member EventWithParameters(s:string, parameters:IDictionary<string, string>) = logger.LogEvent(s, parameters)

    static member Event(s:string) = Log.EventWithParameters(s, dict [])

    static member Exception(ex:Exception, summary:string) = Log.Exception(ex, summary, "")

    static member Exception(ex:Exception, summary:string, details:string) =
        match ex with
        | :? AggregateException as aggEx ->
            let aggEx = aggEx.Flatten()
            let aggSummary = summary + " Details below:"
            logger.LogException(aggEx, aggSummary, combine(aggSummary, details))
            for ex in aggEx.InnerExceptions do
                logger.LogException(ex, summary, combine(summary, details))
        | _ ->
            logger.LogException(ex, summary, combine(summary, details))

    static member Error(s:string) = logger.LogError(s, s)

    static member Error(summary:string, details:string) = logger.LogError(summary, combine(summary, details))
