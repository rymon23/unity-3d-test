using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;
using UnityEditor;

[System.Serializable]
public class HexagonCell : MonoBehaviour
{
    public int id = -1;
    public int size = 12;
    public Vector3[] _cornerPoints;
    public Vector3[] _sides;

    private Dictionary<TileCategory, float> categoyBias;
    public void SetCategoryBias(TileCategory category, float value)
    {
        if (categoyBias == null) categoyBias = new Dictionary<TileCategory, float>();

        if (!categoyBias.ContainsKey(category))
        {
            categoyBias.Add(category, value);
        }
        else
        {
            categoyBias[category] = value;
        }
    }


    public List<HexagonCell> _neighbors;
    public HexagonCell[] neighborsBySide = new HexagonCell[6];
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
    public int[] GetNeighborTileSockets(bool useWalledEdgePreference = false)
    {
        int[] neighborSocketsBySide = new int[6];

        for (int side = 0; side < 6; side++)
        {
            // If no neighbor socket is Edge socket value
            if (neighborsBySide[side] == null)
            {
                neighborSocketsBySide[side] = 1;
            }
            else
            {
                if (useWalledEdgePreference && isEdgeCell && !IsInCluster())
                {

                    if (!neighborsBySide[side].isEdgeCell && !neighborsBySide[side].isEntryCell)
                    {
                        neighborSocketsBySide[side] = (int)TileSocketConstants.InnerCell;
                    }
                    else
                    {
                        // If neighbor has no tile set -1
                        if (neighborsBySide[side].currentTile == null)
                        {
                            neighborSocketsBySide[side] = neighborsBySide[side].isEntryCell ? (int)TileSocketConstants.Entrance : (int)TileSocketConstants.WallPart;
                        }
                        else
                        {
                            int neighborRelativeSide = GetNeighborsRelativeSide((HexagonSides)side);
                            int facingSocket = neighborsBySide[side].currentTile.GetRotatedSideSocketId((HexagonSides)neighborRelativeSide, neighborsBySide[side].currentRotation);
                            neighborSocketsBySide[side] = facingSocket;
                        }
                    }
                }
                else
                {
                    // If neighbor has no tile set -1
                    if (neighborsBySide[side].currentTile == null)
                    {
                        neighborSocketsBySide[side] = -1;
                    }
                    else
                    {
                        int neighborRelativeSide = GetNeighborsRelativeSide((HexagonSides)side);
                        int facingSocket = neighborsBySide[side].currentTile.GetRotatedSideSocketId((HexagonSides)neighborRelativeSide, neighborsBySide[side].currentRotation);
                        neighborSocketsBySide[side] = facingSocket;
                    }
                }


            }
        }
        return neighborSocketsBySide;
    }

    public int currentRotation = 0;
    private Transform center;

    [Header("WFC Params")]
    public float highestProbability;
    public bool isEntryCell { private set; get; }
    public void SetEntryCell(bool enable)
    {
        isEntryCell = enable;
    }
    public bool isEdgeCell { private set; get; }
    public void SetEdgeCell(bool enable)
    {
        isEdgeCell = enable;
    }

    [Header("Cluster")]
    [SerializeField] public bool isClusterPrototype;
    [SerializeField] private int clusterId = -1;
    public bool IsInCluster() => clusterId != -1;
    public int GetClusterID() => clusterId;
    public void SetClusterID(int _id)
    {
        clusterId = _id;
    }
    [SerializeField] private int _numberofNeighborsInCluster = 0;
    public int GetNumberofNeighborsInCluster() => _numberofNeighborsInCluster;
    public void SetNumberofNeighborsInCluster(int num)
    {
        _numberofNeighborsInCluster = num;
    }
    public int GetNumberOfNeighborsUnclustered()
    {
        if (_neighbors.Count == 0) return 0;
        int num = 0;
        foreach (HexagonCell item in _neighbors)
        {
            if (item.isClusterPrototype == false) num++;
        }
        return num;
    }

    [Header("Tile")]
    [SerializeField] private HexagonTile currentTile;
    public bool IsAssigned() => currentTile != null || IsInCluster();
    public HexagonTile GetTile() => currentTile;
    public void SetTile(HexagonTile newTile, int rotation)
    {
        if (IsInCluster())
        {
            Debug.LogError("Trying to set a Tile on a cell with a clusterId assigned");
            return;
        }
        currentTile = newTile;
        currentRotation = rotation;
    }

    private void RecalculateEdgePoints()
    {
        _cornerPoints = ProceduralTerrainUtility.GenerateHexagonPoints(transform.position, size);
        _sides = HexagonGenerator.GenerateHexagonSidePoints(_cornerPoints);
    }

    private void Awake()
    {
        center = transform;
        RecalculateEdgePoints();
    }

    private void Start()
    {
        RecalculateEdgePoints();
    }

