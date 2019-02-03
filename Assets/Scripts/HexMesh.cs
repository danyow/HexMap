using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    Mesh _mesh;
    MeshCollider meshCollider;
    // 顶点的列表
    static List<Vector3> vertices = new List<Vector3>();
    // 三角的列表
    static List<int> triangles = new List<int>();
    // 颜色表
    static List<Color> colors = new List<Color>();

    private void Awake() {
        GetComponent<MeshFilter>().mesh = _mesh = new Mesh();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        _mesh.name   = "Hex Mesh";
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
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            Triangulate(d, cell);
        }
    }

    private void Triangulate(HexDirection direction, HexCell cell)
    {
        /**
            --d---f--e
             \|   |/
              b---c
               \ /
                a
            b = v1
            c = v2
            d = v3
            f = v4
            e = v5
         */ 
        
        Vector3 center = cell.Position;
        EdgeVertices edge = new EdgeVertices(
            center + HexMetrics.GetFirstSolidCorner(direction),
            center + HexMetrics.GetSecondSolidCorner(direction)
        );

        TriangulateEdgeFan(center, edge, cell.color);

        if (direction <= HexDirection.SE)
        {
            TriangulateConnection(direction, cell, edge);
        }

    }

    // 三角化连接处的两个桥组成的长方形
    private void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices edgeA)
    {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null)
        {
            return;
        }
        Vector3 bridge = HexMetrics.GetBridge(direction);

        bridge.y = neighbor.Position.y - cell.Position.y;

        EdgeVertices edgeB = new EdgeVertices(
            edgeA.L + bridge,
            edgeA.R + bridge
        );


        if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
        {
            TriangulateEdgeTerraces(edgeA, cell, edgeB, neighbor);
        } 
        else
        {
            TriangulateEdgeStrip(edgeA, cell.color, edgeB, neighbor.color);
        }


        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (direction <= HexDirection.E && nextNeighbor != null)
        {
            Vector3 e = edgeA.R + HexMetrics.GetBridge(direction.Next());
            e.y = nextNeighbor.Position.y;
            // 找的三角化的角部里面最低的cell是哪个
            if (cell.Elevation <= neighbor.Elevation)
            {
                if (cell.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulateCorner(edgeA.R, cell, edgeB.R, neighbor, e, nextNeighbor);
                }
                else
                {
                    TriangulateCorner(e, nextNeighbor, edgeA.R, cell, edgeB.R, neighbor);
                }
            } 
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulateCorner(edgeB.R, neighbor, e, nextNeighbor, edgeA.R, cell);
            } 
            else
            {
                TriangulateCorner(e, nextNeighbor, edgeA.R, cell, edgeB.R, neighbor);
            }
        }
    }

    private void TriangulateEdgeTerraces(EdgeVertices begin, HexCell beginCell, EdgeVertices end, HexCell endCell)
    {

        EdgeVertices edgeB = EdgeVertices.TerraceLerp(begin, end, 1);
        Color colorB = HexMetrics.TerraceLerp(beginCell.color, endCell.color, 1);

        TriangulateEdgeStrip(begin, beginCell.color, edgeB, colorB);

        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            EdgeVertices edgeA      = edgeB;
            Color   colorA = colorB;
            edgeB = EdgeVertices.TerraceLerp(begin, end, i);
            colorB = HexMetrics.TerraceLerp(beginCell.color, endCell.color, i);
            TriangulateEdgeStrip(edgeA, colorA, edgeB, colorB);
        }

        TriangulateEdgeStrip(edgeB, colorB, end, endCell.color);
    }

    private void TriangulateCorner(Vector3 bottom, HexCell bottomCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        /**
            b要为最低的 bottom
               L---R
                \B/
         */

        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if (leftEdgeType == HexEdgeType.Slope)
        {
            if (rightEdgeType == HexEdgeType.Slope)
            {
                TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }
            if (rightEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
            }
            TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
        } 
        else if (rightEdgeType == HexEdgeType.Slope)
        {
            if (leftEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
        }
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            if (leftCell.Elevation < rightCell.Elevation)
            {
                TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            else
            {
                TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
            }
        }
        else
        {
            AddTriangle(bottom, left, right);
            AddTriangleColor(bottomCell.color, leftCell.color, rightCell.color);
        }
    }

    // ssf 也就是说 lb为斜面 rb也为斜面的 lr为平面的情况
    private void TriangulateCornerTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        Vector3 d = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 f = HexMetrics.TerraceLerp(begin, right, 1);

        Color colorC = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);
        Color colorD = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, 1);

        AddTriangle(begin, d, f);
        AddTriangleColor(beginCell.color, colorC, colorD);


        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 b = d;
            Vector3 c = f;
            Color colorA = colorC;
            Color colorB = colorD;

            d = HexMetrics.TerraceLerp(begin, left, i);
            f = HexMetrics.TerraceLerp(begin, right, i);
            colorC = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
            colorD = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, i);
            AddQuad(b, c, d, f);
            AddQuadColor(colorA, colorB, colorC, colorD);
        }


        AddQuad(d, f, left, right);
        AddQuadColor(colorC, colorD, leftCell.color, rightCell.color);
    }

    private void TriangulateCornerTerracesCliff(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float   t             = Mathf.Abs(1f / (rightCell.Elevation - beginCell.Elevation));
        Vector3 boundary      = Vector3.Lerp(Perturb(begin), Perturb(right), t);
        Color   boundaryColor = Color.Lerp(beginCell.color, rightCell.color, t);

        TriangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
            AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }
    }

    private void TriangulateCornerCliffTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float   t             = Mathf.Abs(1f / (leftCell.Elevation - beginCell.Elevation));
        Vector3 boundary      = Vector3.Lerp(Perturb(begin), Perturb(left), t);
        Color   boundaryColor = Color.Lerp(beginCell.color, leftCell.color, t);

        TriangulateBoundaryTriangle(right, rightCell, begin, beginCell, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
            AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }
    }

    private void TriangulateBoundaryTriangle(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 boundary, Color boundaryColor)
    {
        Vector3 c = Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color colorB = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);

        AddTriangleUnperturbed(Perturb(begin), c, boundary);
        AddTriangleColor(beginCell.color, colorB, boundaryColor);

        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 b = c;
            Color colorA = colorB;
            c = Perturb(HexMetrics.TerraceLerp(begin, left, i));
            colorB = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
            AddTriangleUnperturbed(b, c, boundary);
            AddTriangleColor(colorA, colorB, boundaryColor);
        }

        AddTriangleUnperturbed(c, Perturb(left), boundary);
        AddTriangleColor(colorB, leftCell.color, boundaryColor);
    }

    private void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        a = Perturb(a);
        b = Perturb(b);
        c = Perturb(c);

        Debug.DrawLine(a, b, Color.black);
        Debug.DrawLine(b, c, Color.black);
        Debug.DrawLine(c, a, Color.black);

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
            d-----f
            |\    |
            | \   |
            |  \  |
            |   \ |
            |    \|
            b-----c             
         */

        b = Perturb(b);
        c = Perturb(c);
        d = Perturb(d);
        f = Perturb(f);

        Debug.DrawLine(b, c, Color.black);
        Debug.DrawLine(c, d, Color.black);
        Debug.DrawLine(d, f, Color.black);
        Debug.DrawLine(f, b, Color.black);
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

    private void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);   
        colors.Add(c4);   
    }

    private void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        Debug.DrawLine(v1, v2, Color.black);
        Debug.DrawLine(v2, v3, Color.black);
        Debug.DrawLine(v3, v1, Color.black);
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    private Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = HexMetrics.SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * HexMetrics.cellPerturbStrength;
        // position.y += (sample.y * 2f - 1f) * HexMetrics.cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * HexMetrics.cellPerturbStrength;
        return position;
    }

    public void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
    {
        AddTriangle(center, edge.L, edge.LM);
        AddTriangleColor(color);
        AddTriangle(center, edge.LM, edge.RM);
        AddTriangleColor(color);
        AddTriangle(center, edge.RM, edge.R);
        AddTriangleColor(color);
    }

    public void TriangulateEdgeStrip(EdgeVertices edgeA, Color colorA, EdgeVertices edgeB, Color colorB)
    {
        AddQuad(edgeA.L, edgeA.LM, edgeB.L, edgeB.LM);
        AddQuadColor(colorA, colorB);
        AddQuad(edgeA.LM, edgeA.RM, edgeB.LM, edgeB.RM);
        AddQuadColor(colorA, colorB);
        AddQuad(edgeA.RM, edgeA.R, edgeB.RM, edgeB.R);
        AddQuadColor(colorA, colorB);
    }

}
