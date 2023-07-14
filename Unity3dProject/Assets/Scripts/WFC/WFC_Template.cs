using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;

namespace WFCSystem
{
    public class WFC_Template : IWFCSystem
    {
        private List<HexagonTileTemplate> tileList; //temp

        public WFC_Template(
            List<HexagonTileTemplate> _tileList,
            Dictionary<int, List<HexagonCellPrototype>> _allCellsByLayer,
            HexagonSocketDirectory _socketDirectory,
            LocationMarkerPrefabOption _locationSettings,
            Transform _tileFolder = null,
            bool _allowRotation = true
         )
        {
            tileList = _tileList;
            socketDirectory = _socketDirectory;

            locationSettings = _locationSettings;

            tileFolder = _tileFolder;

            allowRotation = _allowRotation;

            AssignCells(_allCellsByLayer);
        }

        private HexagonSocketDirectory socketDirectory;
        private TileContext tileContext;
        private LocationMarkerPrefabOption locationSettings;
        private Transform tileFolder = null;
        private Transform transform;


        [SerializeField] private HexagonTileTemplate tilePrefabs_ClusterWFC;
        [SerializeField] private HexagonTileTemplate tilePrefabs_MicroClusterParent;


        [Header("Collapse Settings")]
        [SerializeField] private WFCCollapseOrder_General generalCollapseOrder = 0;
        [SerializeField] private WFCCollapseOrder_CellGrid collapseOrder_grid = WFCCollapseOrder_CellGrid.Edges_First;
        [SerializeField] private WFCCollapseOrder_Cells collapseOrder_cells = 0;
        [SerializeField] private WFC_CellNeighborPropagation neighborPropagation = WFC_CellNeighborPropagation.Edges_Only_Include_Layers;

        [Header("Edge Rules")]
        [SerializeField] private bool useEdgeMicroClusters;
        [SerializeField] private bool isWalledEdge;
        [SerializeField] private bool restrictEntryTiles;
        [SerializeField] private HexagonTileTemplate tilePrefabs_EdgeMicroClusterParent;

        [Header("Tile Settings")]
        [SerializeField] private bool allowInvertedTiles = true;
        [SerializeField] private bool allowRotation = true;

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

        [SerializeField] private List<HexagonTileTemplate> tilePrefabs;
        [SerializeField] private List<HexagonTileTemplate> tilePrefabs_Edgable;
        [SerializeField] private List<HexagonTileTemplate> tilePrefabs_Entrances;
        [SerializeField] private List<HexagonTileTemplate> tilePrefabs_TopLayer;
        [SerializeField] private List<HexagonTileTemplate> tilePrefabs_Path;

        Dictionary<int, HexagonTileTemplate> tileLookupByid;
        [SerializeField] private List<TileClusterPrefab> tileClusterPrefabs;

        [Header("Cell Debug")]
        [SerializeField] private List<HexagonCellPrototype> edgeCells_Grid;
        [SerializeField] private List<HexagonCellPrototype> entryCells;

        public Dictionary<int, List<HexagonCellPrototype>> allLevelCellsByLayer;
        public List<HexagonCellPrototype> topLevelCells;
        [Header(" ")]
        private List<HexagonCellPrototype> allCells;
        public Dictionary<int, List<HexagonCellPrototype>> allCellsByLayer { get; private set; }
        [SerializeField] private List<HexagonCellPrototype> allAssignedCellsInOrder;
        Dictionary<Vector2, Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>> cellLookup_ByLayer_BySize_ByWorldSpace;

        DateTime _buildStartTime;
        bool failed = false;

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

            if (EvaluateTiles() == false) return;

            Evalaute_Folder();

            TimeLog("EvaluateTiles - Time");
            StartTimeLog();

            // if (logIncompatibilities) Debug.Log("Executing WFC - Location Type: " + locationSettings.locationType + ", Tile Context: " + tileContext + ", tile: " + tileDirectory.name);

            isWalledEdge = tileContext == TileContext.Micro ? false : true;

            CollapseEntryCells();

            CollapseEdgeCells();
            TimeLog("CollapseEdgeCells - Time");

            if (failed) return;

            remainingAttempts = 1;//maxAttempts;

            do
            {
                CollapseRemainingCellsByLayer();

                remainingAttempts--;
            } while (!failed && remainingAttempts > 0 && WFCUtilities_V2.HasUnassignedCells(allCells));

            TimeLog("CollapseRemainingCellsByLayer - Time");

