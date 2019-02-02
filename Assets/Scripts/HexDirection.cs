/** 
        *N*
    NW      NE
W               E
    SW      SE
        *S*

*/
public enum HexDirection
{
    NE, E, SE, SW, W, NW
}

// 扩展类
public static class HexDirectionExtensions
{
    public static HexDirection Opposite (this HexDirection direction)
    {
        return (int)direction < 3 ? (direction + 3) : (direction - 3);
    }
}