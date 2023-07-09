using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProceduralBase;

namespace WFCSystem
{
    static class WFCUtilities_V2
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


        public static bool HasUnassignedCells(List<HexagonCellPrototype> allCells) => allCells.Any(c => c.IsWFC_Assigned() == false);

        public static List<HexagonCellPrototype> SelectRandomCells(List<HexagonCellPrototype> allCells, int min = 2, int max = 7)
        {
            List<HexagonCellPrototype> availableCells = new List<HexagonCellPrototype>();
            availableCells.AddRange(allCells);

            int numCells = UnityEngine.Random.Range(min, max + 1);
            List<HexagonCellPrototype> selectedCells = new List<HexagonCellPrototype>();

            for (int i = 0; i < numCells; i++)
            {
                if (availableCells.Count == 0) break;
                int index = UnityEngine.Random.Range(0, availableCells.Count);
                HexagonCellPrototype cell = availableCells[index];
                selectedCells.Add(cell);
                availableCells.RemoveAt(index);
            }

            return selectedCells;
        }

        public static List<HexagonCellPrototype> GetAvailableCellsForNextLayer(List<HexagonCellPrototype> allLayerCells)
        {
            List<HexagonCellPrototype> available = new List<HexagonCellPrototype>();

            foreach (HexagonCellPrototype currentCell in allLayerCells)
            {
                if (currentCell.IsAssigned() || currentCell.isGroundRamp) continue;

                if (!currentCell.isLeveledCell && (currentCell.HasBottomNeighbor() == false || currentCell.GetBottomNeighbor().isLeveledRampCell == false))
                {
                    if (available.Contains(currentCell) == false) available.Add(currentCell);
                }
            }
            return available;
        }

        public static bool SelectAndAssignNext(
            HexagonCellPrototype cell,
            List<HexagonTileCore> prefabsList,
            TileContext tileContext,
            HexagonSocketDirectory socketDirectory,
            bool isWalledEdge,
            bool logIncompatibilities = false,
            bool ignoreFailures = true,
            bool allowInvertedTiles = true
        )
        {
            (HexagonTileCore nextTile, List<int[]> rotations) = SelectNextTile(cell, prefabsList, allowInvertedTiles, isWalledEdge, TileContext.Micro, socketDirectory, logIncompatibilities);
            bool assigned = AssignTileToCell(cell, nextTile, rotations, ignoreFailures);
            return assigned;
        }

        public static bool SelectAndAssignNext(
            HexagonCellPrototype cell,
            List<HexagonTileCore> prefabsList,
            List<HexagonCellPrototype> allAssignedCellsInOrder,
            TileContext tileContext,
            HexagonSocketDirectory socketDirectory,
            bool isWalledEdge,
            bool logIncompatibilities = false,
            bool ignoreFailures = true,
            bool allowInvertedTiles = true
        )
        {
            (HexagonTileCore nextTile, List<int[]> rotations) = SelectNextTile(cell, prefabsList, allowInvertedTiles, isWalledEdge, TileContext.Micro, socketDirectory, logIncompatibilities);
            bool assigned = AssignTileToCell(cell, nextTile, rotations, ignoreFailures);
            if (assigned) allAssignedCellsInOrder.Add(cell);
            return assigned;
        }


