using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;

namespace WFCSystem
{
    public static class HexCellUtil
    {

        public static void Evaluate_SubCellNeighbors(
            List<HexagonCellPrototype> neighborsToEvaluate,
            Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>> cellLookup_ByLayer,
            bool enableLog = false
        )
        {
            if (neighborsToEvaluate.Count < 2) return;

            foreach (HexagonCellPrototype cell in neighborsToEvaluate)
            {
                int currentLayer = cell.layer;
                if (cellLookup_ByLayer.ContainsKey(currentLayer) == false)
                {
                    continue;
                }

                int currentSize = cell.size;

                Dictionary<HexagonSide, Vector2> neighborLookupsBySide = HexCoreUtil.GenerateNeighborLookupCoordinatesBySide(cell.center, currentSize);
                HashSet<string> foundUids = new HashSet<string>();
                int sideNeighborsFound = 0;

                foreach (var kvp in neighborLookupsBySide)
                {
                    Vector2 neighborLookup = kvp.Value;
                    HexagonCellPrototype neighbor = cellLookup_ByLayer[currentLayer].ContainsKey(neighborLookup)
                                                            ? cellLookup_ByLayer[currentLayer][neighborLookup]
                                                            : null;

                    if (neighbor != null && neighbor.uid != cell.uid && foundUids.Contains(neighbor.uid) == false)
                    {
                        cell.AssignSideNeighbor(neighbor, kvp.Key);

                        foundUids.Add(neighbor.uid);

                        sideNeighborsFound++;
                        continue;
                    }
                }

                if (sideNeighborsFound < 6)
                {
                    cell.SetEdgeCell(true, EdgeCellType.Default);
                    // HexagonCellPrototype.EvaluateForEdge(cell, EdgeCellType.Default, true);
                }

                if (sideNeighborsFound == 0 || sideNeighborsFound > 8) Debug.LogError("cell neighbors found: " + sideNeighborsFound);
                if (enableLog) Debug.Log("cell neighbors found: " + sideNeighborsFound + ", isEdge: " + cell.IsEdge());
            }
        }


        public static void Evaluate_SubCellNeighbors(
            List<HexagonCellPrototype> neighborsToEvaluate,
            HexagonCellPrototype worldspaceCell,
            Dictionary<Vector2, Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>> cellLookup_ByLayer_BySize_ByWorldSpace,
            int cellLayerOffset,
            bool enableLog = false
        )
        {
            if (neighborsToEvaluate.Count > 2)
            {
                List<Vector2> worldspaceNeighborLookups = HexCoreUtil.GenerateNeighborLookupCoordinates(worldspaceCell.center, worldspaceCell.size);

                foreach (HexagonCellPrototype cell in neighborsToEvaluate)
                {
                    int currentLayer = HexCoreUtil.Calculate_CurrentLayer(cellLayerOffset, (int)cell.center.y);
                    int currentSize = cell.size;

                    Dictionary<HexagonSide, Vector2> neighborLookupsBySide = HexCoreUtil.GenerateNeighborLookupCoordinatesBySide(cell.center, currentSize);
                    Vector2 worldspaceLookup = cell.GetWorldSpaceLookup();
                    HashSet<string> foundUids = new HashSet<string>();
                    int sideNeighborsFound = 0;

                    if (cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup].ContainsKey(currentSize) == false)
                    {
                        // Debug.LogError("currentSize not found: " + currentSize);
                        continue;
                    }
                    else if (cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize].ContainsKey(currentLayer) == false)
                    {
                        // Debug.LogError("currentLayer not found: " + currentLayer + ", cell.center.y: " + (int)cell.center.y);
                        continue;
                    }

                    foreach (var kvp in neighborLookupsBySide)
                    {
                        Vector2 neighborLookup = kvp.Value;
                        HexagonCellPrototype neighbor = cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize][currentLayer].ContainsKey(neighborLookup)
                                                                ? cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize][currentLayer][neighborLookup]
                                                                : null;

