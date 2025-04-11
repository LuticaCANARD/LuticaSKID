﻿namespace LuticaSKID
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
        new (_r, _g, _b, _a) = { 
            r = SKIDColor.FilteringNotVaildColorNumber _r; 
            g = SKIDColor.FilteringNotVaildColorNumber _g; 
            b = SKIDColor.FilteringNotVaildColorNumber _b; 
            a = SKIDColor.FilteringNotVaildColorNumber _a;
        }
        static member (+) (a: SKIDColor, b: SKIDColor) =
            SKIDColor(
                a.r + b.r, a.g + b.g, a.b + b.b,a.a + b.a
            )
        static member (-) (a: SKIDColor, b: SKIDColor) =
            SKIDColor(
                a.r - b.r, a.g - b.g, a.b - b.b,a.a - b.a
            )
        static member (*) (a: SKIDColor, b: SKIDColor) =
            SKIDColor(
                a.r * b.r, a.g * b.g, a.b * b.b,a.a * b.a
            )
        static member (/) (a: SKIDColor, b: float32): Result<SKIDColor, DivideByZeroException> =
            if b = 0.0f then
                Error (DivideByZeroException "Division by zero")
            else
                Ok (SKIDColor(a.r / b, a.g / b, a.b / b, a.a / b))
        
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




   