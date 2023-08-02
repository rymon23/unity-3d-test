using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.SceneManagement;
using WFCSystem;
using Unity.Collections;
using Unity.Jobs;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.AI;

// Database Port: 5432
// Database Superuser: postgres

namespace ProceduralBase
{
    public static class WorldManagerUtil
    {
        public static float Calculate_TreeFitness(int x, int z, FastNoise fastNoise)
        {
            float fitness = (float)fastNoise.GetNoise(x, z);
            fitness += UnityEngine.Random.Range(-0.3f, 0.3f);

            return fitness;
        }

        public static float CalculateNoiseHeightForVertex(int indexX, int indexZ, float terrainHeight, List<FastNoiseUnity> noiseFunctions, float persistence, float octaves, float lacunarity)
        {
            float sum = 0f;
            foreach (FastNoiseUnity noise in noiseFunctions)
            {
                float noiseHeight = GetNoiseHeightValue(indexX, indexZ, noise.fastNoise, persistence, octaves, lacunarity);
                float basePosY = noiseHeight * terrainHeight;

                sum += basePosY;
            }
            float average = sum / noiseFunctions.Count;
            return average;
        }

        public static float CalculateNoiseHeightForVertex(int indexX, int indexZ, float terrainHeight, FastNoise fastNoise, float persistence, float octaves, float lacunarity)
        {
            float noiseHeight = GetNoiseHeightValue(indexX, indexZ, fastNoise, persistence, octaves, lacunarity);
            float basePosY = noiseHeight * terrainHeight;
            return basePosY;
        }

        private static float GetNoiseHeightValue(float x, float z, FastNoise fastNoise, float persistence, float octaves, float lacunarity)
        {
            // Calculate the height of the current point
            float noiseHeight = 0;
            float amplitude = 1;
            // float frequency = 1;

            for (int i = 0; i < octaves; i++)
            {
                float noiseValue = (float)fastNoise.GetNoise(x, z);

                noiseHeight += noiseValue * amplitude;
                amplitude *= persistence;
                // frequency *= lacunarity;
            }
            return noiseHeight;
        }

        public static List<Vector2> GetWorldspaceTerrainChunkLookups(List<HexagonCellPrototype> worldSpaceCells)
        {
            HashSet<Vector2> added = new HashSet<Vector2>();
            List<Vector2> terrainChunkLookups = new List<Vector2>();

            foreach (var cell in worldSpaceCells)
            {
                Vector2[] chunkLookups = cell.CalculateTerrainChunkLookups();
                foreach (Vector2 lookup in chunkLookups)
                {
                    if (added.Contains(lookup) == false)
                    {
                        added.Add(lookup);
                        terrainChunkLookups.Add(lookup);
                    }
                }
            }
            return terrainChunkLookups;
        }


        private static Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> Initialize_ParentWorldCells(
             Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> parentCells_ByParentLookup,
             int amount,
             int cellSize,
             int layerOffset,
             Transform transform,
             int radiusMult = 3
         )
        {
            if (parentCells_ByParentLookup == null)
            {
                Debug.LogError("No parentCells_ByParentLookup");
                return null;
            }

            Vector2 initialParentLookup = VectorUtil.Calculate_AproximateCoordinate(transform.position);
            if (parentCells_ByParentLookup.ContainsKey(initialParentLookup) == false || parentCells_ByParentLookup[initialParentLookup].ContainsKey(initialParentLookup) == false)
            {
                Debug.LogError("initialParentLookup: " + initialParentLookup + " not found in parentCells_ByParentLookup");
                return null;
            }

            HexagonCellPrototype initialParentCell = parentCells_ByParentLookup[initialParentLookup][initialParentLookup];

            if (initialParentCell.neighbors.Count == 0) HexGridPathingUtil.Rehydrate_CellNeighbors(initialParentCell.GetParentLookup(), initialParentCell.GetLookup(), parentCells_ByParentLookup, true);
            if (initialParentCell.neighbors.Count == 0)
            {
                Debug.LogError("No neighbors for initialParentLookup: " + initialParentLookup);
                return null;
            }

            // Get Parent Cells
            List<HexagonCellPrototype> parentCellsToFill = HexGridPathingUtil.GetConsecutiveInactiveWorldSpaceNeighbors(initialParentCell, parentCells_ByParentLookup, amount);
            Debug.LogError("parentCellsToFill: " + parentCellsToFill.Count);
            int totalCreated = 0;

            // Setup vars
            Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> new_cells_ByParentLookup = new Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>>();
            HashSet<string> neighborIDsToEvaluate = new HashSet<string>();
            List<HexagonCellPrototype> neighborsToEvaluate = new List<HexagonCellPrototype>();

            int size = (int)cellSize;

            foreach (HexagonCellPrototype parentCell in parentCellsToFill)
            {
                Vector3 parentCellCenterPos = new Vector3(parentCell.worldCoordinate.x, transform.position.y, parentCell.worldCoordinate.y);
                Vector2 parentCellLookup = parentCell.GetLookup();
                Vector2 parentCellParentLookup = parentCell.GetParentLookup();

                if (new_cells_ByParentLookup.ContainsKey(parentCellLookup) == false) new_cells_ByParentLookup.Add(parentCellLookup, new Dictionary<Vector2, HexagonCellPrototype>());

                Vector3[] regionCorners = HexCoreUtil.GenerateHexagonPoints(parentCellCenterPos, parentCell.size);

                int totalBufferRadius = HexCellUtil.CalculateExpandedHexRadius(size, 3);
                Dictionary<Vector2, Vector3> newCenterPoints = HexGridUtil.Generate_HexagonGridCenterPoints(
                    parentCellCenterPos,
                    size,
                    totalBufferRadius
                );

                int created = 0;
                foreach (Vector3 point in newCenterPoints.Values)
                {
                    Vector2 newCoordinate = VectorUtil.Calculate_AproximateCoordinate(point);
                    Vector2 newLookup = HexCoreUtil.Calculate_CenterLookup(newCoordinate, size);

                    if (parentCells_ByParentLookup.ContainsKey(parentCellParentLookup) && parentCells_ByParentLookup[parentCellParentLookup].ContainsKey(parentCellLookup))
                    {
                        bool foundExisting = parentCells_ByParentLookup[parentCellParentLookup][parentCellLookup].neighbors.Any(n =>
                            new_cells_ByParentLookup.ContainsKey(n.GetLookup()) &&
                            new_cells_ByParentLookup[n.GetLookup()].ContainsKey(newLookup));

                        if (foundExisting)
                        {
                            Debug.LogError("Existing lookup: " + newLookup + " found for parent: " + parentCellLookup + ", skipping duplicate");
                            continue;
                        }
                    }

                    if (VectorUtil.IsPointWithinPolygon(point, regionCorners) || VectorUtil.DistanceXZ(point, parentCellCenterPos) < parentCell.size * 0.95f)
                    {
                        if (new_cells_ByParentLookup[parentCellLookup].ContainsKey(newLookup) == false)
                        {
                            HexagonCellPrototype newCell = new HexagonCellPrototype(new Vector3(newCoordinate.x, transform.position.y, newCoordinate.y), cellSize, null, layerOffset);
                            newCell.SetWorldCoordinate(newCoordinate);
                            newCell.SetParentLookup(parentCellLookup);

                            new_cells_ByParentLookup[parentCellLookup].Add(newLookup, newCell);

                            // Add terrain chunk center lookups to dictionary; 
                            // Vector2[] terrainChunkLookups = newCell.CalculateTerrainChunkLookups();
                            // foreach (Vector2 chunkCenterLookup in terrainChunkLookups)
                            // {
                            //     if (_worldAreaTerrainChunkArea_ByLookup.ContainsKey(chunkCenterLookup) == false) _worldAreaTerrainChunkArea_ByLookup.Add(chunkCenterLookup, parentLookup);
                            // }
                            if (neighborIDsToEvaluate.Contains(newCell.Get_Uid()) == false)
                            {
                                neighborIDsToEvaluate.Add(newCell.Get_Uid());
                                neighborsToEvaluate.Add(newCell);
                            }

                            created++;
                        }
                    }
                }

                Debug.Log("Created " + created + " new child world cells within parent: " + parentCellLookup);
                totalCreated += created;
            }

            if (neighborsToEvaluate.Count > 1)
            {
                Debug.Log("Neighbors To evaluate: " + neighborsToEvaluate.Count);
                HexCellUtil.Evaluate_WorldCellNeighbors(neighborsToEvaluate, new_cells_ByParentLookup, parentCellsToFill[0].size, true);
            }

            Debug.Log("Created " + totalCreated + " new Child World cells across " + new_cells_ByParentLookup.Count + " Parent cells");

            return new_cells_ByParentLookup;
        }





