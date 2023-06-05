using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ProceduralBase;

namespace WFCSystem
{
    public static class HexGridVertexUtil
    {
        public static void AssignTerrainVerticesToPrototypeGrid_V2(
            Dictionary<int, List<HexagonCellPrototype>> gridCellPrototypesByLayer,
            Dictionary<Vector2, TerrainVertex> globalTerrainVertexGridByCoordinate,
            Vector2[,] worldspaceVertexKeys,
            Transform transform,
            int cellLayerElevation,
            float viableFlatGroundCellSteepnessThreshhold,
            bool useEdgeBoundsCheck,
            bool checkCorners,
            float cellRadiusMult = 1.2f,
            float seaLevel = 0,
            bool checkCenterDistanceXYZ = false
        )
        {
            int topLayer = gridCellPrototypesByLayer.Keys.Count - 1;
            List<HexagonCellPrototype> topLayerPrototypes = gridCellPrototypesByLayer[topLayer];

            if (worldspaceVertexKeys == null || worldspaceVertexKeys.GetLength(0) == 0)
            {
                Debug.LogError("NO worldspaceVertexKeys");
            }

            for (int x = 0; x < worldspaceVertexKeys.GetLength(0); x++)
            {
                for (int z = 0; z < worldspaceVertexKeys.GetLength(1); z++)
                {
                    TerrainVertex vertex = globalTerrainVertexGridByCoordinate[worldspaceVertexKeys[x, z]];
                    // if (vertex.isInHexBounds == false) continue;

                    HexagonCellPrototype closestCell = null;
                    foreach (var prototype in topLayerPrototypes)
                    {
                        List<Vector3> aproximateCorners = HexCoreUtil.GenerateHexagonPoints(prototype.center, prototype.size).ToList();
                        bool withinAproxBounds = VectorUtil.IsPointWithinPolygon(vertex.position, aproximateCorners);
                        if (withinAproxBounds)
                        {
                            closestCell = prototype;
                            // closestDistance = VectorUtil.DistanceXZ(vertex.position, prototype.center);
                            break;
                            // if (closestDistance < prototype.size) 
                        }
                    }

                    // (HexagonCellPrototype closestCell, float closestDistance) = GetHexagonCellProtoypeBoundsParentOfVertex(topLayerPrototypes, vertex, useEdgeBoundsCheck, checkCorners, checkCenterDistanceXYZ, transform);

                    if (closestCell != null)
                    // if (closestCell != null && closestDistance > -1 && closestDistance < (closestCell.size))
                    // if (closestCell != null && closestDistance > -1 && closestDistance < (closestCell.size * cellRadiusMult))
                    {

                        closestCell.vertexList_V2.Add(worldspaceVertexKeys[x, z]);
                        bool isInCenterRadius = VectorUtil.DistanceXZ(closestCell.center, vertex.position) < (closestCell.size * HexagonCellPrototype.vertexCenterPointDistance);

                        vertex.type = VertexType.Cell;
                        vertex.isCellCenterPoint = isInCenterRadius;
                        vertex.isCellCornerPoint = (!isInCenterRadius);

                        globalTerrainVertexGridByCoordinate[worldspaceVertexKeys[x, z]] = vertex;
                        // if (updateElevation && vertex.isInherited == false) vertexGrid[indexX, indexZ].position.y = prototype.center.y;
                    }
                }
            }

            foreach (HexagonCellPrototype prototype in topLayerPrototypes)
            {
                if (prototype.vertexList_V2 == null || prototype.vertexList_V2.Count == 0) continue;

                float[] vertexElevationData = GetCellVertexElevationData(prototype, globalTerrainVertexGridByCoordinate);

                float maxSteepness = vertexElevationData[0];
                float avgElevation = vertexElevationData[1];
                float hightestY = vertexElevationData[2];
                float lowestY = vertexElevationData[3];

                HexagonCellPrototype nextCell = prototype;
                while (nextCell != null)
                {
                    if (nextCell != prototype) nextCell.vertexList_V2 = prototype.vertexList_V2;

                    if (avgElevation >= nextCell.center.y && avgElevation < nextCell.center.y + cellLayerElevation)
                    {
                        // Ground
                        nextCell.SetToGround(false);
                        bool isViableForFlatGround = (maxSteepness <= viableFlatGroundCellSteepnessThreshhold);
                        // (bool isViableForFlatGround, float steepness) = IsCellViableForFlatTerrain_WithSteepness(nextCell, vertexGrid, viableFlatGroundCellSteepnessThreshhold);
                        if (isViableForFlatGround)
                        {
                            // nextCell.isFlatGround = true;
                            nextCell.SetToGround(true);
                            FlattenCellVertices(nextCell, globalTerrainVertexGridByCoordinate);
                        }
                    }
                    else
                    {
                        if (avgElevation > nextCell.center.y + cellLayerElevation)
                        {
                            // Under
                            nextCell.SetCellStatus(CellStatus.UnderGround);
                            nextCell.SetMaterialContext(CellMaterialContext.Land);

                        }
                        else
                        {

                            if (nextCell.center.y < seaLevel)
                            {
                                // Underwater
                                nextCell.SetCellStatus(CellStatus.Underwater);
                                nextCell.SetMaterialContext(CellMaterialContext.Water);

                            }
                            else
                            {

                                // Above
                                nextCell.SetCellStatus(CellStatus.AboveGround);
                                nextCell.SetMaterialContext(CellMaterialContext.Air);

                            }
                        }
                    }

                    nextCell = nextCell.layerNeighbors[0];
                }
            }
        }

        public static void AssignTerrainVerticesToPrototypeGrid(
            Dictionary<int, List<HexagonCellPrototype>> gridCellPrototypesByLayer,
            TerrainVertex[,] vertexGrid,
            HexagonGrid hexGrid,
            Transform transform,
            int cellLayerElevation,
            float viableFlatGroundCellSteepnessThreshhold,
            bool useEdgeBoundsCheck,
            bool checkCorners,
            float cellRadiusMult = 1.2f,
            float seaLevel = 0,
            bool checkCenterDistanceXYZ = false
        )
        {
            int topLayer = gridCellPrototypesByLayer.Keys.Count - 1;
            List<HexagonCellPrototype> topLayerPrototypes = gridCellPrototypesByLayer[topLayer];

            for (int x = 0; x < vertexGrid.GetLength(0); x++)
            {
                for (int z = 0; z < vertexGrid.GetLength(1); z++)
                {
                    TerrainVertex vertex = vertexGrid[x, z];
                    // if (vertex.isInHexBounds == false) continue;

                    HexagonCellPrototype closestCell = null;
                    foreach (var prototype in topLayerPrototypes)
                    {
                        List<Vector3> aproximateCorners = HexCoreUtil.GenerateHexagonPoints(prototype.center, prototype.size).ToList();
                        bool withinAproxBounds = VectorUtil.IsPointWithinPolygon(vertexGrid[x, z].position, aproximateCorners);
                        if (withinAproxBounds)
                        {
                            closestCell = prototype;
                            // closestDistance = VectorUtil.DistanceXZ(vertex.position, prototype.center);
                            break;
                            // if (closestDistance < prototype.size) 
                        }
                    }

                    // (HexagonCellPrototype closestCell, float closestDistance) = GetHexagonCellProtoypeBoundsParentOfVertex(topLayerPrototypes, vertex, useEdgeBoundsCheck, checkCorners, checkCenterDistanceXYZ, transform);

                    if (closestCell != null)
                    // if (closestCell != null && closestDistance > -1 && closestDistance < (closestCell.size))
                    // if (closestCell != null && closestDistance > -1 && closestDistance < (closestCell.size * cellRadiusMult))
                    {

                        closestCell.vertexList.Add(new Vector2Int(x, z));
                        bool isInCenterRadius = VectorUtil.DistanceXZ(closestCell.center, vertex.position) < (closestCell.size * HexagonCellPrototype.vertexCenterPointDistance);

                        vertexGrid[x, z].type = VertexType.Cell;
                        vertexGrid[x, z].isCellCenterPoint = isInCenterRadius;
                        vertexGrid[x, z].isCellCornerPoint = (!isInCenterRadius);

                        // if (updateElevation && vertex.isInherited == false) vertexGrid[indexX, indexZ].position.y = prototype.center.y;
                    }
                }
            }

            foreach (HexagonCellPrototype prototype in topLayerPrototypes)
            {
                if (prototype.vertexList == null || prototype.vertexList.Count == 0) continue;

                float[] vertexElevationData = GetCellVertexElevationData(prototype, vertexGrid);

                float maxSteepness = vertexElevationData[0];
                float avgElevation = vertexElevationData[1];
                float hightestY = vertexElevationData[2];
                float lowestY = vertexElevationData[3];

                HexagonCellPrototype nextCell = prototype;
                while (nextCell != null)
                {
                    if (nextCell != prototype) nextCell.vertexList = prototype.vertexList;

                    if (avgElevation >= nextCell.center.y && avgElevation < nextCell.center.y + cellLayerElevation)
                    {
                        // Ground
                        nextCell.SetToGround(false);
                        (bool isViableForFlatGround, float steepness) = IsCellViableForFlatTerrain_WithSteepness(nextCell, vertexGrid, viableFlatGroundCellSteepnessThreshhold);
                        if (isViableForFlatGround)
                        {
                            // nextCell.isFlatGround = true;
                            nextCell.SetToGround(true);
                            FlattenCellVertices(nextCell, vertexGrid);
                            // }
                            // else
                            // {
                            // if (hexGrid != null)
                            // {
                            //     float steepnessMult = (viableFlatGroundCellSteepnessThreshhold / steepness);
                            //     Debug.LogError("steepnessMult: " + steepnessMult + ", steepness: " + steepness + ", maxSteepness: " + maxSteepness);
                            //     // float steepnessOffset = (steepness - viableFlatGroundCellSteepnessThreshhold);
                            //     // Debug.LogError("steepnessOffset: " + steepnessOffset);
                            //     if (steepnessMult > 0.75f)
                            //     {
                            //         List<HexagonCellPrototype> newCellPrototypes = HexagonCellPrototype.GenerateHexGrid(nextCell.center, 4, (int)nextCell.size, nextCell);
                            //         if (newCellPrototypes.Count > 0)
                            //         {
                            //             foreach (var index in nextCell.vertexList)
                            //             {
                            //                 foreach (HexagonCellPrototype child in newCellPrototypes)
                            //                 {

                            //                     bool withinAproxBounds = VectorUtil.IsPointWithinPolygon(vertexGrid[index.x, index.y].position, child.cornerPoints.ToList());

                            //                     if (withinAproxBounds)
                            //                     {
                            //                         child.vertexList.Add(index);
                            //                         break;
                            //                     }
                            //                 }
                            //             }

                            //             foreach (HexagonCellPrototype child in newCellPrototypes)
                            //             {
                            //                 if (IsCellViableForFlatTerrain(child, vertexGrid, viableFlatGroundCellSteepnessThreshhold - 0.3f))
                            //                 {
                            //                     child.SetToGround();
                            //                     child.isFlatGround = true;
                            //                     FlattenCellVertices(child, vertexGrid);
                            //                 }
                            //             }
                            //             if (nextCell.cellPrototypes_X4_ByLayer == null)
                            //             {
                            //                 nextCell.cellPrototypes_X4_ByLayer = new Dictionary<int, List<HexagonCellPrototype>>() {
                            //                 {nextCell.GetLayer(), newCellPrototypes}
                            //             };
                            //             }
                            //         }
                            //     }
                            // }
                        }
                    }
                    else
                    {
                        if (avgElevation > nextCell.center.y + cellLayerElevation)
                        {
                            // Under
                            nextCell.SetCellStatus(CellStatus.UnderGround);
                            nextCell.SetMaterialContext(CellMaterialContext.Land);
                        }
                        else
                        {

                            if (nextCell.center.y < seaLevel)
                            {
                                // Underwater
                                nextCell.SetCellStatus(CellStatus.Underwater);
                                nextCell.SetMaterialContext(CellMaterialContext.Water);
                            }
                            else
                            {

                                // Above
                                nextCell.SetCellStatus(CellStatus.AboveGround);
                                nextCell.SetMaterialContext(CellMaterialContext.Air);
                            }
                        }
                    }

                    nextCell = nextCell.layerNeighbors[0];
                }
            }
        }

