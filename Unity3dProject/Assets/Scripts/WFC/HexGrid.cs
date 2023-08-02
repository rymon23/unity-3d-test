using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ProceduralBase;

namespace WFCSystem
{
    public class HexGrid
    {
        public HexGrid(
            Vector3 _startPosition,
            Dictionary<int, Dictionary<Vector2, Vector3>> new_cellCenters_ByLookup_BySize,
            int _cellSize,
            int _cellLayers,
            int _cellLayerOffset,
            int _radius
        )
        {
            gridCenterPos = _startPosition;

            cellSize = _cellSize;
            cellLayersMax = _cellLayers;
            cellLayerOffset = _cellLayerOffset;
            radius = _radius;

            Generate_Grid(new_cellCenters_ByLookup_BySize);
        }

        public HexGrid(
            Vector3 _startPosition,
            int _cellSize,
            int _cellLayers,
            int _cellLayerOffset,
            int _radius,
            Option_CellGridType _gridType
        )
        {
            gridCenterPos = _startPosition;

            cellSize = _cellSize;
            cellLayersMax = _cellLayers;
            cellLayerOffset = _cellLayerOffset;
            radius = _radius;
            gridType = _gridType;

            Generate_Grid();
        }

        public HexGrid(
            Vector3 _startPosition,
            int _cellSize,
            int _cellLayers,
            int _cellLayerOffset,
            int _maxCellsPerLayer,

            int _radius,
            Option_CellGridType _gridType
        )
        {
            gridCenterPos = _startPosition;

            cellSize = _cellSize;
            cellLayersMax = _cellLayers;
            cellLayerOffset = _cellLayerOffset;

            maxCellsPerLayer = _maxCellsPerLayer;

            radius = _radius;
            gridType = _gridType;

            Generate_Grid();
        }

        public HexGrid(
            Vector3 _startPosition,
            int _cellSize,
            Vector2Int _cellLayersMinMax,
            int _cellLayerOffset,
            int _maxCellsPerLayer,

            int _radius,
            Option_CellGridType _gridType
        )
        {
            gridCenterPos = _startPosition;

            cellSize = _cellSize;

            cellLayersMax = _cellLayersMinMax.y;
            cellLayerOffset = _cellLayerOffset;

            maxCellsPerLayer = _maxCellsPerLayer;

            radius = _radius;
            gridType = _gridType;


            Dictionary<int, Dictionary<Vector2, Vector3>> new_cellCenters_ByLookup_BySize = Generate_CenterPointsGrid(
                gridCenterPos,
                (HexCellSizes)cellSize,
                gridType,
                (HexCellSizes)radius,
                maxCellsPerLayer,
                consecutiveExpansionGridHostMembersMax,
                consecutiveExpansionShuffle
            );

            cellLookup_ByLayer_BySize = Generate_GridFromCenterPoints(
                new_cellCenters_ByLookup_BySize,
                baseLayer,
                gridCenterPos,
                radius,
                cellLayerOffset,
                true,
                cellLayersMax,
                _cellLayersMinMax.x
            );

            // foreach (var kvp in cellLookup_ByLayer_BySize)
            // {
            //     int currentSize = kvp.Key;

            //     foreach (int currentLayer in cellLookup_ByLayer_BySize[currentSize].Keys)
            //     {
            //         // Debug.Log("currentSize: " + currentSize + ",  currentLayer: " + currentLayer);
            //         foreach (Vector2 lookup in cellLookup_ByLayer_BySize[currentSize][currentLayer].Keys)
            //         {
            //             HexagonCellPrototype cell = cellLookup_ByLayer_BySize[currentSize][currentLayer][lookup];
            //             if (lookup != cell.GetLookup())
            //             {
            //                 Debug.LogError("lookup != cell.GetLookup(), lookup: " + lookup + ", cell.GetLookup" + cell.GetLookup());
            //             }
            //             // else
            //             // {
            //             //     Debug.Log("cell lookup: " + lookup);
            //             //     // Debug.Log("cell lookup: " + cell.GetLookup() + ", " + lookup);
            //             // }
            //         }
            //     }
            // }
            // }
        }

