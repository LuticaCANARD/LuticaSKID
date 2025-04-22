namespace LuticaSKID.Models

open System
open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction
open ILGPU
open ILGPU.Runtime


module ColorGroupingModel =
    type ColorResultElement = {
        colorElement:SKIDColor
        weight:float32
        piexelcount:int
    }
    type ColorGroupingResult = {
        colorResult: List<ColorResultElement>
        colorCount: int
    }
    type ColorGroupiongAnlyzeResult = AnalyzeResult<ColorGroupingResult>

    type Process() =
        static member computeInertia (assignments: int[]) (centroids: SKIDColor[]) (image: SKIDImage) =
            assignments
            |> Array.mapi (fun i cid ->
                let p = image.pixels.[i]
                let c = centroids.[cid]
                let dr = p.r- c.r
                let dg = p.g - c.g
                let db = p.b - c.b
                dr * dr + dg * dg + db * db)
            |> Array.sum
                    
        static member findBestK (image:SKIDImage) (maxK:int):ColorGroupiongAnlyzeResult =
            let inertias = ResizeArray()
            let results = ResizeArray()
            let mutable bestK:ColorGroupiongAnlyzeResult = {
                result = { colorResult = []; colorCount = 0 }
            }
            for k in 1 .. maxK do
                let result = Process.ExecuteKmeans image
                let assignments = result.result.colorResult |> Array.ofList |> Array.Parallel.map(fun a->a.colorElement) 
                let centroids = 
                    assignments 
                    |> Array.distinct
                    |> Array.distinctBy id
                let inertia = Process.computeInertia (assignments |> Array.Parallel.mapi (fun i c -> Array.findIndex ((=) c) centroids)) centroids image
                inertias.Add(inertia)
                results.Add((k, result, inertia))
                if inertias.Count > 2 then
                    let last = inertias.[inertias.Count - 1]
                    let prev = inertias.[inertias.Count - 2]
                    if (prev - last) / prev < 0.05f then // inertia 감소폭이 5% 미만이면 종료
                        bestK  <- results |> Seq.minBy (fun (k, r, inertia) ->  k + int(inertia * 0.001f)) |> (fun (_, r, _) -> r)
            bestK

                        
            
        static member kmeansKernel (tid:Index1D) 
            (pixels:ArrayView<SKIDColor>) 
            (centroids:ArrayView<SKIDColor>) 
            (assignments:ArrayView<int>) (k:int) (pixelCount:int) =
                if tid.X < pixelCount then
                    
                    let px = tid.X
                    let r = pixels.[px].r
                    let g = pixels.[px].g
                    let b = pixels.[px].b

                    let mutable best = 0
                    let mutable bestDist = 999999.0f

                    for i = 0 to k - 1 do
                        let ci = i
                        let dr = r - centroids.[ci].r
                        let dg = g - centroids.[ci].g
                        let db = b - centroids.[ci].b
                        let dist = dr * dr + dg * dg + db * db
                        if dist < bestDist then
                            best <- i
                            bestDist <- dist

                    assignments.[tid] <- best
            
        static member ExecuteKmeans (image:SKIDImage) : ColorGroupiongAnlyzeResult =
            use context = Context.CreateDefault()
            use accelerator = context.GetPreferredDevice(preferCPU=false).CreateAccelerator(context)

            let kernel = accelerator.LoadAutoGroupedStreamKernel<Index1D,ArrayView<SKIDColor>,ArrayView<SKIDColor>,ArrayView<int>,int,int> Process.kmeansKernel

            let maxK = 10
            let pixelLength = image.pixels.Length

            let mutable bestResult = Unchecked.defaultof<_>
            let mutable previousInertia = Single.MaxValue
            let mutable bestK = 2
            let mutable diff = Single.MaxValue
            for k in 2 .. maxK do
                if diff > 0.05f then
                    let centroids = Array.init k (fun _ -> SKIDColor(0.0f, 0.0f, 0.0f, 1.0f))
                    let assignments = Array.zeroCreate pixelLength
                    use d_centroids = accelerator.Allocate1D<SKIDColor>(centroids)
                    use d_originImage = accelerator.Allocate1D<SKIDColor>(image.pixels)
                    use d_assignments = accelerator.Allocate1D<int>(assignments.Length)

                    for _ in 0 .. 9 do
                        kernel.Invoke(d_originImage.IntExtent, d_originImage.View, d_centroids.View, d_assignments.View, k, pixelLength)
                        accelerator.Synchronize()

                        let hostAssignments = d_assignments.GetAsArray1D()

                        // centroids 업데이트
                        let centroidSums = Array.init k (fun _ -> (0.0f, 0.0f, 0.0f, 0))
                        for j in 0 .. pixelLength - 1 do
                            let clusterIndex = hostAssignments.[j]
                            let p = image.pixels.[j]
                            let (rSum, gSum, bSum, count) = centroidSums.[clusterIndex]
                            centroidSums.[clusterIndex] <- (rSum + p.r, gSum + p.g, bSum + p.b, count + 1)

                        for i in 0 .. k - 1 do
                            let (rSum, gSum, bSum, count) = centroidSums.[i]
                            if count > 0 then
                                centroids.[i] <- SKIDColor(rSum / float32 count, gSum / float32 count, bSum / float32 count, 1.0f)

                        d_centroids.CopyFromCPU(centroids)


                    let finalCentroids = d_centroids.GetAsArray1D()
                    let finalAssignments = d_assignments.GetAsArray1D()

                    // inertia 계산
                    let inertia =
                        Array.Parallel.sumBy (fun i ->
                            let p = image.pixels.[i]
                            let c = finalCentroids.[finalAssignments.[i]]
                            let dr = p.r - c.r
                            let dg = p.g - c.g
                            let db = p.b - c.b
                            dr*dr + dg*dg + db*db
                        ) [| 0 .. pixelLength - 1 |]
                    let colorResult =
                        finalCentroids
                        |> Array.Parallel.mapi (fun i c ->
                            let count = finalAssignments |> Array.Parallel.filter ((=) i) |> Array.length
                            { colorElement = c; weight = float32 count / float32 pixelLength; piexelcount = count }
                        ) |> Array.toList

                    bestResult <- {
                        result = { colorResult = colorResult; colorCount = finalCentroids.Length };
                    }
                    // inertia 감소가 5% 미만이면 멈춤
                    if previousInertia <> Single.MaxValue then
                        diff <- (previousInertia - inertia) / previousInertia
                        if diff > 0.05f then
                            previousInertia <- inertia
                            bestK <- k

            bestResult



            





