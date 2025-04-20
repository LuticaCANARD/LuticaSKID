namespace LuticaSKID

open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction

module BoxedZoneEditAdaptor =

    // 추가이미지와 원본이미지의 사이즈 구분
    type StickingType = 
        | OriginStickingSize = 0 // 추가하는 텍스쳐의 해상도는 변경하지 않는다.
        | PreferenceStickingSize = 1 // 추가하는 텍스쳐의 해상도를 
    type MarkingImage = {
        image: SKIDImage
        borderSize : SKIDPixelVector2
        zoneSize: SKIDPixelVector2
        stickingType: StickingType
    }
    let generateToBoxedImageStickier (originImage:SKIDImage) (partSetting:MarkingImage) : SKIDImage = 

            let res = new SKIDImage(Array.create (originImage.width * originImage.height) (SKIDColor(0.0f, 0.0f, 0.0f, 1.0f)), originImage.width, originImage.height)
            let addtionImageWidth = partSetting.image.width
            let addtionImageHeight = partSetting.image.height
            let borderSize = partSetting.borderSize
            let zoneSize = partSetting.zoneSize
            let stickingType = partSetting.stickingType
            let startX = borderSize.x
            let startY = borderSize.y



            res
         
    

        