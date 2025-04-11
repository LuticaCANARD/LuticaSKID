namespace LuticaSKID

open System
open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction

module ColorClustering =

    type InitType = {
        ClusterCount: int
        Iterations: int
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

    let kMeans (pixels: SKIDColor[]) (init: InitType) =
        let rand = Random()
        let initialCentroids = [|
            for _ in 1 .. init.ClusterCount ->
                let idx = rand.Next(pixels.Length)
                pixels.[idx]
        |]

        let mutable centroids = initialCentroids

        for _ in 1 .. init.Iterations do
            let clusters = Array.init init.ClusterCount (fun _ -> ResizeArray<SKIDColor>())
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

    let getDominantColor (pixels: SKIDColor[]) (init: InitType) =
        let centroids = kMeans pixels init

        let counts =
            Array.init init.ClusterCount (fun i -> i, 0)
            |> Array.map (fun (i, _) ->
                let count = pixels |> Array.filter (fun p ->
                    let nearest = centroids |> Array.minBy (fun c -> distance p c)
                    distance p centroids.[i] = distance p nearest) |> Array.length
                (i, count)
            )

        let dominantIdx = counts |> Array.maxBy snd |> fst
        centroids.[dominantIdx]

    let getDominantColorDefault pixels =
        getDominantColor pixels { ClusterCount = 4; Iterations = 6 }