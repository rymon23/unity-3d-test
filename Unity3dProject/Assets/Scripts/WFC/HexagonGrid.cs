using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ProceduralBase;

namespace WFCSystem
{
    public class HexagonGrid
    {
        public HexagonGrid(
            HexCellSizes _default_CellSize,
            int _cellLayers,
            int _cellLayersMax,
            int _cellLayerElevation,
            GridPreset _gridPreset
        )
        {
            default_CellSize = _default_CellSize;
            cellLayers = _cellLayers;
            cellLayersMax = _cellLayersMax;
            cellLayerElevation = _cellLayerElevation;
            gridPreset = _gridPreset;
        }

        private GridPreset gridPreset;
        private HexCellSizes default_CellSize = HexCellSizes.Default;
        public int DefaultCellSize() => (int)default_CellSize;
        private int cellLayers = 2;
        private int cellLayersMax = 4;
        private int cellLayerElevation = 3;

        public Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>> cellLookup_ByLayer_BySize { get; private set; }
        public Dictionary<int, Dictionary<int, List<HexagonCellPrototype>>> prototypesBySizeByLayer { get; private set; }
        // public Dictionary<int, List<HexagonCellPrototype>> prototypesBySize { get; private set; }
        private List<HexagonCellCluster> prototypeClusters;

        public Dictionary<int, List<HexagonCellPrototype>> GetDefaultPrototypesByLayer()
        {
            return prototypesBySizeByLayer.ContainsKey(DefaultCellSize()) ? prototypesBySizeByLayer[DefaultCellSize()] : null;
        }
        // public Dictionary<int, List<HexagonCellPrototype>> GetDefaultPrototypesByLayer()
        // {
        //     return prototypesBySizeByLayer.ContainsKey(defaultCellSize) ? prototypesBySizeByLayer[defaultCellSize] : null;
        // }

        public Dictionary<int, Dictionary<int, List<HexagonCellPrototype>>> GetAllCellGridEdges_BySizeByLayer()
        {
            Dictionary<int, Dictionary<int, List<HexagonCellPrototype>>> gridEdges_BySizeByLayer = new Dictionary<int, Dictionary<int, List<HexagonCellPrototype>>>();
            foreach (int currentSize in prototypesBySizeByLayer.Keys)
            {
                gridEdges_BySizeByLayer.Add(currentSize, new Dictionary<int, List<HexagonCellPrototype>>());
                foreach (int currentLayer in prototypesBySizeByLayer[currentSize].Keys)
                {
                    gridEdges_BySizeByLayer[currentSize].Add(currentLayer, prototypesBySizeByLayer[currentSize][currentLayer].FindAll(c => c.IsEdge()));
                }
            }
            return gridEdges_BySizeByLayer;
        }
        public Dictionary<int, List<HexagonCellPrototype>> GetDefaultCellGridEdges_ByLayer()
        {
            Dictionary<int, List<HexagonCellPrototype>> gridEdges_byLayer = new Dictionary<int, List<HexagonCellPrototype>>();
            foreach (var kvp in prototypesBySizeByLayer[DefaultCellSize()])
            {
                int currentLayer = kvp.Key;
                gridEdges_byLayer.Add(currentLayer, prototypesBySizeByLayer[DefaultCellSize()][currentLayer].FindAll(c => c.IsEdge()));
            }
            return gridEdges_byLayer;
        }

        // public List<HexagonCellPrototype> GetAllPrototypesOfSize(int cellSize = -1)
        // {
        //     if (cellSize < 2) cellSize = DefaultCellSize();
        //     return prototypesBySize.ContainsKey(cellSize) ? prototypesBySize[cellSize] : null;
        // }
        // public List<HexagonCellPrototype> GetAllPrototypeEdgesOfType(EdgeCellType type, int cellSize = -1) => GetAllPrototypesOfSize(cellSize).FindAll(c => c._edgeCellType == type);
        // public List<HexagonCellPrototype> GetAllPrototypesOfCellStatus(CellStatus status, int cellSize = -1) => GetAllPrototypesOfSize(cellSize).FindAll(c => c.GetCellStatus() == status);

        public List<HexagonCellCluster> GetPrototypeClustersOfType(CellClusterType type) => prototypeClusters.FindAll(c => c.clusterType == type);


        private bool _isClusterParent;

        public int layerBottom { private set; get; } = -1;
        public int layerTop { private set; get; } = -1;

