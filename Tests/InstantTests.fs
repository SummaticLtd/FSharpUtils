module Tests.InstantTests

open System
open SimpleTests
open FSUtils
open Tests

let private utc = DateTime(2024, 6, 1, 13, 45, 30, DateTimeKind.Utc)

let InstantTestList =
    TestList("Instant", [
        Test.Sync("FromUTCDateTime keeps the instant", fun () ->
            Assert.Equal(utc, Instant.FromUTCDateTime(utc).DT))
        Test.Sync("FromUTCDateTime rejects non-UTC input", fun () ->
            Assert.Throws((fun () -> Instant.FromUTCDateTime(DateTime(2024, 6, 1)) |> ignore), "unspecified kind")
            Assert.Throws((fun () -> Instant.FromUTCDateTime(DateTime.SpecifyKind(utc, DateTimeKind.Local)) |> ignore), "local kind"))
        Test.Sync("FromDateTimeForcingUTC reinterprets the kind without shifting", fun () ->
            let unspecified = DateTime(2024, 6, 1, 13, 45, 30)
            Assert.Equal(utc, Instant.FromDateTimeForcingUTC(unspecified).DT))
        Test.Sync("FromDateTimeOffset converts to UTC", fun () ->
            let dto = DateTimeOffset(2024, 6, 1, 15, 45, 30, TimeSpan.FromHours 2.0)
            Assert.Equal(utc, Instant.FromDateTimeOffset(dto).DT))
        Test.Sync("arithmetic is TimeSpan-based", fun () ->
            let i = Instant.FromUTCDateTime utc
            Assert.Equal(TimeSpan.FromHours 1.0, (i + TimeSpan.FromHours 1.0) - i)
            Assert.Equal(i.DT, ((i + TimeSpan.FromHours 1.0) - TimeSpan.FromHours 1.0).DT))
        Test.Sync("MinValue and MaxValue are UTC", fun () ->
            Assert.Equal(DateTimeKind.Utc, Instant.MinValue.DT.Kind)
            Assert.Equal(DateTimeKind.Utc, Instant.MaxValue.DT.Kind))
        Test.Sync("ToString is culture-independent and sortable", fun () ->
            Assert.Equal("2024-06-01 13-45-30", Instant.FromUTCDateTime(utc).ToString()))
        Test.Sync("comparison follows the instant", fun () ->
            let earlier = Instant.FromUTCDateTime utc
            let later = earlier + TimeSpan.FromSeconds 1.0
            Assert.True(earlier < later, "earlier sorts first")
            Assert.Equal(earlier, Instant.FromUTCDateTime utc))
    ])
