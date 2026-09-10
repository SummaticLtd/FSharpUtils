# FSUtils

General-purpose F# utilities, extracted from the [Summatic](https://summatic.co.uk) app. Published as `Summatic.FSUtils`; the namespace is `FSUtils`.

Everything here is allocation-conscious, trimming-friendly and AOT-friendly: no reflection, `ValueOption` rather than `Option`, `ImmutableArray` rather than `list`, and no `sprintf`.

## Contents

| Module | What it gives you |
| --- | --- |
| `ImmArray` | The `ImmutableArray` module FSharp.Core doesn't ship: 58 functions - `map`, `chooseV`, `groupBy`, `mapFold`, `partition` and the rest - `InlineIfLambda` and builder-based throughout. Lookups return `voption`. |
| `ImmA2D`, `A2D` | An immutable 2D array with structural equality, backed by a row-major `ImmutableArray`, and 0-based `Array2D` replacements. |
| `Equals`, `Hash`, `Compare` | Helpers for implementing `IEquatable<'T>` and `IComparable<'T>` over arrays, 2D arrays and tuples, instead of relying on F# structural equality. |
| `Result`, `ValueOption`, `Task`, `Async`, `Tuple` | The missing combinators, notably `Result.ofImmArrayMap` and `ValueOption.ofImmArrayMap`, which short-circuit. |
| `Instant` | A struct UTC timestamp. Non-UTC input is rejected rather than silently reinterpreted. |
| `Parse` | `voption`-returning, invariant-culture parsers for `int`, `float`, `Guid`, `Uri` and `Complex`. |
| `Dictionary`, `ImmutableDictionary` | `tryFind` returning `voption`, and `addOrReplace`. |
| `Json` | `Result`-returning `System.Text.Json` accessors that check the value kind instead of throwing. |
| `Measure` | Conversions that keep units of measure attached, so `float`/`float32` casts cannot silently drop them. |
| `Seq`, `Array` | `distinctPhysical` (distinct by reference rather than by an overridden `Equals`), `countWhere` and `maxWithSafe`. |
| `NonGenericWorkaround` | Type-checked `Equals`/`CompareTo` for the non-generic overrides, which F# otherwise routes through structural equality ([dotnet/fsharp#9398](https://github.com/dotnet/fsharp/issues/9398)). |
| `SimpleLazy` | `Lazy<'T>` without the trimming warnings. |
| `Builders` | `vmaybe` and `result` computation expressions. |
| `SimpleCD`, `SerialDisposable`, `CancellationScope` | Composite disposables. `SimpleCD` releases in reverse order, reports misuse, and raises an `AggregateException` carrying any failures once everything is released; `SerialDisposable` holds one value at a time, releasing the last when it is replaced; `CancellationScope` cancels a `CancellationToken` when disposed. |
| `ISignal`, `Mutable`, `Signal` | A change-notification primitive: `map` through `map5`, `bind`, and `dispMap`, which gives every value its own `SimpleCD` so per-value resources are released when the next arrives. |
| `Log`, `ISLogger` | A static logging facade over a sink the host installs with `Log.Set`. |
| `withLock` | `lock` over `System.Threading.Lock`, engaging its fast path ([dotnet/fsharp#17287](https://github.com/dotnet/fsharp/issues/17287)). |

## Requirements

.NET 10. The library is built with F# nullable reference types enabled, but consumers need not enable them.

## Notes

Opening `FSUtils` brings modules named `Seq`, `Array`, `Result`, `ValueOption`, `Task` and `Async` into scope, which augment the FSharp.Core modules of the same name. It also auto-opens extensions to `Guid` and `ImmutableArray`.

Until a host calls `Log.Set`, logging goes to the default sink, which writes to `Debug` and breaks into the debugger. That is deliberate - `SimpleCD` reports disposal mistakes through it - but it means a host should install its own sink early.

## Tests

```
dotnet run --project Tests/Tests.fsproj
```

## Licence

MIT.
