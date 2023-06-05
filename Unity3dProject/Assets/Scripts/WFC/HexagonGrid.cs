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


        // public Dictionary<int, List<HexagonCellPrototype>> CreateWorldSpaceCellGrid_FromBasePoints(
        //     List<Vector3> baseCenterPoints,
        //     HexagonCellPrototype worldAreaHexCell,
        //     int gridSize,
        //     int cellSize,
        //     float cellGridElevation,
        //     Transform transform
        // )
        // {
        //     defaultCellSize = cellSize;

        //     Vector3 centerPos = worldAreaHexCell.center;// transform.InverseTransformPoint(worldAreaHexCell.center);
        //     centerPos.y = cellGridElevation;

        //     Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer = HexagonCellPrototype.GenerateGridsByLayer(
        //         baseCenterPoints,
        //         12,
        //         cellLayersMax,
        //         cellLayerElevation,
        //         worldAreaHexCell,
        //         0,
        //         null, // transform,
        //         false
        //     );

        //     prototypesBySize = new Dictionary<int, List<HexagonCellPrototype>>() {
        //         { cellSize, new  List<HexagonCellPrototype>() }
        //     };

        //     foreach (var kvp in newPrototypesByLayer)
        //     {
        //         HexagonCellPrototype.EvaluateCellNeighborsAndEdgesInLayerList(kvp.Value, EdgeCellType.Default, transform, true);

        //         prototypesBySize[cellSize].AddRange(kvp.Value);
        //     }

        //     prototypesBySizeByLayer = new Dictionary<int, Dictionary<int, List<HexagonCellPrototype>>>() {
        //         { cellSize, newPrototypesByLayer }
        //     };

        //     EvaluateLayerTopAndBottom();

        //     return newPrototypesByLayer;
        // }


        // public Dictionary<int, List<HexagonCellPrototype>> CreateWorldSpaceCellGrid(HexagonCellPrototype worldAreaHexCell, int gridSize, int cellSize, float cellGridElevation, Transform transform)
        // {
        //     defaultCellSize = cellSize;

        //     Vector3 centerPos = worldAreaHexCell.center;// transform.InverseTransformPoint(worldAreaHexCell.center);
        //     centerPos.y = cellGridElevation;

        //     // List<int> _cornersToUse = new List<int>() {
        //     //     (int)HexagonCorner.BackA,
        //     //     (int)HexagonCorner.BackB,
        //     //     (int)HexagonCorner.FrontA,
        //     //     (int)HexagonCorner.FrontB
        //     // };

        //     (
        //         Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer,
        //         List<HexagonCellPrototype> outOfBoundsPoints,
        //         List<Vector2> allXZCenterPoints

        //     ) = HexagonCellPrototype.GenerateGridsByLayer_WithOutOfBounds(
        //                     centerPos,
        //                     gridSize,
        //                     12,
        //                     cellLayersMax,
        //                     cellLayerElevation,
        //                     worldAreaHexCell,
        //                     0,
        //                     null, // transform,
        //                     false
        //     );

        //     prototypesBySize = new Dictionary<int, List<HexagonCellPrototype>>() {
        //         { cellSize, new  List<HexagonCellPrototype>() }
        //     };

        //     foreach (var kvp in newPrototypesByLayer)
        //     {
        //         HexagonCellPrototype.EvaluateCellNeighborsAndEdgesInLayerList(kvp.Value, EdgeCellType.Default, transform, true);

        //         prototypesBySize[cellSize].AddRange(kvp.Value);
        //     }

        //     prototypesBySizeByLayer = new Dictionary<int, Dictionary<int, List<HexagonCellPrototype>>>() {
        //         { cellSize, newPrototypesByLayer }
        //     };

        //     EvaluateLayerTopAndBottom();

        //     return newPrototypesByLayer;
        // }


        public List<Vector3> GetHorizontalCenterPoints()
        {
            List<Vector3> points = new List<Vector3>();
            foreach (var item in prototypesBySizeByLayer[DefaultCellSize()][layerBottom])
            {
                points.Add(item.center);
            }
            return points;
        }


        // public bool AddCenterPoints(List<HexagonCellPrototype> newCells, HexagonCellPrototype parentCell)
        // {
        //     if (prototypesBySizeByLayer == null || prototypesBySizeByLayer.ContainsKey(defaultCellSize) == false) return false;
        //     int skipped = 0;

        //     List<HexagonCellPrototype> created = new List<HexagonCellPrototype>();

        //     foreach (var cell in newCells)
        //     {
        //         if (IsCellCenterPointPresent(cell.center))
        //         {
        //             skipped++;
        //             continue;
        //         }

        //         HexagonCellPrototype newPrototype = new HexagonCellPrototype(cell.center, defaultCellSize, parentCell, "", layerBottom);
        //         prototypesBySizeByLayer[defaultCellSize][layerBottom].Add(newPrototype);
        //         created.Add(newPrototype);
        //     }

        //     List<HexagonCellPrototype> addedToPrevLayer = created;

        //     for (int currentLayer = (layerBottom + 1); currentLayer < layerTop + 1; currentLayer++)
        //     {
        //         List<HexagonCellPrototype> newLayer = HexagonCellPrototype.DuplicateGridToNewLayerAbove(addedToPrevLayer, cellLayerElevation, currentLayer, parentCell);
        //         prototypesBySizeByLayer[defaultCellSize][currentLayer].AddRange(newLayer);
        //         addedToPrevLayer = newLayer;
        //     }

        //     if (skipped > 0) Debug.LogError("new center pointss skipped: " + skipped);
        //     return true;
        // }

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

        // public List<HexagonCellPrototype> Generate_GridPreset_Outpost_WPath(TerrainVertex[,] vertexGrid, float cellVertexSearchRadiusMult = 1.4f)
        // {
        //     HexagonCellPrototype.CleanupCellIslandLayerPrototypes(cellPrototypesByLayer_V2, 3);

        //     GenerateCellClusters_Random(CellClusterType.Outpost);

        //     bool pathIgnoresEdgeCells = true;
        //     List<HexagonCellPrototype> path = PathBetweenClusters();
        //     if (path != null) CreatePathCluster(path);

        //     if (enableTunnels) GenerateCellCluster_Random_Underground(); // GenerateCellClusters_Cave();
        //     //     // cellManager.GenerateCellClusters_Cave(maxTunnelMemberSize, _tunnelMeshParent);

        //     List<HexagonCellPrototype> unused = RemoveUnusedPrototypes();

        //     if (vertexGrid != null)
        //     {
        //         HexGridVertexUtil.UnassignCellVertices(unused, vertexGrid);
        //         HexGridVertexUtil.AssignTerrainVerticesToGroundPrototypes(cellPrototypesByLayer_V2, vertexGrid, cellVertexSearchRadiusMult);
        //     }
        //     return path;
        // }

        // public List<HexagonCellPrototype> Generate_GridPreset_City_WPath()
        // {

        //     List<HexagonCellPrototype> gridEdges = GetAllPrototypeEdgesOfType(EdgeCellType.Default);
        //     List<HexagonCellPrototype> entries = HexagonCellPrototype.PickRandomEntryFromGridEdges(gridEdges, 3, true);

        //     bool pathIgnoresEdgeCells = useGridErosion;
        //     List<HexagonCellPrototype> path = HexagonCellPrototype.GenerateRandomPath(entries, allPrototypesList, gameObject.transform.position, pathIgnoresEdgeCells);
        //     if (path != null) CreatePathCluster(path);

        //     Debug.Log("gridEdges: " + gridEdges.Count + ", entries: " + entries.Count + ", allPrototypesList: " + allPrototypesList.Count);

        //     GenerateCellClusters_GridEdge(gridEdges);

        //     return path;
        // }
        // public HexagonCellCluster CreatePathCluster(List<HexagonCellPrototype> path)
        // {
        //     if (path == null || path.Count == 0) return null;

        //     int clusterid = allCellPrototypeClusters.Count == 0 ? 0 : allCellPrototypeClusters.Count + 1;

        //     HexagonCellCluster newPathCluster = new HexagonCellCluster(clusterid, path, CellClusterType.Path, ClusterGroundCellLayerRule.Unset);
        //     allCellPrototypeClusters.Add(newPathCluster);

        //     return newPathCluster;
        // }

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


        public static Dictionary<int, List<HexagonCellPrototype>> Generate_MicroGrid(
            List<HexagonCellPrototype> allHosts,
            int cellLayers,
            bool useEvenTopLayer,
            int cellLayerElevation,
            Transform transform,
            bool useCorners = true,
            HashSet<string> duplicateCheckIds = null,
            int cellSize = 4
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

                    if (highestGroundLayer < item.GetLayer()) highestGroundLayer = item.GetLayer();
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
                cellLayerElevation,
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

            if (cellSize == 4)
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

        public static void Generate_MicroGrid(
            List<HexagonCellCluster> clusters,
            int cellLayers,
            bool useEvenTopLayer,
            int cellLayerElevation,
            Transform transform,
            bool useCorners = true,
            int cellSize = 4
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
            int size = 4
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

                    if (highestGroundLayer < item.GetLayer()) highestGroundLayer = item.GetLayer();
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




        public static Dictionary<int, List<HexagonCellPrototype>> Generate_ProtoypeGrid_FromHosts(
            List<HexagonCellPrototype> allHosts,
            int cellSize,
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
            // // Remove the corner cells in the same point;
            // if (useCorners)
            // {
            //     foreach (var kvp in newPrototypesByLayer)
            //     {
            //         int key = kvp.Key;
            //         List<HexagonCellPrototype> prototypes = kvp.Value;

            //         HexagonCellPrototype.RemoveExcessPrototypesByDistance(prototypes);
            //     }
            // }

            return newPrototypesByLayer;
        }


        public static Dictionary<int, List<HexagonCellPrototype>> Generate_MicroCellGridProtoypes_FromHosts(
            HexagonCell parentCell,
            List<HexagonCell> childCells,
            int cellLayers,
            int cellLayerElevation = 4,
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

                if (hostCell.GetLayer() != parentCell.GetLayer())
                {
                    int layerDifference = hostCell.GetLayer() - parentCell.GetLayer();
                    layerBaseOffset = 1 * layerDifference;

                    center.y = parentCell.transform.position.y + (cellLayerElevation * layerDifference);
                }

                // Generate grid of protottyes 
                Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer = HexagonCellPrototype.GenerateGridsByLayer(center, 12, 4, cellLayers, cellLayerElevation, gridGenerationCenterPosXZOffeset, layerBaseOffset, hostCell?.id);

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

            // // Remove the corner cells in the same point;
            // if (useCorners)
            // {
            //     foreach (var kvp in newPrototypesByLayer)
            //     {
            //         int key = kvp.Key;
            //         List<HexagonCellPrototype> prototypes = kvp.Value;

            //         HexagonCellPrototype.RemoveExcessPrototypesByDistance(prototypes);
            //     }
            // }

            return newPrototypesByLayer;
        }

    }
}