namespace LuticaSKID
open System
open LuticaSKID.StructTypes
module ColorGrammer =

    let average (colors: SKIDColor list) =
        let mutable r, g, b, a = 0.0f, 0.0f, 0.0f, 0.0f
        let n = float32 colors.Length
        for c in colors do
            r <- r + c.r
            g <- g + c.g
            b <- b + c.b
            a <- a + c.a
        SKIDColor(r / n, g / n, b / n, a / n)

    let SamplingAndGetLeadColor (pixels: SKIDColor[]) (c1:SKIDColor) (c2:SKIDColor) =
        let borderColor = ColorSpaceBoundary(c1, c2)
        let mutable leadColor = SKIDColor(0.0f, 0.0f, 0.0f, 0.0f)
        let mutable count = 0
        for p in pixels do
            if borderColor.Contains(p) then
                leadColor <- leadColor + p
                count <- count + 1

        

