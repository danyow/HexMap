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
        // Vector3 center = cell.transform.localPosition;
        // for (int i = 0; i < HexMetrics.corners.Length; i++)
        // {
        //     int curIndex = i;
        //     int nextIndex = i + 1 == HexMetrics.corners.Length ? 0 :  i + 1;
        //     AddTriangle(
        //         center,
        //         center + HexMetrics.corners[curIndex],
        //         center + HexMetrics.corners[nextIndex]
        //     );
        //     AddTriangleColor(cell.color);
        // }
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            Triangulate(d, cell);
        }
    }

    private void Triangulate(HexDirection direction, HexCell cell)
    {
        Vector3 center = cell.transform.localPosition;
        Vector3 a = center;
        Vector3 b = center + HexMetrics.GetFirstSolidCorner(direction);
        Vector3 c = center + HexMetrics.GetSecondSolidCorner(direction);

        AddTriangle(a, b, c);
        AddTriangleColor(cell.color);

        Vector3 bridge = HexMetrics.GetBridge(direction);
        Vector3 d = b + bridge;
        Vector3 f = c + bridge;

        AddQuad(b, c, d, f);


        HexCell prevNeighbor = cell.GetNeighbor(direction.Previous()) ?? cell;
        HexCell neighbor     = cell.GetNeighbor(direction) ?? cell;
        HexCell nextNeighbor = cell.GetNeighbor(direction.Next()) ?? cell;

        AddQuadColor(
            cell.color,
            (cell.color + neighbor.color) * 0.5f
        );
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

    private void AddTriangleColor(Color c1, Color c2, Color c3)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
    }


    private void AddQuad(Vector3 b, Vector3 c, Vector3 d, Vector3 f)
    {

        /**
            d-------f
             \     /
              b---c
               \ /
                a
         */ 

        int vertexIndex = vertices.Count;
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);
        vertices.Add(f);

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }

    private void AddQuadColor(Color c1, Color c2)
    {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c2);   
        colors.Add(c2);   
    }

}
