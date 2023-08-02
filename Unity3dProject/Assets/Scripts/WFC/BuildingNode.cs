using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WFCSystem;
using System.Linq;

namespace ProceduralBase
{
    public class BuildingNode
    {
        public Vector3 Position { get; private set; }
        public Vector3 Lookup() => VectorUtil.PointLookupDefault(Position);
        public BuildingNode(Vector3 position, float _size)
        {
            Position = position;
            size = _size;
        }
        public HexagonCellPrototype owner { get; private set; } = null;
        public float size;
        public bool isEdge;
        public bool isTileEdge { get; private set; }
        public bool isTileInnerEdge { get; private set; }
        public bool ignore { get; private set; }
        public bool isEntry;

        public BuildingNode[] neighbors = new BuildingNode[6] { null, null, null, null, null, null };
        public Vector3[] corners { get; private set; } = new Vector3[8];
        public void SetCorners(Vector3[] _value)
        { corners = _value; }

        public void SetIgnored(bool value)
        {
            ignore = value;
            if (ignore && states.Contains(SurfaceBlockState.Ignore) == false) states.Add(SurfaceBlockState.Ignore);
            else if (!ignore && states.Contains(SurfaceBlockState.Ignore)) states.Remove(SurfaceBlockState.Ignore);
        }

