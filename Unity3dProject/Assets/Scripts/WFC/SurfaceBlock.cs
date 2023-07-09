using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using WFCSystem;
using System.Linq;

namespace ProceduralBase
{
    // public enum CubeCorners { TopLeftFront, TopRightFront, BottomLeftFront, BottomRightFront, TopLeftBack, TopRightBack, BottomLeftBack, BottomRightBack }
    public enum SurfaceBlockSide { Front = 0, Right, Back, Left, Top, Bottom, }
    public enum SurfaceBlockState { Unset = 0, Edge = 1, Corner, Base, Top, Entry, Ignore, Highlight, TileEdge }

    public class SurfaceBlock
    {
        public Vector3 Position { get; private set; }
        public Vector3 Lookup() => VectorUtil.PointLookupDefault(Position);
        public SurfaceBlock(Vector3 position, float _size)
        {
            Position = position;
            size = _size;
        }
        public HexagonCellPrototype owner { get; private set; } = null;
        public float size;
        public bool isEdge;
        public bool isTileEdge { get; private set; }
        public bool ignore;
        public bool isEntry;

        public SurfaceBlock[] neighbors = new SurfaceBlock[6] { null, null, null, null, null, null };
        public Vector3[] corners { get; private set; } = new Vector3[8];
        public void SetCorners(Vector3[] _value)
        {
            corners = _value;
        }

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

        public SurfaceBlock GetNeighborOnSide(SurfaceBlockSide side) => neighbors[(int)side];

        public List<SurfaceBlockState> states = new List<SurfaceBlockState>();

        public bool HasFilteredState(List<SurfaceBlockState> filterOnStates)
        {
            foreach (var item in states)
            {
                if (filterOnStates.Contains(item)) return true;
            }
            return false;
        }

        public bool IsTileEdge()
        {
            if (owner == null) return false;
            for (var i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i] != null && !neighbors[i].ignore && neighbors[i].owner != owner) return true;
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
            //Front
            // Gizmos.DrawSphere(cubePTs[1], 0.2f);
            // Gizmos.DrawSphere(cubePTs[2], 0.2f);
            // Gizmos.DrawSphere(cubePTs[5], 0.2f);
            // Gizmos.DrawSphere(cubePTs[6], 0.2f);
            //Back
            // Gizmos.DrawSphere(cubePTs[0], 0.2f);
            // Gizmos.DrawSphere(cubePTs[3], 0.2f);
            // Gizmos.DrawSphere(cubePTs[4], 0.2f);
            // Gizmos.DrawSphere(cubePTs[7], 0.2f);
            //Bottom
            // Gizmos.DrawSphere(cubePTs[0], 0.2f);
            // Gizmos.DrawSphere(cubePTs[1], 0.2f);
            // Gizmos.DrawSphere(cubePTs[2], 0.2f);
            // Gizmos.DrawSphere(cubePTs[3], 0.2f);
            //Top
            // Gizmos.DrawSphere(cubePTs[4], 0.2f);
            // Gizmos.DrawSphere(cubePTs[5], 0.2f);
            // Gizmos.DrawSphere(cubePTs[6], 0.2f);
            // Gizmos.DrawSphere(cubePTs[7], 0.2f);
            //Right
            // Gizmos.DrawSphere(cubePTs[0], 0.2f);
            // Gizmos.DrawSphere(cubePTs[1], 0.2f);
            // Gizmos.DrawSphere(cubePTs[4], 0.2f);
            // Gizmos.DrawSphere(cubePTs[5], 0.2f);
            //Left
            // Gizmos.DrawSphere(cubePTs[2], 0.2f);
            // Gizmos.DrawSphere(cubePTs[3], 0.2f);
            // Gizmos.DrawSphere(cubePTs[6], 0.2f);
            // Gizmos.DrawSphere(cubePTs[7], 0.2f);
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


