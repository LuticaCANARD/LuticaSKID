namespace LuticaSKID.Models

open System
open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction
open ILGPU
open ILGPU.Runtime


/// <summary>
/// ColorGroupingModel
/// 
/// This module contains the implementation of the K-means clustering algorithm for color grouping.
/// It uses ILGPU for GPU acceleration.
/// 
/// </summary>

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
    type KmeansSetting = {maxK:int;maxIter:int;doNotCountWhitePixel:bool}
    type Process() =
        static member computeInertia (assignments: int[]) (centroids: SKIDColor[]) (image: SKIDImage) =
            assignments
            |> Array.mapi (fun i cid ->
                let p = image.pixels.[i]
                let c = centroids.[cid]
                let dr = p.r - c.r
                let dg = p.g - c.g
                let db = p.b - c.b
                dr * dr + dg * dg + db * db)
            |> Array.sum

        static member calculateCentroidSums k hostAssignments pixels =
            Array.init k (fun _ -> (0.0f, 0.0f, 0.0f, 0))
            |> fun tmp ->
                Array.zip hostAssignments pixels
                |> Array.groupBy fst
                |> Array.iter (fun (clusterIndex, clusterPixels) ->
                    let rSum = clusterPixels |> Array.sumBy (fun (_, p:SKIDColor) -> float p.r)
                    let gSum = clusterPixels |> Array.sumBy (fun (_, p) -> float p.g)
                    let bSum = clusterPixels |> Array.sumBy (fun (_, p) -> float p.b)
                    let count = clusterPixels.Length
                    tmp.[clusterIndex] <- (float32 rSum, float32 gSum, float32 bSum, count)
                )
                tmp
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
            
        static member ExecuteKmeans (image:SKIDImage)(maxK:int)(maxTry:int) : ColorGroupiongAnlyzeResult =
            use context = Context.CreateDefault()
            use accelerator = context.GetPreferredDevice(preferCPU=false).CreateAccelerator(context)
            try 
                let kernel = accelerator.LoadAutoGroupedStreamKernel<Index1D,ArrayView<SKIDColor>,ArrayView<SKIDColor>,ArrayView<int>,int,int> Process.kmeansKernel
                let pixelLength = image.pixels.Length

                let mutable bestResult = Unchecked.defaultof<_>
                let mutable previousInertia = Single.MaxValue
                let mutable bestK = 2
                let mutable diff = Single.MaxValue
                // K = 현재의 색상군집 갯수이다.
                // MaxK = 최대 색상군집 갯수이다.
                
                for k in 2 .. maxK do
                    if diff > 0.05f then
                        let centroids = Array.init k (fun _ -> SKIDColor(0.0f, 0.0f, 0.0f, 1.0f))
                        let assignments = Array.zeroCreate pixelLength
                        use d_centroids = accelerator.Allocate1D<SKIDColor>(centroids)
                        use d_originImage = accelerator.Allocate1D<SKIDColor>(image.pixels)
                        use d_assignments = accelerator.Allocate1D<int>(assignments.Length)
                        // centroids = 초기화된 색상군집이다.
                        // 이 로직부분은 색상군집을 계산해 누적해가는 부분이다.
                        // MaxTry = 최대 반복 횟수이다.
                        for _ in 0 .. maxTry do
                            kernel.Invoke(d_originImage.IntExtent, d_originImage.View, d_centroids.View, d_assignments.View, k, pixelLength)
                            accelerator.Synchronize()
                            let hostAssignments = d_assignments.GetAsArray1D()

                            // 사진에서 centroids를 추출하여 업데이트.
                            // centroids 업데이트
                            let centroidSums = Process.calculateCentroidSums k hostAssignments image.pixels
                            let centroids = Array.Parallel.init k (fun i ->
                                let (rSum, gSum, bSum, count) = centroidSums.[i]
                                if count > 0 then
                                    SKIDColor(rSum / float32 count, gSum / float32 count, bSum / float32 count, 1.0f)
                                else
                                    SKIDColor(0.0f, 0.0f, 0.0f, 1.0f)
                            )
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
            with
            | ex ->
                printfn "ILGPU Exception: %s" ex.Message
                accelerator.Dispose()
                {result = {colorResult = []; colorCount = 0};}



            





