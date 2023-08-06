using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ProceduralBase;

namespace WFCSystem
{
    public class HexVertexNode
    {
        public HexVertexNode(Vector3 _center, float _size, Dictionary<Vector2, Vector3> _cornersByLookup)
        {
            Center = _center;
            CornersByLookup = _cornersByLookup;
            size = _size;
        }

        public Vector3 Center { get; private set; }
        public HashSet<Vector2> CornerLookups { get; private set; } = null;
        public Dictionary<Vector2, Vector3> CornersByLookup { get; private set; } = null;
        public Vector3 Lookup() => VectorUtil.PointLookupDefault(Center);
        public float size;

        public static void Draw(HexVertexNode node, float cornerSize = 0.2f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(node.Center, 0.3f);

            Gizmos.color = Color.yellow;
            VectorUtil.Draw_PointsInGizmos(node.CornersByLookup.Values.ToArray(), cornerSize);
        }

        public static Mesh Generate_Mesh(Dictionary<Vector2, HexVertexNode> nodesByLookup, Transform transform)
        {
            List<Mesh> nodeMeshes = new List<Mesh>();

            foreach (var node in nodesByLookup.Values)
            {
                Mesh nodeMesh = Generate_Mesh(node, transform);
                nodeMeshes.Add(nodeMesh);
            }
            return MeshUtil.GenerateMeshFromVertexSurfaces(nodeMeshes);
        }

        public static Mesh Generate_Mesh(HexVertexNode node, Transform transform)
        {
            List<Vector3> allPoints = new List<Vector3>();
            allPoints.AddRange(node.CornersByLookup.Values.ToList());
            allPoints.Add(node.Center);

            Vector3[] new_vertices = allPoints.ToArray();
            int vetexLength = new_vertices.Length;

            Mesh surfaceMesh = new Mesh();
            surfaceMesh.vertices = VectorUtil.InversePointsToLocal_ToArray(new_vertices, transform);

            int[] triangles = GenerateTriangles(vetexLength, vetexLength - 1);
            surfaceMesh.triangles = triangles;

            MeshUtil.ReverseNormals(surfaceMesh);
            MeshUtil.ReverseTriangles(surfaceMesh); // Updated: Reverse the triangles as well

            // Set the UVs to the mesh (you can customize this based on your requirements)
            Vector2[] uvs = new Vector2[vetexLength];
            for (int j = 0; j < vetexLength; j++)
            {
                uvs[j] = new Vector2(new_vertices[j].x, new_vertices[j].z); // Use x and y as UV coordinates
            }

            surfaceMesh.uv = uvs;
            // Recalculate normals and bounds for the surface mesh
            surfaceMesh.RecalculateNormals();
            surfaceMesh.RecalculateBounds();

            return surfaceMesh;
        }

        public static int[] GenerateTriangles(int vertexCount, int centerVertexIndex)
        {
            int[] triangles = new int[(vertexCount - 1) * 3];
            int index = 0;

            // Connect triangles to the center vertex
            for (int i = 0; i < vertexCount - 1; i++)
            {
                triangles[index++] = centerVertexIndex;
                triangles[index++] = i;
                triangles[index++] = (i + 1) % (vertexCount - 1);
            }

            return triangles;
        }
    }


