namespace LuticaSKID
open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction
open System.Runtime.InteropServices
open System.Collections.Generic
open ILGPU
open ILGPU.Runtime

type AnalyzeResultTypes =     
    | ColorGroupingResult of Models.ColorGroupingModel.ColorGroupiongAnlyzeResult
    | HistogramResult of Models.HistogramProcessor.histogramAnalyzeResult


[<ComVisible(true)>]
type LuticaSKIDAPI () =
   //member this.ilgpuContext = Context.CreateDefault()
   //
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
       | ProcessHistogramEqualize(input) -> 
              let histogram = Models.HistogramProcessor.Process.makeHistogram input.image input.config.Value
              Models.HistogramProcessor.Process.imageHistogramEqualize input.image histogram.result

    member this.AnalyzingColorImage(cmd:ImageAnalyzeCommand) : AnalyzeResultTypes =
        match cmd with
            | AnalyzeColorGroup(input) -> 
                Models.ColorGroupingModel.Process.ExecuteKmeans input.image input.config.Value.maxK input.config.Value.maxIter 
                    |> AnalyzeResultTypes.ColorGroupingResult 
            | AnalyzeHistogram(input) -> 
                Models.HistogramProcessor.Process.makeHistogram input.image input.config.Value 
                    |> AnalyzeResultTypes.HistogramResult
