using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;

namespace WFCSystem
{
    public class WFC_HexMicroGrid : MonoBehaviour
    {
        // public enum StructureType { Unset = 0, Building = 1, Block = 2, Border = 3, Generic = 4 }

        [Header("General Settings")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private WFCCollapseOrder collapseOrder = 0;
        [SerializeField] private CompatibilityCheck compatibilityCheck = 0;
        public enum CompatibilityCheck { Default = 0, DirectTile, CornerSockets, }
        [Range(1, 10)][SerializeField] private int minEntrances = 1;
        [Range(1, 10)][SerializeField] private int maxEntrances = 2;
        [SerializeField] private bool isWalledEdge;

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
        [SerializeField] private int totalLayers = 1;
        [SerializeField] private TileDirectory tileDirectory;
        [SerializeField] private MicroTileSocketDirectory tileSocketDirectory;
        [SerializeField] private TileCompatibilityDirectory tileCompatibilityDirectory;
        private Dictionary<HexagonTileCompatibilitySide, bool[]>[,] tileDirectCompatibilityMatrix; // Compatibility matrix for tiles

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
        float[] rotationValues = { 0f, 60f, 120f, 180f, 240f, 300f };

        Transform folder_incompatibles;


        void Start()
        {
            if (runOnStart) Invoke("ExecuteWFC", 0.2f);
        }

        public void ExecuteWFC()
        {
            EvaluateTiles();
            UpdateCompatibilityMatrix();

            compatibilityMatrix = tileSocketDirectory.GetCompatibilityMatrix();
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
                (HexagonTileCore nextTile, List<int> rotations) = SelectNextTile(entranceCells[i], tilePrefabs_Edgable, false);
                AssignTileToCell(entranceCells[i], nextTile, rotations);
            }
        }

