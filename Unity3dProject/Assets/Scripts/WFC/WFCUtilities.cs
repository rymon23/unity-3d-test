using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProceduralBase;

namespace WFCSystem
{
    public interface IWFCSystem
    {
        public void ExecuteWFC();
        public void SetRadius(int value);
        public void InstantiateAllTiles();

        public void AssignCells(Dictionary<int, List<HexagonCellPrototype>> _allCellsByLayer);
        public void AssignCells(List<HexagonCellPrototype> _allCells);
        public void AssignCells(Dictionary<int, List<HexagonCellPrototype>> _allCellsByLayer, List<HexagonCellPrototype> _allCells);
        public void AssignCells(Dictionary<int, List<HexagonCell>> _allCellsByLayer);
        public void AssignCells(List<HexagonCell> _allCells);
        public void AssignCells(Dictionary<int, List<HexagonCell>> _allCellsByLayer, List<HexagonCell> _allCells);
        public void AssignCells(
            Dictionary<int, List<HexagonCell>> _allCellsByLayer,
            List<HexagonCell> _allCells,
            Dictionary<HexagonCellCluster, Dictionary<int, List<HexagonCell>>> _allCellsByLayer_X4_ByCluster
        );
    }

    public enum WFCCollapseOrder_General
    {
        Default = 0, // Edges -> Center => th rest
        Contract, // Start at the edges
        Expand, // Start at the center
    }
    public enum WFCCollapseOrder_CellGrid
    {
        Edges_First = 0,
        Inners_First,
        Random
    }
    public enum WFCCollapseOrder_Cells
    {
        Neighbor_Propogation = 0,
        Sequential,
    }
    public enum WFC_CellNeighborPropagation
    {
        Edges_Only_Include_Layers = 0,
        Edges_Only_No_Layers,
        Edges_Inners_No_Layers,
        Edges_Inners_Include_Layers,
        NO_Neighbor_Propogation
    }

    static class WFCUtilities
    {
        public static float[] s_TileRotationAngles { get; } = { 0f, 60f, 120f, 180f, 240f, 300f };

        public static void ShuffleTiles(List<HexagonTile> tiles)
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
        public static void ShuffleTiles(List<HexagonTileCluster> tiles)
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
        public static void ShuffleHexTiles(List<IHexagonTile> tiles)
        {
            int n = tiles.Count;
            for (int i = 0; i < n; i++)
            {
                // Get a random index from the remaining elements
                int r = i + UnityEngine.Random.Range(0, n - i);
                // Swap the current element with the random one
                IHexagonTile temp = tiles[r];
                tiles[r] = tiles[i];
                tiles[i] = temp;
            }
        }
        public static void ShuffleTiles(List<HexagonTileCore> tiles)
        {
            int n = tiles.Count;
            for (int i = 0; i < n; i++)
            {
                // Get a random index from the remaining elements
                int r = i + UnityEngine.Random.Range(0, n - i);
                // Swap the current element with the random one
                HexagonTileCore temp = tiles[r];
                tiles[r] = tiles[i];
                tiles[i] = temp;
            }
        }

        public static List<HexagonCell> SelectRandomCells(List<HexagonCell> allCells, int min = 2, int max = 7)
        {
            List<HexagonCell> availableCells = new List<HexagonCell>();
            availableCells.AddRange(allCells);

            int numCells = UnityEngine.Random.Range(min, max + 1);
            List<HexagonCell> selectedCells = new List<HexagonCell>();

            for (int i = 0; i < numCells; i++)
            {
                if (availableCells.Count == 0) break;
                int index = UnityEngine.Random.Range(0, availableCells.Count);
                HexagonCell cell = availableCells[index];
                selectedCells.Add(cell);
                availableCells.RemoveAt(index);
            }

            return selectedCells;
        }

        public static List<HexagonCell> GetAvailableCellsForNextLayer(List<HexagonCell> allLayerCells)
        {
            List<HexagonCell> available = new List<HexagonCell>();

            foreach (HexagonCell currentCell in allLayerCells)
            {
                if (currentCell.IsAssigned() || currentCell.isGroundRamp) continue;

                // if (!currentCell.isLeveledCell && currentCell.layeredNeighbor[0] != null && !currentCell.layeredNeighbor[0].isLeveledRampCell && currentCell.layeredNeighbor[0].isLeveledCell  )
                // if (currentCell.HasBottomNeighbor() && !currentCell.isLeveledCell && !currentCell.layeredNeighbor[0].isLeveledRampCell)
                if (!currentCell.isLeveledCell && (currentCell.HasBottomNeighbor() == false || currentCell.GetBottomNeighbor().isLeveledRampCell == false))
                {
                    if (available.Contains(currentCell) == false) available.Add(currentCell);
                }
            }
            return available;
        }

