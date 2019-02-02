using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    public int width = 6;
    public int height = 6;
    public Color defaultColor = Color.white;
    public Color touchedColor = Color.magenta;


    public HexCell cellPrefab;
    public Text cellLabelPrefab;
    Canvas gridCanvas;
    HexMesh hexMesh;

    HexCell[] cells;

    private void Awake() {

        gridCanvas = GetComponentInChildren<Canvas>();
        hexMesh = GetComponentInChildren<HexMesh>();

        cells = new HexCell[height * width];
        for (int z = 0, i = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    private void Start() {
        hexMesh.Triangulate(cells);
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
        cell.transform.SetParent(transform, false);
        cell.transform.localPosition = pos;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.color = defaultColor;

        CreateCellLabel(x, z, i, pos, cell.coordinates.ToStringOnSeparateLines());
    }

    private void CreateCellLabel(int x, int z, int i, Vector3 pos, string text)
    {
        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.SetParent(gridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(pos.x, pos.z);
        label.text = text;
    }

    private void Update() 
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleInput();
        }    
    }


    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            TouchCell(hit.point);
        }
    }

    void TouchCell(Vector3 pos)
    {
        // ColorCell(pos, touchedColor);
    }


    public void ColorCell(Vector3 pos, Color color)
    {
        pos = transform.InverseTransformPoint(pos);

        HexCoordinates coordinates = HexCoordinates.FromPosition(pos);
        int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
        HexCell cell = cells[index];
        cell.color = color;
        hexMesh.Triangulate(cells);

    }

}