    public static class HexVertexUtil
    {
        public static Dictionary<Vector2, HexVertexNode> Generate_HexMVertexGrid(
            Vector3 initialCenter,
            float smallestSize,
            int gridRadius,
            Transform transform,
            HexCellSizes cellSize,
            Dictionary<Vector2, Vector3> cellCenters_ByLookup,
            Dictionary<Vector2, Vector3> allCenterPointsAdded,
            Dictionary<Vector2, Vector3> pathCenters,
            List<LayeredNoiseOption> layerdNoises_terrain,
            float terrainHeight,
            float groundCellInfluenceRadiusMult = 1.3f,
            float bufferZoneLerpMult = 0.5f,
            int cellLayerOffset = 2,
            int treeStep = 4,
            int seaLevel = 0
        )
        {
            Dictionary<Vector2, HexVertexNode> hexVertexNodeLookups = new Dictionary<Vector2, HexVertexNode>();
            Dictionary<float, Dictionary<Vector2, Vector3>> centerPointsByLookup_BySize = new Dictionary<float, Dictionary<Vector2, Vector3>>();
            List<Vector3> divideCenterPoints = new List<Vector3>();
            List<Vector3> allNewCenterPoints = new List<Vector3>();
            Vector3[] radiusCorners = HexCoreUtil.GenerateHexagonPoints(initialCenter, gridRadius);

            Dictionary<Vector2, Vector3> allCornerLookups = new Dictionary<Vector2, Vector3>();
            float prevStepSize = gridRadius;
            float currentStepSize = gridRadius;

            while (smallestSize < currentStepSize)
            {
                List<Vector3> newCenterPoints = new List<Vector3>();
                currentStepSize = prevStepSize / 3;

                if (divideCenterPoints.Count == 0)
                {
                    newCenterPoints = Generate_HexCenterPoints_X13(initialCenter, currentStepSize);
                }
                else
                {
                    foreach (Vector3 centerPoint in divideCenterPoints)
                    {
                        newCenterPoints.AddRange(Generate_HexCenterPoints_X13(centerPoint, currentStepSize));
                    }
                }

                // Debug.Log("GenerateHexGrid - allSoFar: " + allSoFar.Count + ", currentStepSize: " + currentStepSize);
                allNewCenterPoints.AddRange(newCenterPoints);
                centerPointsByLookup_BySize.Add(currentStepSize, new Dictionary<Vector2, Vector3>());

                foreach (Vector3 point in allNewCenterPoints)
                {
                    if (VectorUtil.IsPointWithinPolygon(point, radiusCorners) == false) continue;

                    Vector2 lookup = HexCoreUtil.Calculate_CenterLookup(point, currentStepSize);
                    if (centerPointsByLookup_BySize[currentStepSize].ContainsKey(lookup) == false)
                    {
                        centerPointsByLookup_BySize[currentStepSize].Add(lookup, point);
                        if (smallestSize == currentStepSize)
                        {

                            // Dictionary<Vector2, Vector3> cornersByLookup = Generate_HexagonCornerPoints(point, smallestSize, allCornerLookups);
                            // HexVertexNode hexVertexNode = new HexVertexNode(point, currentStepSize, cornersByLookup);
                            // hexVertexNodeLookups.Add(lookup, hexVertexNode);

                            HexVertexNode hexVertexNode = Generate_HexNode_WithCornerPoints(
                                point,

                                smallestSize,
                                hexVertexNodeLookups,
                                allCornerLookups,
                                cellCenters_ByLookup,
                                allCenterPointsAdded,
                                pathCenters,
                                layerdNoises_terrain,
                                transform,
                                terrainHeight,
                                groundCellInfluenceRadiusMult = 1.3f,
                                bufferZoneLerpMult = 0.5f,
                                cellLayerOffset,
                                treeStep,
                                seaLevel
                            );
                            hexVertexNodeLookups.Add(lookup, hexVertexNode);

                            List<Vector3> points = Generate_HexCenterPoints_X6(point, currentStepSize);
                            foreach (var item in points)
                            {
                                if (VectorUtil.IsPointWithinPolygon(item, radiusCorners) == false) continue;

                                Vector2 lookupB = HexCoreUtil.Calculate_CenterLookup(item, currentStepSize);
                                if (centerPointsByLookup_BySize[currentStepSize].ContainsKey(lookupB) == false) centerPointsByLookup_BySize[currentStepSize].Add(lookupB, item);

                                if (hexVertexNodeLookups.ContainsKey(lookupB) == false)
                                {
                                    // Dictionary<Vector2, Vector3> cornersByLookupB = Generate_HexagonCornerPoints(item, smallestSize, allCornerLookups);
                                    // HexVertexNode hexVertexNodeB = new HexVertexNode(item, currentStepSize, cornersByLookupB);
                                    // hexVertexNodeLookups.Add(lookupB, hexVertexNodeB);

                                    HexVertexNode hexVertexNodeB = Generate_HexNode_WithCornerPoints(
                                        item,

                                        smallestSize,
                                        hexVertexNodeLookups,
                                        allCornerLookups,
                                        cellCenters_ByLookup,
                                        allCenterPointsAdded,
                                        pathCenters,
                                        layerdNoises_terrain,
                                        transform,
                                        terrainHeight,
                                        groundCellInfluenceRadiusMult = 1.3f,
                                        bufferZoneLerpMult = 0.5f,
                                        cellLayerOffset,
                                        treeStep,
                                        seaLevel
                                    );

                                    hexVertexNodeLookups.Add(lookupB, hexVertexNodeB);
                                }
                            }
                        }
                    }
                }

                prevStepSize = currentStepSize;

                if (currentStepSize <= smallestSize)
                {
                    break;
                }
                else
                {
                    divideCenterPoints.Clear();
                    divideCenterPoints.AddRange(newCenterPoints);
                }
            }
            return hexVertexNodeLookups;
        }