        public Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>> cellLookup_ByLayer_BySize { get; private set; } = new Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>();
        public void SetCells(
            Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>> updated_cellLookup_ByLayer_BySize,
            Dictionary<int, List<HexagonCellPrototype>> neighborsToEvaluate_bySize
        )
        {
            cellLookup_ByLayer_BySize = updated_cellLookup_ByLayer_BySize;
            foreach (var kvp in neighborsToEvaluate_bySize)
            {
                int currentSize = kvp.Key;
                // Debug.Log("subcell neighbors To evaluate - currentSize: " + currentSize);
                HexCellUtil.Evaluate_SubCellNeighbors(
                    neighborsToEvaluate_bySize[currentSize],
                    cellLookup_ByLayer_BySize[currentSize],
                    false,
                    false
                );
            }
        }

        public Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>> GetCellsByLayer() => cellLookup_ByLayer_BySize[cellSize];
        public Dictionary<Vector2, HexagonCellPrototype> GetBaseLayerCells() => cellLookup_ByLayer_BySize[cellSize][baseLayer];
        public HexagonCellPrototype GetBaseLayerCell(Vector2 cellLookup)
        {
            if (
                cellLookup_ByLayer_BySize.ContainsKey(cellSize) &&
                cellLookup_ByLayer_BySize[cellSize].ContainsKey(baseLayer) &&
                cellLookup_ByLayer_BySize[cellSize][baseLayer].ContainsKey(cellLookup)
            )
            {
                return cellLookup_ByLayer_BySize[cellSize][baseLayer][cellLookup];
            }
            return null;
        }

        public HexagonCellPrototype GetCell(Vector2 cellLookup, int cellLayer, bool logErrors = false)
        {
            if (logErrors)
            {
                if (cellLookup_ByLayer_BySize.ContainsKey(cellSize) == false)
                {
                    Debug.LogError("cellLookup_ByLayer_BySize.ContainsKey(cellSize) == false, cellSize: " + cellSize);
                    return null;
                }
                if (cellLookup_ByLayer_BySize[cellSize].ContainsKey(cellLayer) == false)
                {
                    Debug.LogError("cellLookup_ByLayer_BySize[cellSize].ContainsKey(cellLayer) == false, cellLayer: " + cellLayer);
                    return null;
                }
                if (cellLookup_ByLayer_BySize[cellSize][cellLayer].ContainsKey(cellLookup) == false)
                {
                    Debug.LogError("cellLookup_ByLayer_BySize[cellSize][cellLayer].ContainsKey(cellLookup) == false, cellLookup: " + cellLookup);
                    return null;
                }

                return cellLookup_ByLayer_BySize[cellSize][cellLayer][cellLookup];
            }
            else
            {
                if (
                    cellLookup_ByLayer_BySize.ContainsKey(cellSize) &&
                    cellLookup_ByLayer_BySize[cellSize].ContainsKey(cellLayer) &&
                    cellLookup_ByLayer_BySize[cellSize][cellLayer].ContainsKey(cellLookup)
                ) return cellLookup_ByLayer_BySize[cellSize][cellLayer][cellLookup];
            }
            return null;
        }

        public HexagonCellPrototype GetContainingCell(SurfaceBlock block, bool logErrors = false)
        {
            int currentLayer = HexCoreUtil.Calculate_CellLayer(block, cellLayerOffset);
            List<Vector2> nearestCellLookups = HexCoreUtil.Calculate_ClosestHexLookups_X7(block.Position, cellSize);
            // Debug.Log("currentLayer: " + currentLayer + ", cellSize: " + cellSize);

            if (cellLookup_ByLayer_BySize.ContainsKey(cellSize) && cellLookup_ByLayer_BySize[cellSize].ContainsKey(currentLayer))
            {
                foreach (Vector2 currentLookup in nearestCellLookups)
                {
                    HexagonCellPrototype currentCell = GetCell(currentLookup, currentLayer, logErrors);
                    if (currentCell == null) continue;
                    // Debug.Log("currentCell - layer: " + currentLayer + ", lookup: " + currentLookup);

                    if (VectorUtil.IsPointWithinPolygon(block.Position, currentCell.cornerPoints))
                    {
                        // Debug.Log("currentLayer: " + currentLayer);
                        // if (VectorUtil.IsBlockWithinVerticalBounds(currentCell.center.y, currentCell.layerOffset, block.Position, block.size))
                        // {
                        return currentCell;
                        // }
                        // else
                        // {
                        //     Debug.LogError("VectorUtil.IsBlockWithinVerticalBounds == false");
                        //     return null;
                        // }
                    }
                }
            }
            // else
            // {
            //     Debug.LogError("(cellLookup_ByLayer_BySize.ContainsKey(cellSize) == false|| cellLookup_ByLayer_BySize[cellSize].ContainsKey(currentLayer)) == false: ");
            // }
            return null;
        }