        public static SurfaceBlock[,,] CreateSurfaceBlocks(Vector3 origin, int gridSizeX, int gridSizeY, int gridSizeZ, float size, Bounds gridBounds, List<Bounds> structureBounds)
        {
            (Vector3[,,] points, float spacing) = VectorUtil.Generate3DGrid(gridBounds, gridSizeX, gridSizeY, origin.y);
            return SurfaceBlock.CreateSurfaceBlocks(points, structureBounds, spacing);
        }


        public static SurfaceBlock[,,] CreateSurfaceBlocks(Vector3[,,] pointsMatrix, List<Bounds> bounds, float size)
        {
            int sizeX = pointsMatrix.GetLength(0);
            int sizeY = pointsMatrix.GetLength(1);
            int sizeZ = pointsMatrix.GetLength(2);
            SurfaceBlock[,,] surfaceBlocks = new SurfaceBlock[sizeX, sizeY, sizeZ];
            Dictionary<Vector3, SurfaceBlock> centerLookup = new Dictionary<Vector3, SurfaceBlock>();
            List<SurfaceBlock> neighborsToFill = new List<SurfaceBlock>();
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
                            SurfaceBlock surfaceBlock = new SurfaceBlock(position, size);
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

        public void DrawNeighbors()
        {
            for (var i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i] != null && !neighbors[i].ignore)
                {
                    // Gizmos.color = Color.green;
                    // Gizmos.DrawWireSphere(neighbors[i].Position, 0.2f);
                    // Debug.Log("neighbor found on side: " + (SurfaceBlockSide)i);
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

        public static Dictionary<HexagonCellPrototype, List<SurfaceBlock>> GetSurfaceBlocksByCell(SurfaceBlock[,,] blockGrid, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>> cellsByLayer, float layerElevation)
        {
            Dictionary<HexagonCellPrototype, List<SurfaceBlock>> resultsByCell = new Dictionary<HexagonCellPrototype, List<SurfaceBlock>>();
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
                        SurfaceBlock block = blockGrid[x, y, z];
                        if (block != null && !block.ignore)
                        {
                            foreach (int currentLayer in cellsByLayer.Keys)
                            {
                                foreach (var cell in cellsByLayer[currentLayer].Values)
                                {
                                    // if (|| currentLayerBase != currentLayer) currentLayerBase = currentLayer; 

                                    if (VectorUtil.IsCellWithinVerticalBounds(cell.center.y, layerElevation, block.Position, block.size) && VectorUtil.IsPointWithinPolygon(block.Position, cell.cornerPoints))
                                    {
                                        if (resultsByCell.ContainsKey(cell) == false) resultsByCell.Add(cell, new List<SurfaceBlock>());
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
        public static Dictionary<HexagonCellPrototype, List<SurfaceBlock>> GetSurfaceBlocksByCell(SurfaceBlock[,,] blockGrid, List<HexagonCellPrototype> cells)
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
                        if (block != null && !block.ignore)
                        {
                            foreach (var cell in cells)
                            {
                                // if (IsAnyPointWithinPolygon(block.Position, block.size, cell.cornerPoints))
                                if (VectorUtil.IsPointWithinPolygon(block.Position, cell.cornerPoints))
                                {
                                    if (resultsByCell.ContainsKey(cell) == false) resultsByCell.Add(cell, new List<SurfaceBlock>());
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
                        if (block == null || block.ignore || block.owner == null) continue;

                        block.SetTileEdge(block.IsTileEdge());
                    }
                }
            }
            return resultsByCell;
        }

        public static Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, List<Vector3>>> GetIntersectionPointsBySideByCell(Dictionary<HexagonCellPrototype, List<SurfaceBlock>> resultsByCell)
        {
            Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, List<Vector3>>> intersectionPointsBySideByCell = new Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, List<Vector3>>>();
            Dictionary<Vector3, Vector3> intersectionPointsLookup = new Dictionary<Vector3, Vector3>();

            foreach (var cell in resultsByCell.Keys)
            {
                if (intersectionPointsBySideByCell.ContainsKey(cell) == false) intersectionPointsBySideByCell.Add(cell, new Dictionary<HexagonTileSide, List<Vector3>>());

                List<HexagonTileSide> neighborSides = cell.GetNeighborSidesX8(Filter_CellType.Any);
                float cellRadius = cell.size * 1.01f;

                // reset cell side edge sockets
                cell.sideEdgeSocket = new Dictionary<HexagonTileSide, List<HexSideEdgeSocket>>();

                foreach (HexagonTileSide side in neighborSides)
                {
                    if (side == HexagonTileSide.Top || side == HexagonTileSide.Bottom) continue;

                    if (intersectionPointsBySideByCell[cell].ContainsKey(side) == false) intersectionPointsBySideByCell[cell].Add(side, new List<Vector3>());

                    if (cell.sideEdgeSocket.ContainsKey(side) == false) cell.sideEdgeSocket.Add(side, new List<HexSideEdgeSocket>());

                    int _side = (int)side;
                    Vector3[] corners = HexCoreUtil.GetSideCorners(cell, (HexagonSide)side);
                    HashSet<Vector3> addedLookups = new HashSet<Vector3>();

                    foreach (var block in resultsByCell[cell])
                    {
                        List<int> outOfBounds = block.GetOutOfBoundsCorners();
                        if (outOfBounds == null || outOfBounds.Count == 0) continue;

                        foreach (int ix in outOfBounds)
                        {
                            Vector3 intersectionPoint = VectorUtil.FindIntersectionPoint(corners[0], corners[1], block.Position, block.corners[ix]);
                            if (intersectionPoint == Vector3.zero) continue;

                            Vector3 lookup = VectorUtil.PointLookupDefault(intersectionPoint);
                            if (addedLookups.Contains(lookup)) continue;

                            if (
                                VectorUtil.DistanceXZ(intersectionPoint, cell.center) < cellRadius &&
                                VectorUtil.IsPointOnLine(intersectionPoint, corners[0], corners[1]) &&
                                VectorUtil.IsPointWithinPolygon(intersectionPoint, block.corners.Take(4).ToArray())
                            )
                            {
                                addedLookups.Add(lookup);

                                if (intersectionPointsLookup.ContainsKey(lookup) == false)
                                {
                                    intersectionPointsLookup.Add(lookup, intersectionPoint);
                                }
                                else intersectionPoint = intersectionPointsLookup[lookup];

                                intersectionPointsBySideByCell[cell][side].Add(intersectionPoint);

                                HexSideEdgeSocket edgeSocket = new HexSideEdgeSocket(intersectionPoint, HexTileGrid.Calculate_LerpStepOfPoint(intersectionPoint, corners[0], corners[1]));
                                cell.sideEdgeSocket[side].Add(edgeSocket);
                            }
                        }
                    }
                }

                // List<int>[] sideStepProfile = HexagonTileTemplate.Generate_SideEdgeStepProfile(cell);
                // for (var i = 0; i < sideStepProfile.Length; i++)
                // {
                //     if (sideStepProfile[i] == null || sideStepProfile[i].Count == 0) continue;
                //     HexagonSide _side = (HexagonSide)i;
                //     Debug.Log("stepProfile - side: " + _side + ", " + UtilityHelpers.ListToString(sideStepProfile[i]));
                // }
            }

            return intersectionPointsBySideByCell;
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

        public static SurfaceBlock[,,] ClearInnerBlocksAroundPoint(SurfaceBlock[,,] blockGrid, Vector3 point, float radius)
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
                        SurfaceBlock block = blockGrid[x, y, z];
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

        public static SurfaceBlock[,,] ClearInnerBlocks(SurfaceBlock[,,] blockGrid)
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
                        SurfaceBlock block = blockGrid[x, y, z];
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


        public static void DrawGrid(SurfaceBlock[,,] blockGrid, HexagonCellPrototype _highlightedCell = null)
        {
            int gridSizeX = blockGrid.GetLength(0);
            int gridSizeY = blockGrid.GetLength(1);
            int gridSizeZ = blockGrid.GetLength(2);

            bool doOnce = false;

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    for (int z = 0; z < gridSizeZ; z++)
                    {
                        SurfaceBlock block = blockGrid[x, y, z];
                        if (block != null && !block.ignore)
                        {
                            if (_highlightedCell != null && block.owner != _highlightedCell) continue;

                            Gizmos.color = Color.gray;
                            Gizmos.DrawWireSphere(block.Position, 0.15f);

                            if (!doOnce && y == gridSizeY - 1)
                            // if (!doOnce )
                            {
                                doOnce = true;

                                Gizmos.color = Color.black;
                                Gizmos.DrawSphere(block.Position, 0.2f);

                                Gizmos.color = Color.red;
                                Vector3[] neighborCenters = GenerateNeighborCenters(block.Position, block.size * 2);
                                Gizmos.DrawSphere(neighborCenters[(int)SurfaceBlockSide.Bottom], 0.3f);
                            }
                            block.DrawNeighbors();

                            if (block.IsCorner())
                            {
                                Gizmos.color = Color.blue;
                                Gizmos.DrawSphere(block.Position, 0.3f);
                            }

                            if (block.isEntry)
                            {
                                Gizmos.color = Color.green;
                                Gizmos.DrawSphere(block.Position, 0.3f);
                            }
                            if (block.isTileEdge)
                            {
                                Gizmos.color = Color.yellow;
                                Gizmos.DrawSphere(block.Position, 0.3f);
                            }
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



        public static Dictionary<HexagonCellPrototype, GameObject> Generate_MeshObjectsByCell(
            Dictionary<HexagonCellPrototype, List<SurfaceBlock>> resultsByCell,
            GameObject prefab,
            Transform transform,
            List<SurfaceBlockState> filterOnStates,
            Transform folder
        )
        {
            Dictionary<HexagonCellPrototype, GameObject> objectsByCell = new Dictionary<HexagonCellPrototype, GameObject>();
            foreach (var cell in resultsByCell.Keys)
            {
                GameObject gameObject = Generate_MeshObject(resultsByCell[cell], prefab, transform, filterOnStates, folder);
                if (gameObject != null)
                {
                    objectsByCell.Add(cell, gameObject);

                    gameObject.name = "SurfaceBlock_Tile";
                    if (cell.isEdgeCell) gameObject.name += "_Edge";
                }
            }
            return objectsByCell;
        }

        public static GameObject Generate_MeshObject(List<SurfaceBlock> surfaceBlocks, GameObject prefab, Transform transform, List<SurfaceBlockState> filterOnStates, Transform folder)
        {
            if (!prefab)
            {
                Debug.LogError("NO prefab");
                return null;
            }

            bool bFilterOnStates = filterOnStates != null;
            GameObject result = null;
            List<Mesh> meshes = new List<Mesh>();

            foreach (var block in surfaceBlocks)
            {
                if (block != null && block.isEdge && !block.ignore)
                {
                    if (bFilterOnStates && block.HasFilteredState(filterOnStates) == false) continue;

                    Mesh new_Mesh = block.Generate_Mesh(transform);
                    if (new_Mesh != null) meshes.Add(new_Mesh);
                }
            }
            if (meshes.Count == 0) return null;

            Mesh finalMesh = MeshUtil.GenerateMeshFromVertexSurfaces(meshes);
            result = MeshUtil.InstantiatePrefabWithMesh(prefab, finalMesh, transform.position);

            if (folder != null) result.transform.SetParent(folder);
            return result;
        }

        public static List<GameObject> Generate_MeshObjects(SurfaceBlock[,,] blockGrid, GameObject prefab, Transform transform, List<SurfaceBlockState> filterOnStates, Transform folder)
        {
            if (!prefab)
            {
                Debug.LogError("NO prefab");
                return null;
            }

            List<GameObject> objects = new List<GameObject>();
            int gridSizeX = blockGrid.GetLength(0);
            int gridSizeY = blockGrid.GetLength(1);
            int gridSizeZ = blockGrid.GetLength(2);

            bool bFilterOnStates = filterOnStates != null;

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    for (int z = 0; z < gridSizeZ; z++)
                    {
                        SurfaceBlock block = blockGrid[x, y, z];
                        if (block != null && block.isEdge && !block.ignore)
                        {
                            if (bFilterOnStates && block.HasFilteredState(filterOnStates) == false) continue;

                            GameObject new_gameObject = MeshUtil.InstantiatePrefabWithMesh(prefab, block.Generate_Mesh(transform), transform.position);
                            new_gameObject.transform.SetParent(folder);
                            objects.Add(new_gameObject);
                        }
                    }
                }
            }
            return objects;
        }

        public Mesh Generate_Mesh(Transform transform)
        {
            List<Mesh> surfaceMeshes = new List<Mesh>();

            for (var i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i] != null && !neighbors[i].ignore)
                { }
                else
                {
                    SurfaceBlockSide side = (SurfaceBlockSide)i;
                    Vector3[] sideCorners = GetCornersOnSide(side);

                    Mesh surfaceMesh = new Mesh();
                    surfaceMesh.vertices = VectorUtil.InversePointsToArray(sideCorners.ToList(), transform);

                    int vetexLength = sideCorners.Length;

                    if (
                        side == SurfaceBlockSide.Front ||
                        side == SurfaceBlockSide.Back ||
                        side == SurfaceBlockSide.Right ||
                        side == SurfaceBlockSide.Left
                    )
                    {
                        int[] triangles = MeshUtil.GenerateRectangularTriangles(vetexLength);
                        surfaceMesh.triangles = triangles;
                        // Reverse the winding order of the triangles
                        if (side == SurfaceBlockSide.Back)
                        {
                            MeshUtil.ReverseNormals(surfaceMesh);
                            MeshUtil.ReverseTriangles(surfaceMesh); // Updated: Reverse the triangles as well
                        }
                    }
                    else if (side == SurfaceBlockSide.Top)
                    {
                        int[] triangles = MeshUtil.GenerateTriangles(vetexLength);
                        surfaceMesh.triangles = triangles;
                    }
                    else if (side == SurfaceBlockSide.Bottom)
                    {
                        int[] triangles = MeshUtil.GenerateTriangles(vetexLength);
                        surfaceMesh.triangles = triangles;

                        MeshUtil.ReverseNormals(surfaceMesh);
                        MeshUtil.ReverseTriangles(surfaceMesh); // Updated: Reverse the triangles as well
                    }

                    // Set the UVs to the mesh (you can customize this based on your requirements)
                    Vector2[] uvs = new Vector2[vetexLength];
                    for (int j = 0; j < vetexLength; j++)
                    {
                        uvs[j] = new Vector2(sideCorners[j].x, sideCorners[j].y); // Use x and y as UV coordinates
                    }

                    surfaceMesh.uv = uvs;
                    // Recalculate normals and bounds for the surface mesh
                    surfaceMesh.RecalculateNormals();
                    surfaceMesh.RecalculateBounds();

                    surfaceMeshes.Add(surfaceMesh);
                }
            }
            return MeshUtil.GenerateMeshFromVertexSurfaces(surfaceMeshes);
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


        public static Vector3 GetCluster_LengthWidthHeight(List<SurfaceBlock> cluster)
        {
            if (cluster.Count == 0)
                return Vector3.zero;

            Vector3 minPosition = cluster[0].Position;
            Vector3 maxPosition = cluster[0].Position;

            foreach (SurfaceBlock block in cluster)
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


        public static List<List<SurfaceBlock>> GetConsecutiveClusters(
            SurfaceBlock[,,] blockGrid,
            int max,
            List<SurfaceBlockState> filterOnStates,
            Vector3 blockGrid_cluster_LWH_Min,
            CellSearchPriority searchPriority = CellSearchPriority.SideNeighbors
        )
        {
            HashSet<Vector3> visited = new HashSet<Vector3>();
            List<List<SurfaceBlock>> clusters = new List<List<SurfaceBlock>>();

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
                        if (block == null) continue;

                        if (block.HasFilteredState(filterOnStates) == false) continue;

                        if (visited.Contains(block.Lookup()) == false)
                        {
                            List<SurfaceBlock> cluster = GetConsecutiveNeighborsCluster(
                                block,
                                999,
                                searchPriority,
                                filterOnStates,
                                visited
                            );

                            if (cluster.Count > 0)
                            {
                                Vector3 lwh = GetCluster_LengthWidthHeight(cluster);
                                if (
                                    lwh.z >= blockGrid_cluster_LWH_Min.z &&

                                   (lwh.x >= blockGrid_cluster_LWH_Min.x ||
                                    lwh.y >= blockGrid_cluster_LWH_Min.y)
                                    )
                                {
                                    clusters.Add(cluster);
                                    // Debug.Log("surfaceblock cluster LWH: " + lwh + ", members: " + cluster.Count + ", blockGrid_cluster_LWH_Min: " + blockGrid_cluster_LWH_Min);
                                }
                            }
                            if (clusters.Count >= max) return clusters;
                        }

                    }
                }
            }
            return clusters;
        }

        public static List<SurfaceBlock> GetConsecutiveNeighborsCluster(
            SurfaceBlock start,
            int maxMembers,
            CellSearchPriority searchPriority = CellSearchPriority.SideNeighbors,
            List<SurfaceBlockState> filterOnStates = null,
             HashSet<Vector3> visited = null
        )
        {
            List<SurfaceBlock> cluster = new List<SurfaceBlock>();

            if (visited == null) visited = new HashSet<Vector3>();

            if (filterOnStates == null)
            {
                filterOnStates = new List<SurfaceBlockState>();
                // {
                //     BlockState.Unset
                // };
            }

            RecursivelyFindNeighborsForCluster(start, maxMembers, cluster, visited, searchPriority, filterOnStates);
            return cluster;
        }

        private static void RecursivelyFindNeighborsForCluster(
            SurfaceBlock current,
            int maxMembers,
            List<SurfaceBlock> cluster,
            HashSet<Vector3> visited,
            CellSearchPriority searchPriority = CellSearchPriority.SideNeighbors,
            List<SurfaceBlockState> filterOnStates = null
        )
        {
            cluster.Add(current);
            visited.Add(current.Lookup());

            if (cluster.Count >= maxMembers) return;

            int nLength = current.neighbors.Length;
            int mod = searchPriority == CellSearchPriority.LayerNeighbors ? 4 : 0;
            for (var i = 0; i < nLength; i++)
            {
                if (cluster.Count > maxMembers) break;
                int ix = (i + mod) % nLength;
                SurfaceBlock neighbor = current.neighbors[ix];
                if (neighbor == null) continue;

                if (filterOnStates != null && neighbor.HasFilteredState(filterOnStates) == false) continue;

                if (!visited.Contains(neighbor.Lookup()))
                {
                    RecursivelyFindNeighborsForCluster(neighbor, maxMembers, cluster, visited, searchPriority, filterOnStates);
                }
            }
        }

        // public void DrawSurfaceBlock()
        // {
        //     if (neighbors[(int)SurfaceBlockSide.Front] == null)
        //     {
        //         Vector3[] frontCorners = GetCornersOnSide(SurfaceBlockSide.Front);
        //         Gizmos.DrawLine(frontCorners[0], frontCorners[1]);
        //         Gizmos.DrawLine(frontCorners[1], frontCorners[3]);
        //         Gizmos.DrawLine(frontCorners[3], frontCorners[2]);
        //         Gizmos.DrawLine(frontCorners[2], frontCorners[0]);
        //     }

        //     if (neighbors[(int)SurfaceBlockSide.Right] == null)
        //     {
        //         Vector3[] rightCorners = GetCornersOnSide(SurfaceBlockSide.Right);
        //         Gizmos.DrawLine(rightCorners[0], rightCorners[1]);
        //         Gizmos.DrawLine(rightCorners[1], rightCorners[3]);
        //         Gizmos.DrawLine(rightCorners[3], rightCorners[2]);
        //         Gizmos.DrawLine(rightCorners[2], rightCorners[0]);
        //     }

        //     if (neighbors[(int)SurfaceBlockSide.Back] == null)
        //     {
        //         Vector3[] backCorners = GetCornersOnSide(SurfaceBlockSide.Back);
        //         Gizmos.DrawLine(backCorners[0], backCorners[1]);
        //         Gizmos.DrawLine(backCorners[1], backCorners[3]);
        //         Gizmos.DrawLine(backCorners[3], backCorners[2]);
        //         Gizmos.DrawLine(backCorners[2], backCorners[0]);
        //     }

        //     if (neighbors[(int)SurfaceBlockSide.Left] == null)
        //     {
        //         Vector3[] leftCorners = GetCornersOnSide(SurfaceBlockSide.Left);
        //         Gizmos.DrawLine(leftCorners[0], leftCorners[1]);
        //         Gizmos.DrawLine(leftCorners[1], leftCorners[3]);
        //         Gizmos.DrawLine(leftCorners[3], leftCorners[2]);
        //         Gizmos.DrawLine(leftCorners[2], leftCorners[0]);
        //     }

        //     if (neighbors[(int)SurfaceBlockSide.Top] == null)
        //     {
        //         Vector3[] topCorners = GetCornersOnSide(SurfaceBlockSide.Top);
        //         Gizmos.DrawLine(topCorners[0], topCorners[1]);
        //         Gizmos.DrawLine(topCorners[1], topCorners[2]);
        //         Gizmos.DrawLine(topCorners[2], topCorners[3]);
        //         Gizmos.DrawLine(topCorners[3], topCorners[0]);
        //     }

        //     if (neighbors[(int)SurfaceBlockSide.Bottom] == null)
        //     {
        //         Vector3[] bottomCorners = GetCornersOnSide(SurfaceBlockSide.Bottom);
        //         Gizmos.DrawLine(bottomCorners[0], bottomCorners[1]);
        //         Gizmos.DrawLine(bottomCorners[1], bottomCorners[2]);
        //         Gizmos.DrawLine(bottomCorners[2], bottomCorners[3]);
        //         Gizmos.DrawLine(bottomCorners[3], bottomCorners[0]);
        //     }
        // }

        // public static SurfaceBlock[,,] CreateSurfaceBlocks(Vector3[,,] pointsMatrix, float size)
        // {
        //     int sizeX = pointsMatrix.GetLength(0);
        //     int sizeY = pointsMatrix.GetLength(1);
        //     int sizeZ = pointsMatrix.GetLength(2);

        //     SurfaceBlock[,,] surfaceBlocks = new SurfaceBlock[sizeX, sizeY, sizeZ];
        //     Dictionary<Vector3, SurfaceBlock> centerLookup = new Dictionary<Vector3, SurfaceBlock>();
        //     List<SurfaceBlock> neighborsToFill = new List<SurfaceBlock>();
        //     Dictionary<Vector3, Vector3> cornerLookups = new Dictionary<Vector3, Vector3>();

        //     for (int x = 0; x < sizeX; x++)
        //     {
        //         for (int y = 0; y < sizeY; y++)
        //         {
        //             for (int z = 0; z < sizeZ; z++)
        //             {
        //                 Vector3 position = pointsMatrix[x, y, z];
        //                 SurfaceBlock surfaceBlock = new SurfaceBlock(position, size);
        //                 surfaceBlocks[x, y, z] = surfaceBlock;


        //                 Vector3 lookup = VectorUtil.PointLookupDefault(position);
        //                 if (centerLookup.ContainsKey(lookup) == false)
        //                 {
        //                     centerLookup.Add(lookup, surfaceBlock);
        //                 }
        //                 else
        //                 {
        //                     Debug.Log("lookup already exists: " + lookup + ", y: " + y);
        //                 }

        //                 // Generate corners for the current surfaceBlock
        //                 Vector3[] corners = CreateCorners(position, size);
        //                 for (var i = 0; i < corners.Length; i++)
        //                 {
        //                     Vector3 cornerLookup = VectorUtil.PointLookupDefault(corners[i]);
        //                     if (cornerLookups.ContainsKey(cornerLookup))
        //                     {
        //                         corners[i] = cornerLookups[cornerLookup];
        //                     }
        //                     else cornerLookups.Add(cornerLookup, corners[i]);
        //                 }
        //                 surfaceBlock.corners = corners;
        //                 neighborsToFill.Add(surfaceBlock);
        //             }
        //         }
        //     }

        //     foreach (var item in neighborsToFill)
        //     {
        //         Vector3[] neighborCenters = GenerateNeighborCenters(item.Position, 2);
        //         for (var i = 0; i < neighborCenters.Length; i++)
        //         {
        //             Vector3 lookup = VectorUtil.PointLookupDefault(neighborCenters[i]);
        //             if (centerLookup.ContainsKey(lookup) && centerLookup[lookup] != item) item.neighbors[i] = centerLookup[lookup];
        //         }
        //     }

        //     return surfaceBlocks;
        // }

    }


    public class SurfaceBlockGrid
    {
        public SurfaceBlock[,,] grid;
        public Dictionary<Vector3, SurfaceBlock> centerLookups = new Dictionary<Vector3, SurfaceBlock>();
        public Dictionary<Vector3, Vector3> cornerLookups = new Dictionary<Vector3, Vector3>();

        public void CreateSurfaceBlocks(Vector3[,,] pointsMatrix, List<Bounds> bounds, float size)
        {
            int sizeX = pointsMatrix.GetLength(0);
            int sizeY = pointsMatrix.GetLength(1);
            int sizeZ = pointsMatrix.GetLength(2);
            SurfaceBlock[,,] surfaceBlockGrid = new SurfaceBlock[sizeX, sizeY, sizeZ];
            Dictionary<Vector3, SurfaceBlock> new_centerLookups = new Dictionary<Vector3, SurfaceBlock>();
            Dictionary<Vector3, Vector3> new_cornerLookups = new Dictionary<Vector3, Vector3>();
            List<SurfaceBlock> neighborsToFill = new List<SurfaceBlock>();

            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        Vector3 position = pointsMatrix[x, y, z];

                        if (VectorUtil.IsPointWithinBounds(bounds, position))
                        {
                            SurfaceBlock surfaceBlock = new SurfaceBlock(position, size);
                            surfaceBlockGrid[x, y, z] = surfaceBlock;
                            neighborsToFill.Add(surfaceBlock);
                            Vector3 lookup = VectorUtil.PointLookupDefault(position);
                            if (new_centerLookups.ContainsKey(lookup) == false)
                            {
                                new_centerLookups.Add(lookup, surfaceBlock);
                            }
                            else Debug.LogError("lookup already exists: " + lookup + ", y: " + y);

                            // Generate corners for the current surfaceBlock
                            Vector3[] corners = SurfaceBlock.CreateCorners(position, size);
                            for (var i = 0; i < corners.Length; i++)
                            {
                                Vector3 cornerLookup = VectorUtil.PointLookupDefault(corners[i]);
                                if (new_cornerLookups.ContainsKey(cornerLookup))
                                {
                                    corners[i] = new_cornerLookups[cornerLookup];
                                }
                                else new_cornerLookups.Add(cornerLookup, corners[i]);
                            }

                            surfaceBlock.SetCorners(corners);
                        }
                        else
                        {
                            surfaceBlockGrid[x, y, z] = null;
                        }
                    }
                }
            }

            foreach (var item in neighborsToFill)
            {
                Vector3[] neighborCenters = SurfaceBlock.GenerateNeighborCenters(item.Position, size * 2);
                int found = 0;
                for (var i = 0; i < neighborCenters.Length; i++)
                {
                    Vector3 lookup = VectorUtil.PointLookupDefault(neighborCenters[i]);
                    if (centerLookups.ContainsKey(lookup) && centerLookups[lookup] != item)
                    {
                        item.neighbors[i] = centerLookups[lookup];
                        found++;
                    }
                }
                if (found < 6) item.isEdge = true;
            }

            grid = surfaceBlockGrid;
            centerLookups = new_centerLookups;
            cornerLookups = new_cornerLookups;
        }


    }
}