        public static bool SelectAndAssignNext(
            HexagonCell cell,
            List<HexagonTileCore> prefabsList,
            TileContext tileContext,
            HexagonSocketDirectory socketDirectory,
            bool isWalledEdge,
            bool logIncompatibilities = false,
            bool ignoreFailures = true,
            bool allowInvertedTiles = true
        )
        {
            // Debug.LogError("prefabsList: " + prefabsList.Count);

            (HexagonTileCore nextTile, List<int[]> rotations) = WFCUtilities.SelectNextTile(cell, prefabsList, allowInvertedTiles, isWalledEdge, TileContext.Micro, socketDirectory, logIncompatibilities);
            bool assigned = WFCUtilities.AssignTileToCell(cell, nextTile, rotations, ignoreFailures);
            return assigned;
        }

        public static bool SelectAndAssignNext(
            HexagonCell cell,
            List<HexagonTileCore> prefabsList,
            List<HexagonCell> allAssignedCellsInOrder,
            TileContext tileContext,
            HexagonSocketDirectory socketDirectory,
            bool isWalledEdge,
            bool logIncompatibilities = false,
            bool ignoreFailures = true,
            bool allowInvertedTiles = true
        )
        {
            (HexagonTileCore nextTile, List<int[]> rotations) = WFCUtilities.SelectNextTile(cell, prefabsList, allowInvertedTiles, isWalledEdge, TileContext.Micro, socketDirectory, logIncompatibilities);
            bool assigned = WFCUtilities.AssignTileToCell(cell, nextTile, rotations, ignoreFailures);
            if (assigned) allAssignedCellsInOrder.Add(cell);
            return assigned;
        }

        public static void CollapseCellAndPropagate(HexagonCell currentCell, List<HexagonTileCore> tilePrefabs, List<HexagonTileCore> tilePrefabs_Edgable, WFC_CellNeighborPropagation neighborPropagation, TileContext tileContext, HexagonSocketDirectory socketDirectory, bool isWalledEdge, bool logIncompatibilities = false, bool ignoreFailures = true, bool allowInvertedTiles = true)
        {
            bool assigned = currentCell.IsAssigned() ? true : SelectAndAssignNext(currentCell, (currentCell.isEdgeCell ? tilePrefabs_Edgable : tilePrefabs), tileContext, socketDirectory, isWalledEdge, logIncompatibilities, ignoreFailures, allowInvertedTiles);
            if (assigned)
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
                            SelectAndAssignNext(neighbor, tilePrefabs_Edgable, tileContext, socketDirectory, isWalledEdge, logIncompatibilities, ignoreFailures, allowInvertedTiles);
                        }
                    }

