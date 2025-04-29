namespace LuticaSKID.Models
open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction
open System.Runtime.InteropServices
open System.Collections.Generic
open ILGPU
open ILGPU.Runtime
open System
open LuticaSKID.BoxedZoneEditAdaptor

module TextureImageProcessing =
    [<ComVisible(true)>]

    type ImageProcessTwoImageOption = {
        refrenceImage: SKIDImage
        constant: float32
    }

    type SingleImageProcessType =
        | Median = 1
        | GaussianBlur = 2 
        | BoxBlur = 3
        | MotionBlur = 4
        | Sharpen = 5
        | EdgeDetection =6
        | Invert = 7
        | ReplaceSpecialColorZoneToTranspaintColor = 8

    type ReplaceDetectType = 
        | BackgroundIsBlack = 0         // 검은색 배경
        | BackgroundIsWhite = 1         // 흰색 배경
        | BackegroundIsTranspaint = 2   // 투명한 배경

    type ImageProcessType = 
        | TwoImageProcess of ImageProcessTwoImage * ImageProcessTwoImageOption
        | SingleImageProcess of SingleImageProcessType
    type ImageProcessInputOption = {
        processType: ImageProcessType
        constant: float32
    }
    type SimpleImageSynArgu = {
        processType: ImageProcessTwoImage
        partialImage: MarkingImage
        constant: float32
    }

    type Processer() =
        interface ICanParticalImageProcesser<SimpleImageSynArgu> with
            override this.ProcessingPartically
                with get (): MarkedImageProcess<SimpleImageSynArgu> = 
                    fun (acc,image, option,refImage) -> 
                        let pixels = image.pixels
                        let width = image.width
                        let height = image.height
                        let processOp = option.processType
                        if processOp = ImageProcessTwoImage.ColorDifference then
                            let mainColorOnReference = 
                                refImage.image.pixels
                                |> Array.Parallel.filter filteringVaildColor
                                |> Array.average
                            Processer.ProcessMainColor(acc,image, mainColorOnReference, option.constant)
                        else 
                        
                            let cuttedImage:SKIDImage = 
                                generateCroppedImage refImage.image (refImage.center* -1) (SKIDPixelVector2(image.width,image.height))
                            let newOption = {refrenceImage = cuttedImage;constant = option.constant;}
                            Processer.ProcessImage(pixels, width, height, processOp, newOption)
        end
            
        
        static member private ProcessImage(pixels: SKIDColor[], width: int, height: int,opcode:ImageProcessTwoImage, option: ImageProcessTwoImageOption) : SKIDImage =
            if pixels.Length = 0 then
                raise (ArgumentException("The input image pixels cannot be empty."))
            
            let resizedImage = if option.refrenceImage.width <> width || option.refrenceImage.height <> height then resizeImage option.refrenceImage width height 
                                else option.refrenceImage
            // using....
            use GPUContext = Context.CreateDefault()
            use GPUAccelerator = GPUContext.GetPreferredDevice(preferCPU=false).CreateAccelerator(GPUContext)
            use GPUOriginImage = GPUAccelerator.Allocate1D<SKIDColor>(pixels) 
            use GPUReferenceImage = GPUAccelerator.Allocate1D<SKIDColor>(resizedImage.pixels) 
            use GPUResultImage = GPUAccelerator.Allocate1D<SKIDColor>(pixels.Length)
            // 유의 : GPU에 들어가는 Lambda에는 그 어떠한 Capture도 들어갈 수 없다.
            // 따라서, GPU에서 사용할 Lambda는 반드시 모든 변수를 인자로 받고, Enum 역시 int로 변환해야 한다.
            let kernel (index: Index1D) 
                       (origin: ArrayView1D<SKIDColor, Stride1D.Dense>) 
                       (reference: ArrayView1D<SKIDColor, Stride1D.Dense>) 
                       (result: ArrayView1D<SKIDColor, Stride1D.Dense> )
                       (constant_: float32) 
                       (processOp: int) // Changed to int to avoid unsupported FSharpFunc
                       (minval: float32) =
                let processing (a: SKIDColor) (b: SKIDColor) =
                    match processOp with
                    | 0 -> a + b * constant_
                    | 1 -> a - b * constant_
                    | 2 -> a * b * constant_
                    | 3 -> a / (b + minval) * constant_
                    | 4 -> (a + b) / 2.0f * constant_
                    | 5 -> if b.a <= 0.0f then a else generateNoneAlphaColor (a + b * constant_)
                    | 6 -> if b.a <= 0.0f then a else generateNoneAlphaColor (a - b * constant_)
                    | 7 -> if b.a <= 0.0f then a else generateNoneAlphaColor (b * constant_)
                    | _ -> a + b * constant_
                result.[index] <- processing origin.[index] reference.[index]
            let kernelLauncher = GPUAccelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView1D<SKIDColor, Stride1D.Dense>, ArrayView1D<SKIDColor, Stride1D.Dense>, ArrayView1D<SKIDColor, Stride1D.Dense>, float32,int,float32> (kernel)
            kernelLauncher.Invoke(GPUResultImage.IntExtent, GPUOriginImage.View, GPUReferenceImage.View, GPUResultImage.View, option.constant, int opcode, 1e-5f)
            GPUAccelerator.Synchronize()
            let resultPixels = GPUResultImage.GetAsArray1D()
            SKIDImage(resultPixels, width, height)
        
        static member private ProcessMainColor(gpuAccelerator:Accelerator,image:SKIDImage,refColor:SKIDColor,constant:float32): SKIDImage =
            let width = image.width
            let height = image.height
            use GPUOriginImage = gpuAccelerator.Allocate1D<SKIDColor>(image.pixels) 
            use GPUResultImage = gpuAccelerator.Allocate1D<SKIDColor>(image.pixels.Length)
            let kernel (index: Index1D) 
                       (origin: ArrayView1D<SKIDColor, Stride1D.Dense>) 
                       (refColor:SKIDColor) 
                       (result: ArrayView1D<SKIDColor, Stride1D.Dense> )
                       (constant_: float32) =
                       result[index] <- SKIDColor(
                              (origin.[index].r + refColor.r * constant_),
                              (origin.[index].g + refColor.g * constant_),
                              (origin.[index].b + refColor.b * constant_),
                              origin.[index].a
                          )
            let kernelLauncher = gpuAccelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView1D<SKIDColor, Stride1D.Dense>, SKIDColor, ArrayView1D<SKIDColor, Stride1D.Dense>,float32> kernel

            kernelLauncher.Invoke(GPUResultImage.IntExtent, GPUOriginImage.View, refColor, GPUResultImage.View, constant)
            gpuAccelerator.Synchronize()
            let resultPixels = GPUResultImage.GetAsArray1D()
            SKIDImage(resultPixels, width, height)
             
        
        static member private ProcessImageSelf (image:SKIDImage) (op:SingleImageProcessType) (processOption:ImageProcessInputOption) : SKIDImage =
            let width = image.width
            let height = image.height
            use GPUContext = Context.CreateDefault()
            use GPUAccelerator = GPUContext.GetPreferredDevice(preferCPU=false).CreateAccelerator(GPUContext)
            use GPUOriginImage = GPUAccelerator.Allocate1D<SKIDColor>(image.pixels) 
            use GPUResultImage = GPUAccelerator.Allocate1D<SKIDColor>(image.pixels.Length)
            let kernel (index: Index1D) 
                       (origin: ArrayView1D<SKIDColor, Stride1D.Dense>) 
                       (result: ArrayView1D<SKIDColor, Stride1D.Dense> )
                       (processOp: int) // Changed to int to avoid unsupported FSharpFunc
                       (constant: float32) =
                match processOp with
                | 0 -> result.[index] <- origin.[index] * constant
                | 1 -> result.[index] <- origin.[index] / constant
                | 7 -> 
                    let color = origin.[index]
                    result.[index] <- SKIDColor( 1.0f - color.r, 1.0f - color.g, 1.0f - color.b, color.a )
                | _ -> result.[index] <- origin.[index]
            let kernelLauncher = GPUAccelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView1D<SKIDColor, Stride1D.Dense>, ArrayView1D<SKIDColor, Stride1D.Dense>, int, float32> kernel
            kernelLauncher.Invoke(GPUResultImage.IntExtent, GPUOriginImage.View, GPUResultImage.View, int op, processOption.constant)
            GPUAccelerator.Synchronize()
            let resultPixels = GPUResultImage.GetAsArray1D()
            SKIDImage(resultPixels, width, height)

        static member public Process(accelerator:Accelerator)(input: ImageProcessInput<ImageProcessInputOption>) : SKIDImage =
            let processType = input.config.Value.processType
            try 
                match processType with
                | TwoImageProcess (processOp, option) ->
                    let pixels = input.image.pixels
                    let width = input.image.width
                    let height = input.image.height

                    if processOp = ImageProcessTwoImage.ColorDifference then
                        let mainColorOnReference = 
                            option.refrenceImage.pixels
                            |> Array.Parallel.filter filteringVaildColor
                            |> Array.average
                        Processer.ProcessMainColor(accelerator,input.image, mainColorOnReference, option.constant)
                    else Processer.ProcessImage(pixels, width, height, processOp, option)
                | SingleImageProcess(processOperation) ->
                     Processer.ProcessImageSelf input.image processOperation input.config.Value
            with
            | e ->
                accelerator.Dispose() 
                failwith ("error on process "+ e.Message)
