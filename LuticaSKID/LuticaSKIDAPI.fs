namespace LuticaSKID
open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction
open System.Runtime.InteropServices
open System.Collections.Generic
    

[<ComVisible(true)>]
type LuticaSKIDAPI () =
   member this.Process(cmd: ImageProcessCommand) : SKIDImage =
       match cmd with
       | GenerateNormalMap(input) -> NormalModule.generateNormalMap input
       | GenerateMatcapMap(input) -> MatcapModule.generateMatcapMap input
       | GenerateNormalMapFromUV(input) -> NormalModule.generateNormalMapFromFBX input
       | GenerateAvgTexture(input) -> ColorMath.applyMoodColor input
       | ProcessImage(input) -> Models.TextureImageProcessing.Processer.Process input
       | ProcessImageWithPartial(input) -> 
           let partialImage = input.config.Value.partialImage
           let config = input.config.Value
           let processor = Models.TextureImageProcessing.Processer()
           BoxedZoneEditAdaptor.BoxingProcesser.ExecuteImageAfterPartically 
               input.image 
               partialImage 
               processor
               config
    member this.AnalyzingColorImage(cmd:ImageAnalyzeCommand) : AnalyzeResult<_> =
        match cmd with
            | AnalyzeColorGroup(input) -> 
                Models.ColorGroupingModel.Process.ExecuteKmeans input.image

                
