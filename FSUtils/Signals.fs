namespace FSUtils

open System
open System.Collections.Immutable

/// Core interface for signals
type ISignal<'a> =
    /// The current value of the type
    abstract member Value : 'a with get
    abstract member ValueChanged : IEvent<'a>

/// A lightweight wrapper for a mutable value which provides a mechanism for change notification as needed
/// Anything referenced from keepAlive will have a reference from the Mutable object
[<Sealed>]
type Mutable<'a when 'a:equality> internal (initialValue:'a, keepAlive:ImmutableArray<obj>) =
    let mutable value = initialValue
    let e = Event<'a>()
    let publishedE = e.Publish
    member _.KeepAlive = keepAlive
    member _.Value
        with get() = value
        and set v =
            let oldValue = value
            value <- v
            if oldValue <> v then e.Trigger(v)
    interface ISignal<'a> with
        member _.Value with get() = value
        member _.ValueChanged = publishedE
    static member create<'a when 'a:equality>(init:'a) =
        Mutable(init, ImmutableArray<obj>.Empty)
    member t.AsSignal = t :> ISignal<'a>

[<RequireQualifiedAccess>]
module Signal =
    let subscribe<'a, 'b when 'b :> ISignal<'a>> (f:'a -> unit) (s:'b) =
        let h = Handler<'a>(fun _ x -> f x)
        s.ValueChanged.AddHandler h
        {
            new IDisposable with
                member _.Dispose() =
                    s.ValueChanged.RemoveHandler h
        }
    let map (f:'a->'b) (s:ISignal<'a>) =
        let m = Mutable(f(s.Value), ImmutableArray.Create<obj>(s))
        let wrm = WeakReference<Mutable<'b>>(m)
        let mutable removeHandlers:unit -> unit = id
        let h = Handler<'a>(fun _ x ->
            let mutable m' = Unchecked.defaultof<Mutable<'b>>
            if wrm.TryGetTarget(&m') then
                m'.Value <- f(x)
            else removeHandlers())
        removeHandlers <- (fun () -> s.ValueChanged.RemoveHandler h)
        s.ValueChanged.AddHandler h
        m:>ISignal<'b>
    let map2 (f:'a * 'b -> 'c) (sa:ISignal<'a>, sb:ISignal<'b>) =
        let m = Mutable(f(sa.Value, sb.Value), ImmutableArray.Create<obj>(sa, sb))
        let wrm = WeakReference<Mutable<'c>>(m)
        let mutable removeHandlers:unit -> unit = id
        let update() =
            let mutable m' = Unchecked.defaultof<Mutable<'c>>
            if wrm.TryGetTarget(&m') then
                m'.Value <- f(sa.Value, sb.Value)
            else removeHandlers()
        let da = sa |> subscribe (fun _ -> update())
        let db = sb |> subscribe (fun _ -> update())
        removeHandlers <- (fun () -> da.Dispose(); db.Dispose())
        m:>ISignal<'c>
    let map3 (f:'a0 * 'a1 * 'a2 -> 'b) (s0:ISignal<'a0>, s1:ISignal<'a1>, s2:ISignal<'a2>) =
        let m = Mutable(f(s0.Value, s1.Value, s2.Value), ImmutableArray.Create<obj>(s0, s1, s2))
        let wrm = WeakReference<Mutable<'b>>(m)
        let mutable removeHandlers:unit -> unit = id
        let update() =
            let mutable m' = Unchecked.defaultof<Mutable<'b>>
            if wrm.TryGetTarget(&m') then
                m'.Value <- f(s0.Value, s1.Value, s2.Value)
            else removeHandlers()
        let d0 = s0 |> subscribe (fun _ -> update())
        let d1 = s1 |> subscribe (fun _ -> update())
        let d2 = s2 |> subscribe (fun _ -> update())
        removeHandlers <- (fun () -> d0.Dispose(); d1.Dispose(); d2.Dispose())
        m:>ISignal<'b>
    let map4 (f:'a0 * 'a1 * 'a2 * 'a3 -> 'b) (s0:ISignal<'a0>, s1:ISignal<'a1>, s2:ISignal<'a2>, s3:ISignal<'a3>) =
        let m = Mutable(f(s0.Value, s1.Value, s2.Value, s3.Value), ImmutableArray.Create<obj>(s0, s1, s2, s3))
        let wrm = WeakReference<Mutable<'b>>(m)
        let mutable removeHandlers:unit -> unit = id
        let update() =
            let mutable m' = Unchecked.defaultof<Mutable<'b>>
            if wrm.TryGetTarget(&m') then
                m'.Value <- f(s0.Value, s1.Value, s2.Value, s3.Value)
            else removeHandlers()
        let d0 = s0 |> subscribe (fun _ -> update())
        let d1 = s1 |> subscribe (fun _ -> update())
        let d2 = s2 |> subscribe (fun _ -> update())
        let d3 = s3 |> subscribe (fun _ -> update())
        removeHandlers <- (fun () -> d0.Dispose(); d1.Dispose(); d2.Dispose(); d3.Dispose())
        m:>ISignal<'b>
    let map5 (f:'a0 * 'a1 * 'a2 * 'a3 * 'a4 -> 'b) (s0:ISignal<'a0>, s1:ISignal<'a1>, s2:ISignal<'a2>, s3:ISignal<'a3>, s4:ISignal<'a4>) =
        let m = Mutable(f(s0.Value, s1.Value, s2.Value, s3.Value, s4.Value), ImmutableArray.Create<obj>(s0, s1, s2, s3, s4))
        let wrm = WeakReference<Mutable<'b>>(m)
        let mutable removeHandlers:unit -> unit = id
        let update() =
            let mutable m' = Unchecked.defaultof<Mutable<'b>>
            if wrm.TryGetTarget(&m') then
                m'.Value <- f(s0.Value, s1.Value, s2.Value, s3.Value, s4.Value)
            else removeHandlers()
        let d0 = s0 |> subscribe (fun _ -> update())
        let d1 = s1 |> subscribe (fun _ -> update())
        let d2 = s2 |> subscribe (fun _ -> update())
        let d3 = s3 |> subscribe (fun _ -> update())
        let d4 = s4 |> subscribe (fun _ -> update())
        removeHandlers <- (fun () -> d0.Dispose(); d1.Dispose(); d2.Dispose(); d3.Dispose(); d4.Dispose())
        m:>ISignal<'b>
    /// A mapped signal which handles inner disposals and which is disposed when cd is disposed.
    /// If Map[N] is needed, use Signal.map[N] id first, then dispMap.
    let dispMap<'a, 'b when 'b: equality> (cd:SimpleCD) (f:(SimpleCD * 'a) -> 'b) (s:ISignal<'a>) =
        let sd = new SerialDisposable() |> cd.Adding
        let initSCD = new SimpleCD() |> sd.Switching
        let m = Mutable(f(initSCD, s.Value), ImmutableArray.Create<obj>(s))
        s |> subscribe(fun a ->
            let scd = new SimpleCD() |> sd.Switching
            m.Value <- f(scd, a)) |> cd.Add
        m:>ISignal<'b>
    let constant(x:'a) =
        {   new ISignal<'a> with
                member _.Value = x
                member _.ValueChanged = Event<'a>().Publish
        }

    /// Do f(s) initially and each time s changes
    // Subscribe first, so any events triggered by the first function will be handled.
    // Disposables created for every value of the signal are tracked and disposed when the signal changes or the parent cd is disposed.
    let dispBind<'a> (cd:SimpleCD) (f:(SimpleCD * 'a) -> unit) (s:ISignal<'a>) =
        dispMap cd f s |> ignore

    /// Do f(s) initially and each time s changes
    // Subscribe first, so any events triggered by the first function will be handled
    let bind<'a> (f:'a -> unit) (s:ISignal<'a>) =
        let disp = s |> subscribe f
        f(s.Value)
        disp

    /// Bind using Add. Use where objects in f have lifetime >= that of s, since a reference is created FROM s TO the closure of f.
    /// DANGER - sometimes a signal created with Signal.map won't update when bindUsingAdd is used, but using bind seems to work.
    ///     We don't understand why this happens at the moment, so any use of bindUsingAdd should be thoroughly tested.
    // Add listener first, so that any events triggered during the first action will be handled afterwards
    let bindUsingAdd (f:'a -> unit) (s:ISignal<'a>) =
        s.ValueChanged.Add f
        f(s.Value)

    let mapFromImmArray<'a,'b when 'b:equality> (f:ImmutableArray<'a> -> 'b) (arr:ImmutableArray<ISignal<'a>>) =
        let get() = f(arr |> ImmArray.map(fun s -> s.Value))
        let m = Mutable.create(get())
        let wrm = WeakReference<Mutable<'b>>(m)
        let mutable removeHandlers:unit->unit = id
        let update() =
            let mutable m' = Unchecked.defaultof<Mutable<'b>>
            if wrm.TryGetTarget(&m') then
                m'.Value <- get()
            else removeHandlers()
        let disps = arr |> ImmArray.map(fun s -> s |> subscribe(fun _ -> update()))
        removeHandlers <- (fun () -> disps |> ImmArray.iter(fun d -> d.Dispose()))
        m:>ISignal<'b>

    let CSharpBind<'a>(s: ISignal<'a>, f: Action<'a>, cd: SimpleCD) =
        s |> bind(Microsoft.FSharp.Core.FuncConvert.FromAction<'a>(f)) |> cd.Adding