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

    let kMeansPlusPlusInit (pixels: SKIDColor[]) (k: int) =
        let rand = Random()
        let centroids = ResizeArray<SKIDColor>()
        centroids.Add(pixels.[rand.Next(pixels.Length)])

        while centroids.Count < k do
            let distances = pixels |> Array.map (fun p ->
                centroids |> Seq.map (fun c -> distance p c) |> Seq.min
            )
            let probabilities = distances |> Array.map (fun d -> d * d)
            let cumulative = probabilities |> Array.scan (+) 0.0f |> Array.tail
            let total = cumulative.[cumulative.Length - 1]
            let r = float32 (rand.NextDouble()) * total
            let nextCentroid = pixels.[Array.findIndex (fun c -> c >= r) cumulative]
            centroids.Add(nextCentroid)

        centroids.ToArray()

    let kMeans (pixels: SKIDColor[]) (init: InitType) =
        let rand = Random()
        let initialCentroids = kMeansPlusPlusInit pixels init.ClusterCount
        let mutable centroids = initialCentroids
        let mutable hasConverged = false

        for _ in 1 .. init.Iterations do
            if not hasConverged then
                let clusters = Array.init init.ClusterCount (fun _ -> ResizeArray<SKIDColor>())
                pixels
                |> Array.Parallel.iter (fun p ->
                    let distances = centroids |> Array.map (fun c -> distance p c)
                    let idx = distances |> Array.mapi (fun i d -> i, d) |> Array.minBy snd |> fst
                    lock clusters.[idx] (fun () -> clusters.[idx].Add p)
                )

                let newCentroids = clusters |> Array.Parallel.map (fun c ->
                    if c.Count = 0 then
                        pixels.[rand.Next(pixels.Length)]
                    else
                        average (List.ofSeq c)
                )

                // 중심점 변화량 계산
                hasConverged <- 
                    Array.zip centroids newCentroids
                    |> Array.forall (fun (oldC, newC) -> distance oldC newC < 0.001f)

                centroids <- newCentroids

        centroids

    // TODO : 너무 과도한 연산량이 있어서 K-means에 대해서 다시 군집화가 필요함.
    // 군집화에 대해서 다시 구현할 것.
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