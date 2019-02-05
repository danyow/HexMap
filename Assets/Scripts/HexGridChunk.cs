using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexGridChunk : MonoBehaviour
{
    public HexMesh terrain, rivers, roads, water, waterShore;
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
        roads.Clear();
        water.Clear();
        waterShore.Clear();
        for (int i = 0; i < cells.Length; i++)
        {
            Triangulate(cells[i]);
        }
        terrain.Apply();
        rivers.Apply();
        roads.Apply();
        water.Apply();
        waterShore.Apply();
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
            // TriangulateEdgeFan(center, edge, cell.color);
            TriangulateWithoutRiver(direction, cell, center, edge);
        }
        if (direction <= HexDirection.SE)
        {
            TriangulateConnection(direction, cell, edge);
        }

        if (cell.IsUnderwater)
        {
            TriangulateWater(direction, cell, center);
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
            if (!cell.IsUnderwater)
            {
                if (!neighbor.IsUnderwater)
                {
                    TriangulateRiverQuad(
                        edgeA.LM, edgeA.RM, edgeB.LM, edgeB.RM, 
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
                        cell.HasIncomingRiver && cell.IncomingRiver == direction
                    );
                }
                else if (cell.Elevation > neighbor.WaterLevel)
                {
                    TriangulateWaterfallInWater(edgeA.LM, edgeA.RM, edgeB.LM, edgeB.RM, cell.RiverSurfaceY, neighbor.RiverSurfaceY, neighbor.WaterSurfaceY);
                }
            }
            else if (!neighbor.IsUnderwater && neighbor.Elevation > neighbor.WaterLevel)
            {
                TriangulateWaterfallInWater(edgeB.RM, edgeB.LM, edgeA.RM, edgeA.LM, neighbor.RiverSurfaceY, cell.RiverSurfaceY, cell.WaterSurfaceY);
            }
        }

        if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
        {
            TriangulateEdgeTerraces(edgeA, cell, edgeB, neighbor, cell.HasRoadThroughEdge(direction));
        } 
        else
        {
            TriangulateEdgeStrip(edgeA, cell.color, edgeB, neighbor.color, cell.HasRoadThroughEdge(direction));
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

    private void TriangulateEdgeTerraces(EdgeVertices begin, HexCell beginCell, EdgeVertices end, HexCell endCell, bool hasRoad)
    {
        EdgeVertices edgeB = EdgeVertices.TerraceLerp(begin, end, 1);
        Color colorB = HexMetrics.TerraceLerp(beginCell.color, endCell.color, 1);

        TriangulateEdgeStrip(begin, beginCell.color, edgeB, colorB, hasRoad);

        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            EdgeVertices edgeA      = edgeB;
            Color   colorA = colorB;
            edgeB = EdgeVertices.TerraceLerp(begin, end, i);
            colorB = HexMetrics.TerraceLerp(beginCell.color, endCell.color, i);
            TriangulateEdgeStrip(edgeA, colorA, edgeB, colorB, hasRoad);
        }

        TriangulateEdgeStrip(edgeB, colorB, end, endCell.color, hasRoad);
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

    public void TriangulateEdgeStrip(EdgeVertices edgeA, Color colorA, EdgeVertices edgeB, Color colorB, bool hasRoad = false)
    {
        terrain.AddQuad(edgeA.L, edgeA.LM, edgeB.L, edgeB.LM);
        terrain.AddQuadColor(colorA, colorB);
        terrain.AddQuad(edgeA.LM, edgeA.M, edgeB.LM, edgeB.M);
        terrain.AddQuadColor(colorA, colorB);
        terrain.AddQuad(edgeA.M, edgeA.RM, edgeB.M, edgeB.RM);
        terrain.AddQuadColor(colorA, colorB);
        terrain.AddQuad(edgeA.RM, edgeA.R, edgeB.RM, edgeB.R);
        terrain.AddQuadColor(colorA, colorB);

        if (hasRoad)
        {
            TriangulateRoadSegment(edgeA.LM, edgeA.M, edgeA.RM, edgeB.LM, edgeB.M, edgeB.RM);
        }

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
        if (!cell.IsUnderwater)
        {
            bool reversed = cell.IncomingRiver == direction;

            TriangulateRiverQuad(centerL, centerR, m.LM, m.RM, cell.RiverSurfaceY, 0.4f, reversed);
            TriangulateRiverQuad(m.LM, m.RM, edge.LM, edge.RM, cell.RiverSurfaceY, 0.6f, reversed);
        }
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

        if (!cell.IsUnderwater)
        {
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
    }

    private void TriangulateAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices edge)
    {
        if (cell.HasRoads)
        {
            TriangulateRoadAdjacentToRiver(direction, cell, center, edge);
        }

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

    private void TriangulateRoadAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices edge)
    {
        bool hasRoadThroughEdge = cell.HasRoadThroughEdge(direction);
        bool previousHasRiver = cell.HasRiverThroughEdge(direction.Previous());
        bool nextHasRiver = cell.HasRiverThroughEdge(direction.Next());
        Vector2 interpolators = GetRoadInterpolators(direction, cell);
        Vector3 roadCenter = center;

        if (cell.HasRiverBeginOrEnd)
        {
            roadCenter += HexMetrics.GetSolidEdgeMiddle(cell.RiverBeginOrEndDirection.Opposite()) * (1f / 3f);
        }
        else if (cell.IncomingRiver == cell.OutgoingRiver.Opposite())
        {
            Vector3 corner;
            if (previousHasRiver)
            {
                if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Next()))
                {
                    return;
                }
                corner = HexMetrics.GetSecondSolidCorner(direction);
            }
            else
            {
                if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Previous()))
                {
                    return;
                }
                corner = HexMetrics.GetFirstSolidCorner(direction);
            }
            roadCenter += corner * 0.5f;
            center += corner * 0.5f;
        }
        else if (cell.IncomingRiver == cell.OutgoingRiver.Previous())
        {
            roadCenter -= HexMetrics.GetSecondCorner(cell.IncomingRiver) * 0.2f;
        }
        else if (cell.IncomingRiver == cell.OutgoingRiver.Next())
        {
            roadCenter -= HexMetrics.GetFirstCorner(cell.IncomingRiver) * 0.2f;
        }
        else if (previousHasRiver && nextHasRiver)
        {
            if (!hasRoadThroughEdge)
            {
                return;
            }
            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(direction) * HexMetrics.innerToOuter;
            roadCenter += offset * 0.7f;
            center += offset * 0.5f;
        }
        else
        {
            HexDirection middle;
            if (previousHasRiver)
            {
                middle = direction.Next();
            }
            else if (nextHasRiver)
            {
                middle = direction.Previous();
            }
            else
            {
                middle = direction;
            }
            if (
                !cell.HasRoadThroughEdge(middle) &&
                !cell.HasRoadThroughEdge(middle.Previous()) &&
                !cell.HasRoadThroughEdge(middle.Next())
            )
            {
                return;
            }
            roadCenter += HexMetrics.GetSolidEdgeMiddle(middle) * 0.25f;
        }
        Vector3 mL = Vector3.Lerp(roadCenter, edge.L, interpolators.x);
        Vector3 mR = Vector3.Lerp(roadCenter, edge.R, interpolators.y);

        if (previousHasRiver)
        {
            TriangulateRoadEdge(roadCenter, center, mL);
        }

        if (nextHasRiver)
        {
            TriangulateRoadEdge(roadCenter, mR, center);
        }
        TriangulateRoad(roadCenter, mL, mR, edge, hasRoadThroughEdge);
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

    private void TriangulateWithoutRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        TriangulateEdgeFan(center, e, cell.color);
        
        if (cell.HasRoads)
        {
            Vector2 interpolators = GetRoadInterpolators(direction, cell);
            TriangulateRoad(
                center, 
                Vector3.Lerp(center, e.L, interpolators.x), 
                Vector3.Lerp(center, e.R, interpolators.y), 
                e, 
                cell.HasRoadThroughEdge(direction)
            );
        }
    }

    private void TriangulateRoad(Vector3 center, Vector3 mL, Vector3 mR, EdgeVertices e, bool hasRoadThroughCellEdge)
    {
        /**
           e.L-------------e.R
             \  |++/|++/|  /
              \ |+/+|+/+| /
               \|/++|/++|/
               mL---mC--mR
                  \+|+/ 
                   \|/
                  center
         */

        if (hasRoadThroughCellEdge)
        {
            Vector3 mC = Vector3.Lerp(mL, mR, 0.5f);
            TriangulateRoadSegment(mL, mC, mR, e.LM, e.M, e.RM);
            roads.AddTriangle(center, mL, mC);
            roads.AddTriangle(center, mC, mR);
            roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));
        }
        else
        {
            TriangulateRoadEdge(center, mL, mR);
        }
    }

    private void TriangulateRoadSegment(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e, Vector3 f)
    {
        /**
                d   e   f
            |\  |\**|\**|\  |
            | \ |*\*|*\*| \ |
            |  \|**\|**\|  \|
                a   b   c
               0.0 1.0 0.0
         */
        roads.AddQuad(a, b, d, e);
        roads.AddQuad(b, c, e, f);

        roads.AddQuadUV(0f, 1f, 0f, 0f);
        roads.AddQuadUV(1f, 0f, 0f, 0f);
    }

    private void TriangulateRoadEdge(Vector3 center, Vector3 mL, Vector3 mR)
    {
        roads.AddTriangle(center, mL, mR);
        roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
    }


    private Vector2 GetRoadInterpolators(HexDirection direction, HexCell cell)
    {
        Vector2 interpolators;
        if (cell.HasRoadThroughEdge(direction))
        {
            interpolators.x = interpolators.y = 0.5f;
        }
        else
        {
            interpolators.x = cell.HasRoadThroughEdge(direction.Previous()) ? 0.5f : 0.25f;
            interpolators.y = cell.HasRoadThroughEdge(direction.Next()) ? 0.5f : 0.25f;
        }
        return interpolators;
    }

    private void TriangulateWater(HexDirection direction, HexCell cell, Vector3 center)
    {
        center.y = cell.WaterSurfaceY;

        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor != null && !neighbor.IsUnderwater)
        {
            TriangulateWaterShore(direction, cell, neighbor, center);
        }
        else
        {
            TriangulateOpenWater(direction, cell, neighbor, center);
        }
        return;
    }

    private void TriangulateOpenWater(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center)
    {   
        Vector3 c1 = center + HexMetrics.GetFirstWaterCorner(direction);
        Vector3 c2 = center + HexMetrics.GetSecondWaterCorner(direction);
        water.AddTriangle(center, c1, c2);

        if (direction <= HexDirection.SE && neighbor != null)
        {
            Vector3 bridge = HexMetrics.GetWaterBridge(direction);
            Vector3 e1 = c1 + bridge;
            Vector3 e2 = c2 + bridge;

            water.AddQuad(c1, c2, e1, e2);

            if (direction <= HexDirection.E)
            {
                HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
                if (nextNeighbor == null || !nextNeighbor.IsUnderwater)
                {
                    return;
                }
                water.AddTriangle(c2, e2, c2 + HexMetrics.GetWaterBridge(direction.Next()));
            }
        }
    }

     private void TriangulateWaterShore(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center)
    {
        EdgeVertices e1 = new EdgeVertices(
            center + HexMetrics.GetFirstWaterCorner(direction),
            center + HexMetrics.GetSecondWaterCorner(direction)
        );
        water.AddTriangle(center, e1.L, e1.LM);
        water.AddTriangle(center, e1.LM, e1.M);
        water.AddTriangle(center, e1.M, e1.RM);
        water.AddTriangle(center, e1.RM, e1.R);

        Vector3 bridge = HexMetrics.GetWaterBridge(direction);
        Vector3 center2 = neighbor.Position;
        center2.y = center.y;

        EdgeVertices e2 = new EdgeVertices(
            center2 + HexMetrics.GetSecondSolidCorner(direction.Opposite()),
            center2 + HexMetrics.GetFirstSolidCorner(direction.Opposite())
        );

        waterShore.AddQuad(e1.L, e1.LM, e2.L, e2.LM);
        waterShore.AddQuad(e1.LM, e1.M, e2.LM, e2.M);
        waterShore.AddQuad(e1.M, e1.RM, e2.M, e2.RM);
        waterShore.AddQuad(e1.RM, e1.R, e2.RM, e2.R);
        waterShore.AddQuadUV(0f, 0f, 0f, 1f);
        waterShore.AddQuadUV(0f, 0f, 0f, 1f);
        waterShore.AddQuadUV(0f, 0f, 0f, 1f);
        waterShore.AddQuadUV(0f, 0f, 0f, 1f);

        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (nextNeighbor != null)
        {
            Vector3 v3 = nextNeighbor.Position + (nextNeighbor.IsUnderwater ?
                HexMetrics.GetFirstWaterCorner(direction.Previous()) :
                HexMetrics.GetFirstSolidCorner(direction.Previous())
            );
            v3.y = center.y;
            waterShore.AddTriangle(e1.R, e2.R, v3);
            waterShore.AddTriangleUV(new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f));
        }

    }

    private void TriangulateWaterfallInWater(Vector3 b, Vector3 c, Vector3 d, Vector3 f, float y1, float y2, float waterY)
    {
        b.y = c.y = y1;
        d.y = f.y = y2;

        b = HexMetrics.Perturb(b);
        c = HexMetrics.Perturb(c);
        d = HexMetrics.Perturb(d);
        f = HexMetrics.Perturb(f);

        float t = (waterY  - y2) / (y1  - y2);
        d = Vector3.Lerp(d, b, t);
        f = Vector3.Lerp(f, c, t);
        rivers.AddQuadUnperturbed(b, c, d, f);
        rivers.AddQuadUV(0f, 1f, 0.8f, 1f);
    }


}
