using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ProceduralBase;

namespace WFCSystem
{
    public static class HexGridUtil
    {

        public static Dictionary<Vector2, Vector3> Generate_HexGridCenterPointsWithinHosts(List<Vector3> hostPoints, int _cellSize, int _hostCellSize, bool removePartialChildren)
        {
            // int _cellSize = (int)cellSize;
            // int _hostCellSize = (int)hostcellSize;

            Dictionary<Vector2, Vector3> found_byLookup = new Dictionary<Vector2, Vector3>();
            HashSet<Vector2> visited = new HashSet<Vector2>();
            List<Vector2> pointsLookups = removePartialChildren ? new List<Vector2>() : null;

            foreach (Vector3 host in hostPoints)
            {
                Vector3[] radiusCorners = HexCoreUtil.GenerateHexagonPoints(host, _hostCellSize);

                List<Vector3> neighborPoints = HexCoreUtil.GenerateHexCenterPoints_X13(host, _cellSize);
                foreach (var neighbor in neighborPoints)
                {
                    Vector2 neighborLookup = HexCoreUtil.Calculate_CenterLookup(neighbor, _cellSize);

                    if (visited.Contains(neighborLookup)) continue;
                    visited.Add(neighborLookup);

                    if (HexCoreUtil.IsAnyHexPointWithinPolygon(neighbor, _cellSize, radiusCorners) == false) continue;

                    found_byLookup.Add(neighborLookup, neighbor);
                    if (removePartialChildren) pointsLookups.Add(neighborLookup);
                }
            }

            if (removePartialChildren)
            {
                foreach (var lookup in pointsLookups)
                {
                    int neighborsfound = GetHexCenterNeighborsInCollection(found_byLookup[lookup], _cellSize, found_byLookup);
                    if (neighborsfound < 3) found_byLookup.Remove(lookup);
                }
            }

            return found_byLookup;
        }


        public static Dictionary<int, Dictionary<Vector2, Vector3>> Generate_HexGridCenterPoints_X7(Vector3 center, int size)
        {
            Vector3[] cornerPoints = HexCoreUtil.GenerateHexagonPoints(center, size);
            Dictionary<int, Dictionary<Vector2, Vector3>> centerPointsByLookup_BySize = new Dictionary<int, Dictionary<Vector2, Vector3>>() {
                { size, new Dictionary<Vector2, Vector3>() {
                       { HexCoreUtil.Calculate_CenterLookup(center, size), center }
                    }
                }
            };
            for (int i = 0; i < 6; i++)
            {
                Vector3 sidePoint = Vector3.Lerp(cornerPoints[i], cornerPoints[(i + 1) % 6], 0.5f);
                Vector3 direction = (sidePoint - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));

                sidePoint = center + direction * (edgeDistance * 2f);
                centerPointsByLookup_BySize[size].Add(HexCoreUtil.Calculate_CenterLookup(sidePoint, size), sidePoint);
            }
            return centerPointsByLookup_BySize;
        }


        public static Dictionary<int, Dictionary<Vector2, Vector3>> Generate_RandomHexGridCenterPoints_BySize(
            Vector3 initialCenter,
            int _cellSize,
            int maxMembers,
            int gridRadiusMax,
            bool randomOrder,
            bool logResults = false
        )
        {
            // int _cellSize = (int)cellsize;
            Vector3 currentHead = initialCenter;
            Vector3 currentHeadLookup = HexCoreUtil.Calculate_CenterLookup(currentHead, _cellSize);

            Dictionary<int, Dictionary<Vector2, Vector3>> found_byLookup = new Dictionary<int, Dictionary<Vector2, Vector3>>() {
                {
                    _cellSize, new Dictionary<Vector2, Vector3>() {  {  currentHeadLookup, currentHead } }
                }
            };
            HashSet<Vector2> visited = new HashSet<Vector2>() {
                currentHeadLookup
            };
            List<Vector3> headsToCheck = new List<Vector3>();
            headsToCheck.AddRange(HexCoreUtil.Generate_RandomHexNeighborCenters(currentHead, (_cellSize * 3), maxMembers / 2, false));


            int attempts = 999;


            while (currentHead != Vector3.positiveInfinity && found_byLookup[_cellSize].Keys.Count < maxMembers && attempts > 0)
            {

                List<Vector3> neighborPoints = null;
                if (randomOrder)
                {
                    int r = UnityEngine.Random.Range(0, 2);
                    neighborPoints = r == 0 ? HexCoreUtil.GenerateHexCenterPoints_X3(currentHead, _cellSize) : HexCoreUtil.GenerateHexCenterPoints_X6(currentHead, _cellSize);
                }
                else neighborPoints = HexCoreUtil.GenerateHexCenterPoints_X6(currentHead, _cellSize);

                foreach (var neighbor in neighborPoints)
                {
                    // Debug.Log("headsToCheck:  " + headsToCheck.Count);
                    Vector2 neighborLookup = HexCoreUtil.Calculate_CenterLookup(neighbor, _cellSize);

                    if (visited.Contains(neighborLookup)) continue;
                    visited.Add(neighborLookup);

                    found_byLookup[_cellSize].Add(neighborLookup, neighbor);
                    headsToCheck.Add(neighbor);

                    if (found_byLookup[_cellSize].Keys.Count < maxMembers) break;
                }

                if (headsToCheck.Count > 0)
                {
                    currentHead = headsToCheck[0];
                    headsToCheck.Remove(currentHead);
                }
                else break;

                attempts--;
            }

            // Debug.Log("found_byLookup:  " + found_byLookup[_cellSize].Count);
            return found_byLookup;
        }

