namespace LuticaSKID

open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction

module NormalModule =
    type NormalMapConfig = { xFactor:float32;yFactor:float32 }
    
    /// Normal map 생성: 입력은 ImageProcessInput, 출력은 SKIDImage
    let generateNormalMap (input: ImageProcessInput<NormalMapConfig>) : SKIDImage =
        let xFactor = input.config.Value.xFactor;
        let yFactor = input.config.Value.yFactor;

        let width = input.image.width
        let height = input.image.height
        let src = input.image.pixels
        let result = Array.zeroCreate<SKIDColor> (width * height)

        let getPixel (x: int) (y: int) =
            src.[y * width + x]

        let setPixel (x: int) (y: int) (color: SKIDColor) =
            result.[y * width + x] <- color

        for x in 1 .. width - 2 do
            for y in 1 .. height - 2 do
                let center = computeGrayscale (getPixel x y)
                let left   = computeGrayscale (getPixel (x - 1) y)
                let right  = computeGrayscale (getPixel (x + 1) y)
                let top    = computeGrayscale (getPixel x (y + 1))
                let bottom = computeGrayscale (getPixel x (y - 1))

                let dx = (right - left) * xFactor
                let dy = (top - bottom) * yFactor
                let dz = 1.0f

                let (nx, ny, nz) = normalize(-dx, -dy, dz)

                let r = clampColorComponent ((nx * 0.5f) + 0.5f)
                let g = clampColorComponent ((ny * 0.5f) + 0.5f)
                let b = clampColorComponent ((nz * 0.5f) + 0.5f)

                setPixel x y (SKIDColor(r, g, b, 1.0f))

        SKIDImage(result, width, height)
