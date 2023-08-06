using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;
using WFCSystem;

namespace WFCSystem
{
    public enum BuildingRoofType { Flat, Dome, Pointed, Tent }
    public enum BuildingStackType { Level, Focused, Dome, Castle }
    public enum StructureType { Building, Wall }

    public class BuildingPrototype
    {
        public BuildingPrototype(Vector3 _clusterCenter, Vector3 _position, Dictionary<int, Dictionary<Vector2, Vector3>> new_cellCenters_ByLookup_BySize, Transform _transform)
        {
            clusterCenter = _clusterCenter;
            position = _position;
            transform = _transform;
            InitialSetup(new_cellCenters_ByLookup_BySize);
        }

        public BuildingPrototype(Vector3 _position, Dictionary<int, Dictionary<Vector2, Vector3>> new_cellCenters_ByLookup_BySize, Transform _transform)
        {
            position = _position;
            clusterCenter = position;
            transform = _transform;
            InitialSetup(new_cellCenters_ByLookup_BySize);
        }

        public BuildingPrototype(
            Vector3 _gridCenter,
            HexCellSizes cellSize,
            float _blockSize,
            Vector2Int _nodeCellLayersMinMax,
            int _cellLayerOffset,
            int _maxCellsPerLayer,
            int _radius,
            Transform _transform,
            Option_CellGridType _gridType = Option_CellGridType.RandomConsecutive,
            int consecutiveExpansionGridHostMembersMax = 3,
            bool consecutiveExpansionShuffle = false
        )
        {
            blockSize = _blockSize;
            position = _gridCenter;
            transform = _transform;
            nodeGrid_CellLayersMin = _nodeCellLayersMinMax.x;
            nodeGrid_CellLayersMax = _nodeCellLayersMinMax.y;
            nodeGrid_CellLayerOffset = _cellLayerOffset;

            Dictionary<int, Dictionary<Vector2, Vector3>> new_cellCenters_ByLookup_BySize = HexGrid.Generate_CenterPointsGrid(
                _gridCenter,
                cellSize,
                _gridType,
                (HexCellSizes)_radius,
                _maxCellsPerLayer,
                consecutiveExpansionGridHostMembersMax,
                consecutiveExpansionShuffle
            );

            List<Vector3> centers = new_cellCenters_ByLookup_BySize[(int)cellSize].Values.ToList();
            clusterCenter = VectorUtil.Calculate_CenterPositionFromPoints(centers);

            InitialSetup(new_cellCenters_ByLookup_BySize);
        }

        [SerializeField] private Vector3 clusterCenter;
        [SerializeField] private Vector3 position;
        private Transform transform;

        [SerializeField] private BuildingPrototypeDisplaySettings displaySettings = new BuildingPrototypeDisplaySettings();
        [Header(" ")]

        [SerializeField]
        private HexGridDisplaySettings hexGridDisplaySettings = new HexGridDisplaySettings(
                CellDisplay_Type.DrawLines,
                GridFilter_Level.All,
                GridFilter_Type.All,
                HexCellSizes.Default,
                true
                );

        [Header(" ")]

        [Header("Node Grid Settings")]
        [SerializeField] private HexCellSizes nodeGrid_CellSize = HexCellSizes.X_12;
        [Range(12, 108)][SerializeField] private int nodeGrid_GridRadius = 36;
        [Header(" ")]
        [Range(1, 48)][SerializeField] private int nodeGrid_CellLayersMin = 1;
        [Range(1, 48)][SerializeField] private int nodeGrid_CellLayersMax = 2;
        [Header(" ")]
        [Range(2, 12)][SerializeField] private int nodeGrid_CellLayerOffset = 4;

        [Header(" ")]
        [Range(1, 7)][SerializeField] private int nodeGrid_MaxCellsPerLayer = 4;
        [SerializeField] private HexCellSizes nodeGrid_SnapSize = HexCellSizes.X_4;
        [SerializeField] private Option_CellGridType nodeGrid_GridType = Option_CellGridType.Defualt;
        [Header(" ")]

        [Header("Tile Grid Settings")]
        [SerializeField] private HexCellSizes tileGrid_CellSize = HexCellSizes.X_4;
        [Range(1, 48)][SerializeField] private int tileGrid_CellLayers = 1;
        [Range(2, 12)][SerializeField] private int tileGrid_CellLayerOffset = 2;
        [Range(12, 108)][SerializeField] private int tileGrid_GridRadius = 36;

        [Header("Building Settings")]
        [SerializeField] private HexagonSide buildingFront;
        [Range(0.2f, 0.9f)][SerializeField] private float innerRoomRadiusMult = 0.8f;
        [Header("Entryways")]
        [Range(1, 5)][SerializeField] private int entrancesMax = 2;
        [Range(1f, 4f)][SerializeField] private float innerEntryRadius = 3;
        [Range(1f, 4f)][SerializeField] private float doorwayRadius = 2;
        public Vector3 doorDimensions_Default = new Vector3(3f, 2.4f, 1.5f);
        public Vector3 doorDimensions_EXT = new Vector3(3f, 2.4f, 1.5f);
        public Vector3 doorDimensions_INT = new Vector3(3f, 2.4f, 2f);

        [Header("Windows")]
        public Vector3 windowDimensions = new Vector3(3f, 1.3f, 1f);
        [Range(0.2f, 0.6f)][SerializeField] float windowElevationOffsetMult = 0.3f;

        [Header("Stairs")]
        public Vector3 stairDimensions = new Vector3(3f, 1.3f, 1f);
        public Vector3 stairwayDimensions = new Vector3(3f, 2f, 3f);

        [Header(" ")]
        [Header("Surface Block Settings")]
        [Range(0.25f, 10f)][SerializeField] private float blockSize = 1f;
        [Range(12, 128)][SerializeField] private float boundsSize = 25;

        public HexGrid hexNodeGrid { get; private set; } = null;
        public HexGrid hexTileGrid { get; private set; } = null;

