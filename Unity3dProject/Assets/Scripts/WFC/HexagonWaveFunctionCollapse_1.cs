using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;
public enum WFCCollapseOrder
{
    Default = 0, // Edges -> Center => th rest
    Contract, // Start at the edges
    Expand // Start at the center
}
public class HexagonWaveFunctionCollapse_1 : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private WFCCollapseOrder collapseOrder = 0;

    [SerializeField] private int minEntrances = 1;
    [SerializeField] private int maxEntrances = 2;

    [SerializeField] private int maxAttempts = 9999;
    [SerializeField] private int remainingAttempts;
    [SerializeField] private bool generatePaths = true;
    [SerializeField] private bool isWalledEdge;
    [SerializeField] private bool ignoreFailures = true;
    [Header("Layering")]
    [Range(0.1f, 1f)][SerializeField] private float layerStackChance = 0.5f;
    [SerializeField] private int totalLayers = 1;

    [Header("Clusters")]
    [SerializeField] private bool enableClusters;
    [Range(0.1f, 1f)][SerializeField] private float clusterChance = 0.5f;

    //Temp
    [SerializeField] private SubZone subZone;
    [SerializeField] private ZoneCellManager zoneCellManager;
    [SerializeField] private TileSocketMatrixGenerator socketMatrixGenerator;
    [SerializeField] private TileDirectory tileDirectory;
    [SerializeField] private int outOfBoundsSlotId = 1; // Edge socket
    [SerializeField] private int wallInnerSlotId = 10; // Edge socket
    [SerializeField] private List<GameObject> activeTiles;
    [SerializeField] private int matrixLength;
    private int numTiles; // Number of tile prefabs
    public bool[,] compatibilityMatrix = null; // Compatibility matrix for tiles
    public int[,] probabilityMatrix;
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private List<HexagonTile> tilePrefabs;
    [SerializeField] private List<HexagonTile> tilePrefabs_LayerConnector;
    [SerializeField] private List<HexagonTile> tilePrefabs_Edgable;
    [SerializeField] private List<HexagonTile> tilePrefabs_Leveled;
    [SerializeField] private List<HexagonTile> tilePrefabs_Path;
    [SerializeField] private List<HexagonTile> tilePrefabs_ClusterSet;
    [SerializeField] private HexagonTile tilePrefabs_ClusterCenter;
    [SerializeField] private List<HexagonTileCluster> tilePrefabs_cluster;
    Dictionary<int, HexagonTile> tileLookupByid;
    Dictionary<int, HexagonTileCluster> tileClusterLookupByid;
    public List<HexagonCell> cells;
    public List<HexagonCell> edgeCells;
    public List<HexagonCell> entryCells;
    public List<HexagonCell> pathCells;
    public List<HexagonCell> allLeveledCells;

    [SerializeField] private List<HexagonCellCluster> activeCellClusters;
    [SerializeField] private List<HexagonCellCluster> allCellClusters;
    [SerializeField] private int availableCellClusters = 0;
    [SerializeField] private int totalEdgeCells = 0;

    float[] rotationValues = { 0f, 60f, 120f, 180f, 240f, 300f };

    void Start()
    {
        EvaluateTiles();

        UpdateCompatibilityMatrix();

        Invoke("ExecuteWFC", 0.2f);
    }

    private void ExecuteWFC()
    {
        compatibilityMatrix = socketMatrixGenerator.GetCompatibilityMatrix();
        if (compatibilityMatrix != null) matrixLength = compatibilityMatrix.Length;
        matrixLength = socketMatrixGenerator.matrix.Length;

        EvaluateCells();

        probabilityMatrix = new int[cells.Count, tilePrefabs.Count];

        CollapseEntranceCells();
        Debug.Log("Entrance Cells Assigned");

        if (isWalledEdge)
        {
            CollapseEdgeCells();
            Debug.Log("Edge Cells Assigned");
        }

        CollapseLeveledCells();

        // CollapsePathCells();
        // if (generatePaths)
        // {
        //     CollapsePathCells();
        //     Debug.Log("Path Cells Assigned");
        // }

        remainingAttempts = maxAttempts;
        while (remainingAttempts > 0 && IsUnassignedCells())
        {
            remainingAttempts--;
            SelectNext();
        }

        InstantiateTiles();

        Debug.Log("Execution of WFC Complete");
    }

    private bool IsUnassignedCells()
    {
        // Iterate through all cells and check if there is any unassigned cell
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].IsAssigned() == false) return true;
        }
        return false;
    }

    private void CollapseEntranceCells()
    {
        // Handle Entrance Cells First
        List<HexagonCell> entranceCells = edgeCells.FindAll(c => c.isEntryCell);

        for (int i = 0; i < entranceCells.Count; i++)
        {
            (HexagonTile nextTile, List<int> rotations) = SelectNextTile(entranceCells[i], tilePrefabs_Edgable, false);
            AssignTileToCell(entranceCells[i], nextTile, rotations);
        }
    }

    private void CollapseEdgeCells()
    {
        for (int i = 0; i < edgeCells.Count; i++)
        {
            HexagonCell currentCell = edgeCells[i];
            if (currentCell.IsAssigned()) continue;

            (HexagonTile nextTile, List<int> rotations) = SelectNextTile(currentCell, tilePrefabs_Edgable, false);
            AssignTileToCell(currentCell, nextTile, rotations);
        }
    }

    private void CollapseLeveledCells()
    {
        List<HexagonCell> leveledEdgeCells = allLeveledCells.FindAll(c => c.isLeveledEdge);
        List<HexagonTile> tilePrefabs_LeveledRamp = tilePrefabs_Leveled.FindAll(c => c.isLeveledRamp);

        foreach (HexagonCell cell in leveledEdgeCells)
        {
            if (cell.IsAssigned()) continue;

            List<HexagonTile> prefabs = cell.isLeveledRampCell ? tilePrefabs_LeveledRamp : tilePrefabs_Leveled;

            (HexagonTile nextTile, List<int> rotations) = SelectNextTile(cell, prefabs, false);
            AssignTileToCell(cell, nextTile, rotations);
        }

        foreach (HexagonCell cell in allLeveledCells)
        {
            if (cell.IsAssigned()) continue;

            (HexagonTile nextTile, List<int> rotations) = SelectNextTile(cell, tilePrefabs_Leveled, false);
            AssignTileToCell(cell, nextTile, rotations);
        }
    }

    private void CollapsePathCells()
    {
        for (int i = 0; i < pathCells.Count; i++)
        {
            HexagonCell currentCell = pathCells[i];
            if (currentCell.IsAssigned()) continue;

            currentCell.SetPathCell(true);

            (HexagonTile nextTile, List<int> rotations) = SelectNextTile(currentCell, tilePrefabs_Path, false);
            AssignTileToCell(currentCell, nextTile, rotations);
        }
    }

    private void SetIgnoreCellCluster(HexagonCellCluster cluster)
    {
        cluster.selectIgnore = true;
        availableCellClusters -= 1;
    }

    private HexagonCellCluster SelectCellCluster()
    {
        if (allCellClusters.Count == 0) return null;

        // For now: 
        // A cluster can be selected if none of the neighbor cells are assigned
        HexagonCellCluster selected = null;

        for (int i = 0; i < allCellClusters.Count; i++)
        {
            Debug.Log("allCellClusters[" + i + "] - id: " + allCellClusters[i].id);

            if (allCellClusters[i].selectIgnore) continue;

            if (allCellClusters[i].cells.Count == 0 || allCellClusters[i].AreAnyPotentialCellsAssigned() || allCellClusters[i].AreAnyInnerNeighborCellsAssigned())
            {
                Debug.Log("SelectCellCluster 0: id: " + allCellClusters[i].id);

                SetIgnoreCellCluster(allCellClusters[i]);
            }
            else
            {
                Debug.Log("SelectCellCluster 1");

                // Make sure there is a compatible Tile
                List<GameObject> tileClusterSet = SelectClusterTileSet(allCellClusters[i]);
                if (tileClusterSet.Count == 0)
                {
                    Debug.Log("NO Compatible Cluster Tile Found for id: " + allCellClusters[i].id);
                    SetIgnoreCellCluster(allCellClusters[i]);
                }
                else
                {

                    GameObject clusterMainTile = SelectMainClusterTile(allCellClusters[i]);
                    allCellClusters[i].mainTile = clusterMainTile;

                    // Assign cluster cells
                    allCellClusters[i].AssignCells();
                    selected = allCellClusters[i];

                    AssignTilesToCluster(selected, tileClusterSet);

                    // Add to active list
                    activeCellClusters.Add(selected);

                    SetIgnoreCellCluster(allCellClusters[i]);
                    break;
                }
            }
        }
        return selected;
    }

    private List<GameObject> SelectClusterTileSet(HexagonCellCluster cluster)
    {
        List<GameObject> tileClusterSet = new List<GameObject>();

        // Assign the center cell first if 7 cells
        if (cluster.cells.Count == 7)
        {
            cluster.EvaluateParentCell();

            HexagonCell startCell = cluster.GetParentCell();
            HexagonTile startTile = tilePrefabs_ClusterCenter;
            AssignTileToCell(startCell, startTile, 0);
            tileClusterSet.Add(startTile.gameObject);

            Debug.Log("startTile: " + startTile.name + ", startCell: " + startCell.id);
        }

        foreach (HexagonCell cell in cluster.cells)
        {
            if (cell.IsAssigned()) continue;

            // Debug.Log("SelectClusterTileSet cell: " + cell.id);

            // (HexagonTile nextTile, List<int> rotations) = SelectNextTile(cell, tilePrefabs_ClusterSet, true);

            // if (nextTile == null)
            // {
            //     Debug.LogError("No tile found for cluster cell: " + cell.id + ", Cluster: " + cluster.id + ", cells: " + cluster.cells.Count + ", tilesFilled: " + tileClusterSet.Count);
            //     for (int i = 0; i < tileClusterSet.Count; i++)
            //     {
            //         Debug.LogError("tile: " + i + ", " + tileClusterSet[i].gameObject.name);
            //     }
            // }
            //AssignTileToCell(cell, nextTile, rotations);
            HexagonTile nextTile = tilePrefabs_ClusterCenter;
            AssignTileToCell(cell, nextTile, 0);
            tileClusterSet.Add(nextTile.gameObject);
        }
        return tileClusterSet;
    }
    private GameObject SelectMainClusterTile(HexagonCellCluster cluster)
    {
        HexagonTileCluster tileCluster = null;

        Debug.Log("SelectMainClusterTile 0");

        for (int i = 0; i < tilePrefabs_cluster.Count; i++)
        {
            Debug.Log("SelectClusterTile 0: tilePrefabs_cluster[" + i + "]: " + tilePrefabs_cluster[i].id);

            if (IsTileClusterCompatible(cluster, tilePrefabs_cluster[i]))
            {
                // Debug.Log("SelectClusterTile 1: tilePrefabs_cluster[" + i + "]: " + tilePrefabs_cluster[i].id);
                tileCluster = tilePrefabs_cluster[i];
                break;
            }
        }
        if (tileCluster != null) return tileCluster.transform.gameObject;
        return null;
    }

    private bool IsTileClusterCompatible(HexagonCellCluster cluster, HexagonTileCluster tileCluster)
    {
        if (cluster == null || tileCluster == null) return false;
        Debug.Log("IsTileClusterCompatible 0");

        if (tileCluster.id < 0) return false;
        Debug.Log("IsTileClusterCompatible 1");

        if (tileCluster.GetCellSize() != cluster.cells[0].size) return false;
        // if (tileCluster.GetCellSize() != cluster.cells[0].size) return false;
        Debug.Log("IsTileClusterCompatible 2:  tileCluster: " + tileCluster.id + ", cell count: " + tileCluster.GetCellCount() + " != " + cluster.cells.Count);

        if (tileCluster.GetCellCount() > cluster.cells.Count) return false;
        return true;
    }

    private void AssignTilesToCluster(HexagonCellCluster cluster, List<GameObject> tileClusterSet)
    {
        cluster.SetTiles(tileClusterSet);
    }

    private HexagonCell SelectRandomStartCell()
    {
        // Select Random Edge Cell
        if (totalEdgeCells > 0)
        {
            return cells[0]; // edge cells should be ordered by the fewest neighbors first;
        }
        return cells[UnityEngine.Random.Range(0, cells.Count)];
    }

    private HexagonTile SelectRandomTile(bool edgeTile = false)
    {
        // Select Random Edge Cell
        if (edgeTile && totalEdgeCells > 0 && tilePrefabs_Edgable.Count > 0)
        {
            return tilePrefabs_Edgable[0];
            // return tilePrefabs_Edgable[UnityEngine.Random.Range(0, tilePrefabs_Edgable.Count)];
        }
        return tilePrefabs[UnityEngine.Random.Range(0, tilePrefabs.Count)];
    }

    private void AssignTileToCell(HexagonCell cell, HexagonTile tile, int rotation = 0)
    {
        cell.SetTile(tile, rotation);
    }
    private void AssignTileToCell(HexagonCell cell, HexagonTile tile, List<int> rotations)
    {
        if (ignoreFailures && tile == null)
        {
            cell.highlight = true;
        }
        else
        {
            cell.SetTile(tile, rotations[UnityEngine.Random.Range(0, rotations.Count)]);
            cell.highlight = false;
        }
    }

    private void SelectNext()
    {
        bool useCluster = enableClusters && availableCellClusters > 0 && (100 * clusterChance) >= (UnityEngine.Random.Range(0, 100));
        if (useCluster) Debug.Log("useCluster");

        HexagonCellCluster selected = useCluster ? SelectCellCluster() : null;
        // Cluster assigned, end here
        if (selected != null) return;

        HexagonCell nextCell = SelectNextCell(cells);

        (HexagonTile nextTile, List<int> rotations) = SelectNextTile(nextCell, tilePrefabs, false);

        if (nextTile == null) nextCell.isIgnored = true;

        if (nextTile == null && !ignoreFailures)
        {
            Debug.LogError("No tile found for cell: " + nextCell.id);

            for (int i = 0; i < nextCell._neighbors.Count; i++)
            {
                Debug.LogError("neighbor: " + nextCell._neighbors[i].id + ",");
            }
        }

        // Assign tile to the next cell
        AssignTileToCell(nextCell, nextTile, rotations);
    }


    HexagonCell SelectNextCell(List<HexagonCell> cellList)
    {
        HexagonCell nextCell = null;

        // Iterate through the cells
        for (int i = 0; i < cellList.Count; i++)
        {
            HexagonCell currentCell = cellList[i];

            // If the current cell is unassigned
            if (currentCell.IsAssigned() == false)
            {
                currentCell.highlight = true;
                nextCell = currentCell;
            }
        }
        Debug.Log("SelectNextCell - nextCell: " + nextCell.id);

        return nextCell;
    }

    private (HexagonTile, List<int>) SelectNextTile(HexagonCell cell, List<HexagonTile> prefabsList, bool IsClusterCell)
    {
        // Create a list of compatible tiles and their rotations
        List<(HexagonTile, List<int>)> compatibleTilesAndRotations = new List<(HexagonTile, List<int>)>();

        // Iterate through all tiles
        for (int i = 0; i < prefabsList.Count; i++)
        {
            if (cell.isEntryCell && !prefabsList[i].isEntrance) continue;

            if (prefabsList[i].baseLayerOnly && cell.GetGridLayer() > 0) continue;

            // if (IsClusterCell && prefabsList[i].GetInnerClusterSocketCount() != cell.GetNumberofNeighborsInCluster()) continue;

            HexagonTile currentTile = prefabsList[i];

            List<int> compatibleTileRotations = GetCompatibleTileRotations(cell, currentTile);

            if (compatibleTileRotations.Count > 0)
            {
                compatibleTilesAndRotations.Add((currentTile, compatibleTileRotations));
            }
        }
        // If there are no compatible tiles, return null
        if (compatibleTilesAndRotations.Count == 0)
        {
            if (cell.isEntryCell)
            {
                Debug.LogError("No compatible tiles for Entry Cell: " + cell.id);
            }
            else if (cell.isEdgeCell)
            {
                Debug.LogError("No compatible tiles for Edge Cell: " + cell.id);
            }
            else
            {
                Debug.Log("No compatible tiles for cell: " + cell.id);
            }
            return (null, null);
        }

        // Select a random compatible tile and rotation
        int randomIndex = UnityEngine.Random.Range(0, compatibleTilesAndRotations.Count);
        return compatibleTilesAndRotations[randomIndex];
    }

    // private List<int> GetCompatibleTileRotations(HexagonCell currentCell, HexagonTile currentTile)
    // {
    //     int[] neighborTileSockets = currentCell.GetNeighborTileSockets(isWalledEdge);
    //     List<int> compatibleRotations = new List<int>();

    //     // // Check top & bottom compatibility
    //     // if (currentCell.GetGridLayer() > 0)
    //     // {
    //     //     int tileBottomSocket = currentTile.layeredSocketIds[0];
    //     //     int bottomNeighborTileTopSocket = currentCell.GetBottomNeighborTopTileSocket();

    //     //     Debug.Log("cell " + currentCell.id + ", Tile: " + currentTile.gameObject.name + ", tileBottomSocket " + tileBottomSocket + ", bottomNeighborTileTopSocket: " + bottomNeighborTileTopSocket);

    //     //     if (tileBottomSocket != -1 && bottomNeighborTileTopSocket != -1 && !compatibilityMatrix[tileBottomSocket, bottomNeighborTileTopSocket])
    //     //     {
    //     //         return compatibleRotations;
    //     //     }
    //     // }

    //     // Check every rotation
    //     for (int rotation = 0; rotation < 6; rotation++)
    //     {
    //         bool compatibile = true;
    //         // Check that all neighborTileSockets are compatibile
    //         for (int nIX = 0; nIX < neighborTileSockets.Length; nIX++)
    //         {
    //             int rotatedSideSocket = currentTile.GetRotatedSideSocketId((HexagonSides)nIX, rotation);
    //             if (neighborTileSockets[nIX] != -1 && !compatibilityMatrix[rotatedSideSocket, neighborTileSockets[nIX]])
    //             {
    //                 compatibile = false;
    //                 break;
    //             }
    //         }

    //         if (compatibile) compatibleRotations.Add(rotation);
    //     }
    //     // Debug.Log("Cell: " + currentCell.id + ", compatibleRotations: " + compatibleRotations.Count);
    //     return compatibleRotations;
    // }

    private List<int> GetCompatibleTileRotations(HexagonCell currentCell, HexagonTile currentTile)
    {
        List<int> compatibleRotations = new List<int>();

        HexagonCell.NeighborSideCornerSockets[] neighborTileCornerSocketsBySide = currentCell.GetSideNeighborTileSockets(isWalledEdge);

        string tileName = currentTile.gameObject.name;
        if (currentCell.id == "0-4" && neighborTileCornerSocketsBySide.Length > 0)
        {
            Debug.LogError("neighborTileCornerSocketsBySide: " + currentCell.id + ", currentTile: " + currentTile.gameObject.name);

            for (int i = 0; i < neighborTileCornerSocketsBySide.Length; i++)
            {
                Debug.LogError("side: " + (HexagonSide)i + ", bottomCorners: " + neighborTileCornerSocketsBySide[i].bottomCorners[0] + ", " + neighborTileCornerSocketsBySide[i].bottomCorners[1]);
                Debug.LogError("side: " + (HexagonSide)i + ", topCorners: " + neighborTileCornerSocketsBySide[i].topCorners[0] + ", " + neighborTileCornerSocketsBySide[i].topCorners[1]);
            }
        }

        // Check every rotation
        for (int rotation = 0; rotation < 6; rotation++)
        {
            bool compatibile = true;

            for (int side = 0; side < neighborTileCornerSocketsBySide.Length; side++)
            {
                HexagonCell.NeighborSideCornerSockets neighborSide = neighborTileCornerSocketsBySide[side];

                (int[] currentTileBottomSockets, int[] currentTileTopSockets) = currentTile.GetRotatedCornerSocketsBySide((HexagonSide)side, rotation);

                if (!compatibilityMatrix[currentTileBottomSockets[0], neighborSide.bottomCorners[1]])
                {
                    if (currentCell.id == "0-4")
                    {
                        Debug.LogError(tileName + " Not compatibile - side: " + (HexagonSide)side + ", currentTileBottomSocketA: " + currentTileBottomSockets[0] + ", neighborSideB: " + neighborSide.bottomCorners[1]);
                    }
                    Debug.Log("Not compatibile - 0");
                    // Debug.Log("Not compatibile - side: " + side + ", currentTileBottomSockets[0]: " + currentTileBottomSockets[0] + ", neighborSide.bottomCorners[0]: " + neighborSide.bottomCorners[0]);

                    compatibile = false;
                    break;
                }
                if (!compatibilityMatrix[currentTileBottomSockets[1], neighborSide.bottomCorners[0]])
                {
                    if (currentCell.id == "0-4")
                    {
                        Debug.LogError(tileName + " Not compatibile - side: " + (HexagonSide)side + ", currentTileBottomSocketB: " + currentTileBottomSockets[1] + ", neighborSideA: " + neighborSide.bottomCorners[0]);
                    }
                    Debug.Log("Not compatibile - 1");

                    compatibile = false;
                    break;
                }

                if (!compatibilityMatrix[currentTileTopSockets[0], neighborSide.topCorners[1]])
                {
                    if (currentCell.id == "0-4")
                    {
                        Debug.LogError(tileName + " Not compatibile - side: " + (HexagonSide)side + ", currentTileBottomSocketA: " + currentTileBottomSockets[0] + ", neighborSideB: " + neighborSide.bottomCorners[1]);
                    }

                    Debug.Log("Not compatibile - 2");

                    compatibile = false;
                    break;
                }
                if (!compatibilityMatrix[currentTileTopSockets[1], neighborSide.topCorners[0]])
                {
                    if (currentCell.id == "0-4")
                    {
                        Debug.LogError(tileName + " Not compatibile - side: " + (HexagonSide)side + ", currentTileBottomSocketB: " + currentTileBottomSockets[1] + ", neighborSideA: " + neighborSide.bottomCorners[0]);
                    }
                    Debug.Log("Not compatibile - 3");

                    compatibile = false;
                    break;
                }

            }
            if (compatibile)
            {
                compatibleRotations.Add(rotation);
            }
        }

        if (currentCell.id == "0-4" && compatibleRotations.Count == 0)
        {
            Debug.Log("Not compatibile: currentTile - Cell: " + currentTile.gameObject.name);
        }

        Debug.Log("GetCompatibleLayeredTileRotations - Cell: " + currentCell.id + ", compatibleRotations: " + compatibleRotations.Count);
        return compatibleRotations;
    }

    private List<int> GetCompatibleLayeredTileRotations(HexagonCell currentCell, HexagonTile currentTile)
    {
        List<int> compatibleRotations = new List<int>();

        // Check bottom 12 corners of top against the top 12 corners of bottom tile
        // get currentTile bottom edges
        // get bottom Tile top edges
        /// <summary>
        /// for every rotation 
        ///     check the rotated sockets
        /// </summary>

        // Check every rotation
        for (int rotation = 0; rotation < 6; rotation++)
        {
            bool compatibile = true;

            int[] currentTileBottomSockets = new int[12];
            currentTileBottomSockets[0] = currentTile.GetRotatedCornerSocketId(HexagonCorner.FrontA, rotation, false);
            currentTileBottomSockets[1] = currentTile.GetRotatedCornerSocketId(HexagonCorner.FrontB, rotation, false);
            currentTileBottomSockets[2] = currentTile.GetRotatedCornerSocketId(HexagonCorner.FrontRightA, rotation, false);
            currentTileBottomSockets[3] = currentTile.GetRotatedCornerSocketId(HexagonCorner.FrontRightB, rotation, false);
            currentTileBottomSockets[4] = currentTile.GetRotatedCornerSocketId(HexagonCorner.BackRightA, rotation, false);
            currentTileBottomSockets[5] = currentTile.GetRotatedCornerSocketId(HexagonCorner.BackRightB, rotation, false);
            currentTileBottomSockets[6] = currentTile.GetRotatedCornerSocketId(HexagonCorner.BackA, rotation, false);
            currentTileBottomSockets[7] = currentTile.GetRotatedCornerSocketId(HexagonCorner.BackB, rotation, false);
            currentTileBottomSockets[8] = currentTile.GetRotatedCornerSocketId(HexagonCorner.BackLeftA, rotation, false);
            currentTileBottomSockets[9] = currentTile.GetRotatedCornerSocketId(HexagonCorner.BackLeftB, rotation, false);
            currentTileBottomSockets[10] = currentTile.GetRotatedCornerSocketId(HexagonCorner.FrontLeftA, rotation, false);
            currentTileBottomSockets[11] = currentTile.GetRotatedCornerSocketId(HexagonCorner.FrontLeftB, rotation, false);

            int[] bottomTileSockets = currentCell.GetBottomNeighborTileSockets(true);

            if (bottomTileSockets == null)
            {
                compatibile = false;
                break;
            }

            for (int i = 0; i < currentTileBottomSockets.Length; i++)
            {
                if (!compatibilityMatrix[currentTileBottomSockets[i], bottomTileSockets[i]])
                {
                    compatibile = false;
                    break;
                }
            }

            if (!compatibile) break;
            if (compatibile) compatibleRotations.Add(rotation);
        }

        Debug.Log("GetCompatibleLayeredTileRotations - Cell: " + currentCell.id + ", compatibleRotations: " + compatibleRotations.Count);
        return compatibleRotations;
    }

    // private bool AreTilesCompatible(HexagonCell current, HexagonTile tile, int rotation)
    // {
    //     Debug.Log("AreTilesCompatible - tile: " + tile.id);
    //     if (tile == null) return false;

    //     // Check neighbors for compatibility
    //     for (int neighborSideIndex = 0; neighborSideIndex < 6; neighborSideIndex++)
    //     {
    //         // Debug.Log("current.neighborsBySide.Count: " + current.neighborsBySide.Count + ", current.neighborsBySide[]: " + current.neighborsBySide[neighborSideIndex]);
    //         HexagonCell neighbor = current.neighborsBySide[neighborSideIndex];
    //         int tileSideSocket = tile.sideSocketIds[(neighborSideIndex + rotation) % 6];

    //         // Handle Edge Cells
    //         if (neighbor == null || current.isEdgeCell)
    //         {
    //             int rotatedSideSocket;

    //             // if using walled edges, treat non edge cells like Wall Inner sockets
    //             if (isWalledEdge && !neighbor.isEdgeCell)
    //             {
    //                 rotatedSideSocket = wallInnerSlotId;
    //             }
    //             else
    //             {
    //                 rotatedSideSocket = tile.GetRotatedSideSocketId((HexagonSides)neighborSideIndex, rotation);
    //             }

    //             Debug.Log("Edge Cell: " + tile.id + ", rotatedSideSocket: " + rotatedSideSocket);

    //             if (!compatibilityMatrix[rotatedSideSocket, outOfBoundsSlotId])
    //             {
    //                 Debug.Log("Tile incompatible with Edge: " + tile.id + ", rotatedSideSocket: " + rotatedSideSocket + ",  outOfBoundsSlotId: " + outOfBoundsSlotId);

    //                 return false;
    //             }
    //         }
    //         else
    //         {
    //             HexagonTile neighborTile = neighbor.GetTile();

    //             if (neighborTile != null)
    //             {
    //                 int neighborRelativeSide = current.GetNeighborsRelativeSide((HexagonSides)neighborSideIndex);
    //                 // int neighborSideSocket = neighbor.currentTile.sideSocketIds[(neighborRelativeSide + rotation) % 6];
    //                 int neighborSideSocket = neighborTile.GetSideSocketId((HexagonSides)((neighborRelativeSide + rotation) % 6));
    //                 int rotatedSideSocket = tile.GetRotatedSideSocketId((HexagonSides)neighborSideIndex, rotation);

    //                 // Debug.Log("compatibilityMatrix.length: " + compatibilityMatrix.Length + ", tileSide: " + tileSideSocket + ", neighborSideSocket: " + neighborSideSocket);

    //                 // if (!compatibilityMatrix[tileSideSocket, neighborSideSocket])
    //                 if (!compatibilityMatrix[rotatedSideSocket, neighborSideSocket])
    //                 {
    //                     return false;
    //                 }
    //             }
    //         }

    //     }
    //     return true;
    // }

    // public void InitializeTileProbabilities()
    // {
    //     probabilityMatrix = new int[cells.Count, tileLookupByid.Count];

    //     // Iterate through all cells
    //     for (int i = 0; i < cells.Count; i++)
    //     {
    //         HexagonCell currentCell = cells[i];
    //         currentCell.highestProbability = 0; // reset highestProbability to 0
    //                                             // Iterate through all tiles
    //         for (int j = 0; j < tilePrefabs.Count; j++)
    //         {
    //             HexagonTile currentTile = tilePrefabs[j];
    //             // Check the compatibility of the tile with the current cell
    //             if (AreTilesCompatible(currentCell, currentTile, 0))
    //             {
    //                 probabilityMatrix[currentCell.id, currentTile.id] = 1;
    //             }
    //             else
    //             {
    //                 probabilityMatrix[currentCell.id, currentTile.id] = 0;
    //             }
    //             // Update the highestProbability for the current cell
    //             if (probabilityMatrix[currentCell.id, currentTile.id] > currentCell.highestProbability)
    //             {
    //                 currentCell.highestProbability = probabilityMatrix[currentCell.id, currentTile.id];
    //             }
    //         }
    //     }
    // }

    // private void UpdateTileProbabilities(HexagonCell currentCell)
    // {
    //     // Initialize a sum of probabilities for normalization
    //     int probabilitySum = 0;

    //     // Iterate through all tile prefabs
    //     for (int i = 0; i < tilePrefabs.Count; i++)
    //     {
    //         // Get the current tile
    //         HexagonTile currentTile = tilePrefabs[i];
    //         // Initialize a probability product for the current tile
    //         int probabilityProduct = 1;

    //         // Iterate through the neighboring cells
    //         for (int j = 0; j < currentCell._neighbors.Count; j++)
    //         {
    //             HexagonCell neighbor = currentCell._neighbors[j];
    //             if (neighbor.currentTile != null)
    //             {
    //                 // check for compatibility by comparing sideSocketIds
    //                 int currentTileSideSocketId = currentTile.GetSideSocketId((HexagonSides)j);
    //                 int neighborTileSideSocketId = neighbor.currentTile.GetSideSocketId((HexagonSides)j);
    //                 if (currentTileSideSocketId == neighborTileSideSocketId)
    //                 {
    //                     probabilityProduct *= 1;
    //                 }
    //                 else
    //                 {
    //                     probabilityProduct *= 0;
    //                 }
    //             }
    //             else
    //             {
    //                 probabilityProduct *= 1;
    //             }
    //         }
    //         // Update the probability for the current tile
    //         probabilityMatrix[currentCell.id, currentTile.id] = probabilityProduct;
    //         // Add the current probability to the probability sum
    //         probabilitySum += probabilityProduct;
    //     }
    //     // Normalize the probabilities
    //     NormalizeProbabilities(currentCell, probabilitySum);
    // }
    // private void UpdateNeighborCellProbabilities(HexagonCell currentCell, HexagonTile currentTile)
    // {
    //     Debug.Log("UpdateNeighboringCellProbabilities: currentTile:" + currentTile);

    //     // Iterate through the neighboring cells
    //     for (int i = 0; i < currentCell._neighbors.Count; i++)
    //     {
    //         HexagonCell neighbor = currentCell._neighbors[i];
    //         // Skip the current cell
    //         if (neighbor.currentTile != null)
    //         {

    //             Debug.Log("neighbor: " + neighbor + ", currentTile:" + currentTile);


    //             // check for compatibility
    //             for (int j = 0; j < 6; j++)
    //             {
    //                 if (!AreTilesCompatible(neighbor, currentTile, j))
    //                 {
    //                     // Mark the current tile as incompatible with the neighbor
    //                     probabilityMatrix[neighbor.id, currentTile.id] = 0;
    //                 }
    //                 else
    //                 {


    //                     // Update the highestProbability for the current cell
    //                     if (probabilityMatrix[neighbor.id, currentTile.id] > neighbor.highestProbability)
    //                     {
    //                         neighbor.highestProbability = probabilityMatrix[neighbor.id, currentTile.id];
    //                     }
    //                 }
    //             }
    //         }
    //     }
    // }

    public int[,] CreateTileProbabilityMatrix(List<HexagonCell> cells, List<HexagonTile> tilePrefabs)
    {
        int numTiles = tilePrefabs.Count;
        int[,] newProbabilityMatrix = new int[cells.Count, numTiles];

        // Iterate through each cell in the grid
        for (int i = 0; i < cells.Count; i++)
        {
            HexagonCell currentCell = cells[i];

            // Iterate through each tile prefab
            for (int j = 0; j < numTiles; j++)
            {
                HexagonTile currentTile = tilePrefabs[j];
                int probability = 1;

                // Assign the probability for the current tile to the current cell
                newProbabilityMatrix[i, j] = probability;
            }
        }

        return newProbabilityMatrix;
    }

    // private void Reset()
    // {
    //     // Reset all cells
    //     for (int i = 0; i < cells.Count; i++)
    //     {
    //         HexagonCell cell = cells[i];
    //         cell.currentTile = null;
    //     }

    //     // Reset all tile probabilities
    //     InitializeTileProbabilities();
    // }

    // Instantiate the tiles in the appropriate positions to create the final pattern
    void InstantiateTiles()
    {

        Transform folder = new GameObject("Tiles").transform;
        folder.transform.SetParent(gameObject.transform);

        // Instantiate clusters first
        for (int i = 0; i < activeCellClusters.Count; i++)
        {
            GameObject prefab = activeCellClusters[i].mainTile;

            if (prefab == null) continue;

            // int rotation = activeCellClusters[i].GetTileRotation();

            Vector3 pos = activeCellClusters[i].GetFoundationCenter();
            pos.y += 0.2f;

            GameObject activeTile = Instantiate(prefab.gameObject, pos, Quaternion.identity);

            // GameObject instantiatedParent = Instantiate(markerPrefab, spawnPosition, Quaternion.identity);

            // GameObject activeTile = Instantiate(prefab.gameObject, instantiatedParent.transform.localPosition, Quaternion.identity);
            // activeTile.gameObject.transform.rotation = Quaternion.Euler(0f, rotationValues[rotation], 0f);
            // activeTile.transform.position = instantiatedParent.transform.position;
            // activeTile.transform.SetParent(instantiatedParent.transform);
            // activeTile.transform.position = instantiatedParent.transform.TransformPoint(instantiatedParent.transform.position);

            // activeTile.transform.position = instantiatedParent.transform.localPosition;
            // instantiatedParent.transform.position = Vector3.zero;

            // CenterGameObjectAtPosition(activeTile, instantiatedParent.transform.position);


            // Vector3 offset = activeTile.transform.position - instantiatedParent.transform.position;
            // activeTile.transform.position = activeTile.transform.position + offset;



            // instantiatedParent.transform.position = Vector3.zero;

            // activeTile.transform.position = 
            // Vector3 offset = activeTile.transform.position - activeCellClusters[i].center;

            // activeTile.transform.position = activeTile.transform.position + offset;
            // Debug.Log("offset.x: " + offset.x + ", offset.z: " + offset.z);

            activeTiles.Add(activeTile);

            activeTile.transform.SetParent(folder);
        }

        for (int i = 0; i < edgeCells.Count; i++)
        {
            // Skip cluster assigned cells
            // if (cells[i].IsInCluster()) continue;

            HexagonTile prefab = edgeCells[i].GetTile();

            if (prefab == null) continue;


            int rotation = edgeCells[i].currentRotation;

            Vector3 position = edgeCells[i].transform.position;
            position.y += 0.2f;

            GameObject activeTile = Instantiate(prefab.gameObject, position, Quaternion.identity);
            activeTile.gameObject.transform.rotation = Quaternion.Euler(0f, rotationValues[rotation], 0f);
            activeTiles.Add(activeTile);

            activeTile.transform.SetParent(folder);

        }

        for (int i = 0; i < cells.Count; i++)
        {
            // Skip cluster assigned cells
            // if (cells[i].IsInCluster()) continue;

            HexagonTile prefab = cells[i].GetTile();

            if (prefab == null) continue;

            int rotation = cells[i].currentRotation;

            Vector3 position = cells[i].transform.position;
            position.y += 0.2f;

            GameObject activeTile = Instantiate(prefab.gameObject, position, Quaternion.identity);
            activeTile.gameObject.transform.rotation = Quaternion.Euler(0f, rotationValues[rotation], 0f);
            activeTiles.Add(activeTile);
            activeTile.transform.SetParent(folder);

        }
    }



    private void EvaluateTiles()
    {
        // Get All Tile Prefabs
        tileLookupByid = tileDirectory.CreateTileDictionary();
        List<HexagonTile> _tilePrefabs = tileLookupByid.Select(x => x.Value).ToList();

        // Check For Nulls
        foreach (HexagonTile prefab in tilePrefabs)
        {
            int id = prefab.id;
        }

        // Get All Cluster Prefabs
        tileClusterLookupByid = tileDirectory.CreateTileClusterDictionary();
        tilePrefabs_cluster = tileClusterLookupByid.Select(x => x.Value).ToList();

        // Get All Cluster Sets
        tilePrefabs_ClusterSet = HexagonTile.ExtractClusterSetTiles(_tilePrefabs);

        tilePrefabs = new List<HexagonTile>();
        tilePrefabs.AddRange(_tilePrefabs.Except(tilePrefabs_ClusterSet));

        tilePrefabs_ClusterSet = tilePrefabs_ClusterSet.FindAll(x => x.isClusterCenterTile == false);

        // Extract Edge Tiles
        tilePrefabs_Edgable = tilePrefabs.FindAll(x => x.isEdgeable).ToList();

        // Extract Leveled Tiles
        tilePrefabs_Leveled = tilePrefabs.FindAll(x => x.isLeveledTile).ToList();

        // Extract Layer Connector Tiles
        tilePrefabs_LayerConnector = tilePrefabs.FindAll(x => x.isLayerConnector).ToList();

        // Extract Layer Connector Tiles
        tilePrefabs_Path = tilePrefabs.FindAll(x => x.isPath).ToList();

        // Extract Inner Tiles
        List<HexagonTile> innerTilePrefabs = new List<HexagonTile>();
        innerTilePrefabs.AddRange(tilePrefabs.Except(tilePrefabs.FindAll(x => x.IsExteriorWall() || x.isEntrance || x.isLayerConnector || x.isPath)));

        tilePrefabs = innerTilePrefabs;

        // Shuffle the prefabs
        ShuffleTiles(tilePrefabs);
        ShuffleTiles(tilePrefabs_cluster);
    }
    private void EvaluateCells()
    {
        // Place edgeCells first 
        edgeCells = HexagonCell.GetEdgeCells(cells);
        totalEdgeCells = edgeCells.Count;

        entryCells = HexagonCell.GetRandomEntryCells(edgeCells, maxEntrances, true);

        // TODO: find a better way to jsut provide this without doing this extra stuff
        totalLayers = edgeCells.OrderByDescending(c => c.GetGridLayer()).ToList()[0].GetGridLayer() + 1;

        allCellClusters = HexagonCellCluster.GetHexagonCellClusters(cells, transform.position, collapseOrder, isWalledEdge);
        availableCellClusters = allCellClusters.Count;


        pathCells = new List<HexagonCell>();
        if (generatePaths)
        {
            // Debug.Log("entryCells: " + entryCells.Count);
            pathCells = HexagonCell.GetRandomCellPaths(entryCells, cells, transform.position, false);
        }
        else
        {
            pathCells = cells.FindAll(c => c.isPathCell);
        }
        pathCells = HexagonCell.ClearSoloPathCells(pathCells);

        allLeveledCells = HexagonCell.GetRandomLeveledCells(cells.FindAll(c => c.isEdgeCell == false), subZone.radius * 0.5f, true);
        List<HexagonCell> leveledEdgeCells = HexagonCell.GetLeveledEdgeCells(allLeveledCells, true);
        HexagonCell.GetRandomLeveledRampCells(leveledEdgeCells, 4, true, true);

        // Temp
        subZone.cellClusters = allCellClusters;

        List<HexagonCell> _processedCells = new List<HexagonCell>();

        if (isWalledEdge == false) _processedCells.AddRange(edgeCells);

        _processedCells.AddRange(cells.Except(edgeCells).OrderByDescending(c => c.GetGridLayer()));

        cells = _processedCells;
    }

    private void UpdateCompatibilityMatrix()
    {
        socketMatrixGenerator = GetComponent<TileSocketMatrixGenerator>();

        compatibilityMatrix = socketMatrixGenerator.GetCompatibilityMatrix();

        if (compatibilityMatrix.Length == 0)
        {
            Debug.LogError("compatibilityMatrix is unset");
            return;
        }
    }

    private void Awake()
    {
        subZone = GetComponent<SubZone>();
        zoneCellManager = GetComponent<ZoneCellManager>();

        UpdateCompatibilityMatrix();
    }

    void CenterGameObjectAtPosition(GameObject go, Vector3 position)
    {
        // Get the renderer bounds of the GameObject
        Renderer renderer = go.GetComponent<Renderer>();
        Bounds bounds = renderer.bounds;

        // Calculate the center point of the GameObject
        Vector3 centerPoint = bounds.center;

        // Set the position of the Transform to the desired position minus the calculated center point
        go.transform.position = position - centerPoint;
    }


    public void ShuffleTiles(List<HexagonTile> tiles)
    {
        int n = tiles.Count;
        for (int i = 0; i < n; i++)
        {
            // Get a random index from the remaining elements
            int r = i + UnityEngine.Random.Range(0, n - i);
            // Swap the current element with the random one
            HexagonTile temp = tiles[r];
            tiles[r] = tiles[i];
            tiles[i] = temp;
        }
    }
    public void ShuffleTiles(List<HexagonTileCluster> tiles)
    {
        int n = tiles.Count;
        for (int i = 0; i < n; i++)
        {
            // Get a random index from the remaining elements
            int r = i + UnityEngine.Random.Range(0, n - i);
            // Swap the current element with the random one
            HexagonTileCluster temp = tiles[r];
            tiles[r] = tiles[i];
            tiles[i] = temp;
        }
    }
}

