namespace LuticaSKID

open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction
open System.Runtime.InteropServices
open System.Collections.Generic

[<ComVisible(true)>]
type ImagePipeline() =

    static member GenerateNormalMap
        (src: SKIDColor[], width: int, height: int, xFactor: float32, yFactor: float32) : SKIDColor[] =
        
        let image = SKIDImage(src, width, height)
        let config: NormalModule.NormalMapConfig option = 
            Some { xFactor = xFactor; yFactor = yFactor }
        let input = ImageProcessInput<NormalModule.NormalMapConfig>(image,  config)
        let resultImage = NormalModule.generateNormalMap input
        resultImage.pixels

    static member GenerateMatcapMap
        (src: SKIDColor[], width: int, height: int, xFactor: float32, yFactor: float32) : SKIDColor[] =
        
        let image = SKIDImage(src, width, height)
        let config:MatcapModule.MatcapConfig option = 
            Some { DetailLevel = 1; xFactor = xFactor; yFactor = yFactor }
        let input = ImageProcessInput<MatcapModule.MatcapConfig>(image, config)
        let resultImage = MatcapModule.generateMatcapMap input
        resultImage.pixels