        public static Dictionary<Vector2, HexVertexNode> Generate_HexMVertexGrid(
            Vector3 initialCenter,
            float smallestSize,
            int gridRadius
        // Dictionary<Vector2, Vector3> allCornerLookups
        )
        {
            Dictionary<Vector2, HexVertexNode> hexVertexNodeLookups = new Dictionary<Vector2, HexVertexNode>();
            Dictionary<float, Dictionary<Vector2, Vector3>> centerPointsByLookup_BySize = new Dictionary<float, Dictionary<Vector2, Vector3>>();
            List<Vector3> divideCenterPoints = new List<Vector3>();
            List<Vector3> allNewCenterPoints = new List<Vector3>();
            Vector3[] radiusCorners = HexCoreUtil.GenerateHexagonPoints(initialCenter, gridRadius);

            Dictionary<Vector2, Vector3> allCornerLookups = new Dictionary<Vector2, Vector3>();
            float prevStepSize = gridRadius;
            float currentStepSize = gridRadius;

            while (smallestSize < currentStepSize)
            {
                List<Vector3> newCenterPoints = new List<Vector3>();
                currentStepSize = prevStepSize / 3;

                if (divideCenterPoints.Count == 0)
                {
                    newCenterPoints = Generate_HexCenterPoints_X13(initialCenter, currentStepSize);
                }
                else
                {
                    foreach (Vector3 centerPoint in divideCenterPoints)
                    {
                        newCenterPoints.AddRange(Generate_HexCenterPoints_X13(centerPoint, currentStepSize));
                    }
                }

                // Debug.Log("GenerateHexGrid - allSoFar: " + allSoFar.Count + ", currentStepSize: " + currentStepSize);
                allNewCenterPoints.AddRange(newCenterPoints);
                centerPointsByLookup_BySize.Add(currentStepSize, new Dictionary<Vector2, Vector3>());

                foreach (Vector3 point in allNewCenterPoints)
                {
                    if (VectorUtil.IsPointWithinPolygon(point, radiusCorners) == false) continue;

                    Vector2 lookup = HexCoreUtil.Calculate_CenterLookup(point, currentStepSize);
                    if (centerPointsByLookup_BySize[currentStepSize].ContainsKey(lookup) == false)
                    {
                        centerPointsByLookup_BySize[currentStepSize].Add(lookup, point);
                        if (smallestSize == currentStepSize)
                        {
                            Dictionary<Vector2, Vector3> cornersByLookup = Generate_HexagonCornerPoints(point, smallestSize, allCornerLookups);
                            HexVertexNode hexVertexNode = new HexVertexNode(point, currentStepSize, cornersByLookup);
                            hexVertexNodeLookups.Add(lookup, hexVertexNode);

                            List<Vector3> points = Generate_HexCenterPoints_X6(point, currentStepSize);
                            foreach (var item in points)
                            {
                                if (VectorUtil.IsPointWithinPolygon(item, radiusCorners) == false) continue;

                                Vector2 lookupB = HexCoreUtil.Calculate_CenterLookup(item, currentStepSize);
                                if (centerPointsByLookup_BySize[currentStepSize].ContainsKey(lookupB) == false) centerPointsByLookup_BySize[currentStepSize].Add(lookupB, item);

                                if (hexVertexNodeLookups.ContainsKey(lookupB) == false)
                                {
                                    Dictionary<Vector2, Vector3> cornersByLookupB = Generate_HexagonCornerPoints(item, smallestSize, allCornerLookups);
                                    HexVertexNode hexVertexNodeB = new HexVertexNode(item, currentStepSize, cornersByLookupB);
                                    hexVertexNodeLookups.Add(lookupB, hexVertexNodeB);
                                }
                            }
                        }
                    }
                }

                prevStepSize = currentStepSize;

                if (currentStepSize <= smallestSize)
                {
                    break;
                }
                else
                {
                    divideCenterPoints.Clear();
                    divideCenterPoints.AddRange(newCenterPoints);
                }
            }
            return hexVertexNodeLookups;
        }
        // public static Dictionary<float, Dictionary<Vector2, Vector3>> Generate_HexMVertexGrid(
        //     Vector3 initialCenter,
        //     float smallestSize,
        //     int gridRadius,
        //     bool logResults = false
        // )
        // {
        //     Dictionary<float, Dictionary<Vector2, Vector3>> centerPointsByLookup_BySize = new Dictionary<float, Dictionary<Vector2, Vector3>>();
        //     List<Vector3> divideCenterPoints = new List<Vector3>();
        //     List<Vector3> allNewCenterPoints = new List<Vector3>();
        //     Vector3[] radiusCorners = HexCoreUtil.GenerateHexagonPoints(initialCenter, gridRadius);

