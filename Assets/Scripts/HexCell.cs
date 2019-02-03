using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCell : MonoBehaviour
{
    public HexGridChunk chunk;
    public HexCoordinates coordinates;
    private Color _color;
    public Color color
    {
        get { return _color;}
        set {
            if (_color == value)
            {
                return;
            }
            _color = value;
            Refresh();
        }
    }
    

    public RectTransform uiRect;
    private int elevation = int.MinValue;
    public int Elevation
    {
        get { return elevation;}
        set { 
            if (elevation == value)
            {
                return;
            }

            elevation = value;
            Vector3 pos = transform.localPosition;
            pos.y = value * HexMetrics.elevationStep;
            pos.y += (HexMetrics.SampleNoise(pos).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
            transform.localPosition = pos;

            // 这里的uiRect其实就已经是label的了
            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = -pos.y;
            uiRect.localPosition = uiPosition;
            Refresh();
        }
    }

    public Vector3 Position { 
        get {
            return transform.localPosition;
        } 
    }

    
    [SerializeField]
    HexCell[] neighbors;

    public HexCell GetNeighbor(HexDirection direction)
    {
        return neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighbors[(int)direction] = cell;
        // 对向的 
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        return HexMetrics.GetEdgeType(elevation, neighbors[(int)direction].elevation);
    }

    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
    }

    private void Refresh()
    {
        if (chunk)
        {
            chunk.Refresh();
            for (int i = 0; i < neighbors.Length; i++)
            {
                HexCell neighbor = neighbors[i];
                if (neighbor != null && neighbor.chunk != chunk)
                {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }
    
}
