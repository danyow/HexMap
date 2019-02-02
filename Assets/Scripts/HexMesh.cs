using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    Mesh _mesh;
    // 顶点的列表
    List<Vector3> vertices;
    // 三角的列表
    List<int> triangles;
    // 颜色表
    List<Color> colors;

    MeshCollider meshCollider;

    private void Awake() {
        GetComponent<MeshFilter>().mesh = _mesh = new Mesh();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        _mesh.name   = "Hex Mesh";
        vertices     = new List<Vector3>();
        triangles    = new List<int>();
        colors       = new List<Color>();
    }

    // 三角化
    public void Triangulate(HexCell[] cells)
    {
        _mesh.Clear();
        vertices.Clear();
        triangles.Clear();
        colors.Clear();
        for (int i = 0; i < cells.Length; i++)
        {
            Triangulate(cells[i]);
        }
        _mesh.vertices  = vertices.ToArray();
        _mesh.triangles = triangles.ToArray();
        _mesh.colors    = colors.ToArray();
        _mesh.RecalculateNormals();

        meshCollider.sharedMesh = _mesh;
    }

    private void Triangulate(HexCell cell)
    {
        Vector3 center = cell.transform.localPosition;
        for (int i = 0; i < HexMetrics.corners.Length; i++)
        {
            int curIndex = i;
            int nextIndex = i + 1 == HexMetrics.corners.Length ? 0 :  i + 1;
            AddTriangle(
                center,
                center + HexMetrics.corners[curIndex],
                center + HexMetrics.corners[nextIndex]
            );
            AddTriangleColor(cell.color);
        }
    }

    private void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    private void AddTriangleColor(Color color)
    {
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
    }

}