        public bool HasCell(Vector2 cellLookup, int cellLayer)
        {
            if (
                cellLookup_ByLayer_BySize.ContainsKey(cellSize) &&
                cellLookup_ByLayer_BySize[cellSize].ContainsKey(cellLayer) &&
                cellLookup_ByLayer_BySize[cellSize][cellLayer].ContainsKey(cellLookup)
            )
            {
                return cellLookup_ByLayer_BySize[cellSize][cellLayer][cellLookup] != null;
            }
            return false;
        }


        [Header("Cell Grid Settings")]
        private int maxCellsPerLayer = 7;
        [SerializeField] private bool consecutiveExpansionShuffle;
        [Range(1, 72)][SerializeField] private int consecutiveExpansionGridHostMembersMax = 3;
        [Range(0, 3)][SerializeField] private int radiusMult = 1;

        [Header(" ")]
        public int cellSize = 12;
        public int radius = 12;
        private Option_CellGridType gridType = Option_CellGridType.Defualt;

        [Header("Layer Settings")]
        public int cellLayersMax = 3;
        public int cellLayerOffset = 3;
        private float centerPosYOffset = 0;
        public int baseLayer;
        public Vector3 gridCenterPos { get; private set; }

        #region Name
        List<HexagonCellPrototype> baseEdges = new List<HexagonCellPrototype>();
        List<HexagonCellPrototype> allBaseCells = new List<HexagonCellPrototype>();
        List<HexagonCellPrototype> pathCells = new List<HexagonCellPrototype>();
        Dictionary<int, List<HexagonCellPrototype>> baseCellClustersList = null;
        HexagonCellPrototype entranceCell = null;
        HexagonSide entranceSide;
        Bounds gridStructureBounds;
        #endregion


        public static Dictionary<int, Dictionary<Vector2, Vector3>> Generate_CenterPointsGrid(
            Vector3 gridCenter,
            HexCellSizes cellSize,
            Option_CellGridType gridType,
            HexCellSizes radius,
            int maxCellsPerLayer,
            int consecutiveExpansionGridHostMembersMax = 3,
            bool consecutiveExpansionShuffle = false
        )
        {
            Dictionary<int, Dictionary<Vector2, Vector3>> new_cellCenters_ByLookup_BySize = new Dictionary<int, Dictionary<Vector2, Vector3>>();
            int _cellSize = (int)cellSize;
            int _radius = (int)radius;

            // Debug.Log("_cellSize: " + _cellSize + ", _radius: " + _radius + ", gridType: " + gridType);

            if (gridType == Option_CellGridType.RandomConsecutive || gridType == Option_CellGridType.RandomConsecutiveByHost)
            {
                int _hostCellSize = gridType == Option_CellGridType.RandomConsecutiveByHost ? _cellSize * 3 : _cellSize;

                // Debug.Log("_cellSize: " + _cellSize + ", _hostCellSize: " + _hostCellSize + ", _radius: " + _radius + ", gridType: " + gridType);

                Dictionary<Vector2, Vector3> initialPoints_ByLookup = HexGridPathingUtil.GetConsecutiveCellPoints(
                    gridCenter,
                    maxCellsPerLayer,
                    _hostCellSize,
                    _radius,
                    HexNeighborExpansionSize.X_7,
                    HexNeighborExpansionSize.X_7
                );

                if (gridType == Option_CellGridType.RandomConsecutiveByHost)
                {
                    Dictionary<Vector2, Vector3> new_cellCenters_ByLookup = HexGridUtil.Generate_HexGridCenterPointsWithinHosts(initialPoints_ByLookup.Values.ToList(), _cellSize, _hostCellSize, false);
                    new_cellCenters_ByLookup_BySize = new Dictionary<int, Dictionary<Vector2, Vector3>>() {
                        { _cellSize, new_cellCenters_ByLookup },
                        // { _hostCellSize, initialPoints_ByLookup }
                    };
                }
                else
                {
                    new_cellCenters_ByLookup_BySize = new Dictionary<int, Dictionary<Vector2, Vector3>>() {
                    { (int)cellSize, initialPoints_ByLookup },
                };
                }
            }
            else if (gridType == Option_CellGridType.ConsecutiveHost)
            {
                new_cellCenters_ByLookup_BySize = HexGridUtil.Generate_RandomHexGridCenterPoints_BySize(
                    gridCenter,
                    _cellSize,
                    new Vector2Int(consecutiveExpansionGridHostMembersMax, consecutiveExpansionGridHostMembersMax),
                    _radius,
                    consecutiveExpansionShuffle
                );
            }
            else
            {
                if (cellSize < HexCellSizes.X_12 && radius < HexCellSizes.X_12)
                {
                    new_cellCenters_ByLookup_BySize = HexGridUtil.Generate_HexGridCenterPoints_X7(gridCenter, _cellSize);
                }
                else new_cellCenters_ByLookup_BySize = HexGridUtil.Generate_HexGridCenterPoints_BySize(
                                gridCenter,
                                _cellSize,
                                _radius
                            );
            }

            return new_cellCenters_ByLookup_BySize;
        }


