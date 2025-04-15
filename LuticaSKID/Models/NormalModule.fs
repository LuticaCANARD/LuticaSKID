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

        for x in 0 .. width - 1 do
            for y in 0 .. height - 1 do
                let center = computeGrayscale (getPixel x y)
                let left   = computeGrayscale (getPixel (max 0 (x - 1)) y)
                let right  = computeGrayscale (getPixel (min (width - 1) (x + 1)) y)
                let top    = computeGrayscale (getPixel x (min (height - 1) (y + 1)))
                let bottom = computeGrayscale (getPixel x (max 0 (y - 1)))

                let dx = (right - left) * xFactor
                let dy = (top - bottom) * yFactor
                let dz = 1.0f

                let (nx, ny, nz) = normalize(-dx, -dy, dz)

                let r = clampColorComponent ((nx * 0.5f) + 0.5f)
                let g = clampColorComponent ((ny * 0.5f) + 0.5f)
                let b = clampColorComponent ((nz * 0.5f) + 0.5f)

                setPixel x y (SKIDColor(r, g, b, 1.0f))

        SKIDImage(result, width, height)

    let computeNormalFromUV
        (uvs: SKIDVector2[])
        (positions: SKIDVector3[])
        (triangles: int[])
        : SKIDVector3[] =

        let normals = Array.create uvs.Length (SKIDVector3(0.0f, 0.0f, 0.0f))
        let counts = Array.create uvs.Length 0

        let cross (a: SKIDVector3) (b: SKIDVector3) =
            SKIDVector3(
                a.y * b.z - a.z * b.y,
                a.z * b.x - a.x * b.z,
                a.x * b.y - a.y * b.x
            )

        let inline normalizeVec3 (v: SKIDVector3) =
            let len = sqrt (v.x * v.x + v.y * v.y + v.z * v.z)
            if len = 0.0f then SKIDVector3(0.0f, 0.0f, 0.0f)
            else SKIDVector3(v.x / len, v.y / len, v.z / len)

        // 각 삼각형에 대해 normal 계산
        for i in 0 .. 3 .. triangles.Length - 3 do
            let i0 = triangles.[i]
            let i1 = triangles.[i + 1]
            let i2 = triangles.[i + 2]

            let p0 = positions.[i0]
            let p1 = positions.[i1]
            let p2 = positions.[i2]

            let uv0 = uvs.[i0]
            let uv1 = uvs.[i1]
            let uv2 = uvs.[i2]

            let dp1 = SKIDVector3(p1.x - p0.x, p1.y - p0.y, p1.z - p0.z)
            let dp2 = SKIDVector3(p2.x - p0.x, p2.y - p0.y, p2.z - p0.z)

            let duv1 = SKIDVector2(uv1.x - uv0.x, uv1.y - uv0.y)
            let duv2 = SKIDVector2(uv2.x - uv0.x, uv2.y - uv0.y)

            let r = 1.0f / (duv1.x * duv2.y - duv1.y * duv2.x + 1e-8f) // 방지

            let tangent = SKIDVector3(
                (dp1.x * duv2.y - dp2.x * duv1.y) * r,
                (dp1.y * duv2.y - dp2.y * duv1.y) * r,
                (dp1.z * duv2.y - dp2.z * duv1.y) * r
            )

            let bitangent = SKIDVector3(
                (dp2.x * duv1.x - dp1.x * duv2.x) * r,
                (dp2.y * duv1.x - dp1.y * duv2.x) * r,
                (dp2.z * duv1.x - dp1.z * duv2.x) * r
            )

            let n = normalizeVec3 (cross tangent bitangent)

            for idx in [i0; i1; i2] do
                normals.[idx] <- SKIDVector3(
                    normals.[idx].x + n.x,
                    normals.[idx].y + n.y,
                    normals.[idx].z + n.z
                )
                counts.[idx] <- counts.[idx] + 1

        // 평균화
        Array.mapi (fun i (n: SKIDVector3) ->
            if counts.[i] > 0 then
                let scaled = SKIDVector3(n.x / float32 counts.[i], n.y / float32 counts.[i], n.z / float32 counts.[i])
                normalizeVec3 scaled
            else SKIDVector3(0.0f, 0.0f, 1.0f) // fallback
        ) normals

    type UVNormalMapMakeConfig = { 
        UVs: SKIDVector2[]
        Normals: SKIDVector3[]
        Triangles: int[]
    }
    let generateNormalMapFromUV (input: ImageProcessInput<UVNormalMapMakeConfig>) : SKIDImage =
        let width, height = input.image.width, input.image.height
        let src, result = input.image.pixels, Array.zeroCreate<SKIDColor> (width * height)
        let normals = computeNormalFromUV input.config.Value.UVs input.config.Value.Normals input.config.Value.Triangles

        let getPixel x y = src.[y * width + x]
        SKIDImage(result, width, height)

    let generateNormalMapFromFBX (input: ImageProcessInput<UVNormalMapMakeConfig>) : SKIDImage =
        let width, height = input.image.width, input.image.height
        let src, result = input.image.pixels, Array.zeroCreate<SKIDColor> (width * height)
        let uvs, normals, triangles = input.config.Value.UVs, input.config.Value.Normals, input.config.Value.Triangles

        let setPixel x y color = result.[y * width + x] <- color

        for i in 0 .. 3 .. triangles.Length - 3 do
            let i0, i1, i2 = triangles.[i], triangles.[i + 1], triangles.[i + 2]
            let uv0, uv1, uv2 = uvs.[i0], uvs.[i1], uvs.[i2]
            let n0, n1, n2 = normals.[i0], normals.[i1], normals.[i2]

            for x in 0 .. width - 1 do
                for y in 0 .. height - 1 do
                    let px, py = float32 x / float32 (width - 1), 1.0f - (float32 y / float32 (height - 1))
                    let p = SKIDVector2(px, py)

                    let w0 = ((uv1.y - uv2.y) * (p.x - uv2.x) + (uv2.x - uv1.x) * (p.y - uv2.y)) / ((uv1.y - uv2.y) * (uv0.x - uv2.x) + (uv2.x - uv1.x) * (uv0.y - uv2.y))
                    let w1 = ((uv2.y - uv0.y) * (p.x - uv2.x) + (uv0.x - uv2.x) * (p.y - uv2.y)) / ((uv1.y - uv2.y) * (uv0.x - uv2.x) + (uv2.x - uv1.x) * (uv0.y - uv2.y))
                    let w2 = 1.0f - w0 - w1

                    if w0 >= 0.0f && w1 >= 0.0f && w2 >= 0.0f then
                        let nx, ny, nz = w0 * n0.x + w1 * n1.x + w2 * n2.x, w0 * n0.y + w1 * n1.y + w2 * n2.y, w0 * n0.z + w1 * n1.z + w2 * n2.z
                        let normalized = normalize_SKIDVector3(nx, ny, nz)

                        let r, g, b = clampColorComponent ((normalized.x * 0.5f) + 0.5f), clampColorComponent ((normalized.y * 0.5f) + 0.5f), clampColorComponent ((normalized.z * 0.5f) + 0.5f)
                        setPixel x y (SKIDColor(r, g, b, 1.0f))

        SKIDImage(result, width, height)