        public static void UnassignCellVertices(List<HexagonCellPrototype> prototypes, TerrainVertex[,] vertexGrid)
        {
            foreach (HexagonCellPrototype prototype in prototypes)
            {
                if (prototype.IsRemoved() == false || prototype.vertexList == null) continue;

                foreach (var index in prototype.vertexList)
                {
                    vertexGrid[index.x, index.y].type = VertexType.Generic;
                    vertexGrid[index.x, index.y].isCellCenterPoint = false;
                    vertexGrid[index.x, index.y].isCellCornerPoint = false;
                    vertexGrid[index.x, index.y].isFlatGroundCell = false;
                }
            }
        }

        public static void AssignPathCenterVertices(List<HexagonCellPrototype> prototypePath, TerrainVertex[,] vertexGrid)
        {
            if (prototypePath == null || prototypePath.Count == 0) return;

            foreach (HexagonCellPrototype prototype in prototypePath)
            {
                if (prototype.vertexList == null) continue;

                List<int> sidesWithPathNeighbor = HexagonCellPrototype.GetPathNeighborSides(prototype);
                foreach (int side in sidesWithPathNeighbor)
                {
                    Vector2 sidePTPosXZ = new Vector2(prototype.sidePoints[side].x, prototype.sidePoints[side].z);

                    foreach (var index in prototype.vertexList)
                    {
                        Vector3 vertPos = vertexGrid[index.x, index.y].position;
                        Vector2 vertPosXZ = new Vector2(vertPos.x, vertPos.z);
                        if (Vector2.Distance(vertPosXZ, sidePTPosXZ) <= 6f)
                        {
                            vertexGrid[index.x, index.y].type = VertexType.Road;
                            vertexGrid[index.x, index.y].isCellCenterPoint = true;
                        }
                    }
                }
            }
        }


        public static void AssignParentCellVerticesToChildCells(HexagonCellPrototype host, TerrainVertex[,] vertexGrid)
        {
            if (host.vertexList == null) return;

            List<HexagonCellPrototype> groundChildCells = host.GetChildGroundCells();
            if (groundChildCells == null || groundChildCells.Count == 0)
            {
                Debug.LogError("groundChildCells invalid");
                return;
            }

            // foreach (var xz in host.vertexList)
            // {
            //     int indexX = xz.Item1;
            //     int indexZ = xz.Item2;
            //     TerrainVertex vertex = vertexGrid[indexX, indexZ];

            //     HexagonCellPrototype closestCell = HexagonCellPrototype.GetClosestPrototypeXZ(groundChildCells, vertex.position);
            //     if (closestCell != null)
            //     {
            //         if (closestCell.vertexList.Contains(vertexIndex) == false)
            //         {
            //             closestCell.vertexList.Add(vertexIndex);
            //             vertexGrid[vertexIndex / vertexGrid.GetLength(0), vertexIndex % vertexGrid.GetLength(0)].isInMicroCell = true;
            //         }
            //     }
            // }
        }

        // public static void AssignTerrainVerticesToGroundPrototypes_V2(Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer, TerrainVertex[,] vertexGrid, float steepnessThreshhold, Transform transform)
        // {
        //     int topLayer = prototypesByLayer.Keys.Count - 1;

        //     foreach (var topCell in prototypesByLayer[topLayer])
        //     {
        //         HexagonCellPrototype groundCell = HexagonCellPrototype.GetGroundCellInLayerStack(topCell);
        //         if (groundCell != null)
        //         {
        //             // groundCell._vertexIndices = new List<int>();
        //             groundCell._vertexIndicesBySide = new List<int>[groundCell.cornerPoints.Length];
        //             for (int i = 0; i < groundCell._vertexIndicesBySide.Length; i++)
        //             {
        //                 groundCell._vertexIndicesBySide[i] = new List<int>();
        //                 // groundCell._vertexIndicesBySide[i].AddRange(topCell._vertexIndicesBySide?[i]);
        //             }

        //             // groundCell._vertexIndices = topCell._vertexIndices;

        //             groundCell.SetVertexIndices(topCell._vertexIndices);
        //             groundCell._vertexIndicesBySide = topCell._vertexIndicesBySide;

        //             foreach (int vertexIndex in groundCell._vertexIndices)
        //             {
        //                 int indexX = vertexIndex / vertexGrid.GetLength(0);
        //                 int indexZ = vertexIndex % vertexGrid.GetLength(0);
        //                 TerrainVertex currentVertex = vertexGrid[indexX, indexZ];

        //                 // float distanceXZ = VectorUtil.DistanceXZ(transform.TransformVector(groundCell.center), transform.TransformVector(currentVertex.position));
        //                 float distanceXZ = VectorUtil.DistanceXZ(groundCell.center, currentVertex.position);

        //                 // Debug.Log("vertexIndex: " + vertexIndex + ", distanceXZ: " + distanceXZ + ", cell size: " + groundCell.size);

        //                 bool isCellCenterVertex = false;
        //                 float outerRadiusOffset = 0f;

        //                 if (distanceXZ < groundCell.size * 0.33f)
        //                 {
        //                     isCellCenterVertex = true;
        //                 }
        //                 else
        //                 {
        //                     if (distanceXZ > (groundCell.size * 1.05f))
        //                     {
        //                         if (vertexGrid[vertexIndex / vertexGrid.GetLength(0), vertexIndex % vertexGrid.GetLength(0)].position.y >= groundCell.center.y)
        //                         {
        //                             outerRadiusOffset += 0.5f;
        //                         }
        //                         else
        //                         {
        //                             outerRadiusOffset -= 0.5f;
        //                         }
        //                     }

        //                     // Get Closest Corner if not within center radius   
        //                     (Vector3 nearestPoint, float nearestDistance, int nearestIndex) = VectorUtil.GetClosestPoint_XZ_WithDistanceAndIndex(groundCell.cornerPoints, currentVertex.position);

        //                     if (nearestDistance != float.MaxValue)
        //                     {
        //                         HexagonSide side = HexagonCell.GetSideFromCorner((HexagonCorner)nearestIndex);
        //                         groundCell._vertexIndicesBySide[(int)side].Add(vertexIndex);

        //                         vertexGrid[indexX, indexZ].corner = nearestIndex;
        //                         vertexGrid[indexX, indexZ].isCellCornerPoint = true;
        //                     }
        //                 }

        //                 vertexGrid[indexX, indexZ].isCellCenterPoint = isCellCenterVertex;
        //                 vertexGrid[indexX, indexZ].type = VertexType.Cell;

        //                 bool isViableForFlatGround = IsCellViableForFlatTerrain(groundCell, vertexGrid, steepnessThreshhold);
        //                 groundCell._temp__groundViable = isViableForFlatGround;

        //                 if (isViableForFlatGround && (!currentVertex.markedForRemoval && !currentVertex.isInherited && !currentVertex.ignoreSmooth))
        //                 {
        //                     vertexGrid[indexX, indexZ].position.y = groundCell.center.y + outerRadiusOffset;
        //                     vertexGrid[indexX, indexZ].isFlatGroundCell = true;
        //                 }
        //             }

        //         }
        //     }
        // }

