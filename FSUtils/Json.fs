namespace FSUtils

open System
open System.Collections.Immutable
open System.Text.Json

module Json =
    /// JsonDocument rents a pooled buffer and must be disposed, and a JsonElement is a view over
    /// that buffer. Clone() detaches the root so it stays valid once the document is gone.
    let private tryParseRoot(parse: unit -> JsonDocument) : Result<JsonElement, string> =
        try
            use doc = parse()
            doc.RootElement.Clone() |> Ok
        with
        | :? JsonException ->
            Error "Invalid JSON format"
    /// Try to parse a JSON string, returning the RootElement of the doc if successful
    let tryParse(json: string) = tryParseRoot(fun () -> JsonDocument.Parse(json))
    /// Try to parse JSON bytes, returning the RootElement of the doc if successful
    let tryParseBytes(json: byte[]) = tryParseRoot(fun () -> JsonDocument.Parse(ReadOnlyMemory json))
    /// Try to parse a JSON stream, returning the RootElement of the doc if successful
    let tryParseStream(stream: IO.Stream) = tryParseRoot(fun () -> JsonDocument.Parse(stream))
    /// TryGetProperty throws on any kind but Object, so the kind is checked rather than assumed.
    let tryGetProp (je: JsonElement) (prop: string) =
        if je.ValueKind <> JsonValueKind.Object then Error ("Cannot read property '" + prop + "': not an object")
        else
            match je.TryGetProperty(prop) with
            | true, value -> Ok value
            | _ -> Error ("Property '" + prop + "' not found")
    /// Try to get a property as a string from a JsonElement
    let tryGetPropStr (je: JsonElement) (prop: string) =
        tryGetProp je prop
        |> Result.bind(fun value ->
            if value.ValueKind = JsonValueKind.String then value.GetString() |> nonNull |> Ok
            else Error ("Property '" + prop + "' is not a string"))
    /// Try to get a property as an int from a JsonElement
    let tryGetPropInt (je: JsonElement) (prop: string) =
        tryGetProp je prop
        |> Result.bind(fun value ->
            if value.ValueKind = JsonValueKind.Number then
                try
                    value.GetInt32() |> Ok
                with _ -> Error ("Property '" + prop + "' is not a valid integer")
            else Error ("Property '" + prop + "' is not a number"))
    let tryGetPropBool (je: JsonElement) (prop: string) =
        tryGetProp je prop
        |> Result.bind(fun value ->
            if value.ValueKind = JsonValueKind.True then true |> Ok
            elif value.ValueKind = JsonValueKind.False then false |> Ok
            else Error ("Property '" + prop + "' is not a bool"))
    let tryGetStringArray (je: JsonElement) =
        match je.ValueKind with
        | JsonValueKind.Array ->
            je.EnumerateArray().ToImmutableArray()
            |> Result.ofImmArrayMap(fun e ->
                match e.ValueKind with
                | JsonValueKind.String -> e.GetString() |> nonNull |> Ok
                | _ -> Error "non-string value"
            )
        | _ -> Error "not an array"
    /// Try to get a JsonElement as an int.
    let tryGetInt(el: JsonElement) : Result<int, string> =
        if el.ValueKind = JsonValueKind.Number then
            let mutable v = 0
            if el.TryGetInt32(&v) then Ok v else Error "Not a valid integer"
        else Error "Not a number"
    /// Try to get a JsonElement as a string.
    let tryGetString(el: JsonElement) : Result<string, string> =
        if el.ValueKind = JsonValueKind.String then el.GetString() |> nonNull |> Ok else Error "Not a string"
    /// Try to get a JsonElement as an array of elements.
    let tryGetArray(el: JsonElement) : Result<ImmutableArray<JsonElement>, string> =
        if el.ValueKind = JsonValueKind.Array then
            let b = ImmutableArray.CreateBuilder<JsonElement>()
            for item in el.EnumerateArray() do
                b.Add item
            Ok(b.ToImmutable())
        else Error "Not an array"

module JsonNode =
    let tryAsObject(node:Nodes.JsonNode) =
        if node.GetValueKind() = System.Text.Json.JsonValueKind.Object then ValueSome(node.AsObject()) else ValueNone
    let tryAsString (node:Nodes.JsonNode) =
        if node.GetValueKind() = System.Text.Json.JsonValueKind.String then ValueSome(node.ToString()) else ValueNone
    let tryAsArray (node:Nodes.JsonNode) =
        if node.GetValueKind() = System.Text.Json.JsonValueKind.Array then ValueSome(node.AsArray().ToImmutableArray()) else ValueNone

    let jobj(props: list<string * Nodes.JsonNode>) : Nodes.JsonNode =
        let o = Nodes.JsonObject()
        for (key, value) in props do o.Add(key, value)
        o
    let arr(items: seq<Nodes.JsonNode>) : Nodes.JsonNode =
        let a = Nodes.JsonArray()
        for item in items do a.Add(item)
        a
    let str(s: string) : Nodes.JsonNode = nonNull(Nodes.JsonValue.Create(s))
    let fl(f: float) : Nodes.JsonNode = Nodes.JsonValue.Create(f)
    let int(i: int64) : Nodes.JsonNode = Nodes.JsonValue.Create(i)
    let int32(i: int) : Nodes.JsonNode = Nodes.JsonValue.Create(i)
    let bool(b: bool) : Nodes.JsonNode = Nodes.JsonValue.Create(b)
    let guid(g: Guid) : Nodes.JsonNode = Nodes.JsonValue.Create(g)
    let dateTime(d: DateTime) : Nodes.JsonNode = Nodes.JsonValue.Create(d)