        //     Dictionary<Vector2, Vector3> allCornerLookups = new Dictionary<Vector2, Vector3>();
        //     float prevStepSize = gridRadius;
        //     float currentStepSize = gridRadius;

        //     while (smallestSize < currentStepSize)
        //     {
        //         List<Vector3> newCenterPoints = new List<Vector3>();
        //         currentStepSize = prevStepSize / 3;

        //         if (divideCenterPoints.Count == 0)
        //         {
        //             newCenterPoints = Generate_HexCenterPoints_X13(initialCenter, currentStepSize);
        //         }
        //         else
        //         {
        //             foreach (Vector3 centerPoint in divideCenterPoints)
        //             {
        //                 newCenterPoints.AddRange(Generate_HexCenterPoints_X13(centerPoint, currentStepSize));
        //             }
        //         }

        //         // Debug.Log("GenerateHexGrid - allSoFar: " + allSoFar.Count + ", currentStepSize: " + currentStepSize);
        //         allNewCenterPoints.AddRange(newCenterPoints);
        //         centerPointsByLookup_BySize.Add(currentStepSize, new Dictionary<Vector2, Vector3>());

        //         foreach (Vector3 point in allNewCenterPoints)
        //         {
        //             if (VectorUtil.IsPointWithinPolygon(point, radiusCorners) == false) continue;

        //             Vector2 lookup = HexCoreUtil.Calculate_CenterLookup(point, currentStepSize);
        //             if (centerPointsByLookup_BySize[currentStepSize].ContainsKey(lookup) == false)
        //             {
        //                 centerPointsByLookup_BySize[currentStepSize].Add(lookup, point);
        //                 if (smallestSize == currentStepSize)
        //                 {
        //                     Dictionary<Vector2, Vector3> cornersByLookup = Generate_HexagonCornerPoints(point, smallestSize, allCornerLookups);
        //                     List<Vector3> points = Generate_HexCenterPoints_X6(point, currentStepSize);
        //                     foreach (var item in points)
        //                     {
        //                         if (VectorUtil.IsPointWithinPolygon(item, radiusCorners) == false) continue;

