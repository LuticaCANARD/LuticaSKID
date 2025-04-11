namespace LuticaSKID
open LuticaSKID.StructTypes;
module SKIDToolFunction =
    let inline distance (a: SKIDColor) (b: SKIDColor) =
        let dr, dg, db = a.r - b.r, a.g - b.g, a.b - b.b
        dr * dr + dg * dg + db * db // 유클리드 거리 (alpha 제외)
    let inline clampColorComponent (v: float32) = SKIDColor.FilteringNotVaildColorNumber v
    let inline clampInt (min: int) (max: int) (v: int) = if v < min then min elif v > max then max else v
    let inline flatten<'t> (arr2D: 't[,]) (_parallel : bool) =
        let w = arr2D.GetLength(0)
        let h = arr2D.GetLength(1)
        let gen = fun i -> arr2D.[i % w, i / w]
        if _parallel then Array.Parallel.init (w * h) gen else Array.init (w * h) gen
    let inline safeGet (arr: _[,]) x y =
        let w, h = arr.GetLength(0), arr.GetLength(1)
        if x < 0 || y < 0 || x >= w || y >= h then arr.[ clampInt 0 (w - 1) x, clampInt 0 (h - 1) y]
        else arr.[x, y]
    let inline flattenAutoParallel<'t> (arr2D: 't[,]) =
        flatten arr2D (arr2D.GetLength(0) > 255 || arr2D.GetLength(1) > 255)
    let inline computeGrayscale (color: SKIDColor) : float32 =
        0.299f * color.r + 0.587f * color.g + 0.114f * color.b
    let inline normalize (x: float32, y: float32, z: float32) : (float32 * float32 * float32) =
        let len = sqrt (x * x + y * y + z * z)
        if len = 0.0f then (0.0f, 0.0f, 0.0f) else (x / len, y / len, z / len)
    let inline clamp01 (v: float32) = SKIDColor.FilteringNotVaildColorNumber v
