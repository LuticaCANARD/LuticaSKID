namespace LuticaSKID.Models
open System
open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction
open ILGPU
open ILGPU.Runtime
module HistogramProcessor =
    type HistogramResult = {
        histogram: int[]
        pixelCount: int
    }
    type histogramAnalyzeResult = AnalyzeResult<HistogramResult>
    type histogramAnalyzeOption = {
        histogramSize: int
        doNotCountWhitePixel: bool
        originLevel:int
        mask: SKIDImage option
    }
    /// <summary>
    type Process()=
        static member histogramKernel (tid:Index1D) 
            (pixels:ArrayView<SKIDColor>) (histogram:ArrayView<int>) 
            (pixelCount:int)(doNotCountWhitePixel: int)(hlevel:int)(originLevel:float32) =
            if tid.X < pixelCount then
                let px = tid.X
                let r = pixels.[px].r
                let g = pixels.[px].g
                let b = pixels.[px].b
                let ir = int (r * 255.0f)
                let ig = int (g * 255.0f)
                let ib = int (b * 255.0f)
                // Calculate the index for the histogram
                let index = ir * hlevel * hlevel + ig * hlevel + ib
                // Increment the histogram count for this color
                Atomic.Add(&histogram.[index], 1) |> ignore

        static member makeHistogram (image:SKIDImage) (options:histogramAnalyzeOption):histogramAnalyzeResult = 
            let histogramSize = options.histogramSize
            let doNotCountWhitePixel = options.doNotCountWhitePixel
            let pixelCount = image.pixels.Length
            let histogram = Array.create (histogramSize*histogramSize*histogramSize) 0
            let level = options.histogramSize
            let originLevel = float32 options.originLevel
            use context = Context.CreateDefault()
            use accelerator = context.GetPreferredDevice(preferCPU=false).CreateAccelerator(context)
            let targetImg = if options.mask.IsSome then maskingImage image options.mask.Value else image
            use memory = accelerator.Allocate1D<SKIDColor>(targetImg.pixels)
            use histogramBuffer = accelerator.Allocate1D<int>(histogram)
            let kernel = accelerator.LoadAutoGroupedStreamKernel<Index1D,ArrayView<SKIDColor>,ArrayView<int>,int,int,int,float32>(Process.histogramKernel)
            let whiteInt = if doNotCountWhitePixel then 1 else 0
            kernel.Invoke(memory.IntExtent, memory.View, histogramBuffer.View, pixelCount,whiteInt,level,originLevel)
            accelerator.Synchronize()

            {
                result={
                    histogram=histogramBuffer.GetAsArray1D();
                    pixelCount=pixelCount;
                };
            }
        static member imageHistogramEqualize(origin:SKIDImage)(histogram:HistogramResult):SKIDImage = 
            let histogramSize = histogram.histogram.Length
            let pixelCount = histogram.pixelCount
            let histogram = histogram.histogram
            let histogramEqualized = Array.create histogramSize 0
            let cdf = Array.create histogramSize 0
            let cdfMin = histogram |> Array.findIndex (fun x -> x > 0)
            let cdfMax = histogram |> Array.findBack (fun x -> x > 0)
            let cdfRange = cdfMax - cdfMin
            let cdfScale = 255.0f / float32 cdfRange

            // Calculate the CDF
            for i in 0 .. histogramSize - 1 do
                if i = 0 then
                    cdf.[i] <- histogram.[i]
                else
                    cdf.[i] <- cdf.[i - 1] + histogram.[i]

            let rebuildPixels = Array.init origin.pixels.Length (fun i ->
                    let px = origin.pixels.[i]
                    let ir = int (px.r * 255.0f)
                    let ig = int (px.g * 255.0f)
                    let ib = int (px.b * 255.0f)
                    let index = ir * 256 + ig * 256 + ib
                    let newValue = cdf.[index] - cdfMin
                    let newR = float32 newValue / float32 pixelCount * cdfScale
                    let newG = float32 newValue / float32 pixelCount * cdfScale
                    let newB = float32 newValue / float32 pixelCount * cdfScale
                    SKIDColor(newR, newG, newB, px.a)
                )
            SKIDImage(
                rebuildPixels,
                origin.width,
                origin.height
            )



          


