﻿namespace LuticaSKID
open StructTypes;

module ColorMath =
    let averageColor (pixels: SKIDColor[]) =
        let mutable r, g, b, a, n = 0.0f, 0.0f, 0.0f, 0.0f, 0
        for c in pixels do
            let isTooWhite = c.r >= SKIDConstants.WhiteMax &&
                             c.g >= SKIDConstants.WhiteMax &&
                             c.b >= SKIDConstants.WhiteMax
            let isTooTransparent = c.a <= SKIDConstants.MinAlpha

            if not isTooWhite && not isTooTransparent then
                n <- n + 1
                r <- r + c.r
                g <- g + c.g
                b <- b + c.b
                a <- a + c.a

        if n = 0 then
            SKIDColor(1.0f, 1.0f, 1.0f, 1.0f)
        else
            let n = float32 n
            SKIDColor(r / n, g / n, b / n, a / n)

    let shiftColor (color: SKIDColor) (diff: SKIDColor) =
        let clamp (v: float32) = min 1.0f (max 0.0f v)
        SKIDColor(
            clamp (color.r + diff.r),
            clamp (color.g + diff.g),
            clamp (color.b + diff.b),
            clamp (color.a + diff.a)
        )

    let applyMapping (source: SKIDColor[]) (target: SKIDColor[]) =
        let avgS = averageColor source
        let avgT = averageColor target
        let diff =
            SKIDColor(
                avgT.r - avgS.r,
                avgT.g - avgS.g,
                avgT.b - avgS.b,
                avgT.a - avgS.a
            )
        source |> Array.map (fun c -> shiftColor c diff)

