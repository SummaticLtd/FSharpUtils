namespace FSUtils

open System

[<Struct>]
type Instant private (dt:DateTime) =
    /// UTC DateTime
    member _.DT = dt
    member _.DTO = DateTimeOffset(dt)
    member _.LocalDT = dt.ToLocalTime()
    /// E.g. 01 JAN 14:53  or  01 JAN 13 14:53 in *local time*
    member _.LocalTimeString(?includeSeconds:bool) =
        let localDt = dt.ToLocalTime()
        let secondsStr =
            if defaultArg includeSeconds false then ":ss" else ""
        if localDt.Year = DateTime.Now.Year then
            localDt.ToString("dd MMM HH:mm" + secondsStr, Globalization.CultureInfo.InvariantCulture)
        else
            localDt.ToString("dd MMM yyyy HH:mm" + secondsStr, Globalization.CultureInfo.InvariantCulture)
    static member Now = Instant(DateTime.UtcNow)
    static member (-) (x:Instant, y:Instant) = x.DT - y.DT
    static member (+) (x:Instant, y:TimeSpan) = Instant(x.DT + y)
    static member (-) (x:Instant, y:TimeSpan) = Instant(x.DT - y)
    static member FromUTCDateTime(dt:DateTime) =
        if dt.Kind <> DateTimeKind.Utc then raise <| ArgumentException("DateTime is not UTC. Use FromDateTimeForcingUTC to reinterpret it as UTC.")
        Instant(dt)
    /// For extraction of database datetime2s
    static member FromDateTimeForcingUTC(dt:DateTime) =
        Instant(DateTime.SpecifyKind(dt, DateTimeKind.Utc))
    static member FromDateTimeOffset(dto:DateTimeOffset) =
        Instant(dto.UtcDateTime)
    static member MinValue =
        Instant(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc))
    static member MaxValue =
        Instant(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc))
    static member TodayAtTime(timeOfDay: TimeSpan) =
        let localDateTime = DateTime.Now.Date + timeOfDay
        Instant.FromUTCDateTime(localDateTime.ToUniversalTime())
    override t.ToString() =
        t.DT.ToString("yyyy-MM-dd HH-mm-ss")