        public static Dictionary<int, Dictionary<Vector2, Vector3>> Generate_RandomHexGridCenterPoints_BySize(
            Vector3 initialCenter,
            HexCellSizes cellsize,
            int maxMembers,
            bool randomOrder,
            // int gridRadius,
            bool logResults = false
        )
        {
            int _cellSize = (int)cellsize;
            Vector3 currentHead = initialCenter;
            Vector3 currentHeadLookup = HexCoreUtil.Calculate_CenterLookup(currentHead, _cellSize);

            Dictionary<int, Dictionary<Vector2, Vector3>> found_byLookup = new Dictionary<int, Dictionary<Vector2, Vector3>>() {
                {
                    _cellSize, new Dictionary<Vector2, Vector3>() {  {  currentHeadLookup, currentHead } }
                }
            };
            HashSet<Vector2> visited = new HashSet<Vector2>() {
                currentHeadLookup
            };
            List<Vector3> headsToCheck = new List<Vector3>();

            int attempts = 999;


            while (currentHead != Vector3.positiveInfinity && found_byLookup[_cellSize].Keys.Count < maxMembers && attempts > 0)
            {

                List<Vector3> neighborPoints = null;
                if (randomOrder)
                {
                    int r = UnityEngine.Random.Range(0, 2);
                    neighborPoints = r == 0 ? HexCoreUtil.GenerateHexCenterPoints_X3(currentHead, _cellSize) : HexCoreUtil.GenerateHexCenterPoints_X6(currentHead, _cellSize);
                }
                else neighborPoints = HexCoreUtil.GenerateHexCenterPoints_X6(currentHead, _cellSize);

                foreach (var neighbor in neighborPoints)
                {
                    // Debug.Log("headsToCheck:  " + headsToCheck.Count);
                    Vector2 neighborLookup = HexCoreUtil.Calculate_CenterLookup(neighbor, _cellSize);

                    if (visited.Contains(neighborLookup)) continue;
                    visited.Add(neighborLookup);

                    found_byLookup[_cellSize].Add(neighborLookup, neighbor);
                    headsToCheck.Add(neighbor);

                    if (found_byLookup[_cellSize].Keys.Count < maxMembers) break;
                }

                if (headsToCheck.Count > 0)
                {
                    currentHead = headsToCheck[0];
                    headsToCheck.Remove(currentHead);
                }
                else break;

                attempts--;
            }

            // Debug.Log("found_byLookup:  " + found_byLookup[_cellSize].Count);
            return found_byLookup;
        }

        public static int GetHexCenterNeighborsInCollection(Vector3 point, int cellSize, Dictionary<Vector2, Vector3> pointsLookup)
        {
            List<Vector3> neighborPoints = HexCoreUtil.GenerateHexCenterPoints_X6(point, cellSize);
            int found = 0;
            foreach (var neighbor in neighborPoints)
            {
                Vector2 neighborLookup = HexCoreUtil.Calculate_CenterLookup(neighbor, cellSize);

                if (pointsLookup.ContainsKey(neighborLookup)) found++;
            }
            return found;
        }