        private void EvaluateLayerTopAndBottom()
        {
            int top = -1;
            int bottom = -1;
            foreach (int layer in prototypesBySizeByLayer[DefaultCellSize()].Keys)
            {
                if (bottom == -1 || layer < bottom) bottom = layer;
                if (top == -1 || layer > top) top = layer;
            }

            layerBottom = bottom;
            layerTop = top;
        }

        public void AssignGridCells(Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>> new_cellLookup_ByLayer_BySize)
        {
            cellLookup_ByLayer_BySize = new_cellLookup_ByLayer_BySize;

            Dictionary<int, Dictionary<int, List<HexagonCellPrototype>>> new_prototypesBySizeByLayer = new Dictionary<int, Dictionary<int, List<HexagonCellPrototype>>>();

            foreach (int currentSize in cellLookup_ByLayer_BySize.Keys)
            {
                if (currentSize != (int)HexCellSizes.X_12 && currentSize != (int)HexCellSizes.X_4) continue;

                if (new_prototypesBySizeByLayer.ContainsKey(currentSize) == false) new_prototypesBySizeByLayer.Add(currentSize, new Dictionary<int, List<HexagonCellPrototype>>());

                foreach (int currentLayer in cellLookup_ByLayer_BySize[currentSize].Keys)
                {
                    if (new_prototypesBySizeByLayer[currentSize].ContainsKey(currentLayer) == false) new_prototypesBySizeByLayer[currentSize].Add(currentLayer, new List<HexagonCellPrototype>());

                    foreach (HexagonCellPrototype cell in cellLookup_ByLayer_BySize[currentSize][currentLayer].Values)
                    {
                        new_prototypesBySizeByLayer[currentSize][currentLayer].Add(cell);
                    }
                }
            }

            prototypesBySizeByLayer = new_prototypesBySizeByLayer;
        }


        public void EvaluateCellParents(Dictionary<Vector2, Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>> cellLookup_ByLayer_BySize_ByWorldSpace)
        {
            Dictionary<string, Vector2> assigned = new Dictionary<string, Vector2>();
            foreach (int currentLayer in cellLookup_ByLayer_BySize[(int)HexCellSizes.X_12].Keys)
            {
                foreach (HexagonCellPrototype cell in cellLookup_ByLayer_BySize[(int)HexCellSizes.X_12][currentLayer].Values)
                {
                    int foundChildren = 0;
                    List<Vector2> childrenLookups = HexagonCellPrototype.CalculateChildrenLookupCoordinates(cell);
                    foreach (Vector2 lookup in childrenLookups)
                    {
                        HexagonCellPrototype child = cellLookup_ByLayer_BySize[(int)HexCellSizes.X_4][currentLayer].ContainsKey(lookup)
                                                                ? cellLookup_ByLayer_BySize[(int)HexCellSizes.X_4][currentLayer][lookup]
                                                                : null;

                        if (child != null && child.uid != cell.uid && assigned.ContainsKey(child.uid) == false)
                        {
                            cell.SetChild(child);
                            assigned.Add(child.uid, lookup);
                            foundChildren++;
                            continue;
                        }

                        foreach (var neighbor in cell.neighbors)
                        {
                            if (neighbor.IsSameLayer(cell) == false) continue;

                            Vector2 neighborLookup = neighbor.GetLookup();
                            Vector2 neighborWorldspaceLookup = neighbor.worldspaceLookup;

                            child = cellLookup_ByLayer_BySize_ByWorldSpace[neighborWorldspaceLookup][(int)HexCellSizes.X_4][currentLayer].ContainsKey(lookup)
                                                                    ? cellLookup_ByLayer_BySize_ByWorldSpace[neighborWorldspaceLookup][(int)HexCellSizes.X_4][currentLayer][lookup]
                                                                    : null;

                            if (child != null && child.uid != cell.uid && assigned.ContainsKey(child.uid) == false)
                            {
                                cell.SetChild(child);
                                assigned.Add(child.uid, lookup);
                                foundChildren++;
                            }
                        }
                    }

                    if (foundChildren <= 3 || foundChildren > 13)
                    {
                        cell.Highlight(true);
                        Debug.LogError("Cell children found: " + foundChildren + ", layer: " + currentLayer + ", size: " + cell.size);
                    }
                    // Debug.Log("cell children found: " + foundChildren + ", layer: " + currentLayer);
                }
            }
        }



