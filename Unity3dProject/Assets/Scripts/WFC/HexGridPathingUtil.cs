using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ProceduralBase;

namespace WFCSystem
{
    public enum CellSearchPriority { None = 0, SideNeighbors = 1, LayerNeighbors = 2, SideAndSideLayerNeighbors }

    public static class HexGridPathingUtil
    {
        public static List<List<HexagonCellPrototype>> GetConsecutiveClustersList(
            Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>> cellLookup_ByLayer,
            CellSearchPriority searchPriority = CellSearchPriority.SideNeighbors
        )
        {
            List<List<HexagonCellPrototype>> cells_byGroup = new List<List<HexagonCellPrototype>>();
            HashSet<string> visited = new HashSet<string>();
            int added = 0;
            int iterations = 0;

            foreach (int currentLayer in cellLookup_ByLayer.Keys)
            {
                foreach (Vector2 lookup in cellLookup_ByLayer[currentLayer].Keys)
                {
                    iterations++;

                    HexagonCellPrototype cell = cellLookup_ByLayer[currentLayer][lookup];
                    if (cell == null)
                    {
                        // Debug.LogError("(cell == null)");
                        continue;
                    }

                    if (visited.Contains(cell.Get_Uid())) continue;


                    List<HexagonCellPrototype> group = GetConsecutiveNeighborsCluster(
                        cell,
                        999,
                        searchPriority,
                        visited
                    );

                    if (group.Count > 0)
                    {
                        cells_byGroup.Add(group);
                        added++;
                    }
                    // if (clusters.Count >= max) return clusters;
                }
            }
            // Debug.Log("added: " + added + ", iterations: " + iterations);
            return cells_byGroup;
        }

        public static List<HexagonCellPrototype> GetConsecutiveNeighborsCluster(
            HexagonCellPrototype start,
            int maxMembers,
            CellSearchPriority searchPriority = CellSearchPriority.SideNeighbors,
            HashSet<string> visited = null
        )
        {
            List<HexagonCellPrototype> cluster = new List<HexagonCellPrototype>();
            if (visited == null) visited = new HashSet<string>();

            RecursivelyFindNeighborsForCluster(start, maxMembers, cluster, visited, searchPriority);
            return cluster;
        }

        private static void RecursivelyFindNeighborsForCluster(
            HexagonCellPrototype current,
            int maxMembers,
            List<HexagonCellPrototype> cluster,
            HashSet<string> visited,
            CellSearchPriority searchPriority = CellSearchPriority.SideNeighbors
        )
        {
            cluster.Add(current);
            visited.Add(current.Get_Uid());

            // if (cluster.Count >= maxMembers) return;

            // List<HexagonCellPrototype> sortedNeighbors = EvaluateNeighborSearchPriority(current, searchPriority);
            HexagonCellPrototype[] allNeighborsByTileSide = current.GetNeighborTileSides();

            for (int i = 0; i < allNeighborsByTileSide.Length; i++)
            {
                // if (cluster.Count > maxMembers) break;

                HexagonCellPrototype neighbor = allNeighborsByTileSide[i];
                if (neighbor == null) continue;

                if (visited.Contains(neighbor.Get_Uid()) == false)
                {
                    RecursivelyFindNeighborsForCluster(neighbor, maxMembers, cluster, visited, searchPriority);
                }
            }
        }



        public static void Rehydrate_CellNeighbors(
            Vector2 cellLookup,
            Dictionary<Vector2, HexagonCellPrototype> cells_ByLookup,
            bool propagateToNeighbors
        )
        {
            if (cells_ByLookup.ContainsKey(cellLookup) && cells_ByLookup[cellLookup].neighborWorldData != null)
            {
                foreach (CellWorldData neighborData in cells_ByLookup[cellLookup].neighborWorldData)
                {
                    Vector2 neighborLookup = neighborData.lookup;
                    if (cells_ByLookup.ContainsKey(neighborLookup) == false) continue;

                    HexagonCellPrototype neighborCell = cells_ByLookup[neighborLookup];
                    if (neighborCell != null && cells_ByLookup[cellLookup].neighbors.Contains(neighborCell) == false)
                    {
                        cells_ByLookup[cellLookup].neighbors.Add(neighborCell);
                        if (propagateToNeighbors) Rehydrate_CellNeighbors(neighborLookup, cells_ByLookup, false);
                    }
                }
            }
        }

        public static void Rehydrate_CellNeighbors(
            Vector2 parentLookup,
            Vector2 cellLookup,
            Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> cells_ByParentLookup,
            bool propagateToNeighbors
        )
        {
            if (cells_ByParentLookup[parentLookup].ContainsKey(cellLookup) && cells_ByParentLookup[parentLookup][cellLookup].neighborWorldData != null)
            {
                foreach (CellWorldData neighborData in cells_ByParentLookup[parentLookup][cellLookup].neighborWorldData)
                {
                    Vector2 neighborParentLookup = neighborData.parentLookup;
                    if (cells_ByParentLookup.ContainsKey(neighborParentLookup) == false) continue;

                    Vector2 neighborLookup = neighborData.lookup;
                    if (cells_ByParentLookup[neighborParentLookup].ContainsKey(neighborLookup) == false) continue;

                    HexagonCellPrototype neighborCell = cells_ByParentLookup[neighborParentLookup][neighborLookup];
                    if (neighborCell != null && cells_ByParentLookup[parentLookup][cellLookup].neighbors.Contains(neighborCell) == false)
                    {
                        cells_ByParentLookup[parentLookup][cellLookup].neighbors.Add(neighborCell);
                        if (propagateToNeighbors) Rehydrate_CellNeighbors(neighborParentLookup, neighborLookup, cells_ByParentLookup, false);
                    }
                }
            }
        }


        public static Dictionary<Vector2, Vector3> GetConsecutiveCellPoints(
            Vector3 initialCenterPostion,
            int maxMembers,
            int _cellSize,
            int maxRadius,
            HexNeighborExpansionSize headExpansionSize = HexNeighborExpansionSize.X_7,
            HexNeighborExpansionSize initialExpansionSize = HexNeighborExpansionSize.X_7,
            HashSet<Vector2> excludeList = null,
            HashSet<Vector2> added = null,
            Vector3[] absoluteBoundsCorners = null,
            int absoluteBoundsRadius = 108
        )
        {
            if (maxRadius < _cellSize * 3) maxRadius = (_cellSize * 3);

            Vector3 currentHead = initialCenterPostion;
            Vector2 currentHeadLookup = HexCoreUtil.Calculate_CenterLookup(currentHead, _cellSize);

            Dictionary<Vector2, Vector3> foundByLookup = new Dictionary<Vector2, Vector3>();
            HashSet<Vector2> visited = new HashSet<Vector2>();
            List<Vector3> headsToCheck = new List<Vector3>();

            int attempts = 999;
            bool doOnce = false;

            Vector3[] boundsCorners = HexCoreUtil.GenerateHexagonPoints(initialCenterPostion, maxRadius);
            int found = 0;

            while (currentHead != Vector3.positiveInfinity && found < maxMembers && attempts > 0)
            {
                if (found >= maxMembers) break;

                List<Vector3> neighborPoints = doOnce ?
                    HexCoreUtil.GenerateHexCenterPoints_X(currentHead, _cellSize, initialExpansionSize)
                    : HexCoreUtil.GenerateHexCenterPoints_X(currentHead, _cellSize, headExpansionSize);

                doOnce = true;

                for (int i = 0; i < neighborPoints.Count; i++)
                {
                    if (found >= maxMembers) break;

                    Vector3 neighbor = neighborPoints[i];
                    Vector2 neighborLookup = HexCoreUtil.Calculate_CenterLookup(neighbor, _cellSize);

                    if (visited.Contains(neighborLookup)) continue;
                    visited.Add(neighborLookup);

                    if (excludeList != null && excludeList.Contains(neighborLookup)) continue;
                    if (added != null && added.Contains(neighborLookup)) continue;

                    if (HexCoreUtil.IsAnyHexPointWithinPolygon(neighbor, _cellSize, boundsCorners))
                    {
                        if (absoluteBoundsCorners != null && VectorUtil.IsPointWithinPolygon(neighbor, absoluteBoundsCorners) == false) continue;
                        // if (absoluteBoundsCorners != null && HexCoreUtil.IsAnyHexPointWithinPolygon(neighbor, _cellSize, absoluteBoundsCorners) == false) continue;

                        foundByLookup.Add(neighborLookup, neighbor);
                        headsToCheck.Add(neighbor);

                        if (added != null) added.Add(neighborLookup);
                        found++;
                    }
                }

                if (found < maxMembers)
                {
                    if (headsToCheck.Count > 0)
                    {
                        currentHead = headsToCheck[0];
                        currentHeadLookup = HexCoreUtil.Calculate_CenterLookup(currentHead, _cellSize);
                        headsToCheck.Remove(currentHead);
                    }
                    else break;
                }
                attempts--;
            }
            // Debug.Log("foundByLookup: " + foundByLookup.Count);
            return foundByLookup;
        }



        public static (Dictionary<int, List<Vector3>>, Dictionary<Vector2, Vector3>, Vector2Int) GetConsecutiveCellPointsWithintNoiseElevationRange_V7(
            Vector3 initialCenterPostion,
            int maxMembers,
            List<LayeredNoiseOption> layerdNoises_terrain,
            HexCellSizes cellSize,
            float globalTerrainHeight,
            float globalElevation,
            int cellLayerElevation = 3,
            int offsetMult = 2,
            float mutateRangeMult = 6
        )
        {
            int _cellSize = (int)cellSize;

            Vector3 currentHead = initialCenterPostion;
            Vector2 currentHeadLookup = HexCoreUtil.Calculate_CenterLookup(currentHead, _cellSize);

            Dictionary<int, List<Vector3>> found_bySize = new Dictionary<int, List<Vector3>>() {
                { _cellSize, new List<Vector3>() },
            };
            HashSet<Vector2> visited = new HashSet<Vector2>();
            List<Vector3> headsToCheck = new List<Vector3>();

            float baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)currentHead.x, (int)currentHead.z, globalTerrainHeight, layerdNoises_terrain);
            baseNoiseHeight += globalElevation;

            int baseElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(baseNoiseHeight, cellLayerElevation);
            currentHead.y = baseElevation;
            int currentHeadElevation;

            found_bySize[_cellSize].Add(currentHead);
            visited.Add(currentHeadLookup);
            Vector2Int lowestHighestPointHeight = new Vector2Int(baseElevation, baseElevation);

            int attempts = 999;
            bool doOnce = false;

            Dictionary<Vector2, Vector3> islandbufferPoints = new Dictionary<Vector2, Vector3>();
            List<Vector3> islandPointsToEvaluate = new List<Vector3>();