            // if (failed) return;
            // StartTimeLog();
            // InstantiateAllTiles();
            // TimeLog("InstantiateAllTiles - Time");

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
                WFCUtilities_V2.CollapseCellAndPropagate_V2(
                    entryCell,
                    tilePrefabs,
                    tilePrefabs_Edgable,
                    neighborPropagation,
                    tileContext,
                    socketDirectory,
                    isWalledEdge,
                    logIncompatibilities,
                    ignoreFailures,
                    allowInvertedTiles,
                    allowRotation
                );
            }
        }

        private void CollapseEdgeCells()
        {
            Debug.Log("Collapsing Edge Cells...");

            List<HexagonTileTemplate> tilePrefabs_Edgable_Formatted = restrictEntryTiles ? tilePrefabs_Edgable.FindAll(t => t.isEntrance == false) : tilePrefabs_Edgable;
            List<HexagonTileTemplate> tilePrefabs_Edgable_No_Entry_Top_Only = tilePrefabs_TopLayer.FindAll(t => t.isEdgeable);
            // List<HexagonTileTemplate> tilePrefabs_Edgable_GridEdge = tilePrefabs_Edgable.FindAll(t => t.GetGridExclusionRule() == HexagonTileTemplate.GridExclusionRule.GridEdgesOnly);

            if (tilePrefabs_Edgable_No_Entry_Top_Only.Count == 0) tilePrefabs_Edgable_No_Entry_Top_Only = tilePrefabs_Edgable_Formatted;

            bool includeLayers = (neighborPropagation == WFC_CellNeighborPropagation.Edges_Only_Include_Layers || neighborPropagation == WFC_CellNeighborPropagation.Edges_Inners_Include_Layers);

            foreach (var kvp in allCellsByLayer)
            {
                if (failed) break;

                int currentLayer = kvp.Key;
                // Get All Layer Edge Cells and collapse cells with fewest neighbors first
                List<HexagonCellPrototype> layerEdgeCells = edgeCells_Grid.FindAll(e => e.GetGridLayer() == currentLayer).OrderBy(e => e.neighbors.Count + e.GetEdgeSideNeighborCount()).ToList();

                if (layerEdgeCells == null || layerEdgeCells.Count == 0)
                {
                    Debug.LogError("NO Edge Cells on layer: " + currentLayer);
                    continue;
                }

                foreach (HexagonCellPrototype edgeCell in layerEdgeCells)
                {
                    if (failed) break;

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
                int currentCellLayer = currentCell.GetGridLayer();

                bool includeLayerNwighbors = (neighborPropagation == WFC_CellNeighborPropagation.Edges_Only_Include_Layers || neighborPropagation == WFC_CellNeighborPropagation.Edges_Inners_Include_Layers);

                // Get Unassigned Neighbors
                List<HexagonCellPrototype> unassignedNeighbors = currentCell.neighbors.FindAll(n => n.IsWFC_Assigned() == false
                        && ((includeLayerNwighbors == false && n.GetGridLayer() == currentCellLayer)
                        || (includeLayerNwighbors && n.GetGridLayer() >= currentCellLayer)
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
                        List<HexagonCellPrototype> innerNeighbors = unassignedNeighbors.FindAll(n => n.isEdgeCell == false)
                            .OrderBy(n => n.neighbors.Count)
                            .OrderByDescending(n => n.GetEdgeSideNeighborCount()).ToList();

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
                if (failed) break;

                int currentLayer = kvp.Key;
                List<HexagonCellPrototype> layerCells = kvp.Value;
                layerCells = layerCells.OrderBy(e => e.neighbors.Count + e.GetEdgeSideNeighborCount()).ToList();

                if (logIncompatibilities) Debug.LogError("CollapseRemainingCellsByLayer - currentLayer: " + kvp.Key + ", available layerCells: " + layerCells.Count + ", total: " + kvp.Value.Count);

                foreach (HexagonCellPrototype cell in layerCells)
                {
                    if (failed) break;

                    if (logIncompatibilities) Debug.LogError("CollapseRemainingCellsByLayer - L: " + kvp.Key + ", cell: " + cell.LogStats());

                    CollapseCellAndPropagate(cell);
                }
            }
        }

        private bool SelectAndAssignNext(HexagonCellPrototype cell, List<HexagonTileTemplate> prefabsList)
        {
            (HexagonTileTemplate nextTile, List<int[]> rotations) = WFCUtilities_V2.SelectNextTile_V2(cell, prefabsList, allowInvertedTiles, isWalledEdge, tileContext, socketDirectory, logIncompatibilities, allowRotation);

            // if (nextTile != null && nextTile.HasTileClusterPrefab())
            // {
            //     if (cellLookup_ByLayer_BySize_ByWorldSpace == null)
            //     {
            //         Debug.LogError("cellLookup_ByLayer_BySize_ByWorldSpace is null!");
            //         return false;
            //     }

            //     (Dictionary<int, List<HexagonCellPrototype>> subCellGrid, List<HexagonCellPrototype> hostCells) = HexGridUtil.Assign_TileClusterFromHost(
            //         cell,
            //         nextTile,
            //         cellLookup_ByLayer_BySize_ByWorldSpace,
            //         true
            //     );

            //     int clusterMembers = hostCells.Count;
            //     if (clusterMembers == 0 || subCellGrid == null)
            //     {
            //         Debug.LogError("No clusterMembers Or subCellGrid!");
            //         return false;
            //     }

            //     TileClusterPrefab tileClusterPrefab = nextTile.GetClusterPrefab();

            //     WFC_Core wfc = new WFC_Core(
            //         tileClusterPrefab.GetSettings().tileDirectory,
            //         subCellGrid,

            //         locationSettings,
            //         tileFolder
            //     );

            //     wfc.ExecuteWFC();

            //     return true;
            // }

            bool suceess = WFCUtilities_V2.AssignTileToCell_V2(cell, nextTile, rotations, ignoreFailures);

            if (suceess && cell.currentTile_V2 != null) WFCUtilities_V2.InstantiateTile_V2(cell.currentTile_V2, cell, tileFolder, activeTiles);

            failed = !suceess;
            // failed = suceess;

            return suceess;
        }



        public void InstantiateAllTiles()
        {
            foreach (HexagonCellPrototype cell in allCells)
            {
                HexagonTileTemplate prefab = cell.currentTile_V2;
                if (prefab == null) continue;

                WFCUtilities_V2.InstantiateTile_V2(prefab, cell, tileFolder, activeTiles);
            }
        }

        public void Evalaute_Folder()
        {
            if (tileFolder == null)
            {
                tileFolder = new GameObject("Tile Folder").transform;
                tileFolder.transform.SetParent(this.transform);
            }
        }

        private bool EvaluateTiles()
        {
            // Get All Tile Prefabs
            // tileLookupByid = tileDirectory.CreateTileDictionary();
            // List<HexagonTileTemplate> _tilePrefabs = tileLookupByid.Select(x => x.Value).ToList();

            // tileClusterPrefabLookupByid = tileDirectory.CreateTileClusterPrefabDictionary();
            // tileClusterPrefabs = tileClusterPrefabLookupByid.Select(x => x.Value).ToList();

            List<HexagonTileTemplate> allTilePrefabs = tileList;
            // List<HexagonTileTemplate> allTilePrefabs = tileDirectory.GetTiles(true);

            if (allTilePrefabs.Count == 0)
            {
                Debug.LogError("NO tilePrefabs");
                return false;
            }
            Debug.Log("tilePrefabs detected: " + allTilePrefabs.Count); // + ", tileDirectory: " + tileDirectory.name);

            foreach (var tile in allTilePrefabs)
            {
                if (tile.socketProfileBySide == null)
                {
                    Debug.LogError("tile has null socketProfileBySide");
                    return false;
                }
            }

            tilePrefabs = allTilePrefabs;

            // Extract Edge Tiles
            tilePrefabs_Edgable = tilePrefabs.FindAll(x => x.isEdgeable).ToList();
            Debug.Log("tilePrefabs_Edgable: " + tilePrefabs_Edgable.Count);

            // Extract Entrance Tiles
            tilePrefabs_Entrances = new List<HexagonTileTemplate>();
            // tilePrefabs_Entrances = tilePrefabs.FindAll(x => x.isEdgeable && x.isEntrance).ToList();
            // Debug.Log("tilePrefabs_Entrances: " + tilePrefabs_Edgable.Count);

            // Extract Top Layer tiles
            // tilePrefabs_TopLayer = tilePrefabs.FindAll(x => x.GetExcludeLayerState() == HexagonTileTemplate.ExcludeLayerState.TopLayerOnly || x.IsRoofable()).ToList();
            tilePrefabs_TopLayer = new List<HexagonTileTemplate>();
            // Extract Path Tiles
            // tilePrefabs_Path = tilePrefabs.FindAll(x => x.isPath).ToList();

            // Extract Inner Tiles
            List<HexagonTileTemplate> innerTilePrefabs = new List<HexagonTileTemplate>();
            innerTilePrefabs.AddRange(tilePrefabs.Except(tilePrefabs.FindAll(x => x.isEntrance)));

            tilePrefabs = innerTilePrefabs;

            Debug.Log("processed tilePrefabs: " + tilePrefabs.Count);

            // Shuffle the prefabs
            WFCUtilities_V2.ShuffleTiles(tilePrefabs);
            return true;
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

            tileContext = allCells[0].GetSize() == (int)HexCellSizes.X_4 ? TileContext.Micro : TileContext.Default;

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