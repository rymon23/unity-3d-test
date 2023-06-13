using System.Collections;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WFCSystem;

using UnityEngine.Jobs;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;


namespace ProceduralBase
{
    public enum ShowVertexState { None, All, Path, Terrain, Cell, CellCenter, CellCorner, CellFlatGround, CellRamp, GridEdge, MicroCell, Remove, TunnelEntry, OnTunnel, IgnoreSmooth, InheritedVertex, EdgeVertex, InHexBounds, CellNeighbor }

    public enum VertexType { Unset = 0, Road = 1, Cell = 2, Border = 3, Generic = 4 }

    [System.Serializable]
    public struct TerrainVertex
    {
        public Vector2 worldspaceOwnerCoordinate;

        #region Inherited / Parallel Vertex

        public Vector2 parallelWorldspaceOwnerCoordinate;
        public int parallelVertexIndex_X;
        public int parallelVertexIndex_Z;
        #endregion

        #region Off Edge Neighbor
        public Vector2 offEdgeWorldspaceOwnerCoordinate;
        public int offEdgeVertexIndex_X;
        public int offEdgeVertexIndex_Z;
        #endregion

        public Vector2 noiseCoordinate;
        public Vector2 aproximateCoord;
        public int index;
        public Vector3 position;
        public VertexType type;
        public int index_X;
        public int index_Z;
        public float baseNoiseHeight;

        public bool hasCellVertexNeighbor;
        public int cellVertexNeighbors;
        public float closestCellVertexNeighborDist;

        public bool isFlatGroundCell;
        public bool isCellCenterPoint;
        public bool isCellCornerPoint;
        public bool isOnTheEdgeOftheGrid;
        public bool isInMicroCell;
        public bool isOnTunnelCell;
        public bool isOnTunnelGroundEntry;
        public int tunnelCellRoofPosY;
        public int corner;


        public bool isEdgeVertex;
        public bool isInHexBounds;
        public bool isInherited;
        public bool ignoreSmooth;
        public bool markedForRemoval;
        public bool markedIgnore;
    }

    public static class TerrainVertexUtil
    {
        public static bool SameVertexPoint(TerrainVertex vertexA, TerrainVertex vertexB)
        {
            return (vertexA.noiseCoordinate == vertexB.noiseCoordinate ||
                Vector2.Distance(vertexA.noiseCoordinate, vertexB.noiseCoordinate) < 0.4f ||
                VectorUtil.DistanceXZ(vertexA.position, vertexB.position) < 0.4f
                );
        }
        public static bool ShouldSmoothIgnore(TerrainVertex vertex) => vertex.ignoreSmooth || vertex.isOnTunnelCell || vertex.isOnTunnelGroundEntry;
        public static bool IsCellVertex(TerrainVertex vertex) =>
            (
            vertex.isFlatGroundCell &&

            (vertex.isCellCenterPoint
                || vertex.isCellCornerPoint
                || vertex.isOnTunnelGroundEntry
                || vertex.isOnTunnelCell)
            // || vertex.isInMicroCell
            );



        public static (Vector3[], Vector2[], HashSet<Vector2>) ExtractVertexWorldPositionsAndUVs_V2(
            Vector2[,] worldspaceVertexKeys,
            Dictionary<Vector2, TerrainVertex> globalTerrainVertexGrid,
            Transform transform
        )
        {
            HashSet<Vector2> meshExcludeList = new HashSet<Vector2>();
            int gridSizeX = worldspaceVertexKeys.GetLength(0);
            int gridSizeZ = worldspaceVertexKeys.GetLength(1);
            // Create an array to store vertex data
            Vector3[] positions = new Vector3[gridSizeX * gridSizeZ];

            // Create an array to store the UV data
            Vector2[] uvs = new Vector2[gridSizeX * gridSizeZ];

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    uvs[x + z * gridSizeX] = new Vector2(x / (float)(gridSizeX - 1), z / (float)(gridSizeZ - 1));

                    // if (globalTerrainVertexGrid.ContainsKey(worldspaceVertexKeys[x, z]) == false) continue;

                    TerrainVertex currentVertex = globalTerrainVertexGrid[worldspaceVertexKeys[x, z]];
                    if (currentVertex.markedForRemoval)
                    {
                        meshExcludeList.Add(worldspaceVertexKeys[x, z]);
                        // Debug.LogError("meshExcludeList added: " + worldspaceVertexKeys[x, z]);
                    }

                    positions[x + z * gridSizeX] = transform.InverseTransformPoint(currentVertex.position);
                    // positions[x + z * gridSizeX] = currentVertex.position;
                }
            }