        Bounds gridBounds;
        List<Bounds> structureBounds = new List<Bounds>();
        Dictionary<Vector3, SurfaceBlock> surfaceBlockCenterLookups = null;
        Dictionary<Vector2, Dictionary<string, BoundsShapeBlock>> boundsShapesByCellLookup = null;
        List<RectangleBounds> buildingBoundsShells = null;
        List<RectangleBounds> rect_doorwaysInner = null;
        List<RectangleBounds> rect_doorwaysOuter = null;
        List<RectangleBounds> rect_windows = null;
        List<RectangleBounds> rect_stairways = null;
        List<HexagonCellPrototype> baseEdges = null;
        List<HexagonCellPrototype> baseInners = null;
        Dictionary<Vector2, HexagonCellPrototype> stairwayCells = new Dictionary<Vector2, HexagonCellPrototype>();
        Dictionary<HexagonCellPrototype, List<SurfaceBlock>> surfaceBlocksByCell = null;
        List<HexagonCellPrototype> pathCells = null;
        Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, List<SurfaceBlock>>> tileInnerEdgesByCellSide = null;
        Dictionary<Vector3, Vector3> markerPoints_spawn = null;

        #region Saved State
        Vector3 _lastPosition;
        float _boundsSize;
        float _blockSize;
        float _updateDist = 1f;
        Dictionary<string, Color> customColors = UtilityHelpers.CustomColorDefaults();
        #endregion

        private void InitialSetup(Dictionary<int, Dictionary<Vector2, Vector3>> new_cellCenters_ByLookup_BySize = null)
        {
            if (nodeGrid_CellLayersMin > nodeGrid_CellLayersMax) nodeGrid_CellLayersMax = nodeGrid_CellLayersMin;

            EvaluateGridLayers();

            Vector2Int _cellLayersMinMax = new Vector2Int(nodeGrid_CellLayersMin, nodeGrid_CellLayersMax);

            if (new_cellCenters_ByLookup_BySize != null)
            {
                hexNodeGrid = new HexGrid(
                        position,
                        new_cellCenters_ByLookup_BySize,
                        (int)nodeGrid_CellSize,
                        nodeGrid_CellLayersMax,
                        nodeGrid_CellLayerOffset,
                        nodeGrid_GridRadius
                    );

            }
            else
            {
                hexNodeGrid = new HexGrid(
                    position,
                    (int)nodeGrid_CellSize,
                    _cellLayersMinMax,
                    nodeGrid_CellLayerOffset,
                    nodeGrid_MaxCellsPerLayer,

                    nodeGrid_GridRadius,

                    nodeGrid_GridType
                );
            }

            hexTileGrid = new HexGrid(
                    position,
                    (int)tileGrid_CellSize,
                    tileGrid_CellLayers,
                    tileGrid_CellLayerOffset,
                    (int)HexCellSizes.X_36,
                    Option_CellGridType.Defualt,
                    hexNodeGrid
                );

            HexGridToBuilding();

            if (displaySettings.enable_blockGrid) Generate_BlockGrid();
        }

        public void Draw(BuildingPrototypeDisplaySettings _displaySettings = null, HexGridDisplaySettings _hexGridDisplaySettings = null)
        {
            if (_displaySettings != null) displaySettings = _displaySettings;
            if (_hexGridDisplaySettings != null) hexGridDisplaySettings = _hexGridDisplaySettings;

            if (displaySettings.show_clusterCenter)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(clusterCenter, 3);
            }

            if (displaySettings.show_buildingBoundsShells && buildingBoundsShells != null)
            {
                foreach (var item in buildingBoundsShells)
                {
                    Gizmos.color = Color.white;
                    item.Draw();
                }
            }

            if (displaySettings.show_markers && markerPoints_spawn != null)
            {
                foreach (var markerPoint in markerPoints_spawn.Values)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(markerPoint, 0.5f);
                }
            }

            if (displaySettings.show_blockBounds)
            {
                Gizmos.color = Color.white;
                VectorUtil.DrawRectangleLines(gridBounds);
            }

            if (displaySettings.show_doorways)
            {
                if (rect_doorwaysOuter != null)
                {
                    Gizmos.color = Color.green;
                    foreach (var entry in rect_doorwaysOuter)
                    {
                        entry.Draw();
                    }
                }
                if (rect_doorwaysInner != null)
                {
                    Gizmos.color = Color.blue;
                    foreach (var entry in rect_doorwaysInner)
                    {
                        entry.Draw();
                    }
                }
            }

            if (displaySettings.show_windows)
            {
                if (rect_windows != null)
                {
                    Gizmos.color = Color.cyan;
                    foreach (var window in rect_windows)
                    {
                        window.Draw();
                    }
                }
            }

            if (displaySettings.show_stairwayNodes)
            {
                if (rect_stairways != null)
                {
                    Gizmos.color = Color.yellow;
                    foreach (var stairways in rect_stairways)
                    {
                        stairways.Draw();
                    }
                }
            }


