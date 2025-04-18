namespace LuticaSKID
open LuticaSKID.StructTypes;
open System;
open ILGPU;
open ILGPU.Runtime;
open System.Runtime.InteropServices

module SKIDToolFunction =
    let inline distance (a: SKIDColor) (b: SKIDColor) =
        let dr, dg, db = a.r - b.r, a.g - b.g, a.b - b.b
        dr * dr + dg * dg + db * db // 유클리드 거리 (alpha 제외)
    let inline clampColorComponent (v: float32) = SKIDColor.FilteringNotVaildColorNumber v
    let inline clampInt (min: int) (max: int) (v: int) = if v < min then min elif v > max then max else v
    let inline flatten<'t> (arr2D: 't[,]) (_parallel : bool) =
        let w = arr2D.GetLength(0)
        let h = arr2D.GetLength(1)
        let gen = fun i -> arr2D.[i % w, i / w]
        if _parallel then Array.Parallel.init (w * h) gen else Array.init (w * h) gen
    let inline safeGet (arr: _[,]) x y =
        let w, h = arr.GetLength(0), arr.GetLength(1)
        if x < 0 || y < 0 || x >= w || y >= h then arr.[ clampInt 0 (w - 1) x, clampInt 0 (h - 1) y]
        else arr.[x, y]
    let inline flattenAutoParallel<'t> (arr2D: 't[,]) =
        flatten arr2D (arr2D.GetLength(0) > 255 || arr2D.GetLength(1) > 255)
    let inline computeGrayscale (color: SKIDColor) : float32 =
        0.299f * color.r + 0.587f * color.g + 0.114f * color.b
    let inline normalize (x: float32, y: float32, z: float32) : (float32 * float32 * float32) =
        let len = sqrt (x * x + y * y + z * z)
        if len = 0.0f then (0.0f, 0.0f, 0.0f) else (x / len, y / len, z / len)
    let inline normalize_SKIDVector3 (x: float32, y: float32, z: float32) : SKIDVector3 =
        let len = sqrt (x * x + y * y + z * z)
        if len = 0.0f then SKIDVector3 (0.0f, 0.0f, 0.0f) else SKIDVector3 (x / len, y / len, z / len)
    let inline clamp01 (v: float32) = SKIDColor.FilteringNotVaildColorNumber v
    let inline resizeImage (image: SKIDImage) (targetWidth: int) (targetHeight: int) : SKIDImage =
        if targetWidth <= 0 || targetHeight <= 0 then
            raise (ArgumentException("Target width and height must be positive."))

        use context = Context.CreateDefault()
        use accelerator = context.GetPreferredDevice(preferCPU=false).CreateAccelerator(context)

        let scaleX_ = float32 image.width / float32 targetWidth
        let scaleY_ = float32 image.height / float32 targetHeight

        let kernel (index: Index1D) (pixels: ArrayView<SKIDColor>) (output: ArrayView<SKIDColor>)
                   (srcWidth : int)(srcHeight:int)(scaleX:float32)(scaleY:float32)(targetWidth:int)  =
            let dstX = index.X % targetWidth
            let dstY = index.X / targetWidth

            let srcX = min (((float32 dstX + 0.5f) * scaleX) - 0.5f) (float32 (srcWidth - 2))
            let srcY = min (((float32 dstY + 0.5f) * scaleY) - 0.5f) (float32 (srcHeight - 2))

            let ix = int srcX
            let iy = int srcY
            let fx = srcX - float32 ix
            let fy = srcY - float32 iy

            let getColor i j =
                let x = clampInt 0 (srcWidth - 1) (ix + i)
                let y = clampInt 0 (srcHeight - 1) (iy + j)
                pixels.[y * srcWidth + x]

            let interpolate c0 c1 t = c0 * (1.0f - t) + c1 * t

            let c00 = getColor 0 0
            let c10 = getColor 1 0
            let c01 = getColor 0 1
            let c11 = getColor 1 1

            let r = interpolate (interpolate c00.r c10.r fx) (interpolate c01.r c11.r fx) fy
            let g = interpolate (interpolate c00.g c10.g fx) (interpolate c01.g c11.g fx) fy
            let b = interpolate (interpolate c00.b c10.b fx) (interpolate c01.b c11.b fx) fy
            let a = interpolate (interpolate c00.a c10.a fx) (interpolate c01.a c11.a fx) fy

            output.[index] <- SKIDColor(r, g, b, a)


        let resizedPixels = Array.zeroCreate<SKIDColor> (targetWidth * targetHeight)
        let inputBuffer = accelerator.Allocate1D(image.pixels)
        let outputBuffer = accelerator.Allocate1D(resizedPixels)

        let kernelLauncher = accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<SKIDColor>, ArrayView<SKIDColor>,int,int,float32,float32,int>(kernel)
        kernelLauncher.Invoke(outputBuffer.IntExtent, inputBuffer.View, outputBuffer.View,image.width,image.height,scaleX_,scaleY_,targetWidth)
        accelerator.Synchronize()
        let resizedPixels = outputBuffer.GetAsArray1D()
        inputBuffer.Dispose()
        outputBuffer.Dispose()

        // Return the resized image
        SKIDImage(resizedPixels, targetWidth, targetHeight)
   
        
            


    let inline generateNoneAlphaColor(color: SKIDColor) =
        SKIDColor(color.r, color.g, color.b, 1.0f)
    let inline filteringVaildColor (color: SKIDColor) =
        if color.a <= 0.0f then false else true
