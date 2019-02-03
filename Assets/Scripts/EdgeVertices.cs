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

}
