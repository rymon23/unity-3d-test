using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;
using WFCSystem;

[System.Serializable]
public class BuildingGenerator : MonoBehaviour
{
    public enum BuildingRoofType { Flat, Dome, Pointed, Tent }
    public enum BuildingStackType { Level, Focused, Dome, Castle }
    public enum StructureType { Building, Wall }
    [SerializeField] private BuildingPrototypeDisplaySettings buildingPrototypeDisplaySettings = new BuildingPrototypeDisplaySettings();
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
    [SerializeField] private bool resetPrototypes;
    [Header(" ")]
    [SerializeField] private bool generate_tiles;
    [SerializeField] private bool useCompatibilityCheck;
    [SerializeField] private bool instantiateOnGenerate;

    [Header(" ")]
    [SerializeField] private bool generate_MeshTiles;
    [SerializeField] private bool generate_mesh;


    [Header(" ")]
    [SerializeField] private bool showSurfaceBlockEdgeSockets = true;
    [SerializeField] private bool highlight_SurfaceBlockEdgeSide;
    [SerializeField] private HexagonTileSide highlightSide;

    [Header(" ")]
    [SerializeField] private bool showHighlightedCell;
    [Range(0, 468)][SerializeField] private int _highlightedCell;
    private HexagonCellPrototype _currentHighlightedCell = null;

    [Header("Node Grid Settings")]
    [Range(4, 108)][SerializeField] private int nodeGrid_CellSize = 12;
    [Range(12, 108)][SerializeField] private int nodeGrid_GridRadius = 36;
    [Header(" ")]
    [Range(1, 48)][SerializeField] private int nodeGrid_CellLayersMin = 1;
    [Range(1, 48)][SerializeField] private int nodeGrid_CellLayersMax = 2;
    [Header(" ")]
    [Range(2, 12)][SerializeField] private int nodeGrid_CellLayerOffset = 4;
    [Header(" ")]
    [Range(1, 19)][SerializeField] private int nodeGrid_MaxCellsPerLayer = 7;
    [SerializeField] private HexCellSizes nodeGrid_SnapSize = HexCellSizes.X_4;
    [Header(" ")]
    [SerializeField] private Option_CellGridType nodeGrid_GridType = Option_CellGridType.Defualt;
    [Header(" ")]
    [SerializeField] private HexagonSide buildingFront;
    [SerializeField] private bool use_pathing;
    [Header(" ")]

    [Header("Tile Grid Settings")]
    [SerializeField] private HexCellSizes tileGrid_CellSize = HexCellSizes.X_4;
    [Range(1, 48)][SerializeField] private int tileGrid_CellLayers = 1;
    [Range(2, 12)][SerializeField] private int tileGrid_CellLayerOffset = 4;
    [Range(12, 108)][SerializeField] private int tileGrid_GridRadius = 36;

    [Header("Building Settings")]
    [Range(0.2f, 0.9f)][SerializeField] private float innerRoomRadiusMult = 0.8f;
    [Header(" ")]
    [Range(1, 5)][SerializeField] private int entrancesMax = 2;
    [Range(1f, 4f)][SerializeField] private float doorwayRadius = 2;
    [Range(1f, 4f)][SerializeField] private float innerEntryRadius = 3;
    public Vector3 extDoor_dimensions = new Vector3(0.6f, 1.4f, 1.3f);
    public Vector3 intDoor_dimensions = new Vector3(0.6f, 1.4f, 1.8f);

