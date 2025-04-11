namespace LuticaSKID

open StructTypes
open SKIDToolFunction
module ColorMath =
    let clampColorComponent (v: float32) = SKIDColor.FilteringNotVaildColorNumber v

    let averageColor (pixels: SKIDColor[]) =
        let mutable r, g, b, a, n = 0.0f, 0.0f, 0.0f, 0.0f, 0
        for c in pixels do
            let isTooWhite = c.r >= SKIDConstants.MaxValue &&
                             c.g >= SKIDConstants.MaxValue &&
                             c.b >= SKIDConstants.MaxValue
            let isTooTransparent = c.a <= SKIDConstants.MinValue

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
        SKIDColor(
            clampColorComponent (color.r + diff.r),
            clampColorComponent (color.g + diff.g),
            clampColorComponent (color.b + diff.b),
            clampColorComponent (color.a + diff.a)
        )

    let applyMapping (source: SKIDColor[]) (target: SKIDColor[]) =
        let avgS = averageColor source
        let avgT = averageColor target
        let diff = SKIDColor(
            avgT.r - avgS.r,
            avgT.g - avgS.g,
            avgT.b - avgS.b,
            avgT.a - avgS.a
        )
        source |> Array.map (fun c -> shiftColor c diff)

    let getMoodColor (source: SKIDColor[]) (k: int) =
        let domColor = ColorClustering.getDominantColor source {
            ClusterCount = k
            Iterations = 10
        }
        let diff = SKIDColor(domColor.r - 0.5f, domColor.g - 0.5f, domColor.b - 0.5f, 0.0f)
        source |> Array.map (fun c -> shiftColor c diff)


    // 워크플로우용 함수
    let applyMoodColor (input: ImageProcessInput) : SKIDImage =
        let pixels = input.image.pixels
        let k =
            match input.config.TryFind "clusterCount" with
            | Some (:? int as v) -> v
            | _ -> 5
        let shifted = getMoodColor pixels k
        SKIDImage(shifted, input.image.width, input.image.height)