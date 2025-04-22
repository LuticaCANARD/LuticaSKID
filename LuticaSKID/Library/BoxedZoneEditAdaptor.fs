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
                rotateImage resizedPixels partSetting.rotation
        
        static member ExecuteImageAfterPartically (originInput:SKIDImage)(partSetting:MarkingImage)(processer:ICanParticalImageProcesser<'t>)(argu:'t): SKIDImage =
            processer.ProcessingPartically(originInput, argu, BoxingProcesser.generateToBoxedImageStickier partSetting)
            





            
         
    

        