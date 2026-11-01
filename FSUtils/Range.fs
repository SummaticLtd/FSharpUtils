namespace FSUtils

module Range =
    /// Whether f holds for some i in 0 .. n - 1.
    let inline exists ([<InlineIfLambda>] f: int -> bool) (n: int) =
        let mutable i = 0
        while i < n && not (f i) do
            i <- i + 1
        i < n
    /// Whether f holds for every i in 0 .. n - 1.
    let inline forall ([<InlineIfLambda>] f: int -> bool) (n: int) =
        let mutable i = 0
        while i < n && f i do
            i <- i + 1
        i >= n
