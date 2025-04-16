namespace LuticaSKID

open StructTypes
open SKIDToolFunction
module ColorMath =
    type ColorMoodOption = {refrenceImage:SKIDImage; iterCount:int;clusterCount : int; addFactor:float32}
    let clampColorComponent (v: float32) = SKIDColor.FilteringNotVaildColorNumber v

    let averageColor (pixels: SKIDColor[]) =
        let validPixels = 
            pixels 
            |> Array.filter (fun c -> 
                not (c.r >= SKIDConstants.MaxValue && 
                        c.g >= SKIDConstants.MaxValue && 
                        c.b >= SKIDConstants.MaxValue) &&
                c.a > SKIDConstants.MinValue)

        if Array.isEmpty validPixels then
            SKIDColor(1.0f, 1.0f, 1.0f, 1.0f)
        else
            let r, g, b, a = 
                validPixels 
                |> Array.fold (fun (r, g, b, a) c -> 
                    (r + c.r, g + c.g, b + c.b, a + c.a)) (0.0f, 0.0f, 0.0f, 0.0f)
            let n = float32 validPixels.Length
            SKIDColor(r / n, g / n, b / n, a / n)

    let shiftColor (color: SKIDColor) (diff: SKIDColor) =
        SKIDColor(
            clampColorComponent (color.r + diff.r),
            clampColorComponent (color.g + diff.g),
            clampColorComponent (color.b + diff.b),
            clampColorComponent (color.a + diff.a)
        )

    let applyMapping (addFactor:float32)(source: SKIDColor[]) (target: SKIDColor[]) =
        let avgS = averageColor source
        let avgT = averageColor target
        let diff = SKIDColor(
            avgT.r - avgS.r,
            avgT.g - avgS.g,
            avgT.b - avgS.b,
            avgT.a - avgS.a
        )
        source |> Array.map (fun c -> shiftColor c (diff * addFactor))

    let getMoodColor (source: SKIDColor[]) (k: int)(iterCount:int) =
        let domColor = ColorClustering.getDominantColor source {
            ClusterCount = k
            Iterations = iterCount
        }
        let diff = SKIDColor(domColor.r - 0.5f, domColor.g - 0.5f, domColor.b - 0.5f, 0.0f)
        source |> Array.map (fun c -> shiftColor c diff)


    // 워크플로우용 함수
    let applyMoodColor (input: ImageProcessInput<ColorMoodOption>) : SKIDImage =
        let pixels = input.image.pixels
        let k = input.config.Value.clusterCount
        let refrenceImage = input.config.Value.refrenceImage
        let iterCount = input.config.Value.iterCount
        let addFactor = input.config.Value.addFactor
        // TODO : addFactor가 자동으로 조정되는 자동밝기모드 ... > 히스토그램?
        let shifted = getMoodColor refrenceImage.pixels k iterCount |> applyMapping addFactor pixels
        SKIDImage(shifted, input.image.width, input.image.height)