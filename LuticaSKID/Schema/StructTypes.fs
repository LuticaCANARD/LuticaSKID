namespace LuticaSKID
open System.Runtime.InteropServices;
open System
module public StructTypes =
    [<StructLayout(LayoutKind.Sequential)>]
    [<Struct>]
    type SKIDColor =
        val r: float32
        val g: float32
        val b: float32
        val a: float32
        static member Transpaint = SKIDColor(SKIDConstants.MinValue, SKIDConstants.MinValue, SKIDConstants.MinValue, SKIDConstants.MinValue)
        static member FilteringNotVaildColorNumber(f:float32) =
            if f <= SKIDConstants.MinValue then SKIDConstants.MinValue elif f >= SKIDConstants.MaxValue then SKIDConstants.MaxValue else f

        /// <summary>
        /// SKIDColor의 생성자입니다. 색상 값은 0.0f ~ 1.0f 사이의 값이어야 합니다.
        /// </summary>
        new (_r, _g, _b, _a) = { 
            r = SKIDColor.FilteringNotVaildColorNumber _r; 
            g = SKIDColor.FilteringNotVaildColorNumber _g; 
            b = SKIDColor.FilteringNotVaildColorNumber _b; 
            a = SKIDColor.FilteringNotVaildColorNumber _a;
        }
        static member (+) (a: SKIDColor, b: SKIDColor) =
            SKIDColor( a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a )
        static member (-) (a: SKIDColor, b: SKIDColor) =
            SKIDColor(a.r - b.r, a.g - b.g, a.b - b.b, a.a - b.a )
        static member (*) (a: SKIDColor, b: SKIDColor) =
            SKIDColor(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a)
        static member (*) (a: SKIDColor, b: int) =
            SKIDColor(a.r * float32 b, a.g * float32 b, a.b * float32 b, a.a * float32 b)
        static member (/) (a: SKIDColor, b: SKIDColor)=
            SKIDColor(a.r / b.r, a.g / b.g, a.b / b.b, a.a / b.a)
        static member DivideByInt(a: SKIDColor, b: int): SKIDColor =
            SKIDColor(a.r / float32 b, a.g / float32 b, a.b / float32 b, a.a / float32 b)
        static member (*) (a: SKIDColor, b: float32) =
            SKIDColor(a.r * b, a.g * b, a.b * b, a.a * b)
        static member (+) (a: SKIDColor, b: float32) =
            SKIDColor(a.r + b, a.g + b, a.b + b, a.a + b)
        static member (-) (a: SKIDColor, b: float32) =
            SKIDColor(a.r - b, a.g - b, a.b - b, a.a - b)
        static member (/) (a: SKIDColor, b: float32) =
            SKIDColor(a.r / b, a.g / b, a.b / b, a.a / b)
        static member Zero =
            SKIDColor(SKIDConstants.MinValue, SKIDConstants.MinValue, SKIDConstants.MinValue, SKIDConstants.MinValue)
    [<StructLayout(LayoutKind.Sequential)>]
    [<Struct>]
    type SKIDVector3 =         
        val x: float32
        val y: float32
        val z: float32
        new (_x, _y, _z) = { x = _x; y = _y; z = _z }
        static member (+) (a: SKIDVector3, b: SKIDVector3) =
            SKIDVector3(a.x + b.x, a.y + b.y, a.z + b.z)
        static member (-) (a: SKIDVector3, b: SKIDVector3) =
            SKIDVector3(a.x - b.x, a.y - b.y, a.z - b.z)
        static member (*) (a: SKIDVector3, b: float32) =
            SKIDVector3(a.x * b, a.y * b, a.z * b)
        static member (*) (a: SKIDVector3, b: SKIDVector3) =
            SKIDVector3(a.x * b.x, a.y * b.y, a.z * b.z)
        static member (/) (a: SKIDVector3, b: float32): SKIDVector3=
            SKIDVector3(a.x / b, a.y / b, a.z / b)

    [<StructLayout(LayoutKind.Sequential)>]
    [<Struct>]
    type SKIDVector2 =
        val x: float32
        val y: float32
        new (_x, _y) = { x = _x; y = _y }
        static member (+) (a: SKIDVector2, b: SKIDVector2) =
            SKIDVector2(a.x + b.x, a.y + b.y)
        static member (-) (a: SKIDVector2, b: SKIDVector2) =
            SKIDVector2(a.x - b.x, a.y - b.y)
        static member (*) (a: SKIDVector2, b: float32) =
            SKIDVector2(a.x * b, a.y * b)
        static member (*) (a: SKIDVector2, b: SKIDVector2) =
            SKIDVector2(a.x * b.x, a.y * b.y)
        static member (/) (a: SKIDVector2, b: float32): Result<SKIDVector2, DivideByZeroException> =
            if b = 0.0f then Error (DivideByZeroException "Division by zero") else Ok (SKIDVector2(a.x / b, a.y / b))
    
    [<StructLayout(LayoutKind.Sequential)>]
    [<Struct>]
    type SKIDPixelVector2 =
        val x: int
        val y: int
        new (_x, _y) = { x = _x; y = _y }
        static member (+) (a: SKIDPixelVector2, b: SKIDPixelVector2) =
            SKIDPixelVector2(a.x + b.x, a.y + b.y)
        static member (-) (a: SKIDPixelVector2, b: SKIDPixelVector2) =
            SKIDPixelVector2(a.x - b.x, a.y - b.y)
        static member (*) (a: SKIDPixelVector2, b: int) =
            SKIDPixelVector2(a.x * b, a.y * b)
        static member (*) (a: SKIDPixelVector2, b: SKIDPixelVector2) =
            SKIDPixelVector2(a.x * b.x, a.y * b.y)
        static member (/) (a: SKIDPixelVector2, b: int): Result<SKIDPixelVector2, DivideByZeroException> =
            if b = 0 then Error (DivideByZeroException "Division by zero") else Ok (SKIDPixelVector2(a.x / b, a.y / b))

    [<Class>]
    type ColorSpaceBoundary =
        val mutable MinColor: SKIDColor
        val mutable MaxColor: SKIDColor
        new(
            c1:  SKIDColor,
            c2:  SKIDColor
        ) = {
            MinColor = new SKIDColor(min c1.r c2.r,min c1.g c2.g ,min c1.b c2.b ,min c1.a c2.a )
            MaxColor = new SKIDColor(max c1.r c2.r,max c1.g c2.g ,max c1.b c2.b ,max c1.a c2.a )
        }

        /// <summary>
        /// Check if the color is in the range of the color space
        /// </summary>
        member this.Contains(c: SKIDColor) =
            c.r >= this.MinColor.r && c.r <= this.MaxColor.r &&
            c.g >= this.MinColor.g && c.g <= this.MaxColor.g &&
            c.b >= this.MinColor.b && c.b <= this.MaxColor.b &&
            c.a >= this.MinColor.a && c.a <= this.MaxColor.a
        /// <summary>
        /// 이 경계의 중심 색상을 반환합니다.
        /// </summary>
        member this.BoderDomainColor() =
            let r = this.MaxColor.r + this.MinColor.r / 2.0f
            let g = this.MaxColor.g + this.MinColor.g / 2.0f
            let b = this.MaxColor.b + this.MinColor.b / 2.0f
            let a = this.MaxColor.a + this.MinColor.a / 2.0f
            SKIDColor(r, g, b, a)


    [<StructLayout(LayoutKind.Sequential)>]
    [<Struct>]
    type SKIDTexturePositionBorder =
        val minPoint: SKIDVector2
        val maxPoint: SKIDVector2
        val center: SKIDVector2
        new(
            p1:  SKIDVector2,
            p2:  SKIDVector2
        ) = 
            let minPoint_ = min p1 p2
            let maxPoint_ = max p1 p2
            let center_ = SKIDVector2((minPoint_.x + maxPoint_.x) / 2.0f, (minPoint_.y + maxPoint_.y) / 2.0f)
            {
                minPoint = minPoint_
                maxPoint = maxPoint_
                center = center_
            }

        new (center: SKIDVector2,width:float32,height:float32) = 
            let minPoint_ = SKIDVector2(center.x - width / 2.0f, center.y - height / 2.0f)
            let maxPoint_ = SKIDVector2(center.x + width / 2.0f, center.y + height / 2.0f)
            {
                minPoint = minPoint_
                maxPoint = maxPoint_
                center = center
            }
        member this.Contains(p: SKIDVector2) =
            p.x >= this.minPoint.x && p.x <= this.maxPoint.x &&
            p.y >= this.minPoint.y && p.y <= this.maxPoint.y




    [<Class>]
    type SKIDImage =
        val pixels: SKIDColor[]
        val width: int
        val height: int
        new(pixels: SKIDColor[], width: int, height: int) =
            { pixels = pixels; width = width; height = height }


    [<Class>]
    type ImageProcessInput<'TSetting> = 
        val image: SKIDImage
        val config: 'TSetting option
        new (image:SKIDImage, config: 'TSetting option) = { image = image; config = config }
        new (image:SKIDImage) = { image = image; config = None}
    [<Class>]
    type ImageProcessInput = 
        val image: SKIDImage
    type ImageProcessingOrder = 
    | NormalMap = 0
    | MatcapMap = 1
    | ColorMap = 2
    | MoodColorMap = 3
    | ColorMapping = 4
    | None = -1

    type ImageProcessTwoImage = 
    | Add = 0
    | Subtract = 1
    | Multiply = 2
    | Divide  = 3
    | Average = 4
    | ColorBlend = 5
    | ColorDifference = 6
    | TextureReplace = 7 // 텍스쳐 자체를 완벽하게 교체한다.



    type AnalyzeResult<'T>={
        result: 'T
    }   