    [Header("Surface Block Settings")]
    [Range(0.2f, 10f)][SerializeField] private float blockSize = 1f;
    [Range(12, 128)][SerializeField] private float boundsSize = 25;
    [Header(" ")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private TileDirectory tileDirectory;
    [Header(" ")]

    Bounds gridBounds;
    List<Bounds> structureBounds = new List<Bounds>();
    List<RectangleBounds> rect_doorwaysInner = null;
    List<RectangleBounds> rect_doorwaysOuter = null;
    List<RectangleBounds> rect_stairways = null;
    List<HexagonCellPrototype> baseEdges = null;
    List<HexagonCellPrototype> baseInners = null;
    List<HexagonCellPrototype> pathCells = null;

    SurfaceBlock[,,] surfaceBlocksGrid;
    List<BoundsShapeBlock> clearWithinBounds = null;

    public Transform folder_Main { get; private set; } = null;
    public Transform folder_MeshObject { get; private set; } = null;
    public Transform folder_GeneratedTiles { get; private set; } = null;

    public HexGrid hexNodeGrid = null;
    public HexGrid hexTileGrid = null;
    Dictionary<Vector2, HexagonCellPrototype> stairwayCells = new Dictionary<Vector2, HexagonCellPrototype>();
    List<HexagonTileTemplate> generatedTiles = null;
    Dictionary<HexagonCellPrototype, List<SurfaceBlock>> surfaceBlocksByCell = null;
    Dictionary<Vector2, Dictionary<string, BoundsShapeBlock>> boundsShapesByCellLookup = null;
    Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, List<SurfaceBlock>>> tileInnerEdgesByCellSide = null;
    Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, TileSocketProfile>> cellTileSocketProfiles = null;

    [SerializeField] Vector3 defaultBuilding_dimensions = new Vector3(10f, 12f, 6f);
    [Header(" ")]
    [Range(1, 50)][SerializeField] int defaultBuilding_layersMin = 1;
    [Range(1, 50)][SerializeField] int defaultBuilding_layersMax = 10;
    [Range(0.5f, 50f)][SerializeField] float defaultBuilding_size = 2f;
    List<RectangleBounds> buildingBoundsShells = null;
    BuildingPrototype buildingPrototype = null;

    #region Saved State
    Vector3 _lastPosition;
    float _boundsSize;
    float _blockSize;
    float _updateDist = 1f;
    #endregion


    [SerializeField] private List<LayeredNoiseOption> layeredNoise_terrainGlobal;
    [Range(-32, 768)][SerializeField] private float _global_terrainHeightDefault = 24f;

    Vector3 gridStartPos;

    private void OnValidate()
    {
        if (
            resetPrototypes
            || _lastPosition != transform.position
            || _boundsSize != boundsSize
            || _blockSize != blockSize
            || hexNodeGrid == null
            || (hexNodeGrid != null && hexNodeGrid.cellLookup_ByLayer_BySize == null)
            )
        {

            resetPrototypes = false;

            _lastPosition = transform.position;

            boundsSize = UtilityHelpers.RoundToNearestStep(boundsSize, 2f);
            _boundsSize = boundsSize;

            blockSize = UtilityHelpers.RoundToNearestStep(blockSize, 0.2f);
            _blockSize = blockSize;

            boundsShapesByCellLookup = null;

            gridStartPos = HexCoreUtil.Calculate_ClosestHexCenter_V2(transform.position, (int)nodeGrid_SnapSize);
            gridStartPos.y = HexCoreUtil.Calculate_CellSnapElevation(nodeGrid_CellLayerOffset, transform.position.y);

            // Vector3 gridStartPos = HexCoreUtil.Calculate_ClosestHexCenter_V2(transform.position, (int)nodeGrid_SnapSize);

            if (nodeGrid_CellLayersMin > nodeGrid_CellLayersMax) nodeGrid_CellLayersMax = nodeGrid_CellLayersMin;

            EvaluateGridLayers();

            Vector2Int _cellLayersMinMax = new Vector2Int(nodeGrid_CellLayersMin, nodeGrid_CellLayersMax);

            // hexNodeGrid = new HexGrid(
            //         gridStartPos,
            //         nodeGrid_CellSize,
            //         _cellLayersMinMax,
            //         nodeGrid_CellLayerOffset,
            //         nodeGrid_MaxCellsPerLayer,

            //         nodeGrid_GridRadius,

            //         nodeGrid_GridType
            //     );

            // hexTileGrid = new HexGrid(
            //         gridStartPos,
            //         (int)tileGrid_CellSize,
            //         tileGrid_CellLayers,
            //         tileGrid_CellLayerOffset,
            //         (int)HexCellSizes.X_36,
            //         Option_CellGridType.Defualt
            //     );

            // HexGridToBuilding();

            buildingPrototype = new BuildingPrototype(
                gridStartPos,
                (HexCellSizes)nodeGrid_CellSize,
                _cellLayersMinMax,
                nodeGrid_CellLayerOffset,
                nodeGrid_MaxCellsPerLayer,
                nodeGrid_GridRadius,
                transform,
                nodeGrid_GridType
            );

        }


        if (generate_MeshTiles || generate_tiles)
        {
            generate_MeshTiles = false;

            Evalaute_Folder();

            if (surfaceBlocksGrid != null)
            {
                // List<SurfaceBlockState> filterOnStates = new List<SurfaceBlockState>() {
                //         SurfaceBlockState.Entry,
                //         // SurfaceBlockState.Corner,
                //     };

                // Debug.Log("Distance From World Center: " + Vector3.Distance(transform.position, Vector3.zero));
                Dictionary<HexagonCellPrototype, GameObject> gameObjectsByCell = SurfaceBlock.Generate_MeshObjectsByCell(
                    surfaceBlocksByCell,
                    prefab,
                    transform,
                    null,
                    false,
                    folder_GeneratedTiles,
                    true
                );

                if (generate_tiles)
                {
                    if (useCompatibilityCheck)
                    {
                        generatedTiles = HexagonTileTemplate.Generate_Tiles_With_WFC_DryRun(gameObjectsByCell, tileDirectory.GetSocketDirectory(), false);
                    }
                    else generatedTiles = HexagonTileTemplate.Generate_Tiles(gameObjectsByCell, folder_Main, instantiateOnGenerate);
                }
            }

            generate_tiles = false;
        }

        if (generate_mesh)
        {
            generate_mesh = false;

            Evalaute_Folder();

            // GameObject gameObject = SurfaceBlock.Generate_MeshObject(surfaceBlocksGrid, prefab, transform, null, folder_Main);
            List<GameObject> gameObjects = SurfaceBlock.Generate_MeshObjects(surfaceBlocksGrid, prefab, transform, null, folder_Main);
        }
    }

    public void ResetPrototypes()
    {
        resetPrototypes = true;
        OnValidate();
    }

    Dictionary<string, Color> customColors = UtilityHelpers.CustomColorDefaults();
    private void OnDrawGizmos()
    {
        if (_lastPosition != transform.position)
        {
            if (Vector3.Distance(_lastPosition, transform.position) > _updateDist) ResetPrototypes();
        }


        if (buildingPrototype != null)
        {
            buildingPrototype.Draw(buildingPrototypeDisplaySettings);
        }

        RampPrototype new_rect = new RampPrototype(transform.position, doorwayRadius, 0, extDoor_dimensions);
        new_rect.Draw();

        Gizmos.DrawSphere(gridStartPos, 2f);



        if (buildingPrototypeDisplaySettings.show_nodeGrid && hexNodeGrid != null && hexNodeGrid.cellLookup_ByLayer_BySize != null && hexNodeGrid.cellLookup_ByLayer_BySize.Count > 0)
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

                        // if (cell.buildingNode_SlicedCorners != null)
                        // {
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
                        else continue;


                        // for (int _side = 0; _side < 6; _side++)
                        // {
                        //     // if (nullSides.Contains((HexagonSide)_side))
                        //     // {
                        //     Vector2Int corners = HexCoreUtil.GetCornersFromSide_Default((HexagonSide)_side);

                        //     Vector3 dimensions = new Vector3(0.7f, cell.layerOffset, 0.7f);
                        //     RectangleBounds rect_A = new RectangleBounds(cell.buildingNodeEdgePoints[corners.x], doorwayRadius, 0, dimensions);
                        //     rect_A.Draw();
                        //     RectangleBounds rect_B = new RectangleBounds(cell.buildingNodeEdgePoints[corners.y], doorwayRadius, 0, dimensions);
                        //     rect_B.Draw();
                        //     // }
                        // }

                        // List<HexagonSide> nullSides = cell.GetNeighborSides(Filter_CellType.NullValue);
                        // List<HexagonCellPrototype> edgeNeighors = cell.GetEdgeSideNeighbors();

                        // if (nullSides.Count > 0)
                        // {
                        //     Gizmos.color = Color.white;

                        //     // if (neighborSides.Count >= 3)
                        //     // {
                        //     //     Gizmos.DrawWireSphere(cell.center, cell.size * 0.75f);
                        //     // }

                        //     if (currentLayer != hexTileGrid.baseLayer) continue;


                        //     for (int _side = 0; _side < 6; _side++)
                        //     {
                        //         HexagonSide side = (HexagonSide)_side;

                        //         if (nullSides.Contains(side))
                        //         {
                        //             Vector2Int corners = HexCoreUtil.GetCornersFromSide_Default((HexagonSide)_side);

                        //             Vector3 dimensions = new Vector3(0.7f, cell.layerOffset / 2, 0.7f);
                        //             // RectangleBounds rect_A = new RectangleBounds(cell.buildingNodeEdgePoints[corners.x], doorwayRadius, 0, dimensions);
                        //             // rect_A.Draw();
                        //             RectangleBounds rect_B = new RectangleBounds(cell.buildingNodeEdgePoints[corners.y], doorwayRadius, 0, dimensions);
                        //             rect_B.Draw();

                        //             Vector3 pt = VectorUtil.GetPointBetween(cell.buildingNodeEdgePoints[corners.x], cell.buildingNodeEdgePoints[corners.y]);
                        //             float dist = Vector3.Distance(cell.buildingNodeEdgePoints[corners.x], cell.buildingNodeEdgePoints[corners.y]);
                        //             dimensions = new Vector3(dist / 2, 0.5f, 0.5f);

                        //             // int b__side = _side;

                        //             // if (side == buildingFront || side == HexCoreUtil.OppositeSide(buildingFront))
                        //             // {
                        //             //     Vector3 temp = new Vector3(dimensions.x, dimensions.y, dimensions.z);
                        //             //     dimensions.z = temp.x;
                        //             //     dimensions.x = temp.z;
                        //             // }
                        //             // else
                        //             // {
                        //             //     b__side = (int)buildingFront;

                        //             //     // b__side = (_side + 5) % 6;
                        //             // }

                        //             // RectangleBounds rect = new RectangleBounds(pt, doorwayRadius, b__side, dimensions);
                        //             // rect.Draw();
                        //         }


                        //     }

                        //     // foreach (var side in neighborSides)
                        //     // {
                        //     //     HexagonCellPrototype neighbor = cell.neighborsBySide[(int)side];
                        //     //     if (neighbor == null) continue;

                        //     //     Gizmos.DrawLine(neighbor.center, cell.center);

                        //     //     Vector3 pt = VectorUtil.GetPointBetween(neighbor.center, cell.center);
                        //     //     float dist = Vector3.Distance(neighbor.center, cell.center);

                        //     //     Vector3 dimensions = new Vector3(dist / 2, 0.5f, 0.5f);
                        //     //     RectangleBounds rect = new RectangleBounds(pt, doorwayRadius, (int)side, dimensions);
                        //     //     rect.Draw();
                        //     // }
                        // }


                    }
                }

            }

            if (buildingPrototypeDisplaySettings.show_blockBounds)
            {
                Gizmos.color = Color.white;
                VectorUtil.DrawRectangleLines(gridBounds);
            }

            if (buildingPrototypeDisplaySettings.show_doorways)
            {
                if (rect_doorwaysOuter != null)
                {
                    Gizmos.color = Color.green;
                    foreach (var entry in rect_doorwaysOuter)
                    {
                        // Gizmos.DrawWireSphere(entry, doorwayRadius);
                        // RectangleBounds rect = new RectangleBounds(entry, doorwayRadius, 0, extDoor_dimensions);
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

            if (buildingPrototypeDisplaySettings.show_stairwayNodes && stairwayCells != null)
            {
                foreach (var cell in stairwayCells.Values)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(cell.center, cell.size * 0.7f);

                    if (cell.layerNeighbors[1] != null)
                    {
                        Gizmos.color = customColors["orange"];
                        Gizmos.DrawWireSphere(cell.layerNeighbors[1].center, cell.size * 0.5f);
                    }
                }
            }

            if (buildingPrototypeDisplaySettings.show_clearingBounds && clearWithinBounds != null)
            {
                // Gizmos.color = customColors["orange"];
                // foreach (var item in clearWithinBounds)
                // {
                //     item.DrawPoints();
                // }

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
            }

            if (buildingPrototypeDisplaySettings.show_clearingBounds && pathCells != null && pathCells.Count > 0)
            {
                Gizmos.color = Color.green;
                foreach (var cell in pathCells)
                {
                    // Gizmos.DrawWireSphere(cell.center, cell.size * 0.8f);
                    if (cell.buildingNodeClearBounds == null) continue;

                    foreach (var item in cell.buildingNodeClearBounds)
                    {
                        item.Draw();

                    }
                }
            }

        }


        if (buildingPrototypeDisplaySettings.show_buildingBoundsShells && buildingBoundsShells != null)
        {
            foreach (var item in buildingBoundsShells)
            {
                Gizmos.color = Color.white;
                item.Draw();
            }
        }

        if (buildingPrototypeDisplaySettings.show_tileGrid && hexTileGrid != null && hexTileGrid.cellLookup_ByLayer_BySize != null && hexTileGrid.cellLookup_ByLayer_BySize.Count > 0)
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
                    hexGridDisplaySettings.showHighlights
                );



                foreach (int currentLayer in hexTileGrid.cellLookup_ByLayer_BySize[currentSize].Keys)
                {
                    int towersMult = 10;
                    int towerAddSkipAmount = 4;
                    int towerAddSkips = towerAddSkipAmount;

                    // foreach (HexagonCellPrototype cell in hexTileGrid.cellLookup_ByLayer_BySize[currentSize][currentLayer].Values)
                    // {
                    //     if (cell.IsEdge() == false) continue;

                    //     List<HexagonSide> neighborSides = cell.GetNeighborSides(Filter_CellType.Edge);
                    //     List<HexagonCellPrototype> edgeNeighors = cell.GetEdgeSideNeighbors();

                    //     if (neighborSides.Count > 0)
                    //     {
                    //         Gizmos.color = Color.white;

                    //         if (neighborSides.Count >= 3)
                    //         {
                    //             Gizmos.DrawWireSphere(cell.center, cell.size * 0.75f);
                    //         }

                    //         if (currentLayer != hexTileGrid.baseLayer) continue;

                    //         if (towerAddSkips > 0)
                    //         {
                    //             towerAddSkips--;

                    //             Vector3 dimensions = new Vector3(1, 3, 1f);
                    //             RectangleBounds rect = new RectangleBounds(cell.center, doorwayRadius, 0, dimensions);
                    //             rect.Draw();
                    //         }
                    //         else if (towersMult > 0)
                    //         {
                    //             towerAddSkips += towerAddSkipAmount;
                    //             towersMult--;

                    //             Vector3 dimensions = new Vector3(2.5f, 8, 2.5f);
                    //             RectangleBounds rect = new RectangleBounds(cell.center, doorwayRadius, 0, dimensions);
                    //             rect.Draw();
                    //         }

                    //         foreach (var side in neighborSides)
                    //         {
                    //             HexagonCellPrototype neighbor = cell.neighborsBySide[(int)side];
                    //             if (neighbor == null) continue;

                    //             Gizmos.DrawLine(neighbor.center, cell.center);

                    //             Vector3 pt = VectorUtil.GetPointBetween(neighbor.center, cell.center);
                    //             float dist = Vector3.Distance(neighbor.center, cell.center);

                    //             Vector3 dimensions = new Vector3(dist / 2, 0.5f, 0.5f);
                    //             RectangleBounds rect = new RectangleBounds(pt, doorwayRadius, (int)side, dimensions);
                    //             rect.Draw();
                    //         }
                    //     }
                    // }
                }
            }
        }

        if (buildingPrototypeDisplaySettings.enable_blockGrid && hexNodeGrid != null && hexTileGrid != null)
        {
            if (surfaceBlocksGrid == null)
            {
                _highlightedCell = -1;

                int maxHeight = ((nodeGrid_CellLayersMax) * nodeGrid_CellLayerOffset);

                // gridBounds = VectorUtil.CalculateBounds(structureBounds);

                // (
                //     Vector3[,,] points,
                //     float spacing
                // ) = VectorUtil.Generate3DGrid(gridBounds, blockSize, transform.position.y, maxHeight);

                // Debug.Log("block grid - spacing: " + spacing);

                Dictionary<Vector3, SurfaceBlock> blockCenterLookup = new Dictionary<Vector3, SurfaceBlock>();
                surfaceBlocksByCell = new Dictionary<HexagonCellPrototype, List<SurfaceBlock>>();

                surfaceBlocksGrid = SurfaceBlock.CreateSurfaceBlocks_V2(
                        gridBounds,
                        blockSize,
                        transform.position.y,
                        maxHeight,
                        blockCenterLookup,
                        hexNodeGrid,
                        hexTileGrid,
                        surfaceBlocksByCell,
                        boundsShapesByCellLookup
                        );


                // // surfaceBlocksGrid = SurfaceBlock.CreateSurfaceBlocks(
                // //         points,
                // //         spacing,
                // //         hexNodeGrid.cellLookup_ByLayer_BySize[nodeGrid_CellSize][hexNodeGrid.baseLayer],
                // //         blockCenterLookup
                // //         );


                // surfaceBlocksGrid = SurfaceBlock.Generate_TerrainBlockGrid(
                //         gridBounds,
                //         blockSize,
                //         transform.position.y,
                //         layeredNoise_terrainGlobal,
                //         _global_terrainHeightDefault,
                //         blockCenterLookup,
                //         hexNodeGrid,
                //         surfaceBlocksByCell,
                //         boundsShapesByCellLookup
                //     );


                // surfaceBlocksGrid = SurfaceBlock.ClearInnerBlocks(surfaceBlocksGrid);
                // foreach (var item in clearWithinBounds)
                // {
                //     surfaceBlocksGrid = SurfaceBlock.ClearInnerBlocks(surfaceBlocksGrid, item);
                // }
                // SurfaceBlock.EvaluateTileEdges(surfaceBlocksGrid);

                SurfaceBlock.EvaluateNeighbors(blockCenterLookup);

                _currentHighlightedCell = null;

                tileInnerEdgesByCellSide = SurfaceBlock.GetTileInnerEdgesByCellSide(surfaceBlocksByCell);

                // cellTileSocketProfiles = SurfaceBlock.Generate_CellTileSocketProfiles_V2(tileInnerEdgesByCellSide);
                SurfaceBlock.Generate_CellTileSocketProfiles_V2(tileInnerEdgesByCellSide);
            }
            else
            {

                if (buildingPrototypeDisplaySettings.show_blockGrid)
                {
                    SurfaceBlock.DrawGrid(surfaceBlocksGrid, showHighlightedCell ? _currentHighlightedCell : null);
                    // SurfaceBlock.DrawGrid(surfaceBlocksGrid, showHighlightedCell ? _currentHighlightedCell : null, show_blockTerrain);
                }

                if (showHighlightedCell && surfaceBlocksByCell != null)
                {

                    int count = surfaceBlocksByCell.Count;
                    if (_highlightedCell == count) _highlightedCell = 0;
                    _highlightedCell = Mathf.Clamp(_highlightedCell, 0, count - 1);

                    int ix = 0;
                    foreach (var cell in surfaceBlocksByCell.Keys)
                    {
                        ix++;

                        if (_highlightedCell == ix)
                        {
                            if (_currentHighlightedCell != cell) _currentHighlightedCell = cell;

                            Gizmos.color = customColors["purple"];
                            Gizmos.DrawWireSphere(cell.center, 1f);

                            if (cell.borderPoints != null)
                            {
                                Gizmos.color = customColors["orange"];
                                foreach (var item in cell.borderPoints.Values)
                                {
                                    Gizmos.DrawSphere(item, 0.4f);
                                }
                            }
                        }
                    }
                }

                if (showSurfaceBlockEdgeSockets && surfaceBlocksByCell != null)
                {
                    Gizmos.color = Color.magenta;
                    if (tileInnerEdgesByCellSide != null)
                    {
                        foreach (var cell in tileInnerEdgesByCellSide.Keys)
                        {
                            if (showHighlightedCell && cell != _currentHighlightedCell) continue;

                            foreach (var side in tileInnerEdgesByCellSide[cell].Keys)
                            {
                                if (highlight_SurfaceBlockEdgeSide && side != highlightSide) continue;

                                foreach (var block in tileInnerEdgesByCellSide[cell][side])
                                {
                                    Gizmos.DrawSphere(block.Position, 0.3f);
                                }
                            }
                        }
                    }
                }

            }
        }
    }



    public static void Generate_BuildingNodeEdgePoints(HexagonCellPrototype cell, HexagonSide buildingFront)
    {
        if (cell.IsEdge() == false) return;

        List<HexagonSide> neighborSides = cell.GetNeighborSides(Filter_CellType.Any);
        HexagonSide frontOpposite = HexCoreUtil.OppositeSide(buildingFront);
        List<int> mutatedCornersList = new List<int>();

        if (neighborSides.Count == 2)
        {
            List<HexagonSide> nullNeighborSides = cell.GetNeighborSides(Filter_CellType.NullValue);
            List<List<HexagonSide>> consecutiveSets;
            // Debug.Log("consecutiveSets:  " + consecutiveSets.Count);

            if (HexCoreUtil.AreSidesConsecutive(neighborSides) == false)
            {
                consecutiveSets = HexCoreUtil.ExtractAllConsecutiveSides(nullNeighborSides);
                if (consecutiveSets.Count == 1 && consecutiveSets[0].Count == 3)
                {
                    cell.buildingNodeEdgePoints = HexCoreUtil.Generate_PartialHexagonCorners(cell.center, cell.size, consecutiveSets[0], mutatedCornersList);
                    cell.buildingNode_SlicedCorners = mutatedCornersList;
                }
                return;
            }

            List<HexagonSide> filteredNullNeighborSides = nullNeighborSides.FindAll(n => n != frontOpposite && n != buildingFront);
            consecutiveSets = HexCoreUtil.ExtractAllConsecutiveSides(filteredNullNeighborSides);

            if (consecutiveSets.Count == 1 && consecutiveSets[0].Count == 2)
            {
                cell.buildingNodeEdgePoints = HexCoreUtil.Generate_PartialHexagonCorners(cell.center, cell.size, consecutiveSets[0], mutatedCornersList);
                cell.buildingNode_SlicedCorners = mutatedCornersList;
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
                    cell.buildingNodeEdgePoints = HexCoreUtil.Generate_PartialHexagonCorners(cell.center, cell.size, nullNeighborSides, mutatedCornersList);
                    cell.buildingNode_SlicedCorners = mutatedCornersList;
                }
            }
        }
    }

    // public static void Generate_WallNodeEdgePoints(HexagonCellPrototype cell)
    // {
    //     Dictionary<int, List<HexagonCellPrototype>> subCellsX4 =  HexGridUtil.Generate_MicroCellGridProtoypes_FromHosts(
    //         HexagonCell parentCell,
    //         List<HexagonCell> childCells,
    //         int cellLayers,
    //         int cellLayerOffset = 4,
    //         bool useCorners = true
    //     )
    // }
    public static void Generate_WallNodeEdgePoints(HexagonCellPrototype cell)
    {
        if (cell.IsEdge() == false) return;

        List<HexagonSide> neighborSides = cell.GetNeighborSides(Filter_CellType.Edge);
    }


    public void HexGridToBuilding()
    {
        surfaceBlocksGrid = null;

        baseEdges = new List<HexagonCellPrototype>();
        baseInners = new List<HexagonCellPrototype>();

        rect_doorwaysOuter = new List<RectangleBounds>();
        rect_doorwaysInner = new List<RectangleBounds>();
        rect_stairways = new List<RectangleBounds>();
        clearWithinBounds = new List<BoundsShapeBlock>();

        buildingBoundsShells = new List<RectangleBounds>();

        boundsShapesByCellLookup = new Dictionary<Vector2, Dictionary<string, BoundsShapeBlock>>();

        int entrances = 0;
        Dictionary<HexagonSide, Vector3> extEntrancesByLookupBySide = new Dictionary<HexagonSide, Vector3>();
        HashSet<Vector2> extEntryCells = new HashSet<Vector2>();
        HashSet<Vector3> innerEntrywayLookups = new HashSet<Vector3>();

        List<Bounds> new_structureBounds = new List<Bounds>();

        float roomRadius = (innerRoomRadiusMult * nodeGrid_CellSize);

        bool shouldHaveStairs = nodeGrid_CellLayersMax > 1;


        Dictionary<Vector2, Vector3> pathLookups = new Dictionary<Vector2, Vector3>();
        pathCells = new List<HexagonCellPrototype>();

        if (use_pathing)
        {
            pathLookups = HexCoreUtil.Generate_RandomPathLookups(hexNodeGrid.gridCenterPos, nodeGrid_CellSize, nodeGrid_MaxCellsPerLayer);
        }

        Dictionary<int, List<HexagonCellPrototype>> available_stairwayCellsByLayer = new Dictionary<int, List<HexagonCellPrototype>>();


        foreach (var currentLayer in hexNodeGrid.GetCellsByLayer().Keys)
        {
            if (available_stairwayCellsByLayer.ContainsKey(currentLayer) == false) available_stairwayCellsByLayer.Add(currentLayer, new List<HexagonCellPrototype>());
            bool isBaseLayer = hexNodeGrid.baseLayer == currentLayer;

            foreach (Vector2 lookup in hexNodeGrid.cellLookup_ByLayer_BySize[nodeGrid_CellSize][currentLayer].Keys)
            {
                HexagonCellPrototype cell = hexNodeGrid.cellLookup_ByLayer_BySize[nodeGrid_CellSize][currentLayer][lookup];

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
                                // RectangleBounds new_rect = new RectangleBounds(cell.sidePoints[side], doorwayRadius, HexCoreUtil.GetRotationFromSide((HexagonSide)side), extDoor_dimensions);
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



                    // for (int side = 0; side < cell.neighborsBySide.Length; side++)
                    // {
                    //     if (cell.neighborsBySide[side] != null)
                    //     {

                    //         Vector3 dimensions = new Vector3(
                    //                 defaultBuilding_dimensions.x * UtilityHelpers.RoundToNearestStep(UnityEngine.Random.Range(0.6f, 1f), 0.2f),
                    //                 nodeGrid_CellLayerOffset / 2,
                    //                 // UnityEngine.Random.Range(defaultBuilding_layersMin, defaultBuilding_layersMax + 1) * 2,
                    //                 defaultBuilding_dimensions.z
                    //             );

                    //         RectangleBounds rect = new RectangleBounds(cell.center, defaultBuilding_size, side, dimensions);
                    //         buildingBoundsShells.Add(rect);

                    //         Vector3 dimensions2 = new Vector3(
                    //             defaultBuilding_dimensions.x * UtilityHelpers.RoundToNearestStep(UnityEngine.Random.Range(0.6f, 1f), 0.2f),
                    //             nodeGrid_CellLayerOffset / 2,
                    //             defaultBuilding_dimensions.z
                    //         );
                    //         RectangleBounds rect2 = new RectangleBounds(cell.sidePoints[side], defaultBuilding_size, side, dimensions2);
                    //         buildingBoundsShells.Add(rect2);

                    //         break;
                    //     }
                    // }

                    // Vector3 dimensions = new Vector3(
                    //         defaultBuilding_dimensions.x * UtilityHelpers.RoundToNearestStep(UnityEngine.Random.Range(0.6f, 1f), 0.2f),
                    //         nodeGrid_CellLayerOffset,
                    //         // UnityEngine.Random.Range(defaultBuilding_layersMin, defaultBuilding_layersMax + 1) * 2,
                    //         defaultBuilding_dimensions.z
                    //     );
                    // RectangleBounds rect = new RectangleBounds(cell.center, defaultBuilding_size, UnityEngine.Random.Range(0, 6), dimensions);
                    // buildingBoundsShells.Add(rect);
                }

                int createdBlocks = 0;
                for (int side = 0; side < cell.neighborsBySide.Length; side++)
                {
                    if (cell.neighborsBySide[side] != null)
                    {

                        Vector3 dimensions = new Vector3(
                                defaultBuilding_dimensions.x * UtilityHelpers.RoundToNearestStep(UnityEngine.Random.Range(0.6f, 1f), 0.2f),
                                nodeGrid_CellLayerOffset / 2,
                                // UnityEngine.Random.Range(defaultBuilding_layersMin, defaultBuilding_layersMax + 1) * 2,
                                defaultBuilding_dimensions.z
                            );

                        RectangleBounds rect = new RectangleBounds(cell.center, defaultBuilding_size, side, dimensions);
                        buildingBoundsShells.Add(rect);

                        Vector3 dimensions2 = new Vector3(
                            defaultBuilding_dimensions.x * UtilityHelpers.RoundToNearestStep(UnityEngine.Random.Range(0.4f, 1f), 0.2f),
                            nodeGrid_CellLayerOffset / 2,
                            defaultBuilding_dimensions.z
                        );
                        RectangleBounds rect2 = new RectangleBounds(cell.sidePoints[side], defaultBuilding_size, side, dimensions2);
                        buildingBoundsShells.Add(rect2);

                        createdBlocks++;
                    }

                    if (createdBlocks > 2) break;
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

                            RectangleBounds new_rect = new RectangleBounds(pos, doorwayRadius, HexCoreUtil.GetRotationFromSide(side), extDoor_dimensions);
                            rect_doorwaysOuter.Add(new_rect);

                            BoundsShapeBlock new_doorwayShape = new BoundsShapeBlock(new_rect);
                            clearWithinBounds.Add(new_doorwayShape);
                            BoundsShapeBlock.GetIntersectingHexLookups(new_doorwayShape, cell.size, boundsShapesByCellLookup);

                            entrances++;
                        }
                    }

                    if (cell.layerNeighbors[1] != null && nullNeighborSides.Count >= 3) available_stairwayCellsByLayer[currentLayer].Add(cell);

                }

                BoundsShapeBlock new_cellBoundsShape = new BoundsShapeBlock(cell, 0.78f, 0.88f, edge);
                clearWithinBounds.Add(new_cellBoundsShape);
                BoundsShapeBlock.GetIntersectingHexLookups(new_cellBoundsShape, cell.size, boundsShapesByCellLookup);


                List<HexagonSide> innerNeighborSides = cell.GetNeighborSides(Filter_CellType.Any);

                foreach (var side in innerNeighborSides)
                {
                    Vector3 pos = cell.sidePoints[(int)side];
                    Vector3 posLookup = VectorUtil.PointLookupDefault(pos);

                    if (innerEntrywayLookups.Contains(posLookup)) continue;

                    innerEntrywayLookups.Add(posLookup);

                    RectangleBounds new_rect = new RectangleBounds(pos, doorwayRadius, HexCoreUtil.GetRotationFromSide(side), intDoor_dimensions);
                    rect_doorwaysInner.Add(new_rect);

                    BoundsShapeBlock new_doorwayShape = new BoundsShapeBlock(new_rect);
                    clearWithinBounds.Add(new_doorwayShape);
                    BoundsShapeBlock.GetIntersectingHexLookups(new_doorwayShape, cell.size, boundsShapesByCellLookup);
                }

                Vector3[] boundsCorners = HexCoreUtil.GenerateHexagonPoints(cell.center, cell.size);
                Bounds bounds = VectorUtil.CalculateBounds_V2(boundsCorners.ToList());
                new_structureBounds.Add(bounds);
            }
        }

        // List<HexagonCellPrototype> stairwayCells = new List<HexagonCellPrototype>();
        // stairwayCells = new Dictionary<Vector2, HexagonCellPrototype>();

        // foreach (var currentLayer in available_stairwayCellsByLayer.Keys)
        // {
        //     if (available_stairwayCellsByLayer[currentLayer].Count == 0) break;

        //     List<HexagonCellPrototype> sorted = available_stairwayCellsByLayer[currentLayer];

        //     // if (stairwayCells.Count > 0)
        //     // {
        //     sorted = sorted.FindAll(e => !stairwayCells.ContainsKey(e.GetLookup())); //;.OrderByDescending(e => e.GetNeighborSides(Filter_CellType.NullValue)).ToList();
        //                                                                              // }
        //                                                                              // else sorted = sorted.OrderByDescending(e => e.GetNeighborSides(Filter_CellType.NullValue)).ToList();

        //     if (sorted.Count == 0) continue;

        //     stairwayCells.Add(sorted[0].GetLookup(), sorted[0]);
        //     HexagonCellPrototype topNeighbor = sorted[0].layerNeighbors[1];
        //     if (topNeighbor != null && stairwayCells.ContainsKey(topNeighbor.GetLookup()) == false)
        //     {
        //         stairwayCells.Add(topNeighbor.GetLookup(), topNeighbor);
        //     }

        //     clearWithinBounds.Add(new BoundsShapeBlock(sorted[0], 0.78f, 1.8f, true));
        //     // RectangleBounds new_rect = new RectangleBounds(pos, doorwayRadius, HexCoreUtil.GetRotationFromSide(side), extDoor_dimensions);
        //     // rect_stairways.Add(new_rect);
        // }

        structureBounds = new_structureBounds;
        gridBounds = VectorUtil.CalculateBounds(structureBounds);
    }


    public void EvaluateGridLayers()
    {
        int nodeGridHeight = (nodeGrid_CellLayersMax * nodeGrid_CellLayerOffset);
        int tileGridHeight = (tileGrid_CellLayers * tileGrid_CellLayerOffset);

        if (tileGridHeight < nodeGridHeight || (tileGridHeight - tileGrid_CellLayerOffset) > nodeGridHeight)
        {
            Debug.Log("Recalculating tile grids... ");
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

    public void Evalaute_Folder()
    {
        if (folder_MeshObject == null)
        {
            folder_MeshObject = new GameObject("Template Folder" + this.gameObject.name).transform;
            folder_MeshObject.transform.SetParent(this.transform);
        }

        if (folder_GeneratedTiles == null)
        {
            folder_GeneratedTiles = new GameObject("Generated Tiles" + this.gameObject.name).transform;
            folder_GeneratedTiles.transform.SetParent(this.transform);
        }

        if (folder_Main == null)
        {
            folder_Main = new GameObject("WFC Tile" + this.gameObject.name).transform;
            folder_Main.transform.SetParent(this.transform);
        }
    }

    public static (Vector3[], Vector3[]) GenerateArchPoints(float width, float height, float depth, int resolution)
    {
        // Create an empty array to store the points
        Vector3[] points = new Vector3[resolution + 1];

        // Create an empty array to store the bottom 2 points of the arch
        Vector3[] bottomPoints = new Vector3[2];

        // Generate the points of the arch
        for (int i = 0; i <= resolution; i++)
        {
            float t = (float)i / resolution;
            float x = width * t;
            float y = height * Mathf.Sin(Mathf.PI * t);
            float z = depth * Mathf.Cos(Mathf.PI * t);
            points[i] = new Vector3(x, y, z);

            // Store the bottom 2 points of the arch
            if (i == 0 || i == resolution)
            {
                if (i == 0)
                {
                    bottomPoints[0] = new Vector3(x, 0, z);
                }
                else
                {
                    bottomPoints[1] = new Vector3(x, 0, z);
                }

            }
        }

        return (points, bottomPoints);
    }

    public static Vector3[] GenerateDoorwayPoints(Vector3[] archBottom, float width, float height)
    {
        Vector3[] doorwayPoints = new Vector3[4];
        Vector3 topLeft = archBottom[0];
        Vector3 topRight = archBottom[1];
        Vector3 bottomLeft = new Vector3(topLeft.x, topLeft.y - height, topLeft.z);
        Vector3 bottomRight = new Vector3(topRight.x, topRight.y - height, topRight.z);
        // Vector3 bottomRight = archBottom[0];
        // Vector3 bottomRight = archBottom[1];

        doorwayPoints[0] = topRight;
        doorwayPoints[1] = topLeft;
        doorwayPoints[2] = bottomLeft;
        doorwayPoints[3] = bottomRight;

        return doorwayPoints;
    }

    public static (Vector3[], Vector3[], Vector3[]) GenerateArchAndDoorwayPoints(float width, float height, float depth, int resolution, Transform transform)
    {
        // Generate the points of the arch and doorway
        (Vector3[] archPoints, Vector3[] archBottom) = GenerateArchPoints(width, height, depth, resolution);
        Vector3[] doorwayPoints = GenerateDoorwayPoints(archBottom, width, height);

        // Determine the rotation that aligns the shape's front with the game object's forward direction
        Quaternion rotation = Quaternion.LookRotation(transform.forward) * Quaternion.AngleAxis(90, Vector3.up);


        Vector3 difference = transform.position - archPoints[archPoints.Length - 1];

        // Apply the rotation to each point in the arrays
        for (int i = 0; i < archPoints.Length; i++)
        {
            archPoints[i] = rotation * archPoints[i];
            archPoints[i] += new Vector3(difference.x, 0, difference.z);
        }

        difference = transform.position - doorwayPoints[doorwayPoints.Length - 1];
        for (int i = 0; i < doorwayPoints.Length; i++)
        {
            doorwayPoints[i] = rotation * doorwayPoints[i];
            doorwayPoints[i] += new Vector3(difference.x, 0, difference.z);
        }

        // Merge the archPoints and doorwayPoints array
        Vector3[] points = new Vector3[archPoints.Length + doorwayPoints.Length];
        archPoints.CopyTo(points, 0);
        doorwayPoints.CopyTo(points, archPoints.Length);

        return (points, archPoints, doorwayPoints);
    }


    public static List<Vector3> GenerateVerticalGrid(Vector3 origin, float gridSize, int numRows, int numColumns)
    {
        List<Vector3> gridPoints = new List<Vector3>();

        for (int row = 0; row < numRows; row++)
        {
            for (int column = 0; column < numColumns; column++)
            {
                float x = origin.x + column * gridSize;
                float y = origin.y;
                float z = origin.z + row * gridSize;

                Vector3 point = new Vector3(x, y, z);
                gridPoints.Add(point);
            }
        }
        return gridPoints;
    }
}