        public static (Dictionary<Vector2, TerrainVertex>, Vector2[,]) Generate_GlobalVertexGrid_WithNoise_V6(
                Bounds bounds,
                Transform transform,
                float steps,
                List<FastNoiseUnity> noiseFunctions,
                Dictionary<Vector2, LocationPrefab> worldspaceTerraformLookups,
                float terrainHeight,
                float persistence,
                float octaves,
                float lacunarity,

                int edgeExpansionOffset = 3,
                int seaLevel = 0,
                int worldSpaceSize = 108
            )
        {
            // Calculate the minimum x and z positions that are divisible by steps
            float minX = Mathf.Floor(bounds.min.x / steps) * steps;
            float minZ = Mathf.Floor(bounds.min.z / steps) * steps;

            // Calculate the number of steps along the x and z axis based on the spacing
            int xSteps = Mathf.CeilToInt((bounds.max.x - minX) / steps);
            int zSteps = Mathf.CeilToInt((bounds.max.z - minZ) / steps);

            Dictionary<Vector2, TerrainVertex> gridPoints = new Dictionary<Vector2, TerrainVertex>();
            Vector2[,] grid = new Vector2[xSteps + 1, zSteps + 1];

            Vector3 currentTrackPos = Vector3.zero;
            List<Vector3> closestWorldspaceCellCenters = new List<Vector3>();

            Vector2 closestWorldspaceTerraformLookup = Vector2.positiveInfinity;
            Vector3 closestWorldspaceTerraformPos = Vector3.positiveInfinity;

            // Loop through each vertex in the grid
            for (int z = 0; z <= zSteps; z++)
            {
                for (int x = 0; x <= xSteps; x++)
                {
                    // Calculate the x and z coordinates of the current grid point
                    float xPos = minX + x * steps;
                    float zPos = minZ + z * steps;

                    Vector3 position = new Vector3(xPos, 0, zPos);

                    if (closestWorldspaceCellCenters.Count == 0 || VectorUtil.DistanceXZ(position, currentTrackPos) > worldSpaceSize * 0.7f)
                    {
                        closestWorldspaceCellCenters = HexCoreUtil.Calculate_ClosestHexCenterPoints_X7(position, worldSpaceSize);
                        currentTrackPos = position;

                        if (closestWorldspaceCellCenters.Count > 0 && worldspaceTerraformLookups.Count > 0)
                        {
                            Vector2 nearestTerraformLookup = Vector2.positiveInfinity;
                            Vector3 nearestTerraformPoint = Vector2.positiveInfinity;
                            float nearestDistance = float.MaxValue;

                            for (int i = 0; i < closestWorldspaceCellCenters.Count; i++)
                            {
                                float dist = VectorUtil.DistanceXZ(position, closestWorldspaceCellCenters[i]);
                                if (dist < nearestDistance)
                                {
                                    Vector2 cellLookup = HexCoreUtil.Calculate_CenterLookup(closestWorldspaceCellCenters[i], worldSpaceSize);
                                    if (worldspaceTerraformLookups.ContainsKey(cellLookup))
                                    {
                                        nearestDistance = dist;
                                        nearestTerraformLookup = cellLookup;
                                        nearestTerraformPoint = closestWorldspaceCellCenters[i];

                                        if (nearestDistance < worldSpaceSize * 0.5f) break;
                                    }
                                }
                            }
                            closestWorldspaceTerraformLookup = nearestTerraformLookup;
                            closestWorldspaceTerraformPos = nearestTerraformPoint;
                            // Vector2 wsLookup = HexCoreUtil.Calculate_CenterLookup(closestWorldspaceCellCenters[0], worldSpaceSize);
                            // if (worldspaceTerraformLookups.ContainsKey(wsLookup))
                            // {
                            //     closestWorldspaceLookup = wsLookup;
                            // }
                            // else closestWorldspaceLookup = Vector2.positiveInfinity;
                        }
                        else closestWorldspaceTerraformLookup = Vector2.positiveInfinity;
                    }

                    Vector3 worldCoord = transform.TransformVector(position);

                    Vector3 rounded = VectorUtil.RoundVector3To1Decimal(position);
                    Vector2 aproximateCoord = new Vector2(rounded.x, rounded.z);

                    Vector2Int noiseCoordinate = new Vector2Int((int)worldCoord.x, (int)worldCoord.z);

                    float baseNoiseHeight = WorldManagerUtil.CalculateNoiseHeightForVertex(noiseCoordinate.x, noiseCoordinate.y, terrainHeight, noiseFunctions, persistence, octaves, lacunarity);
                    baseNoiseHeight += transform.position.y;


                    if (closestWorldspaceTerraformLookup != Vector2.positiveInfinity)
                    {
                        // Vector2 wsLookup = HexCoreUtil.Calculate_CenterLookup(closestWorldspaceCellCenters[0], worldSpaceSize);
                        // if (worldspaceTerraformLookups.ContainsKey(wsLookup))
                        float distance = VectorUtil.DistanceXZ(position, closestWorldspaceTerraformPos);
                        if (distance < worldSpaceSize * 0.8f)
                        {
                            // float locationNoiseValue = GetNoiseHeightValue((int)cell.center.x, (int)cell.center.z, locationSubNoise.fastNoise, locationNoise_persistence, 2, locationNoise_lacunarity);
                            float centerNoise = WorldManagerUtil.CalculateNoiseHeightForVertex((int)closestWorldspaceTerraformPos.x, (int)closestWorldspaceTerraformPos.z, terrainHeight, noiseFunctions, persistence, octaves, lacunarity);
                            centerNoise += transform.position.y;

                            float distanceNormalized = Mathf.Clamp01(worldSpaceSize / distance);
                            float elevationMod = Mathf.Lerp(baseNoiseHeight, centerNoise, distanceNormalized);
                            baseNoiseHeight = elevationMod;
                        }
                    }

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
                    grid[x, z] = aproximateCoord;
                }
            }
            return (gridPoints, grid);
        }

