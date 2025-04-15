namespace LuticaSKID
open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction
open System.Runtime.InteropServices
open System.Collections.Generic

type ImageProcessCommand =
    | GenerateNormalMap of ImageProcessInput<NormalModule.NormalMapConfig>
    | GenerateMatcapMap of ImageProcessInput<MatcapModule.MatcapConfig>
    | GenerateNormalMapFromUV of ImageProcessInput<NormalModule.UVNormalMapMakeConfig>



[<ComVisible(true)>]
type LuticaSKIDAPI () =

    member this.Process(cmd: ImageProcessCommand) : SKIDImage =
        match cmd with
        | GenerateNormalMap(input) ->
            NormalModule.generateNormalMap input

        | GenerateMatcapMap(input) ->
            MatcapModule.generateMatcapMap input

        | GenerateNormalMapFromUV(input) ->
            NormalModule.generateNormalMapFromFBX input
    