            while (currentHead != Vector3.positiveInfinity && found_bySize[_cellSize].Count < maxMembers && attempts > 0)
            {
                List<Vector3> neighborPoints = doOnce ? HexCoreUtil.GenerateHexCenterPoints_X19(currentHead, _cellSize) : HexCoreUtil.GenerateHexCenterPoints_X12(currentHead, _cellSize);
                // List<Vector3> neighborPoints = doOnce ? HexCoreUtil.GenerateHexCenterPoints_X12(currentHead, _cellSize) : HexCoreUtil.GenerateHexCenterPoints_X6(currentHead, _cellSize);
                doOnce = true;

                List<Vector3> islandPoints = new List<Vector3>();
                currentHeadElevation = (int)currentHead.y;
                int found = 0;

                for (int i = 0; i < neighborPoints.Count; i++)
                {
                    if (found_bySize[_cellSize].Count >= maxMembers) break;

                    Vector3 neighbor = neighborPoints[i];
                    Vector2 neighborLookup = HexCoreUtil.Calculate_CenterLookup(neighbor, _cellSize);

                    if (visited.Contains(neighborLookup)) continue;
                    visited.Add(neighborLookup);

                    float newNoiseHeight = globalElevation + LayerdNoise.Calculate_NoiseHeightForCoordinate((int)neighbor.x, (int)neighbor.z, globalTerrainHeight, layerdNoises_terrain);
                    int newElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(newNoiseHeight, cellLayerElevation);

                    int diff = (newElevation > currentHeadElevation) ? (newElevation - currentHeadElevation) : (currentHeadElevation - newElevation);
                    int maxOffset = (cellLayerElevation * offsetMult);

                    found_bySize[_cellSize].Add(neighbor);

                    if (diff > maxOffset || (diff > 0 && found > 3))
                    {
                        neighbor.y = newElevation;

                        islandbufferPoints.Add(neighborLookup, neighbor);
                        continue;
                    }

                    neighbor.y = currentHeadElevation;

                    headsToCheck.Add(neighbor);
                    islandPointsToEvaluate.Add(neighbor);
                    found++;

                    if (lowestHighestPointHeight.x > newElevation) lowestHighestPointHeight.x = newElevation;
                    else if (lowestHighestPointHeight.y < newElevation) lowestHighestPointHeight.y = newElevation;
                }

                if (found == 0 && islandbufferPoints.ContainsKey(currentHeadLookup) == false)
                {
                    islandbufferPoints.Add(currentHeadLookup, currentHead);
                    islandPointsToEvaluate.Remove(currentHead);
                }


                if (found_bySize[_cellSize].Count < maxMembers)
                {
                    if (headsToCheck.Count > 0)
                    {
                        currentHead = headsToCheck[0];
                        currentHeadLookup = HexCoreUtil.Calculate_CenterLookup(currentHead, _cellSize);
                        headsToCheck.Remove(currentHead);
                    }
                    else break;
                }
                attempts--;
            }


            // List<Vector3> allIslandPoints = Normalize_ConsecutiveLayerPoints(
            //     islandPointsToEvaluate,
            //     // found_bySize[_cellSize],

            //     cellSize,
            //     layerdNoises_terrain,
            //     globalTerrainHeight,
            //     globalElevation,
            //     cellLayerElevation
            // );

            // found_bySize[_cellSize] = allIslandPoints;