                        if (neighbor != null && neighbor.uid != cell.uid && foundUids.Contains(neighbor.uid) == false)
                        {
                            cell.AssignSideNeighbor(neighbor, kvp.Key);
                            foundUids.Add(neighbor.uid);

                            sideNeighborsFound++;
                            continue;
                        }

                        // try
                        // {
                        foreach (Vector2 worldspaceNeighborLookup in worldspaceNeighborLookups)
                        {
                            if (cellLookup_ByLayer_BySize_ByWorldSpace.ContainsKey(worldspaceNeighborLookup) == false) continue;
                            if (cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceNeighborLookup].ContainsKey(currentSize) == false)
                            {
                                continue;
                            }
                            else if (cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceNeighborLookup][currentSize].ContainsKey(currentLayer) == false)
                            {
                                continue;
                            }

                            neighbor = cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceNeighborLookup][currentSize][currentLayer].ContainsKey(neighborLookup)
                                                                   ? cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceNeighborLookup][currentSize][currentLayer][neighborLookup]
                                                                   : null;

                            if (neighbor != null && neighbor.uid != cell.uid && foundUids.Contains(neighbor.uid) == false)
                            {
                                cell.AssignSideNeighbor(neighbor, kvp.Key);
                                foundUids.Add(neighbor.uid);

                                sideNeighborsFound++;
                                continue;
                            }
                        }
                        // }
                        // catch (System.Exception)
                        // {
                        //     Debug.LogError("cell - currentlayer: " + currentLayer + ", size: " + currentSize);
                        //     throw;
                        // }

                    }

                    if (sideNeighborsFound < 6)
                    {
                        cell.SetEdgeCell(true, EdgeCellType.Default);
                        // HexagonCellPrototype.EvaluateForEdge(cell, EdgeCellType.Default, true);
                        // }
                        // else
                        // {
                        //     int sideNeighborCount = cell.neighborsBySide.ToList().FindAll(n => n != null).Count;
                        //     Debug.Log("sideNeighborCount: " + sideNeighborCount);
                    }

                    if (sideNeighborsFound == 0 || sideNeighborsFound > 8) Debug.LogError("cell neighbors found: " + sideNeighborsFound);
                    if (enableLog) Debug.Log("cell neighbors found: " + sideNeighborsFound + ", isEdge: " + cell.IsEdge());
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
                    List<Vector2> parentNeighborLookups = HexCoreUtil.GenerateNeighborLookupCoordinates(new Vector3(cellParentLookup.x, 0, cellParentLookup.y), parentCellSize);
                    Dictionary<HexagonSide, Vector2> neighborLookupsBySide = HexCoreUtil.GenerateNeighborLookupCoordinatesBySide(cell.center, cell.size);

                    HashSet<string> foundUids = new HashSet<string>();

                    foreach (var kvp in neighborLookupsBySide)
                    {
                        Vector2 neighborLookup = kvp.Value;
                        HexagonCellPrototype neighbor = cellLookups_ByParentCell[cellParentLookup].ContainsKey(neighborLookup)
                                                                ? cellLookups_ByParentCell[cellParentLookup][neighborLookup]
                                                                : null;

                        if (neighbor != null && neighbor.uid != cell.uid && foundUids.Contains(neighbor.uid) == false)
                        {
                            cell.AssignSideNeighbor(neighbor, kvp.Key);

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
                                cell.AssignSideNeighbor(neighbor, kvp.Key);

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
                    HashSet<string> foundUids = new HashSet<string>();
                    Dictionary<HexagonSide, Vector2> neighborLookupsBySide = HexCoreUtil.GenerateNeighborLookupCoordinatesBySide(cell.center, cell.size);

                    foreach (var kvp in neighborLookupsBySide)
                    {
                        Vector2 neighborLookup = kvp.Value;
                        HexagonCellPrototype neighbor = cellLookups.ContainsKey(neighborLookup)
                                                                ? cellLookups[neighborLookup]
                                                                : null;

                        if (neighbor != null && neighbor.uid != cell.uid && foundUids.Contains(neighbor.uid) == false)
                        {
                            cell.AssignSideNeighbor(neighbor, kvp.Key);

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
                    HashSet<string> foundUids = new HashSet<string>();
                    Dictionary<HexagonSide, Vector2> neighborLookupsBySide = HexCoreUtil.GenerateNeighborLookupCoordinatesBySide(cell.center, cell.size);

                    foreach (var kvp in neighborLookupsBySide)
                    {
                        Vector2 neighborLookup = kvp.Value;
                        HexagonCellPrototype neighbor = neighborsToEvaluate.ContainsKey(neighborLookup)
                                                                ? neighborsToEvaluate[neighborLookup]
                                                                : null;

                        if (neighbor != null && neighbor.uid != cell.uid && foundUids.Contains(neighbor.uid) == false)
                        {
                            cell.AssignSideNeighbor(neighbor, kvp.Key);

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

        public static bool IsCellInBeteenNeighborsOfStatus(HexagonCellPrototype cell, CellStatus status, Dictionary<Vector2, HexagonCellPrototype> _cellsLookup)
        {
            List<Vector3> neighborPoints = HexCoreUtil.GenerateHexCenterPoints_X6(cell.center, cell.size);
            for (int i = 0; i < neighborPoints.Count; i++)
            {
                Vector2 neighborLookup_A = HexCoreUtil.Calculate_CenterLookup(neighborPoints[i], cell.size);
                if (!_cellsLookup.ContainsKey(neighborLookup_A) || _cellsLookup[neighborLookup_A].size != cell.size) continue;
                HexagonCellPrototype neighborA = _cellsLookup[neighborLookup_A];

                if (neighborA != null && neighborA.GetCellStatus() == status)
                {
                    for (int step = 0; step < 3; step++)
                    {
                        Vector2 neighborLookup_B = HexCoreUtil.Calculate_CenterLookup(neighborPoints[(i + (2 + step)) % 6], cell.size);

                        if (!_cellsLookup.ContainsKey(neighborLookup_B) || _cellsLookup[neighborLookup_B].size != cell.size) continue;
                        HexagonCellPrototype neighborB = _cellsLookup[neighborLookup_B];

                        if (neighborB != null && neighborB.GetCellStatus() == status) return true;
                    }
                }
            }
            return false;
        }

        public static int CalculateExpandedHexRadius(int cellSize, int radiusMult)
        {
            int radius = cellSize;
            for (int i = 0; i < radiusMult; i++)
            {
                radius *= 3;
            }
            return radius;
        }

        public static void SetTunnelCells(List<HexagonCellPrototype> cellsToAssign)
        {
            for (int i = 0; i < cellsToAssign.Count; i++)
            {
                HexagonCellPrototype tunnelCell = cellsToAssign[i];
                TunnelStatus status = TunnelStatus.Unset;

                if (tunnelCell.IsUnderGround())
                {
                    // HexagonCellPrototype topNeighbor = tunnelCell.layerNeighbors[1];
                    HexagonCellPrototype bottomNeighbor = tunnelCell.layerNeighbors[0];

                    if (cellsToAssign.Contains(bottomNeighbor))
                    {
                        status = TunnelStatus.AboveGround;
                    }
                    else
                    {
                        status = TunnelStatus.FlatGround;
                    }
                }
                tunnelCell.SetTunnel(true, status);
            }
        }

        // private static bool EvaluateEdgeCell(HexagonCellPrototype prototype, EdgeCellType edgeCellType, bool assignOriginalGridEdge)
        // {
        //     if (prototype == null) return false;

        //     List<HexagonCellPrototype> allSideNeighbors = prototype.neighbors.FindAll(c => c.IsSameLayer(prototype) && c.IsRemoved() == false);
        //     List<HexagonCellPrototype> allSiblingSideNeighbors = allSideNeighbors.FindAll(n => n.parentId == prototype.parentId);

        //     int sideNeighborCount = allSideNeighbors.Count;
        //     // int totalNeighborCount = prototype.neighbors.Count;
        //     bool isEdge = false;

        //     if (allSiblingSideNeighbors.Count < 6)
        //     {
        //         if (assignOriginalGridEdge == true) prototype.SetOriginalGridEdge(true);


        //         prototype.isEdgeOfParent = true;
        //     }

        //     bool isConnectorCell = allSideNeighbors.Any(n => n.GetParenCellId() != prototype.GetParenCellId());

        //     if (allSiblingSideNeighbors.Any(n => n.IsPath())) prototype.isEdgeTOPath = true;

        //     if (sideNeighborCount < 6)
        //     {
        //         prototype.SetEdgeCell(true, edgeCellType);
        //         isEdge = true;
        //     }
        //     return isEdge;
        // }

        public static bool IsEdgeCell(HexagonCell cell, bool isMultilayerCellGrid, bool assignToName, bool scopeToParentCell)
        {
            if (cell == null) return false;

            if (cell.GetEdgeCellType() > EdgeCellType.Default)
            {
                if (assignToName && !cell.gameObject.name.Contains("_EDGE")) cell.gameObject.name += "_EDGE";
                return true;
            }

            List<HexagonCell> allSideNeighbors = cell._neighbors.FindAll(c => c.GetGridLayer() == cell.GetGridLayer());
            bool isConnectorCell = allSideNeighbors.Find(n => n.GetParenCellId() != cell.GetParenCellId());

            if (scopeToParentCell) allSideNeighbors = allSideNeighbors.FindAll(n => n.GetParenCellId() == cell.GetParenCellId());

            int sideNeighborCount = allSideNeighbors.Count; //cell.GetSideNeighborCount(scopeToParentCell);
            int totalNeighborCount = cell._neighbors.Count;
            bool isEdge = false;

            if (sideNeighborCount < 6 || (isMultilayerCellGrid && totalNeighborCount < 7) || (cell.GetGridLayer() > 0 && cell.layeredNeighbor[0] != null && cell.layeredNeighbor[0]._neighbors.Count < 7))
            {
                cell.SetEdgeCell(true, isConnectorCell ? EdgeCellType.Connector : EdgeCellType.Default);
                isEdge = true;

                if (assignToName && !cell.gameObject.name.Contains("_EDGE")) cell.gameObject.name += isConnectorCell ? "_EDGE_CONNECTOR" : "_EDGE";
            }
            return isEdge;
        }


        public static List<HexagonCell> GetInnerEdges(List<HexagonCell> cells, EdgeCellType edgeCellType, bool assignToName = true, bool scopeToParentCell = false)
        {
            List<HexagonCell> edgeCells = new List<HexagonCell>();
            foreach (HexagonCell prototype in cells)
            {
                if (IsEdge(prototype, edgeCellType, assignToName, scopeToParentCell)) edgeCells.Add(prototype);
            }
            return edgeCells;
        }

        public static List<HexagonCell> GetEdgeCells(List<HexagonCell> cells, bool assignToName = true, bool scopeToParentCell = false)
        {
            List<HexagonCell> edgeCells = new List<HexagonCell>();
            bool hasMultilayerCells = cells.Any(c => c.GetGridLayer() > 0);

            foreach (HexagonCell cell in cells)
            {
                if (IsEdgeCell(cell, hasMultilayerCells, assignToName, scopeToParentCell))
                {
                    edgeCells.Add(cell);
                    // cell.SetEdgeCell(true);
                }
            }
            // Order edge cells by the fewest neighbors first
            return edgeCells.OrderByDescending(x => x._neighbors.Count).ToList();
        }



        private static bool IsEdge(HexagonCell cell, EdgeCellType edgeCellType, bool assignToName, bool scopeToParentCell = false)
        {
            if (cell == null) return false;

            List<HexagonCell> allSideNeighbors = cell._neighbors.FindAll(c => c.GetGridLayer() == cell.GetGridLayer());
            // bool isConnectorCell = allSideNeighbors.Find(n => n.GetParenCellId() != cell.GetParenCellId());
            // if (scopeToParentCell) allSideNeighbors = allSideNeighbors.FindAll(n => n.GetParenCellId() == cell.GetParenCellId());
            int sideNeighborCount = allSideNeighbors.Count; //cell.GetSideNeighborCount(scopeToParentCell);

            if (sideNeighborCount < 6)
            {
                cell.SetEdgeCell(true, edgeCellType);
                if (assignToName && !cell.gameObject.name.Contains("_EDGE")) cell.gameObject.name += "_EDGE";
                return true;
            }
            return false;
        }


        public static void PopulateNeighborsFromCornerPoints(List<HexagonCell> cells, float offset = 0.33f)
        {
            foreach (HexagonCell cell in cells)
            {
                //for each edgepoint on the current hexagontile
                for (int i = 0; i < cell._cornerPoints.Length; i++)
                {
                    //loop through all the hexagontile to check for neighbors
                    for (int j = 0; j < cells.Count; j++)
                    {
                        //skip if the hexagontile is the current tile
                        if (cells[j] == cell || cells[j].GetGridLayer() != cell.GetGridLayer())
                            continue;

                        Vector3 distA = cell.transform.TransformPoint(cell.transform.position);
                        Vector3 distB = cell.transform.TransformPoint(cells[j].transform.position);

                        float distance = Vector3.Distance(distA, distB);
                        if (distance > cell.GetSize() * HexagonCellPrototype.neighborSearchCenterDistMult) continue;

                        //loop through the _cornerPoints of the neighboring tile
                        for (int k = 0; k < cells[j]._cornerPoints.Length; k++)
                        {
                            // if (Vector3.Distance(cells[j]._cornerPoints[k], cell._cornerPoints[i]) <= offset)
                            if (Vector2.Distance(new Vector2(cells[j]._cornerPoints[k].x, cells[j]._cornerPoints[k].z), new Vector2(cell._cornerPoints[i].x, cell._cornerPoints[i].z)) <= offset)
                            {
                                if (cell._neighbors.Contains(cells[j]) == false) cell._neighbors.Add(cells[j]);
                                if (cells[j]._neighbors.Contains(cell) == false) cells[j]._neighbors.Add(cell);
                                break;
                            }
                        }
                    }
                }
                cell.SetNeighborsBySide(offset);
            }
        }

        public static void PopulateNeighborsFromCornerPoints(List<HexagonCellPrototype> cells, float offset = 0.33f)
        {
            int duplicatesFound = 0;
            for (int ixA = 0; ixA < cells.Count; ixA++)
            {
                HexagonCellPrototype cellA = cells[ixA];
                if (cellA.GetCellStatus() == CellStatus.Remove) continue;

                for (int ixB = 0; ixB < cells.Count; ixB++)
                {
                    HexagonCellPrototype cellB = cells[ixB];
                    if (ixB == ixA || cellB.GetCellStatus() == CellStatus.Remove || cellA.layer != cellB.layer) continue;

                    float distance = Vector3.Distance(cellA.center, cellB.center);
                    if (distance > cellA.size * HexagonCellPrototype.neighborSearchCenterDistMult) continue;

                    if (distance < 1f)
                    {
                        cellB.SetCellStatus(CellStatus.Remove);
                        duplicatesFound++;
                        // Debug.LogError("Duplicate Cells: " + cellA.id + ", uid: " + cellA.uid + ", and " + cellB.id + ", uid: " + cellB.uid + "\n total cells: " + cells.Count);
                        continue;
                    }

                    bool found = false;

                    for (int crIXA = 0; crIXA < cellA.cornerPoints.Length; crIXA++)
                    {
                        if (found) break;

                        Vector3 cornerA = cellA.cornerPoints[crIXA];

                        for (int crIXB = 0; crIXB < cellB.cornerPoints.Length; crIXB++)
                        {
                            Vector3 cornerB = cellB.cornerPoints[crIXB];

                            Vector2 posA = new Vector2(cornerA.x, cornerA.z);
                            Vector2 posB = new Vector2(cornerB.x, cornerB.z);

                            if (Vector2.Distance(posA, posB) <= offset)
                            {
                                if (cellA.neighbors.Contains(cellB) == false) cellA.neighbors.Add(cellB);
                                if (cellB.neighbors.Contains(cellA) == false) cellB.neighbors.Add(cellA);
                                found = true;
                                break;
                            }

                        }
                    }
                }
                // cellA.EvaluateNeighborsBySide(offset);
            }
            if (duplicatesFound > 0) Debug.LogError("Duplicate Cells found and marked for removal: " + duplicatesFound);
        }

        public static void PopulateNeighborsFromCornerPoints(List<HexagonCellPrototype> cells, Transform transform, float offset = 0.33f)
        {
            int duplicatesFound = 0;
            for (int ixA = 0; ixA < cells.Count; ixA++)
            {
                HexagonCellPrototype cellA = cells[ixA];
                if (cellA.GetCellStatus() == CellStatus.Remove) continue;

                for (int ixB = 0; ixB < cells.Count; ixB++)
                {
                    HexagonCellPrototype cellB = cells[ixB];
                    if (ixB == ixA || cellB.GetCellStatus() == CellStatus.Remove || cellA.layer != cellB.layer) continue;

                    Vector3 cellPosA = transform.TransformVector(cellA.center);
                    Vector3 cellPosB = transform.TransformVector(cellB.center);

                    float distance = Vector3.Distance(cellPosA, cellPosB);
                    if (distance > cellA.size * HexagonCellPrototype.neighborSearchCenterDistMult) continue;

                    if (distance < 1f)
                    {
                        cellB.SetCellStatus(CellStatus.Remove);
                        duplicatesFound++;
                        // Debug.LogError("Duplicate Cells: " + cellA.id + ", uid: " + cellA.uid + ", and " + cellB.id + ", uid: " + cellB.uid + "\n total cells: " + cells.Count);
                        continue;
                    }

                    bool found = false;

                    for (int crIXA = 0; crIXA < cellA.cornerPoints.Length; crIXA++)
                    {
                        if (found) break;

                        Vector3 cornerA = transform.TransformVector(cellA.cornerPoints[crIXA]);

                        for (int crIXB = 0; crIXB < cellB.cornerPoints.Length; crIXB++)
                        {
                            Vector3 cornerB = transform.TransformVector(cellB.cornerPoints[crIXB]);

                            Vector2 posA = new Vector2(cornerA.x, cornerA.z);
                            Vector2 posB = new Vector2(cornerB.x, cornerB.z);

                            if (Vector2.Distance(posA, posB) <= offset)
                            {
                                if (cellA.neighbors.Contains(cellB) == false) cellA.neighbors.Add(cellB);
                                if (cellB.neighbors.Contains(cellA) == false) cellB.neighbors.Add(cellA);
                                found = true;
                                break;
                            }

                        }
                    }
                }
                // cellA.EvaluateNeighborsBySide(offset);
            }
            if (duplicatesFound > 0) Debug.LogError("Duplicate Cells found and marked for removal: " + duplicatesFound);
        }






        public static (HexagonCell, float) GetClosestCell(List<HexagonCell> cells, Vector2 position)
        {
            HexagonCell nearestCell = cells[0];
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < cells.Count; i++)
            {
                Vector2 posXZ = new Vector2(cells[i].transform.position.x, cells[i].transform.position.z);
                float dist = Vector2.Distance(position, posXZ);
                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    nearestCell = cells[i];
                }
            }
            return (nearestCell, nearestDistance);
        }


        public static void ShuffleCells(List<HexagonCell> cells)
        {
            int n = cells.Count;
            for (int i = 0; i < n; i++)
            {
                // Get a random index from the remaining elements
                int r = i + UnityEngine.Random.Range(0, n - i);
                // Swap the current element with the random one
                HexagonCell temp = cells[r];
                cells[r] = cells[i];
                cells[i] = temp;
            }
        }
        public static Vector3 CalculateCenterPositionOfHexagonCells(HexagonCell[] cells)
        {
            // Calculate the center point
            Vector3 center = Vector3.zero;
            for (int i = 0; i < cells.Length; i++)
                center += cells[i].transform.position;
            center /= cells.Length;

            return center;
        }

        public static List<Vector3> GetCenterPositionsFromCells(List<HexagonCell> cells)
        {
            List<Vector3> positions = new List<Vector3>();
            foreach (var item in cells)
            {
                positions.Add(item.transform.position);
            }
            return positions;
        }

        public static List<Vector3> GetOrderedVertices(List<HexagonCell> cells)
        {
            List<Vector3> vertices = new List<Vector3>();
            foreach (HexagonCell cell in cells)
            {
                Vector3[] cornerPoints = cell._cornerPoints;
                Vector3 transformPos = cell.transform.position;

                for (int i = 0; i < cornerPoints.Length; i++)
                {
                    vertices.Add(cell._cornerPoints[i]);
                }
            }
            return vertices;
        }


        public static List<HexagonCellPrototype> ExtractCellsByLayer(Dictionary<int, List<HexagonCellPrototype>> cellsByLayer, bool logIncompatibilities = true)
        {
            List<HexagonCellPrototype> cells = new List<HexagonCellPrototype>();

            foreach (var kvp in cellsByLayer)
            {
                int currentLayer = kvp.Key;
                List<HexagonCellPrototype> layerCells = kvp.Value;

                foreach (HexagonCellPrototype cell in layerCells)
                {
                    if (logIncompatibilities) Debug.LogError("ExtractCellsByLayer - L: " + currentLayer + ", cell: " + cell.LogStats());
                    int sideNeighborCount = cell.neighborsBySide.ToList().FindAll(n => n != null).Count;
                    Debug.Log("sideNeighborCount: " + sideNeighborCount);

                    cells.Add(cell);
                }
                // cells.AddRange(kvp.Value);
            }
            return cells;
        }

        public static List<HexagonCell> ExtractCellsByLayer(Dictionary<int, List<HexagonCell>> cellsByLayer)
        {
            List<HexagonCell> cells = new List<HexagonCell>();

            foreach (var kvp in cellsByLayer)
            {
                cells.AddRange(kvp.Value);
            }
            return cells;
        }


        public static Dictionary<int, List<HexagonCellPrototype>> OrganizeByLayer(Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>> cellLookup_ByLayer
        )
        {
            Dictionary<int, List<HexagonCellPrototype>> cellsByLayer = new Dictionary<int, List<HexagonCellPrototype>>();

            foreach (var kvp in cellLookup_ByLayer)
            {
                int layer = kvp.Key;
                if (cellsByLayer.ContainsKey(layer) == false) cellsByLayer.Add(layer, new List<HexagonCellPrototype>());

                foreach (HexagonCellPrototype cell in cellLookup_ByLayer[layer].Values)
                {
                    cellsByLayer[layer].Add(cell);
                }
            }
            return cellsByLayer;
        }

        public static Dictionary<int, List<HexagonCellPrototype>> OrganizeByLayer(List<HexagonCellPrototype> cells)
        {
            Dictionary<int, List<HexagonCellPrototype>> cellsByLayer = new Dictionary<int, List<HexagonCellPrototype>>();

            foreach (HexagonCellPrototype cell in cells)
            {
                int layer = cell.GetGridLayer();

                if (cellsByLayer.ContainsKey(layer) == false)
                {
                    cellsByLayer.Add(layer, new List<HexagonCellPrototype>());
                }

                cellsByLayer[layer].Add(cell);
            }
            return cellsByLayer;
        }

        public static Dictionary<int, List<HexagonCell>> OrganizeByLayer(List<HexagonCell> cells)
        {
            Dictionary<int, List<HexagonCell>> cellsByLayer = new Dictionary<int, List<HexagonCell>>();

            foreach (HexagonCell cell in cells)
            {
                int layer = cell.GetGridLayer();

                if (cellsByLayer.ContainsKey(layer) == false)
                {
                    cellsByLayer.Add(layer, new List<HexagonCell>());
                }

                cellsByLayer[layer].Add(cell);
            }
            return cellsByLayer;
        }



        public static HexagonCell[,] CreateRectangularGrid(List<HexagonCell> cellGrid)
        {
            float hexagonSize = cellGrid[0].GetSize();

            //Find the minimum and maximum x and z coordinates of the hexagon cells in the grid
            float minX = cellGrid.Min(cell => cell.transform.position.x);
            float maxX = cellGrid.Max(cell => cell.transform.position.x);
            float minZ = cellGrid.Min(cell => cell.transform.position.z);
            float maxZ = cellGrid.Max(cell => cell.transform.position.z);
            //Determine the number of rows and columns needed for the rectangular grid
            int rows = (int)((maxZ - minZ) / hexagonSize) + 1;
            int cols = (int)((maxX - minX) / (hexagonSize * 0.75f)) + 1;

            //Initialize the rectangular grid with the determined number of rows and columns
            HexagonCell[,] rectGrid = new HexagonCell[rows, cols];

            //Iterate through each hexagon cell in the cell grid
            foreach (HexagonCell cell in cellGrid)
            {
                //Determine the row and column index of the current cell in the rectangular grid
                int row = (int)((cell.transform.position.z - minZ) / hexagonSize);
                int col = (int)((cell.transform.position.x - minX) / (hexagonSize * 0.75f));

                //Assign the current cell to the corresponding position in the rectangular grid
                rectGrid[row, col] = cell;
            }

            return rectGrid;
        }



        public static void CreateGridMesh(List<HexagonCell> cells, MeshFilter meshFilter)
        {
            int vertexIndex = 0;
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            foreach (HexagonCell cell in cells)
            {
                for (int i = 0; i < 6; i++)
                {
                    vertices.Add(cell._cornerPoints[i]);
                    // vertices.Add(cell.transform.TransformPoint(cell._cornerPoints[i]));
                    triangles.Add(vertexIndex + (i + 2) % 6);
                    triangles.Add(vertexIndex + (i + 1) % 6);
                    triangles.Add(vertexIndex);
                }

                vertexIndex += 6;
            }

            // Set the mesh data
            Mesh mesh = new Mesh();
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
        }

        public static void CreateGridMesh(List<Vector3> vertices, MeshFilter meshFilter)
        {
            Dictionary<int, List<int>> pointConnections = new Dictionary<int, List<int>>();
            List<int> triangles = new List<int>();

            for (int i = 0; i < vertices.Count; i++)
            {
                pointConnections[i] = new List<int>();

                for (int j = 0; j < vertices.Count; j++)
                {
                    if (i == j) continue;

                    // Check if the distance between the two vertices is close enough
                    if (Vector3.Distance(vertices[i], vertices[j]) <= 100.0f)
                    {
                        // Check if the triangle between the two vertices is not already in the list
                        bool isDuplicate = false;
                        for (int k = 0; k < triangles.Count; k += 3)
                        {
                            if ((triangles[k] == i && triangles[k + 1] == j) || (triangles[k + 1] == i && triangles[k + 2] == j) || (triangles[k + 2] == i && triangles[k] == j))
                            {
                                isDuplicate = true;
                                break;
                            }
                        }

                        if (!isDuplicate)
                        {
                            triangles.Add(i);
                            triangles.Add(j);
                            triangles.Add(i);
                            pointConnections[i].Add(j);
                        }
                    }
                }
            }

            // Set the mesh data
            Mesh mesh = new Mesh();
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
        }

        public static void UpdateMesh(List<HexagonCell> cells, MeshFilter meshFilter)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            // Add the vertices to the vertices list
            foreach (var cell in cells)
            {
                // Add each of the cell's corner points as a vertex
                foreach (var cornerPoint in cell._cornerPoints)
                {
                    vertices.Add(cornerPoint + cell.transform.position);
                }
            }

            // Add the triangles to the triangles list
            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                int vertexIndex = i * 6;

                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);

                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 3);
                triangles.Add(vertexIndex);

                triangles.Add(vertexIndex + 3);
                triangles.Add(vertexIndex + 4);
                triangles.Add(vertexIndex);

                triangles.Add(vertexIndex + 4);
                triangles.Add(vertexIndex + 5);
                triangles.Add(vertexIndex);
            }

            // Set the mesh data
            Mesh mesh = new Mesh();
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
        }




    }
}