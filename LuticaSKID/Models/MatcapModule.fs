namespace LuticaSKID

open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction

module MatcapModule =

    /// Matcap map 생성: 입력은 ImageProcessInput, 출력은 SKIDImage
    let generateMatcapMap (input: ImageProcessInput) : SKIDImage =
        let xFactor =
            match input.config.TryFind "xFactor" with
            | Some(:? float32 as f) -> f
            | _ -> 1.0f

        let yFactor =
            match input.config.TryFind "yFactor" with
            | Some(:? float32 as f) -> f
            | _ -> 1.0f

        let width = input.image.width
        let height = input.image.height
        let pixels2D = Array2D.init width height (fun x y -> input.image.pixels.[x + y * width])
        let result = Array2D.zeroCreate<SKIDColor> width height

        for x in 0 .. width - 1 do
            for y in 0 .. height - 1 do
                //let center = computeGrayscale pixels2D.[x, y]
                let left   = computeGrayscale (safeGet pixels2D (x - 1) y)
                let right  = computeGrayscale (safeGet pixels2D (x + 1) y)
                let top    = computeGrayscale (safeGet pixels2D x (y + 1))
                let bottom = computeGrayscale (safeGet pixels2D x (y - 1))

                let dx = (right - left) * xFactor
                let dy = (top - bottom) * yFactor
                let dz = 1.0f

                let (nx, ny, nz) = normalize (-dx, -dy, dz)

                let u = clamp01 (nx * 0.5f + 0.5f)
                let v = clamp01 (ny * 0.5f + 0.5f)
                let z = clamp01 (nz * 0.5f + 0.5f)

                result.[x, y] <- SKIDColor(u, v, z, pixels2D.[x, y].a)

        let flat = Array.init (width * height) (fun i -> 
            let x = i % width
            let y = i / width
            result.[x, y]
        )

        SKIDImage(flat, width, height)