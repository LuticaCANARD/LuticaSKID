namespace LuticaSKID

open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction
open System
module BoxedZoneEditAdaptor =

    // 추가이미지와 원본이미지의 사이즈 구분
    type StickingType = 
        | OriginStickingSize = 0 // 추가하는 텍스쳐의 해상도는 변경하지 않는다.
        | PreferenceStickingSize = 1 // 추가하는 텍스쳐의 해상도를 
    type MarkingImage = {
        image: SKIDImage
        zoneSize: SKIDPixelVector2
        stickingType: StickingType
        center: SKIDPixelVector2
        rotation: float32
    }
    type ResizedImage = {
        image: SKIDImage
        zoneSize: SKIDPixelVector2
        center: SKIDPixelVector2
    }
    type MarkedImageProcess<'t> = SKIDImage * 't * ResizedImage  -> SKIDImage

    [<Interface>]
    type ICanParticalImageProcesser<'t> = interface
        abstract member ProcessingPartically: MarkedImageProcess<'t> 
            with get
    end

    type BoxingProcesser() = 
        static member generateToBoxedImageStickier (partSetting:MarkingImage) : ResizedImage = 
            {
                image = BoxingProcesser.resizeImage partSetting
                zoneSize = partSetting.zoneSize
                center = partSetting.center
            }
        static member rotateImage (originImage: SKIDImage) (angle: float32) : SKIDImage =
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

                let rotatedPixels = Array.create (newWidth * newHeight) SKIDColor.Zero

                // Rotate and map pixels to new bounds
                for y in 0 .. originImage.height - 1 do
                    for x in 0 .. originImage.width - 1 do
                        let translatedX = float32 (x - centerX)
                        let translatedY = float32 (y - centerY)

                        let rotatedX = cosTheta * translatedX - sinTheta * translatedY
                        let rotatedY = sinTheta * translatedX + cosTheta * translatedY

                        let newX = int (rotatedX + float32 centerX) - minX
                        let newY = int (rotatedY + float32 centerY) - minY

                        if newX >= 0 && newX < newWidth && newY >= 0 && newY < newHeight then
                            rotatedPixels.[newY * newWidth + newX] <- originImage.pixels.[y * originImage.width + x]

                SKIDImage(rotatedPixels, newWidth, newHeight)
           

            
        static member private resizeImage (partSetting:MarkingImage) : SKIDImage =
            if partSetting.image.pixels.Length = 0 then
                raise (ArgumentException("The input image pixels cannot be empty."))
            elif partSetting.zoneSize.x <= 0 || partSetting.zoneSize.y <= 0 then
                raise (ArgumentException("The input image zone size cannot be less than or equal to zero."))
            else
                let width = partSetting.image.width
                let height = partSetting.image.height
                let zoneSize = partSetting.zoneSize                
                // 부착할 이미지의 해상도에 따라 결정한다.

                let targetWidth, targetHeight =
                    match partSetting.stickingType with
                    | StickingType.OriginStickingSize -> width, height
                    | StickingType.PreferenceStickingSize -> zoneSize.x, zoneSize.y
                    | _ -> raise (ArgumentException("Invalid sticking type."))

                let resizedPixels = resizeImage partSetting.image targetWidth targetHeight
                BoxingProcesser.rotateImage resizedPixels partSetting.rotation
        
        static member ExecuteImageAfterPartically (originInput:SKIDImage)(partSetting:MarkingImage)(processer:ICanParticalImageProcesser<'t>)(argu:'t): SKIDImage =
            processer.ProcessingPartically(originInput, argu, BoxingProcesser.generateToBoxedImageStickier partSetting)
            





            
         
    

        