namespace LuticaSKID
open System.Runtime.CompilerServices

[<System.Runtime.CompilerServices.Extension>]
[<Sealed>]
[<AbstractClass>]
type SKIDConstants() =
    
    static member WhiteMax
        with get() = 0.95f

    static member MinAlpha
        with get() = 0.01f

    static member ModelExtension
        with get() = ".skidmodel"

    static member SupportedFormat
        with get() = "*.png, *.jpg"