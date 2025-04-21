module SKIDToolFunctionTests

//open Xunit
//open LuticaSKID
//open LuticaSKID.Schema.SKIDToolFunction

//[<Fact>]
//let ``Test cropImage with center outside originImageSize but image fully inside`` () =
//    let image = SKIDImage(Array.init 100 (fun _ -> SKIDColor(1.0f, 1.0f, 1.0f, 1.0f)), 10, 10)
//    let center = SKIDPixelVector2(15, 15)
//    let size = SKIDPixelVector2(5, 5)
//    let originImageSize = SKIDPixelVector2(20, 20)
//    let cropped = cropImage image center size originImageSize
//    Assert.Equal(5, cropped.width)
//    Assert.Equal(5, cropped.height)

//[<Fact>]
//let ``Test cropImage with center outside and part of image inside originImageSize`` () =
//    let image = SKIDImage(Array.init 100 (fun _ -> SKIDColor(1.0f, 1.0f, 1.0f, 1.0f)), 10, 10)
//    let center = SKIDPixelVector2(15, 15)
//    let size = SKIDPixelVector2(10, 10)
//    let originImageSize = SKIDPixelVector2(20, 20)
//    let cropped = cropImage image center size originImageSize
//    Assert.True(cropped.width > 0 && cropped.height > 0)

//[<Fact>]
//let ``Test cropImage with center inside and part of image outside originImageSize`` () =
//    let image = SKIDImage(Array.init 100 (fun _ -> SKIDColor(1.0f, 1.0f, 1.0f, 1.0f)), 10, 10)
//    let center = SKIDPixelVector2(5, 5)
//    let size = SKIDPixelVector2(10, 10)
//    let originImageSize = SKIDPixelVector2(8, 8)
//    let cropped = cropImage image center size originImageSize
//    Assert.Equal(8, cropped.width)
//    Assert.Equal(8, cropped.height)
