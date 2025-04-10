namespace LuticaSKID

open System.Runtime.InteropServices
open type StructTypes.SKIDColor;
[<ComVisible(true)>]
type ColorTools() =
    static member MapColors (src: StructTypes.SKIDColor[], dst: StructTypes.SKIDColor[]) : StructTypes.SKIDColor[] =
        ColorMath.applyMapping src dst
