namespace LuticaSKID
open LuticaSKID.StructTypes
open LuticaSKID.SKIDToolFunction
open System.Runtime.InteropServices
[<ComVisible(true)>]
type ColorTools() =
    static member MapColors (src: SKIDColor[], dst: SKIDColor[]) : SKIDColor[] =
        ColorMath.applyMapping src dst
    static member TakeMoodColorAndMap (src: SKIDColor[], dst: SKIDColor[]) : SKIDColor[] =
        let moodColor = ColorMath.GetMoodColorDefault src 
        ColorMath.applyMapping moodColor dst
    static member GenerateNormalMap(src: SKIDColor[],width:int,height:int,xFactor:float32,yFactor:float32) : SKIDColor[] =
        Array2D.init width height (fun x y -> src.[x + y * width]) 
        |> NormalModule.generateNormalMap (xFactor,yFactor)
        |> flattenAutoParallel

        