/** 
        *N*
    NW.6    NE.1
W.5             E.2
    SW.4    SE.3
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

    public static HexDirection Previous (this HexDirection direction)
    {
        return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
    }

    public static HexDirection Next (this HexDirection direction)
    {
        return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
    }

    public static HexDirection Previous2 (this HexDirection direction)
    {
        direction -= 2;
        return direction >= HexDirection.NE ? direction : (direction + 5);
    }

    public static HexDirection Next2 (this HexDirection direction)
    {
        direction += 2;
        return direction <= HexDirection.NW ? direction : (direction - 5);
    }

}