        public static void AssignTerrainVerticesToGroundPrototypes(Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer, TerrainVertex[,] vertexGrid, float cellRadiusMult = 1.3f, bool checkCorners = false)
        {
            // Consolidate Ground Cells
            List<HexagonCellPrototype> allGroundPrototypes = new List<HexagonCellPrototype>();

            foreach (var kvp in prototypesByLayer)
            {
                List<HexagonCellPrototype> prototypes = kvp.Value;

                List<HexagonCellPrototype> groundPrototypes = prototypes.FindAll(p => p.IsGround());
                foreach (HexagonCellPrototype prototype in groundPrototypes)
                {
                    // prototype._vertexIndices = new List<int>();
                    // prototype._vertexIndicesBySide = new List<int>[prototype.cornerPoints.Length];
                    // for (int i = 0; i < prototype._vertexIndicesBySide.Length; i++)
                    // {
                    //     prototype._vertexIndicesBySide[i] = new List<int>();
                    // }
                }
                allGroundPrototypes.AddRange(groundPrototypes);
            }

            // int vertexIndex = 0;
            foreach (TerrainVertex vertice in vertexGrid)
            {
                int vertexIndex = vertice.index;
                (HexagonCellPrototype closestCell, float closestDistance) = GetHexagonCellProtoypeBoundsParentOfVertex(allGroundPrototypes, vertice, true, checkCorners, false);

                int indexX = vertexIndex / vertexGrid.GetLength(0);
                int indexZ = vertexIndex % vertexGrid.GetLength(0);
                Vector3 finalPosition;

                if (closestCell != null && closestDistance < (closestCell.size * cellRadiusMult))
                {
                    closestCell.vertexList.Add(new Vector2Int(vertice.index_X, vertice.index_Z));

                    bool isCellCenterVertex = false;
                    float outerRadiusOffset = 0f;

                    if (closestDistance < closestCell.size * 0.33f)
                    {
                        isCellCenterVertex = true;
                    }
                    else
                    {
                        if (closestDistance > (closestCell.size * 1.05f))
                        {
                            if (vertexGrid[vertexIndex / vertexGrid.GetLength(0), vertexIndex % vertexGrid.GetLength(0)].position.y >= closestCell.center.y)
                            {
                                outerRadiusOffset += 0.5f;
                            }
                            else
                            {
                                outerRadiusOffset -= 0.5f;
                            }
                        }

                        // Get Closest Corner if not within center radius   
                        (Vector3 nearestPoint, float nearestDistance, int nearestIndex) = VectorUtil.GetClosestPoint_XZ_WithDistanceAndIndex(closestCell.cornerPoints, vertice.position);

                        if (nearestDistance != float.MaxValue)
                        {
                            HexagonSide side = HexCoreUtil.GetSideFromCorner((HexagonCorner)nearestIndex);
                            // closestCell._vertexIndicesBySide[(int)side].Add(vertexIndex);

                            vertexGrid[indexX, indexZ].corner = nearestIndex;
                            vertexGrid[indexX, indexZ].isCellCornerPoint = true;
                        }
                    }

                    vertexGrid[indexX, indexZ].isCellCenterPoint = isCellCenterVertex;
                    vertexGrid[indexX, indexZ].type = VertexType.Cell;

                    // finalPosition = new Vector3(vertice.position.x, closestCell.center.y + outerRadiusOffset, vertice.position.z);
                }
                else
                {
                    finalPosition = new Vector3(vertice.position.x, vertice.position.y, vertice.position.z);
                }

                // if (!vertice.markedForRemoval && !vertice.isInherited && !vertice.ignoreSmooth) vertexGrid[indexX, indexZ].position = finalPosition;

                // vertexIndex++;
            }


            foreach (var groundCell in allGroundPrototypes)
            {
                foreach (var vertIndex in groundCell.vertexList)
                {
                    int indexX = vertIndex.x;
                    int indexZ = vertIndex.y;
                    TerrainVertex currentVertex = vertexGrid[indexX, indexZ];

                    float distanceXZ = VectorUtil.DistanceXZ(groundCell.center, currentVertex.position);

                    // Debug.Log("distanceXZ: " + distanceXZ + ", cell size: " + groundCell.size);

                    bool isCellCenterVertex = false;
                    float outerRadiusOffset = 0f;

                    if (distanceXZ < groundCell.size * 0.33f)
                    {
                        isCellCenterVertex = true;
                    }
                    else
                    {
                        if (distanceXZ > (groundCell.size * 1.05f))
                        {
                            if (vertexGrid[indexX, indexZ].position.y >= groundCell.center.y)
                            {
                                outerRadiusOffset += 0.5f;
                            }
                            else
                            {
                                outerRadiusOffset -= 0.5f;
                            }
                        }

                        // Get Closest Corner if not within center radius   
                        (Vector3 nearestPoint, float nearestDistance, int nearestIndex) = VectorUtil.GetClosestPoint_XZ_WithDistanceAndIndex(groundCell.cornerPoints, currentVertex.position);

                        if (nearestDistance != float.MaxValue)
                        {
                            HexagonSide side = HexCoreUtil.GetSideFromCorner((HexagonCorner)nearestIndex);
                            // groundCell._vertexIndicesBySide[(int)side].Add(vertIndex);

                            vertexGrid[indexX, indexZ].corner = nearestIndex;
                            vertexGrid[indexX, indexZ].isCellCornerPoint = true;
                        }
                    }

                    vertexGrid[indexX, indexZ].isCellCenterPoint = isCellCenterVertex;
                    vertexGrid[indexX, indexZ].type = VertexType.Cell;


                    bool isViableForFlatGround = IsCellViableForFlatTerrain(groundCell, vertexGrid, 4);
                    // groundCell._temp__groundViable = isViableForFlatGround;

                    if (isViableForFlatGround && (!currentVertex.markedForRemoval && !currentVertex.isInherited && !currentVertex.ignoreSmooth))
                    {
                        vertexGrid[indexX, indexZ].position = new Vector3(currentVertex.position.x, groundCell.center.y + outerRadiusOffset, currentVertex.position.z);
                        vertexGrid[indexX, indexZ].isFlatGroundCell = true;
                    }
                }
            }

        }

        public static void UpdateTerrainVertexDataWithPrototype(TerrainVertex vertex, HexagonCellPrototype prototype, TerrainVertex[,] vertexGrid, bool updateElevation = true)
        {
            int indexX = vertex.index / vertexGrid.GetLength(0);
            int indexZ = vertex.index % vertexGrid.GetLength(0);

            bool isCellCenterVertex = false;
            // if (VectorUtil.DistanceXZ(prototype.center, vertex.position) < (prototype.size * HexagonCellPrototype.vertexCenterPointDistance))
            // {
            //     isCellCenterVertex = true;
            // }
            // else
            // {
            //     // Get Closest Corner if not within center radius   
            //     (Vector3 nearestPoint, float nearestDistance, int nearestIndex) = VectorUtil.GetClosestPoint_XZ_WithDistanceAndIndex(prototype.cornerPoints, vertex.position);

            //     if (nearestDistance != float.MaxValue)
            //     {
            //         HexagonSide side = HexagonCell.GetSideFromCorner((HexagonCorner)nearestIndex);

            //         if (prototype._vertexIndicesBySide == null) prototype._vertexIndicesBySide = new List<int>[6] { new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>() };

            //         prototype._vertexIndicesBySide[(int)side].Add(vertex.index);

            //         vertexGrid[indexX, indexZ].corner = nearestIndex;
            //         vertexGrid[indexX, indexZ].isCellCornerPoint = true;
            //     }
            // }

            // if (updateElevation && vertex.isInherited == false) vertexGrid[indexX, indexZ].position.y = prototype.center.y;

            vertexGrid[indexX, indexZ].isCellCenterPoint = isCellCenterVertex;
            vertexGrid[indexX, indexZ].type = VertexType.Cell;

            // Debug.LogError("cell assigned to vertex");
        }

        public static (HexagonCellPrototype, float) GetClosestHexagonCellPrototypeToVertex(List<HexagonCellPrototype> prototypes, TerrainVertex vertex)
        {
            Vector3 vertPosXYZ = vertex.position;
            HexagonCellPrototype closestCell = null;
            float closestDistance = float.MaxValue;

            foreach (HexagonCellPrototype prototype in prototypes)
            {
                float distance = VectorUtil.DistanceXZ(prototype.center, vertPosXYZ);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestCell = prototype;
                }
            }
            return (closestCell, closestDistance);
        }

        public static (HexagonCellPrototype, float) GetHexagonCellProtoypeBoundsParentOfVertex(
            List<HexagonCellPrototype> prototypes,
            TerrainVertex vertex,
            bool useEdgeBoundsCheck,
            bool checkCorners,
            bool checkCenterDistanceXYZ,
            Transform transform = null
        )
        {
            Vector3 vertPosXYZ = transform != null ? transform.TransformPoint(vertex.position) : vertex.position;
            // Vector3 vertPosXYZ = vertex.position;
            HexagonCellPrototype closestCell = null;
            float closestDistance = float.MaxValue;

            foreach (HexagonCellPrototype prototype in prototypes)
            {
                // Vector3 posXYZ = transform != null ? transform.TransformPoint(prototype.center) : prototype.center;

                // if (useEdgeBoundsCheck && !checkCenterDistanceXYZ)
                // {
                //     bool withinAproxBounds = VectorUtil.IsPointWithinPolygon(vertex.position, prototype.cornerPoints.ToList());
                //     if (withinAproxBounds)
                //     {
                //         closestCell = prototype;
                //         closestDistance = VectorUtil.DistanceXZ(vertex.position, prototype.center);

                //         if (closestDistance < prototype.size) break;

                //         continue;
                //     }

                //     // (bool inBounds, float centerDistance) = HexagonCellPrototype.IsPointWithinEdgeBounds_WithDistance(vertPosXYZ, prototype);
                //     // if (inBounds)
                //     // {
                //     //     // float dist = VectorUtil.DistanceXZ(posXYZ, vertPosXYZ);
                //     //     // Debug.Log("dist: " + dist + ", prototype.size: " + prototype.size);
                //     //     closestCell = prototype;
                //     //     closestDistance = centerDistance;

                //     //     if (centerDistance < prototype.size) break;

                //     //     continue;
                //     // }
                // }

                Vector3 posXYZ = transform != null ? transform.InverseTransformPoint(prototype.center) : prototype.center;
                Vector3 vectorPosXZ = transform != null ? transform.InverseTransformPoint(vertex.position) : vertex.position;
                vectorPosXZ.y = 0;

                posXYZ.y = 0;

                float distance = Vector3.Distance(posXYZ, vectorPosXZ);
                // float distance = checkCenterDistanceXYZ ? Vector3.Distance(posXYZ, vertex.position) : VectorUtil.DistanceXZ(posXYZ, vertPosXYZ);

                if (distance < closestDistance)
                {
                    if (checkCorners)
                    {
                        Vector3[] prototypeCorners = prototype.cornerPoints;
                        float xMin = prototypeCorners[0].x;
                        float xMax = prototypeCorners[0].x;
                        float zMin = prototypeCorners[0].z;
                        float zMax = prototypeCorners[0].z;
                        for (int i = 1; i < prototypeCorners.Length; i++)
                        {
                            if (prototypeCorners[i].x < xMin)
                                xMin = prototypeCorners[i].x;
                            if (prototypeCorners[i].x > xMax)
                                xMax = prototypeCorners[i].x;
                            if (prototypeCorners[i].z < zMin)
                                zMin = prototypeCorners[i].z;
                            if (prototypeCorners[i].z > zMax)
                                zMax = prototypeCorners[i].z;
                        }

                        if (vertex.position.x >= xMin && vertex.position.x <= xMax && vertex.position.z >= zMin && vertex.position.z <= zMax)
                        {
                            closestDistance = distance;
                            closestCell = prototype;
                        }
                    }
                    else
                    {
                        closestDistance = distance;
                        closestCell = prototype;
                    }
                }
            }
            return (closestCell, closestDistance);
        }



