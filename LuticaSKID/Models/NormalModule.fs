namespace LuticaSKID
open LuticaSKID.StructTypes
module NormalModule =
    let computeGrayscale (color: SKIDColor) : float32 =
        0.299f * color.r + 0.587f * color.g + 0.114f * color.b

    let normalize (x: float32, y: float32, z: float32) : (float32 * float32 * float32) =
        let len = sqrt (x * x + y * y + z * z)
        if len = 0.0f then (0.0f, 0.0f, 0.0f) else (x / len, y / len, z / len)

    let clampColorComponent (v: float32) = SKIDColor.FilteringNotVaildColorNumber v

    let generateNormalMap (xFactor:float32,yFactor:float32) (heightMap: SKIDColor[,]) : SKIDColor[,] =
        let width = heightMap.GetLength(0)
        let height = heightMap.GetLength(1)
        let result = Array2D.zeroCreate<SKIDColor> width height

        for x in 1 .. width - 2 do
            for y in 1 .. height - 2 do
                let center = computeGrayscale heightMap.[x, y]
                let left   = computeGrayscale heightMap.[x - 1, y]
                let right  = computeGrayscale heightMap.[x + 1, y]
                let top    = computeGrayscale heightMap.[x, y + 1]   // Unity 기준: 위쪽은 +Y
                let bottom = computeGrayscale heightMap.[x, y - 1]

                let dx = (right - left) * xFactor
                let dy = (top - bottom) * yFactor   // Unity는 위가 +Y
                let dz = 1.0f

                let (nx, ny, nz) = normalize(-dx, -dy, dz)

                // 0~1 범위로 매핑
                let r = clampColorComponent ((nx * 0.5f) + 0.5f) // X → R
                let g = clampColorComponent ((ny * 0.5f) + 0.5f) // Y → G
                let b = clampColorComponent ((nz * 0.5f) + 0.5f) // Z → B

                result.[x, y] <- SKIDColor(r, g, b, 1.0f)
        
        result


