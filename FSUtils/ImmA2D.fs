namespace FSUtils

open System
open System.Collections.Immutable
open System.Collections.Generic

[<AutoOpen>]
module private Helpers =
    // row-major order
    let inline getRow(cols: int, ind: int) : int = ind / cols
    let inline getCol(cols: int, ind: int) : int = ind % cols
    let inline getInd(cols: int, row: int, col: int) : int = row * cols + col

    let checkInRange(rows: int, cols: int, row: int, col: int) =
        if row < 0 || row >= rows then
            raise (IndexOutOfRangeException("Row index " + row.ToString() + " is out of bounds."))
        if col < 0 || col >= cols then
            raise (IndexOutOfRangeException("Column index " + col.ToString() + " is out of bounds."))

/// 0-based Array2D functions, to avoid trimming issues. https://github.com/fsharp/fslang-suggestions/issues/1454
module A2D =
    let init<'T> (rows: int) (cols: int) (f: int -> int -> 'T) : 'T[,] =
        let arr = Array2D.zeroCreate<'T> rows cols
        for r = 0 to rows - 1 do
            for c = 0 to cols - 1 do
                arr.[r, c] <- f r c
        arr
    let create<'T> (rows: int) (cols: int) (value: 'T) : 'T[,] =
        let arr = Array2D.zeroCreate<'T> rows cols
        for r = 0 to rows - 1 do
            for c = 0 to cols - 1 do
                arr.[r, c] <- value
        arr
    let map<'T, 'U> (f: 'T -> 'U) (arr: 'T[,]) : 'U[,] =
        let rows = arr.GetLength(0)
        let cols = arr.GetLength(1)
        init rows cols (fun r c -> f arr.[r, c])

type ImmA2D<'T when 'T: equality>(rows: int, cols: int, elements: ImmutableArray<'T>) =
    // Unlike Equals.immArray, which assumes IEquatable<'T>, this one works for 'a when 'a: equality.
    // This is used to create something similar to F#'s existing structural Array2D equality.
    static let immArrayEquals(ia1:ImmutableArray<'a>, ia2:ImmutableArray<'a>) =
        if ia1.Length <> ia2.Length then false
        else
            let mutable equals = true
            for i = 0 to ia1.Length - 1 do
                if equals then
                    equals <- ia1.[i] = ia2.[i]
            equals
    member _.Rows = rows
    member _.Cols = cols
    /// Elements, in row-major order
    member _.Elements = elements
    member _.Item(row: int, col: int) : 'T =
        checkInRange(rows, cols, row, col)
        elements.[getInd(cols, row, col)]
    interface IEquatable<ImmA2D<'T>> with
        member _.Equals(Unchecked.NonNullQuick other) =
            rows = other.Rows &&
            cols = other.Cols &&
            elements.Length = other.Elements.Length &&
            immArrayEquals(elements, other.Elements)
    override t.Equals(y: obj) = NonGenericWorkaround.equals<ImmA2D<'T>>(t, y)
    override a2D.GetHashCode() =
        let h = HashCode()
        let l0, l1 = a2D.Rows, a2D.Cols
        let start0 = if l0 >= 3 then l0 - 3 else 0
        let start1 = if l1 >= 3 then l1 - 3 else 0
        for r = start0 to l0 - 1 do
            for c = start1 to l1 - 1 do
                h.Add(a2D.[r, c])
        h.ToHashCode()

module ImmA2D =
    let empty<'T when 'T: equality> : ImmA2D<'T> = ImmA2D(0, 0, ImmArray.empty)
    let init<'T when 'T: equality> (rows: int) (cols: int) (f: int -> int -> 'T) : ImmA2D<'T> =
        let elements = ImmArray.init (rows * cols) (fun i -> f(getRow(cols, i)) (getCol(cols, i)))
        ImmA2D(rows, cols, elements)
    let create<'T when 'T: equality> (rows: int) (cols: int) (value: 'T) : ImmA2D<'T> =
        let elements = ImmArray.init (rows * cols) (fun _ -> value)
        ImmA2D(rows, cols, elements)
    let map<'T, 'U when 'T: equality and 'U: equality> (f: 'T -> 'U) (arr: ImmA2D<'T>) : ImmA2D<'U> =
        let elements = ImmArray.init (arr.Rows * arr.Cols) (fun i -> f arr.Elements.[i])
        ImmA2D(arr.Rows, arr.Cols, elements)
    let mapi<'T, 'U when 'T: equality and 'U: equality> (f: int -> int -> 'T -> 'U) (arr: ImmA2D<'T>) : ImmA2D<'U> =
        let rows, cols = arr.Rows, arr.Cols
        let elements = ImmArray.init (rows * cols) (fun i -> f(getRow(cols, i)) (getCol(cols, i)) arr.Elements.[i])
        ImmA2D(arr.Rows, arr.Cols, elements)
    let iter<'T when 'T: equality> (action: 'T -> unit) (arr: ImmA2D<'T>) : unit =
        arr.Elements |> ImmArray.iter action
    let iteri<'T when 'T: equality> (action: int -> int -> 'T -> unit) (arr: ImmA2D<'T>) : unit =
        let cols = arr.Cols
        arr.Elements |> ImmArray.iteri(fun i v -> action (getRow(cols, i)) (getCol(cols, i)) v)
    // this is a version assuming a row-major backing immarray.
    let fromJagged(rows: seq<#seq<'T>>) =
        let rowsArr = rows.ToImmutableArray()
        let nRows = rowsArr.Length
        if nRows = 0 then
            ImmA2D(0, 0, ImmutableArray<'T>.Empty)
        else
            let firstRowArr = rowsArr.[0].ToImmutableArray()
            let nCols = firstRowArr.Length
            let b = ImmutableArray.CreateBuilder<'T>(nRows * nCols)
            b.AddRange(firstRowArr)
            for r = 1 to nRows - 1 do
                let bCnt = b.Count
                b.AddRange(rowsArr.[r])
                if b.Count - bCnt <> nCols then failwith "All rows must have the same number of columns."
            ImmA2D(nRows, nCols, b.MoveToImmutable())
    let fromJaggedWithKnownCols (nCols: int) (rows: seq<#seq<'T>>) =
        let rowsArr = rows.ToImmutableArray()
        let nRows = rowsArr.Length
        let b = ImmutableArray.CreateBuilder<'T>(nRows * nCols)
        for r = 0 to nRows - 1 do
            let bCnt = b.Count
            b.AddRange(rowsArr.[r])
            if b.Count - bCnt <> nCols then
                failwith "All rows must have the required number of columns."
        ImmA2D(nRows, nCols, b.MoveToImmutable())
    let singleRow<'T when 'T: equality> (row: IReadOnlyCollection<'T>) : ImmA2D<'T> =
        let rowArr = row.ToImmutableArray()
        ImmA2D(1, rowArr.Length, rowArr)
    let singleCol<'T when 'T: equality> (col: IReadOnlyCollection<'T>) : ImmA2D<'T> =
        let colArr = col.ToImmutableArray()
        ImmA2D(colArr.Length, 1, colArr)
    let exists<'T when 'T: equality> (predicate: 'T -> bool) (arr: ImmA2D<'T>) : bool =
        arr.Elements |> ImmArray.exists predicate
    let forall<'T when 'T: equality> (predicate: 'T -> bool) (arr: ImmA2D<'T>) : bool =
        arr.Elements |> ImmArray.forall predicate
    let toVOpt(opts:ImmA2D<'a voption>) =
        if opts |> forall ValueOption.isSome
        then opts |> map ValueOption.get |> ValueSome
        else ValueNone
    let toArray2D<'T when 'T: equality> (arr: ImmA2D<'T>) : 'T[,] =
        let result = Array2D.zeroCreate<'T> arr.Rows arr.Cols
        for r = 0 to arr.Rows - 1 do
            for c = 0 to arr.Cols - 1 do
                result.[r, c] <- arr.[r, c]
        result
    let fromArray2D<'T when 'T: equality> (arr: 'T[,]) : ImmA2D<'T> =
        let rows = arr.GetLength(0)
        let cols = arr.GetLength(1)
        init rows cols (fun r c -> arr.[r, c])

    let fromRow2s<'T when 'T: equality>(row2s: seq<struct('T * 'T)>) : ImmA2D<'T> =
        let row2sArr = row2s.ToImmutableArray()
        let nRows = row2sArr.Length
        let b = ImmutableArray.CreateBuilder<'T>(nRows * 2)
        for r = 0 to nRows - 1 do
            let struct(c0, c1) = row2sArr.[r]
            b.Add(c0); b.Add(c1)
        ImmA2D(nRows, 2, b.MoveToImmutable())
    let fromRow3s<'T when 'T: equality>(row3s: seq<struct('T * 'T * 'T)>) : ImmA2D<'T> =
        let row3sArr = row3s.ToImmutableArray()
        let nRows = row3sArr.Length
        let b = ImmutableArray.CreateBuilder<'T>(nRows * 3)
        for r = 0 to nRows - 1 do
            let struct(c0, c1, c2) = row3sArr.[r]
            b.Add(c0); b.Add(c1); b.Add(c2)
        ImmA2D(nRows, 3, b.MoveToImmutable())
    let fromRow4s<'T when 'T: equality>(row4s: seq<struct('T * 'T * 'T * 'T)>) : ImmA2D<'T> =
        let row4sArr = row4s.ToImmutableArray()
        let nRows = row4sArr.Length
        let b = ImmutableArray.CreateBuilder<'T>(nRows * 4)
        for r = 0 to nRows - 1 do
            let struct(c0, c1, c2, c3) = row4sArr.[r]
            b.Add(c0); b.Add(c1); b.Add(c2); b.Add(c3)
        ImmA2D(nRows, 4, b.MoveToImmutable())
    let fromRow5s<'T when 'T: equality>(row5s: seq<struct('T * 'T * 'T * 'T * 'T)>) : ImmA2D<'T> =
        let row5sArr = row5s.ToImmutableArray()
        let nRows = row5sArr.Length
        let b = ImmutableArray.CreateBuilder<'T>(nRows * 5)
        for r = 0 to nRows - 1 do
            let struct(c0, c1, c2, c3, c4) = row5sArr.[r]
            b.Add(c0); b.Add(c1); b.Add(c2); b.Add(c3); b.Add(c4)
        ImmA2D(nRows, 5, b.MoveToImmutable())
    let fromRow6s<'T when 'T: equality>(row6s: seq<struct('T * 'T * 'T * 'T * 'T * 'T)>) : ImmA2D<'T> =
        let row6sArr = row6s.ToImmutableArray()
        let nRows = row6sArr.Length
        let b = ImmutableArray.CreateBuilder<'T>(nRows * 6)
        for r = 0 to nRows - 1 do
            let struct(c0, c1, c2, c3, c4, c5) = row6sArr.[r]
            b.Add(c0); b.Add(c1); b.Add(c2); b.Add(c3); b.Add(c4); b.Add(c5)
        ImmA2D(nRows, 6, b.MoveToImmutable())
    let fromRow7s<'T when 'T: equality>(row7s: seq<struct('T * 'T * 'T * 'T * 'T * 'T * 'T)>) : ImmA2D<'T> =
        let row7sArr = row7s.ToImmutableArray()
        let nRows = row7sArr.Length
        let b = ImmutableArray.CreateBuilder<'T>(nRows * 7)
        for r = 0 to nRows - 1 do
            let struct(c0, c1, c2, c3, c4, c5, c6) = row7sArr.[r]
            b.Add(c0); b.Add(c1); b.Add(c2); b.Add(c3); b.Add(c4); b.Add(c5); b.Add(c6)
        ImmA2D(nRows, 7, b.MoveToImmutable())
    let fromRow8s<'T when 'T: equality>(row8s: seq<struct('T * 'T * 'T * 'T * 'T * 'T * 'T * 'T)>) : ImmA2D<'T> =
        let row8sArr = row8s.ToImmutableArray()
        let nRows = row8sArr.Length
        let b = ImmutableArray.CreateBuilder<'T>(nRows * 8)
        for r = 0 to nRows - 1 do
            let struct(c0, c1, c2, c3, c4, c5, c6, c7) = row8sArr.[r]
            b.Add(c0); b.Add(c1); b.Add(c2); b.Add(c3); b.Add(c4); b.Add(c5); b.Add(c6); b.Add(c7)
        ImmA2D(nRows, 8, b.MoveToImmutable())
    /// Returns Ok(the Ok values of the mapped ImmA2D) if all outputs are Ok, else the first Error. Short-circuits if an Error is encountered.
    let mapResult<'a, 'Ok, 'Error when 'a: equality and 'Ok: equality> (f: 'a -> Result<'Ok, 'Error>) (l:ImmA2D<'a>) =
        Result.ofImmArrayMap f l.Elements
        |> Result.map (fun elements -> ImmA2D(l.Rows, l.Cols, elements))