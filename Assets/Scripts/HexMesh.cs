using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    public bool useCollider, useColors, useUVCoordinates;
    Mesh _mesh;
    MeshCollider meshCollider;

    // 顶点的列表
    [NonSerialized] List<Vector3> vertices;
    // 颜色表
    [NonSerialized] List<Color> colors;
    // 三角的列表
    [NonSerialized] List<int> triangles;
    // 贴图表
    [NonSerialized] List<Vector2> uvs;


    private void Awake() {
        GetComponent<MeshFilter>().mesh = _mesh = new Mesh();
        if (useCollider)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
        _mesh.name   = "Hex Mesh";
    }

    public void Clear()
    {
        _mesh.Clear();
        vertices  = ListPool<Vector3>.Get();
        if (useColors)
        {
            colors = ListPool<Color>.Get();
        }
        if (useUVCoordinates)
        {
            uvs = ListPool<Vector2>.Get();
        }
        triangles = ListPool<int>.Get();
    }

    public void Apply()
    {
        _mesh.SetVertices(vertices);
        ListPool<Vector3>.Add(vertices);
        if (useColors)
        {
            _mesh.SetColors(colors);
            ListPool<Color>.Add(colors);
        }
        if (useUVCoordinates)
        {
            _mesh.SetUVs(0, uvs);
            ListPool<Vector2>.Add(uvs);
        }
        _mesh.SetTriangles(triangles, 0);
        ListPool<int>.Add(triangles);
        _mesh.RecalculateNormals();
        if (useCollider)
        {
            meshCollider.sharedMesh = _mesh;
        }
    }

    public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        a = HexMetrics.Perturb(a);
        b = HexMetrics.Perturb(b);
        c = HexMetrics.Perturb(c);

        int vertexIndex = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    public void AddTriangleColor(Color color)
    {
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
    }

    public void AddTriangleColor(Color c1, Color c2, Color c3)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
    }

    public void AddQuad(Vector3 b, Vector3 c, Vector3 d, Vector3 f)
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

        b = HexMetrics.Perturb(b);
        c = HexMetrics.Perturb(c);
        d = HexMetrics.Perturb(d);
        f = HexMetrics.Perturb(f);

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

    public void AddQuadColor(Color c1)
    {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c1);   
        colors.Add(c1); 
    }

    public void AddQuadColor(Color c1, Color c2)
    {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c2);   
        colors.Add(c2);   
    }

    public void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);   
        colors.Add(c4);   
    }

    public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector3 uv3)
    {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
    }

    public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector3 uv3, Vector3 uv4)
    {
        uvs.Add(uv1);
        uvs.Add(uv2);
        uvs.Add(uv3);
        uvs.Add(uv4);
    }

    public void AddQuadUV(float uMin, float uMax, float vMin, float vMax)
    {
        uvs.Add(new Vector2(uMin, vMin));
        uvs.Add(new Vector2(uMax, vMin));
        uvs.Add(new Vector2(uMin, vMax));
        uvs.Add(new Vector2(uMax, vMax));
    }

}