        public void Generate_Grid(Dictionary<int, Dictionary<Vector2, Vector3>> new_cellCenters_ByLookup_BySize = null)
        {
            baseLayer = HexCoreUtil.Calculate_CellSnapLayer(cellLayerOffset, gridCenterPos.y);

            HashSet<string> neighborIDsToEvaluate = new HashSet<string>();
            Dictionary<int, List<HexagonCellPrototype>> neighborsToEvaluate_bySize = new Dictionary<int, List<HexagonCellPrototype>>();

            if (new_cellCenters_ByLookup_BySize == null)
            {
                new_cellCenters_ByLookup_BySize = Generate_CenterPointsGrid(
                    gridCenterPos,
                    (HexCellSizes)cellSize,
                    gridType,
                    (HexCellSizes)radius,
                    maxCellsPerLayer,
                    consecutiveExpansionGridHostMembersMax,
                    consecutiveExpansionShuffle
                );
            }

            Vector3[] radiusCorners = HexCoreUtil.GenerateHexagonPoints(gridCenterPos, radius);
            int created = 0;

            Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>> new_cellLookup_ByLayer_BySize = new Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>();

            foreach (int currentSize in new_cellCenters_ByLookup_BySize.Keys)
            {
                if (currentSize >= radius) continue;
                // if (currentSize > (int)HexCellSizes.Default) continue;
                if (currentSize > cellSize) continue;
                int childSize = (int)HexCellSizes.X_4;

                //Add currentSize & childSize
                if (new_cellLookup_ByLayer_BySize.ContainsKey(currentSize) == false)
                {
                    new_cellLookup_ByLayer_BySize.Add(currentSize, new Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>() {
                            { baseLayer, new Dictionary<Vector2, HexagonCellPrototype>() }
                        });
                    neighborsToEvaluate_bySize.Add(currentSize, new List<HexagonCellPrototype>());
                }

                if (new_cellLookup_ByLayer_BySize.ContainsKey(childSize) == false)
                {
                    new_cellLookup_ByLayer_BySize.Add(childSize, new Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>(){
                            { baseLayer, new Dictionary<Vector2, HexagonCellPrototype>() }
                        });
                    neighborsToEvaluate_bySize.Add(childSize, new List<HexagonCellPrototype>());
                }

                foreach (var kvp in new_cellCenters_ByLookup_BySize[currentSize])
                {
                    Vector3 point = kvp.Value;
                    Vector2 pointLookup = HexCoreUtil.Calculate_CenterLookup(point, currentSize);

                    if (HexCoreUtil.IsAnyHexPointWithinPolygon(point, currentSize, radiusCorners))
                    {
                        HexagonCellPrototype newCell = new HexagonCellPrototype(point, currentSize, null, cellLayerOffset, "", true);
                        Vector2 worldspaceLookup = HexCoreUtil.Calculate_ClosestHexCenter_V2(point, radius);
                        newCell.SetWorldCoordinate(new Vector2(point.x, point.y));
                        newCell.SetWorldSpaceLookup(worldspaceLookup);
                        int currentGroundLayer = newCell.layer;

                        baseLayer = currentGroundLayer;


                        if (new_cellLookup_ByLayer_BySize[currentSize].ContainsKey(currentGroundLayer) == false) new_cellLookup_ByLayer_BySize[currentSize].Add(currentGroundLayer, new Dictionary<Vector2, HexagonCellPrototype>());
                        if (new_cellLookup_ByLayer_BySize[currentSize][currentGroundLayer].ContainsKey(pointLookup)) continue;

                        CellStatus groundTypeFound = groundTypeFound = newCell.GetCellStatus();

                        bool addChildren = cellSize > (int)HexCellSizes.X_4;

                        List<HexagonCellPrototype> childCells = null;

                        if (addChildren)
                        {
                            if (new_cellLookup_ByLayer_BySize.ContainsKey(childSize) == false)
                            {
                                new_cellLookup_ByLayer_BySize.Add(childSize, new Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>());
                            }

                            if (new_cellLookup_ByLayer_BySize[childSize].ContainsKey(currentGroundLayer) == false)
                            {
                                new_cellLookup_ByLayer_BySize[childSize].Add(currentGroundLayer, new Dictionary<Vector2, HexagonCellPrototype>());
                            }

                            // Generate child cells within here
                            List<Vector3> childrenX4 = HexCoreUtil.GenerateHexCenterPoints_X13(newCell.center, childSize);
                            childCells = new List<HexagonCellPrototype>();
                            foreach (Vector3 childPoint in childrenX4)
                            {
                                Vector2 childLookup = HexCoreUtil.Calculate_CenterLookup(childPoint, childSize);
                                if (
                                    new_cellLookup_ByLayer_BySize[childSize][currentGroundLayer].ContainsKey(childLookup) == false &&
                                    HexCoreUtil.IsAnyHexPointWithinPolygon(childPoint, childSize, radiusCorners)
                                )
                                {
                                    HexagonCellPrototype newChildCell = new HexagonCellPrototype(childPoint, childSize, newCell, cellLayerOffset);
                                    newChildCell.SetWorldSpaceLookup(worldspaceLookup);

                                    new_cellLookup_ByLayer_BySize[childSize][currentGroundLayer].Add(childLookup, newChildCell);
                                    childCells.Add(newChildCell);

                                    if (neighborIDsToEvaluate.Contains(newChildCell.Get_Uid()) == false)
                                    {
                                        neighborIDsToEvaluate.Add(newChildCell.Get_Uid());
                                        neighborsToEvaluate_bySize[childSize].Add(newChildCell);
                                    }
                                }
                            }
                        }

                        if (new_cellLookup_ByLayer_BySize[currentSize][currentGroundLayer].ContainsKey(pointLookup)) continue;

                        new_cellLookup_ByLayer_BySize[currentSize][currentGroundLayer].Add(pointLookup, newCell);

                        // Add to neighborsToEvaluate_bySize
                        if (neighborIDsToEvaluate.Contains(newCell.Get_Uid()) == false)
                        {
                            neighborIDsToEvaluate.Add(newCell.Get_Uid());
                            neighborsToEvaluate_bySize[currentSize].Add(newCell);
                        }

                        if (cellLayersMax < 2 || (currentSize > (int)HexCellSizes.X_12)) continue;

                        // Generate new layers 
                        HexagonCellPrototype previousCell = newCell;
                        List<HexagonCellPrototype> previousChildCells = childCells;

                        // Add Upper Layers
                        int startingLayer = (currentGroundLayer);
                        int topLayer = startingLayer + cellLayersMax;
                        // int bottomLayer = baseLayer;

                        for (int currentLayer = startingLayer; currentLayer < topLayer; currentLayer++)
                        {
                            HexagonCellPrototype nextLayerCell = HexagonCellPrototype.DuplicateCellToNewLayer_Above(previousCell, cellLayerOffset, null);

                            if (new_cellLookup_ByLayer_BySize[currentSize].ContainsKey(currentLayer) == false)
                            {
                                new_cellLookup_ByLayer_BySize[currentSize].Add(currentLayer, new Dictionary<Vector2, HexagonCellPrototype>());
                            }
                            else if (new_cellLookup_ByLayer_BySize[currentSize][currentLayer].ContainsKey(nextLayerCell.GetLookup())) continue;


                            if (groundTypeFound == CellStatus.FlatGround) nextLayerCell.SetCellStatus(CellStatus.AboveGround);

                            new_cellLookup_ByLayer_BySize[currentSize][currentLayer].Add(nextLayerCell.GetLookup(), nextLayerCell);

                            // Add to neighborsToEvaluate_bySize
                            if (neighborIDsToEvaluate.Contains(nextLayerCell.Get_Uid()) == false)
                            {
                                neighborIDsToEvaluate.Add(nextLayerCell.Get_Uid());
                                neighborsToEvaluate_bySize[currentSize].Add(nextLayerCell);
                            }

                            previousCell = nextLayerCell;

                            // Duplicate Children Cells to Next Layer
                            if (previousChildCells != null)
                            {
                                if (new_cellLookup_ByLayer_BySize[childSize].ContainsKey(currentLayer) == false) new_cellLookup_ByLayer_BySize[childSize].Add(currentLayer, new Dictionary<Vector2, HexagonCellPrototype>());

                                List<HexagonCellPrototype> nextLayerChildCells = new List<HexagonCellPrototype>();
                                foreach (HexagonCellPrototype prevChild in previousChildCells)
                                {
                                    if (new_cellLookup_ByLayer_BySize[childSize][currentLayer].ContainsKey(prevChild.GetLookup())) continue;

                                    HexagonCellPrototype newChildCell = HexagonCellPrototype.DuplicateCellToNewLayer_Above(prevChild, cellLayerOffset, nextLayerCell);

                                    new_cellLookup_ByLayer_BySize[childSize][currentLayer].Add(newChildCell.GetLookup(), newChildCell);

                                    nextLayerChildCells.Add(newChildCell);

                                    if (neighborIDsToEvaluate.Contains(newChildCell.Get_Uid()) == false)
                                    {
                                        neighborIDsToEvaluate.Add(newChildCell.Get_Uid());
                                        neighborsToEvaluate_bySize[childSize].Add(newChildCell);
                                    }
                                }

                                previousChildCells = nextLayerChildCells;
                            }
                        }


                        created++;
                    }
                }
            }

            foreach (var kvp in neighborsToEvaluate_bySize)
            {
                int currentSize = kvp.Key;
                // Debug.Log("subcell neighbors To evaluate - currentSize: " + currentSize);
                HexCellUtil.Evaluate_SubCellNeighbors(
                    neighborsToEvaluate_bySize[currentSize],
                    new_cellLookup_ByLayer_BySize[currentSize],
                    false
                );
            }

            cellLookup_ByLayer_BySize = new_cellLookup_ByLayer_BySize;
        }