        public void Rehydrate_CellGrids(Dictionary<Vector2, Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>> cellLookup_ByLayer_BySize_ByWorldSpace)
        {
            foreach (int currentSize in cellLookup_ByLayer_BySize.Keys)
            {
                if (currentSize != (int)HexCellSizes.X_12 && currentSize != (int)HexCellSizes.X_4) continue;

                foreach (int currentLayer in cellLookup_ByLayer_BySize[currentSize].Keys)
                {
                    foreach (HexagonCellPrototype cell in cellLookup_ByLayer_BySize[currentSize][currentLayer].Values)
                    {
                        Rehydrate(cell, cellLookup_ByLayer_BySize_ByWorldSpace);
                    }
                }
            }
        }

        private void Rehydrate(HexagonCellPrototype cell, Dictionary<Vector2, Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>> cellLookup_ByLayer_BySize_ByWorldSpace)
        {
            if (cell.neighborWorldData != null)
            {
                // Rehydrate Neighbor lists  
                int totalRehydrated = 0;
                foreach (CellWorldData neighborData in cell.neighborWorldData)
                {
                    Vector2 neighborLookup = neighborData.lookup;
                    Vector2 neighborWorldspaceLookup = neighborData.worldspaceLookup;
                    int neighborLayer = neighborData.layer;
                    int currentSize = cell.size;

                    if (
                        cellLookup_ByLayer_BySize_ByWorldSpace.ContainsKey(neighborWorldspaceLookup) == false ||
                        cellLookup_ByLayer_BySize_ByWorldSpace[neighborWorldspaceLookup].ContainsKey(currentSize) == false ||
                        cellLookup_ByLayer_BySize_ByWorldSpace[neighborWorldspaceLookup][currentSize].ContainsKey(neighborLayer) == false ||
                        cellLookup_ByLayer_BySize_ByWorldSpace[neighborWorldspaceLookup][currentSize][neighborLayer].ContainsKey(neighborLookup) == false

                    ) continue;

                    HexagonCellPrototype neighborCell = cellLookup_ByLayer_BySize_ByWorldSpace[neighborWorldspaceLookup][currentSize][neighborLayer][neighborLookup];

                    if (neighborCell != null && neighborCell.uid != cell.uid)
                    {
                        if (cell.neighbors.Contains(neighborCell) == false) cell.neighbors.Add(neighborCell);
                        if (neighborCell.neighbors.Contains(cell) == false) neighborCell.neighbors.Add(cell);

                        if (cell.IsSameLayer(neighborCell) == false)
                        {
                            int ix = neighborLayer < cell.layer ? 0 : 1;
                            cell.layerNeighbors[ix] = neighborCell;

                            int neighborix = neighborLayer < cell.layer ? 1 : 0;
                            neighborCell.layerNeighbors[neighborix] = cell;
                        }
                        totalRehydrated++;
                    }


                }
                HexagonCellPrototype.EvaluateForEdge(cell, EdgeCellType.Default, true);
                // Debug.LogError("totalRehydrated: " + totalRehydrated + ", size: " + cell.size);
            }
        }

        public Dictionary<int, Dictionary<int, List<HexagonCellPrototype>>> CreateWorldSpaceCellGrid_FromBasePoints(
            Dictionary<int, List<Vector3>> baseCenterPointsBySize,
            HexagonCellPrototype worldAreaHexCell,
            float cellGridElevation,
            Transform transform
        )
        {
            Vector3 centerPos = worldAreaHexCell.center;// transform.InverseTransformPoint(worldAreaHexCell.center);
            centerPos.y = cellGridElevation;

            Dictionary<int, Dictionary<int, List<HexagonCellPrototype>>> newPrototypesBySizeByLayer = HexagonCellPrototype.GenerateGridsByLayerBySize(
                baseCenterPointsBySize,
                cellLayersMax,
                cellLayerElevation,
                worldAreaHexCell,
                0,
                null, // transform,
                false
            );

            Dictionary<int, List<HexagonCellPrototype>> newPrototypesBySize = new Dictionary<int, List<HexagonCellPrototype>>();

            foreach (var kvpA in newPrototypesBySizeByLayer)
            {
                int currentSize = kvpA.Key;

                newPrototypesBySize.Add(currentSize, new List<HexagonCellPrototype>());

                foreach (var kvpB in newPrototypesBySizeByLayer[currentSize])
                {
                    // int currentLayer = kvpB.Key;
                    HexagonCellPrototype.EvaluateCellNeighborsAndEdgesInLayerList(kvpB.Value, EdgeCellType.Default, transform, true);
                    newPrototypesBySize[currentSize].AddRange(kvpB.Value);
                }

                Debug.LogError("newPrototypesBySizeByLayer - currentSize: " + currentSize);
            }

            prototypesBySizeByLayer = newPrototypesBySizeByLayer;
            // prototypesBySize = newPrototypesBySize;

            EvaluateLayerTopAndBottom();

            return prototypesBySizeByLayer;
        }

