using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;

namespace WFCSystem
{
    public static class HexCoreUtil
    {
        public static int Calculate_CellLayer(Vector3 position, int layerOffset) => Calculate_CellLayer(position.y, layerOffset);

        public static int Calculate_CellLayer(SurfaceBlock block, int layerOffset)
        {
            int bottomLayer = Calculate_CellLayer(block.BlockBottom_Top().x, layerOffset);
            int topLayer = Calculate_CellLayer(block.BlockBottom_Top().y, layerOffset);
            if (bottomLayer != topLayer)
            {
                return Calculate_CellLayer(block.Position, layerOffset);
            }
            else return bottomLayer;
        }
        public static int Calculate_CellLayer(float elevation, int layerOffset)
        {
            int layer = Calculate_CellSnapLayer(layerOffset, elevation);
            int layerStartY = (layer * layerOffset);
            int layerEndY = layerStartY + layerOffset;

            if (elevation < layerStartY) return layer - 1;
            if (elevation > layerEndY) return layer + 1;
            return layer;
        }

        public static int Calculate_CellSnapLayer(float layerOffset, float currentElevation) => Mathf.FloorToInt((int)currentElevation / (int)layerOffset);
        public static int Calculate_CellSnapElevation(float layerOffset, float currentElevation) => (int)(layerOffset * Mathf.FloorToInt((int)currentElevation / (int)layerOffset));
        public static int[] Calculate_CellLayerNeighbors(Vector3 center, int layerOffset)
        {
            int currentLayer = Calculate_CellLayer(center.y, layerOffset);
            int[] layerLookups = new int[2] {
                (currentLayer - 1), //BTM
                (currentLayer + 1), //TOP
            };
            return layerLookups;
        }

        public static (Vector2, int) Calculate_NearestHexCellLookupData(Vector3 position, HexCellSizes hexSize, int layerOffset)
        {
            Vector3 hexCenter = HexCoreUtil.Calculate_ClosestHexCenter_V2(position, (int)hexSize);
            Vector2 lookup = HexCoreUtil.Calculate_CenterLookup(hexCenter, (int)hexSize);
            return (lookup, (int)UtilityHelpers.RoundHeightToNearestElevation(position.y, layerOffset));
        }

        public static Vector3 GetRandomCellCenterPointWithinRadius(Vector3 center, int radius, int snapCellSize = 4)
        {
            Vector3 position = VectorUtil.GetRandomPointInCircle(radius, center);
            return Calculate_ClosestHexCenter_V2(position, snapCellSize);
        }

        public static CellStatus Calculate_CellStatusFromNoise(Vector3 centerPosition, float noiseValue, int cellLayerOffset, float seaLevel = 0)
        {
            if (noiseValue >= centerPosition.y && noiseValue < (centerPosition.y + cellLayerOffset)) return CellStatus.GenericGround;

            if (noiseValue > (centerPosition.y + cellLayerOffset)) return CellStatus.UnderGround;

            return (centerPosition.y < seaLevel) ? CellStatus.Underwater : CellStatus.AboveGround;
        }
        public static void Assign_CellStatusFromNoise(HexagonCellPrototype cell, List<LayeredNoiseOption> layeredNoiseOptions, int cellLayerOffset, float terrainHeight, float globalElevation, float seaLevel = 0)
        {
            float[] elevationData = HexCoreUtil.Calculate_ElevationDataFromNoise(cell, layeredNoiseOptions, cellLayerOffset, terrainHeight, globalElevation, seaLevel);
            // float maxSteepness = elevationData[0];
            float avgElevation = elevationData[1];
            // float hightestY = elevationData[2];
            // float lowestY = elevationData[3];

            if (avgElevation >= cell.center.y && avgElevation < cell.center.y + cellLayerOffset)
            {
                cell.SetToGround(false);
            }
            else if (avgElevation > cell.center.y + cellLayerOffset)
            {
                cell.SetCellStatus(CellStatus.UnderGround);
            }
            else cell.SetCellStatus((cell.center.y < seaLevel) ? CellStatus.Underwater : CellStatus.AboveGround);
        }

        public static float[] Calculate_ElevationDataFromNoise(HexagonCellPrototype cell, List<LayeredNoiseOption> layeredNoiseOptions, int cellLayerOffset, float terrainHeight, float globalElevation, float seaLevel = 0)
        {
            int currIX = 0;
            float lowestY = float.MaxValue;
            float hightestY = float.MinValue;

            List<Vector3> checkPointsOfCell = new List<Vector3>() {
                cell.center
            };
            checkPointsOfCell.AddRange(cell.cornerPoints);
            float[] elevations = new float[checkPointsOfCell.Count];
            foreach (Vector3 point in checkPointsOfCell)
            {
                float elevationY = globalElevation + LayerdNoise.Calculate_NoiseHeightForCoordinate((int)point.x, (int)point.z, terrainHeight, layeredNoiseOptions);
                if (lowestY > elevationY) lowestY = elevationY;
                if (hightestY < elevationY) hightestY = elevationY;

                currIX++;
            }

            float avgElevation = UtilityHelpers.CalculateAverageOfArray(elevations);
            float maxSteepness = 0f;
            foreach (float elevation in elevations)
            {
                float steepness = Mathf.Abs(elevation - avgElevation);
                if (steepness > maxSteepness) maxSteepness = steepness;
            }

            float[] data = new float[4];
            data[0] = maxSteepness;
            data[1] = avgElevation;
            data[2] = hightestY;
            data[3] = lowestY;
            return data;
        }



        public static bool IsAnyHexPointWithinPolygon(Vector3 point, int pointSize, Vector3[] polygonCorners)
        {
            if (VectorUtil.IsPointWithinPolygon(point, polygonCorners)) return true;
            return IsAnyHexEdgePointWithinPolygon(point, pointSize, polygonCorners);
        }
        public static bool IsAnyHexEdgePointWithinPolygon(Vector3 point, int pointSize, Vector3[] polygonCorners)
        {
            Vector3[] hexCorners = HexCoreUtil.GenerateHexagonPoints(point, pointSize);
            foreach (var item in hexCorners)
            {
                if (VectorUtil.IsPointWithinPolygon(item, polygonCorners)) return true;
            }
            return false;
        }
        public static bool IsAnyHexPointOutsidePolygon(Vector3 point, int pointSize, Vector3[] polygonCorners)
        {
            if (VectorUtil.IsPointWithinPolygon(point, polygonCorners) == false) return true;
            return IsAnyHexEdgePointOutsidePolygon(point, pointSize, polygonCorners);
        }
        public static bool IsAnyHexEdgePointOutsidePolygon(Vector3 point, int pointSize, Vector3[] polygonCorners)
        {
            Vector3[] hexCorners = HexCoreUtil.GenerateHexagonPoints(point, pointSize);
            foreach (var item in hexCorners)
            {
                if (VectorUtil.IsPointWithinPolygon(item, polygonCorners) == false) return true;
            }
            return false;
        }

        public static Vector2 Calculate_CenterLookup(Vector3 position, int size) => Calculate_CenterLookup(new Vector2(position.x, position.z), size);
        public static Vector2 Calculate_CenterLookup(Vector2 position, int size)
        {
            // if (size < (int)HexCellSizes.X_36)  return VectorUtil.ToVector2Int(position);
            if (size < (int)HexCellSizes.X_36) return VectorUtil.PointLookupDefault_X2(position);
            else return Calculate_CenterLookup_WithDynamicRound(position, size);
        }


        public static Vector2 Calculate_CenterLookup_WithDynamicRound(Vector2 position, int size)
        {
            int roundAmount = Mathf.RoundToInt((size / 12) / 5f) * 5;
            float roundedX = Mathf.Round(position.x / roundAmount) * roundAmount;
            float roundedY = Mathf.Round(position.y / roundAmount) * roundAmount;
            // Debug.Log("roundAmount: " + roundAmount + ", size: " + size);
            return new Vector2(roundedX, roundedY);
        }

        public static Vector3 Calculate_ClosestHexCenter(Vector3 position, int hexSize, float inRadiusMult = 0.96f)
        {
            // Calculate the grid coordinates of the given position
            float gridSize = hexSize * 1.5f;
            float gridX = position.x / gridSize;
            float gridZ = position.z / (hexSize * Mathf.Sqrt(3f));

            // Round the grid coordinates to the nearest integers
            int roundedX = Mathf.RoundToInt(gridX);
            int roundedZ = Mathf.RoundToInt(gridZ);

            // Calculate the precise grid position within the hexagon
            float preciseGridX = roundedX * gridSize;
            float preciseGridZ = roundedZ * (hexSize * Mathf.Sqrt(3f));

            List<Vector3> centerPoints = GenerateHexCenterPoints_X7(Vector3.zero, hexSize);
            // float distanceX = VectorUtil.DistanceXZ(centerPoints[2], centerPoints[4]);
            // float distanceZ = VectorUtil.DistanceXZ(centerPoints[0], centerPoints[3]);
            // Vector2 stepDistance = new Vector2(distanceX, distanceZ);
            Vector2 stepDistance = new Vector2(VectorUtil.DistanceXZ(centerPoints[2], centerPoints[4]), VectorUtil.DistanceXZ(centerPoints[0], centerPoints[3]));
            // Calculate the closest center position of the hexagon
            Vector2 centerPosition = new Vector2(preciseGridX, preciseGridZ);
            Vector2 closestCenterPosition = FindClosestGridPoint(centerPosition, stepDistance);
            Vector3 closestCenter = new Vector3(closestCenterPosition.x, 0f, closestCenterPosition.y);

            float distance = VectorUtil.DistanceXZ(position, closestCenter);
            if (distance > (hexSize * inRadiusMult))
            {
                // distanceX = VectorUtil.DistanceXZ(centerPoints[1], centerPoints[3]);
                // distanceZ = VectorUtil.DistanceXZ(centerPoints[0], centerPoints[1]);
                // stepDistance = new Vector2(distanceX, distanceZ);
                // closestCenterPosition = FindClosestGridPoint(centerPosition, stepDistance);
                // closestCenter = new Vector3(closestCenterPosition.x, 0f, closestCenterPosition.y);
                return Calculate_ClosestNeighborHexCenterPoint(position, closestCenter, hexSize);

                // List<Vector3> sidePTs = HexCoreUtil.GenerateHexCenterPoints_X6(closestCenter, hexSize);
                // return VectorUtil.GetClosestPoint_XZ(sidePTs.ToArray(), position);
            }
            return closestCenter;
        }

