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

        public static void Evaluate_WorldCellNeighbors(
            List<HexagonCellPrototype> neighborsToEvaluate,
            Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> cellLookups_ByParentCell,
            int parentCellSize,
            bool enableLog = false
        )
        {
            if (neighborsToEvaluate.Count > 2)
            {
                foreach (HexagonCellPrototype cell in neighborsToEvaluate)
                {
                    int neighborsFound = 0;
                    Vector2 cellParentLookup = cell.GetParentLookup();
                    List<Vector2> parentNeighborLookups = HexagonCellPrototype.GenerateNeighborLookupCoordinates(new Vector3(cellParentLookup.x, 0, cellParentLookup.y), parentCellSize);
                    List<Vector2> neighborLookups = HexagonCellPrototype.GenerateNeighborLookupCoordinates(cell.center, cell.size);
                    HashSet<string> foundUids = new HashSet<string>();

                    foreach (var neighborLookup in neighborLookups)
                    {
                        HexagonCellPrototype neighbor = cellLookups_ByParentCell[cellParentLookup].ContainsKey(neighborLookup)
                                                                ? cellLookups_ByParentCell[cellParentLookup][neighborLookup]
                                                                : null;

                        if (neighbor != null && neighbor.uid != cell.uid && foundUids.Contains(neighbor.uid) == false)
                        {
                            if (cell.neighbors.Contains(neighbor) == false) cell.neighbors.Add(neighbor);
                            if (neighbor.neighbors.Contains(cell) == false) neighbor.neighbors.Add(cell);
                            foundUids.Add(neighbor.uid);

                            neighborsFound++;
                            continue;
                        }

                        foreach (Vector2 parentNeighborLookup in parentNeighborLookups)
                        {
                            if (cellLookups_ByParentCell.ContainsKey(parentNeighborLookup) == false) continue;

                            neighbor = cellLookups_ByParentCell[parentNeighborLookup].ContainsKey(neighborLookup)
                                                                   ? cellLookups_ByParentCell[parentNeighborLookup][neighborLookup]
                                                                   : null;

                            if (neighbor != null && neighbor.uid != cell.uid && foundUids.Contains(neighbor.uid) == false)
                            {
                                if (cell.neighbors.Contains(neighbor) == false) cell.neighbors.Add(neighbor);
                                if (neighbor.neighbors.Contains(cell) == false) neighbor.neighbors.Add(cell);
                                foundUids.Add(neighbor.uid);

                                neighborsFound++;
                                continue;
                            }
                        }
                    }
                    HexagonCellPrototype.EvaluateForEdge(cell, EdgeCellType.Default, true);

                    if (neighborsFound == 0 || neighborsFound > 8) Debug.LogError("cell neighbors found: " + neighborsFound);
                    if (enableLog) Debug.Log("cell neighbors found: " + neighborsFound);
                }
            }
        }

        public static void Evaluate_WorldCellNeighbors(List<HexagonCellPrototype> neighborsToEvaluate, Dictionary<Vector2, HexagonCellPrototype> cellLookups, bool enableLog = false)
        {
            if (neighborsToEvaluate.Count > 1)
            {
                foreach (HexagonCellPrototype cell in neighborsToEvaluate)
                {
                    int neighborsFound = 0;
                    List<Vector2> neighborLookups = HexagonCellPrototype.GenerateNeighborLookupCoordinates(cell.center, cell.size);
                    HashSet<string> foundUids = new HashSet<string>();
                    foreach (var neighborLookup in neighborLookups)
                    {
                        HexagonCellPrototype neighbor = cellLookups.ContainsKey(neighborLookup)
                                                                ? cellLookups[neighborLookup]
                                                                : null;
                        // if (neighbor == null)
                        // {
                        //     (Vector2 foundAprox, Vector2 offset) = FindKeyOrApproximateValue_WithOffset(neighborLookup, cellLookups, 1, 100);
                        //     // Vector2 foundAprox = FindKeyOrApproximateValue(neighborLookup, cellLookups, 1, 100);
                        //     // bool foundAprox = CheckKeyOrApproximateValue(neighborLookup, cellLookups, 0.5f, 2);
                        //     // if (foundAprox) Debug.LogError("foundAprox key");
                        //     if (cellLookups.ContainsKey(foundAprox))
                        //     {
                        //         Debug.LogError("foundAprox key: " + foundAprox + ", for neighborLookup: " + neighborLookup + " - Offset: " + offset);
                        //         neighbor = cellLookups[foundAprox];
                        //     }
                        //     else Debug.LogError("No aprox found for: " + neighborLookup);
                        // }
                        if (neighbor != null && neighbor.uid != cell.uid && foundUids.Contains(neighbor.uid) == false)
                        {
                            if (cell.neighbors.Contains(neighbor) == false) cell.neighbors.Add(neighbor);
                            if (neighbor.neighbors.Contains(cell) == false) neighbor.neighbors.Add(cell);
                            foundUids.Add(neighbor.uid);

                            neighborsFound++;
                        }
                    }
                    HexagonCellPrototype.EvaluateForEdge(cell, EdgeCellType.Default, true);

                    if (neighborsFound == 0 || neighborsFound > 8) Debug.LogError("cell neighbors found: " + neighborsFound);
                    if (enableLog) Debug.Log("cell neighbors found: " + neighborsFound);
                }
            }
        }

        public static void Evaluate_WorldCellNeighbors(Dictionary<Vector2, HexagonCellPrototype> neighborsToEvaluate, bool enableLog = false)
        {
            if (neighborsToEvaluate.Count > 1)
            {
                foreach (HexagonCellPrototype cell in neighborsToEvaluate.Values)
                {
                    int neighborsFound = 0;
                    List<Vector2> neighborLookups = HexagonCellPrototype.GenerateNeighborLookupCoordinates(cell.center, cell.size);
                    HashSet<string> foundUids = new HashSet<string>();
                    foreach (var neighborLookup in neighborLookups)
                    {
                        HexagonCellPrototype neighbor = neighborsToEvaluate.ContainsKey(neighborLookup)
                                                                ? neighborsToEvaluate[neighborLookup]
                                                                : null;

                        if (neighbor != null && neighbor.uid != cell.uid && foundUids.Contains(neighbor.uid) == false)
                        {
                            if (cell.neighbors.Contains(neighbor) == false) cell.neighbors.Add(neighbor);
                            if (neighbor.neighbors.Contains(cell) == false) neighbor.neighbors.Add(cell);
                            foundUids.Add(neighbor.uid);

                            neighborsFound++;
                        }
                    }
                    HexagonCellPrototype.EvaluateForEdge(cell, EdgeCellType.Default, true);

                    if (neighborsFound == 0 || neighborsFound > 8) Debug.LogError("cell neighbors found: " + neighborsFound);
                    if (enableLog) Debug.Log("cell neighbors found: " + neighborsFound);
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

        // public static List<HexagonCellPrototype> GetConsecutiveNeighborsFromStartCell(
        //     HexagonCellPrototype startCell,
        //     Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> worldSpaces_ByArea,
        //     int maxMembers
        // )
        // {
        //     HashSet<string> visitedHeads = new HashSet<string>();
        //     List<HexagonCellPrototype> found = new List<HexagonCellPrototype>();
        //     HexagonCellPrototype currentHead = startCell;
        //     int attempts = 999;

        //     if (found.Contains(startCell) == false) found.Add(startCell);
        //     visitedHeads.Add(startCell.GetId());

        //     while (currentHead != null && found.Count < maxMembers && attempts > 0)
        //     {
        //         // Debug.LogError("found.Count : " + found.Count + ", maxMembers: " + maxMembers);

        //         if (currentHead.neighbors.Count == 0) Rehydrate_WorldSpaceNeighbors(currentHead.GetParentLookup(), currentHead.GetLookup(), worldSpaces_ByArea, true);

        //         // Debug.LogError("currentHead.neighbors: " + currentHead.neighbors.Count);

        //         List<HexagonCellPrototype> shuffledNeighbors = currentHead.neighbors.FindAll(n => n.IsSameLayer(currentHead));
        //         HexagonCellPrototype.Shuffle(shuffledNeighbors);

        //         foreach (var neighbor in shuffledNeighbors)
        //         {
        //             if (found.Contains(neighbor) == false) found.Add(neighbor);
        //         }

        //         visitedHeads.Add(currentHead.uid);
        //         // Debug.LogError("n - found.Count : " + found.Count + ", maxMembers: " + maxMembers);

        //         if (found.Count < maxMembers)
        //         {
        //             if (found.Count > 0)
        //             {
        //                 currentHead = found.Find(c => visitedHeads.Contains(c.uid) == false);

        //                 // if (currentHead == null)
        //                 // {
        //                 //     Debug.LogError("no currentHead");
        //                 // }
        //                 // else Debug.LogError("currentHead.uid: " + currentHead.uid);
        //             }
        //             else break;
        //         }
        //         attempts--;
        //     }
        //     return found;
        // }

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
            visited.Add(start.id);

            RecursivelyFindNeighborsForTunnel(start.layerNeighbors[0], maxMembers, path, visited, ignoresStatus, searchPriority);

            return path;
        }

        private static void RecursivelyFindNeighborsForTunnel(HexagonCellPrototype current,
            int maxMembers,
            List<HexagonCellPrototype> path,
            HashSet<string> visited,
            List<CellStatus> ignoresStatus,
            CellSearchPriority searchPriority
        )
        {
            path.Add(current);
            visited.Add(current.id);

            if (path.Count >= maxMembers) return;

            List<HexagonCellPrototype> sortedNeighbors = EvaluateNeighborSearchPriority(current, searchPriority);

            for (int i = 0; i < sortedNeighbors.Count; i++)
            {
                if (path.Count > maxMembers) break;

                HexagonCellPrototype neighbor = sortedNeighbors[i];
                HexagonCellPrototype neighborTopNeighbor = neighbor.layerNeighbors[1];

                if (
                    !neighbor.IsUnderGround() ||
                    neighbor.IsPreAssigned() ||
                    neighbor.HasPreassignedNeighbor() ||
                    neighborTopNeighbor == null
                )
                {
                    visited.Add(neighbor.id);
                    continue;
                }

                if (neighborTopNeighbor != null && (neighborTopNeighbor.IsPreAssigned() || !neighborTopNeighbor.IsUnderGround()))
                {
                    visited.Add(neighborTopNeighbor.id);
                    continue;
                }

                if (!visited.Contains(neighbor.id))
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
            bool excludeOriginalGridEdge = false
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
                    neighborsList.AddRange(current.neighbors.FindAll(n => n.IsSameLayer(current)).OrderByDescending(n => n.neighbors.Count));
                    if (current.layerNeighbors[0] != null && neighborsList.Contains(current.layerNeighbors[0]) == false) neighborsList.Add(current.layerNeighbors[0]);
                    if (current.layerNeighbors[1] != null && neighborsList.Contains(current.layerNeighbors[1]) == false) neighborsList.Add(current.layerNeighbors[1]);
                    // neighborsList.AddRange(current.layerNeighbors.ToList().FindAll(n => n != null));
                }
                else if (searchPriority == CellSearchPriority.LayerNeighbors)
                {
                    if (current.layerNeighbors[0] != null && neighborsList.Contains(current.layerNeighbors[0]) == false) neighborsList.Add(current.layerNeighbors[0]);
                    if (current.layerNeighbors[1] != null && neighborsList.Contains(current.layerNeighbors[1]) == false) neighborsList.Add(current.layerNeighbors[1]);
                    // neighborsList.AddRange(current.layerNeighbors.ToList().FindAll(n => n != null));
                    neighborsList.AddRange(current.neighbors.FindAll(n => n.IsSameLayer(current)).OrderByDescending(n => n.neighbors.Count));
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
