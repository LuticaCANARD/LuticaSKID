namespace LuticaSKID.Models
open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction
open ILGPU
open ILGPU.Runtime
open System.Threading.Tasks

module HeightMapModel = 
    
    type HeightMapConfig = { xFactor: float32; yFactor: float32 }

    let generateHeightMap (input: ImageProcessInput<HeightMapConfig>) : SKIDImage =
        let image = input.image
        let config = defaultArg input.config { xFactor = 1.0f; yFactor = 1.0f }
        let width = image.width
        let height = image.height
        let pixels = Array.zeroCreate (width * height)

        Parallel.For(0, height, fun y ->
            for x in 0 .. width - 1 do
                let pixel = image.GetPixel(x, y)
                let heightValue = (((pixel.r + pixel.g + pixel.b) / 3.0f) * config.xFactor + ((pixel.r + pixel.g + pixel.b) / 3.0f) * config.yFactor ) * pixel.a
                let normalizedHeight = min 1.0f (max 0.0f heightValue)
                pixels.[y * width + x] <- SKIDColor(normalizedHeight, normalizedHeight, normalizedHeight, 1.0f)
        ) |> ignore

        SKIDImage(pixels, width, height)