        public static Vector3 Calculate_ClosestHexCenter_V2(Vector3 position, int hexSize, float inRadiusMult = 0.96f)
        {
            // Calculate the grid coordinates of the given position
            float gridSize = hexSize * 1.5f;
            float gridX = position.x / gridSize;
            float gridZ = position.z / (hexSize * Mathf.Sqrt(3f));

            // Round the grid coordinates to the nearest integers
            int roundedX = Mathf.RoundToInt(gridX);
            int roundedZ = Mathf.RoundToInt(gridZ);

            // Calculate the precise grid position within the hexagon
            float preciseGridX = roundedX * gridSize;
            float preciseGridZ = roundedZ * (hexSize * Mathf.Sqrt(3f));

            List<Vector3> centerPoints = GenerateHexCenterPoints_X7(Vector3.zero, hexSize);
            Vector2 stepDistance = new Vector2(VectorUtil.DistanceXZ(centerPoints[2], centerPoints[4]), VectorUtil.DistanceXZ(centerPoints[0], centerPoints[3]));

            // Calculate the closest center position of the hexagon
            Vector2 centerPosition = new Vector2(preciseGridX, preciseGridZ);
            Vector2 closestCenterPosition = FindClosestGridPoint(centerPosition, stepDistance);
            Vector3 closestCenter = new Vector3(closestCenterPosition.x, 0f, closestCenterPosition.y);

            float distance = VectorUtil.DistanceXZ(position, closestCenter);
            if (distance > (hexSize * inRadiusMult)) return Calculate_ClosestNeighborHexCenterPoint(position, closestCenter, hexSize);
            return closestCenter;
        }

        private static Vector2 FindClosestGridPoint(Vector2 position, Vector2 stepDistance)
        {
            float roundedX = Mathf.Round(position.x / stepDistance.x);
            float roundedY = Mathf.Round(position.y / stepDistance.y);
            float closestX = roundedX * stepDistance.x;
            float closestY = roundedY * stepDistance.y;
            return new Vector2(closestX, closestY);
        }

