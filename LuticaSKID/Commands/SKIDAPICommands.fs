﻿namespace LuticaSKID


open LuticaSKID.Models
open LuticaSKID.StructTypes
type ImageProcessCommand =
    | GenerateNormalMap of ImageProcessInput<NormalModule.NormalMapConfig>
    | GenerateMatcapMap of ImageProcessInput<MatcapModule.MatcapConfig>
    | GenerateNormalMapFromUV of ImageProcessInput<NormalModule.UVNormalMapMakeConfig>
    | GenerateAvgTexture of ImageProcessInput<ColorMath.ColorMoodOption>
    | ProcessImage of ImageProcessInput<Models.TextureImageProcessing.ImageProcessInputOption>
    | ProcessImageWithPartial of ImageProcessInput<Models.TextureImageProcessing.SimpleImageSynArgu>
    | ProcessHistogramEqualize of ImageProcessInput<Models.HistogramProcessor.histogramAnalyzeOption>
    | ProcessToHeightMap of ImageProcessInput<Models.HeightMapModel.HeightMapConfig>

type ImageAnalyzeCommand =
    | AnalyzeColorGroup of ImageProcessInput<Models.ColorGroupingModel.KmeansSetting>
    | AnalyzeHistogram of ImageProcessInput<Models.HistogramProcessor.histogramAnalyzeOption>

