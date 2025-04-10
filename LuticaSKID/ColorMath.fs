namespace LuticaSKID
open StructTypes;

module ColorMath =
    let averageColor (pixels: SKIDColor []) =
        let r, g, b, a = 
            pixels |> Array.fold (fun (r, g, b, a) c -> 
                r + c.r, g + c.g, b + c.b, a + c.a
            ) (0.0f, 0.0f, 0.0f, 0.0f)
        let n = float32 pixels.Length
        { r = r / n; g = g / n; b = b / n; a = a / n }

    let shiftColor (color: SKIDColor ) (diff: SKIDColor ) =
        let clamp (v: float32) = min 1.0f (max 0.0f v)
        { 
            r = clamp (color.r + diff.r)
            g = clamp (color.g + diff.g)
            b = clamp (color.b + diff.b)
            a = clamp (color.a + diff.a)
        }

    let applyMapping (source: SKIDColor []) (target: SKIDColor []) =
        let avgS = averageColor source
        let avgT = averageColor target
        let diff = 
            { r = avgT.r - avgS.r
              g = avgT.g - avgS.g
              b = avgT.b - avgS.b
              a = avgT.a - avgS.a }
        source |> Array.map (fun c -> shiftColor c diff)
