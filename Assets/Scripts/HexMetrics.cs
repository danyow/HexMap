using UnityEngine;

public static class HexMetrics
{
    public const int chunkSizeX = 5, chunkSizeZ = 5;
    // 外径转化为内径
    public const float outerToInner = 0.866025404f;
    // 内径转化为外径
    public const float innerToOuter = 1f / outerToInner;
    // 外径半径
    public const float outerRadius = 10f;
    // 内径半径 2分之根号3
    public const float innerRadius = outerRadius * outerToInner;
    // 纯色区域
    public const float solidFactor = 0.8f;
    // 混合区域
    public const float blendFactor = 1 - solidFactor;
    // 阶梯高度
    public const float elevationStep = 3;
    // 每个斜坡的平台数木
    public const int terracesPerSlope = 2;
    // 斜坡数
    public const int terraceSteps = terracesPerSlope * 2 + 1;
    // 水平插值值
    public const float horizontalTerraceStepSize = 1f / terraceSteps;
    // 垂直插值值
    public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);
    // 噪音源
    public static Texture2D noiseSource;
    // 微扰幅度
    public const float cellPerturbStrength = 0f;//4f;
    // 噪音覆盖区域大小
    public const float noiseScale = 0.003f;
    // 微扰高度
    public const float elevationPerturbStrength = 0f;
    // 河床的高度
    public const float streamBedElevationOffset = -1f;
    public const float riverSurfaceElevationOffset = -0.5f;

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
    
    public static Vector3 GetSolidEdgeMiddle(HexDirection direction)
    {
        return (corners[(int)direction] + corners[GetNextDirection(direction)]) * (0.5f * solidFactor);
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

    // Y坐标必须在奇数阶梯中改变 不能在偶数阶梯内改变
    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;

        float v = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;
        a.y += (b.y - a.y) * v;
        return a;
    }

    public static Color TerraceLerp(Color a, Color b, int step)
    {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }

    public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
    {
        if (elevation1 == elevation2)
        {
            return HexEdgeType.Flat;
        }
        if (Mathf.Abs(elevation2 - elevation1) == 1)
        {
            return HexEdgeType.Slope;   
        }
        return HexEdgeType.Cliff;
    }

    // 噪音取样的4D向量
    public static Vector4 SampleNoise(Vector3 position)
    {
        return noiseSource.GetPixelBilinear(
            position.x * noiseScale, 
            position.z * noiseScale
        );
    }

    public static Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * cellPerturbStrength;
        return position;
    }
}
