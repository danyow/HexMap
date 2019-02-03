using UnityEngine;

public struct EdgeVertices
{
    public Vector3 L, LM, M, RM, R;

    public EdgeVertices(Vector3 left, Vector3 right)
    {
        L  = left;
        LM = Vector3.Lerp(left, right, 0.25f);
        M  = Vector3.Lerp(left, right, 0.50f);
        RM = Vector3.Lerp(left, right, 0.75f);
        R  = right;
    }

        public EdgeVertices(Vector3 left, Vector3 right, float outerStep)
    {
        L  = left;
        LM = Vector3.Lerp(left, right, outerStep);
        M  = Vector3.Lerp(left, right, 0.50f);
        RM = Vector3.Lerp(left, right, 1f - outerStep);
        R  = right;
    }

    public static EdgeVertices TerraceLerp(EdgeVertices l1, EdgeVertices l2, int step)
    {
        EdgeVertices result;
        result.L  = HexMetrics.TerraceLerp(l1.L, l2.L, step);
        result.LM = HexMetrics.TerraceLerp(l1.LM, l2.LM, step);
        result.M  = HexMetrics.TerraceLerp(l1.M, l2.M, step);
        result.RM = HexMetrics.TerraceLerp(l1.RM, l2.RM, step);
        result.R  = HexMetrics.TerraceLerp(l1.R, l2.R, step);
        return result;
    }

}
