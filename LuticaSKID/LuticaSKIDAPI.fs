namespace LuticaSKID
open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction
open System.Runtime.InteropServices
open System.Collections.Generic

[<ComVisible(true)>]
type LuticaSKIDAPI () =
    member this.Process(cmd: ImageProcessCommand) : SKIDImage =
        match cmd with
        | GenerateNormalMap(input) -> NormalModule.generateNormalMap input
        | GenerateMatcapMap(input) -> MatcapModule.generateMatcapMap input
        | GenerateNormalMapFromUV(input) -> NormalModule.generateNormalMapFromFBX input
        | GenerateAvgTexture(input) -> ColorMath.applyMoodColor input
        | ProcessImage(input) -> Models.TextureImageProcessing.Processer.Process input
