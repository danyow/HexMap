using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    public int chunkCountX = 4, chunkCountZ = 3;
    private int cellCountX;
    private int cellCountZ;
    public Color defaultColor = Color.white;
    public HexGridChunk chunkPrefab;
    public HexCell cellPrefab;
    public Text cellLabelPrefab;
    // Canvas gridCanvas;
    // HexMesh hexMesh;
    HexGridChunk[] chunks;
    HexCell[] cells;
    public Texture2D noiseSource;

    private void Awake() 
    {
        HexMetrics.noiseSource = noiseSource;

        // gridCanvas = GetComponentInChildren<Canvas>();
        // hexMesh    = GetComponentInChildren<HexMesh>();

        cellCountX = chunkCountX * HexMetrics.chunkSizeX;
        cellCountZ = chunkCountZ * HexMetrics.chunkSizeZ;

        CreateChunks();
        CreateCells();
    }

    // private void Start() 
    // {
        // hexMesh.Triangulate(cells);
    // }

    private void OnEnable() 
    {
        HexMetrics.noiseSource = noiseSource;
    }

    private void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];
        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }

    private void CreateCells()
    {
        cells = new HexCell[cellCountZ * cellCountX];
        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    private void CreateCell(int x, int z, int i)
    {
        Vector3 pos;
        // pos.x = x * (HexMetrics.innerRadius * 2);
        // 这里 * 0.5是需要有斜的间隔 需要float的0.5的积 而z/2是需要把数字转化为int行 得到行数 向下取整还是
        // z/2 == Mathf.Floor(z * 0.5f)
        pos.x = (x + z * 0.5f - Mathf.Floor(z * 0.5f)) * (HexMetrics.innerRadius * 2); 
        pos.y = 0;
        pos.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
        // cell.transform.SetParent(transform, false);
        cell.transform.localPosition = pos;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.color = defaultColor;


        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }

        if (z > 0)
        {
            // z & 1 用来区分是偶数
            if ((z & 1 ) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                if (x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                }
            }
        }

        Text text = CreateCellLabel(x, z, i, pos, cell.coordinates.ToStringOnSeparateLines());
        cell.uiRect = text.rectTransform;
        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }

    private Text CreateCellLabel(int x, int z, int i, Vector3 pos, string text)
    {
        Text label = Instantiate<Text>(cellLabelPrefab);
        // label.rectTransform.SetParent(gridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(pos.x, pos.z);
        label.text = text;
        return label;
    }

    private void AddCellToChunk(int x, int z, HexCell cell)
    {
        int chunkX = x / HexMetrics.chunkSizeX;
        int chunkZ = z / HexMetrics.chunkSizeZ;
        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }

    public HexCell GetCell(Vector3 pos)
    {
        pos = transform.InverseTransformPoint(pos);
        HexCoordinates coordinates = HexCoordinates.FromPosition(pos);
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        return cells[index];
    }

    // public void Refresh()
    // {
    //     hexMesh.Triangulate(cells);
    // }

}
