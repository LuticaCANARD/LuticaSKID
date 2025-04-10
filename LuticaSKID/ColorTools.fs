namespace LuticaSKID

open System.Runtime.InteropServices
[<ComVisible(true)>]
type ColorTools() =
    static member MapColors (src: StructTypes.SKIDColor[], dst: StructTypes.SKIDColor[]) : StructTypes.SKIDColor[] =
        ColorMath.applyMapping src dst
    static member TakeMoodColorAndMap (src: StructTypes.SKIDColor[], dst: StructTypes.SKIDColor[]) : StructTypes.SKIDColor[] =
        let moodColor = ColorMath.GetMoodColorDefault src 
        ColorMath.applyMapping moodColor dst