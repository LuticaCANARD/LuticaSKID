namespace LuticaSKID.Models
open System
open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction
open ILGPU
open ILGPU.Runtime
module HistogramProcessor =
    type Process()=
        static member histogramKernel (tid:Index1D) (pixels:ArrayView<SKIDColor>) (histogram:ArrayView<int>) (pixelCount:int) =
            if tid.X < pixelCount then
                let px = tid.X
                let r = pixels.[px].r
                let g = pixels.[px].g
                let b = pixels.[px].b
                let ir = int (r * 255.0f)
                let ig = int (g * 255.0f)
                let ib = int (b * 255.0f)
                // Calculate the index for the histogram
                let index = ir * 256 + ig * 256 + ib
                // Increment the histogram count for this color
                Atomic.Add(&histogram.[index], 1) |> ignore


