namespace Embel
module Test =
    open System
    open Main

    type Rec =
        {
        x : int
        }

        static member create() = {x=1}

        interface IDisposable with
            member t.Dispose() =
                ()

    let test1 (x: Rec) =
        Failure "Just testing"

    let test2 (x: Rec) =
        Failure "Just..."

    let test3 (x: Rec) =
        Failure "...testing"

    let test4 (x: Rec) = Failure null

    let tree = 
        testArray null
            [|
            testArray "Top of tree"
                [|
                testCases "Main tests" Rec.create
                    [|
                    test1
                    test2
                    test3
                    |]
                testIgnore <| testCases "Aux tests" Rec.create
                    [|
                    test1
                    test2
                    test3
                    |]
                |]
            testCases "Side tests" Rec.create
                [|
                test1
                test1
                test1
                |]
            testCase null Rec.create test4
            |]
    run tree |> printfn "%s"
