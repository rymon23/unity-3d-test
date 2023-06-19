using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ProceduralBase;

namespace WFCSystem
{
    public class WFC_Core : IWFCSystem
    {
        public WFC_Core(
            HexagonSocketDirectory _socketDirectory,
            TileDirectory _tileDirectory,
            Dictionary<int, List<HexagonCellPrototype>> _allCellsByLayer,
            LocationMarkerPrefabOption _locationSettings,
            Transform _tileFolder = null
         )
        {
            socketDirectory = _socketDirectory;
            tileDirectory = _tileDirectory;
            locationSettings = _locationSettings;

            tileFolder = _tileFolder;

            AssignCells(_allCellsByLayer);
        }

        public WFC_Core(
            HexagonSocketDirectory _socketDirectory,
            TileDirectory _tileDirectory,
            List<HexagonCellPrototype> _allCells,
            Transform _tileFolder = null
         )
        {
            socketDirectory = _socketDirectory;
            tileDirectory = _tileDirectory;
            tileFolder = _tileFolder;

            AssignCells(_allCells);
        }

        private HexagonSocketDirectory socketDirectory;
        private TileDirectory tileDirectory;
        private TileContext tileContext;
        private LocationMarkerPrefabOption locationSettings;
        private Transform tileFolder = null;
        private Transform transform;


        [SerializeField] private HexagonTileCore tilePrefabs_ClusterWFC;
        [SerializeField] private HexagonTileCore tilePrefabs_MicroClusterParent;


        [Header("Collapse Settings")]
        [SerializeField] private WFCCollapseOrder_General generalCollapseOrder = 0;
        [SerializeField] private WFCCollapseOrder_CellGrid collapseOrder_grid = WFCCollapseOrder_CellGrid.Edges_First;
        [SerializeField] private WFCCollapseOrder_Cells collapseOrder_cells = 0;
        [SerializeField] private WFC_CellNeighborPropagation neighborPropagation = WFC_CellNeighborPropagation.Edges_Only_Include_Layers;

        [Header("Edge Rules")]
        [SerializeField] private bool useEdgeMicroClusters;
        [SerializeField] private bool isWalledEdge;
        [SerializeField] private bool restrictEntryTiles;
        [SerializeField] private HexagonTileCore tilePrefabs_EdgeMicroClusterParent;

        [Header("Tile Settings")]
        [SerializeField] private bool allowInvertedTiles = true;

        [Header("Pathing")]
        [SerializeField] private bool generatePaths = true;
        [SerializeField] private bool useTerrainMeshPath = true;

        [Header("Cluster Micro Grid")]
        [Range(3, 12)][SerializeField] private int cluster_CellLayerElevation = 4;
        [Range(1, 24)][SerializeField] private int cluster_CellLayers = 2;
        [Range(3, 24)][SerializeField] private int cluster_CellLayersMax = 5;
        [SerializeField] private bool cluster_randomizeCellLayers;

        [Header("System Settings")]
        [SerializeField] private bool logIncompatibilities = true;
        [SerializeField] private bool ignoreFailures = true;
        [Range(1, 9999)][SerializeField] private int maxAttempts = 9999;
        [SerializeField] private int remainingAttempts;


        [SerializeField] private List<GameObject> activeTiles;
        [SerializeField] private int matrixLength;
        public bool[,] compatibilityMatrix = null; // Compatibility matrix for tiles

        [SerializeField] private List<HexagonTileCore> tilePrefabs;
        [SerializeField] private List<HexagonTileCore> tilePrefabs_Edgable;
        [SerializeField] private List<HexagonTileCore> tilePrefabs_Entrances;
        [SerializeField] private List<HexagonTileCore> tilePrefabs_TopLayer;
        [SerializeField] private List<HexagonTileCore> tilePrefabs_Leveled;
        [SerializeField] private List<HexagonTileCore> tilePrefabs_Path;
        [SerializeField] private List<HexagonTileCore> tilePrefabs_ClusterSet;
        [SerializeField] private List<HexagonTileCluster> tilePrefabs_cluster;
        Dictionary<int, HexagonTileCore> tileLookupByid;
        Dictionary<int, HexagonTileCluster> tileClusterLookupByid;

        [Header("Cell Debug")]
        [SerializeField] private List<HexagonCellPrototype> edgeCells_Grid;
        [SerializeField] private List<HexagonCellPrototype> entryCells;

        public Dictionary<int, List<HexagonCellPrototype>> allLevelCellsByLayer;
        public List<HexagonCellPrototype> topLevelCells;
        [Header(" ")]
        private List<HexagonCellPrototype> allCells;
        public Dictionary<int, List<HexagonCellPrototype>> allCellsByLayer { get; private set; }
        [SerializeField] private List<HexagonCellPrototype> allAssignedCellsInOrder;

