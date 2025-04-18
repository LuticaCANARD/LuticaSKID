namespace LuticaSKID.Models
open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction
open System.Runtime.InteropServices
open System.Collections.Generic
open ILGPU
open ILGPU.Runtime
open System

module TextureImageProcessing =
    [<ComVisible(true)>]
    type ImageProcessTwoImage = 
        | Add = 0
        | Subtract = 1
        | Multiply = 2
        | Divide  = 3
        | Average = 4
        | ColorBlend = 5
        | ColorDifference = 6
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

    type Processer() =
        static member private ProcessImage(pixels: SKIDColor[], width: int, height: int,opcode:ImageProcessTwoImage, option: ImageProcessTwoImageOption) : SKIDImage =
            let GPUContext = Context.CreateDefault()
            let GPUAccelerator = GPUContext.GetPreferredDevice(preferCPU=false).CreateAccelerator(GPUContext)
            let GPUOriginImage = GPUAccelerator.Allocate1D<SKIDColor>(pixels) 
            let GPUReferenceImage = GPUAccelerator.Allocate1D<SKIDColor>(option.refrenceImage.pixels) 
            let GPUResultImage = GPUAccelerator.Allocate1D<SKIDColor>(pixels.Length)
            // 유의 : GPU에 들어가는 Lambda에는 그 어떠한 Capture도 들어갈 수 없다.
            // 따라서, GPU에서 사용할 Lambda는 반드시 모든 변수를 인자로 받고, Enum 역시 int로 변환해야 한다.
            let kernel (index: Index1D) 
                       (origin: ArrayView1D<SKIDColor, Stride1D.Dense>) 
                       (reference: ArrayView1D<SKIDColor, Stride1D.Dense>) 
                       (result: ArrayView1D<SKIDColor, Stride1D.Dense> )
                       (constant: float32) 
                       (processOp: int) // Changed to int to avoid unsupported FSharpFunc
                       (minval: float32) =
                let processing (a: SKIDColor) (b: SKIDColor) =
                    match processOp with
                    | 0 -> a + b * constant
                    | 1 -> a - b * constant
                    | 2 -> a * b * constant
                    | 3 -> a / (b + minval) * constant
                    | 4 -> (a + b) / 2.0f * constant
                    | 5->if b.a <= 0.0f then a else generateNoneAlphaColor (a + b * constant)
                    | _ -> a + b * constant
                result.[index] <- processing origin.[index] reference.[index]
            let kernelLauncher = GPUAccelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView1D<SKIDColor, Stride1D.Dense>, ArrayView1D<SKIDColor, Stride1D.Dense>, ArrayView1D<SKIDColor, Stride1D.Dense>, float32,int,float32> (kernel)
            kernelLauncher.Invoke(GPUResultImage.IntExtent, GPUOriginImage.View, GPUReferenceImage.View, GPUResultImage.View, option.constant, int opcode, 1e-5f)

            GPUAccelerator.Synchronize()

            let resultPixels = GPUResultImage.GetAsArray1D()
            GPUOriginImage.Dispose()
            GPUReferenceImage.Dispose()
            GPUResultImage.Dispose()
            GPUAccelerator.Dispose()
            GPUContext.Dispose()

            SKIDImage(resultPixels, width, height)
        
        static member private ProcessMainColor(image:SKIDImage,refColor:SKIDColor,constant:float32): SKIDImage =
            let GPUContext = Context.CreateDefault()
            let GPUAccelerator = GPUContext.GetPreferredDevice(preferCPU=false).CreateAccelerator(GPUContext)
            let width = image.width
            let height = image.height
            let GPUOriginImage = GPUAccelerator.Allocate1D<SKIDColor>(image.pixels) 
            let GPUResultImage = GPUAccelerator.Allocate1D<SKIDColor>(image.pixels.Length)
            let kernel (index: Index1D) 
                       (origin: ArrayView1D<SKIDColor, Stride1D.Dense>) 
                       (refColor:SKIDColor) 
                       (result: ArrayView1D<SKIDColor, Stride1D.Dense> )
                       (constant: float32) =
                       result[index] <- SKIDColor(
                              origin.[index].r * refColor.r * constant,
                              origin.[index].g * refColor.g * constant,
                              origin.[index].b * refColor.b * constant,
                              origin.[index].a
                          )
            let kernelLauncher = GPUAccelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView1D<SKIDColor, Stride1D.Dense>, SKIDColor, ArrayView1D<SKIDColor, Stride1D.Dense>,float32> kernel

            kernelLauncher.Invoke(GPUResultImage.IntExtent, GPUOriginImage.View, refColor, GPUResultImage.View, constant)
            GPUAccelerator.Synchronize()
            let resultPixels = GPUResultImage.GetAsArray1D()
            GPUOriginImage.Dispose()
            GPUResultImage.Dispose()
            GPUAccelerator.Dispose()
            GPUContext.Dispose()
            SKIDImage(resultPixels, width, height)
             

        static member public Process(input: ImageProcessInput<ImageProcessInputOption>) : SKIDImage =
            let processType = input.config.Value.processType
            match processType with
            | TwoImageProcess (processOp, option) ->
                let pixels = input.image.pixels
                let width = input.image.width
                let height = input.image.height

                if processOp = ImageProcessTwoImage.ColorDifference then
                    let mainColorOnReference = 
                        option.refrenceImage.pixels
                        |> Array.Parallel.filter filteringVaildColor
                        |> Array.averageBy (fun c -> SKIDColor(c.r, c.g, c.b, 1.0f))
                    Processer.ProcessMainColor(input.image, mainColorOnReference, option.constant)

                    
                else Processer.ProcessImage(pixels, width, height, processOp, option)
            | SingleImageProcess(processOperation) ->
                raise (NotImplementedException("The specified single image process operation is not implemented."))