        public static Dictionary<Vector2, List<(int, int)>> AssignTerrainVerticesToSurfaceMapsByPrototype(List<HexagonCellPrototype> prototypes, TerrainVertex[,] vertexGrid)
        {
            Dictionary<Vector2, List<(int, int)>> _terrainVertexSurfacesIndiciesByCellXZCenter = new Dictionary<Vector2, List<(int, int)>>();

            foreach (TerrainVertex vertex in vertexGrid)
            {
                if (vertex.markedForRemoval) continue;

                (HexagonCellPrototype closestCell, float closestDistance) = GetHexagonCellProtoypeBoundsParentOfVertex(prototypes, vertex, true, false, false);

                if (closestCell == null)
                {
                    // Debug.LogError("NO closestCell found");
                    continue;
                }

                int indexX = vertex.index_X;
                int indexZ = vertex.index_Z;
                Vector2 cellPosXZ = new Vector2(closestCell.center.x, closestCell.center.z);

                if (_terrainVertexSurfacesIndiciesByCellXZCenter.ContainsKey(cellPosXZ) == false)
                {
                    _terrainVertexSurfacesIndiciesByCellXZCenter.Add(cellPosXZ, new List<(int, int)>());
                }

                _terrainVertexSurfacesIndiciesByCellXZCenter[cellPosXZ].Add((indexX, indexZ));
            }
            return _terrainVertexSurfacesIndiciesByCellXZCenter;
        }

        // public static Dictionary<Vector2, List<(int, int)>> EvaluatePrototypeTerrainStatus(Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer, TerrainVertex[,] vertexGrid)
        // {
        //     foreach (TerrainVertex vertex in vertexGrid)
        //     {
        //         if (vertex.markedForRemoval) continue;

        //         (HexagonCellPrototype closestCell, float closestDistance) = GetHexagonCellProtoypeBoundsParentOfVertex(prototypes, vertex, true, false, false);

        //         if (closestCell == null)
        //         {
        //             // Debug.LogError("NO closestCell found");
        //             continue;
        //         }

        //         // Get closest vertex
        //         float closestDistance = float.MaxValue;
        //         TerrainVertex closestVertex = vertexGrid[0, 0];

        //         for (int x = 0; x < vertexGrid.GetLength(0); x++)
        //         {
        //             for (int z = 0; z < vertexGrid.GetLength(1); z++)
        //             {
        //                 TerrainVertex currentVertex = vertexGrid[x, z];

        //                 Vector2 vertexPosXZ = new Vector2(currentVertex.position.x, currentVertex.position.z);
        //                 float dist = VectorUtil.Distance(prototypeCell.center, );
        //                 if (dist < closestDistance)
        //                 {
        //                     // Debug.Log("currentVertex - elevationY:" + currentVertex.position.y);
        //                     closestDistance = dist;
        //                     closestVertex = currentVertex;
        //                 }

        //             }
        //         }

        //         if (closestDistance != float.MaxValue) HexagonCellPrototype.ClearLayersAboveElevationAndSetGround(prototypeCell, closestVertex.position.y, distanceYOffset, fallbackOnBottomCell);
        //     }

        //     // (HexagonCellPrototype closestCell, float closestDistance) = GetClosestHexagonCellPrototypeToVertex(prototypes, vertex);
        // }


        public static void GroundPrototypesToTerrainVertexElevation(
            Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer,
            TerrainVertex[,] vertexGrid,
            Transform transform,
            float distanceYOffset,
            float flatGroundSteepnessThreshhold = 4f,
            bool fallbackOnBottomCell = false
        )
        {
            int topLayer = prototypesByLayer.Keys.Count - 1;
            List<HexagonCellPrototype> topLayerPrototypes = prototypesByLayer[topLayer];

            foreach (HexagonCellPrototype prototypeCell in topLayerPrototypes)
            {
                if (prototypeCell.vertexList == null || prototypeCell.vertexList.Count == 0) continue;

                // Debug.Log("GroundPrototypesToTerrainVertexElevation - A");
                // HexagonCellPrototype groundFound = HexagonCellPrototype.ClearLayersAboveVertexElevationsAndSetGround(
                //     prototypeCell,
                //     prototypeCell.vertexList,
                //     prototypeCell._vertexIndicesBySide,
                //     vertexGrid,
                //     transform,
                //     distanceYOffset,
                //     fallbackOnBottomCell
                // );

                // if (groundFound != null)
                // {
                //     bool isViableForFlatGround = IsCellViableForFlatTerrain(groundFound, vertexGrid, flatGroundSteepnessThreshhold);
                //     groundFound.isFlatGround = isViableForFlatGround;

                //     if (isViableForFlatGround)
                //     {
                //         foreach (int vertexIndex in groundFound._vertexIndices)
                //         {
                //             int indexX = vertexIndex / vertexGrid.GetLength(0);
                //             int indexZ = vertexIndex % vertexGrid.GetLength(0);
                //             TerrainVertex vertex = vertexGrid[indexX, indexZ];

                //             if (TerrainVertexUtil.ShouldSmoothIgnore(vertex)) continue;

                //             // if (isViableForFlatGround && (!vertex.markedForRemoval && !vertex.isInherited && !vertex.ignoreSmooth))
                //             // {
                //             // vertexGrid[indexX, indexZ].position.y = groundCell.center.y + outerRadiusOffset;
                //             vertexGrid[indexX, indexZ].isFlatGroundCell = true;
                //             // }
                //             if (groundFound != null && vertex.isInherited == false)
                //             {
                //                 Vector3 pos = vertexGrid[indexX, indexZ].position;
                //                 pos.y = groundFound.center.y;

                //                 vertexGrid[indexX, indexZ].position = pos;

                //                 // if (vertex.isInherited || vertex.parallelVertexIndex_X != -1)
                //                 // {
                //                 //     vertexGrid[vertex.index_X, vertex.index_Z].position.y = vertexGridDataByWorldSpaceCoordinate[vertex.parallelWorldspaceOwnerCoordinate][vertex.parallelVertexIndex_X, vertex.parallelVertexIndex_Z].position.y;
                //                 //     // vertexGridDataByWorldSpaceCoordinate[vertex.parallelWorldspaceOwnerCoordinate][vertex.parallelVertexIndex_X, vertex.parallelVertexIndex_Z].position.y = vertexGrid[vertex.index_X, vertex.index_Z].position.y - 0.2f;
                //                 //     // continue;
                //                 // }


                //             }
                //         }
                //     }
                // }




                // (HexagonCellPrototype closestCell, float closestDistance) = GetClosestHexagonCellPrototypeToVertex(prototypes, vertex);


                // // Get closest vertex
                // float closestDistance = float.MaxValue;
                // TerrainVertex closestVertex = vertexGrid[0, 0];

                // for (int x = 0; x < vertexGrid.GetLength(0); x++)
                // {
                //     for (int z = 0; z < vertexGrid.GetLength(1); z++)
                //     {
                //         TerrainVertex currentVertex = vertexGrid[x, z];

                //         Vector2 vertexPosXZ = new Vector2(currentVertex.position.x, currentVertex.position.z);
                //         float dist = VectorUtil.Distance(prototypeCell.center, );
                //         if (dist < closestDistance)
                //         {
                //             // Debug.Log("currentVertex - elevationY:" + currentVertex.position.y);
                //             closestDistance = dist;
                //             closestVertex = currentVertex;
                //         }

                //     }
                // }

                // if (closestDistance != float.MaxValue) HexagonCellPrototype.ClearLayersAboveElevationAndSetGround(prototypeCell, closestVertex.position.y, distanceYOffset, fallbackOnBottomCell);
            }
        }

        // public static List<TerrainVertex> GetCellVertices(HexagonCellPrototype prototype, TerrainVertex[,] vertexGrid)
        // {

        //     List<TerrainVertex> vertices = new List<TerrainVertex>();

        //     if (prototype._vertexIndices != null && prototype._vertexIndices.Count > 0)
        //     {
        //         int vertexGridLength = vertexGrid.GetLength(0);

        //         foreach (int ix in prototype._vertexIndices)
        //         {
        //             vertices.Add(vertexGrid[ix / vertexGridLength, ix % vertexGridLength]);
        //         }
        //     }

        //     return vertices;
        // }

        public static bool FlattenCellVertices(HexagonCellPrototype prototype, Dictionary<Vector2, TerrainVertex> globalTerrainVertexGridByCoordinate)
        {
            if (prototype.vertexList_V2 == null || prototype.vertexList_V2.Count == 0) return false;

            foreach (var index in prototype.vertexList_V2)
            {
                TerrainVertex vertex = globalTerrainVertexGridByCoordinate[index];

                vertex.type = VertexType.Cell;
                vertex.position.y = prototype.center.y;
                vertex.isFlatGroundCell = true;

                globalTerrainVertexGridByCoordinate[index] = vertex;
            }
            return true;
        }