        public static Dictionary<int, Dictionary<Vector2, Vector3>> Generate_RandomHexGridCenterPoints_BySize(
            Vector3 initialCenter,
            int _cellSize,
            Vector2Int hostsMinMax,
            int maxRadius,
            bool shuffle,
            bool removePartials = true,
            bool logResults = false
        )
        {
            // int _cellSize = (int)cellsize;
            int minRadius = (_cellSize * 3);
            int _hostCellSize = (_cellSize * 3);

            Vector3 currentHead = initialCenter;
            Vector3 currentHeadLookup = HexCoreUtil.Calculate_CenterLookup(currentHead, _hostCellSize);

            List<Vector3> hostPoints = new List<Vector3>() {
                currentHead,
            };

            Dictionary<int, Dictionary<Vector2, Vector3>> found_byLookup = new Dictionary<int, Dictionary<Vector2, Vector3>>() {
                {
                    _cellSize, new Dictionary<Vector2, Vector3>() {  {  currentHeadLookup, currentHead } }
                }
            };
            HashSet<Vector2> visited = new HashSet<Vector2>() {
                currentHeadLookup
            };

            // HashSet<Vector3> hostsAdded = new HashSet<Vector3>();
            // headsToCheck.AddRange(HexCoreUtil.Generate_RandomHexNeighborCenters(currentHead, (_cellSize * 3), maxMembers / 2, false));
            // List<Vector3>  new_hosts =  HexCoreUtil.Generate_RandomHexNeighborCenters(currentHead, _hostCellSize, hostsMinMax.y, shuffle);
            hostPoints.AddRange(HexCoreUtil.Generate_RandomHexNeighborCenters(currentHead, _hostCellSize, hostsMinMax.y, shuffle));
            // Dictionary<int, Dictionary<Vector2, Vector3>> new_cellCenters_ByLookup_BySize = HexGridUtil.Generate_RandomHexGridCenterPoints_BySize(initialCenter, (HexCellSizes)_hostCellSize, hostsMinMax.y, maxRadius, false);
            // foreach (var point in new_cellCenters_ByLookup_BySize[_hostCellSize].Values)
            // {
            //     hostPoints.Add(point);
            // }

            List<Vector2> pointsLookups = removePartials ? new List<Vector2>() : null;

            foreach (Vector3 host in hostPoints)
            {
                Vector3[] radiusCorners = HexCoreUtil.GenerateHexagonPoints(host, _hostCellSize);

                List<Vector3> neighborPoints = HexCoreUtil.GenerateHexCenterPoints_X13(host, _cellSize);
                foreach (var neighbor in neighborPoints)
                {
                    // Debug.Log("headsToCheck:  " + headsToCheck.Count);
                    Vector2 neighborLookup = HexCoreUtil.Calculate_CenterLookup(neighbor, _cellSize);

                    if (visited.Contains(neighborLookup)) continue;
                    visited.Add(neighborLookup);


                    if (HexCoreUtil.IsAnyHexPointWithinPolygon(neighbor, _cellSize, radiusCorners) == false) continue;

                    found_byLookup[_cellSize].Add(neighborLookup, neighbor);
                    if (removePartials) pointsLookups.Add(neighborLookup);
                }
            }

            if (removePartials)
            {
                foreach (var lookup in pointsLookups)
                {
                    int neighborsfound = GetHexCenterNeighborsInCollection(found_byLookup[_cellSize][lookup], _cellSize, found_byLookup[_cellSize]);
                    if (neighborsfound < 3) found_byLookup[_cellSize].Remove(lookup);
                }
            }

            return found_byLookup;
        }