        // public static Dictionary<int, Dictionary<Vector2, Vector3>> Generate_GridCenterPointsType(
        //     HexCellSizes cellSize,
        //     Option_CellGridType gridType,
        //     Vector3 gridCenterPos,
        //     int gridRadius,
        //     int maxCellsPerLayer,
        //     int consecutiveExpansionGridHostMembersMax,
        //     bool consecutiveExpansionShuffle
        // )
        // {
        //     Dictionary<int, Dictionary<Vector2, Vector3>> new_cellCenters_ByLookup_BySize = null;

        //     if (gridType == Option_CellGridType.RandomConsecutive || gridType == Option_CellGridType.RandomConsecutiveByHost)
        //     {
        //         int _hostCellSize = gridType == Option_CellGridType.RandomConsecutiveByHost ? (int)cellSize * 3 : (int)cellSize;

        //         Dictionary<Vector2, Vector3> initialPoints_ByLookup = HexGridPathingUtil.GetConsecutiveCellPoints(
        //             gridCenterPos,
        //             maxCellsPerLayer,
        //             _hostCellSize,
        //             gridRadius,
        //             HexNeighborExpansionSize.Default,
        //             HexNeighborExpansionSize.Default
        //         );

        //         if (gridType == Option_CellGridType.RandomConsecutiveByHost)
        //         {
        //             Dictionary<Vector2, Vector3> new_cellCenters_ByLookup = HexGridUtil.Generate_HexGridCenterPointsWithinHosts(initialPoints_ByLookup.Values.ToList(), cellSize, _hostCellSize, false);
        //             new_cellCenters_ByLookup_BySize = new Dictionary<int, Dictionary<Vector2, Vector3>>() {
        //                 { (int)cellSize, new_cellCenters_ByLookup },
        //                 // { _hostCellSize, initialPoints_ByLookup }
        //             };
        //         }
        //         else
        //         {
        //             new_cellCenters_ByLookup_BySize = new Dictionary<int, Dictionary<Vector2, Vector3>>() {
        //             { (int)cellSize, initialPoints_ByLookup },
        //         };
        //         }
        //     }
        //     else if (gridType == Option_CellGridType.ConsecutiveHost)
        //     {
        //         new_cellCenters_ByLookup_BySize = HexGridUtil.Generate_RandomHexGridCenterPoints_BySize(
        //             gridCenterPos,
        //             (int)cellSize,
        //             new Vector2Int(consecutiveExpansionGridHostMembersMax, consecutiveExpansionGridHostMembersMax),
        //             gridRadius,
        //             consecutiveExpansionShuffle
        //         );
        //     }
        //     else
        //     {
        //         new_cellCenters_ByLookup_BySize = radiusMult < 1 ?
        //         HexGridUtil.Generate_HexGridCenterPoints_X7(gridCenterPos, (int)cellSize) :

