using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;
using UnityEditor;

[System.Serializable]
public class HexagonCellPrimitive : MonoBehaviour
{

    [MenuItem("MyMenu/Delete Selected Object")]
    public static void DeleteSelectedObject()
    {
        if (Selection.activeGameObject != null)
        {
            UnityEditor.EditorApplication.ExecuteMenuItem("Edit/Delete");
        }
    }

    [SerializeField] private int _id = -1;
    public int GetID() => _id;
    public void SetID(int id)
    {
        _id = id;
    }
    public int size = 12;
    public Vector3[] _cornerPoints;
    public Vector3[] _sides;
    public List<HexagonCellPrimitive> _neighbors;
    public HexagonCellPrimitive[] neighborsBySide = new HexagonCellPrimitive[6];
    public int GetNeighborsRelativeSide(HexagonSides side)
    {
        if (neighborsBySide[(int)side] == null) return -1;

        for (int neighborSide = 0; neighborSide < 6; neighborSide++)
        {
            if (neighborsBySide[(int)side].neighborsBySide[neighborSide] == this)
            {
                return neighborSide;
            }
        }
        return -1;
    }
    public bool isEdgeCell { private set; get; }
    public void SetEdgeCell(bool enable)
    {
        isEdgeCell = enable;
    }

    private void RecalculateEdgePoints()
    {
        _cornerPoints = ProceduralTerrainUtility.GenerateHexagonPoints(transform.position, size);
        _sides = HexagonGenerator.GenerateHexagonSidePoints(_cornerPoints);
    }

    private void Awake()
    {
        RecalculateEdgePoints();
    }

    private void Start()
    {
        RecalculateEdgePoints();
    }

    void OnValidate()
    {
        if (resetPoints || _currentPosition != transform.position || _currentSize != size || _cornerPoints == null || _cornerPoints.Length == 0)
        {
            resetPoints = false;
            _currentPosition = transform.position;
            _currentSize = size;
            RecalculateEdgePoints();
        }

        if (showNeighbors)
        {
            if (_neighbors == null || _neighbors.Count == 0)
            {
                showNeighbors = false;
                return;
            }
        }
    }

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugMode = true;
    [SerializeField] private bool showCenter;
    [SerializeField] private bool showNeighbors;
    [SerializeField] private bool showSides;
    [SerializeField] private bool showCorners;
    [SerializeField] private bool showEdges;
    [SerializeField] private bool resetPoints;
    public bool highlight;

    #region Saved State
    private Vector3 _currentPosition;
    private int _currentSize;
    #endregion

    private void OnDrawGizmos()
    {
        if (!enableDebugMode) return;

        if (_currentPosition != transform.position)
        {
            OnValidate();
        }

        if (showCenter)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(transform.position, 0.3f);
        }