        private void CollapseEdgeCells()
        {
            for (int i = 0; i < edgeCells.Count; i++)
            {
                HexagonCell currentCell = edgeCells[i];
                if (currentCell.IsAssigned()) continue;

                (HexagonTileCore nextTile, List<int> rotations) = SelectNextTile(currentCell, tilePrefabs_Edgable, false);
                AssignTileToCell(currentCell, nextTile, rotations);
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

                    (HexagonTileCore nextTile, List<int> rotations) = SelectNextTile(cell, tilePrefabs, false);

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
        private void AssignTileToCell(HexagonCell cell, HexagonTileCore tile, int rotation = 0)
        {
            cell.SetTile(tile, rotation);
        }
        private void AssignTileToCell(HexagonCell cell, HexagonTileCore tile, List<int> rotations)
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

        private (HexagonTileCore, List<int>) SelectNextTile(HexagonCell cell, List<HexagonTileCore> prefabsList, bool IsClusterCell)
        {
            // Create a list of compatible tiles and their rotations
            List<(HexagonTileCore, List<int>)> compatibleTilesAndRotations = new List<(HexagonTileCore, List<int>)>();

            Transform cell_folder_incompatibles = new GameObject("Incompatibles").transform;
            cell_folder_incompatibles.transform.SetParent(cell.gameObject.transform);


            // Iterate through all tiles
            for (int i = 0; i < prefabsList.Count; i++)
            {
                if (cell.isEntryCell && !prefabsList[i].isEntrance) continue;

                if (prefabsList[i].isLeveledTile && !cell.isLeveledCell) continue;

                if (cell.isLeveledRampCell && !prefabsList[i].isLeveledRamp) continue;

                if (prefabsList[i].baseLayerOnly && cell.GetGridLayer() > 0) continue;

                if (prefabsList[i].noBaseLayer && cell.GetGridLayer() == 0) continue;

                HexagonTileCore currentTile = prefabsList[i];

                List<int> compatibleTileRotations = GetCompatibleTileRotations(cell, currentTile);

                if (compatibleTileRotations.Count > 0)
                {
                    compatibleTilesAndRotations.Add((currentTile, compatibleTileRotations));
                }
                else
                {
                    if (useDebugTileSpawning) InstantiateTile(currentTile, cell, cell_folder_incompatibles, useDebugTileSpawning);
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

        private List<int> GetCompatibleTileRotations(HexagonCell currentCell, HexagonTileCore currentTile)
        {
            string tileName = currentTile.gameObject.name;

            List<int> compatibleRotations = new List<int>();

            if (compatibilityCheck != CompatibilityCheck.CornerSockets)
            {
                compatibleRotations = GetDirectCompatibleTileRotations(currentCell, currentTile);

                if (compatibleRotations.Count > 0 || compatibilityCheck == CompatibilityCheck.DirectTile)
                {
                    if (compatibleRotations.Count > 0)
                    {
                        Debug.Log("Direct compatible tile rotations found for tile: " + tileName);
                    }
                    else
                    {
                        Debug.Log("INCOMPATIBLE: NO Direct compatible tile rotations found for tile: " + tileName);
                    }

                    return compatibleRotations;
                }
            }

            Debug.LogError("YOU SHOULD NOT SEE THIS IF compatibilityCheck == CompatibilityCheck.DirectTile ");


            HexagonCell.NeighborSideCornerSockets[] neighborTileCornerSocketsBySide = currentCell.GetSideNeighborTileSockets(isWalledEdge, true);
            HexagonCell.NeighborLayerCornerSockets[] layeredNeighborTileCornerSockets = currentCell.GetLayeredNeighborTileSockets(TileContext.Micro);

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

        private List<int> GetCompatibleLayeredTileRotations(HexagonCell currentCell, HexagonTileCore currentTile)
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


        private List<int> GetDirectCompatibleTileRotations(HexagonCell currentCell, HexagonTileCore currentTile)
        {
            string tileName = currentTile.gameObject.name;

            List<int> compatibleRotations = new List<int>();

            HexagonCell.NeighborSideCornerSockets[] neighborTileCornerSocketsBySide = currentCell.GetSideNeighborTileSockets(isWalledEdge, true);

            // Check every rotation
            for (int rotation = 0; rotation < 6; rotation++)
            {
                bool compatibile = true;

                // Check Layered Neighbors First
                if (currentCell.GetGridLayer() > 0 && !currentCell.isLeveledGroundCell)
                {
                    // For now just check bottom neighbor's top against current tile's bottom
                    HexagonCell bottomNeighbor = currentCell.layeredNeighbor[0];

                    // If no neighbor of not neighbor tile, use sockets
                    if (bottomNeighbor == null || bottomNeighbor.GetTile() == null)
                    {
                        HexagonCell.NeighborLayerCornerSockets[] layeredNeighborTileCornerSockets = currentCell.GetLayeredNeighborTileSockets(TileContext.Micro);
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
                    else
                    {
                        HexagonTileCore neighborTile = (HexagonTileCore)bottomNeighbor.GetTile();
                        int incomingTileId = currentTile.GetId();
                        int neighborTileId = neighborTile.GetId();

                        if (tileDirectCompatibilityMatrix[incomingTileId, neighborTileId].Count > 0)
                        {
                            bool tableHasKey = (tileDirectCompatibilityMatrix[incomingTileId, neighborTileId].ContainsKey(HexagonTileCompatibilitySide.Top));

                            if (!tableHasKey || !tileDirectCompatibilityMatrix[incomingTileId, neighborTileId][HexagonTileCompatibilitySide.Top][rotation])
                            {
                                compatibile = false;
                                break;
                            }
                        }


                    }
                }

                // Check Side Neighbors
                if (compatibile)
                {

                    for (int side = 0; side < 6; side++)
                    {
                        HexagonCell neighbor = currentCell.neighborsBySide[side];

                        // If no neighbor of not neighbor tile, use sockets
                        if (neighbor == null || neighbor.GetTile() == null)
                        {

                            HexagonCell.NeighborSideCornerSockets neighborSide = neighborTileCornerSocketsBySide[side];
                            (int[] currentTileSideBottomSockets, int[] currentTileSideTopSockets) = currentTile.GetRotatedCornerSocketsBySide((HexagonSide)side, rotation);

                            if (!compatibilityMatrix[currentTileSideBottomSockets[0], neighborSide.bottomCorners[1]])
                            {
                                compatibile = false;
                                break;
                            }
                        }
                        else
                        {
                            // Debug.Log("GetDirectCompatibleTileRotations - A, neighbor: " + neighbor.gameObject.name + ", cell: " + currentCell.gameObject.name);

                            HexagonTileCore neighborTile = (HexagonTileCore)neighbor.GetTile();
                            int incomingTileId = currentTile.GetId();
                            int neighborTileId = neighborTile.GetId();

                            Debug.Log("GetDirectCompatibleTileRotations - A, current tile: " + tileName + ", existingTile: " + neighborTile.gameObject.name);

                            HexagonTileCompatibilitySide neighborRelativeSide = (HexagonTileCompatibilitySide)(int)currentCell.GetNeighborsRelativeSide((HexagonSide)side);


                            bool tableHasKey = (tileDirectCompatibilityMatrix[incomingTileId, neighborTileId].ContainsKey(neighborRelativeSide));

                            Debug.Log("GetDirectCompatibleTileRotations - neighborRelativeSide: " + neighborRelativeSide + ", tableHasKey: " + tableHasKey);

                            if (!tableHasKey || !tileDirectCompatibilityMatrix[incomingTileId, neighborTileId][neighborRelativeSide][rotation])
                            {
                                Debug.Log("GetDirectCompatibleTileRotations - C, incompatible tile: " + tileName + ", existingTile: " + neighborTile.gameObject.name + ", neighborRelativeSide: " + neighborRelativeSide + ", rotation: " + rotation + "\n currentCell side: " + (HexagonSide)side + ", currentCell Id: " + currentCell.id);

                                compatibile = false;
                                break;
                            }
                            // }
                        }
                    }

                }

                if (compatibile)
                {
                    compatibleRotations.Add(rotation);
                }
            }
            Debug.Log("GetDirectCompatibleTileRotations - Cell: " + currentCell.id + ", compatibleRotations: " + compatibleRotations.Count);
            return compatibleRotations;
        }

        void InstantiateTile(HexagonTileCore prefab, HexagonCell cell, Transform folder, bool debug_incompatible)
        {
            int rotation = cell.currentRotation;

            Vector3 position = cell.transform.position;
            position.y += 0.2f;

            GameObject newTile = Instantiate(prefab.gameObject, position, Quaternion.identity);
            newTile.gameObject.transform.rotation = Quaternion.Euler(0f, rotationValues[rotation], 0f);

            if (!debug_incompatible)
            {
                activeTiles.Add(newTile);
            }
            else
            {
                newTile.gameObject.SetActive(false);
            }
            newTile.transform.SetParent(folder);

            HexagonTileCore tileCore = newTile.GetComponent<HexagonTileCore>();
            tileCore.ShowSocketLabels(false);
            tileCore.SetIgnoreSocketLabelUpdates(true);
        }

        void InstantiateAllTiles()
        {

            Transform folder = new GameObject("Tiles").transform;
            folder.transform.SetParent(gameObject.transform);

            for (int i = 0; i < edgeCells.Count; i++)
            {
                // Skip cluster assigned cells
                // if (cells[i].IsInCluster()) continue;

                HexagonTileCore prefab = (HexagonTileCore)edgeCells[i].GetTile();

                if (prefab == null) continue;


                int rotation = edgeCells[i].currentRotation;

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

                HexagonTileCore prefab = (HexagonTileCore)allCells[i].GetTile();

                if (prefab == null) continue;

                int rotation = allCells[i].currentRotation;

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
            tileLookupByid = tileDirectory.CreateMicroTileDictionary();
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
            WFCUtilities.ShuffleHexTiles(tilePrefabs);
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
            compatibilityMatrix = tileSocketDirectory.GetCompatibilityMatrix();
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