            return (found_bySize, islandbufferPoints, lowestHighestPointHeight);
        }

        public static (Dictionary<int, Dictionary<Vector2, Vector3>>, Dictionary<Vector2, Vector3>, Vector2Int) GetConsecutiveCellPointsWithintNoiseElevationRange_V6(
            Vector3 initialCenterPostion,
            int maxMembers,
            List<LayeredNoiseOption> layerdNoises_terrain,
            HexCellSizes cellSize,
            float globalTerrainHeight,
            float globalElevation,
            int cellLayerElevation = 3,
            int offsetMult = 2,
            float mutateRangeMult = 6
        )
        {
            int _cellSize = (int)cellSize;

            Vector3 currentHead = initialCenterPostion;
            Vector2 currentHeadLookup = HexCoreUtil.Calculate_CenterLookup(currentHead, _cellSize);

            Dictionary<int, Dictionary<Vector2, Vector3>> found_bySize = new Dictionary<int, Dictionary<Vector2, Vector3>>() {
                { _cellSize, new Dictionary<Vector2, Vector3>() {
                    {currentHeadLookup, currentHead}
                } },
            };
            HashSet<Vector2> visited = new HashSet<Vector2>();
            List<Vector3> headsToCheck = new List<Vector3>();

            float baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)currentHead.x, (int)currentHead.z, globalTerrainHeight, layerdNoises_terrain);
            baseNoiseHeight += globalElevation;

            int baseElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(baseNoiseHeight, cellLayerElevation);
            currentHead.y = baseElevation;
            int currentHeadElevation;

            // found_bySize[_cellSize].Add(currentHead);

            visited.Add(currentHeadLookup);
            Vector2Int lowestHighestPointHeight = new Vector2Int(baseElevation, baseElevation);

            int attempts = 999;
            bool doOnce = false;

            Dictionary<Vector2, Vector3> islandbufferPoints = new Dictionary<Vector2, Vector3>();
            List<Vector3> islandPointsToEvaluate = new List<Vector3>();

            while (currentHead != Vector3.positiveInfinity && found_bySize[_cellSize].Count < maxMembers && attempts > 0)
            {
                List<Vector3> neighborPoints = doOnce ? HexCoreUtil.GenerateHexCenterPoints_X19(currentHead, _cellSize) : HexCoreUtil.GenerateHexCenterPoints_X12(currentHead, _cellSize);
                // List<Vector3> neighborPoints = doOnce ? HexCoreUtil.GenerateHexCenterPoints_X12(currentHead, _cellSize) : HexCoreUtil.GenerateHexCenterPoints_X6(currentHead, _cellSize);
                doOnce = true;

                List<Vector3> islandPoints = new List<Vector3>();
                currentHeadElevation = (int)currentHead.y;

                int found = 0;

                for (int i = 0; i < neighborPoints.Count; i++)
                {
                    if (found_bySize[_cellSize].Count >= maxMembers) break;

                    Vector3 neighbor = neighborPoints[i];
                    Vector2 neighborLookup = HexCoreUtil.Calculate_CenterLookup(neighbor, _cellSize);

                    if (visited.Contains(neighborLookup)) continue;
                    visited.Add(neighborLookup);

                    float newNoiseHeight = globalElevation + LayerdNoise.Calculate_NoiseHeightForCoordinate((int)neighbor.x, (int)neighbor.z, globalTerrainHeight, layerdNoises_terrain);
                    int newElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(newNoiseHeight, cellLayerElevation);

                    int diff = (newElevation > currentHeadElevation) ? (newElevation - currentHeadElevation) : (currentHeadElevation - newElevation);
                    int maxOffset = (cellLayerElevation * offsetMult);

                    found_bySize[_cellSize].Add(neighborLookup, neighbor);

                    if (diff > maxOffset) // || (diff > 0 && found > 3))
                    {
                        neighbor.y = newElevation;

                        islandbufferPoints.Add(neighborLookup, neighbor);
                        continue;
                    }

                    neighbor.y = currentHeadElevation;

                    headsToCheck.Add(neighbor);
                    islandPointsToEvaluate.Add(neighbor);
                    found++;

                    if (lowestHighestPointHeight.x > newElevation) lowestHighestPointHeight.x = newElevation;
                    else if (lowestHighestPointHeight.y < newElevation) lowestHighestPointHeight.y = newElevation;
                }

                if (found == 0 && islandbufferPoints.ContainsKey(currentHeadLookup) == false)
                {
                    islandbufferPoints.Add(currentHeadLookup, currentHead);
                    islandPointsToEvaluate.Remove(currentHead);
                }


                if (found_bySize[_cellSize].Count < maxMembers)
                {
                    if (headsToCheck.Count > 0)
                    {
                        currentHead = headsToCheck[0];
                        currentHeadLookup = HexCoreUtil.Calculate_CenterLookup(currentHead, _cellSize);
                        headsToCheck.Remove(currentHead);
                    }
                    else break;
                }
                attempts--;
            }

            // List<Dictionary<Vector2, Vector3>> islands = Normalize_ConsecutiveLayerPoints(
            //     islandPointsToEvaluate,
            //     found_bySize[_cellSize],

            //     cellSize,
            //     layerdNoises_terrain,
            //     globalTerrainHeight,
            //     globalElevation,
            //     cellLayerElevation
            // );

            return (found_bySize, islandbufferPoints, lowestHighestPointHeight);
        }



        public static (Dictionary<int, Dictionary<Vector2, Vector3>>, Dictionary<Vector2, Vector3>, Vector2Int) GetConsecutiveCellPointsWithintNoiseElevationRange_V5(
            Vector3 initialCenterPostion,
            int maxMembers,
            List<LayeredNoiseOption> layerdNoises_terrain,
            HexCellSizes cellSize,
            float globalTerrainHeight,
            float globalElevation,
            int cellLayerElevation = 3,
            int offsetMult = 2,
            float mutateRangeMult = 6
        )
        {
            int _cellSize = (int)cellSize;

            Vector3 currentHead = initialCenterPostion;
            Vector2 currentHeadLookup = HexCoreUtil.Calculate_CenterLookup(currentHead, _cellSize);

            Dictionary<int, Dictionary<Vector2, Vector3>> found_bySize = new Dictionary<int, Dictionary<Vector2, Vector3>> {
                { _cellSize, new Dictionary<Vector2, Vector3>() {
                    { currentHeadLookup, currentHead }
                } },
            };
            HashSet<Vector2> visited = new HashSet<Vector2>();
            List<Vector3> headsToCheck = new List<Vector3>();

            float baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)currentHead.x, (int)currentHead.z, globalTerrainHeight, layerdNoises_terrain);
            baseNoiseHeight += globalElevation;

            int baseElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(baseNoiseHeight, cellLayerElevation);
            currentHead.y = baseElevation;
            int currentHeadElevation;

            visited.Add(currentHeadLookup);
            Vector2Int lowestHighestPointHeight = new Vector2Int(baseElevation, baseElevation);

            int attempts = 999;
            bool doOnce = false;

            Dictionary<Vector2, Vector3> islandbufferPoints = new Dictionary<Vector2, Vector3>();

            while (currentHead != Vector3.positiveInfinity && found_bySize[_cellSize].Count < maxMembers && attempts > 0)
            {
                // List<Vector3> neighborPoints = doOnce ? HexCoreUtil.GenerateHexCenterPoints_X19(currentHead, _cellSize) : HexCoreUtil.GenerateHexCenterPoints_X12(currentHead, _cellSize);
                List<Vector3> neighborPoints = doOnce ? HexCoreUtil.GenerateHexCenterPoints_X12(currentHead, _cellSize) : HexCoreUtil.GenerateHexCenterPoints_X6(currentHead, _cellSize);
                doOnce = true;

                List<Vector3> islandPoints = new List<Vector3>();
                currentHeadElevation = (int)currentHead.y;
                int found = 0;

                for (int i = 0; i < neighborPoints.Count; i++)
                {
                    if (found_bySize[_cellSize].Count >= maxMembers) break;

                    Vector3 neighbor = neighborPoints[i];
                    Vector2 neighborLookup = HexCoreUtil.Calculate_CenterLookup(neighbor, _cellSize);

                    if (visited.Contains(neighborLookup)) continue;
                    visited.Add(neighborLookup);

                    float newNoiseHeight = globalElevation + LayerdNoise.Calculate_NoiseHeightForCoordinate((int)neighbor.x, (int)neighbor.z, globalTerrainHeight, layerdNoises_terrain);
                    int newElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(newNoiseHeight, cellLayerElevation);

                    int diff = (newElevation > currentHeadElevation) ? (newElevation - currentHeadElevation) : (currentHeadElevation - newElevation);
                    int maxOffset = (cellLayerElevation * offsetMult);

                    found_bySize[_cellSize].Add(neighborLookup, neighbor);

                    if (diff > maxOffset) // || (diff > 0 && found > 3))
                    {
                        neighbor.y = newElevation;

                        islandbufferPoints.Add(neighborLookup, neighbor);
                        continue;
                    }

                    neighbor.y = currentHeadElevation;

                    headsToCheck.Add(neighbor);
                    found++;

                    if (lowestHighestPointHeight.x > newElevation) lowestHighestPointHeight.x = newElevation;
                    else if (lowestHighestPointHeight.y < newElevation) lowestHighestPointHeight.y = newElevation;
                }

                if (found == 0 && islandbufferPoints.ContainsKey(currentHeadLookup) == false)
                {
                    islandbufferPoints.Add(currentHeadLookup, currentHead);
                }

                if (found_bySize[_cellSize].Count < maxMembers)
                {
                    if (headsToCheck.Count > 0)
                    {
                        currentHead = headsToCheck[0];
                        currentHeadLookup = HexCoreUtil.Calculate_CenterLookup(currentHead, _cellSize);
                        headsToCheck.Remove(currentHead);
                    }
                    else break;
                }
                attempts--;
            }
            return (found_bySize, islandbufferPoints, lowestHighestPointHeight);
        }

        // public static (Dictionary<int, Dictionary<Vector2, Vector3>>, Dictionary<Vector2, Vector3>, Vector2Int) GetConsecutiveCellPointsWithintNoiseElevationRange_V5(
        //     Vector3 initialCenterPostion,
        //     int maxMembers,
        //     List<LayeredNoiseOption> layerdNoises_terrain,
        //     HexCellSizes cellSize,
        //     float globalTerrainHeight,
        //     float globalElevation,
        //     int cellLayerElevation = 3,
        //     int offsetMult = 2,
        //     float mutateRangeMult = 6
        // )
        // {
        //     int _cellSize = (int)cellSize;

        //     Vector3 currentHead = initialCenterPostion;
        //     Vector2 currentHeadLookup = HexCoreUtil.Calculate_CenterLookup(currentHead, _cellSize);

        //     Dictionary<int, Dictionary<Vector2, Vector3>> found_bySize = new Dictionary<int, Dictionary<Vector2, Vector3>> {
        //         { _cellSize, new Dictionary<Vector2, Vector3>() {
        //             { currentHeadLookup, currentHead }
        //         } },
        //     };
        //     HashSet<Vector2> visited = new HashSet<Vector2>();
        //     List<Vector3> headsToCheck = new List<Vector3>();

        //     float baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)currentHead.x, (int)currentHead.z, globalTerrainHeight, layerdNoises_terrain);
        //     baseNoiseHeight += globalElevation;

        //     int baseElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(baseNoiseHeight, cellLayerElevation);
        //     currentHead.y = baseElevation;
        //     int currentHeadElevation;

        //     // found_bySize[_cellSize].Add(currentHead);
        //     visited.Add(currentHeadLookup);
        //     Vector2Int lowestHighestPointHeight = new Vector2Int(baseElevation, baseElevation);

        //     int attempts = 999;
        //     bool doOnce = false;

        //     Dictionary<Vector2, Vector3> islandbufferPoints = new Dictionary<Vector2, Vector3>();
        //     List<Dictionary<Vector2, Vector3>> islandClusters = new List<Dictionary<Vector2, Vector3>>() {
        //         new Dictionary<Vector2, Vector3>{
        //             { currentHeadLookup, currentHead }
        //         }
        //     };
        //     int currentIsland = 0;
        //     Dictionary<Vector2, int> islandindices = new Dictionary<Vector2, int>() {
        //        { currentHeadLookup, currentIsland}
        //     };

        //     while (currentHead != Vector3.positiveInfinity && found_bySize[_cellSize].Count < maxMembers && attempts > 0)
        //     {
        //         List<Vector3> neighborPoints = doOnce ? HexCoreUtil.GenerateHexCenterPoints_X19(currentHead, _cellSize) : HexCoreUtil.GenerateHexCenterPoints_X12(currentHead, _cellSize);
        //         // List<Vector3> neighborPoints = HexCoreUtil.GenerateHexCenterPoints_X6(currentHead, _cellSize);
        //         doOnce = true;

        //         if (islandindices.ContainsKey(currentHeadLookup) == false)
        //         {
        //             currentIsland++;
        //             islandindices.Add(currentHeadLookup, currentIsland);
        //         }

        //         currentHeadElevation = (int)currentHead.y;
        //         int found = 0;

        //         for (int i = 0; i < neighborPoints.Count; i++)
        //         {
        //             if (found_bySize[_cellSize].Count >= maxMembers) break;

        //             Vector3 neighbor = neighborPoints[i];
        //             Vector2 neighborLookup = HexCoreUtil.Calculate_CenterLookup(neighbor, _cellSize);

        //             if (visited.Contains(neighborLookup)) continue;
        //             visited.Add(neighborLookup);

        //             float newNoiseHeight = globalElevation + LayerdNoise.Calculate_NoiseHeightForCoordinate((int)neighbor.x, (int)neighbor.z, globalTerrainHeight, layerdNoises_terrain);
        //             int newElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(newNoiseHeight, cellLayerElevation);

        //             int diff = (newElevation > currentHeadElevation) ? (newElevation - currentHeadElevation) : (currentHeadElevation - newElevation);
        //             int maxOffset = (cellLayerElevation * offsetMult);

        //             found_bySize[_cellSize].Add(neighborLookup, neighbor);

        //             if (diff > maxOffset || (diff > 0 && found > 3))
        //             {
        //                 neighbor.y = newElevation;

        //                 islandbufferPoints.Add(neighborLookup, neighbor);
        //                 continue;
        //             }

        //             if (lowestHighestPointHeight.x > neighbor.y) lowestHighestPointHeight.x = (int)neighbor.y;
        //             else if (lowestHighestPointHeight.y < neighbor.y) lowestHighestPointHeight.y = (int)neighbor.y;

        //             neighbor.y = currentHeadElevation;

        //             headsToCheck.Add(neighbor);
        //             islandindices.Add(neighborLookup, islandindices[currentHeadLookup]);

        //             found++;
        //         }

        //         if (found == 0 && islandbufferPoints.ContainsKey(currentHeadLookup) == false) islandbufferPoints.Add(currentHeadLookup, currentHead);

        //         if (found_bySize[_cellSize].Count < maxMembers)
        //         {
        //             if (headsToCheck.Count > 0)
        //             {
        //                 currentHead = headsToCheck[0];
        //                 currentHeadLookup = HexCoreUtil.Calculate_CenterLookup(currentHead, _cellSize);
        //                 headsToCheck.Remove(currentHead);
        //             }
        //             else break;
        //         }
        //         attempts--;
        //     }

        //     return (found_bySize, islandbufferPoints, lowestHighestPointHeight);
        // }
        public static (Dictionary<int, Dictionary<Vector2, Vector3>>, Dictionary<Vector2, Vector3>, Vector2Int) GetConsecutiveCellPointsWithintNoiseElevationRange_V4(
            Vector3 initialCenterPostion,
            int maxMembers,
            List<LayeredNoiseOption> layerdNoises_terrain,
            HexCellSizes cellSize,
            float globalTerrainHeight,
            float globalElevation,
            int cellLayerElevation = 3,
            int offsetMult = 2,
            float mutateRangeMult = 6
        )
        {
            int _cellSize = (int)cellSize;

            Vector3 currentHead = initialCenterPostion;
            Vector2 currentHeadLookup = HexCoreUtil.Calculate_CenterLookup(currentHead, _cellSize);

            Dictionary<int, Dictionary<Vector2, Vector3>> found_bySize = new Dictionary<int, Dictionary<Vector2, Vector3>> {
                { _cellSize, new Dictionary<Vector2, Vector3>() {
                    { currentHeadLookup, currentHead }
                } },
            };
            HashSet<Vector2> visited = new HashSet<Vector2>();
            List<Vector3> headsToCheck = new List<Vector3>();

            float baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)currentHead.x, (int)currentHead.z, globalTerrainHeight, layerdNoises_terrain);
            baseNoiseHeight += globalElevation;

            int baseElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(baseNoiseHeight, cellLayerElevation);
            currentHead.y = baseElevation;
            int currentHeadElevation;

            // found_bySize[_cellSize].Add(currentHead);
            visited.Add(currentHeadLookup);
            Vector2Int lowestHighestPointHeight = new Vector2Int(baseElevation, baseElevation);

            int attempts = 999;
            bool doOnce = false;

            Dictionary<Vector2, Vector3> islandbufferPoints = new Dictionary<Vector2, Vector3>();
            List<Dictionary<Vector2, Vector3>> islandClusters = new List<Dictionary<Vector2, Vector3>>() {
                new Dictionary<Vector2, Vector3>{
                    { currentHeadLookup, currentHead }
                }
            };
            int currentIsland = 0;
            Dictionary<Vector2, int> islandindices = new Dictionary<Vector2, int>() {
               { currentHeadLookup, currentIsland}
            };

            while (currentHead != Vector3.positiveInfinity && found_bySize[_cellSize].Count < maxMembers && attempts > 0)
            {
                // List<Vector3> neighborPoints = doOnce ? HexCoreUtil.GenerateHexCenterPoints_X19(currentHead, _cellSize) : HexCoreUtil.GenerateHexCenterPoints_X12(currentHead, _cellSize);
                List<Vector3> neighborPoints = HexCoreUtil.GenerateHexCenterPoints_X6(currentHead, _cellSize);
                doOnce = true;

                bool headIsBuffer = islandbufferPoints.ContainsKey(currentHeadLookup);

                if (headIsBuffer == false && islandindices.ContainsKey(currentHeadLookup) == false)
                {
                    currentIsland++;
                    islandindices.Add(currentHeadLookup, currentIsland);
                }

                currentHeadElevation = headIsBuffer ? baseElevation : (int)currentHead.y;
                int found = 0;

                for (int i = 0; i < neighborPoints.Count; i++)
                {
                    if (found_bySize[_cellSize].Count >= maxMembers) break;

                    Vector3 neighbor = neighborPoints[i];
                    Vector2 neighborLookup = HexCoreUtil.Calculate_CenterLookup(neighbor, _cellSize);

                    if (visited.Contains(neighborLookup)) continue;
                    visited.Add(neighborLookup);

                    float newNoiseHeight = globalElevation + LayerdNoise.Calculate_NoiseHeightForCoordinate((int)neighbor.x, (int)neighbor.z, globalTerrainHeight, layerdNoises_terrain);
                    int newElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(newNoiseHeight, cellLayerElevation);

                    int diff = (newElevation > currentHeadElevation) ? (newElevation - currentHeadElevation) : (currentHeadElevation - newElevation);
                    int maxOffset = (cellLayerElevation * offsetMult);

                    found_bySize[_cellSize].Add(neighborLookup, neighbor);

                    if (diff > maxOffset) // || (diff > 0 && found > 3))
                    {
                        neighbor.y = newElevation;

                        islandbufferPoints.Add(neighborLookup, neighbor);
                        if (headIsBuffer == false) headsToCheck.Add(neighbor);

                        continue;
                    }

                    if (lowestHighestPointHeight.x > neighbor.y) lowestHighestPointHeight.x = (int)neighbor.y;
                    else if (lowestHighestPointHeight.y < neighbor.y) lowestHighestPointHeight.y = (int)neighbor.y;

                    if (headIsBuffer)
                    {
                        neighbor.y = newElevation;

                        List<Vector3> newIslandPoints = HexCoreUtil.GenerateHexCenterPoints_X6(neighbor, _cellSize);
                        foreach (var item in newIslandPoints)
                        {
                            Vector3 pt = newIslandPoints[i];
                            Vector2 ptLookup = HexCoreUtil.Calculate_CenterLookup(pt, _cellSize);

                            if (visited.Contains(ptLookup))
                            {
                                if (islandbufferPoints.ContainsKey(ptLookup) == false)
                                {
                                    islandbufferPoints.Add(neighborLookup, neighbor);
                                    break;
                                }
                            }
                        }

                        if (islandbufferPoints.ContainsKey(neighborLookup) == false) headsToCheck.Add(neighbor);
                        break;
                    }

                    neighbor.y = currentHeadElevation;

                    headsToCheck.Add(neighbor);
                    islandindices.Add(neighborLookup, islandindices[currentHeadLookup]);

                    found++;

                    // if (lowestHighestPointHeight.x > neighbor.y) lowestHighestPointHeight.x = (int)neighbor.y;
                    // else if (lowestHighestPointHeight.y < neighbor.y) lowestHighestPointHeight.y = (int)neighbor.y;
                }

                if (found == 0 && islandbufferPoints.ContainsKey(currentHeadLookup) == false) islandbufferPoints.Add(currentHeadLookup, currentHead);

                if (found_bySize[_cellSize].Count < maxMembers)
                {
                    if (headsToCheck.Count > 0)
                    {
                        currentHead = headsToCheck[0];
                        currentHeadLookup = HexCoreUtil.Calculate_CenterLookup(currentHead, _cellSize);
                        headsToCheck.Remove(currentHead);
                    }
                    else break;
                }
                attempts--;
            }

            return (found_bySize, islandbufferPoints, lowestHighestPointHeight);
        }

        public static (Dictionary<int, List<Vector3>>, Dictionary<Vector2, Vector3>, Vector2Int) GetConsecutiveCellPointsWithintNoiseElevationRange_V3(
            Vector3 initialCenterPostion,
            int maxMembers,
            List<LayeredNoiseOption> layerdNoises_terrain,
            HexCellSizes cellSize,
            float globalTerrainHeight,
            float globalElevation,
            int cellLayerElevation = 3,
            int offsetMult = 2,
            float mutateRangeMult = 6
        )
        {
            int _cellSize = (int)cellSize;
            int _islandCellSize = _cellSize * 3;

            Vector3 currentHead = initialCenterPostion;
            Vector2 currentLookup = HexCoreUtil.Calculate_CenterLookup(currentHead, _islandCellSize);

            Dictionary<int, List<Vector3>> found_bySize = new Dictionary<int, List<Vector3>>() {
                { _islandCellSize, new List<Vector3>() },
                { _cellSize, new List<Vector3>() },
            };
            HashSet<Vector2> visited = new HashSet<Vector2>();

            float baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)currentHead.x, (int)currentHead.z, globalTerrainHeight, layerdNoises_terrain);
            baseNoiseHeight += globalElevation;

            int baseElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(baseNoiseHeight, cellLayerElevation);
            currentHead.y = baseElevation;

            found_bySize[_islandCellSize].Add(currentHead);
            found_bySize[_cellSize].Add(currentHead);
            visited.Add(currentLookup);
            Vector2Int lowestHighestPointHeight = new Vector2Int(baseElevation, baseElevation);

            Dictionary<Vector2, Vector3> islandbufferCells = new Dictionary<Vector2, Vector3>();

            // Get setcion cells
            List<Vector3> islandNeighbors = HexCoreUtil.GenerateHexCenterPoints_X12(currentHead, _islandCellSize);
            for (int i = 0; i < islandNeighbors.Count; i++)
            {
                Vector3 neighbor = islandNeighbors[i];
                currentLookup = HexCoreUtil.Calculate_CenterLookup(neighbor, _islandCellSize);
                if (visited.Contains(currentLookup)) continue;


                float newNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)neighbor.x, (int)neighbor.z, globalTerrainHeight, layerdNoises_terrain);
                newNoiseHeight += globalElevation;

                int newElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(newNoiseHeight, cellLayerElevation);

                int diff = (newElevation > baseElevation) ? (newElevation - baseElevation) : (baseElevation - newElevation);
                if (diff > (cellLayerElevation * mutateRangeMult)) continue;

                int maxOffset = (cellLayerElevation * offsetMult);
                if (diff > maxOffset) newElevation = (newElevation > baseElevation) ? (baseElevation + cellLayerElevation) : (baseElevation);

                neighbor.y = newElevation;

                found_bySize[_islandCellSize].Add(neighbor);
                visited.Add(currentLookup);

                if (lowestHighestPointHeight.x > newElevation) lowestHighestPointHeight.x = newElevation;
                else if (lowestHighestPointHeight.y < newElevation) lowestHighestPointHeight.y = newElevation;


                List<Vector3> children = HexCoreUtil.GenerateHexCenterPoints_X12(neighbor, _cellSize);
                for (int j = 0; j < children.Count; j++)
                {
                    Vector3 child = children[j];
                    Vector2 childLookup = HexCoreUtil.Calculate_CenterLookup(child, _cellSize);
                    if (visited.Contains(childLookup)) continue;
                    visited.Add(childLookup);

                    newNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)child.x, (int)child.z, globalTerrainHeight, layerdNoises_terrain);
                    newNoiseHeight += globalElevation;

                    int childElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(newNoiseHeight, cellLayerElevation);

                    diff = (childElevation > newElevation) ? (childElevation - newElevation) : (newElevation - childElevation);
                    if (diff > (cellLayerElevation * mutateRangeMult))
                    {
                        child.y = childElevation;
                        islandbufferCells.Add(childLookup, child);
                        found_bySize[_cellSize].Add(child);
                        continue;
                    }

                    childElevation = newElevation;
                    child.y = childElevation;

                    found_bySize[_cellSize].Add(child);

                    if (lowestHighestPointHeight.x > childElevation) lowestHighestPointHeight.x = childElevation;
                    else if (lowestHighestPointHeight.y < childElevation) lowestHighestPointHeight.y = childElevation;
                }
            }

            return (found_bySize, islandbufferCells, lowestHighestPointHeight);
        }

        public static (Dictionary<int, List<Vector3>>, Vector2Int) GetConsecutiveCellPointsWithintNoiseElevationRange_V2(
            Vector3 initialCenterPostion,
            int maxMembers,
            List<LayeredNoiseOption> layerdNoises_terrain,
            HexCellSizes cellSize,
            float globalTerrainHeight,
            float globalElevation,
            int cellLayerElevation = 3,
            int offsetMult = 2,
            float mutateRangeMult = 6
        )
        {
            int _cellSize = (int)cellSize;

            Vector3 currentHead = initialCenterPostion;
            Vector2 currentLookup = HexCoreUtil.Calculate_CenterLookup(currentHead, _cellSize);

            Dictionary<int, List<Vector3>> found_bySize = new Dictionary<int, List<Vector3>>() {
                { _cellSize, new List<Vector3>() },
            };
            HashSet<Vector2> visited = new HashSet<Vector2>();
            List<Vector3> headsToCheck = new List<Vector3>();

            float baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)currentHead.x, (int)currentHead.z, globalTerrainHeight, layerdNoises_terrain);
            baseNoiseHeight += globalElevation;

            int baseElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(baseNoiseHeight, cellLayerElevation);
            currentHead.y = baseElevation;

            found_bySize[_cellSize].Add(currentHead);
            visited.Add(currentLookup);
            Vector2Int lowestHighestPointHeight = new Vector2Int(baseElevation, baseElevation);

            int attempts = 999;

            while (currentHead != Vector3.positiveInfinity && found_bySize[_cellSize].Count < maxMembers && attempts > 0)
            {
                List<Vector3> neighborPoints = HexCoreUtil.GenerateHexCenterPoints_X12(currentHead, _cellSize);

                if (neighborPoints.Count > 0)
                {
                    for (int i = 0; i < neighborPoints.Count; i++)
                    {
                        if (found_bySize[_cellSize].Count >= maxMembers) break;

                        Vector3 neighbor = neighborPoints[i];

                        currentLookup = HexCoreUtil.Calculate_CenterLookup(neighbor, _cellSize);
                        if (visited.Contains(currentLookup)) continue;


                        float newNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)neighbor.x, (int)neighbor.z, globalTerrainHeight, layerdNoises_terrain);
                        newNoiseHeight += globalElevation;

                        int newElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(newNoiseHeight, cellLayerElevation);

                        int diff = (newElevation > baseElevation) ? (newElevation - baseElevation) : (baseElevation - newElevation);

                        if (diff > (cellLayerElevation * mutateRangeMult)) continue;
                        // {
                        //     int _subCellSize = _cellSize / 3;
                        //     List<Vector3> subPoints = HexCoreUtil.GenerateHexCenterPoints_X12(neighbor, _subCellSize);

                        //     if (subPoints.Count > 0)
                        //     {
                        //         if (found_bySize.ContainsKey(_subCellSize) == false) found_bySize.Add(_subCellSize, new List<Vector3>());

                        //         int added = 0;
                        //         // int previousElevation = -1;

                        //         for (int j = 0; j < subPoints.Count; j++)
                        //         {
                        //             Vector3 subPoint = subPoints[j];

                        //             currentLookup = HexCoreUtil.Calculate_CenterLookup(subPoint, _subCellSize);
                        //             if (visited.Contains(currentLookup)) continue;

                        //             // if (previousElevation > -1 && (added > 0 && (added < 2 || UnityEngine.Random.Range(0, 5) < 3)))
                        //             // {
                        //             //     subPoint.y = previousElevation;

                        //             //     found_bySize[_subCellSize].Add(subPoint);
                        //             //     visited.Add(currentLookup);

                        //             //     previousElevation = newElevation;
                        //             //     added++;

                        //             //     continue;
                        //             // }

                        //             newNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)subPoint.x, (int)subPoint.z, globalTerrainHeight, layerdNoises_terrain);
                        //             newNoiseHeight += globalElevation;

                        //             newElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(newNoiseHeight, cellLayerElevation);

                        //             diff = (newElevation > baseElevation) ? (newElevation - baseElevation) : (baseElevation - newElevation);

                        //             if (diff > (cellLayerElevation * 2)) continue;

                        //             int offset = (cellLayerElevation * offsetMult);
                        //             if (diff > offset) newElevation = (newElevation > baseElevation) ? (baseElevation + cellLayerElevation) : (baseElevation - cellLayerElevation);

                        //             subPoint.y = newElevation;

                        //             found_bySize[_subCellSize].Add(subPoint);
                        //             visited.Add(currentLookup);

                        //             // previousElevation = newElevation;
                        //             added++;
                        //         }
                        //     }
                        //     continue;
                        // }

                        if (VectorUtil.DistanceXZ(currentHead, neighbor) < (_cellSize * 3f))
                        {
                            neighbor.y = currentHead.y;

                            headsToCheck.Add(neighbor);
                            found_bySize[_cellSize].Add(neighbor);
                            visited.Add(currentLookup);
                            continue;
                        }


                        int maxOffset = (cellLayerElevation * offsetMult);
                        if (diff > maxOffset) newElevation = (newElevation > baseElevation) ? (baseElevation + cellLayerElevation) : (baseElevation);
                        // if (diff > maxOffset) newElevation = (newElevation > baseElevation) ? (baseElevation + cellLayerElevation) : (baseElevation - cellLayerElevation);

                        neighbor.y = newElevation;

                        headsToCheck.Add(neighbor);
                        found_bySize[_cellSize].Add(neighbor);
                        visited.Add(currentLookup);

                        if (lowestHighestPointHeight.x > newElevation)
                        {
                            lowestHighestPointHeight.x = newElevation;
                        }
                        else if (lowestHighestPointHeight.y < newElevation) lowestHighestPointHeight.y = newElevation;
                    }
                }

                if (found_bySize[_cellSize].Count < maxMembers)
                {
                    if (headsToCheck.Count > 0)
                    {
                        currentHead = headsToCheck[0];
                        headsToCheck.Remove(currentHead);
                    }
                    else break;
                }
                attempts--;
            }
            return (found_bySize, lowestHighestPointHeight);
        }


        public static (List<Vector3>, Vector2Int) GetConsecutiveCellPointsWithintNoiseElevationRange(
            Vector3 initialCenterPostion,
            int maxMembers,
            List<LayeredNoiseOption> layerdNoises_terrain,
            HexCellSizes cellSize,
            float globalTerrainHeight,
            float globalElevation,
            int cellLayerElevation = 3,
            int offsetMult = 2,
            float mutateRangeMult = 3
        )
        {
            int _cellSize = (int)cellSize;

            Vector3 currentHead = initialCenterPostion;
            Vector2 currentLookup = HexCoreUtil.Calculate_CenterLookup(currentHead, _cellSize);

            List<Vector3> found = new List<Vector3>();
            HashSet<Vector2> visited = new HashSet<Vector2>();

            List<Vector3> headsToCheck = new List<Vector3>();

            float baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)currentHead.x, (int)currentHead.z, globalTerrainHeight, layerdNoises_terrain);
            baseNoiseHeight += globalElevation;
            int baseElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(baseNoiseHeight, cellLayerElevation);

            currentHead.y = baseElevation;
            found.Add(currentHead);
            visited.Add(currentLookup);
            Vector2Int lowestHighestPointHeight = new Vector2Int(baseElevation, baseElevation);

            int attempts = 999;

            while (currentHead != Vector3.positiveInfinity && found.Count < maxMembers && attempts > 0)
            {
                List<Vector3> neighborPoints = HexCoreUtil.GenerateHexCenterPoints_X12(currentHead, _cellSize);

                if (neighborPoints.Count > 0)
                {
                    for (int i = 0; i < neighborPoints.Count; i++)
                    {
                        if (found.Count >= maxMembers) break;

                        Vector3 neighbor = neighborPoints[i];

                        currentLookup = HexCoreUtil.Calculate_CenterLookup(neighbor, _cellSize);
                        if (visited.Contains(currentLookup)) continue;

                        float newNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)neighbor.x, (int)neighbor.z, globalTerrainHeight, layerdNoises_terrain);
                        newNoiseHeight += globalElevation;

                        int newElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(newNoiseHeight, cellLayerElevation);

                        int diff = (newElevation > baseElevation) ? newElevation - baseElevation : baseElevation - newElevation;

                        if (diff > (cellLayerElevation * mutateRangeMult)) continue;

                        int maxOffset = (cellLayerElevation * offsetMult);
                        if (diff > maxOffset) newElevation = (newElevation > baseElevation) ? (baseElevation + maxOffset) : (baseElevation - maxOffset);

                        neighbor.y = newElevation;

                        headsToCheck.Add(neighbor);
                        found.Add(neighbor);
                        visited.Add(currentLookup);

                        if (lowestHighestPointHeight.x > newElevation)
                        {
                            lowestHighestPointHeight.x = newElevation;
                        }
                        else if (lowestHighestPointHeight.y < newElevation) lowestHighestPointHeight.y = newElevation;
                    }
                }

                if (found.Count < maxMembers)
                {
                    if (headsToCheck.Count > 0)
                    {
                        currentHead = headsToCheck[0];
                        headsToCheck.Remove(currentHead);
                    }
                    else break;
                }
                attempts--;
            }
            return (found, lowestHighestPointHeight);
        }



        private static void Rehydrate_WorldSpaceNeighbors(
            Vector2 areaLookup,
            Vector2 worldspaceLookup,
            Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> worldSpaces_ByArea,
            bool propagateToNeighbors
        )
        {
            if (worldSpaces_ByArea[areaLookup].ContainsKey(worldspaceLookup) && worldSpaces_ByArea[areaLookup][worldspaceLookup].neighborWorldData != null)
            {
                foreach (CellWorldData neighborData in worldSpaces_ByArea[areaLookup][worldspaceLookup].neighborWorldData)
                {
                    Vector2 neighborParentLookup = neighborData.parentLookup;
                    Vector2 neighborLookup = neighborData.lookup;
                    if (worldSpaces_ByArea.ContainsKey(neighborParentLookup) == false) continue;

                    if (worldSpaces_ByArea[neighborParentLookup].ContainsKey(neighborLookup) == false) continue;

                    HexagonCellPrototype neighborCell = worldSpaces_ByArea[neighborParentLookup][neighborLookup];
                    if (neighborCell != null && worldSpaces_ByArea[areaLookup][worldspaceLookup].neighbors.Contains(neighborCell) == false)
                    {
                        worldSpaces_ByArea[areaLookup][worldspaceLookup].neighbors.Add(neighborCell);
                        if (propagateToNeighbors) Rehydrate_WorldSpaceNeighbors(neighborParentLookup, neighborLookup, worldSpaces_ByArea, false);
                    }
                }
            }
        }


        public static bool CheckKeyOrApproximateValue(Vector2 key, Dictionary<Vector2, HexagonCellPrototype> dict, float offset, int steps)
        {
            // Check if the exact key is present in the dictionary
            if (dict.ContainsKey(key)) return true;
            // Check for approximate values within the given offset and steps
            for (int i = -steps; i <= steps; i++)
            {
                for (int j = -steps; j <= steps; j++)
                {
                    Vector2 approxKey = new Vector2(key.x + (offset * i), key.y + (offset * j));
                    if (dict.ContainsKey(approxKey)) return true;
                }
            }
            return false;
        }
        public static Vector2 FindKeyOrApproximateValue(Vector2 key, Dictionary<Vector2, HexagonCellPrototype> dict, float offset, int steps)
        {
            // Check if the exact key is present in the dictionary
            if (dict.ContainsKey(key)) return key;
            // Check for approximate values within the given offset and steps
            for (int i = -steps; i <= steps; i++)
            {
                for (int j = -steps; j <= steps; j++)
                {
                    Vector2 approxKey = new Vector2(key.x + (offset * i), key.y + (offset * j));
                    if (dict.ContainsKey(approxKey)) return approxKey;
                }
            }
            return Vector2.positiveInfinity;
        }
        public static (Vector2, Vector2) FindKeyOrApproximateValue_WithOffset(Vector2 key, Dictionary<Vector2, HexagonCellPrototype> dict, float offset, int steps)
        {
            // Check if the exact key is present in the dictionary
            if (dict.ContainsKey(key)) return (key, Vector2.zero);
            // Check for approximate values within the given offset and steps
            for (int i = -steps; i <= steps; i++)
            {
                for (int j = -steps; j <= steps; j++)
                {
                    Vector2 approxKey = new Vector2(key.x + (offset * i), key.y + (offset * j));
                    if (dict.ContainsKey(approxKey)) return (approxKey, new Vector2((offset * i), (offset * j)));
                }
            }
            return (Vector2.positiveInfinity, Vector2.positiveInfinity);
        }



        public static List<Dictionary<Vector2, Vector3>> Extract_ConsecutiveLayerNeighbors(List<Vector3> points, HexCellSizes cellSize)
        {
            List<Dictionary<Vector2, Vector3>> islands = new List<Dictionary<Vector2, Vector3>>();

            int attempts = 999;
            while (points.Count > 0 && attempts > 0)
            {
                (Dictionary<Vector2, Vector3> newisland, List<Vector3> leftOver) = GetConsecutiveLayerNeighbors(points, cellSize);
                islands.Add(newisland);

                if (leftOver.Count == 0) break;
                points = leftOver;
            }

            return islands;
        }

        public static List<Vector3> Normalize_ConsecutiveLayerPoints(
            List<Vector3> points,
            // Dictionary<Vector2, Vector3> pointsByLookup,
            HexCellSizes cellSize,
            List<LayeredNoiseOption> layerdNoises_terrain,
            float globalTerrainHeight,
            float globalElevation,
            int cellLayerElevation = 3
        )
        {
            List<Vector3> allIslandpoints = new List<Vector3>();
            int attempts = 999;
            while (points.Count > 0 && attempts > 0)
            {
                (List<Vector3> found, List<Vector3> leftOver) = Normalize_ConsecutiveLayerNeighbors(
                    points,
                    // pointsByLookup,

                    cellSize,
                    layerdNoises_terrain,
                    globalTerrainHeight,
                    globalElevation,
                    cellLayerElevation = 3
                );

                allIslandpoints.AddRange(found);

                if (leftOver.Count == 0) break;
                points = leftOver;
            }

            return allIslandpoints;
        }

        public static (List<Vector3>, List<Vector3>) Normalize_ConsecutiveLayerNeighbors(
            List<Vector3> points,
            // Dictionary<Vector2, Vector3> pointsByLookup,
            HexCellSizes cellSize,
            List<LayeredNoiseOption> layerdNoises_terrain,
            float globalTerrainHeight,
            float globalElevation,
            int cellLayerElevation = 3
        )
        {
            int _cellSize = (int)cellSize;

            List<Dictionary<Vector2, Vector3>> islandClusters = new List<Dictionary<Vector2, Vector3>>() {
                new Dictionary<Vector2, Vector3>()
            };

            Vector3 currentHead = Vector3.positiveInfinity;
            Vector2 currentHeadLookup = Vector2.positiveInfinity;

            foreach (var item in points)
            {
                currentHead = item;
                currentHeadLookup = HexCoreUtil.Calculate_CenterLookup(currentHead, _cellSize);
                break;
            }
            if (currentHead == Vector3.positiveInfinity) return (null, points);

            float baseNoiseHeight = globalElevation + LayerdNoise.Calculate_NoiseHeightForCoordinate((int)currentHead.x, (int)currentHead.z, globalTerrainHeight, layerdNoises_terrain);
            int baseElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(baseNoiseHeight, cellLayerElevation);

            int initialY = (int)currentHead.y;
            currentHead.y = baseElevation;

            // pointsByLookup[currentHeadLookup] = currentHead;

            HashSet<Vector2> visited = new HashSet<Vector2>(){
                    currentHeadLookup
            };
            List<Vector3> found = new List<Vector3>(){
                    currentHead
            };

            List<Vector3> headsToCheck = new List<Vector3>() {
                currentHead
            };
            List<Vector3> leftOver = new List<Vector3>();

            int attempts = 999;

            while (currentHead != Vector3.positiveInfinity && headsToCheck.Count > 0 && attempts > 0)
            {
                headsToCheck.Remove(currentHead);

                List<Vector3> neighborPoints = HexCoreUtil.GenerateHexCenterPoints_X6(currentHead, _cellSize);
                for (int i = 0; i < neighborPoints.Count; i++)
                {
                    Vector3 neighbor = neighborPoints[i];
                    Vector2 neighborLookup = HexCoreUtil.Calculate_CenterLookup(neighbor, _cellSize);

                    if (visited.Contains(neighborLookup)) continue;
                    visited.Add(neighborLookup);

                    if ((int)neighbor.y == initialY)
                    {
                        neighbor.y = baseElevation;
                        // pointsByLookup[neighborLookup] = neighbor;

                        found.Add(neighbor);
                        headsToCheck.Add(neighbor);
                    }
                    else
                    {
                        leftOver.Add(neighbor);
                    }
                }

                if (headsToCheck.Count > 0)
                {
                    currentHead = headsToCheck[0];
                }
                else break;

                attempts--;
            }
            return (found, leftOver);
        }

        public static (Dictionary<Vector2, Vector3>, List<Vector3>) GetConsecutiveLayerNeighbors(List<Vector3> points, HexCellSizes cellSize)
        {
            int _cellSize = (int)cellSize;


            List<Dictionary<Vector2, Vector3>> islandClusters = new List<Dictionary<Vector2, Vector3>>() {
                new Dictionary<Vector2, Vector3>()
            };

            Vector3 currentHead = Vector3.positiveInfinity;
            Vector2 currentHeadLookup = Vector2.positiveInfinity;

            foreach (var item in points)
            {
                currentHead = item;
                currentHeadLookup = HexCoreUtil.Calculate_CenterLookup(currentHead, _cellSize);
                break;
            }
            if (currentHead == Vector3.positiveInfinity) return (null, points);

            HashSet<Vector2> visited = new HashSet<Vector2>(){
                    currentHeadLookup
            };
            Dictionary<Vector2, Vector3> found = new Dictionary<Vector2, Vector3>(){
                    { currentHeadLookup, currentHead }
            };
            List<Vector3> headsToCheck = new List<Vector3>();
            List<Vector3> leftOver = new List<Vector3>();

            int attempts = 999;

            while (currentHead != Vector3.positiveInfinity && headsToCheck.Count > 0 && attempts > 0)
            {
                List<Vector3> neighborPoints = HexCoreUtil.GenerateHexCenterPoints_X6(currentHead, _cellSize);
                foreach (var neighbor in neighborPoints)
                {
                    Vector2 neighborLookup = HexCoreUtil.Calculate_CenterLookup(neighbor, _cellSize);

                    if (visited.Contains(neighborLookup)) continue;
                    visited.Add(neighborLookup);

                    if (neighbor.y == currentHead.y)
                    {
                        found.Add(neighborLookup, neighbor);
                        headsToCheck.Add(neighbor);
                    }
                    else
                    {
                        leftOver.Add(neighbor);
                    }
                }

                if (headsToCheck.Count > 0)
                {
                    currentHead = headsToCheck[0];
                    headsToCheck.Remove(currentHead);
                }
                else break;

                attempts--;
            }
            return (found, leftOver);
        }

        public static List<HexagonCellPrototype> GetConsecutiveNeighborsFromStartCell(
            HexagonCellPrototype startCell,
            Dictionary<Vector2, HexagonCellPrototype> cells_ByLookup,
            int maxMembers
        )
        {
            HashSet<string> visitedHeads = new HashSet<string>();
            List<HexagonCellPrototype> found = new List<HexagonCellPrototype>();
            HexagonCellPrototype currentHead = startCell;
            int attempts = 999;

            if (found.Contains(startCell) == false) found.Add(startCell);
            visitedHeads.Add(startCell.GetId());

            while (currentHead != null && found.Count < maxMembers && attempts > 0)
            {
                if (currentHead.neighbors.Count == 0) Rehydrate_CellNeighbors(currentHead.GetLookup(), cells_ByLookup, true);

                List<HexagonCellPrototype> shuffledNeighbors = currentHead.neighbors.FindAll(n => n.IsSameLayer(currentHead));
                HexagonCellPrototype.Shuffle(shuffledNeighbors);

                foreach (var neighbor in shuffledNeighbors)
                {
                    if (found.Count >= maxMembers) break;
                    if (found.Contains(neighbor) == false) found.Add(neighbor);
                }

                visitedHeads.Add(currentHead.uid);

                if (found.Count < maxMembers)
                {
                    if (found.Count > 0)
                    {
                        currentHead = found.Find(c => visitedHeads.Contains(c.uid) == false);
                    }
                    else break;
                }
                attempts--;
            }
            return found;
        }

        public static List<HexagonCellPrototype> GetConsecutiveInactiveWorldSpaceNeighbors(
            HexagonCellPrototype startCell,
            Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> cells_ByParentLookup,
            int maxMembers
        )
        {
            HashSet<string> visitedHeads = new HashSet<string>();
            List<HexagonCellPrototype> found = new List<HexagonCellPrototype>();
            HexagonCellPrototype currentHead = startCell;
            int attempts = 999;

            if (found.Contains(startCell) == false && startCell.isWorldSpaceCellInitialized == false) found.Add(startCell);
            visitedHeads.Add(startCell.GetId());

            while (currentHead != null && found.Count < maxMembers && attempts > 0)
            {
                if (currentHead.neighbors.Count == 0) Rehydrate_CellNeighbors(currentHead.GetParentLookup(), currentHead.GetLookup(), cells_ByParentLookup, true);

                List<HexagonCellPrototype> shuffledNeighbors = currentHead.neighbors.FindAll(n => n.IsSameLayer(currentHead) && n.isWorldSpaceCellInitialized == false);

                if (maxMembers > 2) HexagonCellPrototype.Shuffle(shuffledNeighbors);

                foreach (var neighbor in shuffledNeighbors)
                {
                    if (found.Count >= maxMembers) break;
                    if (found.Contains(neighbor) == false) found.Add(neighbor);
                }

                visitedHeads.Add(currentHead.uid);

                if (found.Count < maxMembers)
                {
                    if (found.Count > 0)
                    {
                        currentHead = found.Find(c => visitedHeads.Contains(c.uid) == false);
                    }
                    else break;
                }
                attempts--;
            }
            return found;
        }



        public static List<HexagonCellPrototype> GetConsecutiveNeighborsFromStartCell(HexagonCellPrototype startCell, int maxMembers)
        {
            HashSet<string> visitedHeads = new HashSet<string>();
            List<HexagonCellPrototype> found = new List<HexagonCellPrototype>();
            HexagonCellPrototype currentHead = startCell;
            int attempts = 999;

            if (found.Contains(startCell) == false) found.Add(startCell);
            visitedHeads.Add(startCell.GetId());

            while (currentHead != null && found.Count < maxMembers && attempts > 0)
            {
                List<HexagonCellPrototype> shuffledNeighbors = currentHead.neighbors.FindAll(n => n.IsSameLayer(currentHead));
                HexagonCellPrototype.Shuffle(shuffledNeighbors);

                foreach (var neighbor in shuffledNeighbors)
                {
                    if (found.Contains(neighbor) == false) found.Add(neighbor);
                }

                visitedHeads.Add(currentHead.GetId());

                if (found.Count < maxMembers)
                {
                    if (found.Count > 0)
                    {
                        currentHead = found.Find(c => visitedHeads.Contains(c.GetId()) == false);
                    }
                    else break;
                }

                attempts--;
            }
            return found;
        }


        #region Searching 

        #endregion

        public static List<HexagonCellPrototype> FindPathToCell(HexagonCellPrototype start,
            HexagonCellPrototype end,
            bool ignoreEdgeCells,
            bool startCellIgnoresLayeredNeighbors = true,
            bool terminateAtFirstPathCell = false
        )
        {
            return RecursiveSearch(start, end, CellSearchPriority.SideNeighbors);
        }


        public static List<HexagonCellPrototype> RecursiveSearch(HexagonCellPrototype start, HexagonCellPrototype goal, CellSearchPriority searchPriority, List<CellStatus> ignoresStatus = null)
        {
            HashSet<string> visited = new HashSet<string>();
            Dictionary<string, HexagonCellPrototype> parentMap = new Dictionary<string, HexagonCellPrototype>();
            List<HexagonCellPrototype> path = new List<HexagonCellPrototype>();

            if (ignoresStatus == null)
            {
                ignoresStatus = new List<CellStatus>() {
                        CellStatus.Remove
                    };
            }

            RecursivelySearchNeighbors(start, goal, path, visited, ignoresStatus, searchPriority, parentMap);

            if (!parentMap.ContainsKey(goal.id))
            {
                // Goal cell was not reached, no path found
                return new List<HexagonCellPrototype>();
            }

            // Build path by following parent map from goal cell back to start cell
            HexagonCellPrototype current = goal;
            while (current != start)
            {
                path.Add(current);
                current = parentMap[current.id];
            }
            path.Add(start);
            path.Reverse();

            return path;
        }

        private static bool RecursivelySearchNeighbors(HexagonCellPrototype current, HexagonCellPrototype goal, List<HexagonCellPrototype> path, HashSet<string> visited, List<CellStatus> ignoresStatus, CellSearchPriority searchPriority, Dictionary<string, HexagonCellPrototype> parentMap)
        {
            visited.Add(current.id);

            if (current == goal)
            {
                path.Add(current);
                return true;
            }

            bool found = false;
            List<HexagonCellPrototype> sortedNeighbors = EvaluateNeighborSearchPriority(current, searchPriority);

            // List<HexagonCellPrototype> neighborsList;
            // if (searchPriority != CellSearchPriority.None)
            // {

            //     neighborsList = new List<HexagonCellPrototype>();

            //     if (searchPriority == CellSearchPriority.SideNeighbors)
            //     {
            //         neighborsList.AddRange(current.neighbors.FindAll(n => n.IsSameLayer(current)).OrderByDescending(n => n.neighbors.Count));
            //         neighborsList.AddRange(current.layerNeighbors.ToList().FindAll(n => n != null));
            //     }
            //     else if (searchPriority == CellSearchPriority.LayerNeighbors)
            //     {
            //         neighborsList.AddRange(current.layerNeighbors.ToList().FindAll(n => n != null));
            //         neighborsList.AddRange(current.neighbors.FindAll(n => n.IsSameLayer(current)).OrderByDescending(n => n.neighbors.Count));
            //     }
            //     else if (searchPriority == CellSearchPriority.SideAndSideLayerNeighbors)
            //     {
            //         //Add side neighbors
            //         neighborsList.AddRange(current.neighbors.FindAll(n => n != null && n.IsSameLayer(current)));
            //         for (int i = 0; i < neighborsList.Count; i++)
            //         {
            //             //Add side neighbors' layer neighbors
            //             if (neighborsList[i].layerNeighbors[0] != null) neighborsList.Add(neighborsList[i].layerNeighbors[0]);
            //             if (neighborsList[i].layerNeighbors[1] != null) neighborsList.Add(neighborsList[i].layerNeighbors[1]);
            //         }
            //         neighborsList = neighborsList.OrderByDescending(n => n.neighbors.Count).ToList();
            //     }
            // }
            // else
            // {
            //     neighborsList = current.neighbors.OrderByDescending(n => n.neighbors.Count).ToList();
            // }

            for (int i = 0; i < sortedNeighbors.Count; i++)
            {
                HexagonCellPrototype neighbor = sortedNeighbors[i];

                if (ignoresStatus.Contains(neighbor.GetCellStatus()))
                {
                    visited.Add(neighbor.id);
                    continue;
                }

                if (!visited.Contains(neighbor.id))
                {
                    found = RecursivelySearchNeighbors(neighbor, goal, path, visited, ignoresStatus, searchPriority, parentMap);
                    if (found)
                    {
                        path.Add(current);
                        parentMap[neighbor.id] = current;
                        return true;
                    }
                }
            }
            return false;
        }

        #region Consecutive Spread

        public static List<HexagonCellPrototype> GetTunnelPath(HexagonCellPrototype start, int maxMembers, CellSearchPriority searchPriority)
        {
            HashSet<string> visited = new HashSet<string>();
            List<HexagonCellPrototype> path = new List<HexagonCellPrototype>();

            List<CellStatus> ignoresStatus = new List<CellStatus>() {
                    CellStatus.Remove,
                    CellStatus.AboveGround,
                    CellStatus.GenericGround,
                    CellStatus.FlatGround,
                    CellStatus.Underwater,
                };

            path.Add(start);
            visited.Add(start.uid);

            RecursivelyFindNeighborsForTunnel(start.layerNeighbors[0], maxMembers, path, visited, ignoresStatus, searchPriority);

            return path;
        }

        private static void RecursivelyFindNeighborsForTunnel(
            HexagonCellPrototype current,
            int maxMembers,
            List<HexagonCellPrototype> path,
            HashSet<string> visited,
            List<CellStatus> ignoresStatus,
            CellSearchPriority searchPriority
        )
        {
            path.Add(current);
            visited.Add(current.uid);

            if (path.Count >= maxMembers) return;

            List<HexagonCellPrototype> sortedNeighbors = EvaluateNeighborSearchPriority(current, searchPriority);

            for (int i = 0; i < sortedNeighbors.Count; i++)
            {
                if (path.Count > maxMembers) break;

                HexagonCellPrototype neighbor = sortedNeighbors[i];
                HexagonCellPrototype neighborTopNeighbor = neighbor.layerNeighbors[1];
                if (
                    !neighbor.IsUnderGround() ||
                    // neighbor.IsPreAssigned() ||
                    // neighbor.HasPreassignedNeighbor() ||
                    neighborTopNeighbor == null
                )
                {
                    visited.Add(neighbor.uid);
                    continue;
                }

                // if (neighborTopNeighbor != null && (neighborTopNeighbor.IsPreAssigned() || !neighborTopNeighbor.IsUnderGround()))
                if (neighborTopNeighbor != null && !neighborTopNeighbor.IsUnderGround())
                {
                    visited.Add(neighborTopNeighbor.uid);
                    continue;
                }

                if (!visited.Contains(neighbor.uid))
                {
                    RecursivelyFindNeighborsForTunnel(neighbor, maxMembers, path, visited, ignoresStatus, searchPriority);
                }
            }
        }


        public static List<HexagonCellPrototype> GetConsecutiveNeighborsCluster(
            HexagonCellPrototype start,
            int maxMembers,
            CellSearchPriority searchPriority = CellSearchPriority.SideNeighbors,
            List<CellStatus> ignoresStatus = null,
            bool excludeOriginalGridEdge = false,
            bool edgeOnly = false
        )
        {
            HashSet<string> visited = new HashSet<string>();
            List<HexagonCellPrototype> cluster = new List<HexagonCellPrototype>();

            if (ignoresStatus == null)
            {
                ignoresStatus = new List<CellStatus>() {
                    CellStatus.Remove,
                    CellStatus.AboveGround,
                    CellStatus.UnderGround,
                    CellStatus.Underwater,
                };
            }

            RecursivelyFindNeighborsForCluster(start, maxMembers, cluster, visited, ignoresStatus, searchPriority, excludeOriginalGridEdge);

            return cluster;
        }

        private static void RecursivelyFindNeighborsForCluster(HexagonCellPrototype current,
            int maxMembers,
            List<HexagonCellPrototype> cluster,
            HashSet<string> visited,
            List<CellStatus> ignoresStatus,
            CellSearchPriority searchPriority = CellSearchPriority.SideNeighbors,
            bool excludeOriginalGridEdge = true
        )
        {
            cluster.Add(current);
            visited.Add(current.id);

            if (cluster.Count >= maxMembers) return;

            List<HexagonCellPrototype> sortedNeighbors = EvaluateNeighborSearchPriority(current, searchPriority);

            for (int i = 0; i < sortedNeighbors.Count; i++)
            {
                if (cluster.Count > maxMembers) break;

                HexagonCellPrototype neighbor = sortedNeighbors[i];

                if (excludeOriginalGridEdge && neighbor.IsOriginalGridEdge())
                {
                    visited.Add(neighbor.id);
                    continue;
                }

                if (
                    neighbor.IsPreAssigned() ||
                    neighbor.HasPreassignedNeighbor() ||
                    ignoresStatus.Contains(neighbor.GetCellStatus()))
                {
                    visited.Add(neighbor.id);
                    continue;
                }

                if (!visited.Contains(neighbor.id))
                {
                    RecursivelyFindNeighborsForCluster(neighbor, maxMembers, cluster, visited, ignoresStatus, searchPriority, excludeOriginalGridEdge);
                }
            }
        }


        public static List<HexagonCellPrototype> GetConsecutiveNeighborsList_WithinRadius(
            HexagonCellPrototype start,
            float radius,
            int maxMembers,
            CellSearchPriority searchPriority = CellSearchPriority.SideNeighbors,
            List<CellStatus> ignoresStatus = null,
            bool excludeOriginalGridEdge = false
        )
        {
            HashSet<string> visited = new HashSet<string>();
            List<HexagonCellPrototype> found = new List<HexagonCellPrototype>();

            if (ignoresStatus == null)
            {
                ignoresStatus = new List<CellStatus>() {
                    CellStatus.Remove,
                    CellStatus.AboveGround,
                    CellStatus.UnderGround,
                    CellStatus.Underwater,
                };
            }

            RecursivelyFindNeighbors_WithinRadius(start, radius, start, maxMembers, found, visited, ignoresStatus, searchPriority, excludeOriginalGridEdge);
            return found;
        }

        private static void RecursivelyFindNeighbors_WithinRadius(
            HexagonCellPrototype focus,
            float radius,
            HexagonCellPrototype current,
            int maxMembers,
            List<HexagonCellPrototype> found,
            HashSet<string> visited,
            List<CellStatus> ignoresStatus,
            CellSearchPriority searchPriority = CellSearchPriority.SideNeighbors,
            bool excludeOriginalGridEdge = false
        )
        {
            found.Add(current);
            visited.Add(current.id);

            if (found.Count >= maxMembers) return;

            List<HexagonCellPrototype> sortedNeighbors = EvaluateNeighborSearchPriority(current, searchPriority);

            for (int i = 0; i < sortedNeighbors.Count; i++)
            {
                if (found.Count > maxMembers) break;

                HexagonCellPrototype neighbor = sortedNeighbors[i];

                if (excludeOriginalGridEdge && neighbor.IsOriginalGridEdge())
                {
                    visited.Add(neighbor.id);
                    continue;
                }

                if (
                    VectorUtil.DistanceXZ(neighbor.center, focus.center) > radius ||
                    neighbor.IsPreAssigned() ||
                    neighbor.HasPreassignedNeighbor() ||
                    ignoresStatus.Contains(neighbor.GetCellStatus()))
                {
                    visited.Add(neighbor.id);
                    continue;
                }

                if (!visited.Contains(neighbor.id))
                {
                    RecursivelyFindNeighbors_WithinRadius(focus, radius, neighbor, maxMembers, found, visited, ignoresStatus, searchPriority, excludeOriginalGridEdge);
                }
            }
        }

        #endregion

        private static List<HexagonCellPrototype> EvaluateNeighborSearchPriority(HexagonCellPrototype current, CellSearchPriority searchPriority)
        {
            if (searchPriority != CellSearchPriority.None)
            {
                List<HexagonCellPrototype> neighborsList = new List<HexagonCellPrototype>();

                if (searchPriority == CellSearchPriority.SideNeighbors)
                {
                    neighborsList.AddRange(current.neighbors.FindAll(n => n != null && n.IsSameLayer(current)).OrderByDescending(n => n.neighbors.Count));
                    if (current.layerNeighbors[0] != null && neighborsList.Contains(current.layerNeighbors[0]) == false) neighborsList.Add(current.layerNeighbors[0]);
                    if (current.layerNeighbors[1] != null && neighborsList.Contains(current.layerNeighbors[1]) == false) neighborsList.Add(current.layerNeighbors[1]);
                    // neighborsList.AddRange(current.layerNeighbors.ToList().FindAll(n => n != null));
                }
                else if (searchPriority == CellSearchPriority.LayerNeighbors)
                {
                    if (current.layerNeighbors[0] != null && neighborsList.Contains(current.layerNeighbors[0]) == false) neighborsList.Add(current.layerNeighbors[0]);
                    if (current.layerNeighbors[1] != null && neighborsList.Contains(current.layerNeighbors[1]) == false) neighborsList.Add(current.layerNeighbors[1]);
                    // neighborsList.AddRange(current.layerNeighbors.ToList().FindAll(n => n != null));
                    neighborsList.AddRange(current.neighbors.FindAll(n => n != null && n.IsSameLayer(current)).OrderByDescending(n => n.neighbors.Count));
                }
                else if (searchPriority == CellSearchPriority.SideAndSideLayerNeighbors)
                {
                    //Add side neighbors
                    List<HexagonCellPrototype> sideNeighbors = current.neighbors.FindAll(n => n != null && n.IsSameLayer(current));
                    neighborsList.AddRange(sideNeighbors);

                    foreach (var sideNeighbor in sideNeighbors)
                    {
                        //Add side neighbors' layer neighbors
                        if (sideNeighbor.layerNeighbors[0] != null && neighborsList.Contains(sideNeighbor.layerNeighbors[0]) == false) neighborsList.Add(sideNeighbor.layerNeighbors[0]);
                        if (sideNeighbor.layerNeighbors[1] != null && neighborsList.Contains(sideNeighbor.layerNeighbors[1]) == false) neighborsList.Add(sideNeighbor.layerNeighbors[1]);
                    }

                    neighborsList = neighborsList.OrderByDescending(n => n.neighbors.Count).ToList();
                }
                return neighborsList;
            }
            else
            {
                return current.neighbors.OrderByDescending(n => n.neighbors.Count).ToList();
            }
        }


        public static int GetPathSideNeighborsCount(HexagonCellPrototype cell)
        {
            return cell.neighbors.FindAll(n => n.IsPath() && cell.IsSameLayer(n)).Count;
        }


        public static List<HexagonCellPrototype> ClearPathCellClumps(List<HexagonCellPrototype> pathCells)
        {
            List<HexagonCellPrototype> result = new List<HexagonCellPrototype>();
            List<HexagonCellPrototype> cleared = new List<HexagonCellPrototype>();

            foreach (HexagonCellPrototype cell in pathCells)
            {
                if (cell.IsInCluster() || cell.IsGround() == false && cell.layerNeighbors[0] != null && cell.layerNeighbors[0].IsPath())
                {
                    cell.SetPathCell(false);
                    cleared.Add(cell);
                    continue;
                }

                List<HexagonCellPrototype> pathNeighbors = cell.neighbors.FindAll(n => cleared.Contains(n) == false && n.layer == cell.layer && (n.IsPath() || pathCells.Contains(n)));
                if (pathNeighbors.Count >= 4)
                {
                    // bool neighborHasMultipleConnections = pathNeighbors.Any(n => n.neighbors.FindAll(n => pathNeighbors.Contains(n)).Count > 1);
                    // if (neighborHasMultipleConnections)
                    // {
                    cell.SetPathCell(false);
                    cleared.Add(cell);
                    // }
                    // else
                    // {
                    //     result.Add(cell);
                    // }
                }
                else
                {
                    result.Add(cell);
                }
            }

            return result;
        }


        public static List<HexagonCellPrototype> Generate_CityGridPaths(
            Bounds bounds,
            List<HexagonCellPrototype> allCityCells,
            float pointCenterRadius,
            bool assign,
            int excludeSideNeighborsCount,
            int columns = 44,
            int rows = 45,
            int lineDensity = 75
        )
        {
            List<Vector2> dottedGrid = VectorUtil.GenerateDottedGridLines(bounds, 44f, 45f, lineDensity);
            // List<Vector2> dottedGrid = VectorUtil.GenerateDottedGridLines(bounds, columns, rows, lineDensity);
            HashSet<string> added = new HashSet<string>();
            List<HexagonCellPrototype> path = new List<HexagonCellPrototype>();

            foreach (var cell in allCityCells)
            {
                if (cell.IsGridEdge() || cell.IsPath() || cell.IsEntry()) continue;

                if (cell.neighbors.Any(n => (n.IsEntry() && n.neighbors.Any(p => added.Contains(p.GetId()) == false))))
                {
                    if (added.Contains(cell.GetId()) == false)
                    {
                        path.Add(cell);
                        added.Add(cell.GetId());

                        if (assign) cell.SetPathCell(true);
                        cell.Highlight(true);
                        continue;
                    }
                }

                if (GetPathSideNeighborsCount(cell) > excludeSideNeighborsCount) continue;

                (Vector3 point, float dist) = VectorUtil.GetClosestPoint_XZ_WithDistance(dottedGrid, cell.center);
                if (point != Vector3.positiveInfinity && dist < pointCenterRadius)
                {
                    if (added.Contains(cell.GetId()) == false)
                    {
                        path.Add(cell);
                        added.Add(cell.GetId());

                        if (assign) cell.SetPathCell(true);
                        cell.Highlight(true);
                    }
                }
            }

            return path;
        }

        public static List<HexagonCellPrototype> GenerateRandomPath(List<HexagonCellPrototype> entryPrototypes, List<HexagonCellPrototype> allPrototypes, Vector3 position, bool ignoreEdgeCells = true)
        {
            HexagonCellPrototype centerPrototype = HexagonCellPrototype.GetClosestPrototypeXYZ(allPrototypes, position);
            List<HexagonCellPrototype> initialPath = FindPath(entryPrototypes[0], centerPrototype, ignoreEdgeCells, false);
            List<HexagonCellPrototype> islandOnRamps = allPrototypes.FindAll(c => c.isGroundRamp);

            // int paths = 0;
            for (int i = 1; i < entryPrototypes.Count; i++)
            {
                List<HexagonCellPrototype> newPathA = FindPath(entryPrototypes[i], centerPrototype, ignoreEdgeCells);
                if (newPathA != null) initialPath.AddRange(newPathA);

                List<HexagonCellPrototype> newPathB = FindPath(entryPrototypes[i], entryPrototypes[i - 1], ignoreEdgeCells);
                if (newPathB != null) initialPath.AddRange(newPathB);
            }

            // Debug.Log("GenerateRandomPath - allPrototypes: " + allPrototypes.Count + ", centerPrototype: " + centerPrototype.id);
            foreach (HexagonCellPrototype ramp in islandOnRamps)
            {
                List<HexagonCellPrototype> newPathA = FindPath(ramp, entryPrototypes[UnityEngine.Random.Range(0, entryPrototypes.Count)], ignoreEdgeCells);
                if (newPathA != null) initialPath.AddRange(newPathA);

                ramp.SetPathCell(true);
                if (initialPath.Contains(ramp) == false) initialPath.Add(ramp);
            }

            List<HexagonCellPrototype> finalPath = new List<HexagonCellPrototype>();

            List<HexagonCellPrototype> invalids = initialPath.FindAll(r => r.IsGround() == false);
            Debug.Log("GenerateRandomPath - invalids: " + invalids.Count + ", initialPaths: " + initialPath.Count);

            foreach (HexagonCellPrototype item in initialPath)
            {
                if (item.IsDisposable() || item.IsGround() == false)
                {
                    HexagonCellPrototype groundLayerNeighbor = HexagonCellPrototype.GetGroundLayerNeighbor(item);
                    if (groundLayerNeighbor != null && groundLayerNeighbor.IsGround())
                    {
                        finalPath.Add(groundLayerNeighbor);
                        groundLayerNeighbor.SetPathCell(true);
                    }
                    else
                    {
                        finalPath.Add(item);
                        item.SetPathCell(true);
                    }
                }
                else
                {
                    finalPath.Add(item);
                    item.SetPathCell(true);
                }
            }

            // return finalPath;
            return ClearPathCellClumps(finalPath);
        }



        public static List<HexagonCellPrototype> FindPath(HexagonCellPrototype startCell, HexagonCellPrototype endCell, bool ignoreEdgeCells, bool startCellIgnoresLayeredNeighbors = true, bool terminateAtFirstPathCell = false)
        {
            // Create a queue to store the cells to be visited
            Queue<HexagonCellPrototype> queue = new Queue<HexagonCellPrototype>();

            // Create a dictionary to store the parent of each cell
            Dictionary<string, HexagonCellPrototype> parent = new Dictionary<string, HexagonCellPrototype>();

            // Create a set to store the visited cells
            HashSet<string> visited = new HashSet<string>();

            // Enqueue the start cell and mark it as visited
            queue.Enqueue(startCell);
            visited.Add(startCell.id);

            // Get an inner neighbor of endCell if it is on the edge 
            if (ignoreEdgeCells && (endCell.IsEdge() || endCell.IsEntry()))
            {
                HexagonCellPrototype newEndCell = endCell.neighbors.Find(n => n.layer == endCell.layer && !n.IsEdge() && !n.IsEntry());
                if (newEndCell != null) endCell = newEndCell;
            }

            // Run the BFS loop
            while (queue.Count > 0)
            {
                HexagonCellPrototype currentCell = queue.Dequeue();

                // Check if the current cell is the end cell
                if (currentCell.id == endCell.id || (terminateAtFirstPathCell && currentCell.IsPath()))
                {
                    // Create a list to store the path
                    List<HexagonCellPrototype> path = new List<HexagonCellPrototype>();

                    // Trace back the path from the end cell to the start cell
                    HexagonCellPrototype current = currentCell.IsPath() ? currentCell : endCell;
                    while (current.id != startCell.id)
                    {
                        path.Add(current);
                        current = parent[current.id];
                    }
                    path.Reverse();
                    return path;
                }

                // Enqueue the unvisited neighbors
                foreach (HexagonCellPrototype neighbor in currentCell.neighbors)
                {
                    if (!visited.Contains(neighbor.id))
                    {
                        visited.Add(neighbor.id);

                        if (neighbor.GetCellStatus() == CellStatus.Remove) continue;

                        if (ignoreEdgeCells && neighbor.IsEdge()) continue;

                        // if (neighbor.IsInCluster()) continue;

                        // If entry cell, dont use layered neighbors
                        // if (startCellIgnoresLayeredNeighbors)
                        // {
                        //     if (currentCell == startCell && currentCell.layerNeighbors.Contains(neighbor)) continue;
                        // }

                        if (((currentCell == startCell && startCellIgnoresLayeredNeighbors) || currentCell.IsEntry()) && currentCell.layerNeighbors.Contains(neighbor)) continue;

                        //  Dont use layered neighbors if not ground
                        // if (currentCell.layerNeighbors[1] == neighbor && neighbor.IsGround() == false) continue;

                        queue.Enqueue(neighbor);
                        parent[neighbor.id] = currentCell;
                    }
                }
            }

            // If there is no path between the start and end cells
            return null;
        }


        public static List<HexagonCellPrototype> GetConsecutiveNeighborsList(
            HexagonCellPrototype start,
            int maxMembers,
            List<HexagonCellPrototype> limitMembersToList,
            CellSearchPriority searchPriority = CellSearchPriority.SideNeighbors,
            List<CellStatus> ignoresStatus = null,
            HashSet<string> limitToLParentIds = null,
            bool excludeOriginalGridEdge = false,
            bool ignorePreassigned = false
        )
        {
            HashSet<string> visited = new HashSet<string>();
            List<HexagonCellPrototype> found = new List<HexagonCellPrototype>();

            if (ignoresStatus == null)
            {
                ignoresStatus = new List<CellStatus>() {
                    CellStatus.Remove,
                    CellStatus.AboveGround,
                    CellStatus.UnderGround,
                    CellStatus.Underwater,
                };
            }

            RecursivelyFindNeighborsForList(start, maxMembers, found, visited, ignoresStatus, searchPriority, excludeOriginalGridEdge, limitMembersToList, limitToLParentIds, ignorePreassigned);
            return found;
        }

        private static void RecursivelyFindNeighborsForList(
            HexagonCellPrototype current,
            int maxMembers,
            List<HexagonCellPrototype> found,
            HashSet<string> visited,
            List<CellStatus> ignoresStatus,
            CellSearchPriority searchPriority = CellSearchPriority.SideNeighbors,
            bool excludeOriginalGridEdge = true,
            List<HexagonCellPrototype> limitMembersToList = null,
            HashSet<string> limitToLParentIds = null,
            bool ignorePreassigned = false
        )
        {
            found.Add(current);
            visited.Add(current.id);

            if (found.Count >= maxMembers) return;

            List<HexagonCellPrototype> sortedNeighbors = EvaluateNeighborSearchPriority(current, searchPriority);

            for (int i = 0; i < sortedNeighbors.Count; i++)
            {
                if (found.Count > maxMembers) break;

                HexagonCellPrototype neighbor = sortedNeighbors[i];

                if (excludeOriginalGridEdge && neighbor.IsOriginalGridEdge())
                {
                    visited.Add(neighbor.id);
                    continue;
                }

                if (ignoresStatus.Contains(neighbor.GetCellStatus()))
                {
                    visited.Add(neighbor.id);
                    continue;
                }

                if (ignorePreassigned == false && (neighbor.IsPreAssigned() || neighbor.HasPreassignedNeighbor()))
                {
                    visited.Add(neighbor.id);
                    continue;
                }

                if ((limitMembersToList != null && limitMembersToList.Contains(neighbor) == false) || (limitToLParentIds != null && limitToLParentIds.Contains(neighbor.GetId()) == false))
                {
                    visited.Add(neighbor.id);
                    continue;
                }

                if (!visited.Contains(neighbor.id))
                {
                    RecursivelyFindNeighborsForList(neighbor, maxMembers, found, visited, ignoresStatus, searchPriority, excludeOriginalGridEdge, limitMembersToList, limitToLParentIds, ignorePreassigned);
                }
            }
        }


        // public static List<HexagonCellPrototype> GenerateRandomPathBetweenCells(List<HexagonCellPrototype> pathFocusCells, bool ignoreEdgeCells = false, bool allowNonGroundCells = false)
        // {
        //     List<HexagonCellPrototype> initialPath = new List<HexagonCellPrototype>();

        //     for (int i = 0; i < pathFocusCells.Count; i++)
        //     {
        //         for (int j = 1; j < pathFocusCells.Count; j++)
        //         {
        //             List<HexagonCellPrototype> newPathA = FindPath(pathFocusCells[i], pathFocusCells[j], ignoreEdgeCells);
        //             if (newPathA != null) initialPath.AddRange(newPathA);
        //         }
        //     }

        //     // Exclude pathFocusCells from final path
        //     initialPath = initialPath.FindAll(c => c.IsPreAssigned() == false);

        //     List<HexagonCellPrototype> finalPath = new List<HexagonCellPrototype>();
        //     // List<HexagonCellPrototype> invalids = result.FindAll(r => r.IsGround() == false);
        //     // Debug.Log("GenerateRandomPath - invalids: " + invalids.Count + ", results: " + result.Count);

        //     foreach (HexagonCellPrototype item in initialPath)
        //     {
        //         if (item.IsDisposable() || (allowNonGroundCells == false && item.IsGround() == false) || (allowNonGroundCells && !item.IsGround() && !item.HasGroundNeighbor()))
        //         {
        //             HexagonCellPrototype groundLayerNeighbor = GetGroundCellInLayerStack(item);
        //             // HexagonCellPrototype groundLayerNeighbor = GetGroundLayerNeighbor(item);
        //             if (groundLayerNeighbor != null && groundLayerNeighbor.IsGround())
        //             {
        //                 if (item.IsPreAssigned() == false)
        //                 {
        //                     finalPath.Add(groundLayerNeighbor);
        //                     groundLayerNeighbor.SetPathCell(true);
        //                 }
        //             }
        //             else
        //             {
        //                 finalPath.Add(item);
        //                 item.SetPathCell(true);
        //             }
        //         }
        //         else
        //         {
        //             finalPath.Add(item);
        //             item.SetPathCell(true);
        //         }
        //     }

        //     // return finalPath;
        //     return ClearPathCellClumps(finalPath);
        // }
    }

}