        private bool hasMicroTiles = false;

        DateTime _buildStartTime;

        private void StartTimeLog()
        {
            _buildStartTime = DateTime.Now;
        }
        private void TimeLog(string str)
        {
            UtilityHelpers.LogTime(_buildStartTime, str);
        }

        public void ExecuteWFC()
        {
            UpdateCompatibilityMatrix();
            compatibilityMatrix = socketDirectory.GetCompatibilityMatrix();

            StartTimeLog();

            EvaluateCells();

            TimeLog("EvaluateCells - Time");
            StartTimeLog();

            EvaluateTiles();

            TimeLog("EvaluateTiles - Time");
            StartTimeLog();

            if (logIncompatibilities) Debug.Log("Executing WFC - Location Type: " + locationSettings.locationType + ", Tile Context: " + tileContext);

            isWalledEdge = true;


            if (useEdgeMicroClusters && tilePrefabs_EdgeMicroClusterParent != null)
            {
                CollapseEdgeMicroClusters();
            }
            else
            {
                CollapseEntryCells();
                // if (isWalledEdge) CollapseEdgeCells();
            }

            CollapseEdgeCells();
            TimeLog("CollapseEdgeCells - Time");


            remainingAttempts = 1;//maxAttempts;

            // do
            // {
            CollapseRemainingCellsByLayer();

            remainingAttempts--;
            // } while (remainingAttempts > 0 && WFCUtilities_V2.HasUnassignedCells(allCells));

            TimeLog("CollapseRemainingCellsByLayer - Time");
            StartTimeLog();

            InstantiateAllTiles();

            TimeLog("InstantiateAllTiles - Time");

            if (logIncompatibilities) Debug.Log("Execution of WFC Complete");
        }


        private void CollapseEntryCells()
        {
            if (entryCells == null || entryCells.Count == 0)
            {
                Debug.LogError("NO Entrance Cells");
                return;
            }

            if (logIncompatibilities) Debug.Log("Collapsing " + entryCells.Count + " Entry Cells...");

            foreach (HexagonCellPrototype entryCell in entryCells)
            {
                WFCUtilities_V2.CollapseCellAndPropagate(
                    entryCell,
                    tilePrefabs,
                    tilePrefabs_Edgable,
                    neighborPropagation,
                    tileContext,
                    socketDirectory,
                    isWalledEdge,
                    logIncompatibilities,
                    ignoreFailures,
                    allowInvertedTiles
                );
            }

        }

        private void CollapseEdgeCells()
        {
            // WFCUtilities_V2.CollapseEdgeCells(
            //     allCellsByLayer,
            //     edgeCells_Grid,
            //     tilePrefabs,
            //     tilePrefabs_Edgable,
            //     tilePrefabs_TopLayer,
            //     allAssignedCellsInOrder,
            //     neighborPropagation,
            //     tileContext,
            //     socketDirectory,
            //     restrictEntryTiles,
            //     isWalledEdge,
            //     logIncompatibilities,
            //     ignoreFailures,
            //     allowInvertedTiles
            // );

            Debug.Log("Collapsing Edge Cells...");

            List<HexagonTileCore> tilePrefabs_Edgable_Formatted = restrictEntryTiles ? tilePrefabs_Edgable.FindAll(t => t.isEntrance == false) : tilePrefabs_Edgable;
            List<HexagonTileCore> tilePrefabs_Edgable_No_Entry_Top_Only = tilePrefabs_TopLayer.FindAll(t => t.isEdgeable);
            // List<HexagonTileCore> tilePrefabs_Edgable_GridEdge = tilePrefabs_Edgable.FindAll(t => t.GetGridExclusionRule() == HexagonTileCore.GridExclusionRule.GridEdgesOnly);

            if (tilePrefabs_Edgable_No_Entry_Top_Only.Count == 0) tilePrefabs_Edgable_No_Entry_Top_Only = tilePrefabs_Edgable_Formatted;

            bool includeLayers = (neighborPropagation == WFC_CellNeighborPropagation.Edges_Only_Include_Layers || neighborPropagation == WFC_CellNeighborPropagation.Edges_Inners_Include_Layers);

            foreach (var kvp in allCellsByLayer)
            {
                int currentLayer = kvp.Key;
                List<HexagonCellPrototype> layerEdgeCells = edgeCells_Grid.FindAll(e => e.GetLayer() == currentLayer).OrderByDescending(e => e.neighbors.Count).ToList();

                foreach (HexagonCellPrototype edgeCell in layerEdgeCells)
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
            }
        }