    void OnValidate()
    {
        center = transform;



        if (resetPoints || _currentCenterPosition != center.position || _currentSize != size || _cornerPoints == null || _cornerPoints.Length == 0)
        {
            resetPoints = false;
            _currentCenterPosition = center.position;
            _currentSize = size;
            RecalculateEdgePoints();

            // SetNeighborsBySide(1f * (size / 12f));

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
    private Vector3 _currentCenterPosition;
    private int _currentSize;
    #endregion

    private void OnDrawGizmos()
    {
        if (!enableDebugMode) return;

        center = transform;

        if (_currentCenterPosition != center.position)
        {
            OnValidate();
        }


        if (showCenter)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(center.position, 0.3f);
        }

        if (highlight)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(center.position, 6f);
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
            foreach (HexagonCell neighbor in _neighbors)
            {
                Gizmos.DrawSphere(neighbor.center.position, 3f);
            }
        }
    }
    public void SetNeighborsBySide(float offset = 0.33f)
    {
        HexagonCell[] _neighborsBySide = new HexagonCell[6];
        HashSet<int> added = new HashSet<int>();

        // Debug.Log("Cell id: " + id + ", SetNeighborsBySide: ");
        RecalculateEdgePoints();

        for (int side = 0; side < 6; side++)
        {
            Vector3 sidePoint = _sides[side];

            for (int neighbor = 0; neighbor < _neighbors.Count; neighbor++)
            {
                if (added.Contains(_neighbors[neighbor].id)) continue;

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
                        added.Add(_neighbors[neighbor].id);
                        break;
                    }
                }
            }
        }
        neighborsBySide = _neighborsBySide;
    }

    // public void SetNeighborsBySide()
    // {
    //     // Create a new dictionary to store neighbors by side
    //     Dictionary<int, HexagonCell> newNeighborsBySide = new Dictionary<int, HexagonCell>();
    //     for (int i = 0; i < 6; i++)
    //     {
    //         newNeighborsBySide.Add(i, null);
    //     }
    //     for (int k = 0; k < _neighbors.Count; k++)
    //     {
    //         // Assign the neighbor to the corresponding side in the dictionary
    //         newNeighborsBySide[k] = _neighbors[k];
    //     }
    //     neighborsBySide = newNeighborsBySide;
    // }

    public static List<HexagonCell> GetEdgeCells(List<HexagonCell> cells)
    {
        List<HexagonCell> edgeCells = new List<HexagonCell>();
        foreach (HexagonCell cell in cells)
        {
            if (cell._neighbors.Count < 6)
            {
                edgeCells.Add(cell);
                cell.SetEdgeCell(true);
            }
        }
        // Order edge cells by the fewest neighbors first
        return edgeCells.OrderByDescending(x => x._neighbors.Count).ToList();
    }

    // public static void PopulateNeighborsFromSidePoints(List<HexagonCell> cells, float offset = 0.3f)
    // {
    //     foreach (HexagonCell cell in cells)
    //     {
    //         bool[] hasSideNeighbor = new bool[6];

    //         //for each edgepoint on the current hexagontile
    //         for (int i = 0; i < cell._sides.Length; i++)
    //         {
    //             //loop through all the hexagontile to check for neighbors
    //             for (int j = 0; j < cells.Count; j++)
    //             {
    //                 //skip if the hexagontile is the current tile
    //                 if (cells[j] == cell)
    //                     continue;


    //                 //loop through the _sides of the neighboring tile
    //                 for (int k = 0; k < cells[j]._sides.Length; k++)
    //                 {


    //                     if (Vector3.Distance(cells[j]._sides[k], cell._sides[i]) <= offset)
    //                     {
    //                         if (newNeighborsBySide.ContainsKey(k) == false)
    //                         {
    //                             newNeighborsBySide.Add(cells[j].id, cells[j]);
    //                         }
    //                         break;
    //                     }

    //                     newNeighborsBySide.Add(k, null);
    //                 }
    //             }
    //         }
    //         cell.neighborsBySide = newNeighborsBySide;
    //         cell._neighbors = newNeighborsBySide.Select(x => x.Value).ToList().FindAll(x => x != null);
    //         // cell.SetNeighborsBySide();
    //     }
    // }
    // public static void PopulateNeighborsFromSidePoints(List<HexagonCell> cells, float offset = 0.1f)
    // {
    //     foreach (HexagonCell cell in cells)
    //     {
    //         HexagonCell[] newNeighborsBySide = new HexagonCell[6];

    //         //for each edgepoint on the current hexagontile
    //         for (int i = 0; i < cell._sides.Length; i++)
    //         {
    //             //loop through all the hexagontile to check for neighbors
    //             for (int j = 0; j < cells.Count; j++)
    //             {
    //                 //skip if the hexagontile is the current tile
    //                 if (cells[j] == cell)
    //                     continue;

    //                 //loop through the _sides of the neighboring tile
    //                 for (int k = 0; k < cells[j]._sides.Length; k++)
    //                 {
    //                     if (Vector3.Distance(cells[j]._sides[k], cell._sides[i]) <= offset)
    //                     {
    //                         if (cell._neighbors.Contains(cells[j]) == false)
    //                         {
    //                             cell._neighbors.Add(cells[j]);
    //                             newNeighborsBySide[k] = cells[j];
    //                         }
    //                         break;
    //                     }
    //                 }
    //             }
    //         }
    //         cell.neighborsBySide = newNeighborsBySide;
    //         cell.neighborsBySide = newNeighborsBySide;
    //     }
    // }

    public static void PopulateNeighborsFromCornerPoints(List<HexagonCell> cells, float offset = 0.33f)
    {
        foreach (HexagonCell cell in cells)
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
    public static HexagonCell GetClosestCellByCenterPoint(List<HexagonCell> cells, Vector3 position)
    {
        HexagonCell nearestCellToPos = cells[0];
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
}