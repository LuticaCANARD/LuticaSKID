namespace LuticaSKID

open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction
open System.Runtime.InteropServices
open System.Collections.Generic

[<ComVisible(true)>]
type ColorTools() =

    static member MapColors (src: SKIDColor[], dst: SKIDColor[]) : SKIDColor[] =
        ColorMath.applyMapping src dst

    static member TakeMoodColorAndMap (src: SKIDColor[], dst: SKIDColor[]) : SKIDColor[] =
        let moodColor = ColorMath.getMoodColor src 5
        ColorMath.applyMapping moodColor dst

    static member GenerateNormalMap
        (src: SKIDColor[], width: int, height: int, xFactor: float32, yFactor: float32) : SKIDColor[] =
        
        let image = SKIDImage(src, width, height)
        let config =
            [ "xFactor", box xFactor
              "yFactor", box yFactor ] |> Map.ofList

        let input = ImageProcessInput(image, config)
        let resultImage = NormalModule.generateNormalMap input
        resultImage.pixels

    static member GenerateMatcapMap
        (src: SKIDColor[], width: int, height: int, xFactor: float32, yFactor: float32) : SKIDColor[] =
        
        let image = SKIDImage(src, width, height)
        let config =
            [ "xFactor", box xFactor
              "yFactor", box yFactor ] |> Map.ofList

        let input = ImageProcessInput(image, config)
        let resultImage = NormalModule.generateNormalMap input
        resultImage.pixels