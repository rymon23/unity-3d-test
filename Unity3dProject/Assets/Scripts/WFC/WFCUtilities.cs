using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WFCSystem
{
    public interface IWFCSystem
    {
        public void ExecuteWFC();
        public void SetRadius(int value);
        public void SetCells(List<HexagonCell> _allCells);
        public void InstantiateAllTiles();

    }

    public enum WFCCollapseOrder
    {
        Default = 0, // Edges -> Center => th rest
        Contract, // Start at the edges
        Expand // Start at the center
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

        public static void InstantiateTile(HexagonTileCore prefab, HexagonCell cell, Transform folder, List<GameObject> activeTilesList)
        {
            int rotation = cell.GetTileRotation();
            bool isInverted = cell.IsTileInverted();

            Vector3 position = cell.transform.position;
            position.y += 0.2f;

            GameObject activeTile = GameObject.Instantiate(prefab.gameObject, position, Quaternion.identity);
            HexagonTileCore tileCore = activeTile.GetComponent<HexagonTileCore>();

            if (isInverted)
            {
                tileCore.InvertModel();
                activeTile.name = "INVERTED__" + activeTile.name;
            }
            HexagonTileCore.RotateTile(activeTile.gameObject, rotation);

            activeTilesList.Add(activeTile);
            activeTile.transform.SetParent(folder);

            tileCore.ShowSocketLabels(false);
            tileCore.SetIgnoreSocketLabelUpdates(true);
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
                cell.highlight = true;
                return false;
            }
            else
            {
                int[] selected = rotations_isInvertedTrue[UnityEngine.Random.Range(0, rotations_isInvertedTrue.Count)];
                bool shouldInvert = selected[1] == 1;

                cell.SetTile(tile, selected[0], shouldInvert);
                cell.highlight = false;
                return true;
            }
        }

        public static (HexagonTileCore, List<int[]>) SelectNextTile(HexagonCell cell, List<HexagonTileCore> prefabsList, bool allowInvertedTiles, bool isWalledEdge, HexagonSocketDirectory socketDirectory, bool logIncompatibilities)
        {
            // Create a list of compatible tiles and their rotations
            List<(HexagonTileCore, List<int[]>)> compatibleTilesAndRotations = new List<(HexagonTileCore, List<int[]>)>();

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

                HexagonTileCore currentTile = prefabsList[i];

                List<int[]> compatibleTileRotations = WFCUtilities.GetCompatibleTileRotations(cell, currentTile, allowInvertedTiles, isWalledEdge, socketDirectory, logIncompatibilities);

                if (compatibleTileRotations.Count > 0) compatibleTilesAndRotations.Add((currentTile, compatibleTileRotations));

            }
            // If there are no compatible tiles, return null
            if (compatibleTilesAndRotations.Count == 0)
            {
                if (logIncompatibilities)
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
                }
                return (null, null);
            }

            // Select a random compatible tile and rotation
            int randomIndex = UnityEngine.Random.Range(0, compatibleTilesAndRotations.Count);
            return compatibleTilesAndRotations[randomIndex];
        }

        static public List<int[]> GetCompatibleTileRotations(HexagonCell currentCell, HexagonTileCore currentTile, bool allowInvertedTiles, bool isWalledEdge, HexagonSocketDirectory socketDirectory, bool logIncompatibilities)
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
                List<int[]> compatibleRotations_Inverted = CheckTileSocketCompatibility(currentCell, currentTile, true, isWalledEdge, socketDirectory, logIncompatibilities);
                if (compatibleRotations_Inverted.Count > 0) compatibleRotations.AddRange(compatibleRotations_Inverted);
            }

            List<int[]> compatibleRotations_Uninverted = CheckTileSocketCompatibility(currentCell, currentTile, false, isWalledEdge, socketDirectory, logIncompatibilities);
            if (compatibleRotations_Uninverted.Count > 0) compatibleRotations.AddRange(compatibleRotations_Uninverted);

            // Debug.Log("GetCompatibleTileRotations - Cell: " + currentCell.id + ", compatibleRotations: " + compatibleRotations.Count);
            return compatibleRotations;
        }


        static public List<int[]> CheckTileSocketCompatibility(HexagonCell currentCell, HexagonTileCore currentTile, bool inverted, bool isWalledEdge, HexagonSocketDirectory socketDirectory, bool logIncompatibilities)
        {
            List<int[]> compatibleRotations = new List<int[]>();

            HexagonCell.NeighborSideCornerSockets[] neighborTileCornerSocketsBySide = currentCell.GetSideNeighborTileSockets(isWalledEdge);
            HexagonCell.NeighborLayerCornerSockets[] layeredNeighborTileCornerSockets = currentCell.GetLayeredNeighborTileSockets(TileContext.Default);

            bool checkLayerNeighbors = (currentCell.GetGridLayer() > 0 && !currentCell.isLeveledGroundCell);
            bool[,] compatibilityMatrix = socketDirectory.GetCompatibilityMatrix();

            // Check every rotation
            for (int rotation = 0; rotation < 6; rotation++)
            {
                bool compatibile = true;

                // Check Layered Neighbors First
                if (checkLayerNeighbors)
                {
                    // For now just check bottom neighbor's top against current tile's bottom
                    int[] currentTileBottomSockets = currentTile.GetRotatedLayerCornerSockets(false, rotation, inverted);
                    for (int i = 0; i < layeredNeighborTileCornerSockets[0].corners.Length; i++)
                    {
                        if (!compatibilityMatrix[currentTileBottomSockets[i], layeredNeighborTileCornerSockets[0].corners[i]])
                        {
                            // Debug.LogError(tileName + " Not compatibile with bottom layer. currentTileBottomSocket: " + currentTileBottomSockets[i] + ", corner: " + (HexagonCorner)i);
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
                        compatibile = WFCUtilities.IsTileCompatibleOnSideAndRotation(neighborTileCornerSocketsBySide, currentTile, side, rotation, neighborTile, inverted, socketDirectory, logIncompatibilities);
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


        public static bool IsTileCompatibleOnSideAndRotation(HexagonCell.NeighborSideCornerSockets[] neighborTileCornerSocketsBySide, HexagonTileCore currentTile, int side, int rotation, HexagonTileCore neighborTile, bool inverted, HexagonSocketDirectory socketDirectory, bool logIncompatibilities)
        {
            string tileName = currentTile.gameObject.name;
            string neighborTileName = neighborTile?.gameObject.name;
            string[] sockets = socketDirectory.sockets;
            bool[,] compatibilityMatrix = socketDirectory.GetCompatibilityMatrix();

            bool compatibile = true;

            HexagonCell.NeighborSideCornerSockets neighborSide = neighborTileCornerSocketsBySide[side];
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
                            string currentTileLog = "Incoming Tile: [" + tileName + "], " + level + " CornerSocket: " + sockets[currentTileCornerSocket];
                            string neighborTileLog = "Neighbor Tile: [" + neighborTileName + "], " + level + " CornerSocket: " + sockets[neighborTileCornerSocket];

                            Debug.LogError("Tile: [" + tileName + "] is INCOMPATIBLE with [" + neighborTileName + "] on side: " + (HexagonSide)side + ". \n" + currentTileLog + "\n" + neighborTileLog);
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

            (HexagonCorner currentCornerA, HexagonCorner currentCornerB) = HexagonCell.GetCornersFromSide((HexagonSide)currentRotatedSide);

            (HexagonCorner neighborCornerA, HexagonCorner neighborCornerB) = HexagonCell.GetCornersFromSide((HexagonSide)neighborRotatedSide);

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

            HexagonCell.NeighborLayerCornerSockets[] layeredNeighborTileCornerSockets = currentCell.GetLayeredNeighborTileSockets(TileContext.Micro);

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
            HexagonCell.NeighborLayerCornerSockets neighborTopCornerSockets = new HexagonCell.NeighborLayerCornerSockets();
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