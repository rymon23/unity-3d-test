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
    public string id = "-1";
    public int size = 12;
    public Vector3[] _cornerPoints;
    public Vector3[] _sides;
    public List<HexagonCell> _neighbors;
    public List<Vector3> _vertices;
    public HexagonCell[] layeredNeighbor = new HexagonCell[2]; // 0= bottom , 1= top
    public HexagonCell[] neighborsBySide = new HexagonCell[6];
    public bool road;

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

    public int GetNeighborsRelativeSide(HexagonSide side)
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

    public static (HexagonCorner, HexagonCorner) GetCornersFromSide(HexagonSide side)
    {
        switch (side)
        {
            case HexagonSide.Front:
                return (HexagonCorner.FrontA, HexagonCorner.FrontB);
            case HexagonSide.FrontRight:
                return (HexagonCorner.FrontRightA, HexagonCorner.FrontRightB);
            case HexagonSide.BackRight:
                return (HexagonCorner.BackRightA, HexagonCorner.BackRightB);
            case HexagonSide.Back:
                return (HexagonCorner.BackA, HexagonCorner.BackB);
            case HexagonSide.BackLeft:
                return (HexagonCorner.BackLeftA, HexagonCorner.BackLeftB);
            case HexagonSide.FrontLeft:
                return (HexagonCorner.FrontLeftA, HexagonCorner.FrontLeftB);
            // Front
            default:
                return (HexagonCorner.FrontA, HexagonCorner.FrontB);
        }
    }

    public static HexagonSide GetSideFromCorner(HexagonCorner corner)
    {
        if (corner == HexagonCorner.FrontA || corner == HexagonCorner.FrontB) return HexagonSide.Front;
        if (corner == HexagonCorner.FrontRightA || corner == HexagonCorner.FrontRightB) return HexagonSide.FrontRight;
        if (corner == HexagonCorner.BackRightA || corner == HexagonCorner.BackRightB) return HexagonSide.BackRight;

        if (corner == HexagonCorner.BackA || corner == HexagonCorner.BackB) return HexagonSide.Back;
        if (corner == HexagonCorner.BackLeftA || corner == HexagonCorner.BackLeftB) return HexagonSide.BackLeft;
        if (corner == HexagonCorner.FrontLeftA || corner == HexagonCorner.FrontLeftB) return HexagonSide.FrontLeft;

        return HexagonSide.Front;
    }

    public int[] GetBottomNeighborTileSockets(bool top)
    {
        if (GetGridLayer() == 0) return null;

        if (layeredNeighbor[0] == null) return null;

        if (layeredNeighbor[0].currentTile == null) return null;

        return layeredNeighbor[0].currentTile.GetRotatedCornerSockets(top, layeredNeighbor[0].currentRotation);
    }

    public int[] GetDefaultSocketSet(TileSocketPrimitive tileSocketConstant)
    {
        int[] sockets = new int[2];
        sockets[0] = (int)tileSocketConstant;
        sockets[1] = (int)tileSocketConstant;
        return sockets;
    }

    public NeighborSideCornerSockets[] GetSideNeighborTileSockets(bool useWalledEdgePreference = false)
    {
        // int[] neighborSocketsBySide = new int[6];
        NeighborSideCornerSockets[] neighborCornerSocketsBySide = new NeighborSideCornerSockets[6];

        for (int side = 0; side < 6; side++)
        {
            NeighborSideCornerSockets neighborCornerSockets = new NeighborSideCornerSockets();
            HexagonCell sideNeighbor = neighborsBySide[side];

            // If no neighbor, socket is Edge socket value
            if (sideNeighbor == null)
            {
                neighborCornerSockets.bottomCorners = GetDefaultSocketSet(TileSocketPrimitive.Edge);
                neighborCornerSockets.topCorners = GetDefaultSocketSet(TileSocketPrimitive.Edge);
            }
            else
            {

                if (useWalledEdgePreference && isEdgeCell && !IsInCluster())
                {
                    if (!sideNeighbor.isEdgeCell && !sideNeighbor.isEntryCell)
                    {
                        neighborCornerSockets.bottomCorners = GetDefaultSocketSet(TileSocketPrimitive.InnerCell);
                        neighborCornerSockets.topCorners = GetDefaultSocketSet(TileSocketPrimitive.InnerCell);
                    }
                    else
                    {
                        // check if neighbor is edge or entry cell
                        if (sideNeighbor.currentTile == null)
                        {
                            if (sideNeighbor.isEntryCell || (isEntryCell && sideNeighbor.isEntryCell == false))
                            {
                                neighborCornerSockets.bottomCorners = GetDefaultSocketSet(TileSocketPrimitive.EntranceSide);
                                neighborCornerSockets.topCorners = GetDefaultSocketSet(TileSocketPrimitive.EntranceSide);
                            }
                            else
                            {
                                neighborCornerSockets.bottomCorners = GetDefaultSocketSet(TileSocketPrimitive.WallPart);
                                neighborCornerSockets.topCorners = GetDefaultSocketSet(TileSocketPrimitive.WallPart);
                            }
                        }
                        else
                        {
                            int neighborRelativeSide = GetNeighborsRelativeSide((HexagonSide)side);

                            (HexagonCorner cornerA, HexagonCorner cornerB) = GetCornersFromSide((HexagonSide)neighborRelativeSide);

                            neighborCornerSockets.bottomCorners = new int[2];
                            neighborCornerSockets.topCorners = new int[2];

                            neighborCornerSockets.bottomCorners[0] = sideNeighbor.currentTile.GetRotatedCornerSocketId(cornerA, sideNeighbor.currentRotation, false);
                            neighborCornerSockets.bottomCorners[1] = sideNeighbor.currentTile.GetRotatedCornerSocketId(cornerB, sideNeighbor.currentRotation, false);
                            neighborCornerSockets.topCorners[0] = sideNeighbor.currentTile.GetRotatedCornerSocketId(cornerA, sideNeighbor.currentRotation, true);
                            neighborCornerSockets.topCorners[1] = sideNeighbor.currentTile.GetRotatedCornerSocketId(cornerB, sideNeighbor.currentRotation, true);
                        }
                    }
                }
                else
                {

                    if (isLeveledCell)
                    {
                        if (isLeveledEdge)
                        {
                            if (!sideNeighbor.isLeveledCell)
                            {
                                neighborCornerSockets.bottomCorners = GetDefaultSocketSet(TileSocketPrimitive.InnerCell);
                                neighborCornerSockets.topCorners = GetDefaultSocketSet(TileSocketPrimitive.InnerCell);
                            }
                            else
                            {
                                // check if neighbor is edge or entry cell
                                if (sideNeighbor.currentTile == null)
                                {
                                    if (sideNeighbor.isLeveledEdge)
                                    {
                                        neighborCornerSockets.bottomCorners = GetDefaultSocketSet(TileSocketPrimitive.LeveledEdgePart);
                                        neighborCornerSockets.topCorners = GetDefaultSocketSet(TileSocketPrimitive.LeveledEdgePart);
                                    }
                                    else
                                    {
                                        neighborCornerSockets.bottomCorners = GetDefaultSocketSet(TileSocketPrimitive.LeveledInner);
                                        neighborCornerSockets.topCorners = GetDefaultSocketSet(TileSocketPrimitive.LeveledInner);
                                    }

                                }
                                else
                                {

                                    int neighborRelativeSide = GetNeighborsRelativeSide((HexagonSide)side);

                                    (HexagonCorner cornerA, HexagonCorner cornerB) = GetCornersFromSide((HexagonSide)neighborRelativeSide);

                                    neighborCornerSockets.bottomCorners = new int[2];
                                    neighborCornerSockets.topCorners = new int[2];

                                    neighborCornerSockets.bottomCorners[0] = sideNeighbor.currentTile.GetRotatedCornerSocketId(cornerA, sideNeighbor.currentRotation, false);
                                    neighborCornerSockets.bottomCorners[1] = sideNeighbor.currentTile.GetRotatedCornerSocketId(cornerB, sideNeighbor.currentRotation, false);
                                    neighborCornerSockets.topCorners[0] = sideNeighbor.currentTile.GetRotatedCornerSocketId(cornerA, sideNeighbor.currentRotation, true);
                                    neighborCornerSockets.topCorners[1] = sideNeighbor.currentTile.GetRotatedCornerSocketId(cornerB, sideNeighbor.currentRotation, true);
                                }
                            }
                        }
                        else
                        {
                            if (sideNeighbor.currentTile == null)
                            {
                                neighborCornerSockets.bottomCorners = GetDefaultSocketSet(TileSocketPrimitive.LeveledInner);
                                neighborCornerSockets.topCorners = GetDefaultSocketSet(TileSocketPrimitive.LeveledInner);
                            }
                            else
                            {
                                int neighborRelativeSide = GetNeighborsRelativeSide((HexagonSide)side);

                                (HexagonCorner cornerA, HexagonCorner cornerB) = GetCornersFromSide((HexagonSide)neighborRelativeSide);

                                neighborCornerSockets.bottomCorners = new int[2];
                                neighborCornerSockets.topCorners = new int[2];

                                neighborCornerSockets.bottomCorners[0] = sideNeighbor.currentTile.GetRotatedCornerSocketId(cornerA, sideNeighbor.currentRotation, false);
                                neighborCornerSockets.bottomCorners[1] = sideNeighbor.currentTile.GetRotatedCornerSocketId(cornerB, sideNeighbor.currentRotation, false);
                                neighborCornerSockets.topCorners[0] = sideNeighbor.currentTile.GetRotatedCornerSocketId(cornerA, sideNeighbor.currentRotation, true);
                                neighborCornerSockets.topCorners[1] = sideNeighbor.currentTile.GetRotatedCornerSocketId(cornerB, sideNeighbor.currentRotation, true);
                            }

                        }
                    }
                    else
                    {



                        // If neighbor has no tile set -1
                        if (sideNeighbor.currentTile == null)
                        {
                            neighborCornerSockets.bottomCorners = GetDefaultSocketSet(TileSocketPrimitive.InnerCell);
                            neighborCornerSockets.topCorners = GetDefaultSocketSet(TileSocketPrimitive.InnerCell);
                        }
                        else
                        {
                            int neighborRelativeSide = GetNeighborsRelativeSide((HexagonSide)side);

                            (HexagonCorner cornerA, HexagonCorner cornerB) = GetCornersFromSide((HexagonSide)neighborRelativeSide);

                            neighborCornerSockets.bottomCorners = new int[2];
                            neighborCornerSockets.topCorners = new int[2];

                            neighborCornerSockets.bottomCorners[0] = sideNeighbor.currentTile.GetRotatedCornerSocketId(cornerA, sideNeighbor.currentRotation, false);
                            neighborCornerSockets.bottomCorners[1] = sideNeighbor.currentTile.GetRotatedCornerSocketId(cornerB, sideNeighbor.currentRotation, false);
                            neighborCornerSockets.topCorners[0] = sideNeighbor.currentTile.GetRotatedCornerSocketId(cornerA, sideNeighbor.currentRotation, true);
                            neighborCornerSockets.topCorners[1] = sideNeighbor.currentTile.GetRotatedCornerSocketId(cornerB, sideNeighbor.currentRotation, true);
                        }
                    }


                }
            }

            neighborCornerSocketsBySide[side] = neighborCornerSockets;
        }

        return neighborCornerSocketsBySide;
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
    public bool isEdgeCell;//{ private set; get; }
    public void SetEdgeCell(bool enable)
    {
        isEdgeCell = enable;
    }
    public bool isPathCell;//{ private set; get; }
    public void SetPathCell(bool enable)
    {
        isPathCell = enable;
    }

    public bool isLeveledCell;//{ private set; get; }
    public void SetLeveledCell(bool enable)
    {
        isLeveledCell = enable;
    }
    public bool isLeveledEdge;//{ private set; get; }
    public void SetLeveledEdge(bool enable)
    {
        isLeveledEdge = enable;
    }
    public bool isLeveledRampCell;//{ private set; get; }
    public void SetLeveledRampCell(bool enable)
    {
        isLeveledRampCell = enable;
    }

    [Header("Grid Layer")]
    [SerializeField] private int _gridLayer = 0;
    public int GetGridLayer() => _gridLayer;
    public void SetGridLayer(int gridlayer)
    {
        _gridLayer = gridlayer;
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
    public bool isIgnored;
    public bool IsAssigned() => currentTile != null || IsInCluster() || isIgnored;
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
        HashSet<string> added = new HashSet<string>();

        // Debug.Log("Cell id: " + id + ", SetNeighborsBySide: ");
        RecalculateEdgePoints();

        for (int side = 0; side < 6; side++)
        {
            Vector3 sidePoint = _sides[side];

            for (int neighbor = 0; neighbor < _neighbors.Count; neighbor++)
            {
                if (_neighbors[neighbor].GetGridLayer() != GetGridLayer() || added.Contains(_neighbors[neighbor].id)) continue;

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
        bool hasMultilayerCells = cells.Any(c => c.GetGridLayer() > 0);

        foreach (HexagonCell cell in cells)
        {
            int neighborCount = cell._neighbors.Count;
            if (neighborCount < 6 || (hasMultilayerCells && neighborCount < 7) || (cell.GetGridLayer() > 0 && cell.layeredNeighbor[0]._neighbors.Count < 7))
            {
                edgeCells.Add(cell);

                cell.SetEdgeCell(true);

            }
        }
        // Order edge cells by the fewest neighbors first
        return edgeCells.OrderByDescending(x => x._neighbors.Count).ToList();
    }

    public static List<HexagonCell> GetLeveledEdgeCells(List<HexagonCell> layerCells, bool assign)
    {
        List<HexagonCell> edgeCells = new List<HexagonCell>();

        foreach (HexagonCell cell in layerCells)
        {
            int neighborCount = cell._neighbors.FindAll(n => layerCells.Contains(n) && n.GetGridLayer() == cell.GetGridLayer()).Count;
            if (neighborCount < 6)
            {
                edgeCells.Add(cell);
                if (assign) cell.SetLeveledEdge(true);
            }
        }
        // Order edge cells by the fewest neighbors first
        return edgeCells.OrderByDescending(x => x._neighbors.Count).ToList();
    }

    public static List<HexagonCell> GetRandomEntryCells(List<HexagonCell> edgeCells, int num, bool assign, int gridLayer = 0)
    {
        List<HexagonCell> cells = new List<HexagonCell>();
        cells.AddRange(edgeCells.Except(edgeCells.FindAll(c => c.GetGridLayer() != gridLayer)));

        ShuffleCells(cells);

        List<HexagonCell> entrances = new List<HexagonCell>();

        foreach (HexagonCell cell in cells)
        {
            if (entrances.Count == num) break;

            bool isNeighbor = false;
            foreach (HexagonCell item in entrances)
            {
                if (item._neighbors.Contains(cell))
                {
                    isNeighbor = true;
                    break;
                }
            }
            if (!isNeighbor)
            {
                entrances.Add(cell);
                if (assign) cell.SetEntryCell(true);
            }

        }
        return entrances.OrderByDescending(x => x._neighbors.Count).ToList();
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
                    if (cells[j] == cell || cells[j].GetGridLayer() != cell.GetGridLayer())
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

    public static List<HexagonCell> GetRandomCellPaths(List<HexagonCell> entryCells, List<HexagonCell> allCells, Vector3 centerPosition, bool ignoreEdgeCells = true)
    {
        Debug.Log("GetRandomCellPaths => entryCells: " + entryCells.Count + ", allCells: " + allCells.Count);

        List<HexagonCell> finalPath = new List<HexagonCell>();
        HexagonCell centerCell = HexagonCell.GetClosestCellByCenterPoint(allCells, centerPosition);
        // entryCells = HexagonCell.GetRandomEntryCells(edgeCells, 3, false);

        foreach (HexagonCell entryCell in entryCells)
        {
            HexagonCell innerNeighbor = entryCell._neighbors.Find(n => n.isEdgeCell == false);

            List<HexagonCell> cellPath = HexagonCell.FindPath(innerNeighbor, centerCell, ignoreEdgeCells);
            if (cellPath != null)
            {
                finalPath.AddRange(cellPath);
                if (finalPath.Contains(innerNeighbor) == false) finalPath.Add(innerNeighbor);
            }
        }

        Debug.Log("GetRandomCellPaths => finalPath: " + finalPath.Count + ", centerCell: " + centerCell.id);
        return finalPath;
    }

    public static List<HexagonCell> FindPath(HexagonCell startCell, HexagonCell endCell, bool ignoreEdgeCells)
    {
        // Create a queue to store the cells to be visited
        Queue<HexagonCell> queue = new Queue<HexagonCell>();

        // Create a dictionary to store the parent of each cell
        Dictionary<HexagonCell, HexagonCell> parent = new Dictionary<HexagonCell, HexagonCell>();

        // Create a set to store the visited cells
        HashSet<HexagonCell> visited = new HashSet<HexagonCell>();

        // Enqueue the start cell and mark it as visited
        queue.Enqueue(startCell);
        visited.Add(startCell);

        // Run the BFS loop
        while (queue.Count > 0)
        {
            HexagonCell currentCell = queue.Dequeue();

            // Check if the current cell is the end cell
            if (currentCell == endCell)
            {
                // Create a list to store the path
                List<HexagonCell> path = new List<HexagonCell>();

                // Trace back the path from the end cell to the start cell
                HexagonCell current = endCell;
                while (current != startCell)
                {
                    path.Add(current);
                    current = parent[current];
                }
                path.Reverse();
                return path;
            }

            // Enqueue the unvisited neighbors
            foreach (HexagonCell neighbor in currentCell._neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    if (!ignoreEdgeCells || !neighbor.isEdgeCell)
                    {
                        queue.Enqueue(neighbor);
                        parent[neighbor] = currentCell;
                    }
                }
            }
        }
        // If there is no path between the start and end cells
        return null;
    }


    public static List<HexagonCell> GetRandomLevelConnectorCells(List<HexagonCell> levelPathCells, int max = 2)
    {
        List<HexagonCell> results = new List<HexagonCell>();
        foreach (HexagonCell item in levelPathCells)
        {
            if (results.Count == max) break;

            HexagonCell found = item._neighbors.Find(c => !c.isEntryCell && !c.isEdgeCell && !levelPathCells.Contains(c) && c.GetGridLayer() == item.GetGridLayer());
            if (found != null && !results.Contains(found)) results.Add(found);
        }
        return results;
    }



    public static List<HexagonCell> GetRandomConsecutiveCells(List<HexagonCell> allLevelCells, float maxPercentOfCells = 0.5f)
    {
        List<HexagonCell> allCells = new List<HexagonCell>();
        allCells.AddRange(allLevelCells);

        // Randomly shuffle the list of cells
        ShuffleCells(allCells);

        // Initialize the list of consecutive cells
        List<HexagonCell> consecutiveCells = new List<HexagonCell>();

        // Iterate through the shuffled list of cells
        for (int i = 0; i < allCells.Count; i++)
        {
            HexagonCell currentCell = allCells[i];

            // Check if the current cell is already in the consecutive cells list
            if (consecutiveCells.Contains(currentCell))
                continue;

            // Add the current cell to the consecutive cells list
            consecutiveCells.Add(currentCell);

            // Iterate through the current cell's neighbors
            for (int j = 0; j < 6; j++)
            {
                HexagonCell neighbor = currentCell._neighbors[j];

                // Check if the neighbor is in the allCells list and is not already in the consecutive cells list
                if (allCells.Contains(neighbor) && !consecutiveCells.Contains(neighbor))
                    consecutiveCells.Add(neighbor);
            }

            //Check if the consecutive cells list is larger than the maximum percent allowed and return it
            if ((float)consecutiveCells.Count / (float)allCells.Count > maxPercentOfCells)
                return consecutiveCells;
        }

        //Return the final consecutive cells list
        return consecutiveCells;
    }


    public static List<HexagonCell> SelectCellsInRadiusOfCell(List<HexagonCell> cells, HexagonCell centerCell, float radius)
    {
        Vector2 centerPos = new Vector2(centerCell.transform.position.x, centerCell.transform.position.z);
        //Initialize a list to store cells within the radius distance
        List<HexagonCell> selectedCells = new List<HexagonCell>();

        //Iterate through each cell in the input list
        foreach (HexagonCell cell in cells)
        {
            Vector2 cellPos = new Vector2(cell.transform.position.x, cell.transform.position.z);

            // Check if the distance between the cell and the center cell is within the given radius
            if (Vector2.Distance(centerPos, cellPos) <= radius)
            {
                //If the distance is within the radius, add the current cell to the list of selected cells
                selectedCells.Add(cell);
            }
        }
        //Return the list of selected cells
        return selectedCells;
    }

    public static List<HexagonCell> SelectCellsInRadiusOfRandomCell(List<HexagonCell> cells, float radius)
    {
        //Select a random center cell
        HexagonCell centerCell = cells[UnityEngine.Random.Range(0, cells.Count)];
        return SelectCellsInRadiusOfCell(cells, centerCell, radius);
    }


    public static List<HexagonCell> GetRandomLeveledCells(List<HexagonCell> alllevelCells, float maxRadius, bool assign, int clumpSets = 4)
    {
        List<HexagonCell> result = SelectCellsInRadiusOfRandomCell(alllevelCells, maxRadius);

        for (int i = 0; i < clumpSets; i++)
        {
            List<HexagonCell> newClump = SelectCellsInRadiusOfCell(alllevelCells, result[UnityEngine.Random.Range(0, result.Count)], maxRadius * 0.9f);
            result.AddRange(newClump.FindAll(c => result.Contains(c) == false));
        }

        if (assign)
        {
            foreach (HexagonCell cell in result)
            {
                cell.SetLeveledCell(true);
            }
        }
        return result;
    }

    // public static List<HexagonCell> SyncNextLeveledCells(List<HexagonCell> levelCells, float maxRadius, bool assign)
    // {

    //     foreach (HexagonCell cell in levelCells)
    //     {
    //         HexagonCell topNeighbor = cell.layeredNeighbor[1];
    //         if (topNeighbor != null) {
    //             topNeighbor
    //         }
    //     }
    //     if (assign)
    //     {
    //         foreach (HexagonCell cell in result)
    //         {
    //             cell.SetLeveledCell(true);
    //         }
    //     }
    //     return result;
    // }


    public static List<HexagonCell> GetRandomLeveledRampCells(List<HexagonCell> leveledEdgeCells, int num, bool assign, bool allowNeighbors = true)
    {
        List<HexagonCell> cells = new List<HexagonCell>();
        cells.AddRange(leveledEdgeCells);

        ShuffleCells(cells);

        List<HexagonCell> rampCells = new List<HexagonCell>();

        foreach (HexagonCell cell in cells)
        {
            if (rampCells.Count == num) break;

            bool isNeighbor = false;
            if (allowNeighbors == false)
            {
                foreach (HexagonCell item in rampCells)
                {
                    if (item._neighbors.Contains(cell))
                    {
                        isNeighbor = true;
                        break;
                    }
                }
            }
            if (allowNeighbors || !isNeighbor)
            {
                rampCells.Add(cell);
                if (assign) cell.SetLeveledRampCell(true);
            }

        }
        return rampCells.OrderByDescending(x => x._neighbors.Count).ToList();
    }

    public static (Dictionary<int, List<HexagonCell>>, Dictionary<int, List<HexagonCell>>) GetRandomGridPathsForLevels(Dictionary<int, List<HexagonCell>> cellsByLevel, Vector3 position, int max, bool ignoreEdgeCells, int maxEntryCellsPerLevel = 2)
    {
        Dictionary<int, List<HexagonCell>> pathsByLevel = new Dictionary<int, List<HexagonCell>>();
        Dictionary<int, List<HexagonCell>> rampsByLevel = new Dictionary<int, List<HexagonCell>>();

        int lastLevel = cellsByLevel.Count;

        foreach (var kvp in cellsByLevel)
        {
            int level = kvp.Key;
            List<HexagonCell> levelCells = kvp.Value;

            List<HexagonCell> entryCells = GetRandomEntryCells(levelCells.FindAll(c => c.isEdgeCell), maxEntryCellsPerLevel, false, level);
            List<HexagonCell> newPaths = GetRandomCellPaths(entryCells, levelCells, position, ignoreEdgeCells);

            if (newPaths.Count > 0)
            {
                pathsByLevel.Add(level, newPaths);

                if (level < lastLevel)
                {
                    List<HexagonCell> newRamps = GetRandomLevelConnectorCells(newPaths, maxEntryCellsPerLevel);
                    rampsByLevel.Add(level, newRamps);
                }
            }
        }

        return (pathsByLevel, rampsByLevel);
    }




    public static Dictionary<int, List<HexagonCell>> OrganizeCellsByLevel(List<HexagonCell> cells)
    {
        Dictionary<int, List<HexagonCell>> cellsByLevel = new Dictionary<int, List<HexagonCell>>();

        foreach (HexagonCell cell in cells)
        {
            int level = cell.GetGridLayer();

            if (cellsByLevel.ContainsKey(level) == false)
            {
                cellsByLevel.Add(level, new List<HexagonCell>());
            }

            cellsByLevel[level].Add(cell);
        }
        return cellsByLevel;
    }

    public static HexagonCell[,] CreateRectangularGrid(List<HexagonCell> cellGrid)
    {
        float hexagonSize = cellGrid[0].size;

        //Find the minimum and maximum x and z coordinates of the hexagon cells in the grid
        float minX = cellGrid.Min(cell => cell.transform.position.x);
        float maxX = cellGrid.Max(cell => cell.transform.position.x);
        float minZ = cellGrid.Min(cell => cell.transform.position.z);
        float maxZ = cellGrid.Max(cell => cell.transform.position.z);
        //Determine the number of rows and columns needed for the rectangular grid
        int rows = (int)((maxZ - minZ) / hexagonSize) + 1;
        int cols = (int)((maxX - minX) / (hexagonSize * 0.75f)) + 1;

        //Initialize the rectangular grid with the determined number of rows and columns
        HexagonCell[,] rectGrid = new HexagonCell[rows, cols];

        //Iterate through each hexagon cell in the cell grid
        foreach (HexagonCell cell in cellGrid)
        {
            //Determine the row and column index of the current cell in the rectangular grid
            int row = (int)((cell.transform.position.z - minZ) / hexagonSize);
            int col = (int)((cell.transform.position.x - minX) / (hexagonSize * 0.75f));

            //Assign the current cell to the corresponding position in the rectangular grid
            rectGrid[row, col] = cell;
        }

        return rectGrid;
    }


    public static void MapHeightmapValues(float[,] heightmap, HexagonCell[,] cellGrid, bool assign)
    {
        //Check that the dimensions of the heightmap match the dimensions of the cell grid
        // if (heightmap.GetLength(0) != cellGrid.GetLength(0) || heightmap.GetLength(1) != cellGrid.GetLength(1))
        // {
        //     Debug.LogError("The dimensions of the heightmap do not match the dimensions of the cell grid.");
        //     return;
        // }

        //Iterate through each element of the cell grid
        for (int row = 0; row < cellGrid.GetLength(0); row++)
        {
            for (int col = 0; col < cellGrid.GetLength(1); col++)
            {
                HexagonCell cell = cellGrid[row, col];

                //Check if there is a HexagonCell at the current element
                if (cell != null)
                {
                    //Assign the value of the corresponding element in the heightmap to the HexagonCell's height property
                    // cellGrid[row, col].height = heightmap[row, col];
                    // Set cell to path if value within a set range


                    if (assign)
                    {
                        cell.road = (heightmap[row, col] <= 0.2f);

                        if (cell.isEdgeCell == false)
                        {
                            cell.SetPathCell(cell.road);
                        }
                    }
                    else
                    {
                        cell.road = !cell.isEdgeCell && (heightmap[row, col] <= 0.2f);
                    }


                }
            }
        }
    }

    public static List<HexagonCell> ClearSoloPathCells(List<HexagonCell> pathCells)
    {
        foreach (HexagonCell cell in pathCells)
        {
            bool hasPathNeighbor = cell._neighbors.Any(c => c.isPathCell == true);
            if (hasPathNeighbor == false) cell.SetPathCell(false);
        }
        return pathCells.FindAll(c => c.isPathCell);
    }

    public static void AssignVerticesToCells(List<HexagonCell> cells, List<Vector3> vertices)
    {
        foreach (Vector3 vertex in vertices)
        {
            HexagonCell closestCell = cells[0];
            float closestDistance = Vector3.Distance(vertex, closestCell.transform.position);
            for (int i = 1; i < cells.Count; i++)
            {
                float distance = Vector3.Distance(vertex, cells[i].transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestCell = cells[i];
                }
            }
            closestCell._vertices.Add(vertex);
        }
    }


    public static void ShuffleCells(List<HexagonCell> cells)
    {
        int n = cells.Count;
        for (int i = 0; i < n; i++)
        {
            // Get a random index from the remaining elements
            int r = i + UnityEngine.Random.Range(0, n - i);
            // Swap the current element with the random one
            HexagonCell temp = cells[r];
            cells[r] = cells[i];
            cells[i] = temp;
        }
    }

    [System.Serializable]
    public struct NeighborSideCornerSockets
    {
        public int[] topCorners;
        public int[] bottomCorners;
    }
}