            if (displaySettings.show_nodeGrid && hexNodeGrid != null && hexNodeGrid.cellLookup_ByLayer_BySize != null && hexNodeGrid.cellLookup_ByLayer_BySize.Count > 0)
            {
                foreach (var kvp in hexNodeGrid.cellLookup_ByLayer_BySize)
                {
                    int currentSize = kvp.Key;
                    if (currentSize != (int)nodeGrid_CellSize) continue;

                    Gizmos.color = Color.yellow;
                    HexagonCellPrototype.DrawHexagonCellPrototypeGrid(
                        hexNodeGrid.cellLookup_ByLayer_BySize[currentSize],
                        hexGridDisplaySettings.gridFilter_Type,
                        GridFilter_Level.HostCells,
                        hexGridDisplaySettings.cellDisplayType,
                        false,
                        hexGridDisplaySettings.showHighlights,
                        true
                    );

                    foreach (int currentLayer in hexNodeGrid.cellLookup_ByLayer_BySize[currentSize].Keys)
                    {
                        foreach (HexagonCellPrototype cell in hexNodeGrid.cellLookup_ByLayer_BySize[currentSize][currentLayer].Values)
                        {
                            if (cell.IsEdge() == false) continue;

                            // if (cell.IsPath())
                            // {
                            //     List<HexagonSide> nullNeighborSides = cell.GetNeighborSides(Filter_CellType.NullValue);
                            //     Gizmos.color = Color.green;
                            //     foreach (var ix in nullNeighborSides)
                            //     {
                            //         Gizmos.DrawWireSphere(cell.sidePoints[(int)ix], cell.size * 0.4f);

                            //         // Vector3 dimensions = new Vector3(5f, (cell.layerOffset * 0.65f) - doorwayRadius, 1.8f);
                            //         // RectangleBounds rect_A = new RectangleBounds(cell.buildingNodeEdgePoints[item], doorwayRadius, item, dimensions);
                            //         // rect_A.Draw();
                            //     }
                            // }

                            if (cell.buildingNodeEdgePoints != null)
                            {
                                Gizmos.color = Color.white;
                                cell.Draw_NodeEdges();
                                // foreach (var item in cell.buildingNode_SlicedCorners)
                                // {

                                //     Gizmos.color = Color.red;
                                //     Gizmos.DrawSphere(cell.buildingNodeEdgePoints[item], 1f);

                                //     // Vector3 dimensions = new Vector3(5f, (cell.layerOffset * 0.65f) - doorwayRadius, 1.8f);
                                //     // RectangleBounds rect_A = new RectangleBounds(cell.buildingNodeEdgePoints[item], doorwayRadius, item, dimensions);
                                //     // rect_A.Draw();

                                // }
                            }
                        }
                    }

                }

                // if (displaySettings.show_stairwayNodes && stairwayCells != null)
                // {
                //     foreach (var cell in stairwayCells.Values)
                //     {
                //         Gizmos.color = Color.magenta;
                //         Gizmos.DrawWireSphere(cell.center, cell.size * 0.7f);

                //         if (cell.layerNeighbors[1] != null)
                //         {
                //             Gizmos.color = customColors["orange"];
                //             Gizmos.DrawWireSphere(cell.layerNeighbors[1].center, cell.size * 0.5f);
                //         }
                //     }
                // }

                if (displaySettings.show_clearingBounds)
                {

                    if (boundsShapesByCellLookup != null)
                    {
                        Gizmos.color = customColors["purple"];
                        foreach (var k in boundsShapesByCellLookup.Keys)
                        {
                            foreach (var shape in boundsShapesByCellLookup[k].Values)
                            {
                                shape.DrawPoints();
                            }
                        }
                    }
                    // if (clearWithinBounds != null)
                    // {
                    //     Gizmos.color = customColors["orange"];
                    //     foreach (var item in clearWithinBounds)
                    //     {
                    //         item.DrawPoints();
                    //     }
                    // }
                }

                // if (show_clearingBounds && pathCells != null && pathCells.Count > 0)
                // {
                //     Gizmos.color = Color.green;
                //     foreach (var cell in pathCells)
                //     {
                //         // Gizmos.DrawWireSphere(cell.center, cell.size * 0.8f);
                //         if (cell.buildingNodeClearBounds == null) continue;
                //         foreach (var item in cell.buildingNodeClearBounds)
                //         {
                //             item.Draw();
                //         }
                //     }
                // }
            }

            if (displaySettings.show_tileGrid && hexTileGrid.cellLookup_ByLayer_BySize != null && hexTileGrid.cellLookup_ByLayer_BySize.Count > 0)
            {
                foreach (var kvp in hexTileGrid.cellLookup_ByLayer_BySize)
                {
                    int currentSize = kvp.Key;
                    if (currentSize != (int)tileGrid_CellSize) continue;

                    HexagonCellPrototype.DrawHexagonCellPrototypeGrid(
                        hexTileGrid.cellLookup_ByLayer_BySize[currentSize],
                        hexGridDisplaySettings.gridFilter_Type,
                        GridFilter_Level.HostCells,
                        hexGridDisplaySettings.cellDisplayType,
                        false,
                        true
                    // hexGridDisplaySettings.showHighlights
                    );
                }
            }

            if (displaySettings.enable_blockGrid && hexNodeGrid != null && hexTileGrid != null)
            {
                if (surfaceBlockCenterLookups == null)
                {
                    Generate_BlockGrid();
                }
                else
                {

                    if (displaySettings.show_blockGrid) SurfaceBlock.DrawGrid(surfaceBlockCenterLookups);

                    // if (surfaceBlocksByCell != null)
                    // {
                    //     foreach (HexagonCellPrototype cell in surfaceBlocksByCell.Keys)
                    //     {
                    //         // Gizmos.color = customColors["purple"];
                    //         Gizmos.color = Color.red;
                    //         Gizmos.DrawWireSphere(cell.center, 0.5f);
                    //         Gizmos.color = Color.black;
                    //         VectorUtil.DrawHexagonPointLinesInGizmos(cell.cornerPoints);
                    //     }
                    // }

                    // if (showHighlightedCell && surfaceBlocksByCell != null)
                    // {

                    //     int count = surfaceBlocksByCell.Count;
                    //     if (_highlightedCell == count) _highlightedCell = 0;
                    //     _highlightedCell = Mathf.Clamp(_highlightedCell, 0, count - 1);

                    //     int ix = 0;
                    //     foreach (var cell in surfaceBlocksByCell.Keys)
                    //     {
                    //         ix++;

                    //         if (_highlightedCell == ix)
                    //         {
                    //             if (_currentHighlightedCell != cell) _currentHighlightedCell = cell;

                    //             Gizmos.color = customColors["purple"];
                    //             Gizmos.DrawWireSphere(cell.center, 1f);

                    //             if (cell.borderPoints != null)
                    //             {
                    //                 Gizmos.color = customColors["orange"];
                    //                 foreach (var item in cell.borderPoints.Values)
                    //                 {
                    //                     Gizmos.DrawSphere(item, 0.4f);
                    //                 }
                    //             }
                    //         }
                    //     }
                    // }

                    // if (showSurfaceBlockEdgeSockets && surfaceBlocksByCell != null)
                    // {
                    //     Gizmos.color = Color.magenta;
                    //     if (tileInnerEdgesByCellSide != null)
                    //     {
                    //         foreach (var cell in tileInnerEdgesByCellSide.Keys)
                    //         {
                    //             if (showHighlightedCell && cell != _currentHighlightedCell) continue;

                    //             foreach (var side in tileInnerEdgesByCellSide[cell].Keys)
                    //             {
                    //                 if (highlight_SurfaceBlockEdgeSide && side != highlightSide) continue;

                    //                 foreach (var block in tileInnerEdgesByCellSide[cell][side])
                    //                 {
                    //                     Gizmos.DrawSphere(block.Position, 0.3f);
                    //                 }
                    //             }

                    //         }
                    //     }
                    // }

                }
            }
        }

        public void Generate_BlockGrid(bool logBuildErrors = false)
        {
            if (hexNodeGrid == null || hexTileGrid == null)
            {
                Debug.LogError("hexNodeGrid == null || hexTileGrid == null");
                return;
            }

            int maxHeight = hexNodeGrid.cellLayersMax * nodeGrid_CellLayerOffset;

            surfaceBlocksByCell = new Dictionary<HexagonCellPrototype, List<SurfaceBlock>>();

            surfaceBlockCenterLookups = SurfaceBlock.CreateSurfaceBlocks_V3(
                                                gridBounds,
                                                blockSize,
                                                position.y,
                                                maxHeight,
                                                hexNodeGrid,
                                                hexTileGrid,
                                                surfaceBlocksByCell,
                                                boundsShapesByCellLookup,
                                                logBuildErrors
                                            );

            // surfaceBlocksGrid = SurfaceBlock.ClearInnerBlocks(surfaceBlocksGrid);
            // SurfaceBlock.EvaluateTileEdges(surfaceBlocksGrid);

            hexTileGrid.ExcludeToCells(surfaceBlocksByCell);

            SurfaceBlock.EvaluateNeighbors(surfaceBlockCenterLookups);

            tileInnerEdgesByCellSide = SurfaceBlock.GetTileInnerEdgesByCellSide(surfaceBlocksByCell);

            SurfaceBlock.Generate_CellTileSocketProfiles_V2(tileInnerEdgesByCellSide);
        }

        public static void Generate_BuildingNodeEdgePoints(HexagonCellPrototype cell, HexagonSide buildingFront)
        {
            if (cell.IsEdge() == false) return;

            List<HexagonSide> neighborSides = cell.GetNeighborSides(Filter_CellType.Any);
            HexagonSide frontOpposite = HexCoreUtil.OppositeSide(buildingFront);

            if (neighborSides.Count == 2)
            {
                List<HexagonSide> nullNeighborSides = cell.GetNeighborSides(Filter_CellType.NullValue).FindAll(n => n != frontOpposite && n != buildingFront);
                List<List<HexagonSide>> consecutiveSets = HexCoreUtil.ExtractAllConsecutiveSides(nullNeighborSides);
                // Debug.Log("consecutiveSets:  " + consecutiveSets.Count);

                if (consecutiveSets.Count == 1 && consecutiveSets[0].Count == 2)
                {
                    cell.buildingNodeEdgePoints = HexCoreUtil.Generate_PartialHexagonCorners(cell.center, cell.size, consecutiveSets[0]);
                }
            }
            else if (neighborSides.Count == 3)
            {
                if (HexCoreUtil.AreSidesConsecutive(neighborSides))
                {
                    List<HexagonSide> nullNeighborSides = cell.GetNeighborSides(Filter_CellType.NullValue).FindAll(n => n != frontOpposite);
                    // Debug.Log("nullNeighborSides:  " + nullNeighborSides.Count + ",  has " + frontOpposite + ": " + nullNeighborSides.Contains(frontOpposite));
                    if (nullNeighborSides.Count == 2 && HexCoreUtil.AreSidesConsecutive(nullNeighborSides))
                    {
                        cell.buildingNodeEdgePoints = HexCoreUtil.Generate_PartialHexagonCorners(cell.center, cell.size, nullNeighborSides);
                    }
                }
            }
        }
        // public static void Generate_BuildingNodeEdgePoints(HexagonCellPrototype cell, HexagonSide buildingFront)
        // {
        //     if (cell.IsEdge() == false) return;

        //     List<HexagonSide> neighborSides = cell.GetNeighborSides(Filter_CellType.Any);
        //     HexagonSide frontOpposite = HexCoreUtil.OppositeSide(buildingFront);
        //     List<int> mutatedCornersList = new List<int>();

        //     if (neighborSides.Count == 2)
        //     {
        //         List<HexagonSide> nullNeighborSides = cell.GetNeighborSides(Filter_CellType.NullValue);
        //         List<List<HexagonSide>> consecutiveSets;
        //         // Debug.Log("consecutiveSets:  " + consecutiveSets.Count);

        //         if (HexCoreUtil.AreSidesConsecutive(neighborSides) == false)
        //         {
        //             consecutiveSets = HexCoreUtil.ExtractAllConsecutiveSides(nullNeighborSides);
        //             if (consecutiveSets.Count == 1 && consecutiveSets[0].Count == 3)
        //             {
        //                 cell.buildingNodeEdgePoints = HexCoreUtil.Generate_PartialHexagonCorners(cell.center, cell.size, consecutiveSets[0], mutatedCornersList);
        //                 cell.buildingNode_SlicedCorners = mutatedCornersList;
        //             }
        //             return;
        //         }

        //         List<HexagonSide> filteredNullNeighborSides = nullNeighborSides.FindAll(n => n != frontOpposite && n != buildingFront);
        //         consecutiveSets = HexCoreUtil.ExtractAllConsecutiveSides(filteredNullNeighborSides);

        //         if (consecutiveSets.Count == 1 && consecutiveSets[0].Count == 2)
        //         {
        //             cell.buildingNodeEdgePoints = HexCoreUtil.Generate_PartialHexagonCorners(cell.center, cell.size, consecutiveSets[0], mutatedCornersList);
        //             cell.buildingNode_SlicedCorners = mutatedCornersList;
        //         }
        //     }
        //     else if (neighborSides.Count == 3)
        //     {
        //         if (HexCoreUtil.AreSidesConsecutive(neighborSides))
        //         {
        //             List<HexagonSide> nullNeighborSides = cell.GetNeighborSides(Filter_CellType.NullValue).FindAll(n => n != frontOpposite);
        //             // Debug.Log("nullNeighborSides:  " + nullNeighborSides.Count + ",  has " + frontOpposite + ": " + nullNeighborSides.Contains(frontOpposite));
        //             if (nullNeighborSides.Count == 2 && HexCoreUtil.AreSidesConsecutive(nullNeighborSides))
        //             {
        //                 cell.buildingNodeEdgePoints = HexCoreUtil.Generate_PartialHexagonCorners(cell.center, cell.size, nullNeighborSides, mutatedCornersList);
        //                 cell.buildingNode_SlicedCorners = mutatedCornersList;
        //             }
        //         }
        //     }
        // }
        public static RectangleBounds Create_DoorwayBounds(
            Vector3 position,
            Vector3 dimensions,
            int rotation,
            Dictionary<Vector2, Dictionary<string, BoundsShapeBlock>> boundsShapesByCellLookup,
            HexCellSizes nodeCellSize = HexCellSizes.X_12,
            float sizeMult = 2
        )
        {
            RectangleBounds new_rect = new RectangleBounds(position, sizeMult, rotation, dimensions);
            BoundsShapeBlock new_doorwayShape = new BoundsShapeBlock(new_rect);
            BoundsShapeBlock.GetIntersectingHexLookups(new_doorwayShape, (int)nodeCellSize, boundsShapesByCellLookup);
            return new_rect;
        }

        public static RectangleBounds Create_StairwayBounds(
            Vector3 position,
            Vector3 dimensions,
            int rotation,
            Dictionary<Vector2, Dictionary<string, BoundsShapeBlock>> boundsShapesByCellLookup,
            HexCellSizes nodeCellSize = HexCellSizes.X_12,
            float sizeMult = 2
        )
        {
            RectangleBounds new_rect = new RectangleBounds(position, sizeMult, rotation, dimensions);
            BoundsShapeBlock new_stairway = new BoundsShapeBlock(new_rect);
            BoundsShapeBlock.GetIntersectingHexLookups(new_stairway, (int)nodeCellSize, boundsShapesByCellLookup);
            return new_rect;
        }

        public static RectangleBounds Create_WindowBounds(
            Vector3 rawPosition,
            Vector3 dimensions,
            int rotation,
            Dictionary<Vector2, Dictionary<string, BoundsShapeBlock>> boundsShapesByCellLookup,
            HexagonCellPrototype nodeCell,
            float windowElevationOffsetMult = 0.3f,
            float sizeMult = 2
        )
        {
            Vector3 pos = rawPosition;
            pos.y += nodeCell.layerOffset * windowElevationOffsetMult;
            RectangleBounds new_rect = new RectangleBounds(pos, sizeMult, rotation, dimensions);
            BoundsShapeBlock new_windowShape = new BoundsShapeBlock(new_rect);
            BoundsShapeBlock.GetIntersectingHexLookups(new_windowShape, nodeCell.size, boundsShapesByCellLookup);
            return new_rect;
        }

        public static void Create_Marker(Vector3 rawPosition, Dictionary<Vector3, Vector3> markersByLookup, float elevationOffset = 0)
        {
            Vector3 pos = rawPosition;
            pos.y += elevationOffset;
            Vector3 markerLookup = VectorUtil.PointLookupDefault(pos);
            if (markersByLookup.ContainsKey(markerLookup) == false) markersByLookup.Add(markerLookup, pos);
        }

        public void HexGridToBuilding()
        {
            surfaceBlockCenterLookups = null;

            baseEdges = new List<HexagonCellPrototype>();
            baseInners = new List<HexagonCellPrototype>();

            rect_doorwaysOuter = new List<RectangleBounds>();
            rect_doorwaysInner = new List<RectangleBounds>();
            rect_windows = new List<RectangleBounds>();
            rect_stairways = new List<RectangleBounds>();

            buildingBoundsShells = new List<RectangleBounds>();

            boundsShapesByCellLookup = new Dictionary<Vector2, Dictionary<string, BoundsShapeBlock>>();

            int entrances = 0;
            Dictionary<HexagonSide, Vector3> extEntrancesByLookupBySide = new Dictionary<HexagonSide, Vector3>();
            HashSet<Vector2> extEntryCells = new HashSet<Vector2>();
            HashSet<Vector3> innerEntrywayLookups = new HashSet<Vector3>();
            List<Bounds> new_structureBounds = new List<Bounds>();

            markerPoints_spawn = new Dictionary<Vector3, Vector3>();

            int _nodeCellSize = (int)nodeGrid_CellSize;

            float roomRadius = (innerRoomRadiusMult * _nodeCellSize);

            bool shouldHaveStairs = hexNodeGrid.cellLayersMax > 1;

            Dictionary<Vector2, Vector3> pathLookups = new Dictionary<Vector2, Vector3>();
            pathCells = new List<HexagonCellPrototype>();

            if (displaySettings.enable_pathing)
            {
                pathLookups = HexCoreUtil.Generate_RandomPathLookups(hexNodeGrid.gridCenterPos, _nodeCellSize, nodeGrid_MaxCellsPerLayer);
            }

            Dictionary<int, List<HexagonCellPrototype>> available_stairwayCellsByLayer = new Dictionary<int, List<HexagonCellPrototype>>();

            stairwayDimensions.y = hexNodeGrid.cellLayerOffset * 0.75f;
            doorDimensions_INT.y = (hexNodeGrid.cellLayerOffset / 2) * 0.8f;
            doorDimensions_EXT.y = (hexNodeGrid.cellLayerOffset / 2) * 0.8f;

            foreach (var currentLayer in hexNodeGrid.GetCellsByLayer().Keys)
            {
                if (available_stairwayCellsByLayer.ContainsKey(currentLayer) == false) available_stairwayCellsByLayer.Add(currentLayer, new List<HexagonCellPrototype>());
                bool isBaseLayer = hexNodeGrid.baseLayer == currentLayer;

                foreach (Vector2 lookup in hexNodeGrid.cellLookup_ByLayer_BySize[_nodeCellSize][currentLayer].Keys)
                {
                    HexagonCellPrototype cell = hexNodeGrid.cellLookup_ByLayer_BySize[_nodeCellSize][currentLayer][lookup];

                    bool edge = cell.IsEdge();
                    if (edge) baseEdges.Add(cell);
                    else baseInners.Add(cell);

                    if (isBaseLayer)
                    {
                        if (pathLookups.ContainsKey(lookup))
                        {
                            cell.SetPathCell(true);
                            pathCells.Add(cell);

                            cell.buildingNodeClearBounds = new List<BoundsShapeBlock>();

                            for (int side = 0; side < cell.neighborsBySide.Length; side++)
                            {
                                if (cell.neighborsBySide[side] == null)
                                {
                                    BoundsShapeBlock clearSphere = new BoundsShapeBlock(cell.sidePoints[side], cell.size * 0.4f, 6);
                                    cell.buildingNodeClearBounds.Add(clearSphere);
                                }
                                else if (pathLookups.ContainsKey(cell.neighborsBySide[side].GetLookup()) || cell.neighborsBySide[side].IsPath())
                                {
                                    BoundsShapeBlock clearSphere = new BoundsShapeBlock(cell.sidePoints[side], cell.size * 0.4f, 6);
                                    cell.buildingNodeClearBounds.Add(clearSphere);
                                    // RectangleBounds new_rect = new RectangleBounds(cell.sidePoints[side], doorwayRadius, HexCoreUtil.GetRotationFromSide((HexagonSide)side), doorDimensions_EXT);
                                    // BoundsShapeBlock clearSphere = new BoundsShapeBlock(new_rect);
                                    // clearWithinBounds.Add(clearSphere);
                                }
                            }

                            // List<HexagonSide> nullNeighborSides = cell.GetNeighborSides(Filter_CellType.NullValue);
                            // foreach (var ix in nullNeighborSides)
                            // {
                            //     BoundsShapeBlock clearSphere = new BoundsShapeBlock(cell.sidePoints[(int)ix], cell.size * 0.4f, 6);
                            //     cell.buildingNodeClearBounds.Add(clearSphere);
                            // }

                            BoundsShapeBlock centerClearSphere = new BoundsShapeBlock(cell.center, cell.size * 0.6f, 6);
                            cell.buildingNodeClearBounds.Add(centerClearSphere);

                            if (edge) Generate_BuildingNodeEdgePoints(cell, buildingFront);
                            continue;
                        }
                    }

                    if (edge)
                    {
                        Generate_BuildingNodeEdgePoints(cell, buildingFront);

                        List<HexagonSide> nullNeighborSides = cell.GetNeighborSides(Filter_CellType.NullValue);
                        bool entryAssigned = false;

                        foreach (var side in nullNeighborSides)
                        {
                            if (entrances < entrancesMax && (entrances == 0 || UnityEngine.Random.Range(0, 5) < 2))
                            {
                                if (extEntryCells.Contains(cell.GetLookup())) continue;
                                if (extEntrancesByLookupBySide.ContainsKey(side)) continue;

                                extEntryCells.Add(cell.GetLookup());
                                entryAssigned = true;

                                Vector3 pos = cell.sidePoints[(int)side];

                                extEntrancesByLookupBySide.Add(side, pos);

                                Create_Marker(pos, markerPoints_spawn, 1f);
                                RectangleBounds new_doorwayRect = Create_DoorwayBounds(
                                    pos,
                                    doorDimensions_EXT,
                                    HexCoreUtil.GetRotationFromSide(side),
                                    boundsShapesByCellLookup,
                                    (HexCellSizes)cell.size
                                );
                                rect_doorwaysOuter.Add(new_doorwayRect);

                                entrances++;
                            }
                            else
                            {
                                Vector3 pos = cell.sidePoints[(int)side];

                                RectangleBounds new_windowRect = Create_WindowBounds(
                                    pos,
                                    windowDimensions,
                                    HexCoreUtil.GetRotationFromSide(side),
                                    boundsShapesByCellLookup,
                                    cell,
                                    windowElevationOffsetMult
                                );
                                rect_windows.Add(new_windowRect);
                            }
                        }

                        if (cell.layerNeighbors[1] != null && nullNeighborSides.Count >= 3) available_stairwayCellsByLayer[currentLayer].Add(cell);
                    }

                    BoundsShapeBlock new_cellBoundsShape = new BoundsShapeBlock(cell, 0.78f, 0.88f, edge);
                    BoundsShapeBlock.GetIntersectingHexLookups(new_cellBoundsShape, cell.size, boundsShapesByCellLookup);

                    List<HexagonSide> innerNeighborSides = cell.GetNeighborSides(Filter_CellType.Any);

                    foreach (var side in innerNeighborSides)
                    {
                        Vector3 pos = cell.sidePoints[(int)side];
                        Vector3 posLookup = VectorUtil.PointLookupDefault(pos);

                        if (innerEntrywayLookups.Contains(posLookup)) continue;

                        innerEntrywayLookups.Add(posLookup);

                        Create_Marker(pos, markerPoints_spawn, 1f);
                        RectangleBounds new_doorwayRect = Create_DoorwayBounds(
                            pos,
                            doorDimensions_INT,
                            HexCoreUtil.GetRotationFromSide(side),
                            boundsShapesByCellLookup,
                            (HexCellSizes)cell.size
                        );
                        rect_doorwaysInner.Add(new_doorwayRect);
                    }

                    Vector3[] boundsCorners = HexCoreUtil.GenerateHexagonPoints(cell.center, cell.size);
                    Bounds bounds = VectorUtil.CalculateBounds_V2(boundsCorners.ToList());
                    new_structureBounds.Add(bounds);
                }
            }

            stairwayCells = new Dictionary<Vector2, HexagonCellPrototype>();

            foreach (var currentLayer in available_stairwayCellsByLayer.Keys)
            {
                if (available_stairwayCellsByLayer[currentLayer].Count == 0) break;

                List<HexagonCellPrototype> sorted = available_stairwayCellsByLayer[currentLayer];

                // if (stairwayCells.Count > 0)
                // {
                sorted = sorted.FindAll(e => !stairwayCells.ContainsKey(e.GetLookup())); //;.OrderByDescending(e => e.GetNeighborSides(Filter_CellType.NullValue)).ToList();
                                                                                         // }
                                                                                         // else sorted = sorted.OrderByDescending(e => e.GetNeighborSides(Filter_CellType.NullValue)).ToList();

                if (sorted.Count == 0) continue;

                Vector2 lookup = sorted[0].GetLookup();
                stairwayCells.Add(lookup, sorted[0]);
                Vector3 pos = sorted[0].center;

                RectangleBounds new_stairwayRect = Create_StairwayBounds(
                    pos,
                    stairwayDimensions,
                    0,
                    // HexCoreUtil.GetRotationFromSide(side),
                    boundsShapesByCellLookup,
                    (HexCellSizes)sorted[0].size
                );
                rect_stairways.Add(new_stairwayRect);

                HexagonCellPrototype topNeighbor = sorted[0].layerNeighbors[1];
                if (topNeighbor != null && stairwayCells.ContainsKey(topNeighbor.GetLookup()) == false)
                {
                    stairwayCells.Add(topNeighbor.GetLookup(), topNeighbor);
                }
            }

            structureBounds = new_structureBounds;
            gridBounds = VectorUtil.CalculateBounds(structureBounds);
        }


        public void EvaluateGridLayers(bool log = false)
        {
            int nodeGridHeight = (nodeGrid_CellLayersMax * nodeGrid_CellLayerOffset);
            int tileGridHeight = (tileGrid_CellLayers * tileGrid_CellLayerOffset);

            if (tileGridHeight < nodeGridHeight || (tileGridHeight - tileGrid_CellLayerOffset) > nodeGridHeight)
            {
                if (log) Debug.Log("Recalculating tile grids... ");
                // Debug.LogError("(tileGridHeight < nodeGridHeight) - nodeGridHeight: " + nodeGridHeight + ", tileGridHeight: " + tileGridHeight);
                int attempts = 99;

                while (attempts > 0 && tileGridHeight < nodeGridHeight)
                {
                    tileGrid_CellLayers++;
                    tileGridHeight = (tileGrid_CellLayers * tileGrid_CellLayerOffset);
                    attempts--;
                }

                while (attempts > 0 && (tileGridHeight - tileGrid_CellLayerOffset) > nodeGridHeight)
                {
                    tileGrid_CellLayers--;
                    tileGridHeight = (tileGrid_CellLayers * tileGrid_CellLayerOffset);
                    attempts--;
                }

                if (tileGridHeight < nodeGridHeight || (tileGridHeight - tileGrid_CellLayerOffset) > nodeGridHeight)
                {
                    Debug.LogError("(tileGridHeight < nodeGridHeight || (tileGridHeight - tileGrid_CellLayerOffset) > nodeGridHeight) - nodeGridHeight: " + nodeGridHeight + ", tileGridHeight: " + tileGridHeight);
                }
            }

            if (nodeGrid_CellLayerOffset < tileGrid_CellLayerOffset) tileGrid_CellLayerOffset = nodeGrid_CellLayerOffset;
        }


        public List<HexagonTileTemplate> Generate_Tiles(
            GameObject prefab,
            Transform folder,
            bool useCompatibilityCheck,
            HexagonSocketDirectory socketDirectory,
            List<SurfaceBlockState> filterOnStates = null,
            bool disableObject = false,
            bool log = false
        )
        {
            // Evalaute_Folder();
            List<HexagonTileTemplate> generatedTiles = null;

            if (surfaceBlockCenterLookups == null)
            {
                Debug.LogError("surfaceBlockCenterLookups == null");
                return generatedTiles;
            }

            // Debug.Log("Distance From World Center: " + Vector3.Distance(transform.position, Vector3.zero));
            Dictionary<HexagonCellPrototype, GameObject> gameObjectsByCell = SurfaceBlock.Generate_MeshObjectsByCell(
                surfaceBlocksByCell,
                prefab,
                transform,
                filterOnStates,
                disableObject,
                folder,
                true
            );

            if (gameObjectsByCell == null || gameObjectsByCell.Count == 0)
            {
                Debug.LogError("(gameObjectsByCell == null || gameObjectsByCell.Count == 0)");
                return generatedTiles;
            }

            if (useCompatibilityCheck)
            {
                generatedTiles = HexagonTileTemplate.Generate_Tiles_With_WFC_DryRun(gameObjectsByCell, socketDirectory, disableObject);
            }
            else generatedTiles = HexagonTileTemplate.Generate_Tiles(gameObjectsByCell, folder, true);

            return generatedTiles;
        }


        public static Dictionary<int, BuildingPrototype> Generate_BuildingPrototypesFromBlockClusters(
            Dictionary<int, Dictionary<Vector2, Vector3>> buildingBlockClusters,
            int maxMembers,
            Transform transform,
            HashSet<Vector2> excludeList = null
        )
        {
            Dictionary<int, BuildingPrototype> buildingPrototypes = new Dictionary<int, BuildingPrototype>();
            HashSet<Vector2> added = new HashSet<Vector2>();
            int j = 0;
            foreach (var ix in buildingBlockClusters.Keys)
            {
                foreach (var lookup in buildingBlockClusters[ix].Keys)
                {
                    if (added.Contains(lookup)) continue;

                    Vector3 center = buildingBlockClusters[ix][lookup];

                    Dictionary<Vector2, Vector3> clusterCenterPoints = new Dictionary<Vector2, Vector3>();
                    List<Vector3> foundMembers = new List<Vector3>();
                    List<Vector2> nearestCellLookups = HexCoreUtil.Calculate_ClosestHexLookups_X7(center, 12);

                    foreach (Vector2 currentLookup in nearestCellLookups)
                    {
                        if (buildingBlockClusters[ix].ContainsKey(currentLookup) == false) continue;
                        if (added.Contains(currentLookup)) continue;
                        if (excludeList != null && excludeList.Contains(currentLookup)) continue;

                        clusterCenterPoints.Add(currentLookup, buildingBlockClusters[ix][currentLookup]);
                        foundMembers.Add(buildingBlockClusters[ix][currentLookup]);

                        added.Add(currentLookup);

                        if (clusterCenterPoints.Count >= maxMembers) break;
                    }

                    if (clusterCenterPoints.Count > 0)
                    {
                        Vector3 clusterCenter = VectorUtil.Calculate_CenterPositionFromPoints(foundMembers);
                        Vector3 gridStartPos = HexCoreUtil.Calculate_ClosestHexCenter_V2(clusterCenter, (int)HexCellSizes.X_12);
                        gridStartPos.y = foundMembers[0].y;

                        BuildingPrototype new_buildingPrototype = new BuildingPrototype(
                            clusterCenter,
                            gridStartPos,
                            new Dictionary<int, Dictionary<Vector2, Vector3>> {
                                {(int)HexCellSizes.X_12,  clusterCenterPoints}
                            },
                            transform
                        );

                        buildingPrototypes.Add(j, new_buildingPrototype);
                        j++;
                    }
                }
            }

            Debug.Log(buildingPrototypes.Count + " buildingPrototypes created!");

            return buildingPrototypes;
        }

    }



    [System.Serializable]
    public class BuildingPrototypeDisplaySettings
    {
        public BuildingPrototypeDisplaySettings(
            bool _show_nodeGrid = true,
            bool _show_tileGrid = true,
            bool _show_blockGrid = false,
            bool _show_blockBounds = true,
            bool _show_clearingBounds = true,
            bool _show_doorways = true,
            bool _show_windows = true,
            bool _show_stairwayNodes = false,
            bool _enable_blockGrid = false,
            bool _show_buildingBoundsShells = true,
            bool _show_markers = true,
            bool _show_clusterCenter = true,
            bool _enable_pathing = false
        )
        {
            show_nodeGrid = _show_nodeGrid;
            show_tileGrid = _show_tileGrid;

            show_clusterCenter = _show_clusterCenter;

            show_blockGrid = _show_blockGrid;
            show_blockBounds = _show_blockBounds;
            show_clearingBounds = _show_clearingBounds;
            show_doorways = _show_doorways;
            show_windows = _show_windows;

            show_stairwayNodes = _show_stairwayNodes;
            show_buildingBoundsShells = _show_buildingBoundsShells;
            show_markers = _show_markers;

            enable_blockGrid = _enable_blockGrid;
            enable_pathing = _enable_pathing;
        }

        public bool show_nodeGrid = true;
        public bool show_tileGrid = true;
        public bool show_clusterCenter = true;
        [Header(" ")]
        public bool enable_blockGrid;
        public bool show_blockGrid;
        public bool show_blockBounds = true;
        [Header(" ")]
        public bool show_clearingBounds;
        public bool show_doorways = true;
        public bool show_windows = true;
        public bool show_stairwayNodes;
        public bool show_buildingBoundsShells = true;
        public bool show_markers = false;
        [Header(" ")]
        public bool enable_pathing = false;
    }

    [System.Serializable]
    public class LocationFoundationSettings
    {
        public LocationFoundationSettings(
            int _foundation_innersMax,
            int _foundation_cornersMax,
            int _foundation_random_Inners = 40,
            int _foundation_random_Corners = 40,
            int _foundation_random_Center = 100,
            int _foundation_layerOffset = 2,
            int _foundation_maxLayers = 6,
            int _foundation_maxLayerDifference = 4
        )
        {
            foundation_innersMax = _foundation_innersMax;
            foundation_cornersMax = _foundation_cornersMax;
            foundation_random_Inners = _foundation_random_Inners;
            foundation_random_Corners = _foundation_random_Corners;
            foundation_random_Center = _foundation_random_Center;

            foundation_layerOffset = _foundation_layerOffset;
            foundation_maxLayers = _foundation_maxLayers;
            foundation_maxLayerDifference = _foundation_maxLayerDifference;
        }

        [Range(2, 10)] public int foundation_layerOffset = 2;
        [Range(1, 12)] public int foundation_maxLayers = 6;
        [Range(1, 12)] public int foundation_maxLayerDifference = 4;
        [Header(" ")]
        [Range(1, 12)] public int foundation_innersMax = 4;
        [Range(1, 12)] public int foundation_cornersMax = 1;
        [Header(" ")]
        [Range(0, 100)] public int foundation_random_Inners = 40;
        [Range(0, 100)] public int foundation_random_Corners = 40;
        [Range(0, 100)] public int foundation_random_Center = 90;

        public float groundCellInfluenceRadiusMult { get; private set; } = 1.29f;
        public float bufferZoneLerpMult { get; private set; } = 0.55f;
        public float pathCellLerpMult { get; private set; } = 0.5f;
        public List<LayeredNoiseOption> layeredNoise_terrainGlobal { get; private set; }
    }

}
