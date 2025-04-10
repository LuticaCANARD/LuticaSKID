namespace LuticaSKID

type Color = { r: float32; g: float32; b: float32; a: float32 }

module ColorMath =
    let averageColor (pixels: Color[]) =
        let r, g, b, a =
            pixels |> Array.fold (fun (r, g, b, a) c -> (r + c.r, g + c.g, b + c.b, a + c.a)) (0.f, 0.f, 0.f, 0.f)
        let n = float32 pixels.Length
        { r = r / n; g = g / n; b = b / n; a = a / n }

    let shiftColor (color: Color) (diff: Color) =
        let clamp v = System.Math.Clamp(float v, 0.0, 1.0) |> float32
        {
            r = clamp (color.r + diff.r)
            g = clamp (color.g + diff.g)
            b = clamp (color.b + diff.b)
            a = clamp (color.a + diff.a)
        }

    let applyMapping (source: Color[]) (target: Color[]) =
        let avgS = averageColor source
        let avgT = averageColor target
        let diff =
            { r = avgT.r - avgS.r
              g = avgT.g - avgS.g
              b = avgT.b - avgS.b
              a = avgT.a - avgS.a }

        source |> Array.map (fun c -> shiftColor c diff)