        //         HexGridUtil.Generate_HexGridCenterPoints_BySize(
        //             gridCenterPos,
        //             (int)HexCellSizes.X_4,
        //             gridRadius
        //         );
        //     }

        //     return new_cellCenters_ByLookup_BySize;
        // }

        public static Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>> Generate_GridFromCenterPoints(
            Dictionary<int, Dictionary<Vector2, Vector3>> new_cellCenters_ByLookup_BySize,
            int baseLayer,
            Vector3 gridCenterPos,
            int gridRadius,
            int layerOffset,
            bool randomizeLayers,
            int layersMax = 1,
            int layersMin = 1
        )
        {
            // Debug.Log("layersMax: " + layersMax + ", layersMin: " + layersMin);

            Vector3[] radiusCorners = HexCoreUtil.GenerateHexagonPoints(gridCenterPos, gridRadius);
            int created = 0;
            Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>> new_cellLookup_ByLayer_BySize = new Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>();
            Dictionary<int, List<HexagonCellPrototype>> neighborsToEvaluate_bySize = new Dictionary<int, List<HexagonCellPrototype>>();

            foreach (int currentSize in new_cellCenters_ByLookup_BySize.Keys)
            {
                //Add currentSize
                if (new_cellLookup_ByLayer_BySize.ContainsKey(currentSize) == false)
                {
                    new_cellLookup_ByLayer_BySize.Add(currentSize, new Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>() {
                            { baseLayer, new Dictionary<Vector2, HexagonCellPrototype>() }
                        });
                    neighborsToEvaluate_bySize.Add(currentSize, new List<HexagonCellPrototype>());
                }

                // Create new HexCells at center points
                foreach (var kvp in new_cellCenters_ByLookup_BySize[currentSize])
                {
                    Vector3 point = kvp.Value;
                    Vector2 pointLookup = HexCoreUtil.Calculate_CenterLookup(point, currentSize);

                    if (pointLookup != kvp.Key) Debug.LogError("(pointLookup != kvp.Key) - pointLookup: " + pointLookup + ", " + kvp.Key);

                    if (HexCoreUtil.IsAnyHexPointWithinPolygon(point, currentSize, radiusCorners))
                    {
                        HexagonCellPrototype newCell = new HexagonCellPrototype(point, currentSize, null, layerOffset, "", true);
                        Vector2 worldspaceLookup = HexCoreUtil.Calculate_ClosestHexCenter_V2(point, (int)HexCellSizes.X_108);
                        newCell.SetWorldCoordinate(new Vector2(point.x, point.y));
                        newCell.SetWorldSpaceLookup(worldspaceLookup);
                        int currentGroundLayer = newCell.layer;

                        baseLayer = currentGroundLayer;

                        if (new_cellLookup_ByLayer_BySize[currentSize].ContainsKey(currentGroundLayer) == false) new_cellLookup_ByLayer_BySize[currentSize].Add(currentGroundLayer, new Dictionary<Vector2, HexagonCellPrototype>());
                        if (new_cellLookup_ByLayer_BySize[currentSize][currentGroundLayer].ContainsKey(pointLookup)) continue;

                        // CellStatus groundTypeFound = groundTypeFound = newCell.GetCellStatus();

                        if (new_cellLookup_ByLayer_BySize[currentSize][currentGroundLayer].ContainsKey(pointLookup)) continue;

                        new_cellLookup_ByLayer_BySize[currentSize][currentGroundLayer].Add(pointLookup, newCell);

                        // Add to neighborsToEvaluate_bySize
                        // if (neighborIDsToEvaluate.Contains(newCell.Get_Uid()) == false)
                        // {
                        //     neighborIDsToEvaluate.Add(newCell.Get_Uid());
                        neighborsToEvaluate_bySize[currentSize].Add(newCell);
                        // }

                        created++;

                        int cellLayersMax = randomizeLayers ? UnityEngine.Random.Range(layersMin, layersMax + 1) : layersMax;

                        if (cellLayersMax < 2 || (currentSize > (int)HexCellSizes.X_12)) continue;

                        // Generate new layers 
                        created += Generate_CellStack(
                            newCell,
                            cellLayersMax,
                            new_cellLookup_ByLayer_BySize,
                            neighborsToEvaluate_bySize
                        );

                    }
                }
            }

            foreach (var kvp in neighborsToEvaluate_bySize)
            {
                int currentSize = kvp.Key;
                // Debug.Log("subcell neighbors To evaluate - currentSize: " + currentSize);
                HexCellUtil.Evaluate_SubCellNeighbors(
                    neighborsToEvaluate_bySize[currentSize],
                    new_cellLookup_ByLayer_BySize[currentSize],
                    false
                );
            }
            return new_cellLookup_ByLayer_BySize;
        }