        public void SetTileEdge(bool value)
        {
            isTileEdge = value;
            if (isTileEdge && states.Contains(SurfaceBlockState.TileEdge) == false) states.Add(SurfaceBlockState.TileEdge);
            else if (!isTileEdge && states.Contains(SurfaceBlockState.TileEdge)) states.Remove(SurfaceBlockState.TileEdge);
        }
        public void SetTileInnerEdge(bool value)
        {
            isTileInnerEdge = value;
            if (isTileInnerEdge && states.Contains(SurfaceBlockState.TileInnerEdge) == false) states.Add(SurfaceBlockState.TileInnerEdge);
            else if (!isTileInnerEdge && states.Contains(SurfaceBlockState.TileInnerEdge)) states.Remove(SurfaceBlockState.TileInnerEdge);
        }
        public bool IsTileEdge()
        {
            if (owner == null) return false;
            for (var i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i] != null && neighbors[i].owner != owner) return true;
            }
            return false;
        }
        public bool IsTileInnerEdge()
        {
            if (owner == null) return false;
            for (var i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i] != null && neighbors[i].owner != owner && neighbors[i].owner != null) return true;
            }
            return false;
        }
        public bool HasNeighborInLookup(HashSet<Vector3> lookups)
        {
            for (var i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i] != null && lookups.Contains(neighbors[i].Lookup())) return true;
            }
            return false;
        }

        public bool IsTileTopEdge() => (neighbors[(int)SurfaceBlockSide.Top] == null || neighbors[(int)SurfaceBlockSide.Top].owner != owner);
        public bool IsTileBottomEdge() => (neighbors[(int)SurfaceBlockSide.Bottom] == null || neighbors[(int)SurfaceBlockSide.Bottom].owner != owner);
        public BuildingNode GetNeighborOnSide(SurfaceBlockSide side) => neighbors[(int)side];

        public List<SurfaceBlockState> states = new List<SurfaceBlockState>();

        public bool HasFilteredState(List<SurfaceBlockState> filterOnStates)
        {
            foreach (var item in states)
            {
                if (filterOnStates.Contains(item)) return true;
            }
            return false;
        }

        public List<int> GetOutOfBoundsCorners()
        {
            if (owner == null) return null;
            List<int> results = new List<int>();
            // check bottom corners for now
            for (var i = 0; i < 4; i++)
            {
                if (VectorUtil.IsPointWithinPolygon(corners[i], owner.cornerPoints) == false) results.Add(i);
            }
            return results;
        }

        public bool IsRoof() => (neighbors[(int)SurfaceBlockSide.Top] == null);
        public bool IsFloor() => (neighbors[(int)SurfaceBlockSide.Bottom] == null);
        public bool IsCorner()
        {
            // Check if the block is a corner based on its neighbors
            int numSidesWithoutNeighbors = 0;

            if (neighbors[(int)SurfaceBlockSide.Front] == null)
                numSidesWithoutNeighbors++;

            if (neighbors[(int)SurfaceBlockSide.Right] == null)
                numSidesWithoutNeighbors++;

            if (neighbors[(int)SurfaceBlockSide.Back] == null)
                numSidesWithoutNeighbors++;

            if (neighbors[(int)SurfaceBlockSide.Left] == null)
                numSidesWithoutNeighbors++;

            return numSidesWithoutNeighbors == 2;
        }


        public static (Vector3, SocketFace) GetFaceProfie(BuildingNode block, BuildingNode foreignNeighbor)
        {
            Vector3 faceLookupRaw = VectorUtil.GetPointBetween(block.Position, foreignNeighbor.Position);
            faceLookupRaw = VectorUtil.PointLookupDefault(faceLookupRaw);
            Vector3 ownersCenterLookup = VectorUtil.PointLookupDefault(
                VectorUtil.GetPointBetween(block.owner.center, foreignNeighbor.owner.center)
            );
            Vector3 temp = VectorUtil.CalculateOffsetAbs(faceLookupRaw, ownersCenterLookup);
            Vector3 faceLookup = VectorUtil.PointLookupDefault(temp);
            // Vector3 faceLookup = VectorUtil.PointLookupDefault(new Vector3(temp.x, faceLookupRaw.y, temp.z));
            // Vector3 CenterPointAtZero(faceLookupRaw, Vector3 currentCenter)
            // Vector3 faceLookup = VectorUtil.PointLookupDefault(faceLookupRaw);

            if (block.ignore) return (faceLookup, SocketFace.Blank);
            if (foreignNeighbor.ignore) return (faceLookup, SocketFace.ClosedFace);
            return (faceLookup, SocketFace.OpenFace);
        }

        public static bool AreFacesCompatible(SocketFace faceA, SocketFace faceB)
        {
            if (faceA == SocketFace.Blank && faceB == SocketFace.Blank) return true;
            if (faceA == SocketFace.OpenFace && faceB == SocketFace.OpenFace) return true;
            if (
                faceA == SocketFace.Blank && faceB == SocketFace.ClosedFace ||
                faceB == SocketFace.Blank && faceA == SocketFace.ClosedFace
            ) return true;

            return false;
        }

        public static SurfaceBlockSide GetNeighborsRelativeSide(SurfaceBlockSide side)
        {
            // Assumes the same rotation
            switch (side)
            {
                case (SurfaceBlockSide.Front):
                    return SurfaceBlockSide.Back;

                case (SurfaceBlockSide.Back):
                    return SurfaceBlockSide.Front;

                case (SurfaceBlockSide.Bottom):
                    return SurfaceBlockSide.Top;

                case (SurfaceBlockSide.Top):
                    return SurfaceBlockSide.Bottom;

                case (SurfaceBlockSide.Left):
                    return SurfaceBlockSide.Right;

                case (SurfaceBlockSide.Right):
                    return SurfaceBlockSide.Left;

                default:
                    return side;
            }
        }

        public Vector3[] GetCornersOnSide(SurfaceBlockSide side)
        {
            Vector3[] sideCorners = new Vector3[4];
            switch (side)
            {
                case SurfaceBlockSide.Front:
                    sideCorners[0] = corners[1];
                    sideCorners[1] = corners[2];
                    sideCorners[2] = corners[5];
                    sideCorners[3] = corners[6];
                    break;
                case SurfaceBlockSide.Right:
                    sideCorners[0] = corners[2];
                    sideCorners[1] = corners[3];
                    sideCorners[2] = corners[6];
                    sideCorners[3] = corners[7];
                    break;
                case SurfaceBlockSide.Back:
                    sideCorners[0] = corners[0];
                    sideCorners[1] = corners[3];
                    sideCorners[2] = corners[4];
                    sideCorners[3] = corners[7];
                    break;
                case SurfaceBlockSide.Left:
                    sideCorners[0] = corners[0];
                    sideCorners[1] = corners[1];
                    sideCorners[2] = corners[4];
                    sideCorners[3] = corners[5];
                    break;
                case SurfaceBlockSide.Top:
                    sideCorners[0] = corners[4];
                    sideCorners[1] = corners[5];
                    sideCorners[2] = corners[6];
                    sideCorners[3] = corners[7];
                    break;
                case SurfaceBlockSide.Bottom:
                    sideCorners[0] = corners[0];
                    sideCorners[1] = corners[1];
                    sideCorners[2] = corners[2];
                    sideCorners[3] = corners[3];
                    break;
            }
            return sideCorners;
        }

        public static Vector3[] CreateCorners(Vector3 centerPos, float size)
        {
            Vector3[] new_corners = new Vector3[8];
            // Calculate half size for convenience
            float halfSize = size * 0.5f;
            // Calculate the 8 corner points of the cube
            new_corners[0] = centerPos + new Vector3(-halfSize, -halfSize, -halfSize);
            new_corners[1] = centerPos + new Vector3(-halfSize, -halfSize, halfSize);
            new_corners[2] = centerPos + new Vector3(halfSize, -halfSize, halfSize);
            new_corners[3] = centerPos + new Vector3(halfSize, -halfSize, -halfSize);
            new_corners[4] = centerPos + new Vector3(-halfSize, halfSize, -halfSize);
            new_corners[5] = centerPos + new Vector3(-halfSize, halfSize, halfSize);
            new_corners[6] = centerPos + new Vector3(halfSize, halfSize, halfSize);
            new_corners[7] = centerPos + new Vector3(halfSize, halfSize, -halfSize);

            return new_corners;
        }

        public static BuildingNode[,,] CreateSurfaceBlocks(Vector3 origin, int gridSizeX, int gridSizeY, int gridSizeZ, float size, Bounds gridBounds, List<Bounds> structureBounds)
        {
            (Vector3[,,] points, float spacing) = VectorUtil.Generate3DGrid(gridBounds, gridSizeX, gridSizeY, origin.y);
            return BuildingNode.CreateSurfaceBlocks(points, structureBounds, spacing);
        }

        public static BuildingNode[,,] CreateSurfaceBlocks(Vector3[,,] pointsMatrix, List<Bounds> bounds, float size)
        {
            int sizeX = pointsMatrix.GetLength(0);
            int sizeY = pointsMatrix.GetLength(1);
            int sizeZ = pointsMatrix.GetLength(2);
            BuildingNode[,,] surfaceBlocks = new BuildingNode[sizeX, sizeY, sizeZ];
            Dictionary<Vector3, BuildingNode> centerLookup = new Dictionary<Vector3, BuildingNode>();
            List<BuildingNode> neighborsToFill = new List<BuildingNode>();
            Dictionary<Vector3, Vector3> cornerLookups = new Dictionary<Vector3, Vector3>();

            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        Vector3 position = pointsMatrix[x, y, z];

                        if (VectorUtil.IsPointWithinBounds(bounds, position))
                        {
                            BuildingNode surfaceBlock = new BuildingNode(position, size);
                            surfaceBlocks[x, y, z] = surfaceBlock;
                            neighborsToFill.Add(surfaceBlock);
                            Vector3 lookup = VectorUtil.PointLookupDefault(position);
                            if (centerLookup.ContainsKey(lookup) == false)
                            {
                                centerLookup.Add(lookup, surfaceBlock);
                            }
                            else Debug.LogError("lookup already exists: " + lookup + ", y: " + y);

                            // Generate corners for the current surfaceBlock
                            Vector3[] corners = CreateCorners(position, size);
                            for (var i = 0; i < corners.Length; i++)
                            {
                                Vector3 cornerLookup = VectorUtil.PointLookupDefault(corners[i]);
                                if (cornerLookups.ContainsKey(cornerLookup))
                                {
                                    corners[i] = cornerLookups[cornerLookup];
                                }
                                else cornerLookups.Add(cornerLookup, corners[i]);
                            }

                            surfaceBlock.corners = corners;
                        }
                        else
                        {
                            surfaceBlocks[x, y, z] = null;
                        }
                    }
                }
            }

            foreach (var item in neighborsToFill)
            {
                Vector3[] neighborCenters = GenerateNeighborCenters(item.Position, size * 2);
                int found = 0;
                for (var i = 0; i < neighborCenters.Length; i++)
                {
                    Vector3 lookup = VectorUtil.PointLookupDefault(neighborCenters[i]);
                    if (centerLookup.ContainsKey(lookup) && centerLookup[lookup] != item)
                    {
                        item.neighbors[i] = centerLookup[lookup];
                        found++;
                    }
                }
                if (found < 6)
                {
                    item.isEdge = true;
                    item.states.Add(SurfaceBlockState.Edge);
                }
            }

            return surfaceBlocks;
        }

        public static Vector3[] GenerateNeighborCenters(Vector3 center, float size)
        {
            Vector3[] neighborCenters = new Vector3[6];
            float halfSize = size / 2f; // Half the size of the cube
                                        // Front neighbor center
            neighborCenters[(int)SurfaceBlockSide.Front] = center + new Vector3(0f, 0f, halfSize);
            // Right neighbor center
            neighborCenters[(int)SurfaceBlockSide.Right] = center + new Vector3(halfSize, 0f, 0f);
            // Back neighbor center
            neighborCenters[(int)SurfaceBlockSide.Back] = center + new Vector3(0f, 0f, -halfSize);
            // Left neighbor center
            neighborCenters[(int)SurfaceBlockSide.Left] = center + new Vector3(-halfSize, 0f, 0f);
            // Top neighbor center
            neighborCenters[(int)SurfaceBlockSide.Top] = center + new Vector3(0f, halfSize, 0f);
            // Bottom neighbor center
            neighborCenters[(int)SurfaceBlockSide.Bottom] = center + new Vector3(0f, -halfSize, 0f);
            return neighborCenters;
        }



        public static Dictionary<HexagonCellPrototype, List<BuildingNode>> GetSurfaceBlocksByCell(BuildingNode[,,] blockGrid, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>> cellsByLayer, float layerElevation)
        {
            Dictionary<HexagonCellPrototype, List<BuildingNode>> resultsByCell = new Dictionary<HexagonCellPrototype, List<BuildingNode>>();
            int gridSizeX = blockGrid.GetLength(0);
            int gridSizeY = blockGrid.GetLength(1);
            int gridSizeZ = blockGrid.GetLength(2);
            // int currentLayerBase = -1;

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    for (int z = 0; z < gridSizeZ; z++)
                    {
                        BuildingNode block = blockGrid[x, y, z];
                        if (block != null && !block.ignore)
                        {
                            foreach (int currentLayer in cellsByLayer.Keys)
                            {
                                foreach (var cell in cellsByLayer[currentLayer].Values)
                                {
                                    // if (|| currentLayerBase != currentLayer) currentLayerBase = currentLayer; 

                                    if (VectorUtil.IsBlockWithinVerticalBounds(cell.center.y, layerElevation, block.Position, block.size) && VectorUtil.IsPointWithinPolygon(block.Position, cell.cornerPoints))
                                    {
                                        if (resultsByCell.ContainsKey(cell) == false) resultsByCell.Add(cell, new List<BuildingNode>());
                                        block.owner = cell;
                                        resultsByCell[cell].Add(block);
                                        break;
                                    }
                                }

                            }
                        }
                    }
                }
            }
            return resultsByCell;
        }
        public static Dictionary<HexagonCellPrototype, List<BuildingNode>> GetSurfaceBlocksByCell(BuildingNode[,,] blockGrid, List<HexagonCellPrototype> cells)
        {
            Dictionary<HexagonCellPrototype, List<BuildingNode>> resultsByCell = new Dictionary<HexagonCellPrototype, List<BuildingNode>>();
            int gridSizeX = blockGrid.GetLength(0);
            int gridSizeY = blockGrid.GetLength(1);
            int gridSizeZ = blockGrid.GetLength(2);

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    for (int z = 0; z < gridSizeZ; z++)
                    {
                        BuildingNode block = blockGrid[x, y, z];
                        if (block != null)
                        {
                            foreach (var cell in cells)
                            {
                                // if (IsAnyPointWithinPolygon(block.Position, block.size, cell.cornerPoints))
                                if (VectorUtil.IsPointWithinPolygon(block.Position, cell.cornerPoints))
                                {
                                    if (resultsByCell.ContainsKey(cell) == false) resultsByCell.Add(cell, new List<BuildingNode>());
                                    block.owner = cell;
                                    resultsByCell[cell].Add(block);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return resultsByCell;
        }

        public static Dictionary<HexagonCellPrototype, List<SurfaceBlock>> EvaluateTileEdges(SurfaceBlock[,,] blockGrid)
        {
            Dictionary<HexagonCellPrototype, List<SurfaceBlock>> resultsByCell = new Dictionary<HexagonCellPrototype, List<SurfaceBlock>>();
            int gridSizeX = blockGrid.GetLength(0);
            int gridSizeY = blockGrid.GetLength(1);
            int gridSizeZ = blockGrid.GetLength(2);

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    for (int z = 0; z < gridSizeZ; z++)
                    {
                        SurfaceBlock block = blockGrid[x, y, z];
                        if (block == null || block.owner == null) continue;

                        block.SetTileEdge(block.IsTileEdge());
                        block.SetTileInnerEdge(block.IsTileInnerEdge());
                    }
                }
            }
            return resultsByCell;
        }

        public static List<SurfaceBlock> GetViableEntryways(SurfaceBlock[,,] blockGrid, List<HexagonCellPrototype> edgeCells, float maxCenterRadius)
        {
            List<SurfaceBlock> surfaceBlocks = new List<SurfaceBlock>();
            int gridSizeX = blockGrid.GetLength(0);
            int gridSizeY = blockGrid.GetLength(1);
            int gridSizeZ = blockGrid.GetLength(2);
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    for (int z = 0; z < gridSizeZ; z++)
                    {
                        SurfaceBlock block = blockGrid[x, y, z];
                        if (block != null && block.isEdge && !block.ignore && !block.IsCorner() && !block.IsFloor() && !block.IsRoof())
                        {
                            (HexagonCellPrototype nearest, float dist) = HexagonCellPrototype.GetClosestCenter_WithDistance(edgeCells, block.Position);
                            if (nearest != null && dist < maxCenterRadius)
                            {
                                surfaceBlocks.Add(block);
                                blockGrid[x, y, z].isEntry = true;
                                if (block.states.Contains(SurfaceBlockState.Entry) == false) blockGrid[x, y, z].states.Add(SurfaceBlockState.Entry);
                            }
                        }
                    }
                }
            }
            return surfaceBlocks;
        }

        public static BuildingNode[,,] ClearInnerBlocksAroundPoint(BuildingNode[,,] blockGrid, Vector3 point, float radius)
        {
            int gridSizeX = blockGrid.GetLength(0);
            int gridSizeY = blockGrid.GetLength(1);
            int gridSizeZ = blockGrid.GetLength(2);
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    for (int z = 0; z < gridSizeZ; z++)
                    {
                        BuildingNode block = blockGrid[x, y, z];
                        if (block != null && !block.isEdge && !block.ignore)
                        {
                            float dist = Vector3.Distance(block.Position, point);
                            if (dist < radius)
                            {
                                blockGrid[x, y, z].ignore = true;
                                if (block.states.Contains(SurfaceBlockState.Ignore) == false) blockGrid[x, y, z].states.Add(SurfaceBlockState.Ignore);
                            }
                        }
                    }
                }
            }
            return blockGrid;
        }

        public static BuildingNode[,,] ClearInnerBlocks(BuildingNode[,,] blockGrid)
        {
            int gridSizeX = blockGrid.GetLength(0);
            int gridSizeY = blockGrid.GetLength(1);
            int gridSizeZ = blockGrid.GetLength(2);
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    for (int z = 0; z < gridSizeZ; z++)
                    {
                        BuildingNode block = blockGrid[x, y, z];
                        if (block != null && block.isEdge == false)
                        {
                            blockGrid[x, y, z].ignore = true;
                            if (block.states.Contains(SurfaceBlockState.Ignore) == false) blockGrid[x, y, z].states.Add(SurfaceBlockState.Ignore);
                        }
                    }
                }
            }
            return blockGrid;
        }


        public static void DrawGrid(BuildingNode[,,] blockGrid, HexagonCellPrototype _highlightedCell = null)
        {
            Dictionary<string, Color> customColors = UtilityHelpers.CustomColorDefaults();
            int gridSizeX = blockGrid.GetLength(0);
            int gridSizeY = blockGrid.GetLength(1);
            int gridSizeZ = blockGrid.GetLength(2);
            // bool doOnce = false;

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    for (int z = 0; z < gridSizeZ; z++)
                    {
                        BuildingNode block = blockGrid[x, y, z];
                        if (block != null)
                        {
                            if (_highlightedCell != null && block.owner != _highlightedCell) continue;

                            if (!block.ignore)
                            {
                                Gizmos.color = Color.gray;
                                Gizmos.DrawWireSphere(block.Position, 0.15f);

                                block.DrawNeighbors();

                                if (block.isTileEdge)
                                {
                                    Gizmos.color = Color.yellow;
                                    Gizmos.DrawWireSphere(block.Position, 0.25f);
                                }
                            }

                            // if (block.isTileInnerEdge)
                            // {
                            //     Gizmos.color = customColors["orange"];
                            //     Gizmos.DrawSphere(block.Position, 0.25f);
                            // }

                            // if (!doOnce && y == gridSizeY - 1)
                            // // if (!doOnce )
                            // {
                            //     doOnce = true;

                            //     Gizmos.color = Color.black;
                            //     Gizmos.DrawSphere(block.Position, 0.2f);

                            //     Gizmos.color = Color.red;
                            //     Vector3[] neighborCenters = GenerateNeighborCenters(block.Position, block.size * 2);
                            //     Gizmos.DrawSphere(neighborCenters[(int)SurfaceBlockSide.Bottom], 0.3f);
                            // }

                            // if (block.IsCorner())
                            // {
                            //     Gizmos.color = Color.blue;
                            //     Gizmos.DrawSphere(block.Position, 0.3f);
                            // }
                            // if (block.isEntry)
                            // {
                            //     Gizmos.color = Color.green;
                            //     Gizmos.DrawSphere(block.Position, 0.3f);
                            // }


                            // else
                            // {
                            //     if (block.isEdge)
                            //     {
                            //         Gizmos.color = Color.grey;
                            //     }
                            //     // else Gizmos.color = Color.green;

                            //     Gizmos.DrawSphere(block.Position, 0.3f);
                            // }
                        }
                    }
                }
            }
        }

        public static bool IsAnyPointWithinPolygon(Vector3 point, float pointSize, Vector3[] polygonCorners)
        {
            if (VectorUtil.IsPointWithinPolygon(point, polygonCorners)) return true;
            return IsAnyEdgePointWithinPolygon(point, pointSize, polygonCorners);
        }

        public static bool IsAnyEdgePointWithinPolygon(Vector3 point, float pointSize, Vector3[] polygonCorners)
        {
            Vector3[] corners = SurfaceBlock.CreateCorners(point, pointSize);
            foreach (var item in corners)
            {
                if (VectorUtil.IsPointWithinPolygon(item, polygonCorners)) return true;
            }
            return false;
        }

        public static Vector3 GetCluster_LengthWidthHeight(List<BuildingNode> cluster)
        {
            if (cluster.Count == 0)
                return Vector3.zero;

            Vector3 minPosition = cluster[0].Position;
            Vector3 maxPosition = cluster[0].Position;

            foreach (BuildingNode block in cluster)
            {
                Vector3 position = block.Position;

                // Update the minimum position
                minPosition.x = Mathf.Min(minPosition.x, position.x);
                minPosition.y = Mathf.Min(minPosition.y, position.y);
                minPosition.z = Mathf.Min(minPosition.z, position.z);

                // Update the maximum position
                maxPosition.x = Mathf.Max(maxPosition.x, position.x);
                maxPosition.y = Mathf.Max(maxPosition.y, position.y);
                maxPosition.z = Mathf.Max(maxPosition.z, position.z);
            }

            // Calculate the size of the cluster in each dimension
            Vector3 size = maxPosition - minPosition;

            return new Vector3(size.x, size.z, size.y);
        }


        public static List<List<BuildingNode>> GetConsecutiveClusters(
            BuildingNode[,,] blockGrid,
            int clustersMax,
            Vector2Int membersMinMax,
            List<SurfaceBlockState> filterOnStates,
            Vector3 blockGrid_cluster_LWH_Min,
            CellSearchPriority searchPriority = CellSearchPriority.SideNeighbors
        )
        {
            HashSet<Vector3> visited = new HashSet<Vector3>();
            HashSet<Vector3> added = new HashSet<Vector3>();
            List<List<BuildingNode>> clusters = new List<List<BuildingNode>>();

            int gridSizeX = blockGrid.GetLength(0);
            int gridSizeY = blockGrid.GetLength(1);
            int gridSizeZ = blockGrid.GetLength(2);
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    for (int z = 0; z < gridSizeZ; z++)
                    {
                        BuildingNode block = blockGrid[x, y, z];
                        if (block == null) continue;

                        if (filterOnStates != null && block.HasFilteredState(filterOnStates) == false) continue;

                        Vector3 blockLookup = block.Lookup();
                        if (visited.Contains(blockLookup) == false && added.Contains(blockLookup) == false)
                        {
                            List<BuildingNode> cluster = GetConsecutiveNeighborsCluster(
                                block,
                                UnityEngine.Random.Range(membersMinMax.x, membersMinMax.y),
                                searchPriority,
                                filterOnStates,
                                visited,
                                added
                            );

                            if (cluster.Count > 0)
                            {
                                // Vector3 lwh = GetCluster_LengthWidthHeight(cluster);
                                // if (
                                //     lwh.z >= blockGrid_cluster_LWH_Min.z &&

                                //    (lwh.x >= blockGrid_cluster_LWH_Min.x ||
                                //     lwh.y >= blockGrid_cluster_LWH_Min.y)
                                //     )
                                // {
                                clusters.Add(cluster);
                                // Debug.Log("surfaceblock cluster LWH: " + lwh + ", members: " + cluster.Count + ", blockGrid_cluster_LWH_Min: " + blockGrid_cluster_LWH_Min);
                                // }
                            }
                            if (clusters.Count >= clustersMax) return clusters;
                        }

                    }
                }
            }
            return clusters;
        }

        public static List<BuildingNode> GetConsecutiveNeighborsCluster(
            BuildingNode start,
            int maxMembers,
            CellSearchPriority searchPriority = CellSearchPriority.SideNeighbors,
            List<SurfaceBlockState> filterOnStates = null,
            HashSet<Vector3> visited = null,
            HashSet<Vector3> added = null,
            bool excludeClusterNeighbors = true
        )
        {
            List<BuildingNode> cluster = new List<BuildingNode>();

            if (visited == null) visited = new HashSet<Vector3>();
            if (added == null) added = new HashSet<Vector3>();

            RecursivelyFindNeighborsForCluster(start, maxMembers, cluster, visited, added, excludeClusterNeighbors, searchPriority, filterOnStates);
            return cluster;
        }

        private static void RecursivelyFindNeighborsForCluster(
            BuildingNode current,
            int maxMembers,
            List<BuildingNode> cluster,
            HashSet<Vector3> visited,
            HashSet<Vector3> added = null,
            bool excludeClusterNeighbors = true,
            CellSearchPriority searchPriority = CellSearchPriority.SideNeighbors,
            List<SurfaceBlockState> filterOnStates = null
        )
        {
            cluster.Add(current);
            visited.Add(current.Lookup());
            added.Add(current.Lookup());

            if (cluster.Count >= maxMembers) return;

            int nLength = current.neighbors.Length;
            int mod = searchPriority == CellSearchPriority.LayerNeighbors ? 4 : 0;
            for (var i = 0; i < nLength; i++)
            {
                if (cluster.Count > maxMembers) break;
                int ix = (i + mod) % nLength;
                BuildingNode neighbor = current.neighbors[ix];
                if (neighbor == null) continue;

                Vector3 lookup = neighbor.Lookup();
                if (
                    added.Contains(lookup)
                    || (filterOnStates != null && neighbor.HasFilteredState(filterOnStates) == false)
                // || (excludeClusterNeighbors && neighbor.HasNeighborInLookup(added))
                )
                {
                    visited.Add(lookup);
                    continue;
                }

                if (!added.Contains(neighbor.Lookup()))
                {
                    RecursivelyFindNeighborsForCluster(neighbor, maxMembers, cluster, visited, added, excludeClusterNeighbors, searchPriority, filterOnStates);
                }
            }
        }

        public void DrawNeighbors()
        {
            for (var i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i] != null && !neighbors[i].ignore)
                {
                }
                else
                {
                    // Debug.Log("No neighbor on side: " + (SurfaceBlockSide)i);
                    Gizmos.color = Color.white;
                    Vector3[] sideCorners = GetCornersOnSide((SurfaceBlockSide)i);
                    Gizmos.DrawLine(sideCorners[0], sideCorners[1]);
                    Gizmos.DrawLine(sideCorners[1], sideCorners[3]);
                    Gizmos.DrawLine(sideCorners[3], sideCorners[2]);
                    Gizmos.DrawLine(sideCorners[2], sideCorners[0]);
                    Gizmos.DrawLine(sideCorners[0], sideCorners[3]);
                    Gizmos.DrawLine(sideCorners[2], sideCorners[1]);
                }
            }
        }


    }
}