        public static Dictionary<int, Dictionary<Vector2, Vector3>> Generate_HexGridCenterPoints_BySize(
            Vector3 initialCenter,
            float smallestSize,
            int gridRadius,
            bool logResults = false
        )
        {
            Dictionary<int, Dictionary<Vector2, Vector3>> centerPointsByLookup_BySize = new Dictionary<int, Dictionary<Vector2, Vector3>>();
            List<Vector3> divideCenterPoints = new List<Vector3>();
            List<Vector3> allNewCenterPoints = new List<Vector3>();

            int initialDivideSize = -1;
            int prevStepSize = gridRadius;
            int currentStepSize = gridRadius;

            while (smallestSize < currentStepSize)
            {
                currentStepSize = (prevStepSize / 3);

                List<Vector3> newCenterPoints = new List<Vector3>();

                if (divideCenterPoints.Count == 0)
                {
                    newCenterPoints = HexCoreUtil.GenerateHexCenterPoints_X13(initialCenter, currentStepSize);
                    initialDivideSize = currentStepSize;
                }
                else
                {
                    foreach (Vector3 centerPoint in divideCenterPoints)
                    {
                        newCenterPoints.AddRange(HexCoreUtil.GenerateHexCenterPoints_X13(centerPoint, currentStepSize));
                    }
                }

                // Debug.Log("GenerateHexGrid - allSoFar: " + allSoFar.Count + ", currentStepSize: " + currentStepSize);
                allNewCenterPoints.AddRange(newCenterPoints);
                centerPointsByLookup_BySize.Add(currentStepSize, new Dictionary<Vector2, Vector3>());

                foreach (Vector3 point in allNewCenterPoints)
                {
                    Vector2 lookup = HexCoreUtil.Calculate_CenterLookup(point, currentStepSize);
                    if (centerPointsByLookup_BySize[currentStepSize].ContainsKey(lookup) == false)
                    {
                        centerPointsByLookup_BySize[currentStepSize].Add(lookup, point);
                        if (smallestSize == currentStepSize)
                        {
                            List<Vector3> points = HexCoreUtil.GenerateHexCenterPoints_X6(point, currentStepSize);
                            foreach (var item in points)
                            {
                                Vector2 lookupB = HexCoreUtil.Calculate_CenterLookup(item, currentStepSize);
                                if (centerPointsByLookup_BySize[currentStepSize].ContainsKey(lookupB) == false) centerPointsByLookup_BySize[currentStepSize].Add(lookupB, item);
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
            return centerPointsByLookup_BySize;
        }


        public static Dictionary<Vector2, Vector3> Generate_HexagonGridCenterPoints(
            Vector3 initialCenter,
            float cellSize,
            int gridRadius,
            bool logResults = false
        )
        {
            Dictionary<int, Dictionary<Vector2, Vector3>> centerPointsByLookup_BySize = Generate_HexGridCenterPoints_BySize(
                    initialCenter,
                    cellSize,
                    gridRadius,
                    logResults
                );
            return centerPointsByLookup_BySize[(int)cellSize];
        }



        public static (Dictionary<int, List<HexagonCellPrototype>>, List<HexagonCellPrototype>) Assign_TileClusterFromHost(
            HexagonCellPrototype startHostCell,
            HexagonTileCore tilePrefab,
            Dictionary<Vector2, Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>> cellLookup_ByLayer_BySize_ByWorldSpace,
            bool enableLog = false
        )
        {
            TileClusterPrefabSettings settings = tilePrefab.GetClusterPrefab().GetSettings();
            CellSearchPriority searchPriority = settings.cellSearchPriority != CellSearchPriority.None ? settings.cellSearchPriority : CellSearchPriority.SideAndSideLayerNeighbors;

            int memberCount = (int)UnityEngine.Random.Range(settings.hostCellsMin, settings.hostCellsMax + 1);

            List<HexagonCellPrototype> _hostCells = HexGridPathingUtil.GetConsecutiveNeighborsCluster(startHostCell, memberCount, searchPriority, null, false);
            if (_hostCells.Count == 0)
            {
                Debug.LogError("NO _hostCells");
                return (null, null);
            }

            return (
                Create_TileClusterSubGridForHostCells(
                _hostCells,
                tilePrefab,
                cellLookup_ByLayer_BySize_ByWorldSpace,
                enableLog
                ),
                _hostCells
            );
        }


        public static Dictionary<int, List<HexagonCellPrototype>> Create_TileClusterSubGridForHostCells(
            List<HexagonCellPrototype> hostCells,
            HexagonTileCore tilePrefab,
            Dictionary<Vector2, Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>> cellLookup_ByLayer_BySize_ByWorldSpace,
            bool enableLog = false
        )
        {
            int childSize = (int)HexCellSizes.X_4;

            Dictionary<int, List<HexagonCellPrototype>> new_clusterSubGrid = new Dictionary<int, List<HexagonCellPrototype>>();

            HashSet<string> neighborIDsToEvaluate = new HashSet<string>();
            List<HexagonCellPrototype> neighborsToEvaluate = new List<HexagonCellPrototype>();
            Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>> cellLookup_ByLayer_X4 = new Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>();

            int totalHosts = hostCells.Count;
            int allChildren = 0;

            foreach (HexagonCellPrototype hostCell in hostCells)
            {
                int currentLayer = hostCell.layer;
                Vector2 worldspaceLookup = hostCell.GetWorldSpaceLookup();

                if (cellLookup_ByLayer_BySize_ByWorldSpace.ContainsKey(worldspaceLookup) == false || cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup].ContainsKey(childSize) == false)
                {
                    Debug.LogError("worldspaceLookup OR size not found in cellLookup_ByLayer_BySize_ByWorldSpace");
                    continue;
                }

                List<Vector3> childrenX4 = totalHosts == 1 ? HexCoreUtil.GenerateHexCenterPoints_X7(hostCell.center, childSize) : HexCoreUtil.GenerateHexCenterPoints_X13(hostCell.center, childSize);
                int childrenAdded = 0;

                int l = -1;
                foreach (Vector3 childPoint in childrenX4)
                {
                    Vector2 childLookup = HexCoreUtil.Calculate_CenterLookup(childPoint, childSize);
                    if (
                        cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][childSize].ContainsKey(currentLayer) &&
                        cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][childSize][currentLayer].ContainsKey(childLookup) &&
                        HexCoreUtil.IsAnyHexPointWithinPolygon(childPoint, childSize, hostCell.cornerPoints)
                    )
                    {
                        HexagonCellPrototype child = cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][childSize][currentLayer][childLookup];

                        if (l == -1)
                        {
                            l = child.layer;
                        }
                        else if (l != child.layer || currentLayer != l)
                        {
                            Debug.LogError("layer mismatch - l: " + l + ", current child layer: " + child.layer + ", currentLayer: " + currentLayer);
                        }

                        if (child != null && neighborIDsToEvaluate.Contains(child.Get_Uid()) == false)
                        {
                            if (new_clusterSubGrid.ContainsKey(currentLayer) == false) new_clusterSubGrid.Add(currentLayer, new List<HexagonCellPrototype>());
                            new_clusterSubGrid[currentLayer].Add(child);

                            if (cellLookup_ByLayer_X4.ContainsKey(currentLayer) == false) cellLookup_ByLayer_X4.Add(currentLayer, new Dictionary<Vector2, HexagonCellPrototype>());
                            cellLookup_ByLayer_X4[currentLayer].Add(childLookup, child);

                            neighborIDsToEvaluate.Add(child.Get_Uid());
                            neighborsToEvaluate.Add(child);

                            childrenAdded++;
                        }
                    }

                }
                if (childrenAdded > 0)
                {
                    hostCell.SetTile(tilePrefab, 0);

                    if (enableLog) Debug.Log("Host cell - children found: " + childrenAdded + ", L: " + l + ", hostCell: " + hostCell.LogStats());
                }
                allChildren += childrenAdded;
            }


            Debug.Log("Total Host cells: " + totalHosts + ", allChildren: " + allChildren);

            HexCellUtil.Evaluate_SubCellNeighbors(
                neighborsToEvaluate,
                cellLookup_ByLayer_X4,
                enableLog
            );

            return new_clusterSubGrid;
        }


        public static Dictionary<int, List<HexagonCellPrototype>> ConsolidateGridsByLayer(List<Dictionary<int, List<HexagonCellPrototype>>> cellGrids_byLayerList)
        {
            Dictionary<int, List<HexagonCellPrototype>> result = new Dictionary<int, List<HexagonCellPrototype>>();
            foreach (var grid in cellGrids_byLayerList)
            {
                foreach (var kvp in grid)
                {
                    int currentLayer = kvp.Key;
                    if (result.ContainsKey(currentLayer) == false)
                    {
                        result.Add(currentLayer, new List<HexagonCellPrototype>());
                    }
                    result[currentLayer].AddRange(grid[currentLayer]);
                }
            }
            return result;
        }

        public static Dictionary<int, Dictionary<int, List<HexagonCellPrototype>>> ConsolidateGridsBySizeByLayer(
            List<Dictionary<int, Dictionary<int, List<HexagonCellPrototype>>>> cellsGrids_BySizeByLayerList
        )
        {
            Dictionary<int, Dictionary<int, List<HexagonCellPrototype>>> result = new Dictionary<int, Dictionary<int, List<HexagonCellPrototype>>>();
            foreach (var gridBySizeByLayers in cellsGrids_BySizeByLayerList)
            {
                foreach (int currentSize in gridBySizeByLayers.Keys)
                {
                    if (result.ContainsKey(currentSize) == false)
                    {
                        result.Add(currentSize, new Dictionary<int, List<HexagonCellPrototype>>());
                    }

                    foreach (int currentLayer in gridBySizeByLayers[currentSize].Keys)
                    {
                        if (result[currentSize].ContainsKey(currentLayer) == false)
                        {
                            result[currentSize].Add(currentLayer, new List<HexagonCellPrototype>());
                        }
                        result[currentSize][currentLayer].AddRange(gridBySizeByLayers[currentSize][currentLayer]);
                    }
                }
            }
            return result;
        }

        public static Dictionary<int, List<HexagonCellPrototype>> Generate_MicroCellGridProtoypes_FromHosts(
            HexagonCell parentCell,
            List<HexagonCell> childCells,
            int cellLayers,
            int cellLayerOffset = 4,
            bool useCorners = true
        )
        {
            Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = null;
            Vector2 gridGenerationCenterPosXZOffeset = new Vector2(-1.18f, 0.35f);

            List<HexagonCell> allHostCells = new List<HexagonCell>();
            allHostCells.Add(parentCell);
            allHostCells.AddRange(childCells);

            foreach (HexagonCell hostCell in allHostCells)
            {
                Vector3 center = hostCell.transform.position;
                int layerBaseOffset = 0;

                if (hostCell.GetGridLayer() != parentCell.GetGridLayer())
                {
                    int layerDifference = hostCell.GetGridLayer() - parentCell.GetGridLayer();
                    layerBaseOffset = 1 * layerDifference;

                    center.y = parentCell.transform.position.y + (cellLayerOffset * layerDifference);
                }
                // Generate grid of protottyes 
                int radius = 12;
                int cellSize = 4;
                Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer = HexagonCellPrototype.GenerateGridsByLayer(center, radius, (HexCellSizes)cellSize, cellLayers, cellLayerOffset, gridGenerationCenterPosXZOffeset, layerBaseOffset, hostCell?.id);

                if (newPrototypesByLayer == null)
                {
                    newPrototypesByLayer = prototypesByLayer;
                }
                else
                {
                    foreach (var kvp in prototypesByLayer)
                    {
                        int key = kvp.Key;
                        List<HexagonCellPrototype> prototypes = kvp.Value;

                        if (newPrototypesByLayer.ContainsKey(key) == false)
                        {
                            newPrototypesByLayer.Add(key, prototypes);
                        }
                        else
                        {
                            newPrototypesByLayer[key].AddRange(prototypes);
                        }
                    }
                }
            }
            return newPrototypesByLayer;
        }

        public static void Generate_MicroGrid(
            List<HexagonCellCluster> clusters,
            int cellLayers,
            bool useEvenTopLayer,
            int cellLayerElevation,
            Transform transform,
            bool useCorners = true,
            HexCellSizes cellSize = HexCellSizes.X_4
        )
        {
            HashSet<string> duplicateCheckIds = new HashSet<string>();

            foreach (HexagonCellCluster cluster in clusters)
            {
                Generate_MicroGrid(
                    cluster,
                    cellLayers,
                    useEvenTopLayer,
                    cellLayerElevation,
                    transform,
                    useCorners,
                    duplicateCheckIds,
                    cellSize
                );
            }
        }

        public static void Generate_MicroGrid(
            HexagonCellCluster cluster,
            int layers,
            bool useEvenTopLayer,
            int cellLayerElevation,
            Transform transform,
            bool useCorners = true,
            HashSet<string> duplicateCheckIds = null,
            HexCellSizes size = HexCellSizes.X_4
        )
        {
            if (cluster.prototypes == null || cluster.prototypes.Count == 0)
            {
                Debug.LogError("Invalid cluster prototypes");
                return;
            }

            if (duplicateCheckIds == null) duplicateCheckIds = new HashSet<string>();

            List<HexagonCellPrototype> filteredHosts = new List<HexagonCellPrototype>();
            int duplicatesFound = 0;
            int highestGroundLayer = 0;

            foreach (HexagonCellPrototype item in cluster.prototypes)
            {
                if (duplicateCheckIds.Contains(item.uid) == false)
                {
                    duplicateCheckIds.Add(item.uid);
                    filteredHosts.Add(item);

                    if (highestGroundLayer < item.GetGridLayer()) highestGroundLayer = item.GetGridLayer();
                }
                else
                {
                    duplicatesFound++;
                    // Debug.LogError("GenerateMicroGridFromHosts - Duplicate host id found: " + item.uid);
                }
            }

            int topLayerTarget = highestGroundLayer + layers;

            Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = Generate_ProtoypeGrid_FromHosts(
                        filteredHosts,
                        size,
                        layers,
                        cellLayerElevation,
                        transform,
                        true,
                        useEvenTopLayer,
                        topLayerTarget
                    );

            foreach (var kvp in newPrototypesByLayer)
            {
                HexagonCellPrototype.EvaluateCellNeighborsAndEdgesInLayerList(kvp.Value, EdgeCellType.Default, false);
                // HexagonCellPrototype.EvaluateCellNeighborsAndEdgesInLayerList(kvp.Value, EdgeCellType.Default, transform, true);
            }

            cluster.prototypesByLayer_X4 = newPrototypesByLayer;
        }

        public static Dictionary<int, List<HexagonCellPrototype>> Generate_MicroGrid(
            List<HexagonCellPrototype> allHosts,
            int cellLayers,
            bool useEvenTopLayer,
            int cellLayerOffset,
            Transform transform,
            bool useCorners = true,
            HashSet<string> duplicateCheckIds = null,
            HexCellSizes cellSize = HexCellSizes.X_4
        )
        {
            if (duplicateCheckIds == null) duplicateCheckIds = new HashSet<string>();

            List<HexagonCellPrototype> filteredHosts = new List<HexagonCellPrototype>();
            int duplicatesFound = 0;
            int highestGroundLayer = 0;

            foreach (HexagonCellPrototype item in allHosts)
            {
                if (duplicateCheckIds.Contains(item.uid) == false)
                {
                    duplicateCheckIds.Add(item.uid);
                    filteredHosts.Add(item);

                    if (highestGroundLayer < item.GetGridLayer()) highestGroundLayer = item.GetGridLayer();
                }
                else
                {
                    duplicatesFound++;
                    // Debug.LogError("GenerateMicroGridFromHosts - Duplicate host id found: " + item.uid);
                }
            }

            //TEMP
            if (duplicatesFound > 0) Debug.LogError("Duplicate hosts found: " + duplicatesFound);

            int topLayerTarget = highestGroundLayer + cellLayers;
            Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = Generate_ProtoypeGrid_FromHosts(
                filteredHosts,
                cellSize,
                cellLayers,
                cellLayerOffset,
                transform,
                useCorners,
                useEvenTopLayer,
                topLayerTarget
            );

            Debug.Log("allHosts: " + allHosts.Count + ", filteredHosts: " + filteredHosts.Count + ", topLayerTarget: " + topLayerTarget);
            int count = 0;

            foreach (var kvp in newPrototypesByLayer)
            {
                HexagonCellPrototype.EvaluateCellNeighborsAndEdgesInLayerList(kvp.Value, EdgeCellType.Default, transform, true);
                count += kvp.Value.Count;
            }

            if (cellSize == HexCellSizes.X_4)
            {
                int expectedCount = filteredHosts.Count * 7;

                if (count != expectedCount)
                {
                    Debug.LogError("Hosts: " + filteredHosts.Count + ", sub cells: " + count + ", expectedCount: " + expectedCount);
                }
                else Debug.Log("Hosts: " + filteredHosts.Count + ", sub cells: " + count + ", expectedCount: " + expectedCount);
            }

            return newPrototypesByLayer;
        }

        public static Dictionary<int, List<HexagonCellPrototype>> Generate_ProtoypeGrid_FromHosts(
            List<HexagonCellPrototype> allHosts,
            HexCellSizes cellSize,
            int cellLayers,
            int cellLayerElevation = 4,
            Transform transform = null,
            bool useCorners = false,
            bool useEvenTopLayer = true,
            int topLayerTarget = -1
        )
        {
            Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = new Dictionary<int, List<HexagonCellPrototype>>();
            List<int> cornersToUse = new List<int>()
                {
                    (int)HexagonCorner.FrontA,
                    (int)HexagonCorner.FrontB,
                };


            foreach (HexagonCellPrototype hostCell in allHosts)
            {
                Vector3 center = hostCell.center;
                if (transform != null) center.y -= transform.position.y;

                int layerBaseOffset = hostCell.layer;

                List<int> _cornersToUse = new List<int>();
                _cornersToUse.AddRange(cornersToUse);

                bool anyGridHostCellsInBackNeighborStack = HexagonCellPrototype.HasGridHostCellsOnSideNeighborStack(hostCell, (int)HexagonSide.Back);

                if (anyGridHostCellsInBackNeighborStack == false)
                // if (backNeighbor == null || (backNeighbor != null && backNeighbor.isPath == false))
                {
                    _cornersToUse.Add((int)HexagonCorner.BackA);
                    _cornersToUse.Add((int)HexagonCorner.BackB);
                }

                if (useEvenTopLayer && topLayerTarget > 0) cellLayers = (topLayerTarget - layerBaseOffset);

                // Generate grid of protottyes 
                Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer = HexagonCellPrototype.GenerateGridsByLayer(
                    center,
                    hostCell.size,
                    cellSize,
                    cellLayers,
                    cellLayerElevation,
                    hostCell,
                    layerBaseOffset,
                    transform,
                    _cornersToUse
                );

                hostCell.SetGridHost(true);
                hostCell.cellPrototypes_X4_ByLayer = prototypesByLayer;

                foreach (var kvp in prototypesByLayer)
                {
                    int key = kvp.Key;
                    List<HexagonCellPrototype> prototypes = kvp.Value;
                    if (newPrototypesByLayer.ContainsKey(key) == false)
                    {
                        newPrototypesByLayer.Add(key, prototypes);
                    }
                    else
                    {
                        newPrototypesByLayer[key].AddRange(prototypes);
                    }
                }
            }
            return newPrototypesByLayer;
        }




        #region Depriciated

        public static List<HexagonCellPrototype> GenerateHexGrid(
            Vector3 center,
            int cellSize,
            int radius,
            IHexCell parentCell,
            int layerOffset,
            Transform transform = null,
            List<int> useCorners = null,
            bool logResults = false
        )
        {
            int size = (int)cellSize;
            List<Vector3> spawnCenters = new List<Vector3>();
            List<Vector3> quatCenterPoints = new List<Vector3>();
            List<int> quadrantSizes = new List<int>();

            bool filterOutCorners = (useCorners == null || useCorners.Count == 0);

            int prevStepSize = radius;
            int currentStepSize = radius;
            while (size < currentStepSize)
            {
                currentStepSize = (prevStepSize / 3);

                List<Vector3> newCenterPoints = new List<Vector3>();
                if (quatCenterPoints.Count == 0)
                {
                    newCenterPoints = (!filterOutCorners && currentStepSize <= size) ? HexCoreUtil.GenerateHexagonCenterPoints(center, currentStepSize, useCorners, true)
                        : HexCoreUtil.GenerateHexagonCenterPoints(center, currentStepSize, true, currentStepSize > size);
                    // newCenterPoints = HexCoreUtil.GenerateHexagonCenterPoints(center, currentStepSize, true, currentStepSize > size);
                }
                else
                {
                    foreach (Vector3 centerPoint in quatCenterPoints)
                    {
                        HexagonCellPrototype quadrantPrototype = new HexagonCellPrototype(centerPoint, prevStepSize, false);
                        // List<Vector3> points = HexCoreUtil.GenerateHexagonCenterPoints(centerPoint, currentStepSize, true, true);

                        List<Vector3> points = (!filterOutCorners && currentStepSize <= size) ? HexCoreUtil.GenerateHexagonCenterPoints(centerPoint, currentStepSize, useCorners, true)
                            : HexCoreUtil.GenerateHexagonCenterPoints(centerPoint, currentStepSize, true, true);

                        newCenterPoints.AddRange(points);
                        // quadrantCenterPoints.Add(quadrantPrototype, points);
                    }
                }
                // Debug.Log("GenerateHexGrid - newCenterPoints: " + newCenterPoints.Count + ", currentStepSize: " + currentStepSize + ", desired size: " + size);

                prevStepSize = currentStepSize;

                if (currentStepSize <= size)
                {
                    spawnCenters.AddRange(newCenterPoints);
                    break;
                }
                else
                {
                    quatCenterPoints.Clear();
                    quatCenterPoints.AddRange(newCenterPoints);
                    // Debug.Log("Quadrants of size " + currentStepSize + ": " + quatCenterPoints.Count);
                }
            }

            List<HexagonCellPrototype> results = new List<HexagonCellPrototype>();
            Vector2 baseCenterPosXZ = new Vector2(center.x, center.z);
            // Debug.Log("GenerateHexGrid - spawnCenters: " + spawnCenters.Count + ", size: " + size + ", filterOutCorners: " + filterOutCorners);

            int skipped = 0;
            for (int i = 0; i < spawnCenters.Count; i++)
            {
                // Vector3 centerPoint = spawnCenters[i];
                Vector3 centerPoint = transform != null ? transform.TransformPoint(spawnCenters[i]) : spawnCenters[i];

                // Filter out duplicate points & out of bounds
                float distance = Vector2.Distance(new Vector2(centerPoint.x, centerPoint.z), baseCenterPosXZ);
                // Debug.Log("GenerateHexGrid - centerPoint: " + centerPoint + ", size: " + size + ", distance: " + distance);

                if (filterOutCorners == false || (filterOutCorners && distance < radius))
                {
                    bool skip = false;
                    foreach (HexagonCellPrototype item in results)
                    {
                        // if (Vector2.Distance(new Vector2(centerPoint.x, centerPoint.z), new Vector2(item.center.x, item.center.z)) < 1f)
                        if (Vector3.Distance(centerPoint, item.center) < 1f)
                        {
                            skip = true;
                            skipped++;
                            break;
                        }
                    }
                    if (!skip) results.Add(new HexagonCellPrototype(centerPoint, cellSize, parentCell, layerOffset, "-" + i));
                }
            }

            // Debug.Log("GenerateHexGrid - 01 results: " + results.Count + ", size: " + size);
            if (filterOutCorners)
            {
                HexagonCellPrototype parentHex = new HexagonCellPrototype(center, radius, false);
                results = HexagonCellPrototype.GetPrototypesWithinHexagon(results, center, radius, parentHex.GetEdgePoints(), logResults);
            }

            if (logResults)
            {
                if (parentCell != null)
                {
                    Debug.Log("GenerateHexGrid - results: " + results.Count + ", size: " + size + ", parentCell: " + parentCell.GetId());
                    if (skipped > 0) Debug.LogError("Skipped: " + skipped + ", parentCell: " + parentCell.GetId() + ",  size: " + size);

                    if (transform != null)
                    {
                        Debug.Log("GenerateHexGrid - parent: " + parentCell.GetId() + ", position: " + parentCell.GetPosition() + ",  center: " + center + ", transformed Pos: " + transform.InverseTransformVector(parentCell.GetPosition()));
                    }
                    if (size == 4)
                    {
                        foreach (var item in results)
                        {
                            Debug.Log("GenerateHexGrid - parent: " + parentCell.GetId() + ", result: " + item.id);
                        }
                    }
                }
                else
                {
                    Debug.Log("GenerateHexGrid - 02 results: " + results.Count + ", size: " + size);
                    if (skipped > 0) Debug.LogError("Skipped: " + skipped + ", size: " + size);

                    if (size == 12)
                    {
                        foreach (var item in results)
                        {
                            Debug.Log("GenerateHexGrid - X12, result - id: " + item.id + ", uid: " + item.uid);
                        }
                    }
                }
            }
            return results;
        }


        #endregion

    }
}