        //
        public static TerrainVertex[,] Generate_WorldSpace_VertexGrid_WithNoise(
                HexagonCellPrototype hexCell,
                Transform transform,
                FastNoiseUnity fastNoiseUnity,

                float terrainHeight,
                float persistence,
                float octaves,
                float lacunarity,

                int edgeExpansionOffset = 3,
                int vertexDensity = 100,
                int widthMod = 0,
                int heightMod = 0
            )
        {
            Vector3[] corners = hexCell.cornerPoints;
            Vector3 centerPos = hexCell.center;
            Vector3 bottomLeft = new Vector3(corners[0].x + widthMod, centerPos.y, corners[5].z - heightMod);
            Vector3 bottomRight = new Vector3(corners[3].x - widthMod, centerPos.y, corners[4].z - heightMod);
            Vector3 topLeft = new Vector3(corners[0].x + widthMod, centerPos.y, corners[1].z + heightMod);
            Vector3 topRight = new Vector3(corners[3].x - widthMod, centerPos.y, corners[2].z + heightMod);

            List<Vector3> aproximateCorners = HexCoreUtil.GenerateHexagonPoints(centerPos, hexCell.size + 1).ToList();

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

                    Vector3 worldCoord = transform.TransformVector(pos);
                    Vector2Int coordinateXZ = new Vector2Int((int)worldCoord.x, (int)worldCoord.z);


                    Vector3 rounded = VectorUtil.RoundVector3To1Decimal(pos);
                    Vector2 aproximateCoord = new Vector2(rounded.x, rounded.z);

                    Vector2Int noiseCoordinate = new Vector2Int((int)worldCoord.x, (int)worldCoord.z);


                    float baseNoiseHeight = CalculateNoiseHeightForVertex(coordinateXZ.x, coordinateXZ.y, terrainHeight, fastNoiseUnity.fastNoise, persistence, octaves, lacunarity);
                    baseNoiseHeight += transform.position.y;
                    pos.y = baseNoiseHeight;


                    bool withinAproxBounds = VectorUtil.IsPointWithinPolygon(pos, aproximateCorners);
                    bool withinBounds = true;

                    // Create the TerrainVertex object
                    TerrainVertex vertex = new TerrainVertex()
                    {
                        worldspaceOwnerCoordinate = hexCell.GetLookup(),
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

        public static (Dictionary<Vector2, TerrainVertex>, Vector2[,]) Generate_GlobalVertexGrid_WithNoise_V5(
             Bounds bounds,
             Transform transform,
             float steps,
             List<FastNoiseUnity> noiseFunctions,
             // List<Vector2> smoothAtCoordinates,

             float terrainHeight,
             float persistence,
             float octaves,
             float lacunarity,

             int edgeExpansionOffset = 3,
             int seaLevel = 0
         )
        {
            // Calculate the minimum x and z positions that are divisible by steps
            float minX = Mathf.Floor(bounds.min.x / steps) * steps;
            float minZ = Mathf.Floor(bounds.min.z / steps) * steps;
            // Calculate the number of steps along the x and z axis based on the spacing
            int xSteps = Mathf.CeilToInt((bounds.max.x - minX) / steps);
            int zSteps = Mathf.CeilToInt((bounds.max.z - minZ) / steps);

            Dictionary<Vector2, TerrainVertex> gridPoints = new Dictionary<Vector2, TerrainVertex>();
            Vector2[,] grid = new Vector2[xSteps + 1, zSteps + 1];

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


                    float baseNoiseHeight = CalculateNoiseHeightForVertex(noiseCoordinate.x, noiseCoordinate.y, terrainHeight, noiseFunctions, persistence, octaves, lacunarity);
                    baseNoiseHeight += transform.position.y;
                    position.y = baseNoiseHeight;

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
                    grid[x, z] = aproximateCoord;
                }
            }
            return (gridPoints, grid);
        }

