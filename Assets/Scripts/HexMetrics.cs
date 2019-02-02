using UnityEngine;

public static class HexMetrics
{
    // 外径半径
    public const float outerRadius = 10;                            
    // 内径半径 2分之根号3
    public const float innerRadius = outerRadius * 0.866025404f;
    // 纯色区域
    public const float solidFactor = 0.75f;
    // 混合区域
    public const float blendFactor = 1 - solidFactor;
    // 阶梯高度
    public const float elevationStep = 5;


    // XZ轴的平面
    public static Vector3[] corners = {
        new Vector3(0,            0, outerRadius * 1),
        new Vector3(innerRadius,  0, outerRadius * 0.5f),
        new Vector3(innerRadius,  0, outerRadius * -0.5f),
        new Vector3(0,            0, outerRadius * -1),
        new Vector3(-innerRadius, 0, outerRadius * -0.5f),
        new Vector3(-innerRadius, 0, outerRadius * 0.5f),
    };

    public static int GetNextDirection(HexDirection direction)
    {
        if ((int)direction + 1 == HexMetrics.corners.Length)
        {
            return 0;
        }
        return (int)direction + 1;
    }

    public static Vector3 GetFirstCorner (HexDirection direction)
    {
        return corners[(int)direction];
    }

    public static Vector3 GetSecondCorner (HexDirection direction)
    {
        return corners[GetNextDirection(direction)];
    }

    public static Vector3 GetFirstSolidCorner (HexDirection direction)
    {
        return corners[(int)direction] * solidFactor;
    }

    public static Vector3 GetSecondSolidCorner (HexDirection direction)
    {
        return corners[GetNextDirection(direction)] * solidFactor;
    }

    public static Vector3 GetBridge(HexDirection direction)
    {
        /** 桥的进化 
            1.!-- 原先的时候 是每个六边形的一边内有个桥
            2.!-- 现在将每边相邻的两个桥合成一个长方形 即直接乘以2即可
        */ 
        
        // return (corners[(int)direction] + corners[GetNextDirection(direction)]) * 0.5f * blendFactor;
        return (corners[(int)direction] + corners[GetNextDirection(direction)]) * blendFactor;
    }

}