        if (highlight)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, 6f);
        }

        if (showSides && _sides != null)
        {
            for (int i = 0; i < _sides.Length; i++)
            {
                Gizmos.color = Color.yellow;
                Vector3 pos = _sides[i];
                Gizmos.DrawSphere(pos, 1f);
            }
        }

        if (showCorners)
        {
            Gizmos.color = Color.magenta;
            foreach (Vector3 item in _cornerPoints)
            {
                Gizmos.DrawSphere(item, 0.3f);
            }
        }

        if (showEdges)
        {
            Gizmos.color = Color.magenta;
            ProceduralTerrainUtility.DrawHexagonPointLinesInGizmos(_cornerPoints);
        }

        if (showNeighbors)
        {
            Gizmos.color = Color.green;
            foreach (HexagonCellPrimitive neighbor in _neighbors)
            {
                Gizmos.DrawSphere(neighbor.transform.position, 3f);
            }
        }
    }
    public void SetNeighborsBySide(float offset = 0.33f)
    {
        HexagonCellPrimitive[] _neighborsBySide = new HexagonCellPrimitive[6];
        HashSet<int> added = new HashSet<int>();

        // Debug.Log("Cell id: " + id + ", SetNeighborsBySide: ");
        RecalculateEdgePoints();

        for (int side = 0; side < 6; side++)
        {
            Vector3 sidePoint = _sides[side];

            for (int neighbor = 0; neighbor < _neighbors.Count; neighbor++)
            {
                if (added.Contains(_neighbors[neighbor].GetID())) continue;

                _neighbors[neighbor].RecalculateEdgePoints();

                for (int neighborSide = 0; neighborSide < 6; neighborSide++)
                {
                    Vector3 neighborSidePoint = _neighbors[neighbor]._sides[neighborSide];

                    // float dist = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(neighborSidePoint.x, neighborSidePoint.z));
                    // Debug.Log("offset: " + offset + ", dist: " + dist);
                    // Debug.Log("sidePoint.x: " + sidePoint.x + ", neighborSidePoint.x: " + neighborSidePoint.x);

                    if (Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(neighborSidePoint.x, neighborSidePoint.z)) <= offset)
                    {

                        _neighborsBySide[side] = _neighbors[neighbor];
                        added.Add(_neighbors[neighbor].GetID());
                        break;
                    }
                }
            }
        }
        neighborsBySide = _neighborsBySide;
    }

    public static List<HexagonCellPrimitive> GenerateHexagonCellPrimitives(List<HexagonTilePrototype> hexagonTilePrototypes, GameObject prefab, Transform transform)
    {
        List<HexagonCellPrimitive> hexagonCellPrimitives = new List<HexagonCellPrimitive>();
        int cellId = 0;

        Transform folder = new GameObject("HexagonCellPrimitives").transform;
        folder.transform.SetParent(transform);

        foreach (HexagonTilePrototype prototype in hexagonTilePrototypes)
        {
            Vector3 pointPos = prototype.center;

            GameObject newTile = Instantiate(prefab, pointPos, Quaternion.identity);
            HexagonCellPrimitive hexagonTile = newTile.GetComponent<HexagonCellPrimitive>();
            hexagonTile._cornerPoints = prototype.cornerPoints;
            hexagonTile.size = prototype.size;
            hexagonTile.SetID(cellId);
            hexagonTile.name = "HexagonCellPrimitive_" + cellId;

            hexagonCellPrimitives.Add(hexagonTile);

            hexagonTile.transform.SetParent(folder);

            cellId++;
        }
        return hexagonCellPrimitives;
    }


    public static List<HexagonCellPrimitive> Shuffle(List<HexagonCellPrimitive> cellPrimitives)
    {
        List<HexagonCellPrimitive> results = new List<HexagonCellPrimitive>();
        results.AddRange(cellPrimitives);

        int n = results.Count;
        for (int i = 0; i < n; i++)
        {
            // Get a random index from the remaining elements
            int r = i + UnityEngine.Random.Range(0, n - i);
            // Swap the current element with the random one
            HexagonCellPrimitive temp = results[r];
            results[r] = results[i];
            results[i] = temp;
        }
        return results;
    }

    public static HexagonCellPrimitive GetClosestCellByCenterPoint(List<HexagonCellPrimitive> cells, Vector3 position)
    {
        HexagonCellPrimitive nearestCellToPos = cells[0];
        float nearestDist = float.MaxValue;

        for (int i = 0; i < cells.Count; i++)
        {
            float dist = Vector2.Distance(new Vector2(position.x, position.y), new Vector2(cells[i].transform.position.x, cells[i].transform.position.z));
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestCellToPos = cells[i];
            }
        }
        return nearestCellToPos;
    }

    public static List<HexagonCellPrimitive> GetRandomConsecutiveSetOfCount(List<HexagonCellPrimitive> cells, int max, Vector3 postionCenter)
    {
        List<HexagonCellPrimitive> results = new List<HexagonCellPrimitive>();

        HexagonCellPrimitive nearestCellToPos = GetClosestCellByCenterPoint(cells, postionCenter);
        // cells.OrderBy(x => x._neighbors.Count);

        HexagonCellPrimitive startCell = nearestCellToPos; //cells[0];
        HexagonCellPrimitive lastCell = startCell;
        results.Add(startCell);
        int startNeighborsChecks = 0;

        while (results.Count < max)
        {
            bool found = false;
            List<HexagonCellPrimitive> options = Shuffle(lastCell._neighbors);

            for (int i = 0; i < options.Count; i++)
            {
                HexagonCellPrimitive nextCell = options[i];
                if (results.Contains(nextCell) == false)
                {
                    results.Add(nextCell);
                    lastCell = nextCell;
                    found = true;
                    // break;
                    startNeighborsChecks++;
                    if (startNeighborsChecks >= 2) break;
                }
            }
            if (!found) lastCell = startCell;
        }

        return results;
    }

    public static void PopulateNeighborsFromCornerPoints(List<HexagonCellPrimitive> cells, float offset = 0.33f)
    {
        foreach (HexagonCellPrimitive cell in cells)
        {
            //for each edgepoint on the current hexagontile
            for (int i = 0; i < cell._cornerPoints.Length; i++)
            {
                //loop through all the hexagontile to check for neighbors
                for (int j = 0; j < cells.Count; j++)
                {
                    //skip if the hexagontile is the current tile
                    if (cells[j] == cell)
                        continue;

                    //loop through the _cornerPoints of the neighboring tile
                    for (int k = 0; k < cells[j]._cornerPoints.Length; k++)
                    {
                        // if (Vector3.Distance(cells[j]._cornerPoints[k], cell._cornerPoints[i]) <= offset)
                        if (Vector2.Distance(new Vector2(cells[j]._cornerPoints[k].x, cells[j]._cornerPoints[k].z), new Vector2(cell._cornerPoints[i].x, cell._cornerPoints[i].z)) <= offset)
                        {
                            if (cell._neighbors.Contains(cells[j]) == false)
                            {
                                cell._neighbors.Add(cells[j]);
                            }
                            break;
                        }
                    }
                }
            }
            cell.SetNeighborsBySide(offset);
        }
    }


    public static void RemovePointsWithinOffset(List<Vector3> points, float offset)
    {
        for (int i = 0; i < points.Count; i++)
        {
            for (int j = i + 1; j < points.Count; j++)
            {
                if (Mathf.Abs(points[i].x - points[j].x) < offset && Mathf.Abs(points[i].z - points[j].z) < offset)
                {
                    points.RemoveAt(j);
                    j--;
                }
            }
        }
    }

    public static List<Vector3> GetZoneConnectorPoints(List<HexagonCellPrimitive> cells, float offset = 0.33f)
    {
        List<Vector3> points = new List<Vector3>();

        foreach (HexagonCellPrimitive cell in cells)
        {
            for (int i = 0; i < cell.neighborsBySide.Length; i++)
            {
                if (cell.neighborsBySide[i]?.gameObject != null && cell.neighborsBySide[i].gameObject.activeInHierarchy && !points.Contains(cell._sides[i]))
                {
                    // if (!points.Contains(cell._sides[i]))
                    points.Add(cell._sides[i]);
                }
            }
        }
        RemovePointsWithinOffset(points, offset);

        return points;
    }
}