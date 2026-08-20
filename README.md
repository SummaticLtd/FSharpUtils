# FSUtils

General-purpose F# utilities, extracted from the [Summatic](https://summatic.co.uk) app. Published as `Summatic.FSUtils`; the namespace is `FSUtils`.

Everything here is allocation-conscious, trimming-friendly and AOT-friendly: no reflection, `ValueOption` rather than `Option`, `ImmutableArray` rather than `list`, and no `sprintf`.

## Contents

| Module | What it gives you |
| --- | --- |
| `ImmArray` | The `ImmutableArray` module FSharp.Core doesn't ship: `map`, `chooseV`, `groupBy`, `mapFold`, `partition` and about 80 more, all `InlineIfLambda` and builder-based. Lookups return `voption`. |
| `ImmA2D`, `A2D` | An immutable 2D array with structural equality, backed by a row-major `ImmutableArray`, and 0-based `Array2D` replacements. |
| `Equals`, `Hash`, `Compare` | Helpers for implementing `IEquatable<'T>` and `IComparable<'T>` over arrays, 2D arrays and tuples, instead of relying on F# structural equality. |
| `Result`, `ValueOption`, `Task`, `Async`, `Tuple` | The missing combinators, notably `Result.ofImmArrayMap` and `ValueOption.ofImmArrayMap`, which short-circuit. |
| `Instant` | A struct UTC timestamp. Non-UTC input is rejected rather than silently reinterpreted. |
| `Parse` | `voption`-returning, invariant-culture parsers for `int`, `float`, `Guid` and `Complex`. |
| `Json` | `Result`-returning `System.Text.Json` accessors that check the value kind instead of throwing. |
| `SimpleLazy` | `Lazy<'T>` without the trimming warnings. |
| `Builders` | `maybe`, `vmaybe` and `result` computation expressions. |
| `withLock` | `lock` over `System.Threading.Lock`, engaging its fast path ([dotnet/fsharp#17287](https://github.com/dotnet/fsharp/issues/17287)). |

## Requirements

.NET 10, and F# nullable reference types enabled (`<Nullable>enable</Nullable>`).

## Notes

Opening `FSUtils` brings modules named `Seq`, `Array`, `Result`, `ValueOption`, `Task` and `Async` into scope, which augment the FSharp.Core modules of the same name. It also auto-opens extensions to `Dictionary`, `Guid`, `ImmutableArray` and `TimeSpan`.

## Tests

```
dotnet run --project Tests/Tests.fsproj
```

## Licence

MIT.
