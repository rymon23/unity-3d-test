
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;

namespace WFCSystem
{
    public class WFC_HexDefault : MonoBehaviour, IWFCSystem
    {
        [Header("General Settings")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private HexagonSocketDirectory socketDirectory;
        [SerializeField] private TileDirectory tileDirectory;

        [Header("Collapse Settings")]
        [SerializeField] private WFCCollapseOrder_General generalCollapseOrder = 0;
        [SerializeField] private WFCCollapseOrder_CellGrid collapseOrder_grid = WFCCollapseOrder_CellGrid.Edges_First;
        [SerializeField] private WFCCollapseOrder_Cells collapseOrder_cells = 0;
        [SerializeField] private WFC_CellNeighborPropagation neighborPropagation = WFC_CellNeighborPropagation.Edges_Only_Include_Layers;

        [Header("Edge Rules")]
        [SerializeField] private bool useEdgeMicroClusters;
        [SerializeField] private bool isWalledEdge;
        [SerializeField] private bool restrictEntryTiles;
        [Range(0, 10)][SerializeField] private int minEntrances = 1;
        [Range(1, 10)][SerializeField] private int maxEntrances = 2;


        [SerializeField] private HexagonTileCore tilePrefabs_EdgeMicroClusterParent;

        [Header("Tile Settings")]
        [SerializeField] private bool allowInvertedTiles = true;
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

        [Header("Cell Clustering")]
        [SerializeField] private bool enableClusters;
        [Range(0.1f, 1f)][SerializeField] private float clusterChance = 0.5f;
        [Header("Cluster Host Cell")]
        [SerializeField] private List<HexagonTileCore> clusterParentPrefabs;
        [Range(2, 12)][SerializeField] private int cluster_minHosts = 2;
        [Range(2, 12)][SerializeField] private int cluster_maxHosts = 7;
        [SerializeField] private bool cluster_randomizeHostCount = true;

        [Header("Cluster Micro Grid")]
        [Range(3, 12)][SerializeField] private int cluster_CellLayerElevation = 4;
        [Range(1, 24)][SerializeField] private int cluster_CellLayers = 2;
        [Range(3, 24)][SerializeField] private int cluster_CellLayersMax = 5;
        [SerializeField] private bool cluster_randomizeCellLayers;

        [Header("System Settings")]
        [SerializeField] private bool logIncompatibilities = false;
        [SerializeField] private bool ignoreFailures = true;
        [Range(1, 9999)][SerializeField] private int maxAttempts = 9999;
        [SerializeField] private int remainingAttempts;

        [Header("Grid Settings")]
        [SerializeField] private int _radius;
        public void SetRadius(int value)
        {
            _radius = value;
        }

        [SerializeField] private List<GameObject> activeTiles;
        [SerializeField] private int matrixLength;
        private int numTiles; // Number of tile prefabs
        public bool[,] compatibilityMatrix = null; // Compatibility matrix for tiles
        public int[,] probabilityMatrix;
        [SerializeField] private GameObject markerPrefab;
        [SerializeField] private List<HexagonTileCore> tilePrefabs;
        [SerializeField] private List<HexagonTileCore> tilePrefabs_Edgable;
        [SerializeField] private List<HexagonTileCore> tilePrefabs_Entrances;
        [SerializeField] private List<HexagonTileCore> tilePrefabs_TopLayer;
        [SerializeField] private List<HexagonTileCore> tilePrefabs_LayerConnector;
        [SerializeField] private List<HexagonTileCore> tilePrefabs_Leveled;
        [SerializeField] private List<HexagonTileCore> tilePrefabs_Path;
        [SerializeField] private List<HexagonTileCore> tilePrefabs_ClusterSet;
        [SerializeField] private HexagonTileCore tilePrefabs_ClusterCenter;
        [SerializeField] private List<HexagonTileCluster> tilePrefabs_cluster;
        Dictionary<int, HexagonTileCore> tileLookupByid;
        Dictionary<int, HexagonTileCluster> tileClusterLookupByid;

        [Header("Cell Debug")]
        [SerializeField] private List<HexagonCell> allCellsList;
        [SerializeField] private List<HexagonCell> edgeCells_Grid;
        [SerializeField] private List<HexagonCell> edgeCells_Inner;
        [SerializeField] private List<HexagonCell> entryCells;
        [SerializeField] private List<HexagonCell> allPathingCells;
        [SerializeField] private List<HexagonCellCluster> activeCellClusters;
        [SerializeField] private List<HexagonCellCluster> allCellClusters;
        [SerializeField] private int availableCellClusters = 0;
        [SerializeField] private int totalEdgeCells = 0;
        public Dictionary<int, List<HexagonCell>> allCellsByLayer;
        public Dictionary<int, List<HexagonCell>> allLevelCellsByLayer;
        public Dictionary<int, List<HexagonCell>> allPathCells;
        public Dictionary<int, List<HexagonCell>> cellClustersByLayer;
        public List<HexagonCell> topLevelCells;

        void Start()
        {
            if (runOnStart) Invoke("ExecuteWFC", 0.2f);
        }

        public void ExecuteWFC()
        {
            EvaluateTiles();

            UpdateCompatibilityMatrix();
            compatibilityMatrix = socketDirectory.GetCompatibilityMatrix();

            EvaluateCells();

            if (useEdgeMicroClusters && tilePrefabs_EdgeMicroClusterParent != null)
            {
                CollapseEdgeMicroClusters();
            }
            else
            {
                CollapseEntryCells();
                Debug.Log("Entrance Cells Assigned");

                if (isWalledEdge)
                {
                    CollapseEdgeCells();
                    Debug.Log("Edge Cells Assigned");
                }
            }

            // CollapseLeveledCells();
            // Debug.Log("Level Cells Assigned");

            if (generatePaths)
            {
                CollapsePathCells();
                Debug.Log("Path Cells Assigned");
            }

            // CollapseMicroClusters();

            CollapseRemainingCellsByLayer();

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
            for (int i = 0; i < allCellsList.Count; i++)
            {
                if (allCellsList[i].IsAssigned() == false) return true;
            }
            return false;
        }

        private void CollapseEntryCells()
        {
            if (entryCells.Count > 0)
            {
                Debug.Log("Collapse Entry Cells");

                foreach (HexagonCell entryCell in entryCells)
                {
                    // bool assigned = WFCUtilities.SelectAndAssignNext(entryCell, tilePrefabs_Edgable, TileContext.Default, socketDirectory, isWalledEdge, logIncompatibilities, ignoreFailures, allowInvertedTiles);
                    WFCUtilities.CollapseCellAndPropagate(entryCell, tilePrefabs, tilePrefabs_Edgable, neighborPropagation, TileContext.Default, socketDirectory, isWalledEdge, logIncompatibilities, ignoreFailures, allowInvertedTiles);
                }
            }
        }

        // private void CollapseEntranceCells()
        // {
        //     // Handle Entrance Cells First
        //     List<HexagonCell> entranceCells = edgeCells.FindAll(c => c.isEntryCell);

        //     for (int i = 0; i < entranceCells.Count; i++)
        //     {
        //         SelectAndAssignNext(entranceCells[i], tilePrefabs_Edgable);
        //     }
        // }

        // private void CollapseEdgeCells()
        // {
        //     for (int i = 0; i < edgeCells.Count; i++)
        //     {
        //         HexagonCell currentCell = edgeCells[i];
        //         if (currentCell.IsAssigned()) continue;

        //         SelectAndAssignNext(currentCell, tilePrefabs_Edgable);
        //     }
        // }

        private void CollapseEdgeCells()
        {
            List<HexagonTileCore> tilePrefabs_Edgable_Formatted = restrictEntryTiles ? tilePrefabs_Edgable.FindAll(t => t.isEntrance == false) : tilePrefabs_Edgable;
            List<HexagonTileCore> tilePrefabs_Edgable_No_Entry_Top_Only = tilePrefabs_TopLayer.FindAll(t => t.isEdgeable);
            // List<HexagonTileCore> tilePrefabs_Edgable_GridEdge = tilePrefabs_Edgable.FindAll(t => t.GetGridExclusionRule() == HexagonTileCore.GridExclusionRule.GridEdgesOnly);

            if (tilePrefabs_Edgable_No_Entry_Top_Only.Count == 0) tilePrefabs_Edgable_No_Entry_Top_Only = tilePrefabs_Edgable_Formatted;

            bool includeLayers = (neighborPropagation == WFC_CellNeighborPropagation.Edges_Only_Include_Layers || neighborPropagation == WFC_CellNeighborPropagation.Edges_Inners_Include_Layers);
            int layersToCollapse = includeLayers ? totalLayers : 0;

            // Debug.Log("Collapse Edge Cells for " + (layersToCollapse + 1) + " layers");
            int currentLayer = 0;
            do
            {
                List<HexagonCell> layerEdgeCells = edgeCells_Grid.FindAll(e => e.GetGridLayer() == currentLayer).OrderByDescending(e => e._neighbors.Count).ToList();

                foreach (HexagonCell edgeCell in layerEdgeCells)
                {
                    if (edgeCell.IsAssigned() == false)
                    {
                        if (edgeCell.HasTopNeighbor() == false)
                        {
                            SelectAndAssignNext(edgeCell, tilePrefabs_Edgable_No_Entry_Top_Only);
                        }
                        else
                        {
                            SelectAndAssignNext(edgeCell, tilePrefabs_Edgable_Formatted);
                        }
                    }
                    CollapseCellAndPropagate(edgeCell);
                }

                currentLayer++;

            } while (currentLayer < layersToCollapse);
        }

        private void CollapseCellAndPropagate(HexagonCell currentCell)
        {
            bool assigned = currentCell.IsAssigned() ? true : SelectAndAssignNext(currentCell, currentCell.IsGridEdgeCell() ? tilePrefabs_Edgable : tilePrefabs);

            if (assigned && collapseOrder_cells == WFCCollapseOrder_Cells.Neighbor_Propogation)
            {
                int currentCellLayer = currentCell.GetGridLayer();

                bool includeLayerNwighbors = (neighborPropagation == WFC_CellNeighborPropagation.Edges_Only_Include_Layers || neighborPropagation == WFC_CellNeighborPropagation.Edges_Inners_Include_Layers);

                // Get Unassigned Neighbors
                List<HexagonCell> unassignedNeighbors = currentCell._neighbors.FindAll(n => n.IsAssigned() == false
                        && ((includeLayerNwighbors == false && n.GetGridLayer() == currentCellLayer)
                        || (includeLayerNwighbors && n.GetGridLayer() >= currentCellLayer)
                        ));

                if (unassignedNeighbors.Count > 0)
                {
                    bool includeInners = (currentCell.isEdgeCell == false || neighborPropagation == WFC_CellNeighborPropagation.Edges_Inners_Include_Layers || neighborPropagation == WFC_CellNeighborPropagation.Edges_Inners_No_Layers);

                    List<HexagonCell> edgeNeighbors = unassignedNeighbors.FindAll(n => n.isEdgeCell).OrderBy(n => n.GetEdgeCellType()).ToList();
                    if (edgeNeighbors.Count > 0)
                    {
                        foreach (HexagonCell neighbor in edgeNeighbors)
                        {
                            if (neighbor.IsAssigned()) continue;

                            bool wasAssigned = SelectAndAssignNext(neighbor, tilePrefabs_Edgable);
                        }
                    }

                    if (includeInners)
                    {
                        List<HexagonCell> innerNeighbors = unassignedNeighbors.FindAll(n => n.isEdgeCell == false).OrderByDescending(n => n._neighbors.Count).ToList();

                        foreach (HexagonCell neighbor in innerNeighbors)
                        {
                            if (neighbor.IsAssigned()) continue;
                            SelectAndAssignNext(neighbor, tilePrefabs);
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

                layerCells = HexagonCell.GetAvailableCellsForNextLayer(allCellsByLayer[currentLayer]);
                layerCells = layerCells.OrderByDescending(e => e._neighbors.Count).ToList();
                foreach (HexagonCell cell in layerCells)
                {
                    CollapseCellAndPropagate(cell);
                }
            }
        }

        private void CollapseEdgeMicroClusters()
        {

            if (tilePrefabs_EdgeMicroClusterParent == null)
            {
                Debug.LogError("No prefab for tilePrefabs_MicroClusterParent!");
                return;
            }
            List<HexagonCell> cellsToAssign = edgeCells_Grid.FindAll(c => c.isEntryCell == false && c.isPathCell == false && c.IsGroundCell());
            // List<HexagonCell> cellsToAssign = edgeCells_Grid.FindAll(c => c.isEntryCell == false && c.GetGridLayer() == 0);

            int cellLayers = cluster_CellLayers;
            if (cluster_randomizeCellLayers) cellLayers = UnityEngine.Random.Range(cellLayers, cluster_CellLayersMax);

            // (HexagonCellManager parentCellManager, List<HexagonCell> pathClusterCells) = WFCUtilities.SetupCellMicroCluster(childCells, tilePrefabs_EdgeMicroClusterParent, 2, 4, this.transform, true);
            (HexagonCellManager parentCellManager, List<HexagonCell> pathClusterCells) = WFCUtilities.SetupMicroCellClusterFromHosts(cellsToAssign, tilePrefabs_EdgeMicroClusterParent, cellLayers, cluster_CellLayerElevation, this.transform, true);

            parentCellManager.SetClusterParent();
            activeTiles.Add(parentCellManager.gameObject);

            parentCellManager.gameObject.name += "_EdgeClusterParent";

            HexGridArea gridArea = parentCellManager.gameObject.GetComponent<HexGridArea>();
            gridArea.InitialSetup();
            gridArea.Generate();
        }

        private bool CreateAndCollapseHostClusterAndMicoGrid(List<HexagonCell> availableCells_BaseLayer, List<HexagonTileCore> clusterParentPrefabs)
        {

            if (clusterParentPrefabs == null || clusterParentPrefabs.Count == 0)
            {
                Debug.LogError("No prefabs for clusterParentPrefabs!");
                return false;
            }

            bool sucess = false;

            // List<HexagonCell> cellsToAssign = WFCUtilities.SelectRandomCells(availableCells_BaseLayer, cluster_minHosts, cluster_maxHosts);
            List<HexagonCell> cellsToAssign = new List<HexagonCell>();

            HexagonCell parentCell = availableCells_BaseLayer[UnityEngine.Random.Range(0, availableCells_BaseLayer.Count)];

            int hostCount = cluster_minHosts;
            if (cluster_randomizeHostCount) hostCount = UnityEngine.Random.Range(cluster_minHosts, cluster_maxHosts);

            cellsToAssign = HexagonCell.GetChildrenForMicroClusterParent(parentCell, 0, hostCount - 1);
            if (cellsToAssign == null || cellsToAssign.Count == 0)
            {
                Debug.LogError("No childCells found");
                return false;
            }

            cellsToAssign.Add(parentCell);

            HexagonTileCore clusterParentPrefab = clusterParentPrefabs[UnityEngine.Random.Range(0, clusterParentPrefabs.Count)];

            int cellLayers = cluster_CellLayers;
            if (cluster_randomizeCellLayers) cellLayers = UnityEngine.Random.Range(cellLayers, cluster_CellLayersMax);

            (HexagonCellManager parentCellManager, List<HexagonCell> clusterCells) = WFCUtilities.SetupMicroCellClusterFromHosts(cellsToAssign, clusterParentPrefab, cellLayers, cluster_CellLayerElevation, this.transform, true);

            parentCellManager.SetClusterParent();

            activeTiles.Add(parentCellManager.gameObject);
            parentCellManager.gameObject.name += "_ClusterParent";

            HexGridArea gridArea = parentCellManager.gameObject.GetComponent<HexGridArea>();
            gridArea.InitialSetup();
            gridArea.Generate();

            return sucess;
        }



        private void CollapseLeveledCells()
        {
            if (allLevelCellsByLayer == null) return;

            List<HexagonTileCore> tilePrefabs_LeveledRamp = tilePrefabs_Leveled.FindAll(c => c.isLeveledRamp);
            List<HexagonTileCore> tilePrefabs_LeveledNoRamp = tilePrefabs_Leveled.FindAll(c => c.isLeveledRamp == false);

            foreach (var kvp in allLevelCellsByLayer)
            {
                int layer = kvp.Key;
                List<HexagonCell> leveledCells = kvp.Value;

                topLevelCells = leveledCells;

                List<HexagonCell> leveledEdgeCells = leveledCells.FindAll(c => c.isLeveledEdge);
                foreach (HexagonCell cell in leveledEdgeCells)
                {

                    if (cell.IsAssigned()) continue;

                    List<HexagonTileCore> prefabs = (cell.isLeveledRampCell && !useTerrainMeshPath) ? tilePrefabs_LeveledRamp : tilePrefabs_LeveledNoRamp;

                    SelectAndAssignNext(cell, prefabs);

                    if (cell.isPathCell) cell.highlight = true;
                }

                foreach (HexagonCell cell in leveledCells)
                {
                    if (cell.IsAssigned()) continue;

                    SelectAndAssignNext(cell, tilePrefabs_Leveled);
                }
            }

            // foreach (var kvp in allCellsByLayer)
            // {
            //     int layer = kvp.Key;
            //     List<HexagonCell> layerCells = kvp.Value;

            //     if (layer == 0) continue;

            //     foreach (HexagonCell cell in layerCells)
            //     {
            //         if (cell.IsAssigned()) continue;

            //         if (!cell.isLeveledGroundCell) continue;


            //         SelectAndAssignNext(cell, tilePrefabs);
            //     }
            // }
        }

        private void CollapsePathCells()
        {
            Debug.Log("topLevelCells: " + topLevelCells.Count);
            allPathingCells = new List<HexagonCell>();

            // allPathCells = HexagonCell.GenerateRandomCellPaths(entryCells,
            //         allCellsByLayer,
            //         transform.position);

            allPathCells = new Dictionary<int, List<HexagonCell>>();
            allPathCells.Add(0,
                HexagonCell.GenerateRandomCellPath(entryCells,
                    allCellsByLayer,
                    transform.position)

            );

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

                                    SelectAndAssignNext(item, tilePrefabs_Leveled.FindAll(t => t.isLeveledRamp));
                                }
                            }
                            else
                            {
                                item.SetPathCell(false);
                                continue;
                            }
                        }

                        item.SetPathCell(true);

                        allPathingCells.Add(item);

                        if (useTerrainMeshPath)
                        {
                            updatedPathCell.SetIgnored(true);
                        }
                        else
                        {
                            if (item.IsAssigned()) continue;

                            SelectAndAssignNext(item, tilePrefabs_Path);
                        }
                    }
                }
            }
        }


        [SerializeField] private HexagonTileCore tilePrefabs_MicroClusterParent;
        private void CollapseMicroClusters()
        {
            List<HexagonCell> availableCells_L1 = allCellsByLayer[0].FindAll(c => !c.IsAssigned() && !c.isLeveledCell);
            if (availableCells_L1.Count > 1)
            {
                HexagonCell parentCell = availableCells_L1[UnityEngine.Random.Range(0, availableCells_L1.Count)];
                parentCell.SetTile(tilePrefabs_MicroClusterParent, 0);

                GameObject activeTile = Instantiate(parentCell.GetCurrentTile().gameObject, parentCell.transform.position, Quaternion.identity);
                activeTiles.Add(activeTile);

                availableCells_L1.Remove(parentCell);

                HexagonCellManager parentCellManager = activeTile.GetComponent<HexagonCellManager>();
                parentCellManager.SetClusterParent();
                bool success = parentCellManager.InitializeMicroClusterGrid(parentCell, availableCells_L1, 4);

                HexagonTileCore tileCore = activeTile.GetComponent<HexagonTileCore>();
                tileCore.SetEditorTools(false);

                activeTiles.Add(activeTile);
                activeTile.transform.SetParent(gameObject.transform);


                if (!success)
                {
                    Debug.LogError("CollapseMicroClusters: failed");
                }
                else
                {
                    parentCell.gameObject.name += "_ClusterParent";

                    HexGridArea gridArea = activeTile.GetComponent<HexGridArea>();
                    gridArea.InitialSetup();
                    gridArea.Generate();
                }
            }
        }

        // private void CollapseRemainingCellsByLayer()
        // {
        //     for (int currentLayer = 0; currentLayer < totalLayers; currentLayer++)
        //     {
        //         List<HexagonCell> layerCells;
        //         // List<HexagonCellCluster> layerClusters = new List<HexagonCellCluster>();
        //         if (currentLayer == 0)
        //         {
        //             layerCells = allCellsByLayer[0].FindAll(c => !c.IsAssigned() && !c.isLeveledCell);
        //             // layerClusters = HexagonCellCluster.GetHexagonCellClusters(layerCells, transform.position, collapseOrder, isWalledEdge);

        //             bool useCluster = enableClusters && layerCells.Count > 1 && (100 * clusterChance) >= (UnityEngine.Random.Range(0, 100));
        //             if (useCluster)
        //             {
        //                 List<HexagonCell> availableCells = layerCells.FindAll(c => c.IsAssigned() == false && c.GetUnassignedNeighborCount(false) > 0);
        //                 do
        //                 {
        //                     CreateAndCollapseHostClusterAndMicoGrid(availableCells, clusterParentPrefabs);
        //                     availableCells = availableCells.FindAll(c => c.IsAssigned() == false && c.GetUnassignedNeighborCount(false) > 0);

        //                     useCluster = availableCells.Count <= 1 ? false : (100 * clusterChance) >= (UnityEngine.Random.Range(0, 100));

        //                 } while (useCluster);
        //             }
        //         }
        //         else
        //         {
        //             layerCells = HexagonCell.GetAvailableCellsForNextLayer(allCellsByLayer[currentLayer]);
        //         }

        //         foreach (HexagonCell cell in layerCells)
        //         {
        //             if (cell.IsAssigned()) continue;

        //             cell.highlight = true;

        //             bool assigned = SelectAndAssignNext(cell, tilePrefabs);

        //             if (assigned == false) cell.SetIgnored(true);

        //             if (assigned == false && !ignoreFailures)
        //             {
        //                 Debug.LogError("No tile found for cell: " + cell.id);

        //                 for (int i = 0; i < cell._neighbors.Count; i++)
        //                 {
        //                     Debug.LogError("neighbor: " + cell._neighbors[i].id + ",");
        //                 }
        //             }
        //         }
        //     }
        // }



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
                HexagonTileCore startTile = tilePrefabs_ClusterCenter;
                AssignTileToCell(startCell, startTile, 0);
                tileClusterSet.Add(startTile.gameObject);

                Debug.Log("startTile: " + startTile.name + ", startCell: " + startCell.id);
            }

            foreach (HexagonCell cell in cluster.cells)
            {
                if (cell.IsAssigned()) continue;

                // Debug.Log("SelectClusterTileSet cell: " + cell.id);

                // (HexagonTileCore nextTile, List<int> rotations) = SelectNextTile(cell, tilePrefabs_ClusterSet, true);

                // if (nextTile == null)
                // {
                //     Debug.LogError("No tile found for cluster cell: " + cell.id + ", Cluster: " + cluster.id + ", cells: " + cluster.cells.Count + ", tilesFilled: " + tileClusterSet.Count);
                //     for (int i = 0; i < tileClusterSet.Count; i++)
                //     {
                //         Debug.LogError("tile: " + i + ", " + tileClusterSet[i].gameObject.name);
                //     }
                // }
                //AssignTileToCell(cell, nextTile, rotations);
                HexagonTileCore nextTile = tilePrefabs_ClusterCenter;
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

        private void AssignTileToCell(HexagonCell cell, HexagonTileCore tile, int rotation = 0)
        {
            cell.SetTile(tile, rotation);
        }

        private bool SelectAndAssignNext(HexagonCell cell, List<HexagonTileCore> prefabsList)
        {
            (HexagonTileCore nextTile, List<int[]> rotations) = WFCUtilities.SelectNextTile(cell, prefabsList, allowInvertedTiles, isWalledEdge, TileContext.Default, socketDirectory, logIncompatibilities);
            return WFCUtilities.AssignTileToCell(cell, nextTile, rotations, ignoreFailures);
        }


        public void InstantiateAllTiles()
        {
            Transform folder = new GameObject("Tiles").transform;
            folder.transform.SetParent(gameObject.transform);

            foreach (HexagonCell cell in allCellsList)
            {
                // Skip cluster assigned cells
                // if (cells[i].IsInCluster()) continue;

                HexagonTileCore prefab = cell.GetCurrentTile();
                if (prefab == null) continue;

                WFCUtilities.InstantiateTile(prefab, cell, folder, activeTiles);
            }
        }

        List<HexagonCell> allMicroCells;
        public List<HexagonCell> GetAllMicroCells()
        {
            return allMicroCells;
        }

        // Instantiate the tiles in the appropriate positions to create the final pattern
        // public void InstantiateAllTiles()
        // {

        //     Transform folder = new GameObject("Tiles").transform;
        //     folder.transform.SetParent(gameObject.transform);

        //     for (int i = 0; i < edgeCells.Count; i++)
        //     {
        //         // Skip cluster assigned cells
        //         // if (cells[i].IsInCluster()) continue;

        //         HexagonTileCore prefab = edgeCells[i].GetCurrentTile();
        //         if (prefab == null) continue;

        //         // InstantiateTile(prefab, edgeCells[i], folder);
        //         WFCUtilities.InstantiateTile(prefab, edgeCells[i], folder, activeTiles);
        //     }

        //     for (int i = 0; i < allCells.Count; i++)
        //     {
        //         // Skip cluster assigned cells
        //         // if (cells[i].IsInCluster()) continue;

        //         HexagonTileCore prefab = allCells[i].GetCurrentTile();
        //         if (prefab == null) continue;

        //         // WFCUtilities.InstantiateTile(prefab, allCells[i], folder);
        //         WFCUtilities.InstantiateTile(prefab, allCells[i], folder, activeTiles);
        //     }
        // }

        private void EvaluateTiles()
        {
            // Get All Tile Prefabs
            tileLookupByid = tileDirectory.CreateHexTileDictionary();
            List<HexagonTileCore> _tilePrefabs = tileLookupByid.Select(x => x.Value).ToList();

            // Check For Nulls
            foreach (HexagonTileCore prefab in _tilePrefabs)
            {
                int id = prefab.GetId();
            }

            // Get All Cluster Prefabs
            tileClusterLookupByid = tileDirectory.CreateTileClusterDictionary();
            tilePrefabs_cluster = tileClusterLookupByid.Select(x => x.Value).ToList();

            // Get All Cluster Sets
            tilePrefabs_ClusterSet = HexagonTileCore.ExtractClusterSetTiles(_tilePrefabs);

            tilePrefabs = new List<HexagonTileCore>();
            tilePrefabs.AddRange(_tilePrefabs.Except(tilePrefabs_ClusterSet));

            tilePrefabs_ClusterSet = tilePrefabs_ClusterSet.FindAll(x => x.isClusterCenterTile == false);

            // Extract Edge Tiles
            tilePrefabs_Edgable = tilePrefabs.FindAll(x => x.isEdgeable).ToList();

            // Extract Entrance Tiles
            tilePrefabs_Entrances = tilePrefabs.FindAll(x => x.isEdgeable && x.isEntrance).ToList();

            // Extract Top Layer tiles
            tilePrefabs_TopLayer = tilePrefabs.FindAll(x => x.GetExcludeLayerState() == HexagonTileCore.ExcludeLayerState.TopLayerOnly || x.IsRoofable()).ToList();

            // Extract Leveled Tiles
            tilePrefabs_Leveled = tilePrefabs.FindAll(x => x.isLeveledTile).ToList();

            // Extract Layer Connector Tiles
            tilePrefabs_LayerConnector = tilePrefabs.FindAll(x => x.isLayerConnector).ToList();

            // Extract Path Tiles
            tilePrefabs_Path = tilePrefabs.FindAll(x => x.isPath).ToList();

            // Extract Inner Tiles
            List<HexagonTileCore> innerTilePrefabs = new List<HexagonTileCore>();
            innerTilePrefabs.AddRange(tilePrefabs.Except(tilePrefabs.FindAll(x => x.IsExteriorWall() || x.isEntrance || x.isLayerConnector || x.isPath)));

            tilePrefabs = innerTilePrefabs;

            // Shuffle the prefabs
            WFCUtilities.ShuffleTiles(tilePrefabs);
            WFCUtilities.ShuffleTiles(tilePrefabs_cluster);
        }

        private void EvaluateCells()
        {
            entryCells = new List<HexagonCell>();

            edgeCells_Grid = allCellsList.FindAll(c => c.IsGridEdgeCell());
            // edgeCells_Inner = HexagonCell.GetEdgeCells(allCellsList.Except(edgeCells_Grid).ToList());
            edgeCells_Inner = HexagonCell.GetInnerEdges(allCellsList.Except(edgeCells_Grid).ToList(), EdgeCellType.Inner);
            totalEdgeCells = edgeCells_Grid.Count + edgeCells_Inner.Count;

            if (minEntrances > 0)
            {
                // entryCells = HexagonCell.GetRandomEntryCells(edgeCells, maxEntrances, true, 0);
                entryCells = HexagonCell.PickRandomEntryCellsFromEdgeCells(edgeCells_Grid, maxEntrances, true);
            }

            // TODO: find a better way to jsut provide this without doing this extra stuff
            totalLayers = allCellsByLayer.Keys.Count;// .OrderByDescending(c => c.GetGridLayer()).ToList()[0].GetGridLayer() + 1;

            // List<HexagonCell> _processedCells = new List<HexagonCell>();

            // if (isWalledEdge == false) _processedCells.AddRange(edgeCells);

            // _processedCells.AddRange(allCells.Except(edgeCells).OrderByDescending(c => c.GetGridLayer()));
            // allCells = _processedCells;

            // allCellsByLayer = HexagonCell.OrganizeCellsByLevel(allCellsList);

            // LEVEL CELLS 
            bool useLeveledCells = (100 * leveledCellChance) >= (UnityEngine.Random.Range(0, 100));
            if (useLeveledCells)
            {
                allLevelCellsByLayer = HexagonCell.SetRandomLeveledCellsForAllLayers(allCellsByLayer, _radius * leveledCellRadiusMult, leveledCellClumpSets);
            }
            else
            {
                allLevelCellsByLayer = null;
            }

        }

        public void SetCells(Dictionary<int, List<HexagonCell>> _allCellsByLayer, List<HexagonCell> _allCells)
        {
            allCellsByLayer = _allCellsByLayer;
            allCellsList = _allCells;
        }

        // public void SetCells( List<HexagonCell> _allCells)
        // {
        //     allCellsList = _allCells;
        // }

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
            UpdateCompatibilityMatrix();
        }
    }
}