        public static bool FlattenCellVertices(HexagonCellPrototype prototype, TerrainVertex[,] vertexGrid)
        {
            if (prototype.vertexList == null || prototype.vertexList.Count == 0) return false;

            foreach (var index in prototype.vertexList)
            {
                vertexGrid[index.x, index.y].type = VertexType.Cell;
                vertexGrid[index.x, index.y].position.y = prototype.center.y;
                vertexGrid[index.x, index.y].isFlatGroundCell = true;
            }
            return true;
        }

        public static float GetVertexSteepnesInCell(HexagonCellPrototype prototype, TerrainVertex[,] vertexGrid)
        {
            if (prototype.vertexList == null || prototype.vertexList.Count == 0) return -1f;

            int vertexGridLength = vertexGrid.GetLength(0);

            int vertexCount = prototype.vertexList.Count;
            float[] elevations = new float[vertexCount];
            int currIX = 0;

            foreach (var index in prototype.vertexList)
            {
                elevations[currIX] = vertexGrid[index.x, index.y].position.y;
                currIX++;
            }

            float avgElevation = UtilityHelpers.CalculateAverageOfArray(elevations);
            float maxSteepness = 0f;

            foreach (float elevation in elevations)
            {
                float steepness = Mathf.Abs(elevation - avgElevation);
                if (steepness > maxSteepness)
                {
                    maxSteepness = steepness;
                }
            }

            // Debug.Log("avgElevation: " + avgElevation + ", maxSteepness: " + maxSteepness);
            return maxSteepness;
        }

        public static float[] GetCellVertexElevationData(
            HexagonCellPrototype prototype,
            Dictionary<Vector2, TerrainVertex> globalTerrainVertexGridByCoordinate
        )
        {
            if (prototype.vertexList_V2 == null || prototype.vertexList_V2.Count == 0) return null;

            int vertexCount = prototype.vertexList_V2.Count;
            float[] elevations = new float[vertexCount];
            int currIX = 0;

            float lowestY = float.MaxValue;
            float hightestY = float.MinValue;

            foreach (var index in prototype.vertexList_V2)
            {
                TerrainVertex vertex = globalTerrainVertexGridByCoordinate[index];
                float elevationY = vertex.position.y;
                elevations[currIX] = elevationY;

                if (lowestY > elevationY) lowestY = elevationY;
                if (hightestY < elevationY) hightestY = elevationY;

                currIX++;
            }

            float avgElevation = UtilityHelpers.CalculateAverageOfArray(elevations);
            float maxSteepness = 0f;

            foreach (float elevation in elevations)
            {
                float steepness = Mathf.Abs(elevation - avgElevation);
                if (steepness > maxSteepness)
                {
                    maxSteepness = steepness;
                }
            }

            float[] data = new float[4];
            data[0] = maxSteepness;
            data[1] = avgElevation;
            data[2] = hightestY;
            data[3] = lowestY;

            return data;
        }
        public static float[] GetCellVertexElevationData(HexagonCellPrototype prototype, TerrainVertex[,] vertexGrid)
        {
            if (prototype.vertexList == null || prototype.vertexList.Count == 0) return null;

            int vertexGridLength = vertexGrid.GetLength(0);
            int vertexCount = prototype.vertexList.Count;
            float[] elevations = new float[vertexCount];
            int currIX = 0;

            float lowestY = float.MaxValue;
            float hightestY = float.MinValue;

            foreach (var index in prototype.vertexList)
            {
                float elevationY = vertexGrid[index.x, index.y].position.y;
                elevations[currIX] = elevationY;

                if (lowestY > elevationY) lowestY = elevationY;
                if (hightestY < elevationY) hightestY = elevationY;

                currIX++;
            }

            float avgElevation = UtilityHelpers.CalculateAverageOfArray(elevations);
            float maxSteepness = 0f;

            foreach (float elevation in elevations)
            {
                float steepness = Mathf.Abs(elevation - avgElevation);
                if (steepness > maxSteepness)
                {
                    maxSteepness = steepness;
                }
            }

            float[] data = new float[4];
            data[0] = maxSteepness;
            data[1] = avgElevation;
            data[2] = hightestY;
            data[3] = lowestY;

            return data;
        }

        public static bool IsCellViableForFlatTerrain(HexagonCellPrototype prototype, TerrainVertex[,] vertexGrid, float steepnessThreshhold)
        {
            if (prototype.vertexList == null || prototype.vertexList.Count == 0) return false;

            float steepness = GetVertexSteepnesInCell(prototype, vertexGrid);

            // Debug.Log("steepness: " + steepness + ", steepnessThreshhold: " + steepnessThreshhold);
            return (steepness <= steepnessThreshhold);
        }
        public static (bool, float) IsCellViableForFlatTerrain_WithSteepness(HexagonCellPrototype prototype, TerrainVertex[,] vertexGrid, float steepnessThreshhold)
        {
            if (prototype.vertexList == null || prototype.vertexList.Count == 0) return (false, -1);

            float steepness = GetVertexSteepnesInCell(prototype, vertexGrid);

            // Debug.Log("steepness: " + steepness + ", steepnessThreshhold: " + steepnessThreshhold);
            return ((steepness <= steepnessThreshhold), steepness);
        }

        // public static List<HexagonCellPrototype> GetViableCellsForFlatGroundTerrain(List<HexagonCellPrototype> prototypesWithAssignedVertices, TerrainVertex[,] vertexGrid, float steepnessThreshhold, bool findGroundCell)
        // {
        //     List<HexagonCellPrototype> results = new List<HexagonCellPrototype>(); ;

        //     foreach (HexagonCellPrototype prototype in prototypesWithAssignedVertices)
        //     {

        //         if (findGroundCell)
        //         {
        //             HexagonCellPrototype groundCell = HexagonCellPrototype.GetGroundCellInLayerStack(prototype);
        //             if (groundCell != null)
        //             {
        //                 if (IsCellViableForFlatTerrain(groundCell, vertexGrid, steepnessThreshhold))
        //                 {
        //                     groundCell._temp__groundViable = true;
        //                     results.Add(groundCell);
        //                 }
        //                 else
        //                 {
        //                     groundCell._temp__groundViable = false;
        //                 }
        //             }
        //             continue;
        //         }

        //         if (IsCellViableForFlatTerrain(prototype, vertexGrid, steepnessThreshhold))
        //         {
        //             prototype._temp__groundViable = true;
        //             results.Add(prototype);
        //         }
        //         else
        //         {
        //             prototype._temp__groundViable = false;
        //         }
        //     }
        //     return results;
        // }


        // public static void AssignRampsForIslandLayerPrototypes(Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer, TerrainVertex[,] vertexGrid, int layerElevation = 4, float elevationStep = 0.45f)
        // {
        //     int rampsAssigned = 0;
        //     int totalLayers = prototypesByLayer.Keys.Count;

        //     foreach (var kvpA in prototypesByLayer)
        //     {
        //         if (kvpA.Key == 0) continue;

        //         List<HexagonCellPrototype> prototypesForLayer = kvpA.Value;
        //         Dictionary<int, List<HexagonCellPrototype>> prototypesByIsland = HexagonCellPrototype.GetIslandsFromLayerPrototypes(prototypesForLayer.FindAll(p => p.IsGround()));
        //         // Debug.Log("AssignRampsForIslandLayerPrototypes - Layer: " + kvpA.Key + ", prototypeIslands: " + prototypesByIsland.Keys.Count);

        //         foreach (var kvpB in prototypesByIsland)
        //         {
        //             bool isBottomLayer = (kvpA.Key == 0);
        //             int layerTarget = isBottomLayer ? 1 : 0;

        //             List<HexagonCellPrototype> possibleIslandRamps = kvpB.Value.FindAll(
        //                         p => p.layerNeighbors[layerTarget] != null && p.layerNeighbors[layerTarget].neighbors.Find(n => n.IsGround() && n.layer == p.layerNeighbors[layerTarget].layer) != null)
        //                         .OrderByDescending(c => c.neighbors.Count).ToList();

        //             // Debug.Log("AssignRampsForIslandLayerPrototypes - Layer: " + kvpA.Key + ", island: " + kvpB.Key + ", possibleIslandRamps: " + possibleIslandRamps.Count);

        //             if (possibleIslandRamps.Count > 0)
        //             {
        //                 possibleIslandRamps[0].isGroundRamp = true;

        //                 if (isBottomLayer)
        //                 {
        //                     possibleIslandRamps[0].SetVertices(VertexType.Road, vertexGrid);
        //                 }
        //                 else
        //                 {
        //                     possibleIslandRamps[0].rampSlopeSides = new List<int>();
        //                     for (var side = 0; side < possibleIslandRamps[0].neighborsBySide.Length; side++)
        //                     {
        //                         HexagonCellPrototype sideNeighbor = possibleIslandRamps[0].neighborsBySide[side];

        //                         if (sideNeighbor != null && sideNeighbor.GetCellStatus() == CellStatus.AboveGround)
        //                         {
        //                             possibleIslandRamps[0].rampSlopeSides.Add(side);

        //                         }
        //                     }

        //                     SmoothRampVertices(possibleIslandRamps[0], vertexGrid, layerElevation, elevationStep);
        //                 }

        //                 rampsAssigned++;
        //             }
        //         }

        //     }
        //     // Debug.Log("AssignRampsForIslandLayerPrototypes - rampsAssigned: " + rampsAssigned);
        // }


