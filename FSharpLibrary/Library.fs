namespace FSharpLibrary

module Say =
    let hello name =
        printfn "Hello %s" name

module Calculator =
    let SimdSquare array =
        array |> Array.SIMD.map (fun x -> x*x) (fun x -> x*x)