        private void CollapseCellAndPropagate(HexagonCellPrototype currentCell)
        {
            bool assigned = currentCell.IsWFC_Assigned() ? true : SelectAndAssignNext(currentCell, currentCell.IsGridEdge() ? tilePrefabs_Edgable : tilePrefabs);

            if (assigned && collapseOrder_cells == WFCCollapseOrder_Cells.Neighbor_Propogation)
            {
                int currentCellLayer = currentCell.GetLayer();

                bool includeLayerNwighbors = (neighborPropagation == WFC_CellNeighborPropagation.Edges_Only_Include_Layers || neighborPropagation == WFC_CellNeighborPropagation.Edges_Inners_Include_Layers);

                // Get Unassigned Neighbors
                List<HexagonCellPrototype> unassignedNeighbors = currentCell.neighbors.FindAll(n => n.IsWFC_Assigned() == false
                        && ((includeLayerNwighbors == false && n.GetLayer() == currentCellLayer)
                        || (includeLayerNwighbors && n.GetLayer() >= currentCellLayer)
                        ));

                if (unassignedNeighbors.Count > 0)
                {
                    bool includeInners = (currentCell.isEdgeCell == false || neighborPropagation == WFC_CellNeighborPropagation.Edges_Inners_Include_Layers || neighborPropagation == WFC_CellNeighborPropagation.Edges_Inners_No_Layers);

                    List<HexagonCellPrototype> edgeNeighbors = unassignedNeighbors.FindAll(n => n.isEdgeCell).OrderBy(n => n.GetEdgeCellType()).ToList();
                    if (edgeNeighbors.Count > 0)
                    {
                        foreach (HexagonCellPrototype neighbor in edgeNeighbors)
                        {
                            if (neighbor.IsWFC_Assigned()) continue;

                            bool wasAssigned = SelectAndAssignNext(neighbor, tilePrefabs_Edgable);
                        }
                    }

                    if (includeInners)
                    {
                        List<HexagonCellPrototype> innerNeighbors = unassignedNeighbors.FindAll(n => n.isEdgeCell == false).OrderByDescending(n => n.neighbors.Count).ToList();

                        foreach (HexagonCellPrototype neighbor in innerNeighbors)
                        {
                            if (neighbor.IsWFC_Assigned()) continue;
                            SelectAndAssignNext(neighbor, tilePrefabs);
                        }
                    }

                }
            }
        }