        public static void SmoothVertexElevationAlongPath(List<HexagonCellPrototype> prototypePath, TerrainVertex[,] vertexGrid, float ratio = 1f, int neighborDepth = 3, int cellGridWeight = 2)
        {
            if (prototypePath == null || prototypePath.Count == 0) return;

            List<TerrainVertex> vertexList = new List<TerrainVertex>();
            foreach (HexagonCellPrototype prototype in prototypePath)
            {
                if (prototype.vertexList == null || prototype.vertexList.Count == 0) continue;

                foreach (var index in prototype.vertexList)
                {
                    vertexList.Add(vertexGrid[index.x, index.y]);
                }
            }

            Vector3 centerPos = HexagonCellPrototype.CalculateCenterPositionFromGroup(prototypePath);
            HexagonCellPrototype centerCell = HexagonCellPrototype.GetClosestByCenterPoint(prototypePath, centerPos);

            int inheritedWeight = 3;

            TerrainVertexUtil.SmoothVertices_Path(
                vertexList,
                 vertexGrid,
                  centerCell.center,
                  ratio,
                  neighborDepth,
                  cellGridWeight,
                  inheritedWeight
             );

            // TerrainVertexUtil.SmoothVertices_Path(vertexList, vertexGrid, centerCell.center, ratio, neighborDepth ,cellGridWeight, inheritedWeight,markSmoothIgnore, useIgnoreRules);
        }


        public static List<TerrainVertex> EvaluateGridEdgeVertices(TerrainVertex[,] vertexGrid, int neighborDepth = 8)
        {
            List<TerrainVertex> verticesOnTheGridEdge = new List<TerrainVertex>();
            for (int z = 0; z < vertexGrid.GetLength(1); z++)
            {
                for (int x = 0; x < vertexGrid.GetLength(0); x++)
                {
                    TerrainVertex vertex = vertexGrid[x, z];
                    if (vertex.markedForRemoval) continue;

                    // Reset first
                    vertexGrid[x, z].hasCellVertexNeighbor = false;
                    vertexGrid[x, z].cellVertexNeighbors = 0;
                    vertexGrid[x, z].closestCellVertexNeighborDist = -1;

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


                            // if (
                            // vertex.isCellCenterPoint
                            // || vertex.isCellCornerPoint
                            // // || vertex.isInMicroCell
                            // || vertex.isOnTunnelGroundEntry
                            // || vertex.isOnTunnelCell
                            // )

                            if (TerrainVertexUtil.IsCellVertex(neighborVertex))
                            {
                                vertexGrid[x, z].hasCellVertexNeighbor = true;
                                vertexGrid[x, z].cellVertexNeighbors++;

                                verticesOnTheGridEdge.Add(vertex);

                                float dist = VectorUtil.DistanceXZ(vertex.position, neighborVertex.position);

                                if (vertex.closestCellVertexNeighborDist == -1 || vertex.closestCellVertexNeighborDist < dist)
                                {
                                    vertexGrid[x, z].closestCellVertexNeighborDist = dist;
                                }

                            }
                        }
                    }
                }
            }
            return verticesOnTheGridEdge;
        }

        public static void SmoothGridEdgeVertexList(TerrainVertex[,] vertexGrid, int neighborDepth = 8, float ratio = 1f, int cellGridWeight = 1, int cellGridVertexNeighborEvaluationDepth = 8)
        {
            EvaluateGridEdgeVertices(vertexGrid, cellGridVertexNeighborEvaluationDepth);

            List<TerrainVertex> verticesToSmooth = new List<TerrainVertex>();

            for (int z = 0; z < vertexGrid.GetLength(1); z++)
            {
                for (int x = 0; x < vertexGrid.GetLength(0); x++)
                {
                    TerrainVertex vertex = vertexGrid[x, z];
                    if (vertex.markedForRemoval) continue;

                    // if (vertex.hasCellVertexNeighbor)
                    // {
                    verticesToSmooth.Add(vertex);
                    // }
                }
            }

            // verticesToSmooth.Sort((a, b) => (a.hasCellVertexNeighbor.CompareTo(b.hasCellVertexNeighbor)));
            // verticesToSmooth.Sort((a, b) => a.cellVertexNeighbors.CompareTo(b.cellVertexNeighbors));
            // verticesToSmooth.Sort((a, b) => b.cellVertexNeighbors.CompareTo(a.cellVertexNeighbors));

            verticesToSmooth.Sort((v1, v2) =>
            {
                int result = v2.cellVertexNeighbors.CompareTo(v1.cellVertexNeighbors);
                if (result == 0)
                    result = v1.closestCellVertexNeighborDist.CompareTo(v2.closestCellVertexNeighborDist);
                if (result == 0)
                    result = v1.isInherited.CompareTo(v2.isInherited);

                return result;
            });


            bool markSmoothIgnore = false;
            int inheritedWeight = 4;

            // Debug.Log("verticesToSmooth: " + verticesToSmooth.Count);

            foreach (TerrainVertex vertex in verticesToSmooth)
            {
                if (TerrainVertexUtil.ShouldSmoothIgnore(vertex)) continue;
                // if (vertex.ignoreSmooth || vertex.isInherited) continue;

                if (vertex.type == VertexType.Road) continue;

                if (
                vertex.isFlatGroundCell &&

                (vertex.isCellCenterPoint
                || vertex.isCellCornerPoint
                || vertex.isOnTunnelGroundEntry
                || vertex.isOnTunnelCell)
                // || vertex.isInMicroCell
                )
                {
                    continue;
                }

                bool smoothed = TerrainVertexUtil.SmoothTerrainVertexByNeighbors(
                    vertex,
                    vertexGrid,
                    markSmoothIgnore,
                    ratio,
                    neighborDepth,
                    cellGridWeight,
                    inheritedWeight
                // useIgnoreRules
                );

                // vertexGrid[vertex.index_X, vertex.index_Z].isOnTheEdgeOftheGrid = true;
            }
        }

        public static void SmoothGridEdgeVertexList__V2(
            TerrainVertex[,] vertexGrid,
            Dictionary<Vector2, TerrainVertex[,]> vertexGridDataByWorldSpaceCoordinate,
            int neighborDepth = 8,
            float ratio = 1f,
            int cellGridWeight = 1,
            int cellGridVertexNeighborEvaluationDepth = 8)
        {
            EvaluateGridEdgeVertices(vertexGrid, cellGridVertexNeighborEvaluationDepth);

            List<TerrainVertex> verticesToSmooth = new List<TerrainVertex>();

            for (int z = 0; z < vertexGrid.GetLength(1); z++)
            {
                for (int x = 0; x < vertexGrid.GetLength(0); x++)
                {
                    TerrainVertex vertex = vertexGrid[x, z];
                    if (vertex.markedForRemoval) continue;

                    // if (vertex.hasCellVertexNeighbor)
                    // {
                    verticesToSmooth.Add(vertex);
                    // }
                }
            }
            // verticesToSmooth.Sort((a, b) => (a.hasCellVertexNeighbor.CompareTo(b.hasCellVertexNeighbor)));
            // verticesToSmooth.Sort((a, b) => a.cellVertexNeighbors.CompareTo(b.cellVertexNeighbors));
            // verticesToSmooth.Sort((a, b) => b.cellVertexNeighbors.CompareTo(a.cellVertexNeighbors));

            verticesToSmooth.Sort((v1, v2) =>
            {
                int result = v1.isInherited.CompareTo(v2.isInherited);
                if (result == 0)
                    result = v2.cellVertexNeighbors.CompareTo(v1.cellVertexNeighbors);
                if (result == 0)
                    result = v1.closestCellVertexNeighborDist.CompareTo(v2.closestCellVertexNeighborDist);

                return result;
            });
            // verticesToSmooth.Sort((v1, v2) =>
            // {
            //     int result = v2.cellVertexNeighbors.CompareTo(v1.cellVertexNeighbors);
            //     if (result == 0)
            //         result = v1.closestCellVertexNeighborDist.CompareTo(v2.closestCellVertexNeighborDist);
            //     if (result == 0)
            //         result = v1.isInherited.CompareTo(v2.isInherited);

            //     return result;
            // });


            bool markSmoothIgnore = false;
            int inheritedWeight = 3;

            // Debug.Log("verticesToSmooth: " + verticesToSmooth.Count);

            foreach (TerrainVertex vertex in verticesToSmooth)
            {
                if (TerrainVertexUtil.ShouldSmoothIgnore(vertex)) continue;
                // if (vertex.ignoreSmooth || vertex.isInherited) continue;

                if (vertex.type == VertexType.Road) continue;

                if (
                vertex.isFlatGroundCell &&

                (vertex.isCellCenterPoint
                || vertex.isCellCornerPoint
                || vertex.isOnTunnelGroundEntry
                || vertex.isOnTunnelCell)
                // || vertex.isInMicroCell
                )
                {
                    continue;
                }

                bool smoothed = TerrainVertexUtil.SmoothTerrainVertexByNeighbors__V2(
                    vertex,
                    vertexGrid,
                    vertexGridDataByWorldSpaceCoordinate,

                    markSmoothIgnore,
                    ratio,
                    neighborDepth,
                    cellGridWeight,
                    inheritedWeight
                // useIgnoreRules
                );

                // vertexGrid[vertex.index_X, vertex.index_Z].isOnTheEdgeOftheGrid = true;
            }
        }

        public static void SmoothGridEdgeVertexIndices(List<HexagonCellPrototype> allGroundEdgePrototypes, TerrainVertex[,] vertexGrid, float cellRadiusMult, float searchDistance = 36f, float smoothingFactor = 1f, float smoothingSigma = 0.5f)
        {
            for (int x = 0; x < vertexGrid.GetLength(0); x++)
            {
                for (int z = 0; z < vertexGrid.GetLength(1); z++)
                {
                    TerrainVertex currentVertex = vertexGrid[x, z];

                    if (currentVertex.isInherited) continue;

                    HexagonCellPrototype closestPrototype = HexagonCellPrototype.GetClosestPrototypeXYZ(allGroundEdgePrototypes, currentVertex.position);
                    if (closestPrototype != null)
                    {
                        float avgY = HexagonCellPrototype.GetAverageElevationOfClosestPrototypes(allGroundEdgePrototypes, currentVertex.position, searchDistance * 1.25f);

                        Vector2 vertexPosXZ = new Vector2(currentVertex.position.x, currentVertex.position.z);
                        Vector2 cellPosXZ = new Vector2(closestPrototype.center.x, closestPrototype.center.z);

                        float distanceY = Mathf.Abs(currentVertex.baseNoiseHeight - (avgY));
                        // float distanceY = Mathf.Abs(currentVertex.baseNoiseHeight - (closestPrototype.center.y));
                        // float distance = Mathf.Abs((Vector2.Distance(vertexPosXZ, cellPosXZ) - ((closestPrototype.size * cellRadiusMult) * 0.7f)));
                        // float distance = Mathf.Abs((Vector3.Distance(currentVertex.position, closestPrototype.center) - ((closestPrototype.size * cellRadiusMult) * 0.7f)));

                        float distanceBase = Mathf.Abs((Vector3.Distance(currentVertex.position, closestPrototype.center) - ((closestPrototype.size * cellRadiusMult) * 0.8f)));
                        // float distanceMod = Mathf.Abs((Vector3.Distance(currentVertex.position, closestPrototype.center) - ((closestPrototype.size * cellRadiusMult) * 0.8f)) + distanceY);
                        float distanceMod = Mathf.Abs((Vector2.Distance(vertexPosXZ, cellPosXZ) - ((closestPrototype.size * cellRadiusMult) * 0.8f) + distanceY));

                        // if (distance < (closestPrototype.size * cellRadiusMult))
                        // {
                        //     currentVertex.isOnTheEdgeOftheGrid = true;
                        //     // currentVertex.position.y = closestPrototype.center.y;
                        //     // float slope = Mathf.Lerp(closestPrototype.center.y, currentVertex.baseNoiseHeight * distanceNormalized, distanceNormalized * smoothingFactor);

                        //     vertexGrid[x, z].position.y = avgY;
                        //     continue;
                        // }

                        // float distanceNormalized = distance / searchDistance;
                        float distanceNormalized = Mathf.Clamp01(distanceMod / searchDistance);

                        if (distanceNormalized < 0.4f)
                        {
                            // Debug.Log("distanceNormalized: " + distanceNormalized + ", distanceBase: " + distanceBase + ", distanceMod: " + distanceMod + ",  center.y: " + closestPrototype.center.y + ", currentVertex.y: " + currentVertex.position.y);
                            distanceNormalized *= 0.1f;
                        }


                        float totalWeight = 0f;
                        float weightedHeightSum = 0f;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                if (dx == 0 && dz == 0) continue;

                                int nx = x + dx;
                                int nz = z + dz;
                                if (nx < 0 || nx >= vertexGrid.GetLength(0) || nz < 0 || nz >= vertexGrid.GetLength(1)) continue;

                                TerrainVertex neighborVertex = vertexGrid[nx, nz];
                                // if (!neighborVertex.isOnTheEdgeOftheGrid) continue;

                                float neighborDistanceY = Mathf.Abs(currentVertex.baseNoiseHeight - neighborVertex.baseNoiseHeight);
                                float neighborDistance = Vector3.Distance(currentVertex.position, neighborVertex.position) + (neighborDistanceY);

                                float neighborWeight = Mathf.Exp(-neighborDistance * neighborDistance / (2f * smoothingSigma * smoothingSigma));

                                weightedHeightSum += neighborWeight * neighborVertex.baseNoiseHeight;
                                totalWeight += neighborWeight;
                            }
                        }
                        // float slope = Mathf.Lerp(avgY, weightedHeightSum / totalWeight, distanceNormalized * smoothingFactor);
                        // float slope = Mathf.Lerp(weightedHeightSum / totalWeight, currentVertex.baseNoiseHeight * distanceNormalized, distanceNormalized * smoothingFactor);


                        // float slope = avgY + (distanceY * distanceNormalized);//Mathf.Lerp(closestPrototype.center.y, currentVertex.baseNoiseHeight * distanceNormalized, distanceNormalized * smoothingFactor);
                        // float slope = Mathf.Lerp(closestPrototype.center.y, currentVertex.baseNoiseHeight * distanceNormalized, distanceNormalized * smoothingFactor);
                        // float slope = Mathf.Lerp(closestPrototype.center.y, weightedHeightSum / totalWeight, distanceNormalized * smoothingFactor);
                        // Debug.Log("distanceNormalized: " + distanceNormalized + ", dist: " + distance + ", slope: " + slope + ",  center.y: " + closestPrototype.center.y + ", currentVertex.y: " + currentVertex.position.y);

                        float distY = Mathf.Abs(currentVertex.baseNoiseHeight - (avgY));
                        // float distY = Mathf.Abs(currentVertex.baseNoiseHeight - (slope));

                        // float avgFinal = avgY * (1 - distanceNormalized);

                        float desiredElevation = avgY + ((distY * (distanceNormalized)));
                        // float desiredElevation = avgFinal + ((distY * (distanceNormalized)));

                        desiredElevation = Mathf.Lerp(desiredElevation, (weightedHeightSum / totalWeight), distanceNormalized * smoothingFactor);
                        // desiredElevation = Mathf.Lerp(desiredElevation, (weightedHeightSum / totalWeight), distanceNormalized * smoothingFactor);
                        // float distYB = Mathf.Abs(desiredElevation - (avgY));

                        // float offsetY;

                        float distanceNormalizedBase = Mathf.Clamp01(distanceBase / searchDistance);
                        if (distanceNormalizedBase > 0.25f)
                        {
                            distanceNormalizedBase = Mathf.Clamp01(distanceNormalizedBase * 2f);
                        }

                        // if (desiredElevation < 0)
                        // {
                        //     offsetY = Mathf.Abs(desiredElevation - (avgY)) * -1;
                        // }
                        // else
                        // {
                        //     offsetY = Mathf.Abs(desiredElevation - (avgY));
                        // }

                        float finalPosY = Mathf.Lerp(avgY, desiredElevation, distanceNormalizedBase); ;
                        if (vertexGrid[x, z].isOnTunnelCell && vertexGrid[x, z].tunnelCellRoofPosY > finalPosY)
                        {
                            finalPosY = vertexGrid[x, z].tunnelCellRoofPosY;
                        }

                        // vertexGrid[x, z].position.y = avgY + (offsetY * (distanceNormalizedBase));
                        vertexGrid[x, z].position.y = finalPosY;
                        // vertexGrid[x, z].position.y = avgFinal + ((distY * (distanceNormalized)));
                        // vertexGrid[x, z].position.y = slope + (distY * distanceNormalized);
                        vertexGrid[x, z].isOnTheEdgeOftheGrid = true;

                    }
                }
            }
        }

        // public static void SmoothGridEdgeVertexIndices(List<HexagonCellPrototype> allGroundEdgePrototypes, TerrainVertex[,] vertexGrid, float cellRadiusMult, float searchDistance = 36f, float smoothingFactor = 1f, float smoothingSigma = 0.5f)
        // {
        //     // const float smoothingFactor = 4f;
        //     // const float smoothingSigma = 0.5f;
        //     for (int x = 0; x < vertexGrid.GetLength(0); x++)
        //     {
        //         for (int z = 0; z < vertexGrid.GetLength(1); z++)
        //         {
        //             TerrainVertex currentVertex = vertexGrid[x, z];

        //             if (currentVertex.isInherited || currentVertex.isOnTheEdgeOftheGrid == false) continue;

        //             HexagonCellPrototype closestPrototype = HexagonCellPrototype.GetClosestPrototypeXYZ(allGroundEdgePrototypes, currentVertex.position);
        //             if (closestPrototype != null)
        //             {

        //                 vertexGrid[x, z].isOnTheEdgeOftheGrid = true;

        //             }
        //         }
        //     }
        // }


        public static void ReadjustVerticesAroundTunnelClusters(List<HexagonCellCluster> clusters, TerrainVertex[,] vertexGrid, int layerElevation, float minRadius = 2f, Transform transform = null)
        {
            if (vertexGrid != null)
            {
                AssignTunnelEntryVertices(clusters, vertexGrid, layerElevation, transform);
            }
        }

        public static (List<int>, List<Vector3>) GetVertexIndicesInTunnelEntries(List<HexagonCellCluster> clusters, Mesh mesh)
        {
            HashSet<int> indices = new HashSet<int>();
            List<Vector3> verts = new List<Vector3>();
            Vector3[] vertices = mesh.vertices;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector2 currVertexPosXZ = new Vector2(vertices[i].x, vertices[i].z);

                bool found = false;

                foreach (var cluster in clusters)
                {
                    if (found) break;

                    if (cluster.clusterType != CellClusterType.Tunnel) continue;

                    List<HexagonCellPrototype> tunnelEntrances = cluster.prototypes.FindAll(p => p.isTunnelStart && p.IsUnderGround());
                    foreach (var entry in tunnelEntrances)
                    {
                        Vector2 entryPosXZ = new Vector2(entry.center.x, entry.center.z);

                        if (Vector2.Distance(entryPosXZ, currVertexPosXZ) < entry.size * 1.05f)
                        {
                            indices.Add(i);
                            verts.Add(vertices[i]);
                            found = true;
                            break;
                        }
                    }
                }
            }

            return (indices.ToList(), verts);
        }

        public static void Assign_TunnelEntryVertices_V2(
            List<HexagonCellCluster> clusters,
            Dictionary<Vector2, TerrainVertex> globalTerrainVertexGridByCoordinate,
            int layerElevation,
            Transform transform = null
        )
        {
            foreach (HexagonCellCluster cluster in clusters)
            {
                if (cluster.clusterType != CellClusterType.Tunnel) continue;

                HexagonCellPrototype tunnelEntry = cluster.prototypes.Find(p => p.isTunnelGroundEntry);
                if (tunnelEntry == null)
                {
                    Debug.LogError("NO tunnelEntry found");
                    continue;
                }

                if (tunnelEntry.vertexList_V2 == null | tunnelEntry.vertexList_V2.Count == 0)
                {
                    Debug.LogError("NO vertices for tunnelEntry");
                    continue;
                }

                foreach (Vector2 index in tunnelEntry.vertexList_V2)
                {
                    Vector2 coord;
                    TerrainVertex vertex;

                    if (globalTerrainVertexGridByCoordinate.ContainsKey(index) == false)
                    {
                        coord = new Vector2(index.y, index.x);
                        Debug.LogError("globalTerrainVertexGridByCoordinate does not contain key: " + index + ", attemping the inverse: " + coord);
                        if (globalTerrainVertexGridByCoordinate.ContainsKey(coord) == false)
                        {
                            Debug.LogError("globalTerrainVertexGridByCoordinate does not contain key: " + coord);
                            continue;
                        }
                        Debug.LogError("inverse coord found: " + coord);
                        vertex = globalTerrainVertexGridByCoordinate[coord];
                    }
                    else
                    {
                        coord = index;
                        vertex = globalTerrainVertexGridByCoordinate[coord];
                    }

                    float vertexDistance = VectorUtil.DistanceXZ(tunnelEntry.center, vertex.position);
                    if (vertexDistance > tunnelEntry.size)
                    {
                        Debug.LogError("vertexDistance: " + vertexDistance);

                        coord = new Vector2(index.y, index.x);
                        vertex = globalTerrainVertexGridByCoordinate[coord];

                        vertexDistance = VectorUtil.DistanceXZ(tunnelEntry.center, vertex.position);
                        Debug.LogError("reversed cood vertexDistance: " + vertexDistance);
                    }
                    else
                    {
                        coord = index;
                    }

                    float maxEdgeDistance = 0.46f;

                    (bool isInBounds, Vector3 edgePointIfAny, float edgeDistance) = HexagonCellPrototype.IsPointWithinEdgeBounds_WithEdgePoint(vertex.position, tunnelEntry, maxEdgeDistance);
                    bool IsOnEdge = edgePointIfAny != Vector3.zero && edgeDistance > -1 && edgeDistance < maxEdgeDistance;

                    if (isInBounds == false) continue;

                    // Basement format
                    if (IsOnEdge == false)
                    {
                        float centerDistance = VectorUtil.DistanceXZ(tunnelEntry.center, vertex.position);
                        bool assigned = false;
                        if (centerDistance > -1f && centerDistance < (tunnelEntry.size * 0.45f))
                        {
                            vertex.markedForRemoval = true;
                            assigned = true;
                        }
                        if (edgeDistance > -1)
                        {
                            Vector3 closesCorner = VectorUtil.GetClosestPoint_XZ(tunnelEntry.cornerPoints, vertex.position);
                            vertex.position = closesCorner;
                            assigned = true;
                        }

                        if (assigned)
                        {
                            vertex.isOnTunnelGroundEntry = true;
                            vertex.isOnTunnelCell = true;
                            vertex.tunnelCellRoofPosY = (int)tunnelEntry.center.y;

                            globalTerrainVertexGridByCoordinate[coord] = vertex;
                        }
                    }
                    else
                    {
                        // vertex.position = edgePointIfAny;
                    }
                    // vertex.isOnTunnelGroundEntry = true;
                    // vertex.isOnTunnelCell = true;
                    // // vertex.tunnelCellRoofPosY = (int)tunnelEntry.center.y;
                    // // vertex.position.y = vertex.tunnelCellRoofPosY;

                    // globalTerrainVertexGridByCoordinate[coord] = vertex;
                }


                // foreach (var cell in cluster.prototypes)
                // {
                //     if (cell.isTunnel && !cell.isTunnelGroundEntry)
                //     {
                //         foreach (var index in cell.vertexList_V2)
                //         {
                //             TerrainVertex vertex = globalTerrainVertexGridByCoordinate[index];

                //             vertex.isOnTunnelCell = true;
                //             vertex.tunnelCellRoofPosY = (int)cell.center.y + layerElevation;
                //             // vertex.position.y = vertex.tunnelCellRoofPosY;

                //             globalTerrainVertexGridByCoordinate[index] = vertex;
                //         }
                //     }
                // }

            }
        }


        public static void AssignTunnelEntryVertices(List<HexagonCellCluster> clusters, TerrainVertex[,] vertexGrid, int layerElevation, Transform transform = null)
        {
            if (vertexGrid != null)
            {

                foreach (var cluster in clusters)
                {
                    if (cluster.clusterType != CellClusterType.Tunnel) continue;

                    // Get Vertices within distance of tunnel start
                    List<TerrainVertex> vertexList = new List<TerrainVertex>();
                    for (int x = 0; x < vertexGrid.GetLength(0); x++)
                    {
                        for (int z = 0; z < vertexGrid.GetLength(1); z++)
                        {
                            TerrainVertex currVertex = vertexGrid[x, z];
                            Vector2 currVertexPosXZ = new Vector2(currVertex.position.x, currVertex.position.z);

                            (HexagonCellPrototype closestCell, float closestDistance) = HexGridVertexUtil.GetHexagonCellProtoypeBoundsParentOfVertex(cluster.prototypes, currVertex, true, true, true, transform);
                            if (closestCell == null)
                            {
                                // Debug.LogError("NO closestCell found");
                                continue;
                            }

                            // Vector2 closestPosXZ = new Vector2(closestCell.center.x, closestCell.center.z);

                            float maxEdgeDistance = 0.46f;
                            (bool isInBounds, Vector3 edgePointIfAny, float edgeDistance) = HexagonCellPrototype.IsPointWithinEdgeBounds_WithEdgePoint(vertexGrid[x, z].position, closestCell, maxEdgeDistance);
                            bool IsOnEdge = edgePointIfAny != Vector3.zero && edgeDistance > -1 && edgeDistance < maxEdgeDistance;

                            // if (HexagonCellPrototype.IsPointWithinEdgeBounds(vertexGrid[x, z].position, closestCell) == false)
                            if (isInBounds == false)
                            {
                                continue;
                            }

                            if (closestCell.isTunnelGroundEntry)
                            {
                                if (closestCell.tunnelEntryType == TunnelEntryType.Cave)
                                {
                                    // List<int> sideCuts = new List<int>();

                                    // for (int side = 0; side < closestCell.neighborsBySide.Length; side++)
                                    // {
                                    //     HexagonCellPrototype neighbor = closestCell.neighborsBySide[side];
                                    //     if (neighbor != null && !neighbor.IsGround())
                                    //     {
                                    //         sideCuts.Add(side);
                                    //     }
                                    // }

                                    int density = 12;
                                    List<Vector3> sideCorners = HexagonCellPrototype.GetCornersOnSide(closestCell, 1);
                                    Vector3[] sideCornersInner = VectorUtil.DuplicateCornerPointsTowardsCenter(closestCell.center, sideCorners[0], sideCorners[1], 3f);

                                    List<Vector3> dottedEdgeBottomInner = VectorUtil.GenerateDottedLine(sideCornersInner.ToList(), density);

                                    (Vector3 closestEdgeBTM, float edgeDist2) = VectorUtil.GetClosestPoint_XZ_WithDistance(dottedEdgeBottomInner, vertexGrid[x, z].position);
                                    bool IsOnEdgeBTM = closestEdgeBTM != Vector3.positiveInfinity && edgeDist2 < 2f;

                                    if (IsOnEdgeBTM)
                                    {
                                        vertexGrid[x, z].markedForRemoval = true;
                                        vertexGrid[x, z].position = closestEdgeBTM;
                                    }
                                    else
                                    {

                                        if (edgeDist2 < 3f)
                                        {

                                            vertexGrid[x, z].markedIgnore = true;

                                            float edgeDistFromCenter = VectorUtil.DistanceXZ(closestCell.center, closestEdgeBTM);
                                            // Bottom line
                                            if (edgeDistFromCenter > closestDistance)
                                            {
                                                // vertexGrid[x, z].position = closestEdgeBTM;
                                            }
                                            else // Top line
                                            {
                                                List<Vector3> dottedEdgeBottom = VectorUtil.GenerateDottedLine(sideCorners, density);
                                                List<Vector3> dottedEdgeTop = VectorUtil.DuplicatePositionsToNewYPos(dottedEdgeBottom, layerElevation * 2);
                                                (Vector3 closestEdgeTop, float edgeDistTop) = VectorUtil.GetClosestPoint_XZ_WithDistance(dottedEdgeTop, vertexGrid[x, z].position);
                                                if (closestEdgeTop != Vector3.positiveInfinity)
                                                {
                                                    vertexGrid[x, z].position = closestEdgeTop;
                                                    vertexGrid[x, z].position.y += 3f;
                                                }
                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    // Basement
                                    if (IsOnEdge == false)
                                    // if (closestDistance < closestCell.size)
                                    {
                                        if (closestDistance < closestCell.size * 0.45f)
                                        {
                                            vertexGrid[x, z].markedForRemoval = true;
                                        }
                                        if (edgeDistance > -1)
                                        {
                                            Vector3 closesCorner = VectorUtil.GetClosestPoint_XZ(closestCell.cornerPoints, vertexGrid[x, z].position);

                                            vertexGrid[x, z].position = closesCorner;
                                        }
                                    }
                                    else
                                    {
                                        vertexGrid[x, z].position = edgePointIfAny;
                                    }
                                }

                                vertexGrid[x, z].isOnTunnelCell = true;
                                vertexGrid[x, z].tunnelCellRoofPosY = (int)closestCell.center.y; //+ layerElevation;
                                vertexGrid[x, z].position.y = vertexGrid[x, z].tunnelCellRoofPosY;
                            }
                            else
                            {
                                // HexagonCellPrototype groundCellOfStack = closestCell.IsGround() ? closestCell :  HexagonCellPrototype.GetGroundCellInLayerStack(closestCell);
                                // if (groundCellOfStack != null) {

                                // }

                                if (closestCell.isTunnelGroundEntry)
                                {
                                    vertexGrid[x, z].isOnTunnelGroundEntry = true;
                                }
                                vertexGrid[x, z].isOnTunnelCell = true;
                                vertexGrid[x, z].tunnelCellRoofPosY = (int)closestCell.center.y + layerElevation;
                                continue;
                            }
                        }
                    }
                }
            }
        }

    }
}