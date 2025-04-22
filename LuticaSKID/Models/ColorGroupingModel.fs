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
            
            // ---------------------
            let k = 8
            let centroids = Array.create k (SKIDColor(0.0f, 0.0f, 0.0f, 0.0f))
            let assignments = Array.create image.pixels.Length 0
            // 이전값. 
            let mutable hostCentroids = Array.init (k) (fun i -> centroids.[i]) 
            let mutable hostAssignments = Array.init (image.pixels.Length) (fun i -> assignments.[i])



            // --------------------- Assignments on GPU...
            use d_centroids = accelerator.Allocate1D<SKIDColor>(centroids)
            use d_originImage = accelerator.Allocate1D<SKIDColor>(image.pixels)
            use d_assignments = accelerator.Allocate1D<int>(assignments.Length)

            //--------------------  

            for _ in 0 .. 10 do
                kernel.Invoke(d_originImage.IntExtent, d_originImage.View, d_centroids.View, d_assignments.View, k, image.pixels.Length)
                accelerator.Synchronize()
                // Copy back to host
                hostCentroids <- d_centroids.GetAsArray1D()
                hostAssignments <- d_assignments.GetAsArray1D()
                // Update centroids
                for i in 0 .. k - 1 do
                    let mutable r = 0.0f
                    let mutable g = 0.0f
                    let mutable b = 0.0f
                    let mutable count = 0
                    for j in 0 .. image.pixels.Length - 1 do
                        if hostAssignments.[j] = i then
                            r <- r + image.pixels.[j].r
                            g <- g + image.pixels.[j].g
                            b <- b + image.pixels.[j].b
                            count <- count + 1
                    if count > 0 then
                        centroids.[i] <- SKIDColor(r / float32 count, g / float32 count, b / float32 count, 1.0f)
            let finalCentroids = d_centroids.GetAsArray1D()
            let finalAssignments = d_assignments.GetAsArray1D()
            let colorResult = 
                finalCentroids
                |> Array.mapi (fun i c -> 
                    let count = Array.filter (fun x -> x = i) finalAssignments |> Array.length
                    {colorElement = c; weight = float32 count / float32 image.pixels.Length; piexelcount = count}
                ) |> Array.toList
            {
                result = { colorResult = colorResult; colorCount = finalCentroids.Length };
            }


            





