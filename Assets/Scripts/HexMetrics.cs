using UnityEngine;

public static class HexMetrics
{
    // 外径半径
    public const float outerRadius = 10;                            
    // 内径半径 2分之根号3
    public const float innerRadius = outerRadius * 0.866025404f;    
    // XZ轴的平面
    public static Vector3[] corners = {
        new Vector3(0, 0, outerRadius),
        new Vector3(innerRadius, 0, outerRadius * 0.5f),
        new Vector3(innerRadius, 0, outerRadius * -0.5f),
        new Vector3(0, 0, -outerRadius),
        new Vector3(-innerRadius, 0, outerRadius * -0.5f),
        new Vector3(-innerRadius, 0, outerRadius * 0.5f),
    };

}