        public static (Dictionary<Vector2, TerrainVertex>, Vector2[,]) Generate_GlobalVertexGrid_WithNoise_V4(
            Bounds bounds,
            Transform transform,
            float steps,
            FastNoiseUnity fastNoiseUnity,
            List<Vector2> smoothAtCoordinates,

            float terrainHeight,
            float persistence,
            float octaves,
            float lacunarity,

            int edgeExpansionOffset = 3,
            int seaLevel = 0
        )
        {
            // Calculate the minimum x and z positions that are divisible by steps
            float minX = Mathf.Floor(bounds.min.x / steps) * steps;
            float minZ = Mathf.Floor(bounds.min.z / steps) * steps;

            // Calculate the number of steps along the x and z axis based on the spacing
            int xSteps = Mathf.CeilToInt((bounds.max.x - minX) / steps);
            int zSteps = Mathf.CeilToInt((bounds.max.z - minZ) / steps);

            Dictionary<Vector2, TerrainVertex> gridPoints = new Dictionary<Vector2, TerrainVertex>();
            Vector2[,] grid = new Vector2[xSteps + 1, zSteps + 1];


            Dictionary<Vector2, float> smoothNoiseHeightByCoordinates = new Dictionary<Vector2, float>();
            foreach (Vector2 coord in smoothAtCoordinates)
            {
                Debug.LogError("smoothAtCoordinates - coord: " + coord);

                Vector2Int noiseCoord = new Vector2Int((int)coord.x, (int)coord.y);
                float smoothNoiseHeight = CalculateNoiseHeightForVertex(noiseCoord.x, noiseCoord.y, terrainHeight, fastNoiseUnity.fastNoise, persistence, octaves, lacunarity);
                smoothNoiseHeight += transform.position.y;
                if (smoothNoiseHeight < seaLevel) smoothNoiseHeight = seaLevel + 1f;
                smoothNoiseHeightByCoordinates.Add(coord, smoothNoiseHeight);
            }

            int neighborDepth = 2;
            float primarySmoothRadius = 108 * 0.7f;

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


                    float baseNoiseHeight = CalculateNoiseHeightForVertex(noiseCoordinate.x, noiseCoordinate.y, terrainHeight, fastNoiseUnity.fastNoise, persistence, octaves, lacunarity);
                    baseNoiseHeight += transform.position.y;
                    // float baseNoiseHeight = transform.position.y;

                    (Vector3 smoothPoint, float dist) = VectorUtil.GetClosestPoint_XZ_WithDistance(smoothAtCoordinates, position);
                    if (smoothPoint != Vector3.positiveInfinity && (x > 0 && z > 0))
                    {
                        if (dist < primarySmoothRadius)
                        {
                            neighborDepth = 2;
                        }
                        else neighborDepth = 3;

                        float distanceNormalized = Mathf.Clamp01(108f / dist);
                        // float distanceNormalized = Mathf.Clamp01(dist / 108f);
                        float neighborElevation = 0f;
                        int neighborCount = 0;

                        for (int dx = -neighborDepth; dx <= neighborDepth; dx++)
                        {
                            for (int dz = -neighborDepth; dz <= neighborDepth; dz++)
                            {
                                if (dx == 0 && dz == 0) continue;

                                int nx = x + dx;
                                int nz = z + dz;

                                if (nx < 0 || nx >= x || nz < 0 || nz >= z) continue;

                                TerrainVertex neighborVertex = gridPoints[grid[nx, nz]];
                                neighborElevation += neighborVertex.position.y;
                                neighborCount++;
                            }
                        }

                        neighborElevation += smoothNoiseHeightByCoordinates[smoothPoint];

                        neighborCount++;

                        neighborElevation /= neighborCount;

                        float elevation = Mathf.Lerp(baseNoiseHeight, neighborElevation, distanceNormalized);
                        position.y = elevation;
                    }
                    else
                    {
                        position.y = baseNoiseHeight;
                    }
                    // position.y = baseNoiseHeight;

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
                    grid[x, z] = aproximateCoord;
                }
            }
            return (gridPoints, grid);
        }

        public static (Dictionary<Vector2, TerrainVertex>, Vector2[,]) Generate_GlobalVertexGrid_WithNoise_V3(
            Bounds bounds,
            Transform transform,
            float steps,
            FastNoiseUnity fastNoiseUnity,
            List<Vector2> smoothAtCoordinates,

            float terrainHeight,
            float persistence,
            float octaves,
            float lacunarity,

            int edgeExpansionOffset = 3,
            int seaLevel = 0
        )
        {
            // Calculate the minimum x and z positions that are divisible by steps
            float minX = Mathf.Floor(bounds.min.x / steps) * steps;
            float minZ = Mathf.Floor(bounds.min.z / steps) * steps;

            // Calculate the number of steps along the x and z axis based on the spacing
            int xSteps = Mathf.CeilToInt((bounds.max.x - minX) / steps);
            int zSteps = Mathf.CeilToInt((bounds.max.z - minZ) / steps);
            int maxSteps = xSteps >= zSteps ? xSteps : zSteps;

            Dictionary<Vector2, TerrainVertex> gridPoints = new Dictionary<Vector2, TerrainVertex>();
            Vector2[,] grid = new Vector2[maxSteps + 1, maxSteps + 1];


            Dictionary<Vector2, float> smoothNoiseHeightByCoordinates = new Dictionary<Vector2, float>();
            foreach (Vector2 coord in smoothAtCoordinates)
            {
                Debug.LogError("smoothAtCoordinates - coord: " + coord);

                Vector2Int noiseCoord = new Vector2Int((int)coord.x, (int)coord.y);
                float smoothNoiseHeight = CalculateNoiseHeightForVertex(noiseCoord.x, noiseCoord.y, terrainHeight, fastNoiseUnity.fastNoise, persistence, octaves, lacunarity);
                smoothNoiseHeight += transform.position.y;
                if (smoothNoiseHeight < seaLevel) smoothNoiseHeight = seaLevel + 1f;
                smoothNoiseHeightByCoordinates.Add(coord, smoothNoiseHeight);
            }

            int neighborDepth = 2;
            float primarySmoothRadius = 108 * 0.7f;

            // Loop through each vertex in the grid
            for (int z = 0; z <= maxSteps; z++)
            {
                for (int x = 0; x <= maxSteps; x++)
                {
                    // Calculate the x and z coordinates of the current grid point
                    float xPos = minX + x * steps;
                    float zPos = minZ + z * steps;

                    Vector3 position = new Vector3(xPos, 0, zPos);
                    Vector3 worldCoord = transform.TransformVector(position);

                    Vector3 rounded = VectorUtil.RoundVector3To1Decimal(position);
                    Vector2 aproximateCoord = new Vector2(rounded.x, rounded.z);

                    Vector2Int noiseCoordinate = new Vector2Int((int)worldCoord.x, (int)worldCoord.z);

                    (Vector3 smoothPoint, float dist) = VectorUtil.GetClosestPoint_XZ_WithDistance(smoothAtCoordinates, position);

                    float baseNoiseHeight = CalculateNoiseHeightForVertex(noiseCoordinate.x, noiseCoordinate.y, terrainHeight, fastNoiseUnity.fastNoise, persistence, octaves, lacunarity);
                    baseNoiseHeight += transform.position.y;

                    if (smoothPoint != Vector3.positiveInfinity && (x > 0 && z > 0))
                    {
                        if (dist < primarySmoothRadius)
                        {
                            neighborDepth = 2;
                        }
                        else
                        {
                            neighborDepth = 3;
                        }

                        float distanceNormalized = Mathf.Clamp01(108f / dist);
                        // float distanceNormalized = Mathf.Clamp01(dist / 108f);
                        float neighborElevation = 0f;
                        int neighborCount = 0;

                        for (int dx = -neighborDepth; dx <= neighborDepth; dx++)
                        {
                            for (int dz = -neighborDepth; dz <= neighborDepth; dz++)
                            {
                                if (dx == 0 && dz == 0) continue;

                                int nx = x + dx;
                                int nz = z + dz;

                                if (nx < 0 || nx >= x || nz < 0 || nz >= z) continue;

                                TerrainVertex neighborVertex = gridPoints[grid[nx, nz]];
                                neighborElevation += neighborVertex.position.y;
                                neighborCount++;
                            }
                        }

                        // if (dist < primarySmoothRadius)
                        // {
                        neighborElevation += smoothNoiseHeightByCoordinates[smoothPoint];
                        // }
                        // else
                        // {
                        //     neighborElevation += Mathf.Lerp(baseNoiseHeight, smoothNoiseHeightByCoordinates[smoothPoint], distanceNormalized);
                        // }
                        neighborCount++;

                        neighborElevation /= neighborCount;

                        float elevation = Mathf.Lerp(baseNoiseHeight, neighborElevation, distanceNormalized);
                        position.y = elevation;
                    }
                    else
                    {
                        position.y = baseNoiseHeight;
                    }

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
                    grid[x, z] = aproximateCoord;
                }
            }
            return (gridPoints, grid);
        }



        public static (Dictionary<Vector2, TerrainVertex>, Vector2[,]) Generate_GlobalVertexGrid_WithNoise_V2(
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
            int maxSteps = xSteps >= zSteps ? xSteps : zSteps;

            Dictionary<Vector2, TerrainVertex> gridPoints = new Dictionary<Vector2, TerrainVertex>();
            Vector2[,] grid = new Vector2[maxSteps + 1, maxSteps + 1];

            // Loop through each vertex in the grid
            for (int z = 0; z <= maxSteps; z++)
            {
                for (int x = 0; x <= maxSteps; x++)
                {
                    // Calculate the x and z coordinates of the current grid point
                    float xPos = minX + x * steps;
                    float zPos = minZ + z * steps;

                    Vector3 position = new Vector3(xPos, 0, zPos);
                    Vector3 worldCoord = transform.TransformVector(position);

                    Vector3 rounded = VectorUtil.RoundVector3To1Decimal(position);
                    Vector2 aproximateCoord = new Vector2(rounded.x, rounded.z);

                    Vector2Int noiseCoordinate = new Vector2Int((int)worldCoord.x, (int)worldCoord.z);

                    float baseNoiseHeight = CalculateNoiseHeightForVertex(noiseCoordinate.x, noiseCoordinate.y, terrainHeight, fastNoiseUnity.fastNoise, persistence, octaves, lacunarity);
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
                    grid[x, z] = aproximateCoord;
                }
            }

            // Debug.LogError("gridPoints - x: " + gridPoints.Count + ", grid - X: " + grid.GetLength(0) + ", Z: " + grid.GetLength(1));
            return (gridPoints, grid);
        }


        public static List<Vector2[,]> Generate_VertexGridChunksFromCenterPoints(Bounds bounds, Dictionary<Vector2, TerrainVertex> globalVertexGrid, float steps, List<Vector3> chunkCenterPositions)
        {
            float minX = Mathf.Floor(bounds.min.x / steps) * steps;
            float minZ = Mathf.Floor(bounds.min.z / steps) * steps;

            // Calculate the number of steps along the x and z axis based on the spacing
            int xSteps = Mathf.CeilToInt((bounds.max.x - minX) / steps);
            int zSteps = Mathf.CeilToInt((bounds.max.z - minZ) / steps);

            // Initialize a list to hold the subgrid chunks
            List<Vector2[,]> subgridChunks = new List<Vector2[,]>();

            // Loop through each chunk center position and generate the corresponding subgrid chunk
            foreach (Vector3 centerPosition in chunkCenterPositions)
            {
                int chunkSizeX = Mathf.CeilToInt(centerPosition.x / steps) + 1;
                int chunkSizeZ = Mathf.CeilToInt(centerPosition.z / steps) + 1;

                Vector2[,] subgridChunk = new Vector2[chunkSizeX, chunkSizeZ];

                for (int z = 0; z < chunkSizeZ; z++)
                {
                    for (int x = 0; x < chunkSizeX; x++)
                    {
                        // Calculate the x and z coordinates of the current grid point
                        float xPos = minX + x * steps;
                        float zPos = minZ + z * steps;

                        Vector3 position = new Vector3(xPos, 0, zPos);

                        Vector3 rounded = VectorUtil.RoundVector3To1Decimal(position);
                        Vector2 approximateCoord = new Vector2(rounded.x, rounded.z);
                        Vector2 foundCoord = approximateCoord;

                        if (globalVertexGrid.ContainsKey(foundCoord))
                        {
                            subgridChunk[x, z] = foundCoord;
                        }
                    }
                }

                subgridChunks.Add(subgridChunk);
            }

            return subgridChunks;
        }


        public static List<(Vector2[,], Vector2)> GetVertexGridChunkKeys(
            Dictionary<Vector2, TerrainVertex> globalVertexGrid,
            List<Vector3> centerPoints,
            float steps,
            Vector2 terrainChunkSizeXZ
        )
        {
            List<(Vector2[,], Vector2)> result = new List<(Vector2[,], Vector2)>();
            foreach (Vector3 point in centerPoints)
            {
                Bounds bounds = VectorUtil.GenerateBoundsFromCenter(terrainChunkSizeXZ, point);
                Vector2[,] gridChunk = GetLocalVertexGridKeys_V3(
                    globalVertexGrid,
                    bounds,
                    steps
                );
                result.Add((gridChunk, new Vector2(point.x, point.z)));
            }
            return result;
        }

        public static Vector2[,] GetLocalVertexGridKeys_V3(
            Dictionary<Vector2, TerrainVertex> globalVertexGrid,
            Bounds bounds,
            float steps
        )
        {
            float minX = Mathf.Floor(bounds.min.x / steps) * steps;
            float minZ = Mathf.Floor(bounds.min.z / steps) * steps;

            // Calculate the number of steps along the x and z axis based on the spacing
            int xSteps = Mathf.CeilToInt((bounds.max.x - minX) / steps);
            int zSteps = Mathf.CeilToInt((bounds.max.z - minZ) / steps);

            // Initialize a dictionary to hold the grid points
            Vector2[,] grid = new Vector2[xSteps + 1, zSteps + 1];

            // Loop through all the steps along the x and z axis, and generate the corresponding grid points
            for (int z = 0; z <= zSteps; z++)
            {
                for (int x = 0; x <= xSteps; x++)
                {
                    // Calculate the x and z coordinates of the current grid point
                    float xPos = minX + x * steps;
                    float zPos = minZ + z * steps;
                    Vector3 position = new Vector3(xPos, 0, zPos);

                    Vector3 rounded = VectorUtil.RoundVector3To1Decimal(position);
                    Vector2 aproximateCoord = new Vector2(rounded.x, rounded.z);
                    Vector2 foundCoord = aproximateCoord;

                    if (globalVertexGrid.ContainsKey(foundCoord)) grid[x, z] = foundCoord;
                }
            }
            return grid;
        }



        public static Vector2[,] GetLocalVertexGridKeys_V2(
            Bounds bounds,
            Dictionary<Vector2, TerrainVertex> globalVertexGrid,
            HexagonCellPrototype worldspaceCell,
            Transform transform,
            float steps
        )
        {
            List<Vector3> aproximateCorners = HexCoreUtil.GenerateHexagonPoints(worldspaceCell.center, worldspaceCell.size).ToList();

            float minX = Mathf.Floor(bounds.min.x / steps) * steps;
            float minZ = Mathf.Floor(bounds.min.z / steps) * steps;

            // Calculate the number of steps along the x and z axis based on the spacing
            int xSteps = Mathf.CeilToInt((bounds.max.x - minX) / steps);
            int zSteps = Mathf.CeilToInt((bounds.max.z - minZ) / steps);

            // Initialize a dictionary to hold the grid points
            Vector2[,] grid = new Vector2[xSteps + 1, zSteps + 1];

            int found = 0;
            int missed = 0;
            int added = 0;

            // Loop through all the steps along the x and z axis, and generate the corresponding grid points
            for (int z = 0; z <= zSteps; z++)
            {
                for (int x = 0; x <= xSteps; x++)
                {
                    // Calculate the x and z coordinates of the current grid point
                    float xPos = minX + x * steps;
                    float zPos = minZ + z * steps;

                    Vector3 position = new Vector3(xPos, 0, zPos);

                    Vector3 worldCoord = transform.TransformVector(position); // transform.TransformPoint(position);

                    Vector3 rounded = VectorUtil.RoundVector3To1Decimal(position);
                    Vector2 aproximateCoord = new Vector2(rounded.x, rounded.z);

                    Vector2 foundCoord = aproximateCoord;
                    bool wasfound = false;

                    // Vector2 foundCoord = globalVertexGrid.Keys.ToList().Find(p => (p - new Vector2(xPos, zPos)).magnitude <= 0.04f);
                    // Debug.LogError(worldspaceCell.GetWorldCordinate() + " - foundCoord: " + foundCoord);

                    if (foundCoord != Vector2.positiveInfinity && foundCoord != Vector2.negativeInfinity) added++;

                    if (globalVertexGrid.ContainsKey(foundCoord))
                    {
                        TerrainVertex vertex = globalVertexGrid[foundCoord];
                        bool withinAproxBounds = VectorUtil.IsPointWithinPolygon(vertex.position, aproximateCorners);

                        grid[x, z] = foundCoord;

                        if (withinAproxBounds || vertex.worldspaceOwnerCoordinate == Vector2.positiveInfinity)
                        {
                            vertex.worldspaceOwnerCoordinate = worldspaceCell.GetLookup();
                            // vertex.isInHexBounds = withinAproxBounds;

                            globalVertexGrid[foundCoord] = vertex;
                        }

                        // grid[x, z].isEdgeVertex = (z == 0 || x == 0 || z == zSteps || x == xSteps);
                        // grid[x, z].worldspaceOwnerCoordinate = worldspaceCell.GetWorldCordinate();

                        // if (withinAproxBounds)
                        // {
                        //     grid[x, z].worldspaceOwnerCoordinate = worldspaceCell.GetWorldCordinate();
                        // }
                        // else
                        // {
                        //     Debug.LogError(worldspaceCell.GetWorldCordinate() + " - NOT Within Aprox Bounds: " + foundCoord + ",  pos: " + grid[x, z].position);
                        // }

                        found++;
                        wasfound = true;
                    }

                    if (wasfound)
                    {
                        continue;
                    }
                    else
                    {
                        missed++;
                    }
                }
            }
            if (missed > 0)
            {
                Debug.LogError("OwnerCoordinate: " + worldspaceCell.GetLookup() + " found: " + found + ", missed :" + missed + ", added: " + added);
            }
            return grid;
        }

        public static Vector2[,] GetLocalVertexGridKeys(
            Bounds bounds,
            Dictionary<Vector2, TerrainVertex> globalVertexGrid,
            HexagonCellPrototype worldspaceCell,
            Transform transform,
            float steps
        )
        {
            List<Vector3> aproximateCorners = HexCoreUtil.GenerateHexagonPoints(worldspaceCell.center, worldspaceCell.size).ToList();

            float minX = Mathf.Floor(bounds.min.x / steps) * steps;
            float minZ = Mathf.Floor(bounds.min.z / steps) * steps;

            // Calculate the number of steps along the x and z axis based on the spacing
            int xSteps = Mathf.CeilToInt((bounds.max.x - minX) / steps);
            int zSteps = Mathf.CeilToInt((bounds.max.z - minZ) / steps);

            // Initialize a dictionary to hold the grid points
            Vector2[,] grid = new Vector2[xSteps + 1, zSteps + 1];
            int found = 0;
            int missed = 0;
            int added = 0;

            // Loop through all the steps along the x and z axis, and generate the corresponding grid points
            for (int z = 0; z <= zSteps; z++)
            {
                for (int x = 0; x <= xSteps; x++)
                {
                    // Calculate the x and z coordinates of the current grid point
                    float xPos = minX + x * steps;
                    float zPos = minZ + z * steps;

                    Vector3 position = new Vector3(xPos, 0, zPos);

                    Vector3 worldCoord = transform.TransformVector(position); // transform.TransformPoint(position);

                    Vector3 rounded = VectorUtil.RoundVector3To1Decimal(position);
                    Vector2 aproximateCoord = new Vector2(rounded.x, rounded.z);

                    bool wasfound = false;

                    Vector2 foundCoord = globalVertexGrid.Keys.ToList().Find(p => (p - new Vector2(xPos, zPos)).magnitude <= 0.04f);
                    // Debug.LogError(worldspaceCell.GetWorldCordinate() + " - foundCoord: " + foundCoord);

                    if (foundCoord != Vector2.zero && foundCoord != Vector2.positiveInfinity && foundCoord != Vector2.negativeInfinity) added++;

                    if (globalVertexGrid.ContainsKey(foundCoord))
                    {
                        TerrainVertex vertex = globalVertexGrid[foundCoord];
                        bool withinAproxBounds = VectorUtil.IsPointWithinPolygon(vertex.position, aproximateCorners);

                        grid[x, z] = foundCoord;

                        if (withinAproxBounds || vertex.worldspaceOwnerCoordinate == Vector2.positiveInfinity)
                        {
                            vertex.worldspaceOwnerCoordinate = worldspaceCell.GetLookup();
                            // vertex.isInHexBounds = withinAproxBounds;

                            globalVertexGrid[foundCoord] = vertex;
                        }
                        found++;
                        wasfound = true;
                    }

                    if (wasfound)
                    {
                        continue;
                    }
                    else
                    {
                        missed++;
                    }
                }
            }
            if (missed > 0)
            {
                Debug.LogError("OwnerCoordinate: " + worldspaceCell.GetLookup() + " found: " + found + ", missed :" + missed + ", added: " + added);
            }
            return grid;
        }

        public static TerrainVertex[,] GenerateLocalVertexGrid(
                  Bounds bounds,
                  Dictionary<Vector2, TerrainVertex> globalVertexGrid,
                  HexagonCellPrototype worldspaceCell,
                  Transform transform,
                  float steps
              )
        {
            List<Vector3> aproximateCorners = HexCoreUtil.GenerateHexagonPoints(worldspaceCell.center, worldspaceCell.size).ToList();

            float minX = Mathf.Floor(bounds.min.x / steps) * steps;
            float minZ = Mathf.Floor(bounds.min.z / steps) * steps;

            // Calculate the number of steps along the x and z axis based on the spacing
            int xSteps = Mathf.CeilToInt((bounds.max.x - minX) / steps);
            int zSteps = Mathf.CeilToInt((bounds.max.z - minZ) / steps);

            // Initialize a dictionary to hold the grid points
            TerrainVertex[,] grid = new TerrainVertex[xSteps + 1, xSteps + 1];

            int found = 0;
            int missed = 0;
            int added = 0;

            // Loop through all the steps along the x and z axis, and generate the corresponding grid points
            for (int z = 0; z <= zSteps; z++)
            {
                for (int x = 0; x <= xSteps; x++)
                {
                    // Calculate the x and z coordinates of the current grid point
                    float xPos = minX + x * steps;
                    float zPos = minZ + z * steps;

                    Vector3 position = new Vector3(xPos, 0, zPos);

                    Vector3 worldCoord = transform.TransformVector(position); // transform.TransformPoint(position);

                    Vector3 rounded = VectorUtil.RoundVector3To1Decimal(position);
                    Vector2 aproximateCoord = new Vector2(rounded.x, rounded.z);

                    bool wasfound = false;

                    Vector2 foundCoord = globalVertexGrid.Keys.ToList().Find(p => (p - new Vector2(xPos, zPos)).magnitude <= 0.04f);
                    // Debug.LogError(worldspaceCell.GetWorldCordinate() + " - foundCoord: " + foundCoord);

                    if (foundCoord != Vector2.zero && foundCoord != Vector2.positiveInfinity && foundCoord != Vector2.negativeInfinity) added++;

                    if (globalVertexGrid.ContainsKey(foundCoord))
                    {
                        grid[x, z] = globalVertexGrid[foundCoord];
                        grid[x, z].isEdgeVertex = (z == 0 || x == 0 || z == zSteps || x == xSteps);

                        bool withinAproxBounds = VectorUtil.IsPointWithinPolygon(grid[x, z].position, aproximateCorners);

                        // grid[x, z].isInHexBounds = withinAproxBounds;
                        grid[x, z].worldspaceOwnerCoordinate = worldspaceCell.GetLookup();

                        // if (withinAproxBounds)
                        // {
                        //     grid[x, z].worldspaceOwnerCoordinate = worldspaceCell.GetWorldCordinate();
                        // }
                        // else
                        // {
                        //     Debug.LogError(worldspaceCell.GetWorldCordinate() + " - NOT Within Aprox Bounds: " + foundCoord + ",  pos: " + grid[x, z].position);
                        // }

                        found++;
                        wasfound = true;
                    }

                    if (wasfound)
                    {
                        continue;
                    }
                    else
                    {
                        missed++;
                    }

                }
            }

            Debug.LogError("OwnerCoordinate: " + worldspaceCell.GetLookup() + " found: " + found + ", missed :" + missed + ", added: " + added);
            return grid;
        }


        public static void UpdateTerrainNoiseVertexElevations(
    FastNoiseUnity fastNoiseUnity,
    TerrainVertex[,] vertexGrid,
    Transform transform,
    float terrainHeight,
    float persistence,
    float octaves,
    float lacunarity
)
        {
            if (vertexGrid == null)
            {
                Debug.LogError("Null vertexGrid");
                return;
            }

            for (int x = 0; x < vertexGrid.GetLength(0); x++)
            {
                for (int z = 0; z < vertexGrid.GetLength(1); z++)
                {
                    TerrainVertex currentVertex = vertexGrid[x, z];

                    int posX = (int)currentVertex.noiseCoordinate.x;
                    int posZ = (int)currentVertex.noiseCoordinate.y;

                    float baseNoiseHeight = WorldManagerUtil.CalculateNoiseHeightForVertex(posX, posZ, terrainHeight, fastNoiseUnity.fastNoise, persistence, octaves, lacunarity);
                    baseNoiseHeight += transform.position.y;

                    // if (vertexGrid[x, z].markedForRemoval && !currentVertex.isInherited ) continue;

                    if (
                        vertexGrid[x, z].isOnTunnelCell &&
                        vertexGrid[x, z].isOnTunnelGroundEntry == false &&
                        vertexGrid[x, z].isOnTheEdgeOftheGrid == false &&
                        vertexGrid[x, z].type != VertexType.Cell &&
                        vertexGrid[x, z].type != VertexType.Road
                    )
                    {
                        vertexGrid[x, z].position.y = baseNoiseHeight;

                        if (vertexGrid[x, z].baseNoiseHeight < vertexGrid[x, z].tunnelCellRoofPosY)
                        {
                            float modY = vertexGrid[x, z].tunnelCellRoofPosY; // Mathf.Lerp(baseNoiseHeight, vertexGrid[x, z].tunnelCellRoofPosY, 0.9f);
                            vertexGrid[x, z].position.y = modY;
                            vertexGrid[x, z].baseNoiseHeight = vertexGrid[x, z].position.y;
                        }
                    }
                    else
                    {
                        if (!TerrainVertexUtil.ShouldSmoothIgnore(currentVertex) && !currentVertex.isOnTheEdgeOftheGrid && (currentVertex.type == VertexType.Generic || vertexGrid[x, z].type == VertexType.Unset))
                        {
                            vertexGrid[x, z].position.y = baseNoiseHeight;
                            vertexGrid[x, z].baseNoiseHeight = vertexGrid[x, z].position.y;
                        }
                    }
                }
            }
        }

        public static List<Vector3> UpdateCellPointNoiseElevations(
            FastNoiseUnity fastNoiseUnity,
            List<Vector3> vertexPoints,
            int cellLayerElevation,
            Transform transform,
            float terrainHeight,
            float persistence,
            float octaves,
            float lacunarity
        )
        {
            List<Vector3> result = new List<Vector3>();

            for (int i = 0; i < vertexPoints.Count; i++)
            {
                Vector3 point = vertexPoints[i];

                int posX = (int)point.x;
                int posZ = (int)point.z;

                float baseNoiseHeight = WorldManagerUtil.CalculateNoiseHeightForVertex(posX, posZ, terrainHeight, fastNoiseUnity.fastNoise, persistence, octaves, lacunarity);
                baseNoiseHeight += transform.position.y;

                float mod = baseNoiseHeight % (float)cellLayerElevation;
                float heighDiff = (baseNoiseHeight / (float)cellLayerElevation) + mod;


                result.Add(new Vector3(point.x, baseNoiseHeight, point.z));
            }

            return result;
        }

        public static void Generate_TerrainMeshOnObject(
                  GameObject meshObject,
                  FastNoiseUnity fastNoiseUnity,
                  TerrainVertex[,] vertexGrid,
                  Transform transform,
                  float terrainHeight,
                  float persistence,
                  float octaves,
                  float lacunarity,
                  bool initialSmooth = false
              )
        {
            if (meshObject == null || fastNoiseUnity == null)
            {
                Debug.LogError("Null meshObject or fastNoiseUnity");
                return;
            }

            // Get the MeshFilter component from the instantiatedPrefab
            MeshFilter meshFilter = meshObject.GetComponent<MeshFilter>();
            MeshCollider meshCollider = meshObject.GetComponent<MeshCollider>();

            Mesh mesh = meshFilter.sharedMesh;
            mesh.name = "World Spaace Mesh";

            UpdateTerrainNoiseVertexElevations(
                fastNoiseUnity,
                vertexGrid,
                transform,
                terrainHeight,
                persistence,
                octaves,
                lacunarity
            );

            bool useIgnoreRules = true;
            int neighborDepth = 6;
            int cellGridWeight = 1;
            int inheritedWeight = 1;
            float ratio = 0.9f;
            // if (initialSmooth)
            // {
            //     Debug.Log("initialSmooth!");

            //     // TerrainVertexUtil.SmoothWorldAreaVertexElevationTowardsCenter(
            //     //     vertexGrid,
            //     //     meshObject.transform.position,
            //     //     useIgnoreRules,
            //     //     neighborDepth,
            //     //     cellGridWeight,
            //     //     inheritedWeight,
            //     //     ratio
            //     // );

            //     // TerrainVertexUtil.SmoothWorldAreaVertexElevationTowardsCenter(vertexGrid, meshObject.transform.position, false);
            // }

            // int cylces = 2;
            // do
            // {
            //     HexGridVertexUtil.SmoothGridEdgeVertexList(
            //         vertexGrid,
            //         neighborDepth,
            //         ratio,
            //         cellGridWeight,
            //         3
            //     );
            //     // TerrainVertexUtil.SmoothElevationAroundCellGrid(vertexGrid, 4, 1, 2);

            //     cylces--;
            // } while (cylces > 0);

            // HexGridVertexUtil.SmoothGridEdgeVertexIndices(
            //     cellManager.GetAllPrototypesOfCellStatus(CellStatus.Ground).FindAll(p => p.isEdge && p.IsRemoved() == false),
            //     vertexGrid, cellVertexSearchRadiusMult, gridEdgeSmoothingRadius, smoothingFactor, smoothingSigma);


            (Vector3[] vertexPositions, HashSet<(int, int)> meshTraingleExcludeList) = TerrainVertexUtil.ExtractFinalVertexWorldPositions(vertexGrid, transform);
            // Vector3[] vertexPositions = positions.ToArray();

            if (vertexGrid == null || vertexPositions.Length == 0)
            {
                Debug.LogError("Null vertexGrid or no vertexPositions");
                return;
            }

            mesh.Clear();
            mesh.vertices = vertexPositions;
            mesh.triangles = ProceduralTerrainUtility.GenerateTerrainTriangles(vertexGrid, meshTraingleExcludeList);
            mesh.uv = ProceduralTerrainUtility.GenerateTerrainUVs(vertexGrid);

            // Refresh Terrain Mesh
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // Apply the mesh data to the MeshFilter component
            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;

            // Debug.Log("Generate_TerrainMeshOnObject - Complete");
        }


        public static void RefreshTerrainMeshOnObject(GameObject meshObject, TerrainVertex[,] vertexGrid, Transform transform)
        {
            (Vector3[] vertexPositions, Vector2[] uvs, HashSet<(int, int)> meshTraingleExcludeList) = TerrainVertexUtil.ExtractVertexWorldPositionsAndUVs(vertexGrid, transform);
            // (Vector3[] vertexPositions, HashSet<(int, int)> meshTraingleExcludeList) = TerrainVertexUtil.ExtractFinalVertexWorldPositions(vertexGrid, transform);

            if (vertexGrid == null || vertexPositions.Length == 0)
            {
                Debug.LogError("Null vertexGrid or no vertexPositions");
                return;
            }

            // Get the MeshFilter component from the instantiatedPrefab
            MeshFilter meshFilter = meshObject.GetComponent<MeshFilter>();
            MeshCollider meshCollider = meshObject.GetComponent<MeshCollider>();
            Mesh mesh = meshFilter.sharedMesh;

            mesh.name = "World Spaace Mesh";
            mesh.Clear();
            mesh.vertices = vertexPositions;
            mesh.triangles = ProceduralTerrainUtility.GenerateTerrainTriangles(vertexGrid, meshTraingleExcludeList);
            mesh.uv = uvs;
            // mesh.uv = ProceduralTerrainUtility.GenerateTerrainUVs(vertexGrid);

            // Refresh Terrain Mesh
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // Apply the mesh data to the MeshFilter component
            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;
        }


    }
}