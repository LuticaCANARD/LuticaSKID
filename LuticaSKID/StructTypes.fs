namespace LuticaSKID
open System.Runtime.InteropServices;
module public StructTypes =
    [<StructLayout(LayoutKind.Sequential)>]
    [<Struct>]
    type public SKIDColor(_r: float32, _g: float32, _b: float32, _a: float32) =
        member _.r = _r
        member _.g = _g
        member _.b = _b
        member _.a = _a

   