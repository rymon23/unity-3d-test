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

        public static int Calculate_CurrentLayer(int layerOffset, int currentElevation)
        {
            return Mathf.FloorToInt(currentElevation / layerOffset);
        }
        public static int Calculate_CurrentLayer(int layerOffset, Vector3 position)
        {
            return Calculate_CurrentLayer(layerOffset, (int)position.y);
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


        public static Vector2 Calculate_CenterLookup(Vector3 position, int size) => Calculate_CenterLookup(new Vector2(position.x, position.z), size);
        public static Vector2 Calculate_CenterLookup(Vector2 position, int size)
        {
            if (size < (int)HexCellSizes.X_36)
            {
                return VectorUtil.ToVector2Int(position);
            }
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

        //     // Debug.Log("Hex step dsitance: " + stepDist);

        //     // Calculate the closest center position of the hexagon
        //     Vector3 closestCenter = centerPoints[0];

        //     closestCenter += new Vector3(preciseGridX, 0f, preciseGridZ);

        //     return closestCenter;
        // }

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

        public static List<Vector3> Calculate_ClosestHexCenterPoints_X7(Vector3 position, int cellSize, float inRadiusMult = 1f)
        {
            return GenerateHexCenterPoints_X7(Calculate_ClosestHexCenter_V2(position, cellSize), cellSize);
        }

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


        public static (int, int) GetCornersFromSide_Condensed(HexagonSide side) // Stays Btw 0 - 6
        {
            int cornerA = Mathf.Abs((int)(side + 1)) % 6;
            int cornerB = Mathf.Abs((int)(cornerA + 1)) % 6;
            return (cornerA, cornerB);
        }
        // public static (int, int) GetCornersFromSide(HexagonSide side)
        // {
        //     int cornerA = Mathf.Abs((int)(side)) % 12;
        //     int cornerB = Mathf.Abs((int)(cornerA + 1)) % 12;
        //     return (cornerA, cornerB);
        // }

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

        public static Vector3 Generate_HexNeighborCenterOnSide(Vector3 center, int size, HexagonSide side)
        {
            Vector3[] cornerPoints = GenerateHexagonPoints(center, size);
            int i = ((int)side + 1) % 6;
            Vector3 sidePoint = Vector3.Lerp(cornerPoints[i], cornerPoints[(i + 1) % 6], 0.5f);
            Vector3 direction = (sidePoint - center).normalized;
            return (center + direction * (Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z)) * 2f));
        }

        public static List<Vector3> GenerateHexCenterPoints_X7(Vector3 center, int size)
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
            }
            return results;
        }

        public static List<Vector3> GenerateHexCenterPoints_X6(Vector3 center, int size)
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
            }
            // Debug.Log("total points: " + results.Count);
            return results;
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

        public static HexagonSide GetRelativeHexagonSideOnSharedRotation(HexagonSide side)
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
    }

}
