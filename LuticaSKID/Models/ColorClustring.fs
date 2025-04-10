namespace LuticaSKID
open System
open LuticaSKID.StructTypes

module ColorClustering =

    let inline distance (a: SKIDColor) (b: SKIDColor) =
        let dr, dg, db = a.r - b.r, a.g - b.g, a.b - b.b
        dr * dr + dg * dg + db * db // 유클리드 거리 (alpha 제외)

    let average (colors: SKIDColor list) =
        let mutable r, g, b, a = 0.0f, 0.0f, 0.0f, 0.0f
        let n = float32 colors.Length
        for c in colors do
            r <- r + c.r
            g <- g + c.g
            b <- b + c.b
            a <- a + c.a
        SKIDColor(r / n, g / n, b / n, a / n)

    let kMeans (pixels: SKIDColor[]) (k: int) (iterations: int) =
        let rand = Random()
        let initialCentroids = [|
            for _ in 1 .. k ->
                let idx = rand.Next(pixels.Length)
                pixels.[idx]
        |]

        let mutable centroids = initialCentroids

        for _ in 1 .. iterations do
            let clusters = Array.init k (fun _ -> ResizeArray<SKIDColor>())
            for p in pixels do
                let idx =
                    centroids
                    |> Array.mapi (fun i c -> i, distance p c)
                    |> Array.minBy snd
                    |> fst
                clusters.[idx].Add p

            centroids <- clusters |> Array.map (fun c ->
                if c.Count = 0 then
                    pixels.[rand.Next(pixels.Length)]
                else
                    average (List.ofSeq c)
            )

        centroids

    
    let getDominantColor (pixels: SKIDColor[]) (k:int) =
        let iterations = 6
        let centroids = kMeans pixels k iterations

        let counts =
            Array.init k (fun i -> i, 0)
            |> Array.map (fun (i, _) ->
                let count = pixels |> Array.filter (fun p ->
                    let nearest = centroids |> Array.minBy (fun c -> distance p c)
                    distance p centroids.[i] = distance p nearest) |> Array.length
                (i, count)
            )

        let dominantIdx = counts |> Array.maxBy snd |> fst
        centroids.[dominantIdx]
    let getDominantColorDefault pixels =
        getDominantColor pixels 4
