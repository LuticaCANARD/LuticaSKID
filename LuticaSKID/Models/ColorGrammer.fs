namespace LuticaSKID
open System
open LuticaSKID.StructTypes
module ColorGrammer =
    type InitType = {
        ColorA: SKIDColor
        ColorB: SKIDColor
    }
    let average (colors: SKIDColor list) =
        let mutable r, g, b, a = 0.0f, 0.0f, 0.0f, 0.0f
        let n = float32 colors.Length
        for c in colors do
            r <- r + c.r
            g <- g + c.g
            b <- b + c.b
            a <- a + c.a
        SKIDColor(r / n, g / n, b / n, a / n)

    let SamplingAndGetLeadColor (pixels: SKIDColor[]) (init: InitType) =
        let borderColor = ColorSpaceBoundary(init.ColorA, init.ColorB)
        let mutable leadColor = SKIDColor(0.0f, 0.0f, 0.0f, 0.0f)
        let mutable count = 0
        for p in pixels do
            if borderColor.Contains(p) then
                leadColor <- leadColor + p
                count <- count + 1

        if count > 0 then
            SKIDColor(leadColor.r / float32 count, leadColor.g / float32 count, leadColor.b / float32 count, leadColor.a / float32 count)
        else
            SKIDColor(0.0f, 0.0f, 0.0f, 0.0f)
