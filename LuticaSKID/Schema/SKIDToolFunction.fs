﻿namespace LuticaSKID
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
    let inline resizeImageFromVector (image: SKIDImage) (targetSize:SKIDPixelVector2) : SKIDImage = 
        resizeImage image targetSize.x targetSize.y
    let inline generateNoneAlphaColor(color: SKIDColor) =
        SKIDColor(color.r, color.g, color.b, 1.0f)
    let inline filteringVaildColor (color: SKIDColor) =
        if color.a <= 0.0f then false else true


    let inline cropImage (image: SKIDImage) (center: SKIDPixelVector2) (size: SKIDPixelVector2)(originImageSize:SKIDPixelVector2) : SKIDImage =
        if size.x < 0 || size.y < 0 then
            raise (ArgumentException("Target width and height must be positive."))
        let width = size.x
        let height = size.y
        // TODO : 중심이 이미지의 바깥에 있을 경우에 대한 처리는 사후에 한다.
        let distantXToBorderPositive = originImageSize.x - center.x 
        let distantXToBorderNegative = center.x
        let distantYToBorderPositive = originImageSize.y - center.y
        let distantYToBorderNegative = center.y

        let getPosition = fun(index:int)-> 
            let x = index % image.width
            let y = index / image.width
            SKIDPixelVector2(x,y)

        let filteringOverBoder = fun (v:SKIDPixelVector2) -> 
            let x = v.x
            let y = v.y
            if x < 0 || x >= image.width || y < 0 || y >= image.height then false else true

        let filteredPixels = 
            image.pixels 
            |> Array.mapi (fun index pix -> 
                let v = getPosition index
                if filteringOverBoder v then pix else SKIDColor(0.0f, 0.0f, 0.0f, 0.0f))
            |> Array.Parallel.filter filteringVaildColor
            |> Array.Parallel.map (fun pix -> 
                pix 
                |> fun p -> SKIDColor(clampColorComponent p.r, clampColorComponent p.g, clampColorComponent p.b, clampColorComponent p.a))
        let newWidth = min width distantXToBorderPositive + distantXToBorderNegative
        let newHeight = min height distantYToBorderPositive + distantYToBorderNegative    

        SKIDImage(filteredPixels, newWidth, newHeight)
    let inline generateCroppedImage(image: SKIDImage) (deployPoint: SKIDPixelVector2) (size: SKIDPixelVector2) : SKIDImage =
    // 새 이미지에 맞는 기존 이미지의 마스크를 생성함.
        let newPixels = Array.init(int (size.x * size.y)) (fun i -> 

            // Calculate the x and y coordinates in the new image
            let x = i % size.x
            let y = i / size.x
            let transformedX = deployPoint.x + x + image.width/2
            let transformedY = deployPoint.y + y + image.height/2

            if transformedX < 0 || transformedX >= image.width || transformedY < 0 || transformedY >= image.height then
                SKIDColor(0.0f, 0.0f, 0.0f, 0.0f)
            else
                let origin_image_index = transformedX + transformedY * image.width
                image.pixels.[origin_image_index]
            )
        SKIDImage(newPixels, int size.x, int size.y)

    let inline rotateImage (originImage:SKIDImage)(angle:float32) : SKIDImage =
        if originImage.pixels.Length = 0 then
                raise (ArgumentException("The input image pixels cannot be empty."))
        else
            let radians = float (angle) * Math.PI / 180.0
            let cosTheta = float32 (Math.Cos(radians))
            let sinTheta = float32 (Math.Sin(radians))

            let centerX = originImage.width / 2
            let centerY = originImage.height / 2

            let mutable minX, minY = Int32.MaxValue, Int32.MaxValue
            let mutable maxX, maxY = Int32.MinValue, Int32.MinValue

            // Calculate new bounds
            for y in 0 .. originImage.height - 1 do
                for x in 0 .. originImage.width - 1 do
                    let translatedX = float32 (x - centerX)
                    let translatedY = float32 (y - centerY)

                    let rotatedX = cosTheta * translatedX - sinTheta * translatedY
                    let rotatedY = sinTheta * translatedX + cosTheta * translatedY

                    let newX = int (rotatedX + float32 centerX)
                    let newY = int (rotatedY + float32 centerY)

                    minX <- min minX newX
                    minY <- min minY newY
                    maxX <- max maxX newX
                    maxY <- max maxY newY

            let newWidth = maxX - minX + 1
            let newHeight = maxY - minY + 1
            // Rotate and map pixels to new bounds
            let rotatedPixels = Array.Parallel.init (newWidth * newHeight) (fun i -> 
                let x = i % newWidth
                let y = i / newWidth
                let translatedX = float32 (x + minX - centerX)
                let translatedY = float32 (y + minY - centerY)
                let rotatedX = cosTheta * translatedX - sinTheta * translatedY
                let rotatedY = sinTheta * translatedX + cosTheta * translatedY

                let newX = int (rotatedX + float32 centerX)
                let newY = int (rotatedY + float32 centerY)
                if newX >= 0 && newX < originImage.width && newY >= 0 && newY < originImage.height then
                    originImage.pixels.[newY * originImage.width + newX]
                else
                    SKIDColor(0.0f, 0.0f, 0.0f, 0.0f) // Transparent color for out-of-bounds pixels
            )
            SKIDImage(rotatedPixels, newWidth, newHeight)

    let inline maskingImage (image: SKIDImage) (mask: SKIDImage) : SKIDImage =
        if image.width <> mask.width || image.height <> mask.height then
            raise (ArgumentException("The input image and mask must have the same dimensions."))
        let maskedPixels = Array.Parallel.init (image.pixels.Length) (fun i ->
            let imgPixel = image.pixels.[i]
            let maskPixel = mask.pixels.[i]
            if maskPixel.a > 0.0f then imgPixel else SKIDColor(0.0f, 0.0f, 0.0f, 0.0f)
        )
        SKIDImage(maskedPixels, image.width, image.height)