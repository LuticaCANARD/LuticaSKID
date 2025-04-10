using LuticaSKID;
namespace LuticaSKIDBinder
{
    
    public static class LuticaSKIDBinder
    {
        public static Color[] MapColors(Color[] source, Color[] target)
        {
            return ColorMath.applyMapping(source, target);
        }
    }
}
