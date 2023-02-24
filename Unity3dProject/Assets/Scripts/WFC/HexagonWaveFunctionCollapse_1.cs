using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;

namespace WFCSystem
{
    public class HexagonWaveFunctionCollapse_1 : MonoBehaviour, IWFCSystem
    {
        [Header("General Settings")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private HexagonSocketDirectory socketDirectory;
        [SerializeField] private TileDirectory tileDirectory;
        [SerializeField] private WFCCollapseOrder collapseOrder = 0;
        [Range(1, 10)][SerializeField] private int minEntrances = 1;
        [Range(1, 10)][SerializeField] private int maxEntrances = 2;
        [SerializeField] private bool isWalledEdge;

        [Header("Tile Settings")]
        [Range(0f, 1f)][SerializeField] private float leaveGroundCellEmptyChance = 0;

        [Header("Pathing")]
        [SerializeField] private bool generatePaths = true;
        [SerializeField] private bool useTerrainMeshPath = true;
        [Header("Layering")]
        [Range(0f, 1f)][SerializeField] private float leveledCellChance = 0.8f;
        [Range(0.1f, 1f)][SerializeField] private float leveledCellRadiusMult = 0.5f;
        [Range(1, 10)][SerializeField] private int leveledCellClumpSets = 5;
        [Range(0.1f, 1f)][SerializeField] private float layerStackChance = 0.5f;
        [SerializeField] private int totalLayers = 1;

        [Header("Clusters")]
        [SerializeField] private bool enableClusters;
        [Range(0.1f, 1f)][SerializeField] private float clusterChance = 0.5f;


        [Header("System Settings")]
        [Range(1, 9999)][SerializeField] private int maxAttempts = 9999;
        [SerializeField] private int remainingAttempts;
        [SerializeField] private bool ignoreFailures = true;

        [Header("Grid Settings")]
        [SerializeField] private int _radius;
        public void SetRadius(int value)
        {
            _radius = value;
        }


        //Temp
        [SerializeField] private SubZone subZone;
        [SerializeField] private ZoneCellManager zoneCellManager;
        // [SerializeField] private TileSocketMatrixGenerator socketMatrixGenerator;

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

        [Header("Cell Debug")]
        [SerializeField] private List<HexagonCell> allCells;
        [SerializeField] private List<HexagonCell> edgeCells;
        [SerializeField] private List<HexagonCell> entryCells;
        [SerializeField] private List<HexagonCellCluster> activeCellClusters;
        [SerializeField] private List<HexagonCellCluster> allCellClusters;
        [SerializeField] private int availableCellClusters = 0;
        [SerializeField] private int totalEdgeCells = 0;
        public Dictionary<int, List<HexagonCell>> allCellsByLayer;
        public Dictionary<int, List<HexagonCell>> allLevelCellsByLayer;
        public Dictionary<int, List<HexagonCell>> allPathCells;
        public Dictionary<int, List<HexagonCell>> cellClustersByLayer;
        public List<HexagonCell> topLevelCells;

        float[] rotationValues = { 0f, 60f, 120f, 180f, 240f, 300f };

        void Start()
        {
            if (runOnStart) Invoke("ExecuteWFC", 0.2f);
        }

        public void ExecuteWFC()
        {
            EvaluateTiles();

            UpdateCompatibilityMatrix();
            compatibilityMatrix = socketDirectory.GetCompatibilityMatrix();

            // compatibilityMatrix = socketMatrixGenerator.GetCompatibilityMatrix();
            // if (compatibilityMatrix != null) matrixLength = compatibilityMatrix.Length;
            // matrixLength = socketMatrixGenerator.matrix.Length;

            EvaluateCells();

            probabilityMatrix = new int[allCells.Count, tilePrefabs.Count];

            CollapseEntranceCells();
            Debug.Log("Entrance Cells Assigned");

            if (isWalledEdge)
            {
                CollapseEdgeCells();
                Debug.Log("Edge Cells Assigned");
            }

            CollapseLeveledCells();
            Debug.Log("Level Cells Assigned");

            CollapsePathCells();
            Debug.Log("Path Cells Assigned");

            CollapseRemainingCellsByLayer();

            // if (generatePaths)
            // {
            //     CollapsePathCells();
            //     Debug.Log("Path Cells Assigned");
            // }

            // remainingAttempts = maxAttempts;
            // while (remainingAttempts > 0 && IsUnassignedCells())
            // {
            //     remainingAttempts--;
            //     SelectNext();
            // }

            InstantiateAllTiles();

            Debug.Log("Execution of WFC Complete");
        }

        private bool IsUnassignedCells()
        {
            // Iterate through all cells and check if there is any unassigned cell
            for (int i = 0; i < allCells.Count; i++)
            {
                if (allCells[i].IsAssigned() == false) return true;
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
            if (allLevelCellsByLayer == null) return;

            List<HexagonTile> tilePrefabs_LeveledRamp = tilePrefabs_Leveled.FindAll(c => c.isLeveledRamp);
            List<HexagonTile> tilePrefabs_LeveledNoRamp = tilePrefabs_Leveled.FindAll(c => c.isLeveledRamp == false);

            foreach (var kvp in allLevelCellsByLayer)
            {
                int layer = kvp.Key;
                List<HexagonCell> leveledCells = kvp.Value;

                topLevelCells = leveledCells;

                List<HexagonCell> leveledEdgeCells = leveledCells.FindAll(c => c.isLeveledEdge);
                foreach (HexagonCell cell in leveledEdgeCells)
                {

                    if (cell.IsAssigned()) continue;

                    List<HexagonTile> prefabs = (cell.isLeveledRampCell && !useTerrainMeshPath) ? tilePrefabs_LeveledRamp : tilePrefabs_LeveledNoRamp;

                    (HexagonTile nextTile, List<int> rotations) = SelectNextTile(cell, prefabs, false);
                    AssignTileToCell(cell, nextTile, rotations);

                    if (cell.isPathCell) cell.highlight = true;
                }

                foreach (HexagonCell cell in leveledCells)
                {
                    if (cell.IsAssigned()) continue;

                    (HexagonTile nextTile, List<int> rotations) = SelectNextTile(cell, tilePrefabs_Leveled, false);
                    AssignTileToCell(cell, nextTile, rotations);
                }
            }

            foreach (var kvp in allCellsByLayer)
            {
                int layer = kvp.Key;
                List<HexagonCell> layerCells = kvp.Value;

                if (layer == 0) continue;

                foreach (HexagonCell cell in layerCells)
                {
                    if (cell.IsAssigned()) continue;

                    if (!cell.isLeveledGroundCell) continue;

                    (HexagonTile nextTile, List<int> rotations) = SelectNextTile(cell, tilePrefabs, false);
                    AssignTileToCell(cell, nextTile, rotations);
                }
            }
        }

        private void CollapsePathCells()
        {
            Debug.Log("topLevelCells: " + topLevelCells.Count);

            allPathCells = HexagonCell.GenerateRandomCellPaths(entryCells,
                    allCellsByLayer,
                    transform.position);
            // allPathCells = HexagonCell.GenerateRandomCellPaths(entryCells,
            //         topLevelCells.Find(c => c.isLeveledEdge),
            //         allCellsByLayer,
            //         transform.position);

            foreach (var kvp in allPathCells)
            {
                int key = kvp.Key;
                List<HexagonCell> pathCells = kvp.Value;

                foreach (HexagonCell item in pathCells)
                {

                    HexagonCell updatedPathCell = HexagonCell.EvaluateLevelGroundPath(item);
                    if (updatedPathCell != item)
                    {
                        if (useTerrainMeshPath)
                        {
                            updatedPathCell.SetIgnored(true);
                        }
                        else
                        {
                            AssignTileToCell(updatedPathCell, updatedPathCell.isEdgeCell ? tilePrefabs_Leveled.FindAll(t => t.isLeveledRamp)[0] : tilePrefabs_Path[0], 0);
                        }
                    }
                    else
                    {
                        if (item.isLeveledCell)
                        {
                            // Check if a neighbor is a leveled ramp, if not set as ramp
                            List<HexagonCell> layerNeighbors = item.GetLayerNeighbors();
                            bool hasRampAsNeighbor = false;
                            foreach (var neighbor in layerNeighbors)
                            {
                                if (neighbor.isLeveledRampCell && neighbor.isPathCell)
                                {
                                    hasRampAsNeighbor = true;
                                    break;
                                }
                            }
                            if (hasRampAsNeighbor == false)
                            {
                                item.SetLeveledRampCell(true);
                                item.ClearTile();


                                if (useTerrainMeshPath)
                                {
                                    updatedPathCell.SetIgnored(true);
                                }
                                else
                                {
                                    (HexagonTile tile, List<int> rot) = SelectNextTile(item, tilePrefabs_Leveled.FindAll(t => t.isLeveledRamp), false);
                                    AssignTileToCell(item, tile, rot);
                                }
                            }
                            else
                            {
                                item.SetPathCell(false);
                                continue;
                            }
                        }

                        item.SetPathCell(true);


                        if (useTerrainMeshPath)
                        {
                            updatedPathCell.SetIgnored(true);
                        }
                        else
                        {
                            if (item.IsAssigned()) continue;

                            (HexagonTile nextTile, List<int> rotations) = SelectNextTile(item, tilePrefabs_Path, false);
                            AssignTileToCell(item, nextTile, rotations);
                        }
                    }
                }
            }
        }

        private void CollapseRemainingCellsByLayer()
        {
            for (int currentLayer = 0; currentLayer < totalLayers; currentLayer++)
            {
                List<HexagonCell> layerCells;
                List<HexagonCellCluster> layerClusters = new List<HexagonCellCluster>();
                if (currentLayer == 0)
                {
                    layerCells = allCellsByLayer[0].FindAll(c => !c.IsAssigned() && !c.isLeveledCell);
                    // layerClusters = HexagonCellCluster.GetHexagonCellClusters(layerCells, transform.position, collapseOrder, isWalledEdge);
                }
                else
                {
                    layerCells = HexagonCell.GetAvailableCellsForNextLayer(allCellsByLayer[currentLayer]);
                }

                // bool useCluster = enableClusters && layerClusters.Count > 0 && (100 * clusterChance) >= (UnityEngine.Random.Range(0, 100));
                // if (useCluster)
                // {
                //     while (useCluster)
                //     {
                //         HexagonCellCluster selected = SelectCellCluster(layerClusters);
                //         useCluster = selected != null && (100 * clusterChance) >= (UnityEngine.Random.Range(0, 100));
                //     }
                // }

                foreach (HexagonCell cell in layerCells)
                {
                    if (cell.IsAssigned()) continue;

                    cell.highlight = true;

                    (HexagonTile nextTile, List<int> rotations) = SelectNextTile(cell, tilePrefabs, false);

                    if (nextTile == null) cell.SetIgnored(true);

                    if (nextTile == null && !ignoreFailures)
                    {
                        Debug.LogError("No tile found for cell: " + cell.id);

                        for (int i = 0; i < cell._neighbors.Count; i++)
                        {
                            Debug.LogError("neighbor: " + cell._neighbors[i].id + ",");
                        }
                    }

                    // Assign tile to the next cell
                    AssignTileToCell(cell, nextTile, rotations);
                }
            }
        }



        private void SetIgnoreCellCluster(HexagonCellCluster cluster)
        {
            cluster.selectIgnore = true;
            availableCellClusters -= 1;
        }

        private HexagonCellCluster SelectCellCluster(List<HexagonCellCluster> cellClusters)
        {
            if (cellClusters.Count == 0) return null;

            // For now: 
            // A cluster can be selected if none of the neighbor cells are assigned
            HexagonCellCluster selected = null;

            for (int i = 0; i < cellClusters.Count; i++)
            {
                Debug.Log("cellClusters[" + i + "] - id: " + cellClusters[i].id);

                if (cellClusters[i].selectIgnore) continue;

                if (cellClusters[i].cells.Count == 0 || cellClusters[i].AreAnyPotentialCellsAssigned() || cellClusters[i].AreAnyInnerNeighborCellsAssigned())
                {
                    Debug.Log("SelectCellCluster 0: id: " + cellClusters[i].id);

                    SetIgnoreCellCluster(cellClusters[i]);
                }
                else
                {
                    Debug.Log("SelectCellCluster 1");

                    // Make sure there is a compatible Tile
                    List<GameObject> tileClusterSet = SelectClusterTileSet(cellClusters[i]);
                    if (tileClusterSet.Count == 0)
                    {
                        Debug.Log("NO Compatible Cluster Tile Found for id: " + cellClusters[i].id);
                        SetIgnoreCellCluster(cellClusters[i]);
                    }
                    else
                    {

                        GameObject clusterMainTile = SelectMainClusterTile(cellClusters[i]);
                        cellClusters[i].mainTile = clusterMainTile;

                        // Assign cluster cells
                        cellClusters[i].AssignCells();
                        selected = cellClusters[i];

                        AssignTilesToCluster(selected, tileClusterSet);

                        // Add to active list
                        activeCellClusters.Add(selected);

                        SetIgnoreCellCluster(cellClusters[i]);
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

            if (tileCluster.GetCellSize() != cluster.cells[0].GetSize()) return false;
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
                return allCells[0]; // edge cells should be ordered by the fewest neighbors first;
            }
            return allCells[UnityEngine.Random.Range(0, allCells.Count)];
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

        // private void SelectNext()
        // {
        //     bool useCluster = enableClusters && availableCellClusters > 0 && (100 * clusterChance) >= (UnityEngine.Random.Range(0, 100));
        //     if (useCluster) Debug.Log("useCluster");

        //     HexagonCellCluster selected = useCluster ? SelectCellCluster() : null;
        //     // Cluster assigned, end here
        //     if (selected != null) return;

        //     HexagonCell nextCell = SelectNextCell(allCells);

        //     (HexagonTile nextTile, List<int> rotations) = SelectNextTile(nextCell, tilePrefabs, false);

        //     if (nextTile == null) nextCell.SetIgnored(true);

        //     if (nextTile == null && !ignoreFailures)
        //     {
        //         Debug.LogError("No tile found for cell: " + nextCell.id);

        //         for (int i = 0; i < nextCell._neighbors.Count; i++)
        //         {
        //             Debug.LogError("neighbor: " + nextCell._neighbors[i].id + ",");
        //         }
        //     }

        //     // Assign tile to the next cell
        //     AssignTileToCell(nextCell, nextTile, rotations);
        // }


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

                if (prefabsList[i].isLeveledTile && !cell.isLeveledCell) continue;

                if (cell.isLeveledRampCell && !prefabsList[i].isLeveledRamp) continue;

                if (prefabsList[i].baseLayerOnly && cell.GetGridLayer() > 0) continue;

                if (prefabsList[i].noBaseLayer && cell.GetGridLayer() == 0) continue;

                // if (prefabsList[i].noGroundLayer && (cell.GetGridLayer() == 0 || cell.isLeveledGroundCell)) continue;

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
                else if (cell.isLeveledEdge)
                {
                    Debug.LogError("No compatible tiles for Leveled Edge Cell: " + cell.id);
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

        private List<int> GetCompatibleTileRotations(HexagonCell currentCell, HexagonTile currentTile)
        {
            List<int> compatibleRotations = new List<int>();

            HexagonCell.NeighborSideCornerSockets[] neighborTileCornerSocketsBySide = currentCell.GetSideNeighborTileSockets(isWalledEdge);
            HexagonCell.NeighborLayerCornerSockets[] layeredNeighborTileCornerSockets = currentCell.GetLayeredNeighborTileSockets(TileContext.Default);

            string tileName = currentTile.gameObject.name;
            // if (currentCell.id == "0-4" && neighborTileCornerSocketsBySide.Length > 0)
            // {
            //     Debug.LogError("neighborTileCornerSocketsBySide: " + currentCell.id + ", currentTile: " + currentTile.gameObject.name);

            //     for (int i = 0; i < neighborTileCornerSocketsBySide.Length; i++)
            //     {
            //         Debug.LogError("side: " + (HexagonSide)i + ", bottomCorners: " + neighborTileCornerSocketsBySide[i].bottomCorners[0] + ", " + neighborTileCornerSocketsBySide[i].bottomCorners[1]);
            //         Debug.LogError("side: " + (HexagonSide)i + ", topCorners: " + neighborTileCornerSocketsBySide[i].topCorners[0] + ", " + neighborTileCornerSocketsBySide[i].topCorners[1]);
            //     }
            // }

            // Check every rotation
            for (int rotation = 0; rotation < 6; rotation++)
            {
                bool compatibile = true;

                // Check Layered Neighbors First

                if (currentCell.GetGridLayer() > 0 && !currentCell.isLeveledGroundCell)
                {
                    // For now just check bottom neighbor's top against current tile's bottom
                    int[] currentTileBottomSockets = currentTile.GetRotatedLayerCornerSockets(false, rotation);
                    for (int i = 0; i < layeredNeighborTileCornerSockets[0].corners.Length; i++)
                    {
                        if (!compatibilityMatrix[currentTileBottomSockets[i], layeredNeighborTileCornerSockets[0].corners[i]])
                        {
                            Debug.LogError(tileName + " Not compatibile with bottom layer. currentTileBottomSocket: " + currentTileBottomSockets[i] + ", corner: " + (HexagonCorner)i);
                            compatibile = false;
                            break;
                        }
                    }
                }

                // Check Side Neighbors
                if (compatibile)
                {

                    for (int side = 0; side < neighborTileCornerSocketsBySide.Length; side++)
                    {
                        HexagonCell.NeighborSideCornerSockets neighborSide = neighborTileCornerSocketsBySide[side];

                        (int[] currentTileSideBottomSockets, int[] currentTileSideTopSockets) = currentTile.GetRotatedCornerSocketsBySide((HexagonSide)side, rotation);

                        if (!compatibilityMatrix[currentTileSideBottomSockets[0], neighborSide.bottomCorners[1]])
                        {
                            if (currentCell.id == "0-4")
                            {
                                Debug.LogError(tileName + " Not compatibile - side: " + (HexagonSide)side + ", currentTileBottomSocketA: " + currentTileSideBottomSockets[0] + ", neighborSideB: " + neighborSide.bottomCorners[1]);
                            }
                            Debug.Log("Not compatibile - 0");
                            // Debug.Log("Not compatibile - side: " + side + ", currentTileSideBottomSockets[0]: " + currentTileSideBottomSockets[0] + ", neighborSide.bottomCorners[0]: " + neighborSide.bottomCorners[0]);

                            compatibile = false;
                            break;
                        }
                        if (!compatibilityMatrix[currentTileSideBottomSockets[1], neighborSide.bottomCorners[0]])
                        {
                            if (currentCell.id == "0-4")
                            {
                                Debug.LogError(tileName + " Not compatibile - side: " + (HexagonSide)side + ", currentTileBottomSocketB: " + currentTileSideBottomSockets[1] + ", neighborSideA: " + neighborSide.bottomCorners[0]);
                            }
                            Debug.Log("Not compatibile - 1");

                            compatibile = false;
                            break;
                        }

                        if (!compatibilityMatrix[currentTileSideTopSockets[0], neighborSide.topCorners[1]])
                        {
                            if (currentCell.id == "0-4")
                            {
                                Debug.LogError(tileName + " Not compatibile - side: " + (HexagonSide)side + ", currentTileBottomSocketA: " + currentTileSideBottomSockets[0] + ", neighborSideB: " + neighborSide.bottomCorners[1]);
                            }

                            Debug.Log("Not compatibile - 2");

                            compatibile = false;
                            break;
                        }
                        if (!compatibilityMatrix[currentTileSideTopSockets[1], neighborSide.topCorners[0]])
                        {
                            if (currentCell.id == "0-4")
                            {
                                Debug.LogError(tileName + " Not compatibile - side: " + (HexagonSide)side + ", currentTileBottomSocketB: " + currentTileSideBottomSockets[1] + ", neighborSideA: " + neighborSide.bottomCorners[0]);
                            }
                            Debug.Log("Not compatibile - 3");

                            compatibile = false;
                            break;
                        }

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

            // Check every rotation
            for (int rotation = 0; rotation < 6; rotation++)
            {
                bool compatibile = true;

                int[] currentTileBottomSockets = new int[12];
                currentTileBottomSockets[0] = currentTile.GetRotatedSideCornerSocketId(HexagonCorner.FrontA, rotation, false);
                currentTileBottomSockets[1] = currentTile.GetRotatedSideCornerSocketId(HexagonCorner.FrontB, rotation, false);
                currentTileBottomSockets[2] = currentTile.GetRotatedSideCornerSocketId(HexagonCorner.FrontRightA, rotation, false);
                currentTileBottomSockets[3] = currentTile.GetRotatedSideCornerSocketId(HexagonCorner.FrontRightB, rotation, false);
                currentTileBottomSockets[4] = currentTile.GetRotatedSideCornerSocketId(HexagonCorner.BackRightA, rotation, false);
                currentTileBottomSockets[5] = currentTile.GetRotatedSideCornerSocketId(HexagonCorner.BackRightB, rotation, false);
                currentTileBottomSockets[6] = currentTile.GetRotatedSideCornerSocketId(HexagonCorner.BackA, rotation, false);
                currentTileBottomSockets[7] = currentTile.GetRotatedSideCornerSocketId(HexagonCorner.BackB, rotation, false);
                currentTileBottomSockets[8] = currentTile.GetRotatedSideCornerSocketId(HexagonCorner.BackLeftA, rotation, false);
                currentTileBottomSockets[9] = currentTile.GetRotatedSideCornerSocketId(HexagonCorner.BackLeftB, rotation, false);
                currentTileBottomSockets[10] = currentTile.GetRotatedSideCornerSocketId(HexagonCorner.FrontLeftA, rotation, false);
                currentTileBottomSockets[11] = currentTile.GetRotatedSideCornerSocketId(HexagonCorner.FrontLeftB, rotation, false);

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
        public void InstantiateAllTiles()
        {
            Transform folder = new GameObject("Tiles").transform;
            folder.transform.SetParent(gameObject.transform);

            // Instantiate clusters first
            // for (int i = 0; i < activeCellClusters.Count; i++)
            // {
            //     GameObject prefab = activeCellClusters[i].mainTile;

            //     if (prefab == null) continue;

            //     // int rotation = activeCellClusters[i].GetTileRotation();

            //     Vector3 pos = activeCellClusters[i].GetFoundationCenter();
            //     pos.y += 0.2f;

            //     GameObject activeTile = Instantiate(prefab.gameObject, pos, Quaternion.identity);

            //     // GameObject instantiatedParent = Instantiate(markerPrefab, spawnPosition, Quaternion.identity);

            //     // GameObject activeTile = Instantiate(prefab.gameObject, instantiatedParent.transform.localPosition, Quaternion.identity);
            //     // activeTile.gameObject.transform.rotation = Quaternion.Euler(0f, rotationValues[rotation], 0f);
            //     // activeTile.transform.position = instantiatedParent.transform.position;
            //     // activeTile.transform.SetParent(instantiatedParent.transform);
            //     // activeTile.transform.position = instantiatedParent.transform.TransformPoint(instantiatedParent.transform.position);

            //     // activeTile.transform.position = instantiatedParent.transform.localPosition;
            //     // instantiatedParent.transform.position = Vector3.zero;

            //     // CenterGameObjectAtPosition(activeTile, instantiatedParent.transform.position);


            //     // Vector3 offset = activeTile.transform.position - instantiatedParent.transform.position;
            //     // activeTile.transform.position = activeTile.transform.position + offset;



            //     // instantiatedParent.transform.position = Vector3.zero;

            //     // activeTile.transform.position = 
            //     // Vector3 offset = activeTile.transform.position - activeCellClusters[i].center;

            //     // activeTile.transform.position = activeTile.transform.position + offset;
            //     // Debug.Log("offset.x: " + offset.x + ", offset.z: " + offset.z);

            //     activeTiles.Add(activeTile);

            //     activeTile.transform.SetParent(folder);
            // }

            for (int i = 0; i < edgeCells.Count; i++)
            {
                // Skip cluster assigned cells
                // if (cells[i].IsInCluster()) continue;

                HexagonTile prefab = (HexagonTile)edgeCells[i].GetTile();

                if (prefab == null) continue;


                int rotation = edgeCells[i].GetTileRotation();

                Vector3 position = edgeCells[i].transform.position;
                position.y += 0.2f;

                GameObject activeTile = Instantiate(prefab.gameObject, position, Quaternion.identity);
                activeTile.gameObject.transform.rotation = Quaternion.Euler(0f, rotationValues[rotation], 0f);
                activeTiles.Add(activeTile);

                activeTile.transform.SetParent(folder);

            }

            for (int i = 0; i < allCells.Count; i++)
            {
                // Skip cluster assigned cells
                // if (cells[i].IsInCluster()) continue;

                HexagonTile prefab = (HexagonTile)allCells[i].GetTile();

                if (prefab == null) continue;

                int rotation = allCells[i].GetTileRotation();

                Vector3 position = allCells[i].transform.position;
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
            edgeCells = HexagonCell.GetEdgeCells(allCells);
            totalEdgeCells = edgeCells.Count;

            entryCells = HexagonCell.GetRandomEntryCells(edgeCells, maxEntrances, true, 0);

            // TODO: find a better way to jsut provide this without doing this extra stuff
            totalLayers = edgeCells.OrderByDescending(c => c.GetGridLayer()).ToList()[0].GetGridLayer() + 1;

            // allCellClusters = HexagonCellCluster.GetHexagonCellClusters(allCells, transform.position, collapseOrder, isWalledEdge);
            // availableCellClusters = allCellClusters.Count;

            allCellsByLayer = HexagonCell.OrganizeCellsByLevel(allCells);

            bool useLeveledCells = (100 * leveledCellChance) >= (UnityEngine.Random.Range(0, 100));
            if (useLeveledCells)
            {
                allLevelCellsByLayer = HexagonCell.SetRandomLeveledCellsForAllLayers(allCellsByLayer, _radius * leveledCellRadiusMult, leveledCellClumpSets);
            }
            else
            {
                allLevelCellsByLayer = null;
            }

            // Temp
            if (subZone != null)
            {
                subZone.cellClusters = allCellClusters;
            }

            List<HexagonCell> _processedCells = new List<HexagonCell>();

            if (isWalledEdge == false) _processedCells.AddRange(edgeCells);

            _processedCells.AddRange(allCells.Except(edgeCells).OrderByDescending(c => c.GetGridLayer()));

            allCells = _processedCells;
        }

        public void SetCells(List<HexagonCell> _allCells)
        {
            allCells = _allCells;
        }

        private void UpdateCompatibilityMatrix()
        {
            compatibilityMatrix = socketDirectory.GetCompatibilityMatrix();

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

        // [SerializeField] private MeshFilter meshFilter;
        // [SerializeField] private bool generateGridMesh;
        // void CenterGameObjectAtPosition(GameObject go, Vector3 position)
        // {
        //     // Get the renderer bounds of the GameObject
        //     Renderer renderer = go.GetComponent<Renderer>();
        //     Bounds bounds = renderer.bounds;

        //     // Calculate the center point of the GameObject
        //     Vector3 centerPoint = bounds.center;

        //     // Set the position of the Transform to the desired position minus the calculated center point
        //     go.transform.position = position - centerPoint;
        // }


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

}