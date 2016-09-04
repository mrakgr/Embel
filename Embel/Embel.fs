namespace Embel
module Main = 
    open System
    open System.IO
    open System.Diagnostics
    open System.Reflection
    open System.Reflection.Emit

    /// Gets the name of the function passed into it.
    let getFuncName f = // From: http://stackoverflow.com/questions/36324339/extract-function-name-from-a-function
       let type' = f.GetType()
       let method' = type'.GetMethods() |> Array.find (fun m -> m.Name="Invoke")

       let il = method'.GetMethodBody().GetILAsByteArray()
       let methodCodes = [byte OpCodes.Call.Value;byte OpCodes.Callvirt.Value]

       let position = il |> Array.findIndex(fun x -> methodCodes |> List.exists ((=)x))
       let metadataToken = BitConverter.ToInt32(il, position+1) 

       let actualMethod = type'.Module.ResolveMethod metadataToken
       actualMethod.Name

    type Result =
        | Success
        | Failure of string

    type Tally =
        {
        mutable succeeded : int
        mutable failed : int
        mutable ignored : int
        mutable am_ignoring : bool
        mutable label : string
        time_elapsed : Stopwatch
        output : StreamWriter
        }

        static member create(output: StreamWriter) =
            {succeeded=0; failed=0; ignored=0; am_ignoring=false; label=""; time_elapsed=Stopwatch.StartNew(); output=output}
        static member create() =
            let x = Console.OpenStandardError() |> fun x -> new StreamWriter(x)
            x.AutoFlush <- true
            Tally.create x

        override t.ToString() =
            [|
            sprintf "Test tally: Succeeded: %i" t.succeeded
            sprintf "            Failed: %i" t.failed
            sprintf "            Ignored: %i" t.ignored
            sprintf "            Time Elapsed: %A" t.time_elapsed.Elapsed
            |] |> String.concat "\n"

        interface IDisposable with
            member t.Dispose() = t.output.Dispose()

    let inline runTestCase (x: ^a) (code: ^a -> Result) (state: Tally) =
        if state.am_ignoring then
            state.ignored <- state.ignored+1
            state
        else
            match code x with
            | Success -> state.succeeded <- state.succeeded+1
            | Failure mes -> 
                let name = getFuncName code
                let sep = if String.IsNullOrEmpty state.label then null else "/"
                let mes = if String.IsNullOrEmpty mes = false then ": " + mes else null
                state.output.WriteLine(sprintf "Test %s%s%s failed%s" state.label sep name mes)
                state.failed <- state.failed+1
            state

    let testLabel (label: string) (test: Tally -> Tally) (state: Tally) =
        let backup = state.label
        state.label <- 
            let x = if String.IsNullOrEmpty backup then "" else "/"
            String.concat null [|backup; x; label|]
        test state |> fun state -> state.label <- backup; state

    /// Tests a single test case
    let inline testCase label (setup: unit -> ^a) (test: ^a -> Result) =
        fun (state: Tally) -> 
            use x = setup () 
            runTestCase x test state
        |> testLabel label

    /// Tests an array of cases with the data from the setup function shared amongst the functions.
    let inline testCases (label: string) (setup: unit -> ^a) (tests: (^a -> Result)[]) =
        fun (state: Tally) ->
            use x = setup ()
            Array.fold(fun state test -> runTestCase x test state) state tests
        |> testLabel label

    /// Tests an array of cases with the data from the setup function instantiated separately for each function.
    let inline testCases' (label: string) (setup: unit -> ^a) (tests: (^a -> Result)[]) =
        fun (state: Tally) -> Array.fold(fun state test -> use x = setup () in runTestCase x test state) state tests
        |> testLabel label

    /// Sequentially runs the array of tests
    let testArray (label: string) (tests: (Tally -> Tally)[]) =
        fun (state: Tally) -> Array.fold(fun state test -> test state) state tests
        |> testLabel label

    /// Ignores the selected tests.
    let testIgnore (test: (Tally -> Tally)) (state: Tally) =
        let backup = state.am_ignoring
        state.am_ignoring <- true
        test state |> fun state -> state.am_ignoring <- backup; state

    /// Turns the result of a boolean into the Result type.
    let assertTest (label: string) =
        function
        | true -> Success
        | false -> Failure label

    /// The main run function
    let run (tree: Tally -> Tally) =
        use tally = Tally.create() |> tree
        tally.ToString()
