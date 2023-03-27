using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace WFCSystem
{
    public class WFC_HexMicroGrid : MonoBehaviour, IWFCSystem
    {
        [Header("General Settings")]
        [SerializeField] private bool runOnStart = true;
        public enum CompatibilityCheck { Default = 0, DirectTile, CornerSockets }
        [SerializeField] private CompatibilityCheck compatibilityCheck = 0;
        [SerializeField] private bool allowInvertedTiles = true;

        [Header("Collapse Settings")]
        [SerializeField] private WFCCollapseOrder_General generalCollapseOrder = 0;
        [SerializeField] private WFCCollapseOrder_CellGrid collapseOrder_grid = WFCCollapseOrder_CellGrid.Edges_First;
        [SerializeField] private WFCCollapseOrder_Cells collapseOrder_cells = 0;
        [SerializeField] private WFC_CellNeighborPropagation neighborPropagation = WFC_CellNeighborPropagation.Edges_Only_Include_Layers;

        [Header("Edge Rules")]
        [SerializeField] private bool isWalledEdge;
        [SerializeField] private bool restrictEntryTiles;
        [Range(0, 10)][SerializeField] private int minEntrances = 1;
        [Range(1, 10)][SerializeField] private int maxEntrances = 2;

        [Header("System Settings")]
        [SerializeField] private bool logIncompatibilities = false;
        [SerializeField] private bool ignoreFailures = true;
        [Range(1, 9999)][SerializeField] private int maxAttempts = 9999;
        [SerializeField] private int remainingAttempts;

        // [Header("Roll-back Attempts")]

        [Header("Grid Settings")]
        [SerializeField] private int _radius;
        public void SetRadius(int value)
        {
            _radius = value;
        }
        [SerializeField] private int totalLayers = 1;
        [SerializeField] private TileDirectory tileDirectory;
        [SerializeField] private HexagonSocketDirectory socketDirectory;
        [SerializeField] private TileCompatibilityDirectory tileCompatibilityDirectory;
        private Dictionary<HexagonTileCompatibilitySide, HashSet<int>>[,] tileDirectCompatibilityMatrix; // Compatibility matrix for tiles

        [Header("Prefabs")]
        [SerializeField] private List<HexagonTileCore> tilePrefabs;
        [SerializeField] private List<HexagonTileCore> tilePrefabs_Entrances;
        [SerializeField] private List<HexagonTileCore> tilePrefabs_Edgable;
        [SerializeField] private List<HexagonTileCore> tilePrefabs_TopLayer;
        [SerializeField] private GameObject markerPrefab;

        [Header("Debug")]
        [SerializeField] private bool useDebugTileSpawning;

        [SerializeField] private int matrixLength;
        private int numTiles; // Number of tile prefabs
        public bool[,] compatibilityMatrix = null; // Compatibility matrix for tiles

        [Header("Cell Debug")]
        [SerializeField] private List<HexagonCell> allAssignedCellsInOrder;
        [SerializeField] private List<GameObject> activeTiles;
        [SerializeField] private List<HexagonCell> allCellsList;
        [SerializeField] private List<HexagonCell> edgeCells;
        [SerializeField] private List<HexagonCell> edgeConnectors;
        [SerializeField] private List<HexagonCell> entryCells;
        [SerializeField] private int totalEdgeCells = 0;
        public Dictionary<int, List<HexagonCell>> allCellsByLayer;
        public Dictionary<int, List<HexagonCell>> allLevelCellsByLayer;
        public List<HexagonCell> topLevelCells;
        Dictionary<int, HexagonTileCore> tileLookupByid;
        Transform folder_incompatibles;


        void Start()
        {
            if (runOnStart) Invoke("ExecuteWFC", 0.2f);
        }

        public void ExecuteWFC()
        {
            EvaluateTiles();
            UpdateCompatibilityMatrix();

            compatibilityMatrix = socketDirectory.GetCompatibilityMatrix();

            if (compatibilityMatrix != null) matrixLength = compatibilityMatrix.Length;

            if (compatibilityCheck != CompatibilityCheck.CornerSockets)
            {
                if (tileCompatibilityDirectory == null) Debug.LogError("tileCompatibilityDirectory is null");

                tileDirectCompatibilityMatrix = tileCompatibilityDirectory.GetCompatibilityMatrix();

                if (tileDirectCompatibilityMatrix == null) Debug.LogError("tileDirectCompatibilityMatrix is null");

                tileCompatibilityDirectory.ShowDebugData();
            }

            if (useDebugTileSpawning)
            {
                folder_incompatibles = new GameObject("Incompatibles").transform;
                folder_incompatibles.transform.SetParent(gameObject.transform);
            }

            EvaluateCells();

            CollapseEdgeConnectorCells();

            CollapseEntryCells();

            CollapseEdgeCells();

            CollapseRemainingCellsByLayer();
            //temp 
            // int reattempts = 3;

            // while (reattempts > 0)
            // {
            //     CollapseRemainingCellsByLayer();
            //     reattempts--;
            // }


            InstantiateAllTiles();

            Debug.Log("Execution of WFC Complete");
        }

        private bool IsUnassignedCells()
        {
            // Iterate through all cells and check if there is any unassigned cell
            foreach (HexagonCell cell in allCellsList)
            {
                if (cell.IsAssigned() == false) return true;
            }
            return false;
        }

        private void CollapseCellAndPropagate(HexagonCell currentCell)
        {
            bool assigned = currentCell.IsAssigned() ? true : SelectAndAssignNext(currentCell, currentCell.isEdgeCell ? tilePrefabs_Edgable : tilePrefabs);

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
                            // if (neighbor.GetEdgeCellType() == EdgeCellType.Connector)
                            // {
                            //     Debug.Log("Edge Connector: " + neighbor.id + ", wasAssigned: " + wasAssigned);
                            // }
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

        private void CollapseEntryCells()
        {
            if (entryCells.Count > 0)
            {
                Debug.Log("Collapse Entry Cells");

                foreach (HexagonCell entryCell in entryCells)
                {
                    bool assigned = SelectAndAssignNext(entryCell, tilePrefabs_Entrances);
                    CollapseCellAndPropagate(entryCell);
                }
            }
        }

        private void CollapseEdgeConnectorCells()
        {
            if (edgeConnectors.Count > 0)
            {
                Debug.Log("Collapse Edge Connector Cells");

                foreach (HexagonCell connectorCell in edgeConnectors)
                {
                    bool assigned = SelectAndAssignNext(connectorCell, tilePrefabs_Edgable);
                    CollapseCellAndPropagate(connectorCell);
                }

            }
        }

        private void CollapseEdgeCells()
        {

            List<HexagonTileCore> tilePrefabs_Edgable_Formatted = restrictEntryTiles ? tilePrefabs_Edgable.FindAll(t => t.isEntrance == false) : tilePrefabs_Edgable;
            List<HexagonTileCore> tilePrefabs_Edgable_No_Entry_Top_Only = tilePrefabs_TopLayer.FindAll(t => t.isEdgeable);

            if (tilePrefabs_Edgable_No_Entry_Top_Only.Count == 0) tilePrefabs_Edgable_No_Entry_Top_Only = tilePrefabs_Edgable_Formatted;

            bool includeLayers = (neighborPropagation == WFC_CellNeighborPropagation.Edges_Only_Include_Layers || neighborPropagation == WFC_CellNeighborPropagation.Edges_Inners_Include_Layers);
            int layersToCollapse = includeLayers ? totalLayers : 0;

            // Debug.Log("Collapse Edge Cells for " + (layersToCollapse + 1) + " layers");
            int currentLayer = 0;
            do
            {
                List<HexagonCell> layerEdgeCells = edgeCells.FindAll(e => e.GetGridLayer() == currentLayer).OrderByDescending(e => e._neighbors.Count).ToList();

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

        private void CollapseRemainingCellsByLayer()
        {
            foreach (var kvp in allCellsByLayer)
            {
                int layer = kvp.Key;
                List<HexagonCell> layerCells = HexagonCell.GetAvailableCellsForNextLayer(kvp.Value);
                layerCells = layerCells.OrderByDescending(e => e._neighbors.Count).ToList();
                foreach (HexagonCell cell in layerCells)
                {
                    CollapseCellAndPropagate(cell);
                }
            }

            // for (int currentLayer = 0; currentLayer < totalLayers; currentLayer++)
            // {
            //     List<HexagonCell> layerCells;
            //     List<HexagonCellCluster> layerClusters = new List<HexagonCellCluster>();
            //     // if (currentLayer == 0)
            //     // {
            //     //     layerCells = allCellsByLayer[0].FindAll(c => !c.IsAssigned() && !c.isLeveledCell);
            //     // }
            //     // else
            //     // {
            //     layerCells = HexagonCell.GetAvailableCellsForNextLayer(allCellsByLayer[currentLayer]);
            //     layerCells = layerCells.OrderByDescending(e => e._neighbors.Count).ToList();
            //     foreach (HexagonCell cell in layerCells)
            //     {
            //         CollapseCellAndPropagate(cell);
            //     }
            // }
        }

        private bool SelectAndAssignNext(HexagonCell cell, List<HexagonTileCore> prefabsList)
        {
            // (HexagonTileCore nextTile, List<int[]> rotations) = WFCUtilities.SelectNextTile(cell, prefabsList, allowInvertedTiles, isWalledEdge, TileContext.Micro, socketDirectory, logIncompatibilities);
            // bool assigned = WFCUtilities.AssignTileToCell(cell, nextTile, rotations, ignoreFailures);

            bool assigned = WFCUtilities.SelectAndAssignNext(cell, prefabsList, TileContext.Micro, socketDirectory, isWalledEdge, logIncompatibilities, ignoreFailures, allowInvertedTiles);
            if (assigned) allAssignedCellsInOrder.Add(cell);
            return assigned;
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

        private void EvaluateTiles()
        {
            // Get All Tile Prefabs
            tileLookupByid = tileDirectory.CreateHexTileDictionary();
            List<HexagonTileCore> _tilePrefabs = tileLookupByid.Select(x => x.Value).ToList();

            // Check For Nulls
            foreach (HexagonTileCore prefab in tilePrefabs)
            {
                int id = prefab.GetId();
            }

            tilePrefabs = new List<HexagonTileCore>();
            tilePrefabs.AddRange(_tilePrefabs);

            // Extract Edge Tiles
            tilePrefabs_Edgable = tilePrefabs.FindAll(x => x.isEdgeable && x.isEntrance == false).ToList();

            // Extract Entrance Tiles
            tilePrefabs_Entrances = tilePrefabs.FindAll(x => x.isEdgeable && x.isEntrance).ToList();

            // Extract Top Layer tiles
            tilePrefabs_TopLayer = tilePrefabs.FindAll(x => x.GetExcludeLayerState() == HexagonTileCore.ExcludeLayerState.TopLayerOnly || x.IsRoofable()).ToList();

            // Extract Inner Tiles
            List<HexagonTileCore> innerTilePrefabs = new List<HexagonTileCore>();
            innerTilePrefabs.AddRange(tilePrefabs.Except(tilePrefabs.FindAll(x => x.IsExteriorWall() || x.isEntrance || x.isLayerConnector || x.isPath)));

            tilePrefabs = innerTilePrefabs;

            // Shuffle the prefabs
            WFCUtilities.ShuffleTiles(tilePrefabs);
        }


        private void EvaluateCells()
        {
            entryCells = new List<HexagonCell>();

            edgeCells = HexagonCell.GetEdgeCells(allCellsList);
            totalEdgeCells = edgeCells.Count;

            edgeConnectors = edgeCells.FindAll(c => c.GetEdgeCellType() == EdgeCellType.Connector);

            if (minEntrances > 0)
            {
                entryCells = HexagonCell.GetRandomEntryCells(edgeCells.Except(edgeConnectors).ToList(), maxEntrances, true, 0, false);
            }

            // TODO: find a better way to jsut provide this without doing this extra stuff
            totalLayers = edgeCells.OrderByDescending(c => c.GetGridLayer()).ToList()[0].GetGridLayer() + 1;

            // allCellsByLayer = HexagonCell.OrganizeCellsByLevel(allCellsList);
        }

        public void SetCells(Dictionary<int, List<HexagonCell>> _allCellsByLayer, List<HexagonCell> _allCells)
        {
            allCellsByLayer = _allCellsByLayer;
            allCellsList = _allCells;
        }

        public void SetCells(Dictionary<int, List<HexagonCell>> _allCellsByLayer, List<HexagonCell> _allCells, Dictionary<HexagonCellCluster, Dictionary<int, List<HexagonCell>>> _allCellsByLayer_X4_ByCluster = null)
        {

            allCellsByLayer = _allCellsByLayer;
            allCellsList = _allCells;
            // allCellsByLayer_X4_ByCluster = _allCellsByLayer_X4_ByCluster;
        }

        // public void SetCells(List<HexagonCell> _allCells)
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

        // private List<int> GetDirectCompatibleTileRotations(HexagonCell currentCell, HexagonTileCore currentTile)
        // {
        //     string tileName = currentTile.gameObject.name;

        //     List<int> compatibleRotations = new List<int>();

        //     HexagonCell.NeighborSideCornerSockets[] neighborTileCornerSocketsBySide = currentCell.GetSideNeighborTileSockets(isWalledEdge, true);

        //     // Check every rotation
        //     for (int rotation = 0; rotation < 6; rotation++)
        //     {
        //         bool compatibile = true;

        //         // Check Layered Neighbors First
        //         if (currentCell.GetGridLayer() > 0 && !currentCell.isLeveledGroundCell)
        //         {
        //             // For now just check bottom neighbor's top against current tile's bottom
        //             HexagonCell bottomNeighbor = currentCell.layeredNeighbor[0];

        //             // If no neighbor of not neighbor tile, use sockets
        //             if (bottomNeighbor == null || bottomNeighbor.GetTile() == null)
        //             {
        //                 HexagonCell.NeighborLayerCornerSockets[] layeredNeighborTileCornerSockets = currentCell.GetLayeredNeighborTileSockets(TileContext.Micro);
        //                 int[] currentTileBottomSockets = currentTile.GetRotatedLayerCornerSockets(false, rotation);

        //                 for (int i = 0; i < layeredNeighborTileCornerSockets[0].corners.Length; i++)
        //                 {
        //                     if (!compatibilityMatrix[currentTileBottomSockets[i], layeredNeighborTileCornerSockets[0].corners[i]])
        //                     {
        //                         Debug.LogError(tileName + " Not compatibile with bottom layer. currentTileBottomSocket: " + currentTileBottomSockets[i] + ", corner: " + (HexagonCorner)i);
        //                         compatibile = false;
        //                         break;
        //                     }
        //                 }
        //             }
        //             else
        //             {
        //                 HexagonTileCore neighborTile = (HexagonTileCore)bottomNeighbor.GetTile();
        //                 int incomingTileId = currentTile.GetId();
        //                 int neighborTileId = neighborTile.GetId();

        //                 bool tableHasKey = (tileDirectCompatibilityMatrix[incomingTileId, neighborTileId].ContainsKey(HexagonTileCompatibilitySide.Top));


        //                 // HexagonTileCompatibilitySide currentTileRotatedSide = tileCompatibilityDirectory.GetRotatedTargetSide(incomingRelativeCompatibilitySide, currentRotation);

        //                 // if (!tableHasKey || !tileDirectCompatibilityMatrix[incomingTileId, neighborTileId][HexagonTileCompatibilitySide.Top][rotation])
        //                 // {
        //                 if (!tileCompatibilityDirectory.AreTilesCombatible(incomingTileId, neighborTileId, HexagonTileCompatibilitySide.Top, rotation, bottomNeighbor.GetTileRotation()))
        //                 {
        //                     Debug.Log("GetDirectCompatibleTileRotations - A1, incompatible tile: " + tileName + ", existingTile: " + neighborTile.gameObject.name + ", neighborRelativeSide: " + HexagonTileCompatibilitySide.Top + ", rotation: " + rotation + "\ncurrentCell Id: " + currentCell.id + ", tableHasKey" + tableHasKey);

        //                     compatibile = false;
        //                 }
        //             }
        //         }

        //         // Check Side Neighbors
        //         if (compatibile)
        //         {

        //             for (int side = 0; side < 6; side++)
        //             {
        //                 HexagonCell neighbor = currentCell.neighborsBySide[side];

        //                 // If no neighbor of not neighbor tile, use sockets
        //                 if (neighbor == null || neighbor.GetTile() == null)
        //                 {

        //                     HexagonCell.NeighborSideCornerSockets neighborSide = neighborTileCornerSocketsBySide[side];
        //                     (int[] currentTileSideBottomSockets, int[] currentTileSideTopSockets) = currentTile.GetRotatedCornerSocketsBySide((HexagonSide)side, rotation, false);

        //                     if (!compatibilityMatrix[currentTileSideBottomSockets[0], neighborSide.bottomCorners[1]])
        //                     {
        //                         compatibile = false;
        //                         break;
        //                     }
        //                 }
        //                 else
        //                 {
        //                     // Debug.Log("GetDirectCompatibleTileRotations - A, neighbor: " + neighbor.gameObject.name + ", cell: " + currentCell.gameObject.name);

        //                     HexagonTileCore neighborTile = (HexagonTileCore)neighbor.GetTile();
        //                     int incomingTileId = currentTile.GetId();
        //                     int neighborTileId = neighborTile.GetId();

        //                     Debug.Log("GetDirectCompatibleTileRotations - A, current tile: " + tileName + ", existingTile: " + neighborTile.gameObject.name);

        //                     HexagonTileCompatibilitySide neighborRelativeSide = (HexagonTileCompatibilitySide)(int)currentCell.GetNeighborsRelativeSide((HexagonSide)side);


        //                     bool tableHasKey = (tileDirectCompatibilityMatrix[incomingTileId, neighborTileId].ContainsKey(neighborRelativeSide));

        //                     Debug.Log("GetDirectCompatibleTileRotations - neighborRelativeSide: " + neighborRelativeSide + ", tableHasKey: " + tableHasKey);

        //                     // if (!tableHasKey || !tileDirectCompatibilityMatrix[incomingTileId, neighborTileId][neighborRelativeSide][rotation])
        //                     // {

        //                     if (!tileCompatibilityDirectory.AreTilesCombatible(incomingTileId, neighborTileId, neighborRelativeSide, rotation, neighbor.GetTileRotation()))
        //                     {
        //                         Debug.Log("GetDirectCompatibleTileRotations - C, incompatible tile: " + tileName + ", existingTile: " + neighborTile.gameObject.name + ", neighborRelativeSide: " + neighborRelativeSide + ", rotation: " + rotation + "\n currentCell side: " + (HexagonSide)side + ", currentCell Id: " + currentCell.id);

        //                         compatibile = false;
        //                         break;
        //                     }
        //                     // }
        //                 }
        //             }

        //         }

        //         if (compatibile)
        //         {
        //             compatibleRotations.Add(rotation);
        //         }
        //     }
        //     Debug.Log("GetDirectCompatibleTileRotations - Cell: " + currentCell.id + ", compatibleRotations: " + compatibleRotations.Count);
        //     return compatibleRotations;
        // }

    }

}