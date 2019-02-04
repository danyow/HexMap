using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexGridChunk : MonoBehaviour
{
    public HexMesh terrain, rivers;
    HexCell[] cells;
    Canvas gridCanvas;

    private void Awake() {
        gridCanvas = GetComponentInChildren<Canvas>();

        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
        ShowUI(false);
    }

    // Update is called once per frame
    public void Refresh()
    {
        enabled = true;
    }

    private void LateUpdate() {
        Triangulate();
        enabled = false;
    }

    public void AddCell(int index, HexCell cell)
    {
        cells[index] = cell;
        cell.chunk = this;
        cell.transform.SetParent(transform, false);
        cell.uiRect.SetParent(gridCanvas.transform, false);
    }

    public void ShowUI(bool visible)
    {
        gridCanvas.gameObject.SetActive(visible);
    }


        // 三角化
    public void Triangulate()
    {
        terrain.Clear();
        rivers.Clear();
        for (int i = 0; i < cells.Length; i++)
        {
            Triangulate(cells[i]);
        }
        terrain.Apply();
        rivers.Apply();
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

        if (cell.HasRiver)
        {
            if (cell.HasRiverThroughEdge(direction))
            {
                edge.M.y = cell.StreamBedY;
                if (cell.HasRiverBeginOrEnd)
                {
                    TriangulateWithRiverBeginOrEnd(direction, cell, center,edge);
                }
                else
                {
                    TriangulateWithRiver(direction, cell, center, edge);
                }
            }
            else
            {
                TriangulateAdjacentToRiver(direction, cell, center, edge);
            }
        }
        else
        {
            TriangulateEdgeFan(center, edge, cell.color);
        }
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

        if (cell.HasRiverThroughEdge(direction))
        {
            edgeB.M.y = neighbor.StreamBedY;
            TriangulateRiverQuad(
                edgeA.LM, edgeA.RM, edgeB.LM, edgeB.RM, 
                cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
                cell.HasIncomingRiver && cell.IncomingRiver == direction
            );
        }

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
            terrain.AddTriangle(bottom, left, right);
            terrain.AddTriangleColor(bottomCell.color, leftCell.color, rightCell.color);
        }
    }

    
    public void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
    {
        terrain.AddTriangle(center, edge.L, edge.LM);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.LM, edge.M);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.M, edge.RM);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.RM, edge.R);
        terrain.AddTriangleColor(color);
    }

    public void TriangulateEdgeStrip(EdgeVertices edgeA, Color colorA, EdgeVertices edgeB, Color colorB)
    {
        terrain.AddQuad(edgeA.L, edgeA.LM, edgeB.L, edgeB.LM);
        terrain.AddQuadColor(colorA, colorB);
        terrain.AddQuad(edgeA.LM, edgeA.M, edgeB.LM, edgeB.M);
        terrain.AddQuadColor(colorA, colorB);
        terrain.AddQuad(edgeA.M, edgeA.RM, edgeB.M, edgeB.RM);
        terrain.AddQuadColor(colorA, colorB);
        terrain.AddQuad(edgeA.RM, edgeA.R, edgeB.RM, edgeB.R);
        terrain.AddQuadColor(colorA, colorB);
    }

    // ssf 也就是说 lb为斜面 rb也为斜面的 lr为平面的情况
    private void TriangulateCornerTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        Vector3 d = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 f = HexMetrics.TerraceLerp(begin, right, 1);

        Color colorC = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);
        Color colorD = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, 1);

        terrain.AddTriangle(begin, d, f);
        terrain.AddTriangleColor(beginCell.color, colorC, colorD);


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
            terrain.AddQuad(b, c, d, f);
            terrain.AddQuadColor(colorA, colorB, colorC, colorD);
        }


        terrain.AddQuad(d, f, left, right);
        terrain.AddQuadColor(colorC, colorD, leftCell.color, rightCell.color);
    }

    private void TriangulateCornerTerracesCliff(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float   t             = Mathf.Abs(1f / (rightCell.Elevation - beginCell.Elevation));
        Vector3 boundary      = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), t);
        Color   boundaryColor = Color.Lerp(beginCell.color, rightCell.color, t);

        TriangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }
    }

    private void TriangulateCornerCliffTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float   t             = Mathf.Abs(1f / (leftCell.Elevation - beginCell.Elevation));
        Vector3 boundary      = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), t);
        Color   boundaryColor = Color.Lerp(beginCell.color, leftCell.color, t);

        TriangulateBoundaryTriangle(right, rightCell, begin, beginCell, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }
    }

    private void TriangulateBoundaryTriangle(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 boundary, Color boundaryColor)
    {
        Vector3 c = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color colorB = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);

        terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), c, boundary);
        terrain.AddTriangleColor(beginCell.color, colorB, boundaryColor);

        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 b = c;
            Color colorA = colorB;
            c = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            colorB = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
            terrain.AddTriangleUnperturbed(b, c, boundary);
            terrain.AddTriangleColor(colorA, colorB, boundaryColor);
        }

        terrain.AddTriangleUnperturbed(c, HexMetrics.Perturb(left), boundary);
        terrain.AddTriangleColor(colorB, leftCell.color, boundaryColor);
    }

    private void TriangulateWithRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices edge)
    {
        Vector3 centerL, centerR;
        if (cell.HasRiverThroughEdge(direction.Opposite()))
        {
            centerL = center + HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
            centerR = center + HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
        }
        else if (cell.HasRiverThroughEdge(direction.Next()))
        {
            centerL = center;
            centerR = Vector3.Lerp(center, edge.R, 2f / 3f);
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()))
        {
            centerL = Vector3.Lerp(center, edge.L, 2f / 3f);
            centerR = center;
        }
        else if (cell.HasRiverThroughEdge(direction.Next2()))
        {
            centerL = center;
            centerR = center + HexMetrics.GetSolidEdgeMiddle(direction.Next()) * (0.5f * HexMetrics.innerToOuter);
        }
        else
        {
            centerL = center + HexMetrics.GetSolidEdgeMiddle(direction.Previous()) * (0.5f * HexMetrics.innerToOuter);
            centerR = center;
        }
        // 取平均值 确定最后的中心
        center = Vector3.Lerp(centerL, centerR, 0.5f);

        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(centerL, edge.L, 0.5f),
            Vector3.Lerp(centerR, edge.R, 0.5f),
            1f / 6f
        );
        m.M.y = center.y = edge.M.y;

        TriangulateEdgeStrip(m, cell.color, edge, cell.color);

        terrain.AddTriangle(centerL, m.L, m.LM);
        terrain.AddTriangleColor(cell.color);
        terrain.AddQuad(centerL, center, m.LM, m.M);
        terrain.AddQuadColor(cell.color);
        terrain.AddQuad(center, centerR, m.M, m.RM);
        terrain.AddQuadColor(cell.color);
        terrain.AddTriangle(centerR, m.RM, m.R);
        terrain.AddTriangleColor(cell.color);

        bool reversed = cell.IncomingRiver == direction;

        TriangulateRiverQuad(centerL, centerR, m.LM, m.RM, cell.RiverSurfaceY, 0.4f, reversed);
        TriangulateRiverQuad(m.LM, m.RM, edge.LM, edge.RM, cell.RiverSurfaceY, 0.6f, reversed);
    }

    private void TriangulateWithRiverBeginOrEnd(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices edge)
    {
        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, edge.L, 0.5f),
            Vector3.Lerp(center, edge.R, 0.5f)
        );
        m.M.y = edge.M.y;
        TriangulateEdgeStrip(m, cell.color, edge, cell.color);
        TriangulateEdgeFan(center, m, cell.color);

        bool reversed = cell.HasIncomingRiver;
        TriangulateRiverQuad(m.LM, m.RM, edge.LM, edge.RM, cell.RiverSurfaceY, 0.6f, reversed);

        center.y = m.LM.y = m.RM.y = cell.RiverSurfaceY;
        rivers.AddTriangle(center, m.LM, m.RM);
        if (reversed)
        {
            rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f));
        }
        else
        {
            rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(0f, 0.6f), new Vector2(1f, 0.6f));
        }
    }

    private void TriangulateAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices edge)
    {
        if (cell.HasRiverThroughEdge(direction.Next()))
        {
            if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                center += HexMetrics.GetSolidEdgeMiddle(direction) * (HexMetrics.innerToOuter * 0.5f);
            }
            else if (cell.HasRiverThroughEdge(direction.Previous2()))
            {
                center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
            }
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()) && cell.HasRiverThroughEdge(direction.Next2()))
        {
            center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
        }

        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, edge.L, 0.5f),
            Vector3.Lerp(center, edge.R, 0.5f)
        );
        TriangulateEdgeStrip(m, cell.color, edge, cell.color);
        TriangulateEdgeFan(center, m, cell.color);
    }

    private void TriangulateRiverQuad(Vector3 b, Vector3 c, Vector3 d, Vector3 f, float y, float v, bool reversed)
    {
        TriangulateRiverQuad(b, c, d, f, y, y, v, reversed);
    }

    private void TriangulateRiverQuad(Vector3 b, Vector3 c, Vector3 d, Vector3 f, float y1, float y2, float v, bool reversed)
    {
        b.y = c.y = y1;
        d.y = f.y = y2;
        rivers.AddQuad(b, c, d, f);
        if (reversed)
        {
            rivers.AddQuadUV(1, 0, 0.8f - v, 0.6f - v);
        }
        else
        {
            rivers.AddQuadUV(0, 1, v, v + 0.2f);
        }
    }
}
