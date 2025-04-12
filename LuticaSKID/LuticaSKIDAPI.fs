namespace LuticaSKID
open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction
open System.Runtime.InteropServices
open System.Collections.Generic

type ImageProcessCommand =
    | GenerateNormalMap of obj
    | GenerateMatcapMap of obj



[<ComVisible(true)>]
type LuticaSKIDAPI () =

    member this.Process(cmd: ImageProcessCommand) : SKIDImage =
        match cmd with
        | GenerateNormalMap(boxedInput) ->
            match boxedInput with
            | :? ImageProcessInput<NormalModule.NormalMapConfig> as input ->
                NormalModule.generateNormalMap input
            | _ -> failwith "Invalid input for GenerateNormalMap"

        | GenerateMatcapMap(boxedInput) ->
            match boxedInput with
            | :? ImageProcessInput<MatcapModule.MatcapConfig> as input ->
                MatcapModule.generateMatcapMap input
            | _ -> failwith "Invalid input for GenerateMatcapMap"
    
