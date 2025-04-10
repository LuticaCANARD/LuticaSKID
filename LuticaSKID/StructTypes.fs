namespace LuticaSKID
open System.Runtime.InteropServices;
module public StructTypes =
    [<StructLayout(LayoutKind.Sequential)>]
    [<Struct>]
    type SKIDColor =
        val r: float32
        val g: float32
        val b: float32
        val a: float32
        new (r, g, b, a) = { r = r; g = g; b = b; a = a }

   