        public List<Vector3> GetHorizontalCenterPoints()
        {
            List<Vector3> points = new List<Vector3>();
            foreach (var item in prototypesBySizeByLayer[DefaultCellSize()][layerBottom])
            {
                points.Add(item.center);
            }
            return points;
        }

        public bool IsCellCenterPointPresent(Vector3 centerPoint)
        {
            Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer = GetDefaultPrototypesByLayer();
            if (prototypesByLayer == null) return false;

            return prototypesByLayer[layerBottom].Any(r => VectorUtil.DistanceXZ(r.center, centerPoint) < 1f);
        }

        // public List<HexagonCellPrototype> Setup_WorldAreaGrid_MainPath(GridPreset gridPreset = GridPreset.Unset)
        // {
        //     List<HexagonCellPrototype> gridEdges = GetAllPrototypeEdgesOfType(EdgeCellType.Default);
        //     List<HexagonCellPrototype> pathEdgeNodes = Setup_GridEdgePathConnectors(gridEdges, UnityEngine.Random.Range(2, 3));
        //     List<HexagonCellPrototype> mainPath = HexagonCellPrototype.GenerateRandomPathBetweenCells(pathEdgeNodes, false, false);

        //     return mainPath;
        // }

        public List<HexagonCellPrototype> Setup_GridEdgePathConnectors(List<HexagonCellPrototype> allGridEdgeCells, int minimun = 3)
        {
            List<HexagonCellPrototype> gridEdgePathConnectors = new List<HexagonCellPrototype>();

            // int desiredAmount = minimumPerSide * 6;
            int desiredAmount = minimun;
            List<HexagonCellPrototype> result = HexagonCellPrototype.PickRandomEntryFromGridEdges(allGridEdgeCells, minimun, true);
            foreach (var item in result)
            {
                item.SetOriginalGridEdge(true);

                item.isWorldSpaceEdgePathConnector = true;
                item.SetPathCell(true, PathCellType.End);
            }

            return result;
        }

        public List<HexagonCellPrototype> RemoveUnusedPrototypes()
        {
            List<HexagonCellPrototype> unusedPrototypes = new List<HexagonCellPrototype>();

            List<int> layerKeys = new List<int>();
            foreach (var kvp in prototypesBySizeByLayer[DefaultCellSize()])
            {
                layerKeys.Add(kvp.Key);
            }

            foreach (int layer in layerKeys)
            {
                List<HexagonCellPrototype> found = prototypesBySizeByLayer[DefaultCellSize()][layer].FindAll(p =>
                    p.IsRemoved() || (p.IsPreAssigned() == false));

                unusedPrototypes.AddRange(found);

                // foreach (HexagonCellPrototype item in found)
                // {
                //     if (item.IsGround()) {
                //     List<HexagonCellPrototype> layerStack = HexagonCellPrototype.GetAllUpperCellsInLayerStack(item);
                //     unusedPrototypes.AddRange(layerStack.FindAll(p => p.IsPreAssigned() == false));
                //     }
                // }
                prototypesBySizeByLayer[DefaultCellSize()][layer] = prototypesBySizeByLayer[DefaultCellSize()][layer].Except(found).ToList();
            }

            if (unusedPrototypes.Count > 0)
            {
                foreach (HexagonCellPrototype item in unusedPrototypes)
                {
                    item.SetCellStatus(CellStatus.Remove);
                }
            }

            EvaluatePrototypeGridEdges();

            return unusedPrototypes;
        }


        public void EvaluatePrototypeGridEdges()
        {
            foreach (var kvp in prototypesBySizeByLayer[DefaultCellSize()])
            {
                HexagonCellPrototype.GetEdgePrototypes(kvp.Value, EdgeCellType.Default);
            }
        }
    }
}