        private void CollapseRemainingCellsByLayer()
        {
            Debug.Log("Collapsing Remaining Cells...");

            foreach (var kvp in allCellsByLayer)
            {
                List<HexagonCellPrototype> layerCells;
                List<HexagonCellCluster> layerClusters = new List<HexagonCellCluster>();

                layerCells = kvp.Value; // WFCUtilities_V2.GetAvailableCellsForNextLayer(kvp.Value);
                // layerCells = WFCUtilities_V2.GetAvailableCellsForNextLayer(kvp.Value);
                layerCells = layerCells.OrderByDescending(e => e.neighbors.Count).ToList();

                if (logIncompatibilities) Debug.LogError("currentLayer: " + kvp.Key + ", available layerCells: " + layerCells.Count + ", total: " + kvp.Value.Count);

                foreach (HexagonCellPrototype cell in layerCells)
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
            List<HexagonCellPrototype> cellsToAssign = edgeCells_Grid.FindAll(c => c.isEntryCell == false && c.isPathCell == false && c.IsGround());
            // List<HexagonCellPrototype> cellsToAssign = edgeCells_Grid.FindAll(c => c.isEntryCell == false && c.GetLayer() == 0);

            int cellLayers = cluster_CellLayers;
            if (cluster_randomizeCellLayers) cellLayers = UnityEngine.Random.Range(cellLayers, cluster_CellLayersMax);

            // (HexagonCellManager parentCellManager, List<HexagonCellPrototype> pathClusterCells) = WFCUtilities_V2.SetupCellMicroCluster(childCells, tilePrefabs_EdgeMicroClusterParent, 2, 4, this.transform, true);
            (HexagonCellManager parentCellManager, List<HexagonCellPrototype> pathClusterCells) = WFCUtilities_V2.Instantiate_MicroCellClusterFromHosts(cellsToAssign, tilePrefabs_EdgeMicroClusterParent, cellLayers, cluster_CellLayerElevation, this.transform, true);

            parentCellManager.SetClusterParent();
            activeTiles.Add(parentCellManager.gameObject);

            parentCellManager.gameObject.name += "_EdgeClusterParent";

            HexGridArea gridArea = parentCellManager.gameObject.GetComponent<HexGridArea>();
            gridArea.InitialSetup();
            gridArea.Generate();
        }


        private bool SelectAndAssignNext(HexagonCellPrototype cell, List<HexagonTileCore> prefabsList)
        {
            (HexagonTileCore nextTile, List<int[]> rotations) = WFCUtilities_V2.SelectNextTile(cell, prefabsList, allowInvertedTiles, isWalledEdge, tileContext, socketDirectory, logIncompatibilities);
            return WFCUtilities_V2.AssignTileToCell(cell, nextTile, rotations, ignoreFailures);
        }

        public void InstantiateAllTiles()
        {
            Transform folder = new GameObject("Tiles").transform;

            if (tileFolder != null)
            {
                folder.transform.SetParent(tileFolder);
            }
            else if (transform != null)
            {
                folder.transform.SetParent(transform);
            }

            foreach (IHexCell cell in allCells)
            {
                HexagonTileCore prefab = cell.GetCurrentTile();
                if (prefab == null) continue;

                WFCUtilities_V2.InstantiateTile(prefab, cell, folder, activeTiles);
            }
        }

        private void EvaluateTiles()
        {
            // Get All Tile Prefabs
            tileLookupByid = tileDirectory.CreateHexTileDictionary();

            hasMicroTiles = tileDirectory.HasMicroTiles();

            List<HexagonTileCore> _tilePrefabs = tileLookupByid.Select(x => x.Value).ToList();

            if (_tilePrefabs.Count == 0)
            {
                Debug.LogError("Empty tilePrefabs");
            }

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

            // Extract Path Tiles
            tilePrefabs_Path = tilePrefabs.FindAll(x => x.isPath).ToList();

            // Extract Inner Tiles
            List<HexagonTileCore> innerTilePrefabs = new List<HexagonTileCore>();
            innerTilePrefabs.AddRange(tilePrefabs.Except(tilePrefabs.FindAll(x => x.IsExteriorWall() || x.isEntrance || x.isLayerConnector || x.isPath)));

            tilePrefabs = innerTilePrefabs;

            // Shuffle the prefabs
            WFCUtilities_V2.ShuffleTiles(tilePrefabs);
            WFCUtilities_V2.ShuffleTiles(tilePrefabs_cluster);
        }

        private void EvaluateCells()
        {
            if (allCells == null ||
                allCells.Count == 0 ||
                allCellsByLayer == null ||
                allCellsByLayer.Count == 0
            )
            {
                Debug.LogError("Empty Cell list for allCells or allCellsByLayer");
            }

            tileContext = allCells[0].GetSize() < 12 ? TileContext.Micro : TileContext.Default;

            edgeCells_Grid = allCells.FindAll(c => c.IsGridEdge());

            entryCells = allCells.FindAll(c => c.IsEntry());
        }

        public void AssignCells(Dictionary<int, List<HexagonCellPrototype>> _allCellsByLayer)
        {
            allCellsByLayer = _allCellsByLayer;
            allCells = HexCellUtil.ExtractCellsByLayer(_allCellsByLayer);
        }
        public void AssignCells(List<HexagonCellPrototype> _allCells)
        {
            allCells = _allCells;
            allCellsByLayer = HexCellUtil.OrganizeByLayer(_allCells);
        }

        public void AssignCells(Dictionary<int, List<HexagonCellPrototype>> _allCellsByLayer, List<HexagonCellPrototype> _allCells)
        {
            allCells = _allCells;
            allCellsByLayer = _allCellsByLayer;
        }

        public void AssignCells(Dictionary<int, List<HexagonCell>> _allCellsByLayer)
        {
        }
        public void AssignCells(List<HexagonCell> _allCells)
        {
        }
        public void AssignCells(Dictionary<int, List<HexagonCell>> _allCellsByLayer, List<HexagonCell> _allCells)
        {
        }
        public void AssignCells(
            Dictionary<int, List<HexagonCell>> _allCellsByLayer,
            List<HexagonCell> _allCells,
            Dictionary<HexagonCellCluster, Dictionary<int, List<HexagonCell>>> _allCellsByLayer_X4_ByCluster
        )
        {
        }
        public void SetRadius(int value) { }

        private void UpdateCompatibilityMatrix()
        {
            compatibilityMatrix = socketDirectory.GetCompatibilityMatrix();

            if (compatibilityMatrix.Length == 0)
            {
                Debug.LogError("compatibilityMatrix is unset");
                return;
            }
        }
    }
}