        public static void CollapseCellAndPropagate(
            HexagonCellPrototype currentCell,
            List<HexagonTileCore> tilePrefabs,
            List<HexagonTileCore> tilePrefabs_Edgable,
            WFC_CellNeighborPropagation neighborPropagation,
            TileContext tileContext,
            HexagonSocketDirectory socketDirectory,
            bool isWalledEdge,
            bool logIncompatibilities = false,
            bool ignoreFailures = true,
            bool allowInvertedTiles = true
        )
        {
            bool assigned = currentCell.IsWFC_Assigned() ? true : SelectAndAssignNext(currentCell, (currentCell.IsEdge() ? tilePrefabs_Edgable : tilePrefabs), tileContext, socketDirectory, isWalledEdge, logIncompatibilities, ignoreFailures, allowInvertedTiles);
            if (assigned)
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
                    bool includeInners = (currentCell.IsEdge() == false || neighborPropagation == WFC_CellNeighborPropagation.Edges_Inners_Include_Layers || neighborPropagation == WFC_CellNeighborPropagation.Edges_Inners_No_Layers);

                    List<HexagonCellPrototype> edgeNeighbors = unassignedNeighbors.FindAll(n => n.isEdgeCell).OrderBy(n => n.GetEdgeCellType()).ToList();
                    if (edgeNeighbors.Count > 0)
                    {
                        foreach (HexagonCellPrototype neighbor in edgeNeighbors)
                        {
                            if (neighbor.IsWFC_Assigned()) continue;
                            SelectAndAssignNext(neighbor, tilePrefabs_Edgable, tileContext, socketDirectory, isWalledEdge, logIncompatibilities, ignoreFailures, allowInvertedTiles);
                        }
                    }

                    if (includeInners)
                    {
                        List<HexagonCellPrototype> innerNeighbors = unassignedNeighbors.FindAll(n => n.isEdgeCell == false).OrderByDescending(n => n.neighbors.Count).ToList();

                        foreach (HexagonCellPrototype neighbor in innerNeighbors)
                        {
                            if (neighbor.IsWFC_Assigned()) continue;
                            SelectAndAssignNext(neighbor, tilePrefabs, tileContext, socketDirectory, isWalledEdge, logIncompatibilities, ignoreFailures, allowInvertedTiles);
                        }
                    }

                }
            }
        }



        public static void CollapseEdgeCells(
            Dictionary<int, List<HexagonCellPrototype>> allCellsByLayer,
            List<HexagonCellPrototype> edgeCells_Grid,
            List<HexagonTileCore> tilePrefabs,
            List<HexagonTileCore> tilePrefabs_Edgable,
            List<HexagonTileCore> tilePrefabs_TopLayer,
            List<HexagonCellPrototype> allAssignedCellsInOrder,
            WFC_CellNeighborPropagation neighborPropagation,
            TileContext tileContext,
            HexagonSocketDirectory socketDirectory,
            bool restrictEntryTiles,
            bool isWalledEdge,
            bool logIncompatibilities = false,
            bool ignoreFailures = true,
            bool allowInvertedTiles = true
        )
        {
            Debug.Log("Collapsing Edge Cells...");

            List<HexagonTileCore> tilePrefabs_Edgable_Formatted = restrictEntryTiles ? tilePrefabs_Edgable.FindAll(t => t.isEntrance == false) : tilePrefabs_Edgable;
            List<HexagonTileCore> tilePrefabs_Edgable_No_Entry_Top_Only = tilePrefabs_TopLayer.FindAll(t => t.isEdgeable);

            if (tilePrefabs_Edgable_No_Entry_Top_Only.Count == 0) tilePrefabs_Edgable_No_Entry_Top_Only = tilePrefabs_Edgable_Formatted;

            bool includeLayers = (neighborPropagation == WFC_CellNeighborPropagation.Edges_Only_Include_Layers || neighborPropagation == WFC_CellNeighborPropagation.Edges_Inners_Include_Layers);

            foreach (var kvp in allCellsByLayer)
            {
                int currentLayer = kvp.Key;
                // foreach (int currentLayer in allLayers)
                // {
                List<HexagonCellPrototype> layerEdgeCells = edgeCells_Grid.FindAll(e => e.GetGridLayer() == currentLayer).OrderByDescending(e => e.neighbors.Count).ToList();

                foreach (HexagonCellPrototype edgeCell in layerEdgeCells)
                {
                    if (edgeCell.IsAssigned() == false)
                    {
                        SelectAndAssignNext(
                            edgeCell,
                            edgeCell.HasTopNeighbor() ? tilePrefabs_Edgable_Formatted : tilePrefabs_Edgable_No_Entry_Top_Only,
                            allAssignedCellsInOrder,
                            TileContext.Micro,
                            socketDirectory,
                            isWalledEdge,
                            logIncompatibilities,
                            ignoreFailures,
                            allowInvertedTiles
                        );
                    }

                    CollapseCellAndPropagate(
                        edgeCell,
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
        }


        public static void InstantiateTile(HexagonTileCore prefab, IHexCell cell, Transform folder, List<GameObject> activeTilesList, bool disableEditor = true)
        {
            int rotation = cell.GetTileRotation();
            bool isInverted = cell.IsTileInverted();
            bool isTopLayer = cell.HasTopNeighbor() == false;

            Vector3 position = cell.GetPosition();
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

            if (activeTilesList != null) activeTilesList.Add(tileGO);
            tileGO.transform.SetParent(folder);

            if (disableEditor) tileCore.SetEditorTools(false);
        }


        public static List<GameObject> Instantiate_WFC_FromClusters(
            Dictionary<HexagonCellCluster, Dictionary<int, List<HexagonCellPrototype>>> cellsByLayer_ByCluster,
            HexagonTileCore wfcTilePrefab,
            Transform parentFolder
        )
        {
            List<GameObject> parentTileGameObjects = new List<GameObject>();
            foreach (var kvp in cellsByLayer_ByCluster)
            {
                HexagonCellCluster cluster = kvp.Key;

                //TEMP
                if (cluster.clusterType == CellClusterType.Path) continue;

                GameObject parentTileGO = Instantiate_WFC_FromCellGrid(cluster.cells[0], kvp.Value, wfcTilePrefab, cluster.cells[0].GetSize(), parentFolder);
                parentTileGameObjects.Add(parentTileGO);
            }
            return parentTileGameObjects;
        }

        public static GameObject Instantiate_WFC_FromCellGrid(
            IHexCell parentCell,
            Dictionary<int,
            List<HexagonCellPrototype>> cellsByLayer,
            HexagonTileCore wfcTilePrefab,
            int hostRadius,
            Transform parentFolder
        )
        {
            parentCell.SetTile(wfcTilePrefab, 0);

            GameObject wfcTileGameObject = GameObject.Instantiate(wfcTilePrefab.gameObject, parentCell.GetPosition(), Quaternion.identity);

            HexagonTileCore parentTile = wfcTileGameObject.GetComponent<HexagonTileCore>();
            parentTile.ShowSocketLabels(false);
            parentTile.SetIgnoreSocketLabelUpdates(true);

            IWFCSystem wfc = wfcTileGameObject.GetComponent<IWFCSystem>();
            if (wfc == null) Debug.LogError("Missing WFC system component!");

            if (parentFolder != null) wfcTileGameObject.transform.SetParent(parentFolder);

            List<HexagonCellPrototype> allCells = new List<HexagonCellPrototype>();
            foreach (var kvp in cellsByLayer)
            {
                allCells.AddRange(kvp.Value);
            }

            Debug.Log("Instantiate_WFC_FromCellGrid - allCells: " + allCells.Count + "");

            wfc.SetRadius(hostRadius);
            wfc.AssignCells(cellsByLayer, allCells);
            // Run WFC
            wfc.ExecuteWFC();

            return wfcTileGameObject;
        }

        public static (HexagonCellManager, List<HexagonCellPrototype>) Instantiate_MicroCellClusterFromHosts(
            List<HexagonCellPrototype> cellsToAssign,
            HexagonTileCore tilePrefabs_MicroClusterParent,
            int cellLayers,
            int cellLayerElevation,
            Transform parentFolder,
            bool useV2 = false)
        {
            if (cellsToAssign.Count < 1)
            {
                Debug.LogError("Not enough cells found");
                return (null, null);
            }
            HexagonCellPrototype parentCell = cellsToAssign[0];

            parentCell.SetTile(tilePrefabs_MicroClusterParent, 0);
            GameObject parentTileGO = GameObject.Instantiate(tilePrefabs_MicroClusterParent.gameObject, parentCell.center, Quaternion.identity);

            HexagonCellManager parentCellManager = parentTileGO.GetComponent<HexagonCellManager>();
            parentCellManager.SetClusterParent();

            HexagonTileCore parentTile = parentTileGO.GetComponent<HexagonTileCore>();
            parentTile.ShowSocketLabels(false);
            parentTile.SetIgnoreSocketLabelUpdates(true);


            List<HexagonCellPrototype> children = new List<HexagonCellPrototype>();
            children.AddRange(cellsToAssign.FindAll(c => c != parentCell));

            // parentCellManager.CreateMicroCellClusterPrototypesFromHosts(parentCell, children, cellLayers);

            // if (parentFolder != null) parentTileGO.transform.SetParent(parentFolder);

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

        public static bool AssignTileToCell(IHexCell cell, HexagonTileCore tile, List<int[]> rotations_isInvertedTrue, bool ignoreFailures)
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

        public static (HexagonTileCore, List<int[]>) SelectNextTile(
            HexagonCellPrototype cell,
            List<HexagonTileCore> prefabsList,
            bool allowInvertedTiles,
            bool isWalledEdge,
            TileContext tileContext,
            HexagonSocketDirectory socketDirectory,
            bool logIncompatibilities)
        {

            if (prefabsList == null || prefabsList.Count == 0)
            {
                Debug.LogError("prefabsList is empty");
                return (null, null);
            }

            // Create a list of compatible tiles and their rotations
            List<(HexagonTileCore, List<int[]>)> compatibleTilesAndRotations = new List<(HexagonTileCore, List<int[]>)>();

            // Iterate through all tiles
            for (int i = 0; i < prefabsList.Count; i++)
            {
                HexagonTileCore currentTile = prefabsList[i];

                // if (cell.GetEdgeCellType() == EdgeCellType.Default && currentTile.IsGridEdgeCompatible() == false) continue;
                if (currentTile.ShouldExcludeCellStatus(cell.GetCellStatus())) continue;

                if (cell.IsEntry() && !currentTile.isEntrance) continue;

                if (cell.IsPath() && !currentTile.allowPathPlacement) continue;

                // if (currentTile.isLeveledTile && !cell.isLeveledCell) continue;

                // if (cell.isLeveledRampCell && !currentTile.isLeveledRamp) continue;

                if (currentTile.GetExcludeLayerState() != HexagonTileCore.ExcludeLayerState.Unset)
                {
                    if (currentTile.GetExcludeLayerState() == HexagonTileCore.ExcludeLayerState.BaseLayerOnly)
                    {
                        if (cell.IsGround() == false) continue;
                    }
                    else if (currentTile.GetExcludeLayerState() == HexagonTileCore.ExcludeLayerState.TopLayerOnly)
                    {
                        if (cell.HasTopNeighbor()) continue;
                    }
                    else if (currentTile.GetExcludeLayerState() == HexagonTileCore.ExcludeLayerState.NoBaseLayer)
                    {
                        if (cell.IsGround()) continue;
                    }
                }

                List<int[]> compatibleTileRotations = GetCompatibleTileRotations(cell, currentTile, allowInvertedTiles, isWalledEdge, tileContext, socketDirectory, logIncompatibilities);

                if (compatibleTileRotations.Count > 0) compatibleTilesAndRotations.Add((currentTile, compatibleTileRotations));

            }
            // If there are no compatible tiles, return null
            if (compatibleTilesAndRotations.Count == 0)
            {
                if (logIncompatibilities)
                {
                    if (cell.IsEntry())
                    {
                        Debug.LogError("No compatible tiles for Entry Cell: " + cell.LogStats());
                    }
                    else if (cell.IsEdge())
                    {
                        Debug.LogError("No compatible tiles for Edge Cell: " + cell.LogStats());
                    }
                    else if (cell.isLeveledEdge)
                    {
                        Debug.LogError("No compatible tiles for Leveled Edge Cell: " + cell.LogStats());
                    }
                    else
                    {
                        Debug.Log("No compatible tiles for cell: " + cell.LogStats());
                    }
                }
                return (null, null);
            }

            // Select a random compatible tile and rotation
            int randomIndex = UnityEngine.Random.Range(0, compatibleTilesAndRotations.Count);
            return compatibleTilesAndRotations[randomIndex];
        }

        static public List<int[]> GetCompatibleTileRotations(HexagonCellPrototype currentCell, HexagonTileCore currentTile, bool allowInvertedTiles, bool isWalledEdge, TileContext tileContext, HexagonSocketDirectory socketDirectory, bool logIncompatibilities)
        {
            List<int[]> compatibleRotations = new List<int[]>();
            string tileName = currentTile.gameObject.name;

            // Check for Variant Incompatibility
            if (currentTile.GetTileVariant() != TileVariant.Unset)
            {
                foreach (HexagonCellPrototype neighbor in currentCell.neighbors)
                {
                    if (neighbor != null)
                    {
                        HexagonTileCore neighborTile = (HexagonTileCore)neighbor.GetTile();
                        if (neighborTile != null && PassesVariantIncompatibilityCheck(currentTile, neighborTile) == false)
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


        static public List<int[]> CheckTileSocketCompatibility(
            HexagonCellPrototype currentCell,
            HexagonTileCore currentTile,
            bool inverted,
            bool isWalledEdge,
            TileContext tileContext,
            HexagonSocketDirectory socketDirectory,
            bool logIncompatibilities
        )
        {
            List<int[]> compatibleRotations = new List<int[]>();

            NeighborSideCornerSockets[] neighborTileCornerSocketsBySide = tileContext == TileContext.Default ?
                            currentCell.GetSideNeighborTileSockets(isWalledEdge)
                            : currentCell.GetSideNeighborTileSockets(isWalledEdge, true);

            NeighborLayerCornerSockets[] layeredNeighborTileCornerSockets = currentCell.GetLayeredNeighborTileSockets(tileContext);

            bool checkLayerNeighbors = currentCell.IsGround() == false;
            bool[,] compatibilityMatrix = socketDirectory.GetCompatibilityMatrix();

            // Check every rotation
            for (int rotation = 0; rotation < 6; rotation++)
            {
                bool compatibile = true;

                // Check Layered Neighbors First
                if (checkLayerNeighbors)
                {
                    HexagonCellPrototype neighborCell = currentCell.GetBottomNeighbor();
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
                                string neighborTileName = (neighborTile != null) ? neighborTile.gameObject.name : "EMPTY";
                                string[] sockets = socketDirectory.sockets;

                                string currentTileLog = "Incoming: [" + tileName + "], Socket: " + sockets[currentTileBottomSockets[i]] + ", Rotation: " + rotation + ", current Cell: " + currentCell.LogStats();
                                string neighborTileLog = "Bottom: [" + neighborTileName + "], Socket: " + sockets[layeredNeighborTileCornerSockets[0].corners[i]] + ", neighbor Cell: " + neighborCell?.LogStats();

                                Debug.LogError("Current Tile: [" + tileName + "] is INCOMPATIBLE with bottom neighbor [" + neighborTileName + "] \n" + currentTileLog + "\n" + neighborTileLog);
                                // Debug.LogError("Cell: " + currentCell.id + ", Tile: [" + tileName + "] is INCOMPATIBLE with bottom neighbor [" + neighborTileName + "] \n" + currentTileLog + "\n" + neighborTileLog);
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
                        HexagonCellPrototype neighborCell = currentCell.neighborsBySide[side];
                        compatibile = IsTileCompatibleOnSideAndRotation(neighborTileCornerSocketsBySide, currentTile, currentCell, neighborCell, side, rotation, inverted, socketDirectory, logIncompatibilities);
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


        public static bool IsTileCompatibleOnSideAndRotation(
            NeighborSideCornerSockets[] neighborTileCornerSocketsBySide,
            HexagonTileCore currentTile,
            HexagonCellPrototype currentCell,
            HexagonCellPrototype neighborCell,
            int side,
            int rotation,
            bool inverted,
            HexagonSocketDirectory socketDirectory,
            bool logIncompatibilities
        )
        {
            string tileName = currentTile.gameObject.name;
            HexagonTileCore neighborTile = neighborCell?.GetCurrentTile();

            string neighborCellStats = (neighborCell != null) ? neighborCell.LogStats() : "NULL";
            string neighborTileName = (neighborTile != null) ? neighborTile.gameObject.name : "EMPTY";
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
                            string currentTileLog = "Incoming: [" + tileName + "], " + level + " Socket: " + sockets[currentTileCornerSocket] + ", Rotation: " + rotation + ", current Cell: " + currentCell.LogStats();
                            string neighborTileLog = "Neighbor: [" + neighborTileName + "], " + level + " Socket: " + sockets[neighborTileCornerSocket] + ", neighbor Cell: " + neighborCellStats;

                            Debug.LogError("Current Tile: [" + tileName + "] is INCOMPATIBLE with [" + neighborTileName + "] on Side: " + (HexagonSide)side + ". \n" + currentTileLog + "\n" + neighborTileLog);
                        }
                        compatibile = false;
                        return false;
                    }
                }
            }
            return compatibile;
        }

        public static bool IsTileCompatibleOnSideAndRotation(HexagonCellPrototype currentCell,
            HexagonTileCore currentTile,
            int currentRotatedSide,
            HexagonTileCore neighborTile,
            int neighborRotatedSide,
            HexagonSocketDirectory socketDirectory,
            bool isWalledEdge = true
        )
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

        public static bool IsTileCompatibleOnLayerAndRotation(HexagonCellPrototype currentCell,
            HexagonTileCore currentTile,
            bool[,] compatibilityMatrix,
            int rotation
        )
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

        public static bool IsTileCompatibleOnLayerAndRotation(
            HexagonCellPrototype currentCell,
            HexagonTileCore currentTile,
            int currentCellRotation,
            HexagonTileCore bottomNeighborTile,
            int bottomNeighborRotation,
            HexagonSocketDirectory socketDirectory
        )
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

        public static void ShuffleCells(List<HexagonCellPrototype> cells)
        {
            int n = cells.Count;
            for (int i = 0; i < n; i++)
            {
                // Get a random index from the remaining elements
                int r = i + UnityEngine.Random.Range(0, n - i);
                // Swap the current element with the random one
                HexagonCellPrototype temp = cells[r];
                cells[r] = cells[i];
                cells[i] = temp;
            }
        }

        public static List<HexagonCellPrototype> Select_RandomEntryCellsFromEdges(
            List<HexagonCellPrototype> allEdgeCells,
            int num,
            bool assign,
            float minEntranceDistance,
            List<HexagonCellPrototype> allCellsList = null,
            int groundNeighborCountMin = 2
        )
        {
            // sort the layers by edge cell count;
            // sort edge cells by neighbor count
            // select random cells from beginning of lists
            List<HexagonCellPrototype> possibleCells = new List<HexagonCellPrototype>();

            List<HexagonCellPrototype> filtered = allEdgeCells.FindAll(c =>
                            c.IsGround() &&
                            c.IsGridEdge()
                        );
            List<HexagonCellPrototype> available;

            if (allCellsList != null)
            {
                available = filtered.FindAll(c =>
                               c.neighbors.Any(n =>
                                        allCellsList.Contains(n) &&
                                       n.IsGridEdge() == false &&
                                       n.IsSameLayer(c))
                           );
            }
            else
            {
                available = filtered;
            }

            foreach (HexagonCellPrototype edgeCell in available)
            {
                if (!edgeCell.IsGround()) continue;

                int groundNeighborCount = edgeCell.neighbors.FindAll(n => n.IsGround() && n.IsSameLayer(edgeCell)).Count;
                if (groundNeighborCount >= groundNeighborCountMin) possibleCells.Add(edgeCell);
            }

            return Select_RandomEntryCells(possibleCells, num, assign, -1, minEntranceDistance);
        }

        public static List<HexagonCellPrototype> Select_RandomEntryCellsFromEdges(
            List<HexagonCellPrototype> allEdgeCells,
            int num,
            bool assign,
            bool excludeAdjacentNeighbors = true,
            int groundNeighborCountMin = 2
        )
        {
            // sort the layers by edge cell count;
            // sort edge cells by neighbor count
            // select random cells from beginning of lists
            List<HexagonCellPrototype> possibleCells = new List<HexagonCellPrototype>();
            List<HexagonCellPrototype> available = allEdgeCells.FindAll(c => c.IsGround() && c.IsGridEdge());

            foreach (HexagonCellPrototype edgeCell in available)
            {
                if (!edgeCell.IsGround()) continue;

                int groundNeighborCount = edgeCell.neighbors.FindAll(n => n.IsGround() && n.IsSameLayer(edgeCell)).Count;
                if (groundNeighborCount >= groundNeighborCountMin) possibleCells.Add(edgeCell);
            }

            return Select_RandomEntryCells(possibleCells, num, assign, -1, excludeAdjacentNeighbors);
        }


        public static List<HexagonCellPrototype> Select_RandomEntryCells(
            List<HexagonCellPrototype> possibleCells,
            int num,
            bool assign,
            int gridLayer,
            float minPeerDistance
        )
        {
            List<HexagonCellPrototype> cells = new List<HexagonCellPrototype>();
            cells.AddRange(gridLayer == -1 ? possibleCells : possibleCells.FindAll(c => c.GetGridLayer() == gridLayer));

            ShuffleCells(cells);

            HashSet<string> added = new HashSet<string>();
            List<HexagonCellPrototype> entrances = new List<HexagonCellPrototype>();

            entrances.Add(cells[0]);
            added.Add(cells[0].GetId());
            if (assign) cells[0].SetEntryCell(true);

            if (num == 1) return entrances;

            while (entrances.Count < num && cells.Count > 0)
            {

                cells = cells.FindAll(c =>
                    added.Contains(c.GetId()) == false &&
                    !entrances.Any(e => VectorUtil.DistanceXZ(c.center, e.center) < minPeerDistance)).
                        OrderByDescending(x => VectorUtil.AverageDistanceFromPointsXZ(x, entrances)).ToList();

                if (cells.Count > 0)
                {
                    entrances.Add(cells[0]);
                    added.Add(cells[0].GetId());
                    if (assign) cells[0].SetEntryCell(true);
                }
            }

            return entrances;
        }

        public static List<HexagonCellPrototype> Select_RandomEntryCells(
            List<HexagonCellPrototype> possibleCells,
            int num,
            bool assign,
            int gridLayer,
            bool excludeAdjacentNeighbors = true
        )
        {
            List<HexagonCellPrototype> cells = new List<HexagonCellPrototype>();
            cells.AddRange(gridLayer == -1 ? possibleCells : possibleCells.FindAll(c => c.GetGridLayer() == gridLayer));

            ShuffleCells(cells);

            List<HexagonCellPrototype> entrances = new List<HexagonCellPrototype>();

            foreach (HexagonCellPrototype cell in cells)
            {
                if (entrances.Count >= num) break;

                bool isNeighbor = false;
                foreach (HexagonCellPrototype item in entrances)
                {
                    if ((item.neighbors.Contains(cell) && !excludeAdjacentNeighbors) || (excludeAdjacentNeighbors && item.neighbors.Any(nb => nb.neighbors.Contains(cell))))
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
            return entrances.OrderByDescending(x => x.neighbors.Count).ToList();
        }

        public static List<HexagonCellPrototype> GetChildrenForMicroClusterParent(HexagonCellPrototype parentCell, int howMNanyDegreesFromDirectNeighbors = 1, int maxMembers = 6)
        {
            List<HexagonCellPrototype> children = new List<HexagonCellPrototype>();
            string parentCellId = parentCell.id;
            int found = 0;

            for (var side = 0; side < parentCell.neighborsBySide.Length; side++)
            {
                HexagonCellPrototype neighbor = parentCell.neighborsBySide[side];

                if (neighbor == null) continue;

                // Check if direct neighbor is available
                if (neighbor.IsAssigned() == false)
                {
                    neighbor.SetClusterCellParentId(parentCellId);
                    children.Add(neighbor);
                    found++;
                }
                else if (howMNanyDegreesFromDirectNeighbors > 0)
                {
                    // Check if neighbor above is available
                    HexagonCellPrototype offNeighborTop = neighbor.GetTopNeighbor();
                    if (offNeighborTop != null && offNeighborTop.IsAssigned() == false)
                    {
                        offNeighborTop.SetClusterCellParentId(parentCellId);
                        children.Add(offNeighborTop);
                        found++;
                    }

                    if (found >= maxMembers) break;

                    // Check if 2nd degree neighbor is available
                    HexagonCellPrototype offNeighbor = neighbor.neighborsBySide[side];
                    if (offNeighbor != null && offNeighbor.IsAssigned() == false && offNeighbor.layerNeighbors[1] != null && offNeighbor.layerNeighbors[1].IsAssigned() == false)
                    {
                        offNeighbor.SetClusterCellParentId(parentCellId);
                        children.Add(offNeighbor);
                        found++;
                        // // Check if neighbor above is available
                        // if (offNeighbor.layeredNeighbor[1] != null) {
                        //     children.Add(offNeighbor.layeredNeighbor[1]);
                        // }
                    }
                }

                if (found >= maxMembers) break;
            }

            return children;
        }


        // public static void CollapseCellAndPropagate(
        //     HexagonCell currentCell,
        //     List<HexagonTileCore> prefabsList,
        //     TileContext tileContext,
        //     HexagonSocketDirectory socketDirectory,

        //     WFCCollapseOrder_Cells collapseOrder_cells,
        //     WFC_CellNeighborPropagation neighborPropagation,

        //     List<HexagonTileCore> tilePrefabs,
        //     List<HexagonTileCore> tilePrefabs_Edgable,

        //     List<HexagonCell> allAssignedCellsInOrder,

        //     bool isWalledEdge,
        //     bool logIncompatibilities = false,
        //     bool ignoreFailures = true,
        //     bool allowInvertedTiles = true
        // )
        // {
        //     bool assigned = currentCell.IsAssigned() ?
        //                         true
        //                         : WFCUtilities.SelectAndAssignNext(
        //                             currentCell,
        //                             currentCell.isEdgeCell ? tilePrefabs_Edgable : tilePrefabs,

        //                             allAssignedCellsInOrder,
        //                             TileContext.Micro,
        //                             socketDirectory,
        //                             isWalledEdge,
        //                             logIncompatibilities,
        //                             ignoreFailures,
        //                             allowInvertedTiles
        //                         );

        //     if (assigned && collapseOrder_cells == WFCCollapseOrder_Cells.Neighbor_Propogation)
        //     {
        //         int currentCellLayer = currentCell.GetGridLayer();

        //         bool includeLayerNwighbors = (neighborPropagation == WFC_CellNeighborPropagation.Edges_Only_Include_Layers || neighborPropagation == WFC_CellNeighborPropagation.Edges_Inners_Include_Layers);

        //         // Get Unassigned Neighbors
        //         List<HexagonCell> unassignedNeighbors = currentCell._neighbors.FindAll(n => n.IsAssigned() == false
        //                 && ((includeLayerNwighbors == false && n.GetGridLayer() == currentCellLayer)
        //                 || (includeLayerNwighbors && n.GetGridLayer() >= currentCellLayer)
        //                 ));

        //         if (unassignedNeighbors.Count > 0)
        //         {
        //             bool includeInners = (currentCell.isEdgeCell == false || neighborPropagation == WFC_CellNeighborPropagation.Edges_Inners_Include_Layers || neighborPropagation == WFC_CellNeighborPropagation.Edges_Inners_No_Layers);

        //             List<HexagonCell> edgeNeighbors = unassignedNeighbors.FindAll(n => n.isEdgeCell).OrderBy(n => n.GetEdgeCellType()).ToList();
        //             if (edgeNeighbors.Count > 0)
        //             {
        //                 foreach (HexagonCell neighbor in edgeNeighbors)
        //                 {
        //                     if (neighbor.IsAssigned()) continue;

        //                     bool wasAssigned = WFCUtilities.SelectAndAssignNext(
        //                                         neighbor,
        //                                         tilePrefabs_Edgable,

        //                                         allAssignedCellsInOrder,
        //                                         TileContext.Micro,
        //                                         socketDirectory,
        //                                         isWalledEdge,
        //                                         logIncompatibilities,
        //                                         ignoreFailures,
        //                                         allowInvertedTiles
        //                                     );
        //                 }
        //             }

        //             if (includeInners)
        //             {
        //                 List<HexagonCell> innerNeighbors = unassignedNeighbors.FindAll(n => n.isEdgeCell == false).OrderByDescending(n => n._neighbors.Count).ToList();

        //                 foreach (HexagonCell neighbor in innerNeighbors)
        //                 {
        //                     if (neighbor.IsAssigned()) continue;

        //                     WFCUtilities.SelectAndAssignNext(
        //                         neighbor,
        //                         tilePrefabs,

        //                         allAssignedCellsInOrder,
        //                         TileContext.Micro,
        //                         socketDirectory,
        //                         isWalledEdge,
        //                         logIncompatibilities,
        //                         ignoreFailures,
        //                         allowInvertedTiles
        //                     );
        //                 }
        //             }

        //         }
        //     }
        // }


    }
}