        //                         Vector2 lookupB = HexCoreUtil.Calculate_CenterLookup(item, currentStepSize);
        //                         if (centerPointsByLookup_BySize[currentStepSize].ContainsKey(lookupB) == false) centerPointsByLookup_BySize[currentStepSize].Add(lookupB, item);
        //                     }
        //                 }
        //             }
        //         }

        //         prevStepSize = currentStepSize;

        //         if (currentStepSize <= smallestSize)
        //         {
        //             break;
        //         }
        //         else
        //         {
        //             divideCenterPoints.Clear();
        //             divideCenterPoints.AddRange(newCenterPoints);
        //         }
        //     }
        //     return centerPointsByLookup_BySize;
        // }


        public static HexVertexNode Generate_HexNode_WithCornerPoints(
            Vector3 center,
            float radius,
            Dictionary<Vector2, HexVertexNode> hexVertexNodeLookups,
            Dictionary<Vector2, Vector3> allCornerLookups,
            Dictionary<Vector2, Vector3> cellCenters_ByLookup,
            Dictionary<Vector2, Vector3> allCenterPointsAdded,
            Dictionary<Vector2, Vector3> pathCenters,
            List<LayeredNoiseOption> layerdNoises_terrain,
            Transform transform,
            float terrainHeight,
            float groundCellInfluenceRadiusMult = 1.3f,
            float bufferZoneLerpMult = 0.5f,
            int cellLayerOffset = 2,
            int treeStep = 4,
            int seaLevel = 0
        )
        {


            Dictionary<Vector2, Vector3> cornersByLookup = new Dictionary<Vector2, Vector3>();

            for (int i = 0; i < 6; i++)
            {
                float angle = 60f * i;
                float x = center.x + radius * Mathf.Cos(Mathf.Deg2Rad * angle);
                float z = center.z + radius * Mathf.Sin(Mathf.Deg2Rad * angle);
                Vector3 new_point = new Vector3(x, center.y, z);
                Vector2 lookup = VectorUtil.PointLookupDefault_X2(new_point);

                if (allCornerLookups.ContainsKey(lookup) == false)
                {
                    allCornerLookups.Add(lookup, new_point);
                }

                allCornerLookups[lookup] = Generate_TerrainFoundationVertices(
                     allCornerLookups[lookup],
                     transform,
                     hexVertexNodeLookups,
                     HexCellSizes.X_12,
                     cellCenters_ByLookup,
                     allCenterPointsAdded,
                     pathCenters,
                     layerdNoises_terrain,
                     terrainHeight,
                     groundCellInfluenceRadiusMult,
                     bufferZoneLerpMult,
                     2
                 );


                cornersByLookup.Add(lookup, allCornerLookups[lookup]);
            }

            Vector3 new_center = Generate_TerrainFoundationVertices(
                      center,
                      transform,
                      hexVertexNodeLookups,
                      HexCellSizes.X_12,
                      cellCenters_ByLookup,
                      allCenterPointsAdded,
                      pathCenters,
                      layerdNoises_terrain,
                      terrainHeight,
                      groundCellInfluenceRadiusMult,
                      bufferZoneLerpMult,
                      2
                  );
            HexVertexNode hexVertexNode = new HexVertexNode(new_center, radius, cornersByLookup);
            return hexVertexNode;
        }

        public static Dictionary<Vector2, Vector3> Generate_HexagonCornerPoints(Vector3 center, float radius, Dictionary<Vector2, Vector3> allCornerLookups = null)
        {
            Dictionary<Vector2, Vector3> cornersByLookup = new Dictionary<Vector2, Vector3>();
            bool useAllCornerLookups = allCornerLookups != null;
            for (int i = 0; i < 6; i++)
            {
                float angle = 60f * i;
                float x = center.x + radius * Mathf.Cos(Mathf.Deg2Rad * angle);
                float z = center.z + radius * Mathf.Sin(Mathf.Deg2Rad * angle);
                Vector3 new_point = new Vector3(x, center.y, z);
                Vector2 lookup = VectorUtil.PointLookupDefault_X2(new_point);
                if (useAllCornerLookups)
                {
                    if (allCornerLookups.ContainsKey(lookup) == false) allCornerLookups.Add(lookup, new_point);
                    cornersByLookup.Add(lookup, allCornerLookups[lookup]);
                }
                else cornersByLookup.Add(lookup, new_point);
            }
            return cornersByLookup;
        }