            return (positions, uvs, meshExcludeList);
        }



        public static (Vector3[], Vector2[], HashSet<Vector2>) ExtractVertexWorldPositionsAndUVs(
            Vector2[,] worldspaceVertexKeys,
            Dictionary<Vector2, TerrainVertex> globalTerrainVertexGrid,
            Transform transform
        )
        {
            HashSet<Vector2> meshExcludeList = new HashSet<Vector2>();
            int gridSizeX = worldspaceVertexKeys.GetLength(0);
            int gridSizeZ = worldspaceVertexKeys.GetLength(1);
            // Create an array to store vertex data
            // List<Vector3> positions = new List<Vector3>();
            Vector3[] positions = new Vector3[gridSizeX * gridSizeZ];

            // Create an array to store the UV data
            Vector2[] uvs = new Vector2[gridSizeX * gridSizeZ];

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    uvs[x + z * gridSizeX] = new Vector2(x / (float)gridSizeX, z / (float)gridSizeZ);

                    TerrainVertex currentVertex = globalTerrainVertexGrid[worldspaceVertexKeys[x, z]];

                    if (currentVertex.markedForRemoval)
                    {
                        meshExcludeList.Add(worldspaceVertexKeys[x, z]);
                        // Debug.LogError("meshExcludeList added: " + worldspaceVertexKeys[x, z]);
                    }

                    positions[x * gridSizeX + z] = transform.InverseTransformPoint(currentVertex.position);
                    // positions[x * gridSizeX + z] = currentVertex.position;
                    // positions.Add(transform.InverseTransformPoint(currentVertex.position));
                }
            }
            return (positions, uvs, meshExcludeList);
        }

        // public static (Vector3[], Vector2[], HashSet<(int, int)>) ExtractVertexWorldPositionsAndUVs(
        //     Vector2[,] worldspaceVertexKeys,
        //     Dictionary<Vector2, TerrainVertex> globalTerrainVertexGrid,
        //     Transform transform
        // )
        // {
        //     HashSet<(int, int)> meshExcludeList = new HashSet<(int, int)>();
        //     int gridSizeX = worldspaceVertexKeys.GetLength(0);
        //     int gridSizeZ = worldspaceVertexKeys.GetLength(1);
        //     // Create an array to store vertex data
        //     List<Vector3> positions = new List<Vector3>();
        //     // Create an array to store the UV data
        //     Vector2[] uvs = new Vector2[gridSizeX * gridSizeZ];

        //     for (int x = 0; x < gridSizeX; x++)
        //     {
        //         for (int z = 0; z < gridSizeZ; z++)
        //         {
        //             uvs[x + z * gridSizeX] = new Vector2(x / (float)gridSizeX, z / (float)gridSizeZ);

        //             TerrainVertex currentVertex = globalTerrainVertexGrid[worldspaceVertexKeys[x, z]];

        //             // if (currentVertex.markedForRemoval) meshExcludeList.Add((x, z));
        //             // if (currentVertex.markedForRemoval || !currentVertex.isInHexBounds) meshExcludeList.Add((x, z));

        //             positions.Add(transform.InverseTransformPoint(currentVertex.position));
        //         }
        //     }
        //     return (positions.ToArray(), uvs, meshExcludeList);
        // }
        // public static (Vector3[], Vector2[], HashSet<(int, int)>) ExtractVertexWorldPositionsAndUVs(
        //     Vector2[,] worldspaceVertexKeys,
        //     Dictionary<Vector2, TerrainVertex> globalTerrainVertexGrid,
        //     Transform transform
        // )
        // {
        //     HashSet<(int, int)> meshExcludeList = new HashSet<(int, int)>();
        //     int gridSizeX = worldspaceVertexKeys.GetLength(0);
        //     int gridSizeZ = worldspaceVertexKeys.GetLength(1);
        //     // Create an array to store vertex data
        //     Vector3[] positions = new Vector3[gridSizeX * gridSizeZ];
        //     // Create an array to store the UV data
        //     Vector2[] uvs = new Vector2[gridSizeX * gridSizeZ];

        //     for (int x = 0; x < gridSizeX; x++)
        //     {
        //         for (int z = 0; z < gridSizeZ; z++)
        //         {
        //             uvs[x + z * gridSizeX] = new Vector2(x / (float)gridSizeX, z / (float)gridSizeZ);

        //             TerrainVertex currentVertex = globalTerrainVertexGrid[worldspaceVertexKeys[x, z]];

        //             // if (currentVertex.markedForRemoval) meshExcludeList.Add((x, z));
        //             // if (currentVertex.markedForRemoval || !currentVertex.isInHexBounds) meshExcludeList.Add((x, z));

        //             positions[x * gridSizeX + z] = transform.InverseTransformPoint(currentVertex.position);
        //         }
        //     }
        //     return (positions, uvs, meshExcludeList);
        // }


        // public static (Vector3[], Vector2[], HashSet<(int, int)>) ExtractVertexWorldPositionsAndUVs(TerrainVertex[,] vertexGrid, Transform transform)
        // {
        //     HashSet<(int, int)> meshExcludeList = new HashSet<(int, int)>();
        //     int gridSizeX = vertexGrid.GetLength(0);
        //     int gridSizeZ = vertexGrid.GetLength(1);
        //     // Create an array to store vertex data
        //     Vector3[] positions = new Vector3[gridSizeX * gridSizeZ];
        //     // Create an array to store the UV data
        //     Vector2[] uvs = new Vector2[gridSizeX * gridSizeZ];

        //     for (int x = 0; x < gridSizeX - 1; x++)
        //     {
        //         for (int z = 0; z < gridSizeZ - 1; z++)
        //         {
        //             uvs[x + z * gridSizeX] = new Vector2(x / (float)gridSizeX, z / (float)gridSizeZ);

        //             TerrainVertex currentVertex = vertexGrid[x, z];

        //             // if (currentVertex.markedForRemoval || !currentVertex.isInHexBounds) meshExcludeList.Add((x, z));

        //             positions[x * gridSizeX + z] = transform.InverseTransformPoint(vertexGrid[x, z].position);
        //         }
        //     }
        //     return (positions, uvs, meshExcludeList);
        // }


        public static (Vector3[], Vector2[], HashSet<(int, int)>) ExtractVertexWorldPositionsAndUVs(TerrainVertex[,] vertexGrid, Transform transform)
        {
            HashSet<(int, int)> meshExcludeList = new HashSet<(int, int)>();
            int gridSizeX = vertexGrid.GetLength(0);
            int gridSizeZ = vertexGrid.GetLength(1);
            // Create an array to store vertex data
            // List<Vector3> positions = new List<Vector3>();
            Vector3[] positions = new Vector3[gridSizeX * gridSizeZ];
            // Create an array to store the UV data
            Vector2[] uvs = new Vector2[gridSizeX * gridSizeZ];

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    uvs[x + z * gridSizeX] = new Vector2(x / (float)gridSizeX, z / (float)gridSizeZ);

                    if (x >= gridSizeX || z >= gridSizeZ) continue;

                    // TerrainVertex currentVertex = vertexGrid[x, z];

                    // positions.Add(vertexGrid[x, z].position);
                    positions[x * gridSizeX + z] = transform.InverseTransformPoint(vertexGrid[x, z].position);

                    // if (currentVertex != null && currentVertex.position != null)
                    // {
                    // positions[x * gridSizeX + z] = transform.InverseTransformPoint(currentVertex.position);
                    // }
                    // else
                    // {
                    //     meshExcludeList.Add((x, z));
                    // }
                }
            }
            return (positions, uvs, meshExcludeList);
        }


        public static (Vector3[], HashSet<(int, int)>) ExtractFinalVertexWorldPositions(TerrainVertex[,] vertexGrid, Transform transform)
        {
            HashSet<(int, int)> meshExcludeList = new HashSet<(int, int)>();
            int gridSizeX = vertexGrid.GetLength(0);
            int gridSizeZ = vertexGrid.GetLength(1);
            Vector3[] positions = new Vector3[gridSizeX * gridSizeZ];

            for (int x = 0; x < vertexGrid.GetLength(0); x++)
            {
                for (int z = 0; z < vertexGrid.GetLength(1); z++)
                {
                    TerrainVertex currentVertex = vertexGrid[x, z];

                    if (currentVertex.markedForRemoval || !currentVertex.isInHexBounds) meshExcludeList.Add((x, z));

                    positions[x * gridSizeX + z] = transform.InverseTransformPoint(vertexGrid[x, z].position);
                    // positions[x + z * gridSizeX] = transform.InverseTransformPoint(vertexGrid[x, z].position);
                    // positions.Add(transform.InverseTransformPoint(vertexGrid[x, z].position));
                }
            }
            return (positions, meshExcludeList);
        }

        public static TerrainVertex[,] GenerateRectangleGridInHexagon(
            Vector3 centerPos,
            int size,
            Vector3 topLeft,
            Vector3 topRight,
            Vector3 bottomLeft,
            Vector3 bottomRight,
            Transform transform,
            Vector2 _worldspaceOwnerCoordinate,
            int vertexDensity = 100,
            int edgeExpansionOffset = 6,
            bool markRemoveOutOfBounds = false
        )
        {
            // List<Vector3> expandedCorners = HexCoreUtil.GenerateHexagonPoints(centerPos, size + edgeExpansionOffset).ToList();
            List<Vector3> aproximateCorners = HexCoreUtil.GenerateHexagonPoints(centerPos, size + 1).ToList();

            // Vector3[] sides = HexagonGenerator.GenerateHexagonSidePoints(corners);
            // List<Vector3> dottedEdgeLine = VectorUtil.GenerateDottedLine(corners.ToList(), 75);

            // Calculate the step sizes along each axis
            float xStep = 1f / vertexDensity * (topRight - topLeft).magnitude;
            float zStep = 1f / vertexDensity * (bottomLeft - topLeft).magnitude;

            // Create the grid
            TerrainVertex[,] grid = new TerrainVertex[vertexDensity + 1, vertexDensity + 1];
            int currentIndex = 0;

            // Loop through each vertex in the grid
            for (int z = 0; z <= vertexDensity; z++)
            {
                for (int x = 0; x <= vertexDensity; x++)
                {
                    // Calculate the position of the vertex
                    Vector3 pos = topLeft + xStep * x * (topRight - topLeft).normalized + zStep * z * (bottomLeft - topLeft).normalized;

                    bool withinBounds = true;
                    // if (markRemoveOutOfBounds) withinBounds = VectorUtil.IsPointWithinPolygon(pos, expandedCorners);

                    // bool withinInnerBounds = VectorUtil.IsPointWithinPolygon(pos, innerCorners);
                    bool withinAproxBounds = VectorUtil.IsPointWithinPolygon(pos, aproximateCorners);

                    Vector3 worldCoord = transform.TransformVector(pos); // transform.TransformPoint(position);
                    Vector2Int noiseCoordinate = new Vector2Int((int)worldCoord.x, (int)worldCoord.z);

                    // Create the TerrainVertex object
                    TerrainVertex vertex = new TerrainVertex()
                    {
                        worldspaceOwnerCoordinate = _worldspaceOwnerCoordinate,
                        noiseCoordinate = noiseCoordinate,
                        position = pos,
                        index_X = x,
                        index_Z = z,
                        index = currentIndex,
                        type = VertexType.Generic,
                        markedForRemoval = !withinBounds,

                        parallelWorldspaceOwnerCoordinate = Vector2.positiveInfinity,
                        parallelVertexIndex_X = -1,
                        parallelVertexIndex_Z = -1,

                        isEdgeVertex = (z == 0 || x == 0 || z == vertexDensity || x == vertexDensity),
                        isInHexBounds = withinAproxBounds
                    };

                    // Add the vertex to the grid
                    grid[x, z] = vertex;
                    currentIndex++;
                }
            }

            return grid;
        }


        public static TerrainVertex[,] MergeElderVertexData(TerrainVertex[,] vertexGrid, TerrainVertex[,] elderVertexGrid, HexagonCellPrototype hexCell, int edgeExpansionOffset = 3)
        {
            List<Vector3> expandedCorners = HexCoreUtil.GenerateHexagonPoints(hexCell.center, hexCell.size + edgeExpansionOffset).ToList();
            List<Vector3> innerCorners = HexCoreUtil.GenerateHexagonPoints(hexCell.center, hexCell.size - edgeExpansionOffset).ToList();
            int aproxMod = 2;
            List<Vector3> aproximateCorners = HexCoreUtil.GenerateHexagonPoints(hexCell.center, hexCell.size - aproxMod).ToList();
            List<Vector3> aproximateElderCorners = HexCoreUtil.GenerateHexagonPoints(hexCell.center, hexCell.size + aproxMod).ToList();
            Vector3[] sides = hexCell.sidePoints;

            // Iterate through each vertex in the grid
            for (int z = 0; z < vertexGrid.GetLength(1); z++)
            {
                for (int x = 0; x < vertexGrid.GetLength(0); x++)
                {
                    TerrainVertex currentVertex = vertexGrid[x, z];
                    int foundElderIX_X = -1;
                    int foundElderIX_Z = -1;

                    foreach (TerrainVertex elderVertex in elderVertexGrid)
                    {
                        bool parallelFound = false;

                        if (!parallelFound && SameVertexPoint(currentVertex, elderVertex))
                        {
                            parallelFound = true;

                            foundElderIX_X = elderVertex.index_X;
                            foundElderIX_Z = elderVertex.index_Z;

                            TerrainVertex temp = vertexGrid[x, z];

                            vertexGrid[x, z] = elderVertex;
                            vertexGrid[x, z].index = temp.index;
                            vertexGrid[x, z].index_X = temp.index_X;
                            vertexGrid[x, z].index_Z = temp.index_Z;

                            vertexGrid[x, z].worldspaceOwnerCoordinate = temp.worldspaceOwnerCoordinate;
                            vertexGrid[x, z].isInHexBounds = temp.isInHexBounds;

                            vertexGrid[x, z].markedForRemoval = false;
                            vertexGrid[x, z].isInherited = true;

                            // Add lookup context to current access elder vertex 
                            vertexGrid[x, z].parallelWorldspaceOwnerCoordinate = elderVertex.worldspaceOwnerCoordinate;
                            vertexGrid[x, z].parallelVertexIndex_X = elderVertex.index_X;
                            vertexGrid[x, z].parallelVertexIndex_Z = elderVertex.index_Z;

                            // Add lookup context to elder access curret vertex 
                            elderVertexGrid[foundElderIX_X, foundElderIX_Z].parallelWorldspaceOwnerCoordinate = vertexGrid[x, z].worldspaceOwnerCoordinate;
                            elderVertexGrid[foundElderIX_X, foundElderIX_Z].parallelVertexIndex_X = x;
                            elderVertexGrid[foundElderIX_X, foundElderIX_Z].parallelVertexIndex_Z = z;

                            // Debug.Log("Elder Vertex found and inherited");
                            if (currentVertex.isEdgeVertex == false) break;
                        }
                        else if (currentVertex.isEdgeVertex && elderVertex.isEdgeVertex)
                        {
                            if (Vector2.Distance(elderVertex.position, currentVertex.position) < 1.2f)
                            {
                                if (elderVertex.index_X == x || elderVertex.index_Z == z)
                                {
                                    Debug.Log("Elder Edge Vertex found!");

                                    // Add lookup context to current access elder vertex 
                                    vertexGrid[x, z].offEdgeWorldspaceOwnerCoordinate = elderVertex.worldspaceOwnerCoordinate;
                                    vertexGrid[x, z].offEdgeVertexIndex_X = elderVertex.index_X;
                                    vertexGrid[x, z].offEdgeVertexIndex_Z = elderVertex.index_Z;
                                    vertexGrid[x, z].position.y = elderVertex.position.y;

                                    // Add lookup context to elder access curret vertex 
                                    elderVertexGrid[elderVertex.index_X, elderVertex.index_Z].offEdgeWorldspaceOwnerCoordinate = vertexGrid[x, z].worldspaceOwnerCoordinate;
                                    elderVertexGrid[elderVertex.index_X, elderVertex.index_Z].offEdgeVertexIndex_X = x;
                                    elderVertexGrid[elderVertex.index_X, elderVertex.index_Z].offEdgeVertexIndex_Z = z;
                                }
                            }
                        }
                    }

                    // Keep a sliver of the elder vertex and mark as ignoreSmooth  
                    // bool isSameVertex = (foundElderIX_X != -1);
                    // if (isSameVertex)
                    // {
                    //     bool withinAproxBounds = VectorUtil.IsPointWithinPolygon(vertexGrid[x, z].position, aproximateCorners);
                    //     if (withinAproxBounds)
                    //     {
                    //         bool withinInnerBounds = VectorUtil.IsPointWithinPolygon(vertexGrid[x, z].position, innerCorners);
                    //         if (withinInnerBounds)
                    //         {
                    //             elderVertexGrid[foundElderIX_X, foundElderIX_Z].markedForRemoval = true;
                    //             vertexGrid[x, z].isInherited = false;
                    //         }
                    //         else
                    //         {
                    //             elderVertexGrid[foundElderIX_X, foundElderIX_Z].markedForRemoval = false;
                    //             vertexGrid[x, z].markedForRemoval = false;
                    //             vertexGrid[x, z].ignoreSmooth = true;
                    //             vertexGrid[x, z].isInherited = true;
                    //         }
                    //     }
                    //     else
                    //     {
                    //         bool withinElderAproxBounds = !VectorUtil.IsPointWithinPolygon(vertexGrid[x, z].position, aproximateElderCorners);
                    //         if (withinElderAproxBounds)
                    //         {
                    //             // elderVertexGrid[foundElderIX_X, foundElderIX_Z].ignoreSmooth = true;
                    //             vertexGrid[x, z].markedForRemoval = true;
                    //         }

                    //         elderVertexGrid[foundElderIX_X, foundElderIX_Z].markedForRemoval = false;
                    //         vertexGrid[x, z].isInherited = true;
                    //         // vertexGrid[x, z].ignoreSmooth = true;
                    //     }
                    // }


                    // bool withinBounds = VectorUtil.IsPointWithinPolygon(vertexGrid[x, z].position, expandedCorners);
                    // if (withinBounds == false)
                    // {
                    //     if (foundElderIX_X != -1)
                    //     {
                    //         vertexGrid[x, z].markedForRemoval = true;
                    //     }
                    //     // vertexGrid[x, z].ignoreSmooth = true;
                    // }
                    // else
                    // {
                    //     bool withinTrueBounds = VectorUtil.IsPointWithinPolygon(vertexGrid[x, z].position, innerCorners);
                    //     if (withinTrueBounds == false)
                    //     {
                    //         vertexGrid[x, z].ignoreSmooth = true;
                    //     }

                    //     if (foundElderIX_X != -1)
                    //     {
                    //         (Vector3 closestPoint, float edgeDistance, int side) = VectorUtil.GetClosestPoint_XZ_WithDistanceAndIndex(sides, vertexGrid[x, z].position);
                    //         if (closestPoint != Vector3.zero && side != 0 && side != 3)
                    //         {
                    //             elderVertexGrid[foundElderIX_X, foundElderIX_Z].markedForRemoval = true;
                    //             // elderVertexGrid[foundElderIX_X, foundElderIX_Z].ignoreSmooth = true;
                    //         }
                    //     }
                    // }
                }
            }
            return vertexGrid;
        }

        public static TerrainVertex[,] Generate_TerrainVertexGrid(Vector2 worldspaceCoordinate, Vector3 position, Transform transform, int areaSize, int vertexDensity = 100)
        {
            List<Vector3> rectangleCorners = VectorUtil.HexagonCornersToRectangle(position, areaSize);

            TerrainVertex[,] vertexGrid = TerrainVertexUtil.GenerateRectangleGridInHexagon(
                                position,
                                areaSize,
                                rectangleCorners[2],
                                rectangleCorners[3],
                                rectangleCorners[0],
                                rectangleCorners[1],
                                transform,
                                worldspaceCoordinate,
                                vertexDensity,
                                5
                            );

            return vertexGrid;
        }


        public static TerrainVertex[,] GenerateVertexGridInHexagon(
            Vector3 centerPos,
            int size,
            Vector3 topLeft,
            Vector3 topRight,
            Vector3 bottomLeft,
            Vector3 bottomRight,
            List<Vector3> neighborTerrainEdgeVertices,
            int steps = 50,
            float proximityMax = 3f
        )
        {
            List<Vector3> corners = HexCoreUtil.GenerateHexagonPoints(centerPos, size).ToList();
            // List<Vector3> dottedEdgeLine = VectorUtil.GenerateDottedLine(corners.ToList(), 75);

            // Calculate the step sizes along each axis
            float xStep = 1f / steps * (topRight - topLeft).magnitude;
            float zStep = 1f / steps * (bottomLeft - topLeft).magnitude;

            // Create the grid
            TerrainVertex[,] grid = new TerrainVertex[steps + 1, steps + 1];
            int currentIndex = 0;

            // Loop through each vertex in the grid
            for (int z = 0; z <= steps; z++)
            {
                for (int x = 0; x <= steps; x++)
                {
                    // Calculate the position of the vertex
                    Vector3 position = topLeft + xStep * x * (topRight - topLeft).normalized + zStep * z * (bottomLeft - topLeft).normalized;

                    bool isMeshEdge = false;
                    // Check if the point is within proximityMax distance of any point in neighborTerrainEdgeVertices on the x and z axis
                    foreach (Vector3 neighborPoint in neighborTerrainEdgeVertices)
                    {
                        if (Mathf.Abs(neighborPoint.x - position.x) < proximityMax && Mathf.Abs(neighborPoint.z - position.z) < proximityMax)
                        {
                            position = neighborPoint;
                            isMeshEdge = true;
                            break;
                        }
                    }

                    // if (isMeshEdge)
                    // {
                    //     Debug.Log("neighborTerrainEdgeVertex found");
                    // }

                    // Check if the point is within the edge bounds
                    // (bool withinBounds, float distance) = VectorUtil.IsPointWithinEdgeBounds_WithDistance2(position, centerPos, dottedEdgeLine, size);

                    bool withinBounds = VectorUtil.IsPointWithinPolygon(position, corners);

                    // if (!withinBounds) continue;
                    // if (!withinBounds && distance > 1f) continue;

                    // Create the TerrainVertex object
                    TerrainVertex vertex = new TerrainVertex()
                    {
                        position = position,
                        index_X = x,
                        index_Z = z,
                        index = currentIndex,
                        type = VertexType.Generic,
                        // isMeshEdge = isMeshEdge
                        parallelWorldspaceOwnerCoordinate = Vector2.positiveInfinity,
                        parallelVertexIndex_X = -1,
                        parallelVertexIndex_Z = -1
                    };

                    // Add the vertex to the grid
                    grid[x, z] = vertex;
                    currentIndex++;
                }
            }

            return grid;
        }


        // public static TerrainVertex[,] GenerateVertexGridInHexagon(
        //     Vector3 centerPos,
        //     int size,
        //     Vector3 topLeft,
        //     Vector3 topRight,
        //     Vector3 bottomLeft,
        //     Vector3 bottomRight,
        //     List<Vector3> neighborTerrainEdgeVertices,
        //     int steps = 50,
        //     float protimityMax = 0.4f
        // )
        // {
        //     Vector3[] corners = HexCoreUtil.GenerateHexagonPoints(centerPos, size);
        //     List<Vector3> dottedEdgeLine = VectorUtil.GenerateDottedLine(corners.ToList(), 75);

        //     // Calculate the step sizes along each axis
        //     float xStep = 1f / steps * (topRight - topLeft).magnitude;
        //     float zStep = 1f / steps * (bottomLeft - topLeft).magnitude;

        //     // Create the grid
        //     TerrainVertex[,] grid = new TerrainVertex[steps + 1, steps + 1];

        //     // Loop through each vertex in the grid
        //     for (int z = 0; z <= steps; z++)
        //     {
        //         for (int x = 0; x <= steps; x++)
        //         {
        //             // Calculate the position of the vertex
        //             Vector3 position = topLeft + xStep * x * (topRight - topLeft).normalized + zStep * z * (bottomLeft - topLeft).normalized;

        //             // Check if the point is within the edge bounds
        //             // (bool withinBounds, float distance) = VectorUtil.IsPointWithinEdgeBounds_WithDistance2(position, centerPos, dottedEdgeLine, size);

        //             bool withinBounds = VectorUtil.IsPointWithinPolygon(position, corners.ToList());

        //             // if (!withinBounds) continue;
        //             // if (!withinBounds && distance > 1f) continue;

        //             // Create the TerrainVertex object
        //             TerrainVertex vertex = new TerrainVertex()
        //             {
        //                 position = position,
        //                 index_X = x,
        //                 index_Z = z,
        //                 type = VertexType.Generic,
        //                 markedVoid = (withinBounds == false)
        //             };

        //             // Add the vertex to the grid
        //             grid[x, z] = vertex;
        //         }
        //     }

        //     return grid;
        // }


        public static TerrainVertex[,] GenerateRectangleGrid(Vector3 topLeft, Vector3 topRight, Vector3 bottomLeft, Vector3 bottomRight, Transform transform, Vector2 _worldspaceOwnerCoordinate, int vertexDensity = 50)
        {
            // Calculate the step sizes along each axis
            float xStep = 1f / vertexDensity * (topRight - topLeft).magnitude;
            float zStep = 1f / vertexDensity * (bottomLeft - topLeft).magnitude;

            // Create the grid
            TerrainVertex[,] grid = new TerrainVertex[vertexDensity + 1, vertexDensity + 1];

            // Loop through each vertex in the grid
            for (int z = 0; z <= vertexDensity; z++)
            {
                for (int x = 0; x <= vertexDensity; x++)
                {
                    // Calculate the position of the vertex
                    Vector3 position = topLeft + xStep * x * (topRight - topLeft).normalized + zStep * z * (bottomLeft - topLeft).normalized;

                    Vector3 rounded = VectorUtil.RoundVector3To1Decimal(position);
                    Vector2 aproximateCoord = new Vector2(rounded.x, rounded.z);
                    Vector3 worldCoord = transform.TransformVector(position);
                    Vector2Int noiseCoordinate = new Vector2Int((int)worldCoord.x, (int)worldCoord.z);

                    // Create the TerrainVertex object
                    TerrainVertex vertex = new TerrainVertex()
                    {
                        worldspaceOwnerCoordinate = _worldspaceOwnerCoordinate,

                        noiseCoordinate = noiseCoordinate,
                        aproximateCoord = aproximateCoord,
                        position = position,
                        index_X = x,
                        index_Z = z,
                        type = VertexType.Generic,

                        parallelWorldspaceOwnerCoordinate = Vector2.positiveInfinity,
                        parallelVertexIndex_X = -1,
                        parallelVertexIndex_Z = -1
                    };

                    // Add the vertex to the grid
                    grid[x, z] = vertex;
                }
            }

            return grid;
        }


        public static TerrainVertex[,] GenerateVertexGrid(Vector3 centerPosition, int areaSize, float scale, Vector2 _worldspaceOwnerCoordinate, VertexType vertexType = VertexType.Generic)
        {
            // Calculate the half size of the grid
            float halfSize = areaSize / 2.0f;

            // Get the number of steps in each direction using the scale parameter
            int steps = Mathf.RoundToInt(areaSize / scale);
            float stepSize = areaSize / steps;

            // Create a 2D array to store the vertices
            TerrainVertex[,] grid = new TerrainVertex[steps + 1, steps + 1];
            int index = 0;

            for (int x = 0; x < steps + 1; x++)
            {
                for (int z = 0; z < steps + 1; z++)
                {
                    float xPos = centerPosition.x - halfSize + x * stepSize;
                    float zPos = centerPosition.z - halfSize + z * stepSize;

                    Vector3 position = new Vector3(xPos, centerPosition.y, zPos);

                    Vector3 rounded = VectorUtil.RoundVector3To1Decimal(position);
                    Vector2 aproximateCoord = new Vector2(rounded.x, rounded.z);

                    Vector2Int noiseCoordinate = new Vector2Int((int)position.x, (int)position.z);

                    grid[x, z] = new TerrainVertex
                    {
                        worldspaceOwnerCoordinate = _worldspaceOwnerCoordinate,

                        noiseCoordinate = noiseCoordinate,
                        aproximateCoord = aproximateCoord,

                        position = new Vector3(xPos, centerPosition.y, zPos),
                        index = index,
                        index_X = x,
                        index_Z = z,
                        type = VertexType.Generic,

                        parallelWorldspaceOwnerCoordinate = Vector2.positiveInfinity,
                        parallelVertexIndex_X = -1,
                        parallelVertexIndex_Z = -1
                    };
                    index++;
                }
            }
            return grid;
        }

        public static TerrainVertex[,] GenerateVertexGrid(Vector3 centerPosition, int areaSize, VertexType vertexType = VertexType.Generic)
        {
            TerrainVertex[,] grid = new TerrainVertex[areaSize, areaSize];
            int index = 0;

            float halfSize = areaSize / 2.0f;

            for (int x = 0; x < areaSize; x++)
            {
                for (int z = 0; z < areaSize; z++)
                {
                    grid[x, z] = new TerrainVertex
                    {
                        position = new Vector3(centerPosition.x - halfSize + x, centerPosition.y, centerPosition.z - halfSize + z),
                        index = index,
                        index_X = x,
                        index_Z = z,
                        type = VertexType.Generic
                    };
                    index++;
                }
            }
            return grid;
        }

        public static TerrainVertex? GetClosestTerrainVertex(Vector3 position, List<TerrainVertex> vertexList)
        {
            TerrainVertex? nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (TerrainVertex vert in vertexList)
            {
                float dist = Vector2.Distance(position, vert.position);
                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    nearest = vert;
                }

            }
            return nearest;
        }



        public static List<Vector3> GetTerrainVertexSurface(List<(int, int)> vertexIndices, TerrainVertex[,] vertexGrid)
        {
            List<Vector3> surface = new List<Vector3>();

            foreach (var indices in vertexIndices)
            {
                (int x, int z) = indices;
                surface.Add(vertexGrid[x, z].position);
            }
            // Debug.Log("terrainVertexSurface count: " + surface.Count);

            return surface;
        }

        public static List<TerrainVertex> GetTerrainVertexSurface_TVert(List<(int, int)> vertexIndices, TerrainVertex[,] vertexGrid)
        {
            List<TerrainVertex> surface = new List<TerrainVertex>();

            foreach (var indices in vertexIndices)
            {
                (int x, int z) = indices;
                surface.Add(vertexGrid[x, z]);
            }
            return surface;
        }
        public static List<List<Vector3>> GetTerrainVertexSurfaces(Dictionary<Vector2, List<(int, int)>> vertexIndicesByXZPos, TerrainVertex[,] vertexGrid)
        {
            List<List<Vector3>> surfaces = new List<List<Vector3>>();

            foreach (var kvp in vertexIndicesByXZPos)
            {
                surfaces.Add(GetTerrainVertexSurface(kvp.Value, vertexGrid));
            }
            return surfaces;
        }
        public static List<List<TerrainVertex>> GetTerrainVertexSurfaces(Dictionary<TerrainVertex, List<(int, int)>> vertexIndicesByXZPos, TerrainVertex[,] vertexGrid)
        {
            List<List<TerrainVertex>> surfaces = new List<List<TerrainVertex>>();

            foreach (var kvp in vertexIndicesByXZPos)
            {
                surfaces.Add(GetTerrainVertexSurface_TVert(kvp.Value, vertexGrid));
            }
            return surfaces;
        }
        public static Dictionary<Vector2, List<Vector3>> GetTerrainVertexSurfacesByXZPos(Dictionary<Vector2, List<(int, int)>> vertexIndicesByXZPos, TerrainVertex[,] vertexGrid)
        {
            Dictionary<Vector2, List<Vector3>> surfacesByCenterXZ = new Dictionary<Vector2, List<Vector3>>();

            foreach (var kvp in vertexIndicesByXZPos)
            {
                surfacesByCenterXZ.Add(kvp.Key, GetTerrainVertexSurface(kvp.Value, vertexGrid));
            }
            return surfacesByCenterXZ;
        }
        public static Dictionary<Vector2, List<TerrainVertex>> GetTerrainVertexSurfacesByXZPos_TVert(Dictionary<Vector2, List<(int, int)>> vertexIndicesByXZPos, TerrainVertex[,] vertexGrid)
        {
            Dictionary<Vector2, List<TerrainVertex>> surfacesByCenterXZ = new Dictionary<Vector2, List<TerrainVertex>>();

            foreach (var kvp in vertexIndicesByXZPos)
            {
                surfacesByCenterXZ.Add(kvp.Key, GetTerrainVertexSurface_TVert(kvp.Value, vertexGrid));
            }
            return surfacesByCenterXZ;
        }



        public static void SmoothVertexList(List<TerrainVertex> vertexList, TerrainVertex[,] vertexGrid, float smoothingFactor = 1f, int neighborhoodSize = 8, float maxDistance = 12f)
        {
            vertexList.Sort((v1, v2) =>
            {
                int result = v1.position.x.CompareTo(v2.position.x);
                if (result == 0)
                    result = v1.position.z.CompareTo(v2.position.z);
                return result;
            });

            for (int i = 1; i < vertexList.Count - 1; i++)
            {
                TerrainVertex currVertex = vertexList[i];
                TerrainVertex prevVertex = vertexList[i - 1];
                TerrainVertex nextVertex = vertexList[i + 1];

                // Calculate the weighted average elevation of the current vertex, previous vertex, and next vertex, as well as additional neighbors in the wider neighborhood within a maximum distance
                int neighborhoodStartIndex = Mathf.Max(i - neighborhoodSize, 0);
                int neighborhoodEndIndex = Mathf.Min(i + neighborhoodSize, vertexList.Count - 1);
                float totalWeight = 1f;
                float weightedElevation = currVertex.position.y;
                for (int j = neighborhoodStartIndex; j <= neighborhoodEndIndex; j++)
                {
                    if (j == i)
                        continue;

                    Vector2 neighPosXZ = new Vector2(vertexList[j].position.x, vertexList[j].position.z);
                    Vector2 currVertexPosXZ = new Vector2(currVertex.position.x, currVertex.position.z);
                    float distance = Vector2.Distance(neighPosXZ, currVertexPosXZ);
                    if (distance <= maxDistance)
                    {
                        float weight = 1f + smoothingFactor * (1f - distance / maxDistance);
                        weightedElevation += vertexList[j].position.y * weight;
                        totalWeight += weight;
                    }
                }
                weightedElevation /= totalWeight;
                currVertex.position.y = weightedElevation;

                vertexList[i] = currVertex;
                vertexGrid[vertexList[i].index / vertexGrid.GetLength(0), vertexList[i].index % vertexGrid.GetLength(0)].position = currVertex.position;
                vertexGrid[vertexList[i].index / vertexGrid.GetLength(0), vertexList[i].index % vertexGrid.GetLength(0)].type = VertexType.Road;
            }
        }


        // public static Dictionary<Vector2, TerrainVertex[,]> SmoothWorldSpaceVertexData(
        //     Dictionary<Vector2,
        //     TerrainVertex[,]> vertexGrids,
        //     bool useIgnoreRules,
        //     bool markSmoothIgnore,
        //     float ratio,
        //     int neighborDepth = 4,
        //     int cellGridWeight = 2,
        //     int inheritedWeight = 3
        // )
        // {
        //     // Consolidate Grids
        //     List<TerrainVertex> verticesToSmooth = new List<TerrainVertex>();
        //     foreach (var kvp in vertexGrids)
        //     {
        //         TerrainVertex[,] grid = kvp.Value;

        //         for (int z = 0; z < grid.GetLength(1); z++)
        //         {
        //             for (int x = 0; x < grid.GetLength(0); x++)
        //             {
        //                 verticesToSmooth.Add(grid[x, z]);
        //             }
        //         }
        //     }

        //     SmoothTerrainVertexList(
        //         verticesToSmooth,
        //         vertexGrids,
        //         useIgnoreRules,
        //         markSmoothIgnore,
        //         ratio,
        //         neighborDepth,
        //         cellGridWeight,
        //         inheritedWeight
        //     );

        //     return vertexGrids;
        // }


        public static void SmoothElevationAroundCellGrid(TerrainVertex[,] vertexGrid, int neighborDepth, float ratio = 1f, int cellGridWeight = 3)
        {
            List<TerrainVertex> verticesToSmooth = new List<TerrainVertex>();
            List<Vector3> vertexPositionsInGrid = new List<Vector3>();
            for (int z = 0; z < vertexGrid.GetLength(1); z++)
            {
                for (int x = 0; x < vertexGrid.GetLength(0); x++)
                {
                    TerrainVertex vertex = vertexGrid[x, z];
                    if (vertex.markedForRemoval) continue;

                    if (vertex.isCellCornerPoint || vertex.isInMicroCell)
                    {
                        continue;
                    }

                    if (
                    vertex.isCellCenterPoint
                    // || vertex.isCellCornerPoint
                    || vertex.isOnTunnelGroundEntry
                    || vertex.isOnTunnelCell
                    )
                    // if (IsCellVertex(vertex))
                    {
                        vertexPositionsInGrid.Add(vertex.position);
                        continue;
                    }
                    verticesToSmooth.Add(vertex);
                }
            }

            Vector3 centerPos = VectorUtil.CalculateCenterPositionFromPoints(vertexPositionsInGrid);
            // verticesToSmooth.Sort((a, b) => VectorUtil.DistanceXZ(a.position, centerPos).CompareTo(VectorUtil.DistanceXZ(b.position, centerPos)));
            verticesToSmooth.Sort((a, b) => VectorUtil.DistanceXZ(b.position, centerPos).CompareTo(VectorUtil.DistanceXZ(a.position, centerPos)));

            // SmoothTerrainVertexList(verticesToSmooth, vertexGrid, ratio, neighborDepth, cellGridWeight);
            int inheritedWeight = 4;
            bool markSmoothIgnore = false;

            int targetWeight = 4;

            // SmoothTerrainVertexList_TargetPoint(
            //     verticesToSmooth,
            //     vertexGrid,
            //     centerPos,
            //     targetWeight,
            //     markSmoothIgnore,
            //     ratio,
            //     neighborDepth,
            //     cellGridWeight,
            //     inheritedWeight
            // );

            SmoothTerrainVertexList(
                verticesToSmooth,
                vertexGrid,
                markSmoothIgnore,
                ratio,
                neighborDepth,
                cellGridWeight,
                inheritedWeight
            //  useIgnoreRules
            );

        }


        public static void SmoothWorldAreaVertexElevationTowardsCenter(
            TerrainVertex[,] vertexGrid,
            Vector3 center,
            bool useIgnoreRules,
            int neighborDepth = 4,
            int cellGridWeight = 2,
            int inheritedWeight = 3,
            float ratio = 1f,
            bool markSmoothIgnore = false
        )
        {
            List<TerrainVertex> verticesToSmooth = new List<TerrainVertex>();

            // Loop through each vertex in the grid and add it to the list if it's within the radius
            for (int z = 0; z < vertexGrid.GetLength(1); z++)
            {
                for (int x = 0; x < vertexGrid.GetLength(0); x++)
                {
                    TerrainVertex vertex = vertexGrid[x, z];
                    if (vertex.markedForRemoval) continue;
                    // float distance = VectorUtil.DistanceXZ(vertex.position, center);
                    // if (distance <= radius)
                    // {
                    verticesToSmooth.Add(vertex);
                    // }
                }
            }

            verticesToSmooth.Sort((a, b) => VectorUtil.DistanceXZ(b.position, center).CompareTo(VectorUtil.DistanceXZ(a.position, center)));

            // SmoothTerrainVertexList(verticesToSmooth, vertexGrid, ratio, neighborDepth, cellGridWeight, inheritedWeight, markSmoothIgnore, useIgnoreRules);

            SmoothTerrainVertexList(
                verticesToSmooth,
                vertexGrid,
                markSmoothIgnore,
                ratio,
                neighborDepth,
                cellGridWeight,
                inheritedWeight,
                useIgnoreRules
            );
        }

        public static void SmoothWorldAreaVertexElevationTowardsCenter__V2(
            TerrainVertex[,] vertexGrid,
            Dictionary<Vector2, TerrainVertex[,]> vertexGridDataByWorldSpaceCoordinate,
            Vector3 center,
            bool useIgnoreRules,
            int neighborDepth = 4,
            int cellGridWeight = 2,
            int inheritedWeight = 2,
            float ratio = 1f,
            bool markSmoothIgnore = false
        )
        {
            List<TerrainVertex> verticesToSmooth = new List<TerrainVertex>();
            // Loop through each vertex in the grid and add it to the list if it's within the radius
            for (int z = 0; z < vertexGrid.GetLength(1); z++)
            {
                for (int x = 0; x < vertexGrid.GetLength(0); x++)
                {
                    TerrainVertex vertex = vertexGrid[x, z];
                    if (vertex.markedForRemoval) continue;
                    // float distance = VectorUtil.DistanceXZ(vertex.position, center);
                    // if (distance <= radius)
                    // {
                    verticesToSmooth.Add(vertex);
                    // }
                }
            }
            verticesToSmooth.Sort((a, b) => VectorUtil.DistanceXZ(b.position, center).CompareTo(VectorUtil.DistanceXZ(a.position, center)));

            SmoothTerrainVertexList__V2(
                verticesToSmooth,
                vertexGrid,
                vertexGridDataByWorldSpaceCoordinate,
                markSmoothIgnore,
                ratio,
                neighborDepth,
                cellGridWeight,
                inheritedWeight,
                useIgnoreRules
            );
        }

        public static void SmoothVertices_Path(List<TerrainVertex> vertexList, TerrainVertex[,] vertexGrid,
            Vector3 directionCenter,
            float ratio = 1f,
            int neighborDepth = 4,
            int cellGridWeight = 2,
            int inheritedWeight = 3
        )
        {
            // Sort the vertices by distance from the center
            vertexList.Sort((a, b) => VectorUtil.DistanceXZ(b.position, directionCenter).CompareTo(VectorUtil.DistanceXZ(a.position, directionCenter)));
            // SmoothTerrainVertexList(vertexList, vertexGrid, ratio, neighborDepth, cellGridWeight, inheritedWeight, true);
            bool markSmoothIgnore = false;

            SmoothTerrainVertexList(
                vertexList,
                vertexGrid,
                markSmoothIgnore,
                ratio,
                neighborDepth,
                cellGridWeight,
                inheritedWeight
            //  useIgnoreRules
            );
        }

        public static void SmoothVerticesTowardsCenter(List<TerrainVertex> vertexList, TerrainVertex[,] vertexGrid, Vector3 center,
            float ratio = 1f, int neighborDepth = 4, int cellGridWeight = 2, int inheritedWeight = 3)
        {
            // Sort the vertices by distance from the center
            vertexList.Sort((a, b) => VectorUtil.DistanceXZ(b.position, center).CompareTo(VectorUtil.DistanceXZ(a.position, center)));
            // SmoothTerrainVertexList(vertexList, vertexGrid, ratio, neighborDepth, cellGridWeight, inheritedWeight, true);
            bool markSmoothIgnore = false;

            SmoothTerrainVertexList(
                vertexList,
                vertexGrid,
                markSmoothIgnore,
                ratio,
                neighborDepth,
                cellGridWeight,
                inheritedWeight
            //  useIgnoreRules
            );
            // Vector3 centerVertexPos = vertexList[vertexList.Count - 1].position;
            // // Loop through each vertex and adjust its elevation towards the average elevation of its neighbors
            // foreach (TerrainVertex vertex in vertexList)
            // {
            //     if (vertex.isInherited) continue;

            //     // float distance = VectorUtil.DistanceXZ(vertex.position, center);
            //     // float ratio = 1f;//- (distance / radius);
            //     if (ratio > 0f)
            //     {
            //         // Get the average elevation of the vertex and its neighbors
            //         float neighborElevation = 0f;
            //         int neighborCount = 0;

            //         for (int dx = -1; dx <= neighborDepth; dx++)
            //         {
            //             for (int dz = -1; dz <= neighborDepth; dz++)
            //             {
            //                 if (dx == 0 && dz == 0) continue;

            //                 int nx = vertex.index_X + dx;
            //                 int nz = vertex.index_Z + dz;
            //                 if (nx < 0 || nx >= vertexGrid.GetLength(0) || nz < 0 || nz >= vertexGrid.GetLength(1)) continue;

            //                 TerrainVertex neighborVertex = vertexGrid[nx, nz];
            //                 if (neighborVertex.markedForRemoval) continue;

            //                 if (neighborVertex.isInherited)
            //                 {

            //                     neighborElevation += (neighborVertex.position.y * inheritedWeightMult);
            //                     neighborCount += (1 * inheritedWeightMult);
            //                 }
            //                 else
            //                 {
            //                     neighborElevation += neighborVertex.position.y;
            //                     neighborCount++;
            //                 }
            //             }
            //         }

            //         neighborElevation /= neighborCount;

            //         // Lerp between the current elevation and the average neighbor elevation based on the ratio
            //         float elevation = Mathf.Lerp(vertex.position.y, neighborElevation, ratio);
            //         vertexGrid[vertex.index_X, vertex.index_Z].position.y = elevation;
            //     }
            // }
        }
        public static bool SmoothTerrainVertexByNeighbors(
            TerrainVertex vertex,
            TerrainVertex[,] vertexGrid,
            bool markSmoothIgnore,
            float ratio,
            int neighborDepth = 6,
            int cellGridWeight = 2,
            int inheritedWeight = 3,
            bool useIgnoreRules = true
        )
        {
            // if (useIgnoreRules && (vertex.ignoreSmooth || vertex.isInherited)) return false;

            if (ratio <= 0f) return false;

            // Get the average elevation of the vertex and its neighbors
            float neighborElevation = 0f;
            int neighborCount = 0;

            // float heightDifferenceCap = 0.66f;
            // int heightModMult = 1;

            // bool capHeight = false;

            for (int dx = -1; dx <= neighborDepth; dx++)
            {
                for (int dz = -1; dz <= neighborDepth; dz++)
                {
                    if (dx == 0 && dz == 0) continue;

                    int nx = vertex.index_X + dx;
                    int nz = vertex.index_Z + dz;
                    if (nx < 0 || nx >= vertexGrid.GetLength(0) || nz < 0 || nz >= vertexGrid.GetLength(1)) continue;

                    TerrainVertex neighborVertex = vertexGrid[nx, nz];
                    if (neighborVertex.markedForRemoval) continue;

                    if (neighborVertex.ignoreSmooth)
                    {
                        neighborElevation += (neighborVertex.position.y * 2);
                        neighborCount += (1 * 2);
                    }

                    if (IsCellVertex(neighborVertex))
                    {
                        neighborElevation += (neighborVertex.position.y * cellGridWeight);
                        neighborCount += (1 * cellGridWeight);
                    }

                    if (neighborVertex.isInherited)
                    {
                        neighborElevation += (neighborVertex.position.y * inheritedWeight);
                        neighborCount += (1 * inheritedWeight);
                    }
                    else
                    {
                        // float elevationDiff = Mathf.Abs(Mathf.Abs(neighborVertex.position.y) - Mathf.Abs(vertex.position.y));

                        // if (elevationDiff > heightDifferenceCap)
                        // // if ((neighborVertex.hasCellVertexNeighbor && elevationDiff > heightDifferenceCap) || (elevationDiff > heightDifferenceCap * 1.33f))
                        // {
                        //     // float avgYelevation = Mathf.Lerp(vertex.position.y, Mathf.Lerp(vertex.position.y, neighborVertex.position.y, 1f), 1f);
                        //     float mult;
                        //     if (vertex.position.y > neighborVertex.position.y)
                        //     {
                        //         mult = Mathf.Clamp01(neighborVertex.position.y / vertex.position.y);
                        //     }
                        //     else
                        //     {
                        //         mult = Mathf.Clamp01(vertex.position.y / neighborVertex.position.y);
                        //     }
                        //     float avgYelevation = (vertex.position.y * mult);
                        //     // Debug.Log("diff Mult: " + mult + ", avgYelevation: " + avgYelevation);

                        //     neighborElevation += (avgYelevation);
                        //     neighborCount += (1 * heightModMult);

                        //     if (IsCellVertex(neighborVertex))
                        //     {
                        //         neighborElevation += (avgYelevation);
                        //         neighborCount += (1 * cellGridWeight);
                        //     }
                        // }
                        // else
                        // {
                        neighborElevation += neighborVertex.position.y;
                        neighborCount++;
                        // }
                    }
                }
            }

            neighborElevation /= neighborCount;

            // Lerp between the current elevation and the average neighbor elevation based on the ratio
            // if (capHeight)
            // {
            //     // float elevation = Mathf.Lerp(vertex.position.y, vertex.position.y + heightDifferenceCap, ratio);
            //     vertexGrid[vertex.index_X, vertex.index_Z].position.y = neighborElevation;
            // }
            // else
            // {
            float elevation = Mathf.Lerp(vertex.position.y, neighborElevation, ratio);
            vertexGrid[vertex.index_X, vertex.index_Z].position.y = elevation;
            // }

            if (markSmoothIgnore) vertexGrid[vertex.index_X, vertex.index_Z].ignoreSmooth = true;

            return true;
        }

        public static bool SmoothTerrainVertexByNeighbors__V2(
            TerrainVertex vertex,
            TerrainVertex[,] vertexGrid,
            Dictionary<Vector2, TerrainVertex[,]> vertexGridDataByWorldSpaceCoordinate,
            bool markSmoothIgnore,
            float ratio,
            int neighborDepth = 6,
            int cellGridWeight = 2,
            int inheritedWeight = 3,
            bool useIgnoreRules = true
        )
        {
            // if (useIgnoreRules && (vertex.ignoreSmooth || vertex.isInherited)) return false;

            if (vertex.isInherited || vertex.parallelVertexIndex_X != -1)
            {
                vertexGrid[vertex.index_X, vertex.index_Z].position.y = vertexGridDataByWorldSpaceCoordinate[vertex.parallelWorldspaceOwnerCoordinate][vertex.parallelVertexIndex_X, vertex.parallelVertexIndex_Z].position.y - 0.3f;
                // vertexGridDataByWorldSpaceCoordinate[vertex.parallelWorldspaceOwnerCoordinate][vertex.parallelVertexIndex_X, vertex.parallelVertexIndex_Z].position.y = vertexGrid[vertex.index_X, vertex.index_Z].position.y - 0.2f;
                return true;
            }


            if (ratio <= 0f) return false;

            // Get the average elevation of the vertex and its neighbors
            float neighborElevation = 0f;
            int neighborCount = 0;

            for (int dx = -1; dx <= neighborDepth; dx++)
            {
                for (int dz = -1; dz <= neighborDepth; dz++)
                {
                    if (dx == 0 && dz == 0) continue;

                    int nx = vertex.index_X + dx;
                    int nz = vertex.index_Z + dz;
                    if (nx < 0 || nx >= vertexGrid.GetLength(0) || nz < 0 || nz >= vertexGrid.GetLength(1)) continue;

                    TerrainVertex neighborVertex = vertexGrid[nx, nz];

                    if (neighborVertex.markedForRemoval) continue;

                    if (neighborVertex.ignoreSmooth)
                    {
                        neighborElevation += (neighborVertex.position.y * 2);
                        neighborCount += (1 * 2);
                    }

                    if (IsCellVertex(neighborVertex))
                    {
                        neighborElevation += (neighborVertex.position.y * cellGridWeight);
                        neighborCount += (1 * cellGridWeight);
                    }

                    if (neighborVertex.isInherited)
                    {
                        neighborElevation += (neighborVertex.position.y * inheritedWeight);
                        neighborCount += (1 * inheritedWeight);
                    }
                    else
                    {
                        neighborElevation += neighborVertex.position.y;
                        neighborCount++;
                    }
                }
            }

            neighborElevation /= neighborCount;

            float elevation = Mathf.Lerp(vertex.position.y, neighborElevation, ratio);
            vertexGrid[vertex.index_X, vertex.index_Z].position.y = elevation;

            // if (vertex.isInherited || vertex.parallelVertexIndex_X != -1)
            // {
            //     vertexGridDataByWorldSpaceCoordinate[vertex.parallelWorldspaceOwnerCoordinate][vertex.parallelVertexIndex_X, vertex.parallelVertexIndex_Z].position.y = vertexGrid[vertex.index_X, vertex.index_Z].position.y - 0.2f;
            // }

            if (markSmoothIgnore) vertexGrid[vertex.index_X, vertex.index_Z].ignoreSmooth = true;

            return true;
        }




        public static void SmoothTerrainVertexList(
            List<TerrainVertex> vertexList,
            TerrainVertex[,] vertexGrid,
            bool markSmoothIgnore,
            float ratio,
            int neighborDepth = 5,
            int cellGridWeight = 2,
            int inheritedWeight = 4,
            bool useIgnoreRules = true
        )
        {
            // Loop through each vertex and adjust its elevation towards the average elevation of its neighbors
            foreach (TerrainVertex vertex in vertexList)
            {
                if (useIgnoreRules && (vertex.ignoreSmooth || vertex.isInherited)) continue;

                if (ratio > 0f)
                {
                    // Get the average elevation of the vertex and its neighbors
                    float neighborElevation = 0f;
                    int neighborCount = 0;

                    for (int dx = -1; dx <= neighborDepth; dx++)
                    {
                        for (int dz = -1; dz <= neighborDepth; dz++)
                        {
                            if (dx == 0 && dz == 0) continue;

                            int nx = vertex.index_X + dx;
                            int nz = vertex.index_Z + dz;
                            if (nx < 0 || nx >= vertexGrid.GetLength(0) || nz < 0 || nz >= vertexGrid.GetLength(1)) continue;

                            TerrainVertex neighborVertex = vertexGrid[nx, nz];
                            // if (neighborVertex.markedForRemoval) continue;

                            if (neighborVertex.isInherited || neighborVertex.markedForRemoval)
                            // if (neighborVertex.isInherited)
                            {
                                neighborElevation += (neighborVertex.position.y * inheritedWeight);
                                neighborCount += (1 * inheritedWeight);
                            }
                            else if (IsCellVertex(neighborVertex))
                            {
                                neighborElevation += (neighborVertex.position.y * cellGridWeight);
                                neighborCount += (1 * cellGridWeight);
                            }
                            else
                            {
                                neighborElevation += neighborVertex.position.y;
                                neighborCount++;
                            }
                        }
                    }

                    neighborElevation /= neighborCount;

                    // Lerp between the current elevation and the average neighbor elevation based on the ratio
                    float elevation = Mathf.Lerp(vertex.position.y, neighborElevation, ratio);
                    vertexGrid[vertex.index_X, vertex.index_Z].position.y = elevation;
                    if (markSmoothIgnore) vertexGrid[vertex.index_X, vertex.index_Z].ignoreSmooth = true;
                }
            }
        }

        public static void SmoothTerrainVertexList__V2(
            List<TerrainVertex> vertexList,
            TerrainVertex[,] vertexGrid,
            Dictionary<Vector2, TerrainVertex[,]> vertexGridDataByWorldSpaceCoordinate,
            bool markSmoothIgnore,
            float ratio,
            int neighborDepth = 4,
            int cellGridWeight = 1,
            int inheritedWeight = 3,
            bool useIgnoreRules = true
        )
        {

            int gridLength0 = vertexGrid.GetLength(0);
            int gridLength1 = vertexGrid.GetLength(1);
            // int quarter0 = gridLength0 / 4;
            // int quarter1 = gridLength1 / 4;
            // Loop through each vertex and adjust its elevation towards the average elevation of its neighbors
            foreach (TerrainVertex vertex in vertexList)
            {

                if (IsCellVertex(vertex)) continue;
                // if (useIgnoreRules && (vertex.ignoreSmooth || vertex.isInherited)) continue;

                float mod = 0;
                bool isDoupelganger = false;
                if (vertex.isInherited || vertex.parallelVertexIndex_X != -1)
                {

                    // if (
                    //     vertex.index_X <= (quarter0) ||
                    //     vertex.index_X >= (gridLength0 - quarter0) ||
                    //     vertex.index_Z <= (quarter1) ||
                    //     vertex.index_Z >= (gridLength1 - quarter1)
                    // )
                    // {
                    //     mod = 1f;
                    // }
                    isDoupelganger = true;
                    vertexGrid[vertex.index_X, vertex.index_Z].position.y = (vertexGridDataByWorldSpaceCoordinate[vertex.parallelWorldspaceOwnerCoordinate][vertex.parallelVertexIndex_X, vertex.parallelVertexIndex_Z].position.y + mod);
                    // vertexGridDataByWorldSpaceCoordinate[vertex.parallelWorldspaceOwnerCoordinate][vertex.parallelVertexIndex_X, vertex.parallelVertexIndex_Z].position.y = vertexGrid[vertex.index_X, vertex.index_Z].position.y - 0.2f;
                    // continue;
                }


                if (ratio > 0f)
                {
                    // Get the average elevation of the vertex and its neighbors
                    float neighborElevation = 0f;
                    int neighborCount = 0;

                    for (int dx = -1; dx <= neighborDepth; dx++)
                    {
                        for (int dz = -1; dz <= neighborDepth; dz++)
                        {
                            if (dx == 0 && dz == 0) continue;
                            int nx;
                            int nz;
                            if (isDoupelganger)
                            {
                                nx = vertex.parallelVertexIndex_X + dx;
                                nz = vertex.parallelVertexIndex_Z + dz;
                            }
                            else
                            {
                                nx = vertex.index_X + dx;
                                nz = vertex.index_Z + dz;
                            }

                            if (nx < 0 || nx >= gridLength0 || nz < 0 || nz >= gridLength1) continue;

                            TerrainVertex neighborVertex = isDoupelganger ? vertexGridDataByWorldSpaceCoordinate[vertex.parallelWorldspaceOwnerCoordinate][nx, nz] : vertexGrid[nx, nz];
                            // if (neighborVertex.markedForRemoval) continue;

                            // if (neighborVertex.isInherited || neighborVertex.markedForRemoval)
                            if (neighborVertex.isInherited)
                            {
                                neighborElevation += (neighborVertex.position.y * inheritedWeight);
                                neighborCount += (1 * inheritedWeight);
                            }
                            else if (IsCellVertex(neighborVertex))
                            {
                                neighborElevation += (neighborVertex.position.y * cellGridWeight);
                                neighborCount += (1 * cellGridWeight);
                            }
                            else
                            {
                                neighborElevation += neighborVertex.position.y;
                                neighborCount++;
                            }
                        }
                    }

                    neighborElevation /= neighborCount;

                    // Lerp between the current elevation and the average neighbor elevation based on the ratio
                    float elevation = Mathf.Lerp(vertex.position.y, neighborElevation, ratio);
                    vertexGrid[vertex.index_X, vertex.index_Z].position.y = elevation;


                    if (vertex.isInherited || vertex.parallelVertexIndex_X != -1)
                    {
                        // vertexGrid[vertex.index_X, vertex.index_Z].position.y = vertexGridDataByWorldSpaceCoordinate[vertex.parallelWorldspaceOwnerCoordinate][vertex.parallelVertexIndex_X, vertex.parallelVertexIndex_Z].position.y;
                        vertexGridDataByWorldSpaceCoordinate[vertex.parallelWorldspaceOwnerCoordinate][vertex.parallelVertexIndex_X, vertex.parallelVertexIndex_Z].position.y = (vertexGrid[vertex.index_X, vertex.index_Z].position.y);
                        // continue;
                    }


                    if (markSmoothIgnore) vertexGrid[vertex.index_X, vertex.index_Z].ignoreSmooth = true;
                }
            }
        }


        //
        // public static void SmoothTerrainVertexList(
        //     List<TerrainVertex> vertexList,
        //     Dictionary<Vector2, TerrainVertex[,]> vertexGrids,
        //     bool useIgnoreRules,
        //     bool markSmoothIgnore,
        //     float ratio,
        //     int neighborDepth = 4,
        //     int cellGridWeight = 2,
        //     int inheritedWeight = 3
        // )
        // {
        //     // Loop through each vertex and adjust its elevation towards the average elevation of its neighbors
        //     foreach (TerrainVertex vertex in vertexList)
        //     {
        //         if (useIgnoreRules && (vertex.ignoreSmooth || vertex.isInherited)) continue;

        //         if (ratio > 0f)
        //         {
        //             Vector2 worldSpaceCoord = vertex.worldspaceOwnerCoordinate;

        //             if (vertexGrids.ContainsKey(worldSpaceCoord) == false)
        //             {
        //                 Debug.LogError("Missing worldSpaceCoord: " + worldSpaceCoord);
        //                 break;
        //             }
        //             // Get the average elevation of the vertex and its neighbors
        //             float neighborElevation = 0f;
        //             int neighborCount = 0;

        //             for (int dx = -1; dx <= neighborDepth; dx++)
        //             {
        //                 for (int dz = -1; dz <= neighborDepth; dz++)
        //                 {
        //                     if (dx == 0 && dz == 0) continue;

        //                     int nx = vertex.index_X + dx;
        //                     int nz = vertex.index_Z + dz;
        //                     if (nx < 0 || nx >= vertexGrids[worldSpaceCoord].GetLength(0) || nz < 0 || nz >= vertexGrids[worldSpaceCoord].GetLength(1)) continue;

        //                     TerrainVertex neighborVertex = vertexGrids[worldSpaceCoord][nx, nz];

        //                     // if (neighborVertex.markedForRemoval) continue;

        //                     if (neighborVertex.isInherited || neighborVertex.markedForRemoval)
        //                     // if (neighborVertex.isInherited)
        //                     {
        //                         neighborElevation += (neighborVertex.position.y * inheritedWeight);
        //                         neighborCount += (1 * inheritedWeight);
        //                     }
        //                     else if (IsCellVertex(neighborVertex))
        //                     {
        //                         neighborElevation += (neighborVertex.position.y * cellGridWeight);
        //                         neighborCount += (1 * cellGridWeight);
        //                     }
        //                     else
        //                     {
        //                         neighborElevation += neighborVertex.position.y;
        //                         neighborCount++;
        //                     }
        //                 }
        //             }

        //             neighborElevation /= neighborCount;

        //             // Lerp between the current elevation and the average neighbor elevation based on the ratio
        //             float elevation = Mathf.Lerp(vertex.position.y, neighborElevation, ratio);
        //             vertexGrids[worldSpaceCoord][vertex.index_X, vertex.index_Z].position.y = elevation;
        //             if (markSmoothIgnore) vertexGrids[worldSpaceCoord][vertex.index_X, vertex.index_Z].ignoreSmooth = true;
        //         }
        //     }
        // }

        public static List<List<Vector3>> DisplayTerrainVertexSurfaces(Dictionary<Vector2, List<(int, int)>> vertexIndicesByXZPos, TerrainVertex[,] vertexGrid)
        {
            List<List<Vector3>> surfaces = new List<List<Vector3>>();

            foreach (var kvp in vertexIndicesByXZPos)
            {
                Vector3 pos = new Vector3(kvp.Key.x, 0, kvp.Key.y);
                Vector3[,] rawVertexGrid2D = HexagonCellPrototype.GenerateRectangularGrid(pos, 20, 20);

                foreach (var item in rawVertexGrid2D)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(item, 0.33f);
                }

                // foreach (var indices in kvp.Value)
                // {
                //     (int x, int z) = indices;
                //     Gizmos.color = Color.magenta;
                //     Gizmos.DrawSphere(vertexGrid[x, z].position, 0.4f);
                // }
            }
            return surfaces;
        }

        public static void DisplayTerrainVertices(TerrainVertex[,] vertexGrid, ShowVertexState showState, Transform transform)
        {
            if (vertexGrid == null) return;

            Color brown = new Color(0.4f, 0.2f, 0f);
            Color orange = new Color(1f, 0.5f, 0f);
            Color purple = new Color(0.8f, 0.2f, 1f);

            for (int x = 0; x < vertexGrid.GetLength(0); x++)
            {
                for (int z = 0; z < vertexGrid.GetLength(1); z++)
                {
                    TerrainVertex currentVertex = vertexGrid[x, z];
                    DisplayTerrainVertex(currentVertex, showState, transform);

                    // bool show = false;
                    // float rad = 0.33f;
                    // Gizmos.color = Color.black;


                    // if (showState == ShowVertexState.All)
                    // {
                    //     show = true;
                    // }
                    // else if (showState == ShowVertexState.Cell)
                    // {
                    //     show = currentVertex.type == VertexType.Cell;
                    //     Gizmos.color = Color.red;

                    //     rad = 0.4f;
                    // }
                    // else if (showState == ShowVertexState.CellFlatGround)
                    // {
                    //     show = currentVertex.isFlatGroundCell;
                    //     Gizmos.color = Color.red;
                    //     rad = 0.45f;
                    // }
                    // else if (showState == ShowVertexState.MicroCell)
                    // {
                    //     show = currentVertex.isInMicroCell;
                    //     Gizmos.color = currentVertex.type == VertexType.Road ? Color.blue : Color.red;
                    //     rad = 0.4f;
                    // }
                    // else if (showState == ShowVertexState.Path)
                    // {
                    //     show = currentVertex.type == VertexType.Road;
                    //     Gizmos.color = Color.cyan;
                    //     rad = 0.66f;
                    // }
                    // else if (showState == ShowVertexState.Terrain)
                    // {
                    //     show = currentVertex.type == VertexType.Generic;
                    //     Gizmos.color = Color.red;
                    //     rad = 0.66f;
                    // }
                    // else if (showState == ShowVertexState.CellCenter)
                    // {
                    //     show = currentVertex.isCellCenterPoint;
                    //     Gizmos.color = Color.red;
                    //     rad = 0.66f;
                    // }
                    // else if (showState == ShowVertexState.CellCorner)
                    // {
                    //     show = currentVertex.isCellCornerPoint;
                    //     Gizmos.color = Color.red;
                    //     rad = 0.6f;
                    // }
                    // else if (showState == ShowVertexState.GridEdge)
                    // {
                    //     show = currentVertex.isOnTheEdgeOftheGrid;
                    //     Gizmos.color = Color.green;
                    //     rad = 0.66f;
                    // }
                    // else if (showState == ShowVertexState.IgnoreSmooth)
                    // {
                    //     show = currentVertex.ignoreSmooth;
                    //     Gizmos.color = Color.red;
                    //     rad = 0.6f;
                    // }
                    // else if (showState == ShowVertexState.InheritedVertex)
                    // {
                    //     show = currentVertex.isInherited;
                    //     Gizmos.color = Color.red;
                    //     rad = 0.6f;
                    // }
                    // else if (showState == ShowVertexState.EdgeVertex)
                    // {
                    //     show = currentVertex.isEdgeVertex;
                    //     Gizmos.color = Color.red;
                    //     rad = 0.5f;
                    // }
                    // else if (showState == ShowVertexState.InHexBounds)
                    // {
                    //     show = currentVertex.isInHexBounds;
                    //     Gizmos.color = Color.red;
                    //     rad = 0.5f;
                    // }
                    // else if (showState == ShowVertexState.CellNeighbor)
                    // {
                    //     show = currentVertex.hasCellVertexNeighbor;
                    //     Gizmos.color = Color.red;
                    //     rad = 0.6f;
                    // }
                    // else if (showState == ShowVertexState.Remove)
                    // {
                    //     show = currentVertex.markedForRemoval || currentVertex.isOnTunnelCell;//|| currentVertex.isTunnelStartCorner;

                    //     Gizmos.color = currentVertex.markedForRemoval ? Color.red : Color.green;
                    //     if (currentVertex.markedIgnore && !currentVertex.markedForRemoval)
                    //     {
                    //         Gizmos.color = Color.blue;
                    //     }
                    //     rad = 0.33f;
                    // }
                    // else if (showState == ShowVertexState.TunnelEntry)
                    // {
                    //     show = currentVertex.isOnTunnelGroundEntry;
                    //     Gizmos.color = Color.red;
                    //     rad = 0.33f;
                    // }
                    // else if (showState == ShowVertexState.OnTunnel)
                    // {
                    //     show = currentVertex.isOnTunnelCell;
                    //     Gizmos.color = Color.red;
                    //     rad = 0.33f;
                    // }

                    // Vector3 worldPosition = currentVertex.position;
                    // // Vector3 worldPosition = transform != null ? transform.TransformPoint(currentVertex.position) : currentVertex.position;
                    // if (show) Gizmos.DrawSphere(worldPosition, rad);
                }

            }

        }
        public static void DisplayTerrainVertex(TerrainVertex currentVertex, ShowVertexState showState, Transform transform)
        {
            bool show = false;
            float rad = 0.33f;
            Gizmos.color = Color.black;

            if (showState == ShowVertexState.All)
            {
                show = true;
            }
            else if (showState == ShowVertexState.Cell)
            {
                show = currentVertex.type == VertexType.Cell;
                Gizmos.color = Color.red;

                rad = 0.4f;
            }
            else if (showState == ShowVertexState.CellFlatGround)
            {
                show = currentVertex.isFlatGroundCell;
                Gizmos.color = Color.red;
                rad = 0.45f;
            }
            else if (showState == ShowVertexState.MicroCell)
            {
                show = currentVertex.isInMicroCell;
                Gizmos.color = currentVertex.type == VertexType.Road ? Color.blue : Color.red;
                rad = 0.4f;
            }
            else if (showState == ShowVertexState.Path)
            {
                show = currentVertex.type == VertexType.Road;
                Gizmos.color = Color.cyan;
                rad = 0.66f;
            }
            else if (showState == ShowVertexState.Terrain)
            {
                show = currentVertex.type == VertexType.Generic;
                Gizmos.color = Color.red;
                rad = 0.66f;
            }
            else if (showState == ShowVertexState.CellCenter)
            {
                show = currentVertex.isCellCenterPoint;
                Gizmos.color = Color.red;
                rad = 0.66f;
            }
            else if (showState == ShowVertexState.CellCorner)
            {
                show = currentVertex.isCellCornerPoint;
                Gizmos.color = Color.red;
                rad = 0.6f;
            }
            else if (showState == ShowVertexState.GridEdge)
            {
                show = currentVertex.isOnTheEdgeOftheGrid;
                Gizmos.color = Color.green;
                rad = 0.66f;
            }
            else if (showState == ShowVertexState.IgnoreSmooth)
            {
                show = currentVertex.ignoreSmooth;
                Gizmos.color = Color.red;
                rad = 0.6f;
            }
            else if (showState == ShowVertexState.InheritedVertex)
            {
                show = currentVertex.isInherited;
                Gizmos.color = Color.red;
                rad = 0.6f;
            }
            else if (showState == ShowVertexState.EdgeVertex)
            {
                show = currentVertex.isEdgeVertex;
                Gizmos.color = Color.red;
                rad = 0.5f;
            }
            else if (showState == ShowVertexState.InHexBounds)
            {
                show = currentVertex.isInHexBounds;
                Gizmos.color = Color.red;
                rad = 0.5f;
            }
            else if (showState == ShowVertexState.CellNeighbor)
            {
                show = currentVertex.hasCellVertexNeighbor;
                Gizmos.color = Color.red;
                rad = 0.6f;
            }
            else if (showState == ShowVertexState.Remove)
            {
                show = currentVertex.markedForRemoval;
                Gizmos.color = Color.red;
                // Gizmos.color = currentVertex.markedForRemoval ? Color.red : Color.green;
                // if (currentVertex.markedIgnore && !currentVertex.markedForRemoval)
                // {
                //     Gizmos.color = Color.blue;
                // }
                rad = 0.33f;
            }
            else if (showState == ShowVertexState.TunnelEntry)
            {
                show = currentVertex.isOnTunnelGroundEntry;
                Gizmos.color = Color.red;
                rad = 0.33f;
            }
            else if (showState == ShowVertexState.OnTunnel)
            {
                show = currentVertex.isOnTunnelCell;
                Gizmos.color = Color.red;
                rad = 0.33f;
            }

            Vector3 worldPosition = currentVertex.position;
            // Vector3 worldPosition = transform != null ? transform.TransformPoint(currentVertex.position) : currentVertex.position;
            if (show) Gizmos.DrawSphere(worldPosition, rad);

        }

        public static Dictionary<Vector2, TerrainVertex> Generate_GlobalVertexGrid_WithNoise(
             Bounds bounds,
             Transform transform,
             float steps,
             FastNoiseUnity fastNoiseUnity,

             float terrainHeight,
             float persistence,
             float octaves,
             float lacunarity,

             int edgeExpansionOffset = 3
         )
        {
            // Calculate the minimum x and z positions that are divisible by steps
            float minX = Mathf.Floor(bounds.min.x / steps) * steps;
            float minZ = Mathf.Floor(bounds.min.z / steps) * steps;

            // Calculate the number of steps along the x and z axis based on the spacing
            int xSteps = Mathf.CeilToInt((bounds.max.x - minX) / steps);
            int zSteps = Mathf.CeilToInt((bounds.max.z - minZ) / steps);

            Dictionary<Vector2, TerrainVertex> gridPoints = new Dictionary<Vector2, TerrainVertex>();

            // Loop through each vertex in the grid
            for (int z = 0; z <= zSteps; z++)
            {
                for (int x = 0; x <= xSteps; x++)
                {
                    // Calculate the x and z coordinates of the current grid point
                    float xPos = minX + x * steps;
                    float zPos = minZ + z * steps;

                    Vector3 position = new Vector3(xPos, 0, zPos);
                    Vector3 worldCoord = transform.TransformVector(position);

                    Vector3 rounded = VectorUtil.RoundVector3To1Decimal(position);
                    Vector2 aproximateCoord = new Vector2(rounded.x, rounded.z);

                    Vector2Int noiseCoordinate = new Vector2Int((int)worldCoord.x, (int)worldCoord.z);

                    float baseNoiseHeight = WorldManagerUtil.CalculateNoiseHeightForVertex(noiseCoordinate.x, noiseCoordinate.y, terrainHeight, fastNoiseUnity.fastNoise, persistence, octaves, lacunarity);
                    baseNoiseHeight += transform.position.y;
                    position.y = baseNoiseHeight;

                    // Create the TerrainVertex object
                    TerrainVertex vertex = new TerrainVertex()
                    {
                        noiseCoordinate = noiseCoordinate,
                        aproximateCoord = aproximateCoord,
                        position = position,
                        index_X = x,
                        index_Z = z,

                        type = VertexType.Generic,
                        markedForRemoval = false,
                        isInHexBounds = false,
                        // index = currentIndex,

                        worldspaceOwnerCoordinate = Vector2.positiveInfinity,
                        parallelWorldspaceOwnerCoordinate = Vector2.positiveInfinity,
                        parallelVertexIndex_X = -1,
                        parallelVertexIndex_Z = -1,
                    };

                    // Add the vertex to the grid
                    gridPoints[aproximateCoord] = vertex;

                }
            }

            return gridPoints;
        }


        public static Dictionary<Vector2, TerrainVertex> GenerateGlobalVertexGrid(Bounds bounds, float steps, Transform transform)
        {
            // // Calculate the number of steps along the x and z axis based on the spacing
            // int xSteps = Mathf.CeilToInt(bounds.size.x / steps);
            // int zSteps = Mathf.CeilToInt(bounds.size.z / steps);

            // Calculate the minimum x and z positions that are divisible by steps
            float minX = Mathf.Floor(bounds.min.x / steps) * steps;
            float minZ = Mathf.Floor(bounds.min.z / steps) * steps;

            // Calculate the number of steps along the x and z axis based on the spacing
            int xSteps = Mathf.CeilToInt((bounds.max.x - minX) / steps);
            int zSteps = Mathf.CeilToInt((bounds.max.z - minZ) / steps);

            // Initialize a dictionary to hold the grid points
            Dictionary<Vector2, TerrainVertex> gridPoints = new Dictionary<Vector2, TerrainVertex>();

            // Loop through all the steps along the x and z axis, and generate the corresponding grid points
            for (int z = 0; z <= zSteps; z++)
            {
                for (int x = 0; x <= xSteps; x++)
                {
                    // // Calculate the x and z coordinates of the current grid point
                    // float xPos = bounds.min.x + x * steps;
                    // float zPos = bounds.min.z + z * steps;

                    // Calculate the x and z coordinates of the current grid point
                    float xPos = minX + x * steps;
                    float zPos = minZ + z * steps;

                    Vector3 position = new Vector3(xPos, 0, zPos);
                    Vector3 worldCoord = transform.TransformVector(position);

                    Vector3 rounded = VectorUtil.RoundVector3To1Decimal(position);
                    Vector2 aproximateCoord = new Vector2(rounded.x, rounded.z);

                    Vector2Int noiseCoordinate = new Vector2Int((int)worldCoord.x, (int)worldCoord.z);


                    // Create the TerrainVertex object
                    TerrainVertex vertex = new TerrainVertex()
                    {
                        noiseCoordinate = noiseCoordinate,
                        aproximateCoord = aproximateCoord,
                        position = position,
                        index_X = x,
                        index_Z = z,

                        type = VertexType.Generic,
                        markedForRemoval = false,

                        worldspaceOwnerCoordinate = Vector2.positiveInfinity,
                        parallelWorldspaceOwnerCoordinate = Vector2.positiveInfinity,
                        parallelVertexIndex_X = -1,
                        parallelVertexIndex_Z = -1,

                        // isEdgeVertex = (z == 0 || x == 0 || z == zSteps || x == xSteps),
                    };

                    Debug.LogError("globalCoordinate: " + aproximateCoord);

                    // Set the current grid point in the dictionary
                    gridPoints[aproximateCoord] = vertex;
                }
            }

            return gridPoints;
        }

    }
}