                    if (includeInners)
                    {
                        List<HexagonCell> innerNeighbors = unassignedNeighbors.FindAll(n => n.isEdgeCell == false).OrderByDescending(n => n._neighbors.Count).ToList();

                        foreach (HexagonCell neighbor in innerNeighbors)
                        {
                            if (neighbor.IsAssigned()) continue;
                            SelectAndAssignNext(neighbor, tilePrefabs, tileContext, socketDirectory, isWalledEdge, logIncompatibilities, ignoreFailures, allowInvertedTiles);
                        }
                    }

                }
            }
        }


        public static void InstantiateTile(HexagonTileCore prefab, HexagonCell cell, Transform folder, List<GameObject> activeTilesList, bool disableEditor = true)
        {
            int rotation = cell.GetTileRotation();
            bool isInverted = cell.IsTileInverted();
            bool isTopLayer = cell.HasTopNeighbor() == false;

            Vector3 position = cell.transform.position;
            position.y += 0.2f;

            GameObject tileGO = GameObject.Instantiate(prefab.gameObject, position, Quaternion.identity);
            HexagonTileCore tileCore = tileGO.GetComponent<HexagonTileCore>();

            if (isInverted)
            {
                tileCore.InvertModel();
                tileGO.name = "INVERTED__" + tileGO.name;
            }

            if (isTopLayer && prefab.IsRoofable())
            {
                tileCore.SetModelRoofActive(true);
            }

            HexagonTileCore.RotateTile(tileGO.gameObject, rotation);

            activeTilesList.Add(tileGO);
            tileGO.transform.SetParent(folder);

            if (disableEditor) tileCore.SetEditorTools(false);
        }


        public static List<GameObject> CreateWFCFromMicroCellGridClusters(Dictionary<HexagonCellCluster, Dictionary<int, List<HexagonCell>>> allCellsByLayer_X4_ByCluster, HexagonTileCore tilePrefabs_MicroClusterParent, Transform parentFolder)
        {
            List<GameObject> parentTileGameObjects = new List<GameObject>();
            foreach (var kvp in allCellsByLayer_X4_ByCluster)
            {
                HexagonCellCluster cluster = kvp.Key;

                //TEMP
                if (cluster.clusterType == CellClusterType.Path) continue;

                GameObject parentTileGO = CreateWFCFromMicroCellGrid(cluster.cells[0], kvp.Value, tilePrefabs_MicroClusterParent, 12, parentFolder);
                parentTileGameObjects.Add(parentTileGO);
            }
            return parentTileGameObjects;
        }
        // public static List<GameObject> CreateWFCFromMicroCellGridClusters(List<HexagonCellCluster> clusters, HexagonTileCore tilePrefabs_MicroClusterParent, Transform parentFolder)
        // {
        //     List<GameObject> parentTileGameObjects = new List<GameObject>();
        //     foreach (HexagonCellCluster cluster in clusters)
        //     {
        //         parentTileGameObjects.Add(CreateWFCFromMicroCellGrid(cluster.cells[0], cluster.cellsByLayer, tilePrefabs_MicroClusterParent, 12, parentFolder));
        //     }
        //     return parentTileGameObjects;
        // }

        public static GameObject CreateWFCFromMicroCellGrid(HexagonCell parentCell, Dictionary<int, List<HexagonCell>> _allCellsByLayer, HexagonTileCore tilePrefabs_MicroClusterParent, int hostRadius, Transform parentFolder)
        {
            parentCell.SetTile(tilePrefabs_MicroClusterParent, 0);
            GameObject parentTileGO = GameObject.Instantiate(tilePrefabs_MicroClusterParent.gameObject, parentCell.transform.position, Quaternion.identity);

            HexagonTileCore parentTile = parentTileGO.GetComponent<HexagonTileCore>();
            parentTile.ShowSocketLabels(false);
            parentTile.SetIgnoreSocketLabelUpdates(true);

            IWFCSystem gridWFC = parentTileGO.GetComponent<IWFCSystem>();
            if (gridWFC == null) Debug.LogError("Missing WFC system component!");

            if (parentFolder != null) parentTileGO.transform.SetParent(parentFolder);

            List<HexagonCell> _allCells = new List<HexagonCell>();
            foreach (var kvp in _allCellsByLayer)
            {
                _allCells.AddRange(kvp.Value);
            }

            Debug.Log("CreateWFCFromMicroCellGrid - _allCells: " + _allCells.Count + "");


            gridWFC.SetRadius(hostRadius);
            gridWFC.AssignCells(_allCellsByLayer, _allCells);
            // Run WFC
            gridWFC.ExecuteWFC();

            return parentTileGO;
        }

        public static (HexagonCellManager, List<HexagonCell>) SetupMicroCellClusterFromHosts(List<HexagonCell> cellsToAssign, HexagonTileCore tilePrefabs_MicroClusterParent, int cellLayers, int cellLayerElevation, Transform parentFolder, bool useV2 = false)
        {
            if (cellsToAssign.Count < 1)
            {
                Debug.LogError("Not enough cells found");
                return (null, null);
            }
            HexagonCell parentCell = cellsToAssign[0];

            parentCell.SetTile(tilePrefabs_MicroClusterParent, 0);
            GameObject parentTileGO = GameObject.Instantiate(tilePrefabs_MicroClusterParent.gameObject, parentCell.transform.position, Quaternion.identity);

            HexagonCellManager parentCellManager = parentTileGO.GetComponent<HexagonCellManager>();
            parentCellManager.SetClusterParent();

            HexagonTileCore parentTile = parentTileGO.GetComponent<HexagonTileCore>();
            parentTile.ShowSocketLabels(false);
            parentTile.SetIgnoreSocketLabelUpdates(true);


            List<HexagonCell> children = new List<HexagonCell>();
            children.AddRange(cellsToAssign.FindAll(c => c != parentCell));

            parentCellManager.CreateMicroCellClusterPrototypesFromHosts(parentCell, children, cellLayers);

            if (parentFolder != null) parentTileGO.transform.SetParent(parentFolder);

            return (parentCellManager, children);
        }

        public static void InvertTile(HexagonTileCore tile)
        {
            Vector3 scale = tile.gameObject.transform.localScale;
            scale.z = -scale.z;
            tile.gameObject.transform.localScale = scale;
        }
        public static void InvertTile(GameObject tile)
        {
            Vector3 scale = tile.transform.localScale;
            scale.z = -scale.z;
            tile.transform.localScale = scale;
        }
        public static void InvertTile(GameObject tile, float positionZ)
        {
            Vector3 scale = tile.transform.localScale;
            scale.z = -scale.z;
            tile.transform.localScale = scale;
            tile.transform.localPosition = new Vector3(tile.transform.localPosition.x, tile.transform.localPosition.y, positionZ);
        }

        public static bool PassesLayerGroupingCompatibilityCheck(HexagonTileCore tileA, HexagonTileCore tileB, bool logIncompatibilities)
        {

            bool passed = (tileA.GetLayeredGrouping() == tileB.GetLayeredGrouping());
            if (passed == false && logIncompatibilities)
            {
                string tileName = tileA.gameObject.name;
                string neighborTileName = tileB.gameObject.name;
                Debug.LogError("Tile: [" + tileName + "] failed LayerGroupingCompatibilityCheck with bottom neighbor [" + neighborTileName + "]. \nMismatched groups: " + tileA.GetLayeredGrouping() + " / " + tileB.GetLayeredGrouping());
            }
            return passed;
        }

        public static bool PassesVariantIncompatibilityCheck(HexagonTileCore tileA, HexagonTileCore tileB)
        {
            TileCategory category_A = tileA.GetTileCategory();
            TileVariant variant_A = tileA.GetTileVariant();
            if (category_A == TileCategory.Unset || variant_A == TileVariant.Unset) return true;

            if (category_A != TileCategory.Unset && category_A == tileB.GetTileCategory() && variant_A != TileVariant.Unset)
            {
                if (variant_A != tileB.GetTileVariant()) return false;
            }
            return true;
        }

        public static bool AssignTileToCell(HexagonCell cell, HexagonTileCore tile, List<int[]> rotations_isInvertedTrue, bool ignoreFailures)
        {
            if (ignoreFailures && tile == null)
            {
                cell.Highlight(true);
                return false;
            }
            else
            {
                int[] selected = rotations_isInvertedTrue[UnityEngine.Random.Range(0, rotations_isInvertedTrue.Count)];
                bool shouldInvert = selected[1] == 1;

                cell.SetTile(tile, selected[0], shouldInvert);
                cell.Highlight(false);
                return true;
            }
        }

        public static (HexagonTileCore, List<int[]>) SelectNextTile(HexagonCell cell,
            List<HexagonTileCore> prefabsList,
            bool allowInvertedTiles,
            bool isWalledEdge,
            TileContext tileContext,
            HexagonSocketDirectory socketDirectory,
            bool logIncompatibilities
        )
        {

            if (prefabsList == null || prefabsList.Count == 0)
            {
                Debug.LogError("prefabsList is empty");
                return (null, null);
            }

            // Create a list of compatible tiles and their rotations
            List<(HexagonTileCore, List<int[]>)> compatibleTilesAndRotations = new List<(HexagonTileCore, List<int[]>)>();

            // Debug.LogError("prefabsList: " + prefabsList.Count);

            // Iterate through all tiles
            for (int i = 0; i < prefabsList.Count; i++)
            {
                HexagonTileCore currentTile = prefabsList[i];

                // Debug.LogError("currentTile: " + currentTile.gameObject.name);

                // if (cell.GetEdgeCellType() == EdgeCellType.Default && currentTile.IsGridEdgeCompatible() == false) continue;

                if (currentTile.ShouldExcludeCellStatus(cell.GetCellStatus())) continue;

                if (cell.IsEntry() && !currentTile.isEntrance) continue;

                if (cell.isPathCell && !currentTile.allowPathPlacement) continue;

                // if (currentTile.isLeveledTile && !cell.isLeveledCell) continue;

                // if (cell.isLeveledRampCell && !currentTile.isLeveledRamp) continue;

                if (currentTile.GetExcludeLayerState() != HexagonTileCore.ExcludeLayerState.Unset)
                {
                    if (currentTile.GetExcludeLayerState() == HexagonTileCore.ExcludeLayerState.BaseLayerOnly)
                    {
                        if (cell.IsGroundCell() == false) continue;
                    }
                    else if (currentTile.GetExcludeLayerState() == HexagonTileCore.ExcludeLayerState.TopLayerOnly)
                    {
                        if (cell.HasTopNeighbor()) continue;
                    }
                    else if (currentTile.GetExcludeLayerState() == HexagonTileCore.ExcludeLayerState.NoBaseLayer)
                    {
                        if (cell.IsGroundCell()) continue;
                    }
                }

                // if (currentTile.noGroundLayer && (cell.GetGridLayer() == 0 || cell.isLeveledGroundCell)) continue;
                // if (IsClusterCell && currentTile.GetInnerClusterSocketCount() != cell.GetNumberofNeighborsInCluster()) continue;

                List<int[]> compatibleTileRotations = WFCUtilities.GetCompatibleTileRotations(cell, currentTile, allowInvertedTiles, isWalledEdge, tileContext, socketDirectory, logIncompatibilities);

                if (compatibleTileRotations.Count > 0) compatibleTilesAndRotations.Add((currentTile, compatibleTileRotations));
            }

            // If there are no compatible tiles, return null
            if (compatibleTilesAndRotations.Count == 0)
            {
                Debug.LogError("compatibleTilesAndRotations.Count: " + compatibleTilesAndRotations.Count + ",  logIncompatibilities: " + logIncompatibilities);

                if (logIncompatibilities)
                {
                    if (cell.IsEntry())
                    {
                        Debug.LogError("No compatible tiles for Entry Cell: " + cell.id);
                    }
                    else if (cell.IsEdge())
                    {
                        Debug.LogError("No compatible tiles for Edge Cell: " + cell.id);
                    }
                    else if (cell.isLeveledEdge)
                    {
                        Debug.LogError("No compatible tiles for Leveled Edge Cell: " + cell.id);
                    }
                    else
                    {
                        Debug.LogError("No compatible tiles for cell: " + cell.id);
                    }
                }
                return (null, null);
            }

            // Select a random compatible tile and rotation
            int randomIndex = UnityEngine.Random.Range(0, compatibleTilesAndRotations.Count);
            return compatibleTilesAndRotations[randomIndex];
        }

        static public List<int[]> GetCompatibleTileRotations(HexagonCell currentCell, HexagonTileCore currentTile, bool allowInvertedTiles, bool isWalledEdge, TileContext tileContext, HexagonSocketDirectory socketDirectory, bool logIncompatibilities)
        {
            List<int[]> compatibleRotations = new List<int[]>();
            string tileName = currentTile.gameObject.name;

            // Check for Variant Incompatibility
            if (currentTile.GetTileVariant() != TileVariant.Unset)
            {
                foreach (HexagonCell neighbor in currentCell._neighbors)
                {
                    if (neighbor != null)
                    {
                        HexagonTileCore neighborTile = (HexagonTileCore)neighbor.GetTile();
                        if (neighborTile != null && WFCUtilities.PassesVariantIncompatibilityCheck(currentTile, neighborTile) == false)
                        {
                            // Debug.LogError("Tile: " + tileName + " not compatibile with neighbor tile: " + neighborTile.gameObject.name + ", failed at PassesVariantIncompatibilityCheck");
                            return compatibleRotations;
                        }
                    }
                }
            }

            if (allowInvertedTiles && currentTile.IsInvertable())
            {
                List<int[]> compatibleRotations_Inverted = CheckTileSocketCompatibility(currentCell, currentTile, true, isWalledEdge, tileContext, socketDirectory, logIncompatibilities);
                if (compatibleRotations_Inverted.Count > 0) compatibleRotations.AddRange(compatibleRotations_Inverted);
            }

            List<int[]> compatibleRotations_Uninverted = CheckTileSocketCompatibility(currentCell, currentTile, false, isWalledEdge, tileContext, socketDirectory, logIncompatibilities);
            if (compatibleRotations_Uninverted.Count > 0) compatibleRotations.AddRange(compatibleRotations_Uninverted);

            // Debug.Log("GetCompatibleTileRotations - Cell: " + currentCell.id + ", compatibleRotations: " + compatibleRotations.Count);
            return compatibleRotations;
        }


        static public List<int[]> CheckTileSocketCompatibility(HexagonCell currentCell, HexagonTileCore currentTile, bool inverted, bool isWalledEdge, TileContext tileContext, HexagonSocketDirectory socketDirectory, bool logIncompatibilities)
        {
            List<int[]> compatibleRotations = new List<int[]>();

            NeighborSideCornerSockets[] neighborTileCornerSocketsBySide = tileContext == TileContext.Default ? currentCell.GetSideNeighborTileSockets(isWalledEdge) : currentCell.GetSideNeighborTileSockets(isWalledEdge, true);
            NeighborLayerCornerSockets[] layeredNeighborTileCornerSockets = currentCell.GetLayeredNeighborTileSockets(tileContext);

            bool checkLayerNeighbors = currentCell.IsGroundCell() == false;
            bool[,] compatibilityMatrix = socketDirectory.GetCompatibilityMatrix();

            // Check every rotation
            for (int rotation = 0; rotation < 6; rotation++)
            {
                bool compatibile = true;

                // Check Layered Neighbors First
                if (checkLayerNeighbors)
                {
                    HexagonTileCore neighborTile = currentCell.GetBottomNeighbor()?.GetCurrentTile();

                    if (neighborTile != null)
                    {
                        // Check for Variant Incompatibility
                        compatibile = PassesLayerGroupingCompatibilityCheck(currentTile, neighborTile, logIncompatibilities);
                        if (compatibile == false) break;
                    }

                    // For now just check bottom neighbor's top against current tile's bottom
                    int[] currentTileBottomSockets = currentTile.GetRotatedLayerCornerSockets(false, rotation, inverted);
                    for (int i = 0; i < layeredNeighborTileCornerSockets[0].corners.Length; i++)
                    {
                        if (!compatibilityMatrix[currentTileBottomSockets[i], layeredNeighborTileCornerSockets[0].corners[i]])
                        {
                            if (logIncompatibilities)
                            {
                                string tileName = currentTile.gameObject.name;
                                string neighborTileName = neighborTile?.gameObject.name;
                                string[] sockets = socketDirectory.sockets;

                                string currentTileLog = "Incoming Tile: [" + tileName + "], CornerSocket: " + sockets[currentTileBottomSockets[i]] + ", Rotation: " + rotation;
                                string neighborTileLog = "Bottom Tile: [" + neighborTileName + "], CornerSocket: " + sockets[layeredNeighborTileCornerSockets[0].corners[i]];

                                Debug.LogError("Cell: " + currentCell.id + ", Tile: [" + tileName + "] is INCOMPATIBLE with bottom neighbor [" + neighborTileName + "] \n" + currentTileLog + "\n" + neighborTileLog);
                                // Debug.LogError(tileName + " Not compatibile with bottom layer. currentTileBottomSocket: " + currentTileBottomSockets[i] + ", corner: " + (HexagonCorner)i);
                            }
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
                        HexagonTileCore neighborTile = currentCell.neighborsBySide[side]?.GetCurrentTile();
                        compatibile = WFCUtilities.IsTileCompatibleOnSideAndRotation(neighborTileCornerSocketsBySide, currentTile, currentCell, side, rotation, neighborTile, inverted, socketDirectory, logIncompatibilities);
                        if (!compatibile) break;
                    }
                }

                if (compatibile)
                {
                    int[] rotation_isInverted = new int[2] { rotation, inverted ? 1 : 0 };
                    compatibleRotations.Add(rotation_isInverted);
                }
            }

            return compatibleRotations;
        }


        public static bool IsTileCompatibleOnSideAndRotation(NeighborSideCornerSockets[] neighborTileCornerSocketsBySide, HexagonTileCore currentTile, HexagonCell currentCell, int side, int rotation, HexagonTileCore neighborTile, bool inverted, HexagonSocketDirectory socketDirectory, bool logIncompatibilities)
        {
            string tileName = currentTile.gameObject.name;
            string neighborTileName = neighborTile?.gameObject.name;
            string[] sockets = socketDirectory.sockets;
            bool[,] compatibilityMatrix = socketDirectory.GetCompatibilityMatrix();

            bool compatibile = true;

            NeighborSideCornerSockets neighborSide = neighborTileCornerSocketsBySide[side];
            (int[] currentTileSideBottomSockets, int[] currentTileSideTopSockets) = currentTile.GetRotatedCornerSocketsBySide((HexagonSide)side, rotation, inverted);

            // Check Bottom and Top Corners
            for (var sideLevel = 0; sideLevel < 2; sideLevel++)
            {
                bool isBottom = sideLevel == 0;
                string level = isBottom ? "BTM" : "TOP";

                int[] currentTileSideSockets = isBottom ? currentTileSideBottomSockets : currentTileSideTopSockets;
                int[] neighborTileCornerSockets = isBottom ? neighborSide.bottomCorners : neighborSide.topCorners;

                for (var i = 0; i < 2; i++)
                {
                    // corner A checks cornerB of neighbor etc
                    int currentTileCornerSocket = currentTileSideSockets[i];
                    int neighborTileCornerSocket = neighborTileCornerSockets[(i + 1) % 2];

                    if (!compatibilityMatrix[currentTileCornerSocket, neighborTileCornerSocket])
                    {
                        if (logIncompatibilities)
                        {
                            string currentTileLog = "Incoming Tile: [" + tileName + "], " + level + " CornerSocket: " + sockets[currentTileCornerSocket] + ", rotation: " + rotation;
                            string neighborTileLog = "Neighbor Tile: [" + neighborTileName + "], " + level + " CornerSocket: " + sockets[neighborTileCornerSocket];

                            Debug.LogError("Cell: " + currentCell.id + ", Tile: [" + tileName + "] is INCOMPATIBLE with [" + neighborTileName + "] on side: " + (HexagonSide)side + ". \n" + currentTileLog + "\n" + neighborTileLog);
                        }

                        compatibile = false;
                        return false;
                    }
                }
            }

            return compatibile;
        }

        public static bool IsTileCompatibleOnSideAndRotation(HexagonCell currentCell, HexagonTileCore currentTile, int currentRotatedSide, HexagonTileCore neighborTile, int neighborRotatedSide, HexagonSocketDirectory socketDirectory, bool isWalledEdge = true)
        {
            string tileName = currentTile.gameObject.name;
            string neighborTileName = neighborTile.gameObject.name;
            string[] sockets = socketDirectory.sockets;
            bool[,] compatibilityMatrix = socketDirectory.GetCompatibilityMatrix();

            bool compatibile = true;

            (int[] currentTileSideBottomSockets, int[] currentTileSideTopSockets) = currentTile.GetCornerSocketsBySide((HexagonSide)currentRotatedSide);

            (int[] neighborSideBottomSockets, int[] neighborSideTopSockets) = neighborTile.GetCornerSocketsBySide((HexagonSide)neighborRotatedSide);

            (HexagonCorner currentCornerA, HexagonCorner currentCornerB) = HexCoreUtil.GetCornersFromSide((HexagonSide)currentRotatedSide);

            (HexagonCorner neighborCornerA, HexagonCorner neighborCornerB) = HexCoreUtil.GetCornersFromSide((HexagonSide)neighborRotatedSide);

            // Check Bottom Corners
            for (var i = 0; i < 2; i++)
            {
                // corner A checks cornerB of neighbor etc
                int currentTileCornerSocket = currentTileSideBottomSockets[i];
                int neighborTileCornerSocket = neighborSideBottomSockets[(i + 1) % 2];

                if (!compatibilityMatrix[currentTileCornerSocket, neighborTileCornerSocket])
                {
                    HexagonCorner currentCorner = i == 0 ? currentCornerA : currentCornerB;
                    HexagonCorner neighborCorner = i == 0 ? neighborCornerB : neighborCornerA;

                    Debug.LogError(tileName + " not compatibile with side of " + neighborTileName + ". \nCurrent Tile: [" + tileName + "] Side: " + (HexagonSide)currentRotatedSide + ", Corner: BTM - " + currentCorner + ",  CornerSocket: " + sockets[currentTileCornerSocket] + "\nNeighbor Tile: [" + neighborTileName + "] Side: " + (HexagonSide)neighborRotatedSide + ", Corner: BTM -" + neighborCorner + ", CornerSocket: " + sockets[neighborTileCornerSocket]);
                    compatibile = false;
                    return false;
                }
            }

            // Check Top Corners
            for (var i = 0; i < 2; i++)
            {
                // corner A checks cornerB of neighbor etc
                int currentTileCornerSocket = currentTileSideTopSockets[i];
                int neighborTileCornerSocket = neighborSideTopSockets[(i + 1) % 2];

                if (!compatibilityMatrix[currentTileCornerSocket, neighborTileCornerSocket])
                {
                    HexagonCorner currentCorner = i == 0 ? currentCornerA : currentCornerB;
                    HexagonCorner neighborCorner = i == 0 ? neighborCornerB : neighborCornerA;

                    Debug.LogError(tileName + " not compatibile with side of " + neighborTileName + ". \nCurrent Tile: [" + tileName + "] Side: " + (HexagonSide)currentRotatedSide + ", Corner: TOP - " + currentCorner + ",  CornerSocket: " + sockets[currentTileCornerSocket] + "\nNeighbor Tile: [" + neighborTileName + "] Side: " + (HexagonSide)neighborRotatedSide + ", Corner: TOP -" + neighborCorner + ", CornerSocket: " + sockets[neighborTileCornerSocket]);
                    compatibile = false;
                    return false;
                }
            }

            return compatibile;
        }

        public static bool IsTileCompatibleOnLayerAndRotation(HexagonCell currentCell, HexagonTileCore currentTile, bool[,] compatibilityMatrix, int rotation)
        {
            string tileName = currentTile.gameObject.name;

            bool compatibile = true;

            NeighborLayerCornerSockets[] layeredNeighborTileCornerSockets = currentCell.GetLayeredNeighborTileSockets(TileContext.Micro);

            // For now just check bottom neighbor's top against current tile's bottom
            int[] currentTileBottomSockets = currentTile.GetRotatedLayerCornerSockets(false, rotation, false);
            for (int i = 0; i < layeredNeighborTileCornerSockets[0].corners.Length; i++)
            {
                int currentTileSocket = currentTileBottomSockets[i];
                int layeredNeighborTileCornerSocket = layeredNeighborTileCornerSockets[0].corners[i];

                if (!compatibilityMatrix[currentTileSocket, layeredNeighborTileCornerSocket])
                {
                    Debug.LogError(tileName + " Not compatibile with bottom layer. \ncurrentTileSocket: " + currentTileSocket + ", corner: " + (HexagonCorner)i + ", layeredNeighborTileCornerSocket: " + layeredNeighborTileCornerSocket);
                    compatibile = false;
                    break;
                }
            }

            return compatibile;
        }

        public static bool IsTileCompatibleOnLayerAndRotation(HexagonCell currentCell, HexagonTileCore currentTile, int currentCellRotation, HexagonTileCore bottomNeighborTile, int bottomNeighborRotation, HexagonSocketDirectory socketDirectory)
        {
            bool[,] compatibilityMatrix = socketDirectory.GetCompatibilityMatrix();
            string[] sockets = socketDirectory.sockets;

            string currentTileName = currentTile.gameObject.name;
            string neighborTileName = bottomNeighborTile.gameObject.name;

            bool compatibile = true;

            // For now just check bottom neighbor's top against current tile's bottom
            NeighborLayerCornerSockets neighborTopCornerSockets = new NeighborLayerCornerSockets();
            neighborTopCornerSockets.corners = new int[6];
            neighborTopCornerSockets.corners = bottomNeighborTile.GetRotatedLayerCornerSockets(true, bottomNeighborRotation, false);

            int[] currentTileBottomSockets = currentTile.GetRotatedLayerCornerSockets(false, currentCellRotation, false);

            for (int i = 0; i < neighborTopCornerSockets.corners.Length; i++)
            {
                int currentTileSocket = currentTileBottomSockets[i];
                int layeredNeighborTileCornerSocket = neighborTopCornerSockets.corners[i];

                if (!compatibilityMatrix[currentTileSocket, layeredNeighborTileCornerSocket])
                {
                    Debug.LogError(currentTileName + " not compatibile with bottom layer " + neighborTileName + ". \nCurrent Tile Corner: " + (HexagonCorner)i + ",  currentTileSocket: " + sockets[currentTileSocket] + "\nNeighbor Tile Corner: " + (HexagonCorner)i + ", layeredNeighborTileCornerSocket: " + sockets[layeredNeighborTileCornerSocket]);
                    compatibile = false;
                    break;
                }
            }
            return compatibile;
        }




    }
}