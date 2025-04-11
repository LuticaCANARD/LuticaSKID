namespace LuticaSKID
open System.Runtime.CompilerServices

[<System.Runtime.CompilerServices.Extension>]
[<Sealed>]
[<AbstractClass>]
type SKIDConstants() =
    
    static member MaxValue
        with get() = 1.f

    static member MinValue
        with get() = 0.0f

    static member ModelExtension
        with get() = ".skidmodel"

    static member SupportedFormat
        with get() = "*.png, *.jpg"