        public static int Generate_CellStack(
            HexagonCellPrototype baseCell,
            int layers,
            Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>> cellLookup_ByLayer_BySize,
            Dictionary<int, List<HexagonCellPrototype>> neighborsToEvaluate_bySize
        )
        {
            // Add Upper Layers
            int startingLayer = baseCell.layer + 1;
            int topLayer = layers;

            int layerOffeset = baseCell.layerOffset;

            HexagonCellPrototype previousCell = baseCell;
            int cellSize = baseCell.size;

            int created = 0;

            for (int currentLayer = startingLayer; currentLayer < topLayer; currentLayer++)
            {
                HexagonCellPrototype nextLayerCell = HexagonCellPrototype.DuplicateCellToNewLayer_Above(previousCell, layerOffeset, null);

                if (cellLookup_ByLayer_BySize[cellSize].ContainsKey(currentLayer) == false)
                {
                    cellLookup_ByLayer_BySize[cellSize].Add(currentLayer, new Dictionary<Vector2, HexagonCellPrototype>());
                }
                else if (cellLookup_ByLayer_BySize[cellSize][currentLayer].ContainsKey(nextLayerCell.GetLookup())) continue;

                cellLookup_ByLayer_BySize[cellSize][currentLayer].Add(nextLayerCell.GetLookup(), nextLayerCell);
                // Add to neighborsToEvaluate_bySize
                // if (neighborIDsToEvaluate.Contains(nextLayerCell.Get_Uid()) == false)
                // {
                //     neighborIDsToEvaluate.Add(nextLayerCell.Get_Uid());
                neighborsToEvaluate_bySize[cellSize].Add(nextLayerCell);
                // }

                previousCell = nextLayerCell;

                created++;
            }
            return created;
        }

    }
}