        public static Vector3 Calculate_ClosestNeighborHexCenterPoint(Vector3 position, Vector3 center, int size)
        {
            Vector3[] cornerPoints = GenerateHexagonPoints(center, size);
            Vector3[] sideCenterPoints = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                Vector3 sidePoint = Vector3.Lerp(cornerPoints[i], cornerPoints[(i + 1) % 6], 0.5f);
                Vector3 direction = (sidePoint - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));
                sideCenterPoints[i] = center + direction * (edgeDistance * 2f);
            }
            return VectorUtil.GetClosestPoint_XZ(sideCenterPoints, position);
        }

        public static List<Vector3> Calculate_ClosestHexCenterPoints_X7(Vector3 position, int cellSize)
        {
            return GenerateHexCenterPoints_X7(Calculate_ClosestHexCenter_V2(position, cellSize), cellSize);
        }

        public static List<Vector2> Calculate_ClosestHexLookups_X7(Vector3 position, int cellSize) => Generate_Lookups_X7(Calculate_ClosestHexCenter_V2(position, cellSize), cellSize);

        public static List<Vector2> Calculate_ClosestHexLookups_X13(Vector3 position, int cellSize) => Generate_Lookups_X13(Calculate_ClosestHexCenter_V2(position, cellSize), cellSize);


        public static List<Vector3> Calculate_ClosestHexCenterPoints_X13(Vector3 position, int cellSize, float inRadiusMult = 1f)
        {
            return GenerateHexCenterPoints_X13(Calculate_ClosestHexCenter_V2(position, cellSize), cellSize);
        }

        public static Dictionary<int, List<Vector3>> Calculate_ClosestHexCenterPoints_X13(Vector3 position, int[] cellSizes, float inRadiusMult = 1f)
        {
            Dictionary<int, List<Vector3>> resultsbySize = new Dictionary<int, List<Vector3>>();

            foreach (var cellSize in cellSizes)
            {
                List<Vector3> newCenterPoints = GenerateHexCenterPoints_X13(Calculate_ClosestHexCenter_V2(position, cellSize), cellSize);
                resultsbySize.Add(cellSize, newCenterPoints);
            }
            return resultsbySize;
        }

        // public static Vector3 Calculate_ClosestHexCenter(Vector3 position, int hexSize)
        // {
        //     // Calculate the grid coordinates of the given position
        //     float gridSize = hexSize * 1.5f;
        //     float gridX = position.x / gridSize;
        //     float gridZ = position.z / (hexSize * Mathf.Sqrt(3f));

        //     // Round the grid coordinates to the nearest integers
        //     int roundedX = Mathf.RoundToInt(gridX);
        //     int roundedZ = Mathf.RoundToInt(gridZ);

        //     // Calculate the precise grid position within the hexagon
        //     float preciseGridX = roundedX * gridSize;
        //     float preciseGridZ = roundedZ * (hexSize * Mathf.Sqrt(3f));

        //     List<Vector3> centerPoints = GenerateHexCenterPoints_X7(Vector3.zero, hexSize);
        //     float distanceX = VectorUtil.DistanceXZ(centerPoints[2], centerPoints[4]);
        //     float distanceZ = VectorUtil.DistanceXZ(centerPoints[0], centerPoints[3]);
        //     Vector2 stepDistance = new Vector2(distanceX, distanceZ);

        //     // Calculate the closest center position of the hexagon
        //     Vector2 centerPosition = new Vector2(preciseGridX, preciseGridZ);
        //     Vector2 closestCenterPosition = FindClosestGridPoint(centerPosition, stepDistance);
        //     Vector3 closestCenter = new Vector3(closestCenterPosition.x, 0f, closestCenterPosition.y);

        //     return closestCenter;
        // }

        // public static Vector3 Calculate_ClosestHexCenter(Vector3 position, int hexSize)
        // {
        //     // Calculate the grid coordinates of the given position
        //     float gridSize = hexSize * 1.5f;
        //     float gridX = position.x / gridSize;
        //     float gridZ = position.z / (gridSize * Mathf.Sqrt(3f));

        //     // Round the grid coordinates to the nearest integers
        //     int roundedX = Mathf.RoundToInt(gridX);
        //     int roundedZ = Mathf.RoundToInt(gridZ);

        //     // Calculate the precise grid position within the hexagon
        //     float preciseGridX = roundedX * gridSize;
        //     float preciseGridZ = roundedZ * (gridSize * Mathf.Sqrt(3f));

        //     // Calculate the closest center position of the hexagon
        //     Vector3 closestCenter = GenerateHexCenterPoints_X7(Vector3.zero, hexSize)[0];
        //     closestCenter += new Vector3(preciseGridX, 0f, preciseGridZ);

        //     return closestCenter;
        // }


        public static (Vector2, Vector3) GetCloseestCellLookupInDictionary(
            Vector3 position,
            List<Vector3> cellCenterPoints,
            Dictionary<Vector2, LocationPrefab> worldspaceTerraformLookups,
            int cellSize
        )
        {
            Vector2 nearestTerraformLookup = Vector2.positiveInfinity;
            Vector3 nearestTerraformPoint = Vector2.positiveInfinity;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < cellCenterPoints.Count; i++)
            {
                float dist = VectorUtil.DistanceXZ(position, cellCenterPoints[i]);
                if (dist < nearestDistance)
                {
                    Vector2 cellLookup = HexCoreUtil.Calculate_CenterLookup(cellCenterPoints[i], cellSize);
                    if (worldspaceTerraformLookups.ContainsKey(cellLookup))
                    {
                        nearestDistance = dist;
                        nearestTerraformLookup = cellLookup;
                        nearestTerraformPoint = cellCenterPoints[i];

                        if (nearestDistance < cellSize * 0.5f) break;
                    }
                }
            }
            return (nearestTerraformLookup, nearestTerraformPoint);
        }

        public static HexagonCellPrototype GetCloseestCellLookupInDictionary(
            Vector3 position,
            List<Vector3> cellCenterPoints,
            Dictionary<Vector2, HexagonCellPrototype> cellLookups,
            int cellSize
        )
        {
            Vector2 nearestLookup = Vector2.positiveInfinity;
            HexagonCellPrototype nearestCell = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < cellCenterPoints.Count; i++)
            {
                float dist = VectorUtil.DistanceXZ(position, cellCenterPoints[i]);
                if (dist < nearestDistance && dist < cellSize)
                {
                    Vector2 cellLookup = HexCoreUtil.Calculate_CenterLookup(cellCenterPoints[i], cellSize);
                    if (cellLookups.ContainsKey(cellLookup))
                    {
                        nearestDistance = dist;
                        nearestLookup = cellLookup;
                        nearestCell = cellLookups[cellLookup];
                        if (nearestDistance < cellSize * 0.5f) break;
                    }
                }
            }
            return nearestCell;

        }
        public static (HexagonCellPrototype, float) GetCloseestCellLookupInDictionary_withDistance(
            Vector3 position,
            List<Vector3> cellCenterPoints,
            Dictionary<Vector2, HexagonCellPrototype> cellLookups,
            int cellSize
        )
        {
            Vector2 nearestLookup = Vector2.positiveInfinity;
            HexagonCellPrototype nearestCell = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < cellCenterPoints.Count; i++)
            {
                float dist = VectorUtil.DistanceXZ(position, cellCenterPoints[i]);
                if (dist < nearestDistance)
                {
                    Vector2 cellLookup = HexCoreUtil.Calculate_CenterLookup(cellCenterPoints[i], cellSize);
                    if (cellLookups.ContainsKey(cellLookup))
                    {
                        nearestDistance = dist;
                        nearestLookup = cellLookup;
                        nearestCell = cellLookups[cellLookup];

                        bool isin = VectorUtil.IsPositionWithinPolygon(nearestCell.cornerPoints, position);
                        if (isin || nearestDistance < cellSize * 0.5f) break;
                    }
                }
            }
            return (nearestCell, nearestDistance);
        }

        public static (HexagonCellPrototype, float) GetCloseestCellLookupInDictionary_withDistance(
            Vector3 position,
            Dictionary<int, List<Vector3>> cellCenterPoints_bySize,
            Dictionary<Vector2, HexagonCellPrototype> cellLookups,
            HashSet<CellStatus> includeCellStatusList = null
        )
        {
            Vector2 nearestLookup = Vector2.positiveInfinity;
            HexagonCellPrototype nearestCell = null;
            float nearestDistance = float.MaxValue;
            bool found = false;
            bool hasIncludeCellStatusList = includeCellStatusList != null;

            foreach (int cellSize in cellCenterPoints_bySize.Keys)
            {
                foreach (Vector3 centerPoint in cellCenterPoints_bySize[cellSize])
                {
                    float dist = VectorUtil.DistanceXZ(position, centerPoint);
                    if (dist < nearestDistance)
                    {
                        Vector2 cellLookup = HexCoreUtil.Calculate_CenterLookup(centerPoint, cellSize);
                        if (cellLookups.ContainsKey(cellLookup))
                        {
                            if (hasIncludeCellStatusList && includeCellStatusList.Contains(cellLookups[cellLookup].GetCellStatus()) == false) continue;

                            nearestDistance = dist;
                            nearestLookup = cellLookup;
                            nearestCell = cellLookups[cellLookup];

                            if (nearestCell.IsPath())
                            {
                                found = (nearestDistance < (cellSize * 0.3f));
                                if (found) break;
                            }
                            else
                            {
                                found = VectorUtil.IsPositionWithinPolygon(nearestCell.cornerPoints, position);
                                if (found) break;
                            }
                        }
                    }

                }
                if (found) break;
            }
            return (nearestCell, nearestDistance);
        }



        public static Vector3 Generate_HexNeighborCenterOnSide(Vector3 center, int size, HexagonSide side)
        {
            Vector3[] cornerPoints = GenerateHexagonPoints(center, size);
            int i = ((int)side + 1) % 6;
            Vector3 sidePoint = Vector3.Lerp(cornerPoints[i], cornerPoints[(i + 1) % 6], 0.5f);
            Vector3 direction = (sidePoint - center).normalized;
            return (center + direction * (Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z)) * 2f));
        }

        public static List<Vector2> Generate_Lookups_X7(Vector3 center, int size)
        {
            Vector3[] cornerPoints = GenerateHexagonPoints(center, size);
            List<Vector2> lookups = new List<Vector2>() {
                Calculate_CenterLookup(center, size)
            };

            for (int i = 0; i < 6; i++)
            {
                Vector3 sidePoint = Vector3.Lerp(cornerPoints[i], cornerPoints[(i + 1) % 6], 0.5f);
                Vector3 direction = (sidePoint - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));
                sidePoint = center + direction * (edgeDistance * 2f);
                lookups.Add(Calculate_CenterLookup(sidePoint, size));
            }
            return lookups;
        }

        public static List<Vector2> Generate_Lookups_X13(Vector3 center, int size)
        {
            Vector3[] cornerPoints = GenerateHexagonPoints(center, size);
            List<Vector2> lookups = new List<Vector2>() {
                Calculate_CenterLookup(center, size)
            };

            for (int i = 0; i < 6; i++)
            {
                // Get Side
                Vector3 sidePoint = Vector3.Lerp(cornerPoints[i], cornerPoints[(i + 1) % 6], 0.5f);
                Vector3 direction = (sidePoint - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));

                sidePoint = center + direction * (edgeDistance * 2f);
                lookups.Add(Calculate_CenterLookup(sidePoint, size));

                // Get Corner
                float angle = 60f * i;
                float x = center.x + size * Mathf.Cos(Mathf.Deg2Rad * angle);
                float z = center.z + size * Mathf.Sin(Mathf.Deg2Rad * angle);

                Vector3 cornerPoint = new Vector3(x, center.y, z);
                direction = (cornerPoint - center).normalized;
                edgeDistance = Vector2.Distance(new Vector2(cornerPoint.x, cornerPoint.z), new Vector2(center.x, center.z));

                cornerPoint = center + direction * (edgeDistance * 3f);
                lookups.Add(Calculate_CenterLookup(cornerPoint, size));
            }
            return lookups;
        }

        public static Vector2[] Generate_NeighborLookups_X6(Vector3 center, int size)
        {
            Vector3[] cornerPoints = GenerateHexagonPoints(center, size);
            Vector2[] results = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                Vector3 sidePoint = Vector3.Lerp(cornerPoints[i], cornerPoints[(i + 1) % 6], 0.5f);
                Vector3 direction = (sidePoint - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));
                sidePoint = center + direction * (edgeDistance * 2f);
                results[((i + 5) % 6)] = Calculate_CenterLookup(sidePoint, size);
            }
            return results;
        }

        public static Dictionary<HexagonSide, Vector2> Generate_NeighborLookups_BySide(Vector3 center, int size)
        {
            Vector3[] cornerPoints = GenerateHexagonPoints(center, size);
            Dictionary<HexagonSide, Vector2> results = new Dictionary<HexagonSide, Vector2>();
            for (int i = 0; i < 6; i++)
            {
                Vector3 sidePoint = Vector3.Lerp(cornerPoints[i], cornerPoints[(i + 1) % 6], 0.5f);
                Vector3 direction = (sidePoint - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));
                sidePoint = center + direction * (edgeDistance * 2f);
                results.Add((HexagonSide)((i + 5) % 6), Calculate_CenterLookup(sidePoint, size));
            }
            return results;
        }

        public static Dictionary<HexagonTileSide, Vector2> GenerateNeighborLookupCoordinates_X8(Vector3 center, int size, int layerOffset)
        {
            Vector3[] cornerPoints = GenerateHexagonPoints(center, size);
            Dictionary<HexagonTileSide, Vector2> results_by_tileSide = new Dictionary<HexagonTileSide, Vector2>();
            for (int i = 0; i < 6; i++)
            {
                Vector3 sidePoint = Vector3.Lerp(cornerPoints[i], cornerPoints[(i + 1) % 6], 0.5f);
                Vector3 direction = (sidePoint - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));
                sidePoint = center + direction * (edgeDistance * 2f);
                results_by_tileSide.Add((HexagonTileSide)((i + 5) % 6), Calculate_CenterLookup(sidePoint, size));
            }
            results_by_tileSide.Add(HexagonTileSide.Bottom, Calculate_CenterLookup(new Vector3(center.x, center.y - layerOffset, center.z), size));
            results_by_tileSide.Add(HexagonTileSide.Top, Calculate_CenterLookup(new Vector3(center.x, center.y + layerOffset, center.z), size));

            return results_by_tileSide;
        }



        public static List<Vector3> GenerateHexCenterPoints_X7(Vector3 center, int size)
        {
            Vector3[] cornerPoints = GenerateHexagonPoints(center, size);
            List<Vector3> results = new List<Vector3>() {
                center
            };
            for (int i = 0; i < 6; i++)
            {
                Vector3 sidePoint = Vector3.Lerp(cornerPoints[i], cornerPoints[(i + 1) % 6], 0.5f);
                Vector3 direction = (sidePoint - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));

                sidePoint = center + direction * (edgeDistance * 2f);
                results.Add(sidePoint);
            }
            return results;
        }

        public static List<Vector3> GenerateHexCenterPoints_X3(Vector3 center, int size, bool useOdds = false)
        {
            Vector3[] cornerPoints = GenerateHexagonPoints(center, size);
            List<Vector3> results = new List<Vector3>();
            for (int i = 0; i < 6; i++)
            {
                if (useOdds && (i != 1 && i != 3 && i != 5)) continue;
                if (useOdds == false && (i == 1 || i == 3 || i == 5)) continue;

                Vector3 sidePoint = Vector3.Lerp(cornerPoints[i], cornerPoints[(i + 1) % 6], 0.5f);
                Vector3 direction = (sidePoint - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));

                sidePoint = center + direction * (edgeDistance * 2f);
                results.Add(sidePoint);
            }
            return results;
        }

        public static List<Vector3> GenerateHexCenterPoints_X6(Vector3 center, int size)
        {
            Vector3[] cornerPoints = GenerateHexagonPoints(center, size);
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

        public static List<Vector3> Generate_RandomHexNeighborCenters(Vector3 center, int size, int max, bool shuffle)
        {
            List<Vector3> allNeighborPoints = GenerateHexCenterPoints_X6(center, size);
            VectorUtil.Shuffle(allNeighborPoints);
            int count = Mathf.Clamp(max, 1, allNeighborPoints.Count);
            List<Vector3> results = new List<Vector3>();
            for (int i = 0; i < count; i++)
            {
                results.Add(allNeighborPoints[i]);
            }
            return results;
        }

        public static List<Vector3> GenerateHexCenterPoints_X(Vector3 center, int size, HexNeighborExpansionSize neighborExpansionSize)
        {
            switch (neighborExpansionSize)
            {
                case HexNeighborExpansionSize.X_7:
                    return GenerateHexCenterPoints_X7(center, size);
                case HexNeighborExpansionSize.X_12:
                    return GenerateHexCenterPoints_X12(center, size);
                case HexNeighborExpansionSize.X_13:
                    return GenerateHexCenterPoints_X13(center, size);
                case HexNeighborExpansionSize.X_19:
                    return GenerateHexCenterPoints_X19(center, size);
                default:
                    return GenerateHexCenterPoints_X6(center, size);
            }
        }


        public static List<Vector3> GenerateHexCenterPoints_X12(Vector3 center, int size)
        {
            Vector3[] cornerPoints = GenerateHexagonPoints(center, size);
            List<Vector3> results = new List<Vector3>();
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

        public static List<Vector3> GenerateHexCenterPoints_X13(Vector3 center, int size)
        {
            Vector3[] cornerPoints = GenerateHexagonPoints(center, size);
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

        public static List<Vector3> Generate_RandomPathCenters(Vector3 center, int size, int max)
        {
            List<Vector3> results = new List<Vector3>();
            int startSide = UnityEngine.Random.Range(0, 5);
            int otherSide = (startSide + UnityEngine.Random.Range(2, 3)) % 6;

            Vector3 head = Generate_HexNeighborCenterOnSide(center, size, (HexagonSide)otherSide);
            results.Add(head);
            results.Add(center);

            Vector3 current = Generate_HexNeighborCenterOnSide(center, size, (HexagonSide)startSide);
            results.Add(current);

            for (int i = 0; i < max; i++)
            {
                int side = startSide;
                if (UnityEngine.Random.Range(0, 100) < 50)
                {
                    side = (startSide + UnityEngine.Random.Range(0, 1)) % 6;
                }
                else side = (startSide + UnityEngine.Random.Range(5, 6)) % 6;

                current = Generate_HexNeighborCenterOnSide(current, size, (HexagonSide)side);
                results.Add(current);
            }
            return results;
        }

        public static Dictionary<Vector2, Vector3> Generate_RandomPathLookups(Vector3 center, int size, int max)
        {
            Dictionary<Vector2, Vector3> results = new Dictionary<Vector2, Vector3>();
            int startSide = UnityEngine.Random.Range(0, 5);
            int otherSide = (startSide + UnityEngine.Random.Range(2, 3)) % 6;

            Vector3 head = Generate_HexNeighborCenterOnSide(center, size, (HexagonSide)otherSide);
            results.Add(Calculate_CenterLookup(head, size), head);
            results.Add(Calculate_CenterLookup(center, size), center);

            Vector3 current = Generate_HexNeighborCenterOnSide(center, size, (HexagonSide)startSide);
            results.Add(Calculate_CenterLookup(current, size), current);

            for (int i = 0; i < max; i++)
            {
                int side = startSide;
                if (UnityEngine.Random.Range(0, 100) < 50)
                {
                    side = (startSide + UnityEngine.Random.Range(0, 1)) % 6;
                }
                else side = (startSide + UnityEngine.Random.Range(5, 6)) % 6;

                current = Generate_HexNeighborCenterOnSide(current, size, (HexagonSide)side);
                results.Add(Calculate_CenterLookup(current, size), current);
            }
            return results;
        }


        public static Dictionary<int, Dictionary<Vector2, Vector3>> Generate_BaseBuildingNodeGroups(Vector3 center, HashSet<Vector2> excludeList, int maxRadius)
        {
            Dictionary<int, Dictionary<Vector2, Vector3>> buildingBlockClusters = new Dictionary<int, Dictionary<Vector2, Vector3>>();

            List<Vector3> hostPoints = HexCoreUtil.GenerateHexCenterPoints_X19(center, (int)HexCellSizes.X_36);
            HashSet<Vector2> added = new HashSet<Vector2>();
            Vector3[] absoluteBoundsCorners = HexCoreUtil.GenerateHexagonPoints(center, maxRadius);

            int ix = 0;
            foreach (var item in hostPoints)
            {
                Dictionary<Vector2, Vector3> clusterCenterPoints = HexGridPathingUtil.GetConsecutiveCellPoints(
                    item,
                    999,
                    12,
                    (int)HexCellSizes.X_36 * 3,
                    HexNeighborExpansionSize.X_7,
                    HexNeighborExpansionSize.X_7,
                    excludeList,
                    added
                , absoluteBoundsCorners,
                maxRadius
                );

                if (clusterCenterPoints.Count > 0)
                {
                    buildingBlockClusters.Add(ix, clusterCenterPoints);
                    ix++;
                }
            }
            return buildingBlockClusters;
        }


        public static Dictionary<int, Dictionary<Vector2, Vector3>> Generate_BaseBuildingClusters(
            Dictionary<int, Dictionary<Vector2, Vector3>> buildingBlockClusters,
            int maxMembers,
            HashSet<Vector2> excludeList
        )
        {
            Dictionary<int, Dictionary<Vector2, Vector3>> buildingNodeClusters = new Dictionary<int, Dictionary<Vector2, Vector3>>();
            HashSet<Vector2> added = new HashSet<Vector2>();
            int j = 0;
            foreach (var ix in buildingBlockClusters.Keys)
            {
                foreach (var lookup in buildingBlockClusters[ix].Keys)
                {
                    if (added.Contains(lookup)) continue;

                    Vector3 center = buildingBlockClusters[ix][lookup];

                    Dictionary<Vector2, Vector3> clusterCenterPoints = new Dictionary<Vector2, Vector3>();
                    List<Vector2> nearestCellLookups = HexCoreUtil.Calculate_ClosestHexLookups_X7(center, 12);

                    foreach (Vector2 currentLookup in nearestCellLookups)
                    {
                        if (buildingBlockClusters[ix].ContainsKey(currentLookup) == false) continue;
                        if (added.Contains(currentLookup)) continue;
                        if (excludeList != null && excludeList.Contains(currentLookup)) continue;

                        clusterCenterPoints.Add(currentLookup, buildingBlockClusters[ix][currentLookup]);
                        added.Add(currentLookup);

                        if (clusterCenterPoints.Count >= maxMembers) break;
                    }

                    if (clusterCenterPoints.Count > 0)
                    {
                        buildingNodeClusters.Add(j, clusterCenterPoints);
                        j++;
                    }
                }
            }

            return buildingNodeClusters;
        }


        public static (Dictionary<Vector2, Vector3>, Dictionary<Vector2, Vector3>) Generate_FoundationPoints(
            Vector3 center,
            int hostCellSize,
            int innersMax,
            int cornersMax,
            int random_Inners = 40,
            int random_Corners = 40,
            int random_Center = 100,
            Dictionary<Vector2, Vector3> allCenterPointsAdded = null
        )
        {
            int nodeCellSize = hostCellSize / 3;
            List<Vector3> hostPoints = HexCoreUtil.GenerateHexCenterPoints_X13(center, hostCellSize);

            HashSet<Vector2> assigned = new HashSet<Vector2>();
            Dictionary<Vector2, Vector3> final_foundationNodes = new Dictionary<Vector2, Vector3>();
            Dictionary<Vector2, Vector3> final_bufferNodes = new Dictionary<Vector2, Vector3>();

            bool addToBundle = allCenterPointsAdded != null;

            foreach (var host in hostPoints)
            {

                // int _min = UnityEngine.Random.Range(1, 4) == 1 ? UnityEngine.Random.Range(1, 4) : UnityEngine.Random.Range(4, 8);
                int _min = UnityEngine.Random.Range(2, 6);
                int _max = UnityEngine.Random.Range(4, 9);
                // int _max = _min == 1 ? UnityEngine.Random.Range(1, 4) : UnityEngine.Random.Range(5, 9);

                (
                    Dictionary<Vector2, Vector3> foundationNodes,
                    Dictionary<Vector2, Vector3> bufferNodes

                ) = HexCoreUtil.Generate_FoundationNode_CenterPoints(
                    host,
                    nodeCellSize,
                    innersMax,
                    cornersMax,
                    random_Inners,
                    random_Corners,
                    random_Center,
                    _min,
                    _max
                // UnityEngine.Random.Range(1, 3) == 1 ? UnityEngine.Random.Range(1, 3) : UnityEngine.Random.Range(4, 8),
                // UnityEngine.Random.Range(4, 7)
                );

                foreach (var k in foundationNodes.Keys)
                {
                    if (final_bufferNodes.ContainsKey(k)) continue;
                    if (final_foundationNodes.ContainsKey(k))
                    {
                        final_foundationNodes.Remove(k);
                        final_bufferNodes.Add(k, foundationNodes[k]);
                        continue;
                    }

                    final_foundationNodes.Add(k, foundationNodes[k]);

                    if (addToBundle && !allCenterPointsAdded.ContainsKey(k)) allCenterPointsAdded.Add(k, foundationNodes[k]);
                }

                foreach (var k in bufferNodes.Keys)
                {
                    if (final_bufferNodes.ContainsKey(k)) continue;
                    if (final_foundationNodes.ContainsKey(k)) continue;
                    // if (final_foundationNodes.ContainsKey(k))
                    // {
                    //     final_foundationNodes.Remove(k);
                    //     final_bufferNodes.Add(k, bufferNodes[k]);
                    //     continue;
                    // }

                    final_bufferNodes.Add(k, bufferNodes[k]);

                    if (addToBundle && !allCenterPointsAdded.ContainsKey(k)) allCenterPointsAdded.Add(k, bufferNodes[k]);
                }
            }
            return (final_foundationNodes, final_bufferNodes);
        }

        public static (Dictionary<Vector2, Vector3>, Dictionary<Vector2, Vector3>) Generate_FoundationNode_CenterPoints(
            Vector3 center,
            int size,
            int innersMax,
            int cornersMax,
            int random_Inners = 40,
            int random_Corners = 40,
            int random_Center = 100,
            int min = 2,
            int max = 7
        )
        {
            Vector3[] cornerPoints = GenerateHexagonPoints(center, size);
            Dictionary<Vector2, Vector3> foundationNodes = new Dictionary<Vector2, Vector3>();
            Dictionary<Vector2, Vector3> bufferNodes = new Dictionary<Vector2, Vector3>();

            if (UnityEngine.Random.Range(0, 100) < random_Center)
            {
                foundationNodes.Add(HexCoreUtil.Calculate_CenterLookup(center, size), center);
            }
            else bufferNodes.Add(HexCoreUtil.Calculate_CenterLookup(center, size), center);

            int inners = 0;
            int corners = 0;
            int startIX = UnityEngine.Random.Range(0, 7);

            for (int j = 0; j < 6; j++)
            {
                int ix = (j + startIX) % 6;

                if (foundationNodes.Count >= max)
                {
                    inners = innersMax;
                    corners = cornersMax;
                }

                // Get Side
                Vector3 sidePoint = Vector3.Lerp(cornerPoints[ix], cornerPoints[(ix + 1) % 6], 0.5f);
                Vector3 direction = (sidePoint - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));
                sidePoint = center + direction * (edgeDistance * 2f);

                int cornerChance = random_Corners;
                if (inners < innersMax && (foundationNodes.Count < min || UnityEngine.Random.Range(0, 100) < random_Inners))
                {
                    foundationNodes.Add(HexCoreUtil.Calculate_CenterLookup(sidePoint, size), sidePoint);
                    inners++;
                    cornerChance = (cornerChance / 3);
                }
                else bufferNodes.Add(HexCoreUtil.Calculate_CenterLookup(sidePoint, size), sidePoint);

                // Get Corner
                float angle = 60f * ix;
                float x = center.x + size * Mathf.Cos(Mathf.Deg2Rad * angle);
                float z = center.z + size * Mathf.Sin(Mathf.Deg2Rad * angle);

                Vector3 cornerPoint = new Vector3(x, center.y, z);
                direction = (cornerPoint - center).normalized;
                edgeDistance = Vector2.Distance(new Vector2(cornerPoint.x, cornerPoint.z), new Vector2(center.x, center.z));

                cornerPoint = center + direction * (edgeDistance * 3f);

                if (corners > cornersMax || (UnityEngine.Random.Range(0, 100) > cornerChance))
                {
                    foundationNodes.Add(HexCoreUtil.Calculate_CenterLookup(cornerPoint, size), cornerPoint);
                    corners++;
                }
                else bufferNodes.Add(HexCoreUtil.Calculate_CenterLookup(cornerPoint, size), cornerPoint);

            }
            // Debug.Log("total points: " + results.Count);
            return (foundationNodes, bufferNodes);
        }

        public static List<Vector3> GenerateHexCenterPoints_X7_Corners(Vector3 center, int size)
        {
            Vector3[] cornerPoints = GenerateHexagonPoints(center, size);
            List<Vector3> results = new List<Vector3>() {
                center
            };

            int innersMax = 3;
            int inners = 0;

            for (int i = 0; i < 6; i++)
            {
                // Get Side
                Vector3 sidePoint = Vector3.Lerp(cornerPoints[i], cornerPoints[(i + 1) % 6], 0.5f);
                Vector3 direction = (sidePoint - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));

                sidePoint = center + direction * (edgeDistance * 2f);

                if (inners < innersMax && (UnityEngine.Random.Range(0, 3) == 1))
                {
                    results.Add(sidePoint);
                    inners++;
                }

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


        public static List<Vector3> GenerateHexCenterPoints_X19(Vector3 center, int size)
        {
            Vector3[] cornerPoints = GenerateHexagonPoints(center, size);
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

                Vector3 offSidePoint = center + (direction * (edgeDistance * 2f)) * 2;
                results.Add(offSidePoint);

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

        public static List<Vector3> GenerateHexagonCenterPoints(Vector3 center, int size, bool addStartingCenter = true, bool useCorners = false)
        {
            Vector3[] cornerPoints = GenerateHexagonPoints(center, size);
            List<Vector3> results = new List<Vector3>();
            if (addStartingCenter) results.Add(center);

            for (int i = 0; i < 6; i++)
            {
                // Get Side
                Vector3 sidePoint = Vector3.Lerp(cornerPoints[i], cornerPoints[(i + 1) % 6], 0.5f);
                Vector3 direction = (sidePoint - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));
                int currentSide = (i + 5) % 6;

                sidePoint = center + direction * (edgeDistance * 2f);
                results.Add(sidePoint);

                if (useCorners)   // Get Corner
                {
                    float angle = 60f * i;
                    float x = center.x + size * Mathf.Cos(Mathf.Deg2Rad * angle);
                    float z = center.z + size * Mathf.Sin(Mathf.Deg2Rad * angle);

                    Vector3 cornerPoint = new Vector3(x, center.y, z);
                    direction = (cornerPoint - center).normalized;
                    edgeDistance = Vector2.Distance(new Vector2(cornerPoint.x, cornerPoint.z), new Vector2(center.x, center.z));

                    cornerPoint = center + direction * (edgeDistance * 3f);
                    results.Add(cornerPoint);
                }
            }

            return results;
        }

        public static List<Vector3> GenerateHexagonCenterPoints(Vector3 center, int size, List<int> useCorners, bool addStartingCenter = true)
        {
            Vector3[] cornerPoints = GenerateHexagonPoints(center, size);
            List<Vector3> results = new List<Vector3>();
            if (addStartingCenter) results.Add(center);
            for (int i = 0; i < 6; i++)
            {
                // Get Side
                Vector3 sidePoint = Vector3.Lerp(cornerPoints[i], cornerPoints[(i + 1) % 6], 0.5f);
                Vector3 direction = (sidePoint - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));

                sidePoint = center + direction * (edgeDistance * 2f);
                results.Add(sidePoint);

                if (useCorners != null)
                {
                    int currentSide = (i + 5) % 6;
                    (HexagonCorner cornerA, HexagonCorner cornerB) = GetCornersFromSide((HexagonSide)currentSide);
                    if (useCorners.Contains((int)cornerA) || useCorners.Contains((int)cornerB))
                    {
                        for (int cornerIX = 0; cornerIX < 2; cornerIX++)
                        {
                            float angle = 60f * (i + cornerIX);
                            float x = center.x + size * Mathf.Cos(Mathf.Deg2Rad * angle);
                            float z = center.z + size * Mathf.Sin(Mathf.Deg2Rad * angle);

                            Vector3 cornerPoint = new Vector3(x, center.y, z);
                            direction = (cornerPoint - center).normalized;
                            edgeDistance = Vector2.Distance(new Vector2(cornerPoint.x, cornerPoint.z), new Vector2(center.x, center.z));

                            cornerPoint = center + direction * (edgeDistance * 3f);
                            results.Add(cornerPoint);
                        }
                    }
                }
            }
            return results;
        }
        public static Vector3[] GenerateHexagonPoints(Vector3 center, float radius)
        {
            Vector3[] points = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = 60f * i;
                float x = center.x + radius * Mathf.Cos(Mathf.Deg2Rad * angle);
                float z = center.z + radius * Mathf.Sin(Mathf.Deg2Rad * angle);
                points[i] = new Vector3(x, center.y, z);
            }
            return points;
        }


        public static List<Vector3> BlockifyHexagonCorners(Vector3 center, float size)
        {
            Vector3[] cornerPoints = HexCoreUtil.GenerateHexagonPoints(center, size);

            List<HexagonSide> nullNeighborSides = new List<HexagonSide>() {
                    HexagonSide.Front,
                    HexagonSide.FrontRight,

                    HexagonSide.Back,
                    HexagonSide.BackLeft,
                };
            return HexCoreUtil.Generate_PartialHexagonCorners(center, size, nullNeighborSides).ToList();
        }

        // public static Vector3[] Generate_PartialHexagonCorners(Vector3 center, float size, List<HexagonSide> excludeSides)
        // {
        //     Vector3[] cornerPoints = HexCoreUtil.GenerateHexagonPoints(center, size);
        //     List<Vector3> partials = new List<Vector3>();

        //     for (int i = 0; i < 6; i++)
        //     {
        //         HexagonSide side = (HexagonSide)GetSideFromCorner((HexagonCorner)i);
        //         if (excludeSides.Contains(side))
        //         {
        //             Vector2Int cornerIX = HexCoreUtil.GetCornersFromSide_Default(side);

        //             Vector3 alteredPoint = VectorUtil.GetPointBetween(cornerPoints[(cornerIX.y + 0) % 6], cornerPoints[(cornerIX.y + 1) % 6]);
        //             cornerPoints[cornerIX.x] = alteredPoint;
        //             cornerPoints[cornerIX.y] = alteredPoint;
        //             i++;
        //         }
        //     }
        //     return cornerPoints;
        // }

        // public static Vector3[] Generate_PartialHexagonCorners(Vector3 center, float size, List<HexagonSide> excludeSides)
        // {
        //     Vector3[] cornerPoints = HexCoreUtil.GenerateHexagonPoints(center, size);
        //     List<Vector3> partials = new List<Vector3>();
        //     for (int i = 0; i < 6; i++)
        //     {
        //         HexagonSide side = (HexagonSide)i;
        //         if (excludeSides.Contains(side)) {

        //             continue;
        //         }

        //         Vector2Int cornerIX = HexCoreUtil.GetCornersFromSide_Default(side);
        //         partials.Add(cornerPoints[cornerIX.x]);
        //         partials.Add(cornerPoints[cornerIX.y]);
        //     }
        //     return partials;
        // }

        // public static Vector3[] Generate_PartialHexagonCorners(Vector3 center, float size, List<HexagonSide> excludeSides)
        // {
        //     Vector3[] cornerPoints = HexCoreUtil.GenerateHexagonPoints(center, size);
        //     Dictionary<Vector3, int> keep = new Dictionary<Vector3, int>();
        //     List<Vector3> partials = new List<Vector3>();
        //     int pointDist = -1;

        //     for (int i = 0; i < 6; i++)
        //     {
        //         HexagonSide side = (HexagonSide)i;
        //         if (excludeSides.Contains(side))
        //         {
        //             continue;
        //         }

        //         Vector2Int cornerIX = HexCoreUtil.GetCornersFromSide_Default(side);
        //         partials.Add(cornerPoints[cornerIX.x]);
        //         partials.Add(cornerPoints[cornerIX.y]);

        //         Vector3 lookupA = VectorUtil.PointLookupDefault(cornerPoints[cornerIX.x]);
        //         if (keep.ContainsKey(lookupA)) keep.Add(lookupA, cornerIX.x);

        //         Vector3 lookupB = VectorUtil.PointLookupDefault(cornerPoints[cornerIX.y]);
        //         if (keep.ContainsKey(lookupB)) keep.Add(lookupB, cornerIX.y);

        //         if (pointDist == -1) pointDist = (int)Vector3.Distance(cornerPoints[cornerIX.x], cornerPoints[cornerIX.y]);
        //     }

        //     for (int i = 0; i < partials.Count - 1; i++)
        //     {
        //         Vector3 lookup = VectorUtil.PointLookupDefault(partials[i]);

        //         if (pointDist < (int)Vector3.Distance(partials[i], partials[i + 1]))
        //         {
        //             Vector3 new_point = VectorUtil.GetPointBetween(partials[i], partials[i + 1]);
        //             if (keep.ContainsKey(lookup))
        //             {
        //                 partials.Add(new_point);
        //                 cornerPoints[keep[lookup]] = new_point;
        //             }
        //         }
        //     }

        //     return partials.ToArray();
        // }
        // public static Vector3[] Generate_PartialHexagonCorners(Vector3 center, float size, List<HexagonSide> excludeSides)
        // {
        //     Vector3[] cornerPoints = HexCoreUtil.GenerateHexagonPoints(center, size);
        //     List<Vector3> partials = new List<Vector3>();
        //     for (int i = 0; i < 6; i++)
        //     {
        //         HexagonSide side = (HexagonSide)i;
        //         if (excludeSides.Contains(side)) continue;

        //         Vector2Int cornerIX = HexCoreUtil.GetCornersFromSide_Default(side);
        //         partials.Add(cornerPoints[cornerIX.x]);
        //         partials.Add(cornerPoints[cornerIX.y]);
        //     }
        //     return partials.ToArray();
        // }

        public static Vector3[] Generate_PartialHexagonCorners(Vector3 center, float size, List<HexagonSide> excludeSides, List<int> mutatedCornersList = null)
        {
            Vector3[] cornerPoints = HexCoreUtil.GenerateHexagonPoints(center, size);
            Vector3[] cornerPoints_sm = HexCoreUtil.GenerateHexagonPoints(center, size / 2);
            List<Vector3> partials = new List<Vector3>();
            HashSet<HexagonSide> visited = new HashSet<HexagonSide>();

            int commonCornerIX = -1;
            if (excludeSides.Count == 2) commonCornerIX = GetCommonCornerFromConsecutiveSides(excludeSides[0], excludeSides[1]);

            if (excludeSides.Count == 3 && AreSidesConsecutive(excludeSides))
            {
                List<int> commonCorners = GetCommonCornerFromConsecutiveSides(excludeSides);
                if (commonCorners.Count > 0)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        if (commonCorners.Contains(i))
                        {
                            partials.Add(cornerPoints_sm[(i + 0) % 6]);
                            if (mutatedCornersList != null) mutatedCornersList.Add((i + 0) % 6);
                        }
                        else partials.Add(cornerPoints[(i + 0) % 6]);
                    }
                    return partials.ToArray();
                }
            }

            if (commonCornerIX > -1)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (i == commonCornerIX)
                    {
                        partials.Add(cornerPoints_sm[(i + 0) % 6]);
                        if (mutatedCornersList != null) mutatedCornersList.Add((i + 0) % 6);
                    }
                    else partials.Add(cornerPoints[(i + 0) % 6]);
                }
                return partials.ToArray();
            }

            for (int i = 0; i < 6; i++)
            {
                HexagonSide side = (HexagonSide)i;
                Vector2Int cornerIX = HexCoreUtil.GetCornersFromSide_Default(side);

                if (excludeSides.Contains(side) && visited.Contains(side) == false)
                {
                    // partials.Add(cornerPoints_sm[cornerIX.x]);
                    partials.Add(cornerPoints_sm[(i + 1) % 6]);
                    if (mutatedCornersList != null) mutatedCornersList.Add((i + 1) % 6);
                    visited.Add(side);
                    continue;
                }

                partials.Add(cornerPoints[(i + 0) % 6]);

                // partials.Add(cornerPoints[cornerIX.x]);
                // partials.Add(cornerPoints[cornerIX.y]);
            }
            return partials.ToArray();
        }

        // public static Vector3[] Generate_PartialHexagonCorners(Vector3 center, float size, List<HexagonSide> excludeSides)
        // {
        //     Vector3[] points = new Vector3[6];
        //     for (int i = 0; i < 6; i++)
        //     {
        //         float radius = size;
        //         HexagonSide side = (HexagonSide)((i + 5) % 6);
        //         if (excludeSides.Contains(side)) radius = size / 2;

        //         float angle = 60f * i;
        //         float z = center.z + radius * Mathf.Sin(Mathf.Deg2Rad * angle);
        //         float x = center.x + radius * Mathf.Cos(Mathf.Deg2Rad * angle);
        //         points[i] = new Vector3(x, center.y, z);
        //     }
        //     return points;
        // }

        // public static Vector3[] Generate_PartialHexagonCorners(Vector3 center, float size, List<HexagonSide> excludeSides)
        // {
        //     Vector3[] cornerPoints = HexCoreUtil.GenerateHexagonPoints(center, size);
        //     List<Vector3> partials = new List<Vector3>();

        //     List<int> partialsIxs = new List<int>();


        //     for (int i = 0; i < 6; i++)
        //     {
        //         HexagonSide side = (HexagonSide)i;
        //         Vector2Int cornerIX = HexCoreUtil.GetCornersFromSide_Default(side);

        //         if (excludeSides.Contains(side))
        //         {
        //             int ix = (cornerIX.x + 5) % 6;
        //             if (partialsIxs.Contains(ix)) partialsIxs.Add(ix);

        //             ix = (cornerIX.y + 1) % 6;
        //             if (partialsIxs.Contains(ix)) partialsIxs.Add(ix);
        //             continue;
        //         }
        //         partials.Add(cornerPoints[cornerIX.x]);
        //         partials.Add(cornerPoints[cornerIX.y]);
        //     }

        //     for (int i = 0; i < partialsIxs.Count - 1; i++)
        //     {
        //         HexagonSide side = (HexagonSide)i;
        //         Vector3 alteredPoint = VectorUtil.GetPointBetween(cornerPoints[partialsIxs[i]], cornerPoints[(partialsIxs[i + 1]) % 6]);
        //         cornerPoints[(partialsIxs[i] + 1) % 6] = alteredPoint;
        //         cornerPoints[(partialsIxs[i] + 5) % 6] = alteredPoint;
        //     }

        //     return cornerPoints;
        // }



        public static List<Vector3> Generate_RandomPartialHexagonPoints(Vector3 center, int size, int min, int max)
        {
            int num = UnityEngine.Random.Range(min, max);
            int startIX = UnityEngine.Random.Range(0, 7);

            // Vector3[] cornerPoints = GenerateHexagonPoints(center, size);
            // List<Vector3> results = new List<Vector3>();
            // for (int i = 0; i < 6; i += 2)
            // {
            //     Vector3 sidePoint = Vector3.Lerp(cornerPoints[i], cornerPoints[(i + 1) % 6], 0.5f);
            //     Vector3 direction = (sidePoint - center).normalized;
            //     float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));

            //     sidePoint = center + direction * (edgeDistance * 2f);
            //     results.Add(sidePoint);
            // }
            // return results;


            // return GenerateHexCenterPoints_X3(center, size, UnityEngine.Random.Range(0, 2) == 0);

            List<Vector3> points = new List<Vector3>();

            for (int i = 0; i < num; i += 2)
            {
                int ix = (i + startIX) % 6;
                float angle = 60f * ix;
                float x = center.x + size * Mathf.Cos(Mathf.Deg2Rad * angle);
                float z = center.z + size * Mathf.Sin(Mathf.Deg2Rad * angle);
                points.Add(new Vector3(x, center.y, z));
            }
            return points;
        }

        public static Vector2[] GenerateHexagonPointsXZ(Vector2 center, float radius)
        {
            Vector2[] points = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = 60f * i;
                float x = center.x + radius * Mathf.Cos(Mathf.Deg2Rad * angle);
                float z = center.y + radius * Mathf.Sin(Mathf.Deg2Rad * angle);
                points[i] = new Vector2(x, z);
            }
            return points;
        }

        public static Vector3[] GenerateHexagonCornerPoints(Vector3 center, float radius)
        {
            Vector3[] points = new Vector3[6];

            for (int i = 0; i < 6; i++)
            {
                float angle = 60f * i;
                float x = center.x + radius * Mathf.Cos(Mathf.Deg2Rad * angle);
                float z = center.z + radius * Mathf.Sin(Mathf.Deg2Rad * angle);

                Vector3 corner = new Vector3(x, center.y, z);
                Vector3 direction = (corner - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(corner.x, corner.z), new Vector2(center.x, center.z));
                corner = center + direction * edgeDistance * 3f;
                points[i] = corner;
            }
            return points;
        }

        public static Vector3[] GenerateHexagonSidePoints(Vector3[] corners)
        {
            Vector3[] hexagonSides = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                // Find the center point between the current corner and the next corner
                Vector3 side = Vector3.Lerp(corners[i], corners[(i + 1) % 6], 0.5f);
                hexagonSides[(i + 5) % 6] = side; // Places index 0 at the front side
            }
            return hexagonSides;
        }


        public static Vector3[] GenerateHexagonSidePoints(Vector3[] corners, float offset, Vector3 center)
        {
            Vector3[] hexagonSides = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                // Find the center point between the current corner and the next corner
                // Vector3 center = Vector3.Lerp(corners[i], corners[(i + 1) % 6], 0.5f);

                // Calculate the direction from the center to the current corner

                // Calculate the side point by offsetting from the center along the direction
                // Vector3 side = center + direction * offset;

                Vector3 side = Vector3.Lerp(corners[i], corners[(i + 1) % 6], 0.5f);
                Vector3 direction = (side - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(side.x, side.z), new Vector2(center.x, center.z));
                side = center + direction * edgeDistance * 2f;

                hexagonSides[(i + 5) % 6] = side; // Places index 0 at the front side
            }
            return hexagonSides;
        }


        public static List<Vector3> Generate_DottedCellEdgedPathPoints(Vector3 center, int hostCellSize, int stepDensity = 7)
        {
            List<Vector3> centerPoints = HexCoreUtil.GenerateHexCenterPoints_X13(center, hostCellSize);
            List<Vector3> dottedPath = new List<Vector3>();
            foreach (var item in centerPoints)
            {
                dottedPath.AddRange(VectorUtil.GenerateDottedLine(HexCoreUtil.GenerateHexagonPoints(item, hostCellSize).ToList(), stepDensity));
            }
            return dottedPath;
        }

        public static Dictionary<Vector2, Vector3> Generate_CellEdgedPathCellsCenters(Vector3 center, int cellSize, int hostCellSize, int stepDensity = 7)
        {
            Dictionary<Vector2, Vector3> resultsByLookup = new Dictionary<Vector2, Vector3>();
            List<Vector3> dottedPath = Generate_DottedCellEdgedPathPoints(center, hostCellSize, stepDensity);
            foreach (var item in dottedPath)
            {
                Vector3 hexCenter = Calculate_ClosestHexCenter_V2(item, cellSize);
                Vector2 lookup = Calculate_CenterLookup(hexCenter, cellSize);
                if (resultsByLookup.ContainsKey(lookup) == false) resultsByLookup.Add(lookup, hexCenter);
            }
            return resultsByLookup;
        }
        public static (Dictionary<int, Dictionary<Vector2, Vector3>>, Dictionary<Vector2, Vector3>) Generate_CellCityClusterCenters(
            Vector3 center,
            int cellSize,
            int hostCellSize,
            int clusterSizeMax,
            int stepDensity = 8,

            bool randomIze = false,
            int random_host = 50,
            int random_Center = 90,
            int random_upscale = 50
        )
        {
            // Vector3 gridCenter = Calculate_ClosestHexCenter_V2(center, hostCellSize);
            List<Vector3> hostCenterPoints = HexCoreUtil.GenerateHexCenterPoints_X7(center, hostCellSize);
            List<Vector3> dottedPath = new List<Vector3>();

            int startIx = 0;

            if (randomIze) startIx = UnityEngine.Random.Range(0, 7);

            for (int i = 0; i < hostCenterPoints.Count; i++)
            {
                int j = (i + startIx) % hostCenterPoints.Count;
                Vector3 item = hostCenterPoints[j];

                if (randomIze)
                {
                    if (i == 0 && UnityEngine.Random.Range(0, 100) > random_Center)
                    {
                        continue;
                    }
                    else if (UnityEngine.Random.Range(0, 100) > random_host) continue;
                }

                if (randomIze && UnityEngine.Random.Range(0, 100) < random_upscale)
                {
                    dottedPath.AddRange(VectorUtil.GenerateDottedLine(HexCoreUtil.GenerateHexagonPoints(item, hostCellSize * 3).ToList(), stepDensity * 3));
                    continue;
                }
                // if (randomIze && UnityEngine.Random.Range(0, 4) < 2)
                // {
                //     if (UnityEngine.Random.Range(0, 3) == 1)
                //     {
                //         dottedPath.AddRange(VectorUtil.GenerateDottedLine(HexCoreUtil.Generate_RandomPartialHexagonPoints(item, hostCellSize, 3, 7), stepDensity));
                //     }

                //     // else
                //     // {
                //     //     // Vector3[] sides = HexagonGenerator.GenerateHexagonSidePoints(HexCoreUtil.GenerateHexagonPoints(item, hostCellSize));
                //     //     // dottedPath.AddRange(VectorUtil.GenerateDottedLine(sides.ToList(), stepDensity));
                //     //     dottedPath.AddRange(VectorUtil.GenerateDottedLine(HexCoreUtil.GenerateHexagonPoints(item, hostCellSize).ToList(), stepDensity));
                //     // }
                // }
                // else
                dottedPath.AddRange(VectorUtil.GenerateDottedLine(HexCoreUtil.GenerateHexagonPoints(item, hostCellSize).ToList(), stepDensity));
            }

            Dictionary<Vector2, Vector3> pathCellCentersByLookup = new Dictionary<Vector2, Vector3>();
            HashSet<Vector2> excludeList = new HashSet<Vector2>();
            HashSet<Vector2> added = new HashSet<Vector2>();

            foreach (var item in dottedPath)
            {
                Vector3 hexCenter = Calculate_ClosestHexCenter_V2(item, cellSize);
                Vector2 lookup = Calculate_CenterLookup(hexCenter, cellSize);
                if (pathCellCentersByLookup.ContainsKey(lookup) == false)
                {
                    pathCellCentersByLookup.Add(lookup, hexCenter);
                    excludeList.Add(lookup);
                }
            }

            Dictionary<int, Dictionary<Vector2, Vector3>> buildingBlockClusters = new Dictionary<int, Dictionary<Vector2, Vector3>>();
            int ix = 0;

            foreach (var item in hostCenterPoints)
            {
                Vector3 hexCenter = Calculate_ClosestHexCenter_V2(item, hostCellSize);
                Dictionary<Vector2, Vector3> clusterCenterPoints = HexGridPathingUtil.GetConsecutiveCellPoints(
                    hexCenter,
                    clusterSizeMax,
                    cellSize,
                    hostCellSize,
                    HexNeighborExpansionSize.Default,
                    HexNeighborExpansionSize.Default,
                    excludeList,
                    added
                );

                if (clusterCenterPoints.Count > 0)
                {
                    buildingBlockClusters.Add(ix, clusterCenterPoints);
                    ix++;
                }
            }

            return (buildingBlockClusters, pathCellCentersByLookup);
        }


        public static (int, int) GetCornersFromSide_Condensed(HexagonSide side) // Stays Btw 0 - 6
        {
            int cornerA = Mathf.Abs((int)(side + 1)) % 6;
            int cornerB = Mathf.Abs((int)(cornerA + 1)) % 6;
            return (cornerA, cornerB);
        }
        public static Vector2Int GetCornersFromSide_Default(HexagonSide side) // Stays Btw 0 - 6
        {
            int cornerA = Mathf.Abs((int)(side + 1)) % 6;
            return new Vector2Int(cornerA, Mathf.Abs((int)(cornerA + 1)) % 6);
        }

        public static HexagonSide NextSide(HexagonSide side, bool clockwise)
        {
            int _side = (int)side;
            return clockwise ? (HexagonSide)((_side + 1) % 6) : (HexagonSide)((_side + 5) % 6);
        }
        public static HexagonSide OppositeSide(HexagonSide side)
        {
            return (HexagonSide)(((int)side + 3) % 6);
        }
        public static HexagonSide[] OppositeConsecutiveSides(HexagonSide side_A, HexagonSide side_B)
        {
            return new HexagonSide[2] { OppositeSide(side_A), OppositeSide(side_B), };
        }
        public static bool AreSidesConsecutive(HexagonSide side_A, HexagonSide side_B)
        {
            int sideValue_A = (int)side_A;
            int sideValue_B = (int)side_B;

            int difference = Mathf.Abs(sideValue_A - sideValue_B);
            return difference == 1 || difference == 5;
        }

        public static int GetCommonCornerFromConsecutiveSides(HexagonSide side_A, HexagonSide side_B)
        {
            int sideValue_A = (int)side_A;
            int sideValue_B = (int)side_B;

            HashSet<int> temp = new HashSet<int>();
            Vector2Int cornerIXs_A = HexCoreUtil.GetCornersFromSide_Default(side_A);
            temp.Add(cornerIXs_A.x);
            temp.Add(cornerIXs_A.y);

            Vector2Int cornerIXs_B = HexCoreUtil.GetCornersFromSide_Default(side_B);
            if (temp.Contains(cornerIXs_B.x)) return cornerIXs_B.x;
            if (temp.Contains(cornerIXs_B.y)) return cornerIXs_B.y;

            return -1;
        }

        public static List<int> GetCommonCornerFromConsecutiveSides(List<HexagonSide> sides)
        {
            HashSet<int> added = new HashSet<int>();
            List<int> results = new List<int>();
            for (int i = 0; i < sides.Count - 1; i++)
            {
                int commonCorner = GetCommonCornerFromConsecutiveSides(sides[i], sides[i + 1]);
                if (added.Contains(commonCorner) == false)
                {
                    added.Add(commonCorner);
                    results.Add(commonCorner);
                }
            }
            return results;
        }

        public static List<List<HexagonSide>> ExtractAllConsecutiveSides(List<HexagonSide> sides, int min = 2)
        {
            HashSet<HexagonSide> visited = new HashSet<HexagonSide>();
            List<List<HexagonSide>> foundSets = new List<List<HexagonSide>>();

            if (sides == null || sides.Count <= 1)
            {
                Debug.LogError("(sides == null || sides.Count <= 1)");
                return null;
            }

            for (int i = 0; i < sides.Count; i++)
            {
                HexagonSide currentSide = sides[i];

                // If the current side is already added to a set, skip it
                if (visited.Contains(currentSide))
                    continue;

                visited.Add(currentSide);

                // Create a new list to hold the consecutive sides
                List<HexagonSide> consecutiveSet = ExtractConsecutiveSidesFromStart(sides, currentSide, visited);

                // Add the consecutive set to the list of found sets
                if (consecutiveSet != null && consecutiveSet.Count >= min) // You can adjust this threshold as needed
                {
                    // Debug.Log("consecutiveSet:  " + consecutiveSet.Count);
                    foundSets.Add(consecutiveSet);
                }
            }

            return foundSets;
        }

        public static List<HexagonSide> ExtractConsecutiveSidesFromStart(List<HexagonSide> sides, HexagonSide start = HexagonSide.Front, HashSet<HexagonSide> visited = null)
        {
            if (sides == null || sides.Count <= 1)
                return null;

            int _currentSide = (sides.Contains(start)) ? (int)start : (int)sides[0];
            int step = 1;
            int ix = 0;

            List<HexagonSide> found = new List<HexagonSide>() {
                (HexagonSide)_currentSide
            };

            bool useVisited = visited != null;

            while (ix < sides.Count - 1)
            {
                HexagonSide nextSide = (HexagonSide)((_currentSide + step) % 6);
                // Debug.Log("_currentSide:  " + _currentSide + ", nextSide: " + nextSide + ", step: " + step);
                if (!sides.Contains(nextSide))
                {
                    if (step != 1) break;
                    step = 5; // switch to counter-clockwise
                    continue;
                }

                if (found.Contains(nextSide) == false) found.Add(nextSide);
                if (useVisited && visited.Contains(nextSide) == false) visited.Add(nextSide);
                _currentSide = (int)nextSide;
                ix++;
            }

            if (found.Count >= 2) return found;
            return null;
        }

        public static bool AreSidesConsecutive(List<HexagonSide> sides)
        {
            if (sides == null || sides.Count <= 1)
                return false;

            int _currentSide = (int)sides[0];
            int step = 1;
            int ix = 0;

            while (ix < sides.Count - 1)
            {
                HexagonSide nextSide = (HexagonSide)((_currentSide + step) % 6);

                // Debug.Log("_currentSide:  " + _currentSide + ", nextSide: " + nextSide + ", step: " + step);
                if (!sides.Contains(nextSide))
                {
                    if (step != 1) return false;
                    step = 5; // switch to counter-clockwise
                    continue;
                }
                _currentSide = (int)nextSide;
                ix++;
            }

            return true;
        }

        public static (HexagonCorner, HexagonCorner) GetCornersFromSide(HexagonSide side)
        {
            switch (side)
            {
                case HexagonSide.Front:
                    return (HexagonCorner.FrontA, HexagonCorner.FrontB);
                case HexagonSide.FrontRight:
                    return (HexagonCorner.FrontRightA, HexagonCorner.FrontRightB);
                case HexagonSide.BackRight:
                    return (HexagonCorner.BackRightA, HexagonCorner.BackRightB);
                case HexagonSide.Back:
                    return (HexagonCorner.BackA, HexagonCorner.BackB);
                case HexagonSide.BackLeft:
                    return (HexagonCorner.BackLeftA, HexagonCorner.BackLeftB);
                case HexagonSide.FrontLeft:
                    return (HexagonCorner.FrontLeftA, HexagonCorner.FrontLeftB);
                // Front
                default:
                    return (HexagonCorner.FrontA, HexagonCorner.FrontB);
            }
        }

        public static HexagonSide GetSideFromCorner(HexagonCorner corner)
        {
            if (corner == HexagonCorner.FrontA || corner == HexagonCorner.FrontB) return HexagonSide.Front;
            if (corner == HexagonCorner.FrontRightA || corner == HexagonCorner.FrontRightB) return HexagonSide.FrontRight;
            if (corner == HexagonCorner.BackRightA || corner == HexagonCorner.BackRightB) return HexagonSide.BackRight;

            if (corner == HexagonCorner.BackA || corner == HexagonCorner.BackB) return HexagonSide.Back;
            if (corner == HexagonCorner.BackLeftA || corner == HexagonCorner.BackLeftB) return HexagonSide.BackLeft;
            if (corner == HexagonCorner.FrontLeftA || corner == HexagonCorner.FrontLeftB) return HexagonSide.FrontLeft;

            return HexagonSide.Front;
        }

        public static Vector3[] GetSideCorners(HexagonCellPrototype cell, HexagonSide side)
        {
            Vector2Int cornerIX = HexCoreUtil.GetCornersFromSide_Default(side);
            return new Vector3[2] { cell.cornerPoints[cornerIX.x], cell.cornerPoints[cornerIX.y] };
        }

        public static int GetRotationFromSide(HexagonSide side) => (int)side;

        public static HexagonTileSide GetRotatedSide(HexagonSide side, int rotation)
        {
            int sideCount = 6;
            int rotatedIndex = ((int)side + rotation) % sideCount;

            return (HexagonTileSide)rotatedIndex;
        }

        public static HexagonTileSide GetRelativeHexagonSide(HexagonTileSide side)
        {
            // Assumes shared rotation
            switch (side)
            {
                case (HexagonTileSide.Front):
                    return HexagonTileSide.Back;
                case (HexagonTileSide.FrontRight):
                    return HexagonTileSide.BackLeft;
                case (HexagonTileSide.Back):
                    return HexagonTileSide.Front;
                case (HexagonTileSide.FrontLeft):
                    return HexagonTileSide.BackRight;
                case (HexagonTileSide.BackRight):
                    return HexagonTileSide.FrontLeft;
                case (HexagonTileSide.BackLeft):
                    return HexagonTileSide.FrontRight;
                case (HexagonTileSide.Top):
                    return HexagonTileSide.Bottom;
                case (HexagonTileSide.Bottom):
                    return HexagonTileSide.Top;
                default:
                    return HexagonTileSide.Front;
            }
        }

        public static HexagonSide GetRelativeHexagonSide(HexagonSide side)
        {
            switch (side)
            {
                case (HexagonSide.Front):
                    return HexagonSide.Back;
                case (HexagonSide.FrontRight):
                    return HexagonSide.BackLeft;
                case (HexagonSide.Back):
                    return HexagonSide.Front;
                case (HexagonSide.FrontLeft):
                    return HexagonSide.BackRight;
                case (HexagonSide.BackRight):
                    return HexagonSide.FrontLeft;
                case (HexagonSide.BackLeft):
                    return HexagonSide.FrontRight;
                default:
                    return HexagonSide.Front;
            }
        }

        public static bool IsHexCenterBetweenNeighborsInLookup(
            Vector3 hexCenter,
            int size,
            Dictionary<Vector2, Vector3> hexCenterLookups,
            Dictionary<Vector2, Vector3> found
        )
        {
            Vector2[] neighborLookups = HexCoreUtil.Generate_NeighborLookups_X6(hexCenter, size);

            for (int i = 0; i < neighborLookups.Length; i++)
            {
                Vector2 neighborLookup_A = neighborLookups[i];

                if (!hexCenterLookups.ContainsKey(neighborLookup_A)) continue;

                for (int step = 0; step < 3; step++)
                {
                    Vector2 neighborLookup_B = neighborLookups[(i + (2 + step)) % 6];

                    if (!hexCenterLookups.ContainsKey(neighborLookup_B)) continue;

                    if (found != null)
                    {
                        if (found.ContainsKey(neighborLookup_A) == false) found.Add(neighborLookup_A, hexCenterLookups[neighborLookup_A]);
                        if (found.ContainsKey(neighborLookup_B) == false) found.Add(neighborLookup_B, hexCenterLookups[neighborLookup_B]);
                    }
                    return true;
                }
            }
            return false;
        }
    }


}