        public static List<Vector3> Generate_HexCenterPoints_X6(Vector3 center, float size)
        {
            Vector3[] cornerPoints = HexCoreUtil.GenerateHexagonPoints(center, size);
            List<Vector3> results = new List<Vector3>();
            for (int i = 0; i < 6; i++)
            {
                Vector3 sidePoint = Vector3.Lerp(cornerPoints[i], cornerPoints[(i + 1) % 6], 0.5f);
                Vector3 direction = (sidePoint - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));

                sidePoint = center + direction * (edgeDistance * 2f);

                results.Add(sidePoint);
            }
            // Debug.Log("total points: " + results.Count);
            return results;
        }

        public static List<Vector3> Generate_HexCenterPoints_X13(Vector3 center, float size)
        {
            Vector3[] cornerPoints = HexCoreUtil.GenerateHexagonPoints(center, size);
            List<Vector3> results = new List<Vector3>() {
                center
            };
            for (int i = 0; i < 6; i++)
            {
                // Get Side
                Vector3 sidePoint = Vector3.Lerp(cornerPoints[i], cornerPoints[(i + 1) % 6], 0.5f);
                Vector3 direction = (sidePoint - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));

                sidePoint = center + direction * (edgeDistance * 2f);

                results.Add(sidePoint);

                // Get Corner
                float angle = 60f * i;
                float x = center.x + size * Mathf.Cos(Mathf.Deg2Rad * angle);
                float z = center.z + size * Mathf.Sin(Mathf.Deg2Rad * angle);

                Vector3 cornerPoint = new Vector3(x, center.y, z);
                direction = (cornerPoint - center).normalized;
                edgeDistance = Vector2.Distance(new Vector2(cornerPoint.x, cornerPoint.z), new Vector2(center.x, center.z));

                cornerPoint = center + direction * (edgeDistance * 3f);

                results.Add(cornerPoint);
            }
            // Debug.Log("total points: " + results.Count);
            return results;
        }


        public static Vector3 Generate_TerrainFoundationVertices(
            Vector3 position,
            Transform transform,
            Dictionary<Vector2, HexVertexNode> nodesByLookup,
            HexCellSizes cellSize,
            Dictionary<Vector2, Vector3> cellCenters_ByLookup,
            Dictionary<Vector2, Vector3> allCenterPointsAdded,
            Dictionary<Vector2, Vector3> pathCenters,
            List<LayeredNoiseOption> layerdNoises_terrain,
            float terrainHeight,
            float groundCellInfluenceRadiusMult = 1.3f,
            float bufferZoneLerpMult = 0.5f,
            int cellLayerOffset = 2,
            int treeStep = 4,
            int seaLevel = 0
        )
        {
            int worldSpaceSize = (int)HexCellSizes.Worldspace;
            int _cellSize = (int)cellSize;
            Vector3 currentTrackPos = Vector3.zero;
            Vector2 closest_subCellTerraformLookup = Vector2.positiveInfinity;
            int checkingLayer = int.MaxValue;
            float noiseBias = 0.5f;
            float previousGroundHeight = float.MaxValue;

            HashSet<CellStatus> includeCellStatusList = new HashSet<CellStatus>() {
                CellStatus.FlatGround
            };


            // position = transform.TransformPoint(position);
            Vector3 worldCoord = transform.TransformVector(position);
            Vector3 rounded = VectorUtil.RoundVector3To1Decimal(position);
            Vector2 aproximateCoord = new Vector2(rounded.x, rounded.z);
            Vector2Int noiseCoordinate = new Vector2Int((int)worldCoord.x, (int)worldCoord.z);

            float baseNoiseHeight = 0;
            bool terraformed = false;
            bool markedForRemoval = false;
            bool updateBufferCell = false;

            // Evaluate nearest subcell
            (Vector2 nearestLookup, float nearestDist) = LocationFoundationGen.GetCloseest_HexLookupInDictionary_withDistance(
                position,
                _cellSize,
                cellCenters_ByLookup
            // allCenterPointsAdded
            );

            if (nearestLookup != null && nearestLookup != Vector2.positiveInfinity && nearestDist != float.MaxValue)
            {
                // Debug.Log("nearestLookup: " + nearestLookup + ",  nearestDist: " + nearestDist);
                bool isPath = false; // cellCenters_ByLookup.ContainsKey(nearestLookup) == false;

                Vector3 nearestHexCenter = isPath ? allCenterPointsAdded[nearestLookup] : cellCenters_ByLookup[nearestLookup];

                // float cellBaseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)nearestHexCenter.x, (int)nearestHexCenter.z, terrainHeight, layerdNoises_terrain);
                float cellNoiseHeight = nearestHexCenter.y;

                if (isPath == false) previousGroundHeight = cellNoiseHeight;

                // bool isPath = false; //nearestGroundCell.IsPath();  // false; //hNoiseValue > locationNoise_pathNoiseMin);

                if (nearestDist < (_cellSize * groundCellInfluenceRadiusMult) && !isPath)
                {
                    // baseNoiseHeight = UtilityHelpers.RoundHeightToNearestElevation(cellNoiseHeight, cellLayerElevation);
                    baseNoiseHeight = cellNoiseHeight;
                    terraformed = true;
                }
                else
                {
                    // Debug.Log("nearestLookup: " + nearestLookup + ",  nearestDist: " + nearestDist);
                    Vector3 closestHexCenter_X4 = HexCoreUtil.Calculate_ClosestHexCenter_V2(position, 4);
                    float hexNoiseX4 = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)closestHexCenter_X4.x, (int)closestHexCenter_X4.z, terrainHeight, layerdNoises_terrain);
                    baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate(noiseCoordinate.x, noiseCoordinate.y, terrainHeight, layerdNoises_terrain);
                    baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, hexNoiseX4, 0.5f);

                    if (nearestDist < _cellSize * 6.6f)
                    {
                        baseNoiseHeight += transform.position.y;

                        // if (baseNoiseHeight < cellNoiseHeight) baseNoiseHeight += noiseBias;
                        // else if (baseNoiseHeight > cellNoiseHeight) baseNoiseHeight -= noiseBias;

                        // float outterEdgeMult = (groundCellInfluenceRadiusMult + 0.33f);
                        // if (nearestDist < (_cellSize * outterEdgeMult))
                        // {
                        //     baseNoiseHeight = Mathf.Clamp(baseNoiseHeight, cellNoiseHeight - 0.9f, cellNoiseHeight + 0.9f);
                        // }
                        // else baseNoiseHeight = UtilityHelpers.RoundToNearestStep(baseNoiseHeight, 0.45f);

                        // float distRadiusMod = 4f;
                        // float distMult = (1.01f - Mathf.Clamp01(nearestDist / (_cellSize * distRadiusMod)));
                        // baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, cellNoiseHeight, distMult);

                        // float roundedValue = UtilityHelpers.RoundHeightToNearestElevation(baseNoiseHeight, cellLayerOffset);
                        // baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, roundedValue, distMult);
                        // float roundedValue = Mathf.Lerp(baseNoiseHeight, cellNoiseHeight, distMult);
                        // baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, roundedValue, 0.2f);

                        // baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, cellNoiseHeight, distMult);

                        baseNoiseHeight = Mathf.Lerp(cellNoiseHeight, baseNoiseHeight, 0.99f);
                        // int lerps = 1;
                        // for (int i = 0; i < lerps; i++)
                        // {
                        // baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, cellNoiseHeight, bufferZoneLerpMult);
                        // }

                        (Vector2 nearestPathLookup, float nearestPathDist) = LocationFoundationGen.GetCloseest_HexLookupInDictionary_withDistance(
                            position,
                            4,
                            pathCenters
                        );

                        isPath = (
                            nearestPathLookup != Vector2.positiveInfinity
                            && nearestPathDist != float.MaxValue
                            && (nearestPathDist < (4 * 4.0f))
                        );

                        if (isPath)
                        {

                            Vector3 pathCenter = pathCenters[nearestPathLookup];
                            // float pathNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)pathCenter.x, (int)pathCenter.z, terrainHeight, layerdNoises_terrain);
                            // pathNoiseHeight = Mathf.Lerp(pathNoiseHeight, cellNoiseHeight, 0.3f);
                            float pathNoiseHeight = pathCenters[nearestPathLookup].y;

                            if (nearestPathDist < (4 * 0.8f))
                            {
                                // baseNoiseHeight = pathNoiseHeight;
                                baseNoiseHeight = Mathf.Lerp(pathNoiseHeight, baseNoiseHeight, 0.6f);

                                terraformed = true;
                            }
                            else
                            {
                                pathNoiseHeight = Mathf.Lerp(cellNoiseHeight, pathNoiseHeight, bufferZoneLerpMult);
                                baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, pathNoiseHeight, bufferZoneLerpMult);
                            }
                            terraformed = true;
                        }
                        // else
                        // {
                        //     baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, cellNoiseHeight, bufferZoneLerpMult);
                        // }

                    }
                    else
                    {

                        baseNoiseHeight += transform.position.y;

                        if (baseNoiseHeight < cellNoiseHeight) baseNoiseHeight += noiseBias;
                        else if (baseNoiseHeight > cellNoiseHeight) baseNoiseHeight -= noiseBias;

                        baseNoiseHeight = UtilityHelpers.RoundToNearestStep(baseNoiseHeight, 0.4f);

                        baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, cellNoiseHeight, 0.9f);
                    }
                }
            }
            else
            {
                // if (worldspaceBaseNoiseHeight != float.MaxValue)
                // {
                baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate(noiseCoordinate.x, noiseCoordinate.y, terrainHeight, layerdNoises_terrain);

                // if (baseNoiseHeight < worldspaceBaseNoiseHeight) baseNoiseHeight += 0.3f;
                // else if (baseNoiseHeight > worldspaceBaseNoiseHeight) baseNoiseHeight -= 0.3f;

                baseNoiseHeight += transform.position.y;

                baseNoiseHeight = UtilityHelpers.RoundToNearestStep(baseNoiseHeight, 0.3f);
                // }
            }

            // terraformed = true;


            if (!terraformed)
            {
                baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate(noiseCoordinate.x, noiseCoordinate.y, terrainHeight, layerdNoises_terrain);
                baseNoiseHeight += transform.position.y;
            }

            position.y = baseNoiseHeight;

            return position;

            // Create the TerrainVertex object
            // TerrainVertex vertex = new TerrainVertex()
            // {
            //     noiseCoordinate = noiseCoordinate,
            //     aproximateCoord = aproximateCoord,
            //     position = position,
            //     // index_X = x,
            //     // index_Z = z,
            //     type = VertexType.Generic,
            //     markedForRemoval = markedForRemoval,
            //     isInHexBounds = false,

            //     worldspaceOwnerCoordinate = Vector2.positiveInfinity,
            //     parallelWorldspaceOwnerCoordinate = Vector2.positiveInfinity,
            //     parallelVertexIndex_X = -1,
            //     parallelVertexIndex_Z = -1,
            // };

            // Add the vertex to the grid
            // gridPoints[aproximateCoord] = vertex;
            // gridLookups[x, z] = aproximateCoord;

        }


    }
}