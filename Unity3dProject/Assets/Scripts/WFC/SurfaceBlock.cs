using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using WFCSystem;
using System.Linq;

namespace ProceduralBase
{
    // public enum CubeCorners { TopLeftFront, TopRightFront, BottomLeftFront, BottomRightFront, TopLeftBack, TopRightBack, BottomLeftBack, BottomRightBack }
    public enum SurfaceBlockSide { Front = 0, Right, Back, Left, Top, Bottom, }
    public enum SurfaceBlockState { Unset = 0, Edge = 1, Corner, Base, Top, Entry, Ignore, Highlight, TileEdge, TileInnerEdge }
    public enum SocketFace { Blank, OpenFace, ClosedFace }
    public enum BoundsType { Rectangle = 0, Bounds, Hexagon, Sphere, Pyramid, Cone }

    public class BoundsShapeBlock
    {
        public BoundsShapeBlock(Bounds _bounds)
        {
            boundsType = BoundsType.Bounds;
            Center = _bounds.center;
            bounds = _bounds;

            edgePoints = new List<Vector3>();
            edgePoints.AddRange(VectorUtil.GetBoundsCorners(bounds));
            uid = UtilityHelpers.GenerateUniqueID($"{boundsType}");
        }
        public BoundsShapeBlock(RectangleBounds rect)
        {
            boundsType = BoundsType.Rectangle;
            Center = rect.Position;
            dimensions = rect.dimensions;

            edgePoints = new List<Vector3>();
            edgePoints.AddRange(rect.corners);
            uid = UtilityHelpers.GenerateUniqueID($"{boundsType}");
        }

        public BoundsShapeBlock(Vector3 _center, float _radius, int densityMultiplier = 6)
        {
            boundsType = BoundsType.Sphere;
            Center = _center;
            radius = _radius;
            edgePoints = GenerateSphereEdgePoints(_center, _radius, densityMultiplier);
            uid = UtilityHelpers.GenerateUniqueID($"{boundsType}");
        }

        public BoundsShapeBlock(HexagonCellPrototype cell, float sizeOffsetMult = 1f, float heightMult = 1f, bool useBuildingNodeEdges = false)
        {
            boundsType = BoundsType.Hexagon;
            Center = cell.center;
            radius = ((cell.size * sizeOffsetMult) * 0.98f);
            dimensions = new Vector3(radius, (cell.layerOffset * heightMult), radius);

            edgePoints = new List<Vector3>();
            uid = UtilityHelpers.GenerateUniqueID($"{boundsType}");

            if (useBuildingNodeEdges && cell.buildingNodeEdgePoints != null)
            {
                if (sizeOffsetMult != 1)
                {
                    List<Vector3> scaledPoints = VectorUtil.ScaleShape(cell.buildingNodeEdgePoints.ToList(), sizeOffsetMult, cell.center);
                    edgePoints.AddRange(scaledPoints);
                }
                else edgePoints.AddRange(cell.buildingNodeEdgePoints);
            }
            else
            {
                if (sizeOffsetMult != 1)
                {
                    edgePoints.AddRange(HexCoreUtil.GenerateHexagonPoints(cell.center, cell.size * sizeOffsetMult));
                }
                else edgePoints.AddRange(cell.cornerPoints);
            }
        }

        public string uid { get; private set; }
        public BoundsType boundsType { get; private set; }
        public Vector3 Center { get; private set; }
        public List<Vector3> edgePoints { get; private set; }
        public Vector3 dimensions { get; private set; } // LWH 
        public float radius { get; private set; }
        public Bounds bounds { get; private set; }

        public static List<Vector3> GenerateSphereEdgePoints(Vector3 center, float radius, int densityMultiplier)
        {
            List<Vector3> edgePoints = new List<Vector3>();

            int density = Mathf.Max(4, densityMultiplier);

            float angleIncrement = 360f / density;
            float currentAngle = 0f;

            while (currentAngle < 360f)
            {
                float radianAngle = currentAngle * Mathf.Deg2Rad;

                float x = center.x + radius * Mathf.Cos(radianAngle);
                float y = center.y + radius * Mathf.Sin(radianAngle);
                float z = center.z;

                Vector3 point = new Vector3(x, y, z);
                edgePoints.Add(point);

                currentAngle += angleIncrement;
            }
            return edgePoints;
        }

        public bool IsWithinBounds(Vector3 point) => IsWithinBounds(this, point);

        public static bool IsWithinBounds(BoundsShapeBlock boundsShapeBlock, Vector3 point)
        {
            // Debug.Log("boundsType: " + boundsShapeBlock.boundsType);
            switch (boundsShapeBlock.boundsType)
            {
                case BoundsType.Sphere:
                    // Debug.Log("boundsShapeBlock.radius: " + boundsShapeBlock.radius);
                    return Vector3.Distance(point, boundsShapeBlock.Center) < boundsShapeBlock.radius * 0.99f;
                case BoundsType.Bounds:
                    return VectorUtil.IsPointWithinBounds(boundsShapeBlock.bounds, point);
                case BoundsType.Rectangle:
                    return RectangleBounds.IsPointWithinBounds(point, boundsShapeBlock.edgePoints.ToArray());
                case BoundsType.Hexagon:
                    return VectorUtil.IsPointWithinPolygon(point, boundsShapeBlock.edgePoints, boundsShapeBlock.dimensions.y);
                default:
                    return Vector3.Distance(point, boundsShapeBlock.Center) < boundsShapeBlock.radius * 0.99f;
                    // return VectorUtil.IsPointWithinPolygon_V2(point, boundsShapeBlock.edgePoints);
            }
        }

        public static List<Vector2> GetIntersectingHexLookups(
            BoundsShapeBlock boundsShape,
            int cellSize,
            Dictionary<Vector2, Dictionary<string, BoundsShapeBlock>> boundsShapesByCellLookup = null
        )
        {
            List<Vector2> hexLookups = new List<Vector2>();
            HashSet<Vector2> added = new HashSet<Vector2>();

            foreach (var edgePoint in boundsShape.edgePoints)
            {
                Vector3 center = HexCoreUtil.Calculate_ClosestHexCenter(edgePoint, cellSize);
                Vector2 lookup = HexCoreUtil.Calculate_CenterLookup(center, cellSize);

                if (added.Contains(lookup) == false)
                {
                    added.Add(lookup);
                    hexLookups.Add(lookup);
                }
                if (boundsShapesByCellLookup != null)
                {
                    if (boundsShapesByCellLookup.ContainsKey(lookup) == false) boundsShapesByCellLookup.Add(lookup, new Dictionary<string, BoundsShapeBlock>());
                    if (boundsShapesByCellLookup[lookup].ContainsKey(boundsShape.uid) == false) boundsShapesByCellLookup[lookup].Add(boundsShape.uid, boundsShape);
                }
            }
            return hexLookups;
        }

        public void Draw()
        {
            if (boundsType == BoundsType.Sphere) Gizmos.DrawWireSphere(Center, radius);
        }

        public void DrawPoints(float size = 0.3f)
        {
            for (int i = 0; i < edgePoints.Count; i++)
            {
                Vector3 pointA = edgePoints[i];
                Vector3 pointB = edgePoints[(i + 1) % edgePoints.Count];
                Gizmos.DrawLine(pointA, pointB);

                Gizmos.DrawSphere(Center, size);
            }
            // foreach (var item in edgePoints)
            // {
            //     Gizmos.DrawSphere(item, size);
            // }
        }
    }


    public class SurfaceBlock
    {
        public Vector3 Position { get; private set; }
        public float terrainHeight { get; private set; }
        public Vector3 Lookup() => VectorUtil.PointLookupDefault(Position);
        public SurfaceBlock(Vector3 position, float _size)
        {
            Position = position;
            size = _size;
        }
        public HexagonCellPrototype owner { get; private set; } = null;
        // public Vector2 nodeCellLookup { get; private set; } = Vector2.positiveInfinity;
        // public bool HasNodeLookup() => nodeCellLookup != Vector2.positiveInfinity;

        public float size;
        public bool isEdge { get; private set; }
        public bool isTileEdge { get; private set; }
        public bool isTileInnerEdge { get; private set; }
        public bool ignore { get; private set; }
        public bool isEntry;

        public SurfaceBlock[] neighbors = new SurfaceBlock[6] { null, null, null, null, null, null };
        public Vector3[] corners { get; private set; } = new Vector3[8];
        public void SetCorners(Vector3[] _value)
        { corners = _value; }

        public Vector2 BlockBottom_Top() => new Vector2(Position.y - (size / 2), Position.y + (size / 2));

        public void SetIgnored(bool value)
        {
            ignore = value;
            if (ignore && states.Contains(SurfaceBlockState.Ignore) == false) states.Add(SurfaceBlockState.Ignore);
            else if (!ignore && states.Contains(SurfaceBlockState.Ignore)) states.Remove(SurfaceBlockState.Ignore);
        }

        public bool IsEdge()
        {
            for (var i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i] == null || neighbors[i].ignore) return true;
            }
            return false;
        }
        public void SetEdge(bool value)
        {
            isEdge = value;
            if (isEdge && states.Contains(SurfaceBlockState.Edge) == false) states.Add(SurfaceBlockState.Edge);
            else if (!isEdge && states.Contains(SurfaceBlockState.Edge)) states.Remove(SurfaceBlockState.Edge);
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

        public bool IsTileTopEdge() => (neighbors[(int)SurfaceBlockSide.Top] == null || neighbors[(int)SurfaceBlockSide.Top].owner != owner);
        public bool IsTileBottomEdge() => (neighbors[(int)SurfaceBlockSide.Bottom] == null || neighbors[(int)SurfaceBlockSide.Bottom].owner != owner);
        public bool HasDifferentOwner(SurfaceBlock neighbor) => (neighbor != null && neighbor.owner != owner && neighbor.owner != null);
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

        public bool IsMeshable() => (isEdge && ignore == false);

        public static Vector3[] UpdateOutOfBoundsCorners(Vector3 center, Vector3[] blockCorners, List<Vector3> parentCorners)
        {
            // Move any block corner that is out of bounds into bounds towards the center
            for (int i = 0; i < blockCorners.Length; i++)
            {
                Vector3 corner = blockCorners[i];
                if (!VectorUtil.IsPointWithinPolygon(corner, parentCorners))
                {
                    // Find the nearest edge points of the bounds to the corner
                    Vector3 closestEdgePointA = Vector3.zero;
                    Vector3 closestEdgePointB = Vector3.zero;
                    float closestDistanceSqr = float.MaxValue;

                    for (int j = 0; j < parentCorners.Count; j++)
                    {
                        Vector3 edgePointA = parentCorners[j];
                        Vector3 edgePointB = parentCorners[(j + 1) % parentCorners.Count];
                        Vector3 closestPointOnEdge = GetClosestPointOnLineSegment(corner, edgePointA, edgePointB);
                        float distanceSqr = (corner - closestPointOnEdge).sqrMagnitude;

                        if (distanceSqr < closestDistanceSqr)
                        {
                            closestDistanceSqr = distanceSqr;
                            closestEdgePointA = edgePointA;
                            closestEdgePointB = edgePointB;
                        }
                    }

                    // Move the corner to the nearest point on the edge of the bounds
                    Vector3 nearestPoint = GetClosestPointOnLineSegment(corner, closestEdgePointA, closestEdgePointB);
                    blockCorners[i] = nearestPoint;
                }
            }

            return blockCorners;
        }

        // Helper method to find the closest point on a line segment to a given point
        public static Vector3 GetClosestPointOnLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 lineDirection = lineEnd - lineStart;
            float lineLengthSqr = lineDirection.sqrMagnitude;

            if (lineLengthSqr == 0f)
                return lineStart;

            float t = Mathf.Clamp01(Vector3.Dot(point - lineStart, lineDirection) / lineLengthSqr);
            return lineStart + t * lineDirection;
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


        public static (Vector3, SocketFace) GetFaceProfie(SurfaceBlock block, SurfaceBlock foreignNeighbor)
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
                item.SetEdge((found < 6));
            }

            return surfaceBlocks;
        }


        public static SurfaceBlock[,,] Generate_TerrainBlockGrid(
            Bounds bounds,
            float blockSize,
            float baseElevation,
            List<LayeredNoiseOption> layerdNoises_terrain,
            float terrainHeight,
            Dictionary<Vector3, SurfaceBlock> blockCenterLookup,
            HexGrid hexNodeGrid,
            Dictionary<HexagonCellPrototype, List<SurfaceBlock>> blocksByTileCell = null,
            Dictionary<Vector2, Dictionary<string, BoundsShapeBlock>> boundsShapesByCellLookup = null
        )
        {
            float maxHeight = 2f;
            int gridSizeX = Mathf.CeilToInt(bounds.size.x / blockSize);
            int gridSizeZ = Mathf.CeilToInt(bounds.size.z / blockSize);

            float spacingX = bounds.size.x / gridSizeX;
            float spacingZ = bounds.size.z / gridSizeZ;
            float _size = Mathf.Min(spacingX, spacingZ);
            _size = UtilityHelpers.RoundToNearestStep(_size, 0.2f);

            int gridSizeY = Mathf.FloorToInt((maxHeight - baseElevation) / _size) + 1;

            // Calculate the starting y position for the grid
            float startY = baseElevation + (_size * 0.5f); // Offset by half the spacing to center the points

            SurfaceBlock[,,] surfaceBlocks = new SurfaceBlock[gridSizeX, gridSizeY, gridSizeZ];

            List<SurfaceBlock> neighborsToFill = new List<SurfaceBlock>();
            Dictionary<Vector3, Vector3> cornerLookups = new Dictionary<Vector3, Vector3>();
            if (blockCenterLookup == null) blockCenterLookup = new Dictionary<Vector3, SurfaceBlock>();

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    for (int z = 0; z < gridSizeZ; z++)
                    {
                        // Calculate the position of the current point
                        float xPos = bounds.min.x + (x * _size) + (_size * 0.5f); // Offset by half the spacing to center the points
                        float yPos = startY + (y * _size);
                        float zPos = bounds.min.z + (z * _size) + (_size * 0.5f); // Offset by half the spacing to center the points
                        // Set the point in the grid matrix


                        Vector3 position = new Vector3(xPos, yPos, zPos);
                        SurfaceBlock surfaceBlock = new SurfaceBlock(position, _size);

                        Vector2Int noiseCoordinate = new Vector2Int((int)position.x, (int)position.z);
                        surfaceBlock.terrainHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate(noiseCoordinate.x, noiseCoordinate.y, terrainHeight, layerdNoises_terrain);

                        // Evaluate Node Cell bounds
                        bool inBounds = false;

                        HexagonCellPrototype nodeCell = hexNodeGrid.GetContainingCell(surfaceBlock);
                        if (nodeCell != null)
                        {
                            if (HexCellUtil.IsPointWithinCellCornerBounds(position, nodeCell))
                            {
                                if (nodeCell.IsPath())
                                {
                                    if (nodeCell.buildingNodeClearBounds != null)
                                    {
                                        bool keep = true;
                                        foreach (var boundsShapeBlock in nodeCell.buildingNodeClearBounds)
                                        {
                                            if (BoundsShapeBlock.IsWithinBounds(boundsShapeBlock, position))
                                            {
                                                keep = false;
                                                break;
                                            }
                                        }
                                        inBounds = keep;
                                    }
                                }
                                else inBounds = true;
                            }
                        }


                        if (!inBounds)
                        {
                            surfaceBlocks[x, y, z] = null;
                            continue;
                        }

                        surfaceBlocks[x, y, z] = surfaceBlock;
                        neighborsToFill.Add(surfaceBlock);

                        // HexagonCellPrototype tileCellOwner = hexTileGrid.GetContainingCell(surfaceBlock);
                        // if (tileCellOwner != null)
                        // {
                        //     if (blocksByTileCell.ContainsKey(tileCellOwner) == false) blocksByTileCell.Add(tileCellOwner, new List<SurfaceBlock>());
                        //     surfaceBlock.owner = tileCellOwner;
                        //     blocksByTileCell[tileCellOwner].Add(surfaceBlock);
                        // }
                        // else Debug.LogError("tileCellOwner Not found for surfaceBlock");

                        Vector3 lookup = VectorUtil.PointLookupDefault(position);
                        if (blockCenterLookup.ContainsKey(lookup) == false)
                        {
                            blockCenterLookup.Add(lookup, surfaceBlock);
                        }
                        else Debug.LogError("lookup already exists: " + lookup + ", y: " + y);

                        // Generate corners for the current surfaceBlock
                        Vector3[] corners = CreateCorners(position, _size);
                        for (var i = 0; i < corners.Length; i++)
                        {
                            Vector3 cornerLookup = VectorUtil.PointLookupDefault(corners[i]);
                            if (cornerLookups.ContainsKey(cornerLookup))
                            {
                                corners[i] = cornerLookups[cornerLookup];
                            }
                            else
                            {
                                //Top
                                if (i >= 4 && i <= 7)
                                {
                                    float tHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)corners[i].x, (int)corners[i].z, terrainHeight, layerdNoises_terrain);
                                    Vector3 new_pos = corners[i];
                                    new_pos.y += tHeight;
                                    corners[i] = new_pos;
                                }

                                cornerLookups.Add(cornerLookup, corners[i]);
                            }
                        }

                        surfaceBlock.corners = corners;
                    }
                }
            }

            foreach (SurfaceBlock item in neighborsToFill)
            {
                Vector3[] neighborCenters = GenerateNeighborCenters(item.Position, _size * 2);
                int found = 0;
                for (var i = 0; i < neighborCenters.Length; i++)
                {
                    Vector3 lookup = VectorUtil.PointLookupDefault(neighborCenters[i]);
                    if (blockCenterLookup.ContainsKey(lookup) && blockCenterLookup[lookup] != item)
                    {
                        item.neighbors[i] = blockCenterLookup[lookup];
                        found++;
                    }
                }
                item.SetEdge((found < 6));

                if (!item.ignore && !item.IsRoof() && !item.IsFloor())
                {
                    List<Vector2> nearestCellLookups = HexCoreUtil.Calculate_ClosestHexLookups_X7(item.Position, 12);
                    foreach (Vector2 currentLookup in nearestCellLookups)
                    {
                        if (boundsShapesByCellLookup.ContainsKey(currentLookup) == false) continue;
                        bool exit = false;
                        foreach (BoundsShapeBlock boundsShape in boundsShapesByCellLookup[currentLookup].Values)
                        {
                            if (BoundsShapeBlock.IsWithinBounds(boundsShape, item.Position))
                            {
                                item.SetIgnored(true);
                                exit = true;
                                break;
                            }
                        }
                        if (exit) break;
                    }
                }
                item.SetTileEdge(item.IsTileEdge());
                item.SetTileInnerEdge(item.IsTileInnerEdge());
            }

            return surfaceBlocks;
        }


        public static Dictionary<Vector3, SurfaceBlock> CreateSurfaceBlocks_V3(
            Bounds bounds,
            float blockSize,
            float baseElevation,
            float maxHeight,
            HexGrid hexNodeGrid,
            HexGrid hexTileGrid,
            Dictionary<HexagonCellPrototype, List<SurfaceBlock>> blocksByTileCell = null,
            Dictionary<Vector2, Dictionary<string, BoundsShapeBlock>> boundsShapesByCellLookup = null,
            bool logErrors = true
        )
        {
            int gridSizeX = Mathf.CeilToInt(bounds.size.x / blockSize);
            int gridSizeZ = Mathf.CeilToInt(bounds.size.z / blockSize);

            float spacingX = bounds.size.x / gridSizeX;
            float spacingZ = bounds.size.z / gridSizeZ;
            float _size = Mathf.Min(spacingX, spacingZ);
            _size = UtilityHelpers.RoundToNearestStep(_size, 0.2f);

            int gridSizeY = Mathf.FloorToInt((maxHeight - baseElevation) / _size) + 1;

            // Calculate the starting y position for the grid
            float startY = baseElevation + (_size * 0.5f); // Offset by half the spacing to center the points

            List<SurfaceBlock> neighborsToFill = new List<SurfaceBlock>();
            Dictionary<Vector3, Vector3> cornerLookups = new Dictionary<Vector3, Vector3>();
            Dictionary<Vector3, SurfaceBlock> blockCenterLookups = new Dictionary<Vector3, SurfaceBlock>();
            // Debug.LogError("[gridSizeX, gridSizeY, gridSizeZ]: " + gridSizeX + ", " + gridSizeY + " , " + gridSizeZ + ", _size: " + _size + ", hexNodeGrid: " + hexNodeGrid.GetBaseLayerCells().Count);

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    for (int z = 0; z < gridSizeZ; z++)
                    {
                        // Calculate the position of the current point
                        float xPos = bounds.min.x + (x * _size) + (_size * 0.5f); // Offset by half the spacing to center the points
                        float yPos = startY + (y * _size);
                        float zPos = bounds.min.z + (z * _size) + (_size * 0.5f); // Offset by half the spacing to center the points
                        // Set the point in the grid matrix
                        Vector3 position = new Vector3(xPos, yPos, zPos);

                        SurfaceBlock surfaceBlock = new SurfaceBlock(position, _size);

                        // Evaluate Node Cell bounds
                        bool inBounds = false;

                        HexagonCellPrototype nodeCell = hexNodeGrid.GetContainingCell(surfaceBlock, logErrors);
                        if (nodeCell != null)
                        {
                            if (HexCellUtil.IsPointWithinCellCornerBounds(position, nodeCell))
                            {
                                if (nodeCell.IsPath())
                                {
                                    if (nodeCell.buildingNodeClearBounds != null)
                                    {
                                        bool keep = true;
                                        foreach (var boundsShapeBlock in nodeCell.buildingNodeClearBounds)
                                        {
                                            if (BoundsShapeBlock.IsWithinBounds(boundsShapeBlock, position))
                                            {
                                                keep = false;
                                                break;
                                            }
                                        }
                                        inBounds = keep;
                                    }
                                }
                                else inBounds = true;
                            }
                        }

                        if (!inBounds) continue;

                        // Assign Cell Owner
                        HexagonCellPrototype tileCellOwner = hexTileGrid.GetContainingCell(surfaceBlock);
                        if (tileCellOwner != null)
                        {
                            if (blocksByTileCell.ContainsKey(tileCellOwner) == false) blocksByTileCell.Add(tileCellOwner, new List<SurfaceBlock>());
                            surfaceBlock.owner = tileCellOwner;
                            blocksByTileCell[tileCellOwner].Add(surfaceBlock);
                        }
                        else Debug.LogError("tileCellOwner Not found for surfaceBlock");

                        Vector3 lookup = VectorUtil.PointLookupDefault(position);
                        if (blockCenterLookups.ContainsKey(lookup) == false)
                        {
                            blockCenterLookups.Add(lookup, surfaceBlock);
                            neighborsToFill.Add(surfaceBlock);
                        }
                        else Debug.LogError("lookup already exists: " + lookup + ", y: " + y);

                        // Generate corners for the current surfaceBlock
                        Vector3[] corners = CreateCorners(position, _size);
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
                }
            }

            foreach (SurfaceBlock item in neighborsToFill)
            {
                Vector3[] neighborCenters = GenerateNeighborCenters(item.Position, _size * 2);
                int found = 0;
                for (var i = 0; i < neighborCenters.Length; i++)
                {
                    Vector3 lookup = VectorUtil.PointLookupDefault(neighborCenters[i]);
                    if (blockCenterLookups.ContainsKey(lookup) && blockCenterLookups[lookup] != item)
                    {
                        item.neighbors[i] = blockCenterLookups[lookup];
                        found++;
                    }
                }
                item.SetEdge((found < 6));

                if (!item.ignore && !item.IsRoof() && !item.IsFloor())
                {
                    List<Vector2> nearestCellLookups = HexCoreUtil.Calculate_ClosestHexLookups_X7(item.Position, 12);
                    foreach (Vector2 currentLookup in nearestCellLookups)
                    {
                        if (boundsShapesByCellLookup.ContainsKey(currentLookup) == false) continue;
                        bool exit = false;
                        foreach (BoundsShapeBlock boundsShape in boundsShapesByCellLookup[currentLookup].Values)
                        {
                            if (BoundsShapeBlock.IsWithinBounds(boundsShape, item.Position))
                            {
                                item.SetIgnored(true);
                                exit = true;
                                break;
                            }
                        }
                        if (exit) break;
                    }
                }
                item.SetTileEdge(item.IsTileEdge());
                item.SetTileInnerEdge(item.IsTileInnerEdge());
            }

            Debug.Log("surfaceBlocks: " + neighborsToFill.Count);
            return blockCenterLookups;
        }


        public static SurfaceBlock[,,] CreateSurfaceBlocks_V2(
            Bounds bounds,
            float blockSize,
            float baseElevation,
            float maxHeight,
            Dictionary<Vector3, SurfaceBlock> blockCenterLookup,
            HexGrid hexNodeGrid,
            HexGrid hexTileGrid,
            Dictionary<HexagonCellPrototype, List<SurfaceBlock>> blocksByTileCell = null,
            Dictionary<Vector2, Dictionary<string, BoundsShapeBlock>> boundsShapesByCellLookup = null
        )
        {
            int gridSizeX = Mathf.CeilToInt(bounds.size.x / blockSize);
            int gridSizeZ = Mathf.CeilToInt(bounds.size.z / blockSize);

            float spacingX = bounds.size.x / gridSizeX;
            float spacingZ = bounds.size.z / gridSizeZ;
            float _size = Mathf.Min(spacingX, spacingZ);
            _size = UtilityHelpers.RoundToNearestStep(_size, 0.2f);

            int gridSizeY = Mathf.FloorToInt((maxHeight - baseElevation) / _size) + 1;

            // Calculate the starting y position for the grid
            float startY = baseElevation + (_size * 0.5f); // Offset by half the spacing to center the points

            SurfaceBlock[,,] surfaceBlocks = new SurfaceBlock[gridSizeX, gridSizeY, gridSizeZ];

            List<SurfaceBlock> neighborsToFill = new List<SurfaceBlock>();
            Dictionary<Vector3, Vector3> cornerLookups = new Dictionary<Vector3, Vector3>();
            if (blockCenterLookup == null) blockCenterLookup = new Dictionary<Vector3, SurfaceBlock>();

            // Debug.LogError("[gridSizeX, gridSizeY, gridSizeZ]: " + gridSizeX + ", " + gridSizeY + " , " + gridSizeZ + ", _size: " + _size + ", hexNodeGrid: " + hexNodeGrid.GetBaseLayerCells().Count);

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    for (int z = 0; z < gridSizeZ; z++)
                    {
                        // Calculate the position of the current point
                        float xPos = bounds.min.x + (x * _size) + (_size * 0.5f); // Offset by half the spacing to center the points
                        float yPos = startY + (y * _size);
                        float zPos = bounds.min.z + (z * _size) + (_size * 0.5f); // Offset by half the spacing to center the points
                        // Set the point in the grid matrix
                        Vector3 position = new Vector3(xPos, yPos, zPos);

                        SurfaceBlock surfaceBlock = new SurfaceBlock(position, _size);

                        // Evaluate Node Cell bounds
                        bool inBounds = false;

                        HexagonCellPrototype nodeCell = hexNodeGrid.GetContainingCell(surfaceBlock);
                        if (nodeCell != null)
                        {
                            if (HexCellUtil.IsPointWithinCellCornerBounds(position, nodeCell))
                            {
                                if (nodeCell.IsPath())
                                {
                                    if (nodeCell.buildingNodeClearBounds != null)
                                    {
                                        bool keep = true;
                                        foreach (var boundsShapeBlock in nodeCell.buildingNodeClearBounds)
                                        {
                                            if (BoundsShapeBlock.IsWithinBounds(boundsShapeBlock, position))
                                            {
                                                keep = false;
                                                break;
                                            }
                                        }
                                        inBounds = keep;
                                    }
                                }
                                else inBounds = true;
                            }
                        }

                        if (!inBounds)
                        {
                            surfaceBlocks[x, y, z] = null;
                            continue;
                        }

                        surfaceBlocks[x, y, z] = surfaceBlock;
                        neighborsToFill.Add(surfaceBlock);

                        // Assign Cell Owner
                        // (
                        //     Vector2 cellLookup,
                        //     int cellLayer
                        // ) = HexCoreUtil.Calculate_NearestHexCellLookupData(position, (HexCellSizes)hexTileGrid.cellSize, hexTileGrid.cellLayerOffset);

                        HexagonCellPrototype tileCellOwner = hexTileGrid.GetContainingCell(surfaceBlock);
                        if (tileCellOwner != null)
                        {
                            if (blocksByTileCell.ContainsKey(tileCellOwner) == false) blocksByTileCell.Add(tileCellOwner, new List<SurfaceBlock>());
                            surfaceBlock.owner = tileCellOwner;
                            blocksByTileCell[tileCellOwner].Add(surfaceBlock);
                        }
                        else Debug.LogError("tileCellOwner Not found for surfaceBlock");

                        Vector3 lookup = VectorUtil.PointLookupDefault(position);
                        if (blockCenterLookup.ContainsKey(lookup) == false)
                        {
                            blockCenterLookup.Add(lookup, surfaceBlock);
                        }
                        else Debug.LogError("lookup already exists: " + lookup + ", y: " + y);

                        // Generate corners for the current surfaceBlock
                        Vector3[] corners = CreateCorners(position, _size);
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
                }
            }

            foreach (SurfaceBlock item in neighborsToFill)
            {
                Vector3[] neighborCenters = GenerateNeighborCenters(item.Position, _size * 2);
                int found = 0;
                for (var i = 0; i < neighborCenters.Length; i++)
                {
                    Vector3 lookup = VectorUtil.PointLookupDefault(neighborCenters[i]);
                    if (blockCenterLookup.ContainsKey(lookup) && blockCenterLookup[lookup] != item)
                    {
                        item.neighbors[i] = blockCenterLookup[lookup];
                        found++;
                    }
                }
                item.SetEdge((found < 6));

                if (!item.ignore && !item.IsRoof() && !item.IsFloor())
                {
                    List<Vector2> nearestCellLookups = HexCoreUtil.Calculate_ClosestHexLookups_X7(item.Position, 12);
                    foreach (Vector2 currentLookup in nearestCellLookups)
                    {
                        if (boundsShapesByCellLookup.ContainsKey(currentLookup) == false) continue;
                        bool exit = false;
                        foreach (BoundsShapeBlock boundsShape in boundsShapesByCellLookup[currentLookup].Values)
                        {
                            if (BoundsShapeBlock.IsWithinBounds(boundsShape, item.Position))
                            {
                                item.SetIgnored(true);
                                exit = true;
                                break;
                            }
                        }
                        if (exit) break;
                    }
                }
                item.SetTileEdge(item.IsTileEdge());
                item.SetTileInnerEdge(item.IsTileInnerEdge());
            }

            // Debug.Log("surfaceBlocks: " + neighborsToFill.Count);

            return surfaceBlocks;
        }



        public static SurfaceBlock[,,] CreateSurfaceBlocks(
            Vector3[,,] pointsMatrix,
            float size,
            Dictionary<Vector2, HexagonCellPrototype> baseLayerCells,
            Dictionary<Vector3, SurfaceBlock> centerLookup = null
        )
        {
            if (centerLookup == null) centerLookup = new Dictionary<Vector3, SurfaceBlock>();

            int sizeX = pointsMatrix.GetLength(0);
            int sizeY = pointsMatrix.GetLength(1);
            int sizeZ = pointsMatrix.GetLength(2);
            SurfaceBlock[,,] surfaceBlocks = new SurfaceBlock[sizeX, sizeY, sizeZ];
            List<SurfaceBlock> neighborsToFill = new List<SurfaceBlock>();
            Dictionary<Vector3, Vector3> cornerLookups = new Dictionary<Vector3, Vector3>();

            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        Vector3 position = pointsMatrix[x, y, z];

                        bool inBounds = false;
                        foreach (HexagonCellPrototype cell in baseLayerCells.Values)
                        {
                            float blockTop = position.y + (size * 0.5f);
                            float blockBottom = position.y - (size * 0.5f);

                            if (cell.buildingNodeEdgePoints != null && cell.buildingNodeEdgePoints.Length > 1)
                            {
                                if (VectorUtil.IsPointWithinPolygon(position, cell.buildingNodeEdgePoints))
                                {
                                    HexagonCellPrototype stackBoundsCell = HexagonCellPrototype.FindBoundsCellInLayerStack(new Vector2(blockBottom, blockTop), cell);
                                    if (stackBoundsCell != null && HexCellUtil.IsPointWithinCellCornerBounds(position, stackBoundsCell))
                                    {
                                        if (stackBoundsCell.IsPath())
                                        {
                                            if (stackBoundsCell.buildingNodeClearBounds != null)
                                            {
                                                bool keep = true;
                                                foreach (var boundsShapeBlock in stackBoundsCell.buildingNodeClearBounds)
                                                {
                                                    if (BoundsShapeBlock.IsWithinBounds(boundsShapeBlock, position))
                                                    {
                                                        keep = false;
                                                        break;
                                                    }
                                                }
                                                inBounds = keep;
                                            }
                                            else break;

                                        }
                                        else inBounds = true;
                                    }
                                    else break;
                                }
                            }
                            else if (VectorUtil.IsPointWithinPolygon(position, cell.cornerPoints))
                            {

                                HexagonCellPrototype stackBoundsCell = HexagonCellPrototype.FindBoundsCellInLayerStack(new Vector2(blockBottom, blockTop), cell);
                                // if (stackBoundsCell != null && !stackBoundsCell.IsPath() && HexCellUtil.IsPointWithinCellCornerBounds(position, stackBoundsCell))
                                if (stackBoundsCell != null && HexCellUtil.IsPointWithinCellCornerBounds(position, stackBoundsCell))
                                {
                                    if (stackBoundsCell.IsPath())
                                    {
                                        if (stackBoundsCell.buildingNodeClearBounds != null)
                                        {
                                            bool keep = true;
                                            foreach (var boundsShapeBlock in stackBoundsCell.buildingNodeClearBounds)
                                            {
                                                if (BoundsShapeBlock.IsWithinBounds(boundsShapeBlock, position))
                                                {
                                                    keep = false;
                                                    break;
                                                }
                                            }
                                            inBounds = keep;
                                        }
                                        else break;

                                    }
                                    else inBounds = true;
                                }
                                else break;
                            }
                        }

                        if (!inBounds)
                        {
                            surfaceBlocks[x, y, z] = null;
                            continue;
                        }

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
                }
            }

            foreach (SurfaceBlock item in neighborsToFill)
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

                item.SetEdge((found < 6));
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

        public static void EvaluateNeighbors(Dictionary<Vector3, SurfaceBlock> centerLookup)
        {
            foreach (SurfaceBlock block in centerLookup.Values)
            {
                Vector3[] neighborCenters = GenerateNeighborCenters(block.Position, block.size * 2);
                int found = 0;
                for (var i = 0; i < neighborCenters.Length; i++)
                {
                    Vector3 lookup = VectorUtil.PointLookupDefault(neighborCenters[i]);
                    if (centerLookup.ContainsKey(lookup) && centerLookup[lookup] != block)
                    {
                        block.neighbors[i] = centerLookup[lookup];
                        found++;
                    }
                }
                block.SetEdge(block.IsEdge());
            }
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
                        if (block != null)
                        // if (block != null && !block.ignore)
                        {
                            foreach (int currentLayer in cellsByLayer.Keys)
                            {
                                foreach (var cell in cellsByLayer[currentLayer].Values)
                                {
                                    // if (|| currentLayerBase != currentLayer) currentLayerBase = currentLayer; 

                                    if (VectorUtil.IsBlockWithinVerticalBounds(cell.center.y, cell.layerOffset, block.Position, block.size) && VectorUtil.IsPointWithinPolygon(block.Position, cell.cornerPoints))
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
                        if (block != null)
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


        public static Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, List<SurfaceBlock>>> GetTileInnerEdgesByCellSide(Dictionary<HexagonCellPrototype, List<SurfaceBlock>> surfaceBlocksByCell)
        {
            Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, List<SurfaceBlock>>> tileInnerEdgesByCellSide = new Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, List<SurfaceBlock>>>();
            Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, Dictionary<Vector3, SocketFace>>> tileSocketProfileBySide = new Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, Dictionary<Vector3, SocketFace>>>();

            foreach (var cell in surfaceBlocksByCell.Keys)
            {
                if (tileInnerEdgesByCellSide.ContainsKey(cell) == false) tileInnerEdgesByCellSide.Add(cell, new Dictionary<HexagonTileSide, List<SurfaceBlock>>());

                if (tileSocketProfileBySide.ContainsKey(cell) == false) tileSocketProfileBySide.Add(cell, new Dictionary<HexagonTileSide, Dictionary<Vector3, SocketFace>>());

                HashSet<SurfaceBlock> added = new HashSet<SurfaceBlock>();
                Dictionary<HexagonCellPrototype, List<SurfaceBlock>> edgeByNeighborOwnerCell = new Dictionary<HexagonCellPrototype, List<SurfaceBlock>>();

                foreach (var block in surfaceBlocksByCell[cell])
                {
                    if (added.Contains(block)) continue;

                    if (!block.isTileInnerEdge) continue;

                    Dictionary<SurfaceBlockSide, SocketFace> blockSocketsBySide = new Dictionary<SurfaceBlockSide, SocketFace>();

                    for (var _blockSide = 0; _blockSide < block.neighbors.Length; _blockSide++)
                    {
                        SurfaceBlock neighbor = block.neighbors[_blockSide];
                        if (block.HasDifferentOwner(neighbor))
                        {
                            if (edgeByNeighborOwnerCell.ContainsKey(neighbor.owner) == false) edgeByNeighborOwnerCell.Add(neighbor.owner, new List<SurfaceBlock>());
                            edgeByNeighborOwnerCell[neighbor.owner].Add(block);
                        }
                    }
                    added.Add(block);
                }

                HexagonCellPrototype[] neighborTileSides = cell.GetNeighborTileSides();

                for (int _side = 0; _side < neighborTileSides.Length; _side++)
                {
                    HexagonTileSide side = (HexagonTileSide)_side;
                    HexagonCellPrototype neighbor = neighborTileSides[_side];

                    // List<HexagonTileSide> neighborSides = cell.GetNeighborSidesX8(Filter_CellType.Any);
                    // foreach (HexagonTileSide side in neighborSides)
                    // {
                    //     int _side = (int)side;
                    //     HexagonCellPrototype neighbor = null;

                    //     if (side == HexagonTileSide.Top || side == HexagonTileSide.Bottom)
                    //     {
                    //         neighbor = (side == HexagonTileSide.Bottom) ? cell.layerNeighbors[0] : cell.layerNeighbors[1];
                    //     }
                    //     else
                    //     {
                    // neighbor = cell.neighborsBySide[_side];
                    // }

                    if (tileInnerEdgesByCellSide[cell].ContainsKey(side) == false) tileInnerEdgesByCellSide[cell].Add(side, new List<SurfaceBlock>());

                    if (neighbor != null && edgeByNeighborOwnerCell.ContainsKey(neighbor))
                    {
                        tileInnerEdgesByCellSide[cell][side] = edgeByNeighborOwnerCell[neighbor];
                    }
                    // else
                    // {
                    //     if (neighbor != null && (side == HexagonTileSide.Top || side == HexagonTileSide.Bottom)) Debug.LogError("Neighbor key not found on Side: " + side);
                    // }
                }
            }
            return tileInnerEdgesByCellSide;
        }

        public static void Generate_CellTileSocketProfiles_V2(Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, List<SurfaceBlock>>> tileInnerEdgesByCellSide)
        {
            foreach (var cell in tileInnerEdgesByCellSide.Keys)
            {
                cell.tileSocketProfileBySide = new Dictionary<HexagonTileSide, TileSocketProfile>();

                HexagonCellPrototype[] neighborTileSides = cell.GetNeighborTileSides();

                for (int _side = 0; _side < neighborTileSides.Length; _side++)
                {
                    HexagonTileSide side = (HexagonTileSide)_side;
                    HexagonCellPrototype neighbor = neighborTileSides[_side];

                    if (tileInnerEdgesByCellSide[cell].ContainsKey(side))
                    {
                        TileSocketProfile new_tileSocketProfile = TileSocketProfile.Generate_TileSocketProfile(tileInnerEdgesByCellSide[cell][side], neighbor);
                        cell.tileSocketProfileBySide.Add(side, new_tileSocketProfile);

                        HexagonTileSide relativeSide = HexCoreUtil.GetRelativeHexagonSide(side);

                        if (neighbor == null || tileInnerEdgesByCellSide.ContainsKey(neighbor) == false || tileInnerEdgesByCellSide[neighbor].ContainsKey(relativeSide))
                        {
                            //     Debug.LogError("neighbor not found in tileInnerEdgesByCellSide");
                            continue;
                        }

                        TileSocketProfile neighbor_tileSocketProfile = TileSocketProfile.Generate_TileSocketProfile(tileInnerEdgesByCellSide[neighbor][relativeSide], cell);

                        if (neighbor.tileSocketProfileBySide == null) neighbor.tileSocketProfileBySide = new Dictionary<HexagonTileSide, TileSocketProfile>();
                        neighbor.tileSocketProfileBySide.Add(relativeSide, neighbor_tileSocketProfile);

                        bool incompatibile = TileSocketProfile.IsCompatible(new_tileSocketProfile, neighbor_tileSocketProfile);
                        if (incompatibile)
                        {
                            // Debug.LogError("Incompatibile socket profiles! - sides: " + side + " / " + relativeSide);
                            cell.Highlight(true);
                            neighbor.Highlight(true);
                        }
                    }
                    else
                    {

                        GlobalSockets defaultSocketId = GlobalSockets.Empty_Space;
                        if (neighbor != null)
                        {
                            defaultSocketId = GlobalSockets.InnerCell_Generic;
                        }
                        else
                        {
                            if (side == HexagonTileSide.Bottom)
                            {
                                defaultSocketId = GlobalSockets.Structure_Bottom;
                            }
                            else if (side == HexagonTileSide.Top)
                            {
                                defaultSocketId = GlobalSockets.Structure_Top;
                            }
                            else
                            {
                                defaultSocketId = GlobalSockets.Structure_Outer;
                            }
                        }
                        cell.tileSocketProfileBySide.Add(side, new TileSocketProfile(defaultSocketId));
                    }
                }
            }
        }


        public static Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, TileSocketProfile>> Generate_CellTileSocketProfiles(
            Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, List<SurfaceBlock>>> tileInnerEdgesByCellSide
        )
        {
            Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, TileSocketProfile>> cellTileSocketProfiles = new Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, TileSocketProfile>>();

            foreach (var cell in tileInnerEdgesByCellSide.Keys)
            {
                if (cellTileSocketProfiles.ContainsKey(cell) == false) cellTileSocketProfiles.Add(cell, new Dictionary<HexagonTileSide, TileSocketProfile>());

                cell.tileSocketProfileBySide = new Dictionary<HexagonTileSide, TileSocketProfile>();

                HexagonCellPrototype[] neighborTileSides = cell.GetNeighborTileSides();

                for (int _side = 0; _side < neighborTileSides.Length; _side++)
                {
                    HexagonTileSide side = (HexagonTileSide)_side;
                    HexagonCellPrototype neighbor = neighborTileSides[_side];
                    // if (side == HexagonTileSide.Top || side == HexagonTileSide.Bottom)
                    // {
                    //     neighbor = (side == HexagonTileSide.Bottom) ? cell.layerNeighbors[0] : cell.layerNeighbors[1];
                    // }
                    // else neighbor = cell.neighborsBySide[_side];

                    if (tileInnerEdgesByCellSide[cell].ContainsKey(side))
                    {
                        TileSocketProfile new_tileSocketProfile = TileSocketProfile.Generate_TileSocketProfile(tileInnerEdgesByCellSide[cell][side], neighbor);
                        if (cellTileSocketProfiles[cell].ContainsKey(side) == false) cellTileSocketProfiles[cell].Add(side, new_tileSocketProfile);

                        cell.tileSocketProfileBySide.Add(side, new_tileSocketProfile);

                        if (cellTileSocketProfiles.ContainsKey(neighbor) == false)
                        {
                            cellTileSocketProfiles.Add(neighbor, new Dictionary<HexagonTileSide, TileSocketProfile>());
                            HexagonTileSide relativeSide = HexCoreUtil.GetRelativeHexagonSide(side);

                            if (tileInnerEdgesByCellSide.ContainsKey(neighbor))
                            {
                                TileSocketProfile neighbor_tileSocketProfile = TileSocketProfile.Generate_TileSocketProfile(tileInnerEdgesByCellSide[neighbor][relativeSide], cell);

                                cellTileSocketProfiles[neighbor].Add(relativeSide, neighbor_tileSocketProfile);


                                if (neighbor.tileSocketProfileBySide == null) neighbor.tileSocketProfileBySide = new Dictionary<HexagonTileSide, TileSocketProfile>();
                                neighbor.tileSocketProfileBySide.Add(relativeSide, neighbor_tileSocketProfile);

                                bool incompatibile = TileSocketProfile.IsCompatible(new_tileSocketProfile, neighbor_tileSocketProfile);
                                if (incompatibile)
                                {
                                    Debug.LogError("Incompatibile socket profiles! - sides: " + side + " / " + relativeSide);
                                    cell.Highlight(true);
                                    neighbor.Highlight(true);
                                }
                            }
                            // else
                            // {
                            //     Debug.LogError("neighbor not found in tileInnerEdgesByCellSide");
                            // }
                        }
                    }
                    else
                    {
                        if (cellTileSocketProfiles[cell].ContainsKey(side) == false)
                        {
                            GlobalSockets defaultSocketId = GlobalSockets.Empty_Space;
                            if (neighbor != null)
                            {
                                defaultSocketId = GlobalSockets.InnerCell_Generic;
                            }
                            else
                            {
                                if (side == HexagonTileSide.Bottom)
                                {
                                    defaultSocketId = GlobalSockets.Structure_Bottom;
                                }
                                else if (side == HexagonTileSide.Top)
                                {
                                    defaultSocketId = GlobalSockets.Structure_Top;
                                }
                                else
                                {
                                    defaultSocketId = GlobalSockets.Structure_Outer;
                                }
                            }
                            cellTileSocketProfiles[cell].Add(side, new TileSocketProfile(defaultSocketId));
                            cell.tileSocketProfileBySide.Add(side, new TileSocketProfile(defaultSocketId));
                        }
                    }
                }

            }
            return cellTileSocketProfiles;
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
                if (cell.tileSocketProfileBySide == null)
                {
                    cell.tileSocketProfileBySide = new Dictionary<HexagonTileSide, TileSocketProfile>() {
                        { HexagonTileSide.Front,  new TileSocketProfile(cell) },
                        { HexagonTileSide.FrontRight,   new TileSocketProfile(cell) },
                        { HexagonTileSide.BackRight,   new TileSocketProfile(cell) },
                        { HexagonTileSide.Back,   new TileSocketProfile(cell) },
                        { HexagonTileSide.BackLeft,   new TileSocketProfile(cell) },
                        { HexagonTileSide.FrontLeft,   new TileSocketProfile(cell) },
                        { HexagonTileSide.Top,   new TileSocketProfile(cell) },
                        { HexagonTileSide.Bottom,  new TileSocketProfile(cell) }
                    };
                }

                foreach (HexagonTileSide side in neighborSides)
                {
                    if (intersectionPointsBySideByCell[cell].ContainsKey(side) == false) intersectionPointsBySideByCell[cell].Add(side, new List<Vector3>());

                    if (cell.tileSocketProfileBySide.ContainsKey(side) == false) cell.tileSocketProfileBySide.Add(side, new TileSocketProfile(cell));

                    int _side = (int)side;
                    HashSet<Vector3> addedLookups = new HashSet<Vector3>();

                    HexagonCellPrototype neighbor = null;

                    if (side == HexagonTileSide.Top || side == HexagonTileSide.Bottom)
                    {
                        neighbor = (side == HexagonTileSide.Top) ? cell.layerNeighbors[1] : cell.layerNeighbors[0];
                        if (neighbor != null && neighbor.tileSocketProfileBySide == null) neighbor.tileSocketProfileBySide = new Dictionary<HexagonTileSide, TileSocketProfile>();

                        foreach (var block in resultsByCell[cell])
                        {
                            if (block.ignore) continue;

                            bool tileEdgeFound = false;
                            Vector3[] blockCorners = null;

                            if (side == HexagonTileSide.Top && block.IsTileTopEdge())
                            {
                                blockCorners = block.GetCornersOnSide(SurfaceBlockSide.Top);
                                tileEdgeFound = true;
                            }
                            else if (side == HexagonTileSide.Bottom && block.IsTileBottomEdge())
                            {
                                blockCorners = block.GetCornersOnSide(SurfaceBlockSide.Bottom);
                                tileEdgeFound = true;
                            }

                            if (!tileEdgeFound) continue;

                            foreach (var item in blockCorners)
                            {
                                if (item == Vector3.zero) continue;
                                Vector3 intersectionPoint = item;

                                Vector3 lookup = VectorUtil.PointLookupDefault(intersectionPoint);
                                if (addedLookups.Contains(lookup)) continue;

                                if (VectorUtil.DistanceXZ(intersectionPoint, cell.center) < cellRadius)
                                {
                                    addedLookups.Add(lookup);

                                    if (intersectionPointsLookup.ContainsKey(lookup) == false)
                                    {
                                        intersectionPointsLookup.Add(lookup, intersectionPoint);
                                    }
                                    else intersectionPoint = intersectionPointsLookup[lookup];

                                    intersectionPointsBySideByCell[cell][side].Add(lookup);
                                    cell.tileSocketProfileBySide[side].AddValue(lookup, intersectionPoint);

                                    if (neighbor != null)
                                    {
                                        HexagonTileSide relativeSide = (side == HexagonTileSide.Top) ? HexagonTileSide.Bottom : HexagonTileSide.Top;
                                        if (neighbor.tileSocketProfileBySide.ContainsKey(relativeSide) == false) neighbor.tileSocketProfileBySide.Add(relativeSide, new TileSocketProfile(neighbor));
                                        neighbor.tileSocketProfileBySide[relativeSide].AddValue(lookup, intersectionPoint);
                                    }
                                }
                            }
                        }
                        continue;
                    }

                    Vector3[] corners = HexCoreUtil.GetSideCorners(cell, (HexagonSide)side);

                    neighbor = cell.neighborsBySide[_side];
                    if (neighbor != null && neighbor.tileSocketProfileBySide == null) neighbor.tileSocketProfileBySide = new Dictionary<HexagonTileSide, TileSocketProfile>();

                    if (neighbor != null)
                    {
                        // Dont add sockets for empty side 
                        if (resultsByCell.ContainsKey(neighbor) == false || CanGenerateTileMeshForCell(resultsByCell[neighbor], null) == false)
                        {
                            cell.tileSocketProfileBySide[side].SetDefaultID((int)GlobalSockets.InnerCell_Generic);
                            int _relativeSide = (int)HexCoreUtil.GetRelativeHexagonSide((HexagonSide)_side);
                            if (_relativeSide != -1)
                            {
                                HexagonTileSide relativeSide = (HexagonTileSide)_relativeSide;
                                if (neighbor.tileSocketProfileBySide.ContainsKey(relativeSide) == false) neighbor.tileSocketProfileBySide.Add(relativeSide, new TileSocketProfile(neighbor));
                                neighbor.tileSocketProfileBySide[relativeSide].SetDefaultID((int)GlobalSockets.InnerCell_Generic);
                            }
                            continue;
                        }
                    }

                    foreach (var block in resultsByCell[cell])
                    {
                        if (block.ignore) continue;

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

                                intersectionPointsBySideByCell[cell][side].Add(lookup);
                                cell.tileSocketProfileBySide[side].AddValue(lookup, intersectionPoint);

                                if (neighbor != null)
                                {
                                    int _relativeSide = (int)HexCoreUtil.GetRelativeHexagonSide((HexagonSide)_side);
                                    if (_relativeSide == -1)
                                    {
                                        Debug.LogError("_relativeSide:  " + _relativeSide + ",  for side: " + side);
                                    }
                                    else
                                    {
                                        HexagonTileSide relativeSide = (HexagonTileSide)_relativeSide;
                                        if (neighbor.tileSocketProfileBySide.ContainsKey(relativeSide) == false) neighbor.tileSocketProfileBySide.Add(relativeSide, new TileSocketProfile(neighbor));
                                        neighbor.tileSocketProfileBySide[relativeSide].AddValue(lookup, intersectionPoint);
                                    }
                                }
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
                        if (block != null && block.ignore == false && block.isEdge == false)
                        {
                            blockGrid[x, y, z].SetIgnored(true);
                        }
                    }
                }
            }
            return blockGrid;
        }

        public static SurfaceBlock[,,] ClearInnerBlocks(SurfaceBlock[,,] blockGrid, BoundsShapeBlock boundsShapeBlock)
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
                        if (block != null && block.ignore == false
                            && !block.IsRoof() && !block.IsFloor()
                            && BoundsShapeBlock.IsWithinBounds(boundsShapeBlock, block.Position))
                        {
                            blockGrid[x, y, z].SetIgnored(true);
                        }
                    }
                }
            }
            return blockGrid;
        }

        public static SurfaceBlock[,,] ClearInnerBlocks(SurfaceBlock[,,] blockGrid, List<BoundsShapeBlock> clearWithinBounds)
        {
            int gridSizeX = blockGrid.GetLength(0);
            int gridSizeY = blockGrid.GetLength(1);
            int gridSizeZ = blockGrid.GetLength(2);

            bool bClearWithinRadius = clearWithinBounds != null && clearWithinBounds.Count > 0;

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    for (int z = 0; z < gridSizeZ; z++)
                    {
                        SurfaceBlock block = blockGrid[x, y, z];

                        if (block != null && !block.IsRoof() && !block.IsFloor())
                        {
                            bool ignore = false;
                            if (bClearWithinRadius)
                            {
                                foreach (BoundsShapeBlock boundsShape in clearWithinBounds)
                                {
                                    if (ignore) break;
                                    if (boundsShape == null) continue;

                                    if (BoundsShapeBlock.IsWithinBounds(boundsShape, block.Position))
                                    {
                                        ignore = true;
                                        break;
                                    }
                                }
                                if (!ignore) continue;
                            }

                            if (ignore) blockGrid[x, y, z].SetIgnored(true);
                        }
                    }
                }
            }
            return blockGrid;
        }

        public static SurfaceBlock[,,] ClearInnerBlocks(SurfaceBlock[,,] blockGrid, Dictionary<float, List<Vector3>> clearWithinRadius)
        {
            int gridSizeX = blockGrid.GetLength(0);
            int gridSizeY = blockGrid.GetLength(1);
            int gridSizeZ = blockGrid.GetLength(2);

            bool bClearWithinRadius = clearWithinRadius != null;

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    for (int z = 0; z < gridSizeZ; z++)
                    {
                        SurfaceBlock block = blockGrid[x, y, z];

                        if (block != null && !block.IsRoof() && !block.IsFloor())
                        {
                            bool ignore = false;
                            if (bClearWithinRadius)
                            // if (bClearWithinRadius && block.isEdge)
                            {
                                foreach (var item in clearWithinRadius.Keys)
                                {
                                    if (ignore) break;
                                    foreach (var point in clearWithinRadius[item])
                                    {
                                        if (ignore) break;

                                        if (Vector3.Distance(point, block.Position) < item)
                                        {
                                            ignore = true;
                                            break;
                                        }
                                    }
                                }
                                if (!ignore) continue;
                            }
                            if (ignore) blockGrid[x, y, z].SetIgnored(true);
                        }
                    }
                }
            }
            return blockGrid;
        }


        public static void DrawGrid(Dictionary<Vector3, SurfaceBlock> blockCenterLookups, HexagonCellPrototype _highlightedCell = null)
        {
            Dictionary<string, Color> customColors = UtilityHelpers.CustomColorDefaults();
            foreach (SurfaceBlock block in blockCenterLookups.Values)
            {
                SurfaceBlock.DrawBlock(block);
            }
        }

        public static void DrawGrid(SurfaceBlock[,,] blockGrid, HexagonCellPrototype _highlightedCell = null, bool terrain = false)
        {
            Dictionary<string, Color> customColors = UtilityHelpers.CustomColorDefaults();
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
                        SurfaceBlock.DrawBlock(block);
                    }
                }
            }
        }

        public static void DrawBlock(SurfaceBlock block, HexagonCellPrototype _highlightedCell = null, bool terrain = false)
        {
            if (block != null)
            {
                if (_highlightedCell != null && block.owner != _highlightedCell) return;

                // if (terrain)
                // {
                //     // Gizmos.color = customColors["brown"];
                //     // Gizmos.color = customColors["purple"];
                //     Gizmos.color = Color.white;

                //     Vector3 terrainPos = block.Position;
                //     terrainPos.y += block.terrainHeight;
                //     Gizmos.DrawWireSphere(terrainPos, block.size / 2);

                //     continue;
                // }


                if (block.owner == null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(block.Position, block.size / 2);

                    if (block.ignore)
                    {
                        Gizmos.color = Color.black;
                        Gizmos.DrawSphere(block.Position, block.size / 2);
                    }
                }
                // else if (block.IsMeshable())
                // {
                //     Gizmos.color = Color.green;
                //     Gizmos.DrawWireSphere(block.Position, block.size / 2);
                // }

                if (!block.ignore)
                {

                    if (block.isTileEdge)
                    {
                        if (block.isTileInnerEdge)
                        {
                            Gizmos.color = Color.blue;
                            Gizmos.DrawWireSphere(block.Position, block.size / 2);
                        }
                        else
                        {
                            Gizmos.color = Color.magenta;
                            Gizmos.DrawWireSphere(block.Position, block.size / 2);
                        }
                    }
                    // else
                    // {
                    //     Gizmos.color = Color.yellow;
                    //     Gizmos.DrawWireSphere(block.Position, block.size / 2);
                    // }

                    block.DrawNeighbors();

                    // if (block.isTileEdge)
                    // {
                    //     Gizmos.color = Color.yellow;
                    //     Gizmos.DrawWireSphere(block.Position, 0.25f);
                    // }
                    // else
                    // {

                    //     Gizmos.color = Color.blue;
                    //     Gizmos.DrawWireSphere(block.Position, 0.25f);
                    // }
                }
            }
        }

        public static bool CanGenerateTileMeshForCell(List<SurfaceBlock> cellSurfaceBlocks, List<SurfaceBlockState> filterOnStates)
        {
            bool bFilterOnStates = filterOnStates != null;
            foreach (var block in cellSurfaceBlocks)
            {
                if (block != null && block.IsMeshable())
                {
                    if (bFilterOnStates && block.HasFilteredState(filterOnStates) == false) continue;
                    return true;
                }
            }
            return false;
        }

        public static Dictionary<HexagonCellPrototype, GameObject> Generate_MeshObjectsByCell(
            Dictionary<HexagonCellPrototype, List<SurfaceBlock>> surfaceBlocksByCell,
            GameObject prefab,
            Transform transform,
            List<SurfaceBlockState> filterOnStates,
            bool disableObject,
            Transform folder,
            bool resetTransform,
            string nameHeader = "Building_"
        )
        {
            Dictionary<HexagonCellPrototype, GameObject> objectsByCell = new Dictionary<HexagonCellPrototype, GameObject>();
            int ix = 0;
            foreach (var cell in surfaceBlocksByCell.Keys)
            {
                GameObject gameObject = Generate_MeshObject(surfaceBlocksByCell[cell], prefab, transform, cell.center, filterOnStates, folder);
                if (gameObject != null)
                {
                    objectsByCell.Add(cell, gameObject);

                    gameObject.name = "Tile_X" + cell.size + "_" + nameHeader + "_" + ix;
                    if (cell.isEdgeCell) gameObject.name += "_Edge";

                    if (resetTransform)
                    {
                        gameObject.transform.position = Vector3.zero;
                        // gameObject.transform.rotation = Quaternion.identity;
                        // gameObject.transform.localPosition = Vector3.zero;
                    }

                    if (disableObject) gameObject.SetActive(false);

                    ix++;
                }
                else
                {
                    objectsByCell.Add(cell, null);
                }
            }
            return objectsByCell;
        }

        public static GameObject Generate_MeshObject(List<SurfaceBlock> surfaceBlocks, GameObject prefab, Transform transform, Vector3 centerPosition, List<SurfaceBlockState> filterOnStates, Transform folder)
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
                if (block != null && block.IsMeshable())
                {
                    if (bFilterOnStates && block.HasFilteredState(filterOnStates) == false) continue;

                    Mesh new_Mesh = block.Generate_Mesh(transform);
                    if (new_Mesh != null) meshes.Add(new_Mesh);
                }
            }
            if (meshes.Count == 0) return null;

            Mesh finalMesh = MeshUtil.GenerateMeshFromVertexSurfaces(meshes);
            MeshUtil.CenterMeshAtZero(finalMesh, centerPosition);

            // result = MeshUtil.InstantiatePrefabWithMesh(prefab, finalMesh, Vector3.zero);
            result = MeshUtil.InstantiatePrefabWithMesh(prefab, finalMesh, centerPosition);

            if (folder != null) result.transform.SetParent(folder);
            return result;
        }

        public static List<GameObject> Generate_MeshObjects(
            SurfaceBlock[,,] blockGrid,
            GameObject prefab,
            Transform transform,
            List<SurfaceBlockState> filterOnStates,
            Transform folder
        )
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
                        if (block != null && !block.ignore)
                        // if (block != null && block.isEdge && !block.ignore)
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

        public static List<GameObject> Generate_MeshObjects(
            SurfaceBlock[,,] blockGrid,
            GameObject prefab,
            Transform transform,
            List<SurfaceBlockState> filterOnStates,
            Transform folder,
            int meshGroupLimit
        )
        {
            if (!prefab)
            {
                Debug.LogError("NO prefab");
                return null;
            }

            int gridSizeX = blockGrid.GetLength(0);
            int gridSizeY = blockGrid.GetLength(1);
            int gridSizeZ = blockGrid.GetLength(2);
            bool bFilterOnStates = filterOnStates != null;

            List<GameObject> results = new List<GameObject>();
            List<Mesh> meshGroup = new List<Mesh>();

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

                            Mesh new_Mesh = block.Generate_Mesh(transform);
                            if (new_Mesh != null)
                            {
                                meshGroup.Add(new_Mesh);

                                if (meshGroup.Count < meshGroupLimit)
                                {
                                    Mesh singleMesh = MeshUtil.GenerateMeshFromVertexSurfaces(meshGroup);
                                    GameObject new_gameobject = MeshUtil.InstantiatePrefabWithMesh(prefab, singleMesh, transform.position);

                                    if (folder != null) new_gameobject.transform.SetParent(folder);
                                    results.Add(new_gameobject);

                                    meshGroup.Clear();
                                }
                            }
                        }
                    }
                }
            }

            return results;
        }

        public static GameObject Generate_MeshObject(
            SurfaceBlock[,,] blockGrid,
            GameObject prefab,
            Transform transform,
            List<SurfaceBlockState> filterOnStates,
            Transform folder
        )
        {
            if (!prefab)
            {
                Debug.LogError("NO prefab");
                return null;
            }

            List<Mesh> meshes = new List<Mesh>();
            GameObject result = null;

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

                            Mesh new_Mesh = block.Generate_Mesh(transform);
                            if (new_Mesh != null) meshes.Add(new_Mesh);
                        }
                    }
                }
            }
            if (meshes.Count == 0) return null;

            // Mesh finalMesh = MeshUtil.GenerateMeshFromVertexSurfaces(meshes);
            Mesh finalMesh = MeshUtil.GenerateMeshFromVertexSurfaces(meshes);

            result = MeshUtil.InstantiatePrefabWithMesh(prefab, finalMesh, transform.position);
            if (folder != null) result.transform.SetParent(folder);

            return result;
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
                    int vetexLength = sideCorners.Length;

                    Mesh surfaceMesh = new Mesh();
                    surfaceMesh.vertices = sideCorners;
                    // surfaceMesh.vertices = VectorUtil.InversePointsToLocal_ToArray(sideCorners.ToList(), transform);
                    // surfaceMesh.vertices = VectorUtil.TransformPointsToWorldPos_ToArray(sideCorners.ToList(), transform);

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
                    else if (side == SurfaceBlockSide.Top || side == SurfaceBlockSide.Bottom)
                    {
                        int[] triangles = MeshUtil.GenerateTriangles(vetexLength);
                        surfaceMesh.triangles = triangles;

                        if (side == SurfaceBlockSide.Bottom)
                        {
                            MeshUtil.ReverseNormals(surfaceMesh);
                            MeshUtil.ReverseTriangles(surfaceMesh); // Updated: Reverse the triangles as well
                        }
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

            foreach (var block in neighborsToFill)
            {
                Vector3[] neighborCenters = SurfaceBlock.GenerateNeighborCenters(block.Position, size * 2);
                int found = 0;
                for (var i = 0; i < neighborCenters.Length; i++)
                {
                    Vector3 lookup = VectorUtil.PointLookupDefault(neighborCenters[i]);
                    if (centerLookups.ContainsKey(lookup) && centerLookups[lookup] != block)
                    {
                        block.neighbors[i] = centerLookups[lookup];
                        found++;
                    }
                }
                block.SetEdge((found < 6));
            }

            grid = surfaceBlockGrid;
            centerLookups = new_centerLookups;
            cornerLookups = new_cornerLookups;
        }

    }
}