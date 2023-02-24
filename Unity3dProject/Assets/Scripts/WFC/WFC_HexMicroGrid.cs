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
        [SerializeField] private WFCCollapseOrder collapseOrder = 0;
        [SerializeField] private CompatibilityCheck compatibilityCheck = 0;
        public enum CompatibilityCheck { Default = 0, DirectTile, CornerSockets, }
        [Range(1, 10)][SerializeField] private int minEntrances = 1;
        [Range(1, 10)][SerializeField] private int maxEntrances = 2;
        [SerializeField] private bool isWalledEdge;
        [SerializeField] private bool allowInvertedTiles = true;

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
        [SerializeField] private int totalLayers = 1;
        [SerializeField] private TileDirectory tileDirectory;
        [SerializeField] private HexagonSocketDirectory socketDirectory;
        [SerializeField] private TileCompatibilityDirectory tileCompatibilityDirectory;
        private Dictionary<HexagonTileCompatibilitySide, HashSet<int>>[,] tileDirectCompatibilityMatrix; // Compatibility matrix for tiles

        [Header("Prefabs")]
        [SerializeField] private List<HexagonTileCore> tilePrefabs;
        [SerializeField] private List<HexagonTileCore> tilePrefabs_Edgable;
        [SerializeField] private GameObject markerPrefab;

        [Header("Debug")]
        [SerializeField] private bool useDebugTileSpawning;

        [SerializeField] private int matrixLength;
        private int numTiles; // Number of tile prefabs
        public bool[,] compatibilityMatrix = null; // Compatibility matrix for tiles

        [Header("Cell Debug")]
        [SerializeField] private List<GameObject> activeTiles;
        [SerializeField] private List<HexagonCell> allCells;
        [SerializeField] private List<HexagonCell> edgeCells;
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

            CollapseEntranceCells();
            Debug.Log("Entrance Cells Assigned");

            CollapseEdgeCells();
            Debug.Log("Edge Cells Assigned");

            CollapseRemainingCellsByLayer();

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
                SelectAndAssignNext(entranceCells[i], tilePrefabs_Edgable);
            }
        }

        private void CollapseEdgeCells()
        {
            for (int i = 0; i < edgeCells.Count; i++)
            {
                HexagonCell currentCell = edgeCells[i];
                if (currentCell.IsAssigned()) continue;

                SelectAndAssignNext(currentCell, tilePrefabs_Edgable);
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
                }
                else
                {
                    layerCells = HexagonCell.GetAvailableCellsForNextLayer(allCellsByLayer[currentLayer]);
                }

                foreach (HexagonCell cell in layerCells)
                {
                    if (cell.IsAssigned()) continue;

                    cell.highlight = true;

                    bool assigned = SelectAndAssignNext(cell, tilePrefabs);

                    if (assigned == false) cell.SetIgnored(true);

                    if (assigned == false && !ignoreFailures)
                    {
                        Debug.LogError("No tile found for cell: " + cell.id);

                        for (int i = 0; i < cell._neighbors.Count; i++)
                        {
                            Debug.LogError("neighbor: " + cell._neighbors[i].id + ",");
                        }
                    }
                }
            }
        }

        private bool SelectAndAssignNext(HexagonCell cell, List<HexagonTileCore> prefabsList)
        {
            (HexagonTileCore nextTile, List<int[]> rotations) = WFCUtilities.SelectNextTile(cell, prefabsList, allowInvertedTiles, isWalledEdge, socketDirectory, logIncompatibilities);
            return WFCUtilities.AssignTileToCell(cell, nextTile, rotations, ignoreFailures);
        }

        public void InstantiateAllTiles()
        {
            Transform folder = new GameObject("Tiles").transform;
            folder.transform.SetParent(gameObject.transform);

            for (int i = 0; i < edgeCells.Count; i++)
            {
                // Skip cluster assigned cells
                // if (cells[i].IsInCluster()) continue;

                HexagonTileCore prefab = edgeCells[i].GetCurrentTile();
                if (prefab == null) continue;

                WFCUtilities.InstantiateTile(prefab, edgeCells[i], folder, activeTiles);
            }

            for (int i = 0; i < allCells.Count; i++)
            {
                // Skip cluster assigned cells
                // if (cells[i].IsInCluster()) continue;

                HexagonTileCore prefab = allCells[i].GetCurrentTile();
                if (prefab == null) continue;

                WFCUtilities.InstantiateTile(prefab, allCells[i], folder, activeTiles);
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
            tilePrefabs_Edgable = tilePrefabs.FindAll(x => x.isEdgeable).ToList();

            // Extract Inner Tiles
            List<HexagonTileCore> innerTilePrefabs = new List<HexagonTileCore>();
            innerTilePrefabs.AddRange(tilePrefabs.Except(tilePrefabs.FindAll(x => x.IsExteriorWall() || x.isEntrance || x.isLayerConnector || x.isPath)));

            tilePrefabs = innerTilePrefabs;

            // Shuffle the prefabs
            WFCUtilities.ShuffleTiles(tilePrefabs);
        }


        private void EvaluateCells()
        {
            // Place edgeCells first 
            edgeCells = HexagonCell.GetEdgeCells(allCells);
            totalEdgeCells = edgeCells.Count;

            entryCells = HexagonCell.GetRandomEntryCells(edgeCells, maxEntrances, true, 0, false);

            // TODO: find a better way to jsut provide this without doing this extra stuff
            totalLayers = edgeCells.OrderByDescending(c => c.GetGridLayer()).ToList()[0].GetGridLayer() + 1;

            allCellsByLayer = HexagonCell.OrganizeCellsByLevel(allCells);

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
            UpdateCompatibilityMatrix();
        }

        // void InstantiateTile(HexagonTileCore prefab, HexagonCell cell, Transform folder, bool debug_incompatible)
        // {
        //     int rotation = cell.GetTileRotation();

        //     Vector3 position = cell.transform.position;
        //     position.y += 0.2f;

        //     GameObject newTile = Instantiate(prefab.gameObject, position, Quaternion.identity);
        //     newTile.gameObject.transform.rotation = Quaternion.Euler(0f, rotationValues[rotation], 0f);

        //     if (!debug_incompatible)
        //     {
        //         activeTiles.Add(newTile);
        //     }
        //     else
        //     {
        //         newTile.gameObject.SetActive(false);
        //     }
        //     newTile.transform.SetParent(folder);

        //     HexagonTileCore tileCore = newTile.GetComponent<HexagonTileCore>();
        //     tileCore.ShowSocketLabels(false);
        //     tileCore.SetIgnoreSocketLabelUpdates(true);
        // }


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