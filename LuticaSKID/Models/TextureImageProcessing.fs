namespace LuticaSKID.Models
open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction
open System.Runtime.InteropServices
open System.Collections.Generic
open ILGPU;
open ILGPU.Runtime;
open System

module TextureImageProcessing =
    type ImageProcessTwoImage = 
        | Add 
        | Subtract 
        | Multiply 
        | Divide 
    type ImageProcessTwoImageOption = {
        refrenceImage: SKIDImage
        constant: float32
    }
    type SingleImageProcessType =
        | Average 
        | Median 
        | GaussianBlur 
        | BoxBlur 
        | MotionBlur 
        | Sharpen 
        | EdgeDetection
        | Invert 


    type ImageProcessType = 
        | TwoImageProcess of ImageProcessTwoImage * ImageProcessTwoImageOption
        | SingleImageProcess of SingleImageProcessType
    type ImageProcessInputOption = {
        processType: ImageProcessType
    }

    let GenerateProcessedImage(input: ImageProcessInput<ImageProcessInputOption>) : SKIDImage =
        let pixels = input.image.pixels
        let width = input.image.width
        let height = input.image.height
        let processType = input.config.Value.processType
        let GPUContext = Context.CreateDefault()
        let GPUAccelerator = GPUContext.GetPreferredDevice(preferCPU=false).CreateAccelerator(GPUContext)
        let GPUOriginImage = GPUAccelerator.Allocate1D<SKIDColor>(pixels) 


        match processType with
        | TwoImageProcess (processOp,option) ->

            let operation =
                match processOp with
                | Add -> fun (a: SKIDColor) (b: SKIDColor) -> a + b * option.constant
                | Subtract -> fun (a: SKIDColor) (b: SKIDColor) -> a - b * option.constant
                | Multiply -> fun (a: SKIDColor) (b: SKIDColor) -> a * b * option.constant
                | Divide -> fun (a: SKIDColor) (b: SKIDColor) -> a / (b + LuticaSKID.SKIDConstants.MinValue) * option.constant
            let GPUResultImage = GPUAccelerator.Allocate1D<SKIDColor>(option.refrenceImage.pixels) 
            let gpuProcessFun = 
                Action<Index1D, ArrayView<SKIDColor>, ArrayView<SKIDColor>>(
                    fun (index: Index1D) (src: ArrayView<SKIDColor>) (dst: ArrayView<SKIDColor>) ->
                        let i = index.X
                        if i < int src.Length then
                            dst.[i] <- (operation dst.[i] src.[i]) // Apply the operation to each pixel
                )

            let loadingImage = GPUAccelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<SKIDColor>, ArrayView<SKIDColor>>(gpuProcessFun)
            loadingImage.Invoke((int GPUResultImage.Length), GPUOriginImage.View ,GPUResultImage.View)
            GPUAccelerator.Synchronize()
            let resultPixels = GPUResultImage.GetAsArray1D()
            SKIDImage(resultPixels, width, height)
        | SingleImageProcess(processOperation) ->
            match processOperation with 
            | Average -> 
                let resultPixels = Array.zeroCreate<SKIDColor> (width * height)
                for i in 0 .. (width * height - 1) do
                    let pixel = pixels.[i]
                    resultPixels.[i] <- SKIDColor(pixel.r / 2.0f, pixel.g / 2.0f, pixel.b / 2.0f, pixel.a)
                SKIDImage(resultPixels, width, height)
           
        