using UnityEngine;

public struct EdgeVertices
{
   /**
    A-----B-----C-----D
    */
    public Vector3 L, LM, RM, R;

    public EdgeVertices(Vector3 left, Vector3 right)
    {
        L = left;
        LM = Vector3.Lerp(left, right, 1f / 3f);
        RM = Vector3.Lerp(left, right, 2f / 3f);
        R = right;
    }

    public static EdgeVertices TerraceLerp(EdgeVertices l1, EdgeVertices l2, int step)
    {
        EdgeVertices result;
        result.L  = HexMetrics.TerraceLerp(l1.L, l2.L, step);
        result.LM = HexMetrics.TerraceLerp(l1.LM, l2.LM, step);
        result.RM = HexMetrics.TerraceLerp(l1.RM, l2.RM, step);
        result.R  = HexMetrics.TerraceLerp(l1.R, l2.R, step);
        return result;
    }

}
