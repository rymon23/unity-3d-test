using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using System.Linq;
using WFCSystem;

[System.Serializable]
public class CityGenerator : MonoBehaviour
{
    [SerializeField] private bool show_nodeGrid = true;
    [SerializeField] private bool show_tileGrid = true;
    [SerializeField] private bool show_blockGrid;
    [Header(" ")]
    [SerializeField] private bool show_doorways;
    [SerializeField] private bool show_stairwayNodes;
    [SerializeField] private bool show_clearingBounds;
    [Header(" ")]
    [SerializeField] private bool show_GridHexEdge;

    [Header(" ")]
    // [Range(0, 128)][SerializeField] private int _highlightedCell;
    // private HexagonCellPrototype _currentHighlightedCell = null;
    // [Header(" ")]

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


    [Header("Node Grid Settings")]
    [Range(4, 108)][SerializeField] private int nodeGrid_CellSize = 7;
    [Range(1, 48)][SerializeField] private int nodeGrid_CellLayers = 1;
    [Range(2, 12)][SerializeField] private int nodeGrid_CellLayerOffset = 4;
    [Range(12, 108)][SerializeField] private int nodeGrid_GridRadius = 36;

    [Header(" ")]
    [Range(1, 19)][SerializeField] private int nodeGrid_MaxCellsPerLayer = 7;
    [SerializeField] private HexCellSizes nodeGrid_SnapSize = HexCellSizes.X_4;
    [SerializeField] private Option_CellGridType nodeGrid_GridType = Option_CellGridType.Defualt;
    [Header(" ")]
    // [Range(4, 108)][SerializeField] private int foundation_alternateCell_size = 12;
    [Range(1, 12)][SerializeField] private int foundation_innersMax;
    [Range(1, 12)][SerializeField] private int foundation_cornersMax;
    [Range(0, 100)][SerializeField] private int foundation_random_Inners = 40;
    [Range(0, 100)][SerializeField] private int foundation_random_Corners = 40;
    [Range(0, 100)][SerializeField] private int foundation_random_Center = 100;

    [Header("Noise Mapping")]

    [SerializeField] private List<LayeredNoiseOption> layerdNoises_terrain = new List<LayeredNoiseOption>();
    [SerializeField] private float globalTerrainHeight = 90;
    [SerializeField] private float globalElevation = 0;
    [SerializeField] private int offsetMult = 2;

    [Header(" ")]

    [Header("Tile Grid Settings")]
    [SerializeField] private HexCellSizes tileGrid_CellSize = HexCellSizes.X_4;
    [Range(1, 48)][SerializeField] private int tileGrid_CellLayers = 1;
    [Range(2, 12)][SerializeField] private int tileGrid_CellLayerOffset = 4;
    [Range(12, 108)][SerializeField] private int tileGrid_GridRadius = 36;
    // [SerializeField] private Option_CellGridType tileGrid_GridType = Option_CellGridType.Defualt;

    [Header("Path Settings")]
    [SerializeField] private HexCellSizes path_cellSize = HexCellSizes.X_4;
    [SerializeField] private HexCellSizes path_SnapSize = HexCellSizes.X_4;
    [Range(12, 108)][SerializeField] private int path_HostCellSize = 36;
    [Range(12, 48)][SerializeField] private int path_StepDensity = 10;
    [SerializeField] private bool path_RandomIze;
    [Header(" ")]


    [Header("Building Settings")]
    [Range(0.2f, 0.9f)][SerializeField] private float innerRoomRadiusMult = 0.8f;
    [Header(" ")]
    [Range(1, 5)][SerializeField] private int entrancesMax = 2;
    [Range(1f, 4f)][SerializeField] private float doorwayRadius = 2;
    [Range(1f, 4f)][SerializeField] private float innerEntryRadius = 3;
    public Vector3 extDoor_dimensions = new Vector3(0.6f, 1.4f, 1.3f);
    public Vector3 intDoor_dimensions = new Vector3(0.6f, 1.4f, 1.8f);


    [Header("Node Clusters ")]
    [SerializeField] private bool show_HighlightedCluster;
    [Range(0, 128)][SerializeField] private int _highlightedCluster;
    private HexagonCellPrototype _currentHighlightedCluster = null;
    [Header(" ")]


    [Header("Surface Block Settings")]
    [Range(0.25f, 10f)][SerializeField] private float blockSize = 1f;
    [Range(12, 128)][SerializeField] private float boundsSize = 25;
    [Header(" ")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private TileDirectory tileDirectory;
    [Header(" ")]


    private Vector3[] gridCornerPoints;


    [Header(" ")]

    Bounds gridBounds;
    List<Bounds> structureBounds = new List<Bounds>();
    List<RectangleBounds> rect_doorwaysInner = null;
    List<RectangleBounds> rect_doorwaysOuter = null;
    List<RectangleBounds> rect_stairways = null;
    List<HexagonCellPrototype> baseEdges = null;
    List<HexagonCellPrototype> baseInners = null;

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
    Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, List<SurfaceBlock>>> tileInnerEdgesByCellSide = null;

    List<HexagonCellPrototype> pathCells = new List<HexagonCellPrototype>();
    List<HexagonCellPrototype> availibleOverPathCells = new List<HexagonCellPrototype>();
    List<HexagonCellPrototype> overPathCells = new List<HexagonCellPrototype>();
    Dictionary<Vector2, HexagonCellPrototype> overPathCellConnectors = null;

    // Dictionary<Vector2, Vector3> pathCenters = null;
    Dictionary<int, List<HexagonCellPrototype>> baseCellClustersList = null;
    List<List<HexagonCellPrototype>> nodeCells_byGroup = null;
    Dictionary<Vector2, Vector3> baseNodeCenters = null;
    Dictionary<Vector2, Vector3> basePathCenters = null;


    #region Saved State
    Vector3 _lastPosition;
    float _boundsSize;
    float _blockSize;
    float _updateDist = 1f;
    #endregion



    private void OnValidate()
    {
        if (
            resetPrototypes
            || _lastPosition != transform.position
            || _boundsSize != boundsSize
            || _blockSize != blockSize
            || hexNodeGrid == null
            || (hexNodeGrid != null && hexNodeGrid.cellLookup_ByLayer_BySize == null)
            // || _cellLayerOffset != cellLayerOffset
            // || _centerPosYOffset != centerPosYOffset
            )
        {

            resetPrototypes = false;

            _lastPosition = transform.position;

            boundsSize = UtilityHelpers.RoundToNearestStep(boundsSize, 2f);
            _boundsSize = boundsSize;

            blockSize = UtilityHelpers.RoundToNearestStep(blockSize, 0.25f);
            _blockSize = blockSize;

            gridCornerPoints = HexCoreUtil.GenerateHexagonPoints(transform.position, tileGrid_GridRadius);

            int nodeGridHeight = (nodeGrid_CellLayers * nodeGrid_CellLayerOffset);
            int tileGridHeight = (tileGrid_CellLayers * tileGrid_CellLayerOffset);

            // if (tileGridHeight < nodeGridHeight)
            // {
            //     Debug.LogError("(tileGridHeight < nodeGridHeight) - nodeGridHeight: " + nodeGridHeight + ", tileGridHeight: " + tileGridHeight);
            // }

            if (nodeGrid_CellLayerOffset < tileGrid_CellLayerOffset) tileGrid_CellLayerOffset = nodeGrid_CellLayerOffset;

            Vector3 gridStartPos = HexCoreUtil.Calculate_ClosestHexCenter_V2(transform.position, (int)nodeGrid_SnapSize);
            hexNodeGrid = new HexGrid(
                    gridStartPos,
                    nodeGrid_CellSize,
                    nodeGrid_CellLayers,
                    nodeGrid_CellLayerOffset,
                    nodeGrid_MaxCellsPerLayer,

                    nodeGrid_GridRadius,

                    nodeGrid_GridType
                );

            Vector3 tileGridStartPos = HexCoreUtil.Calculate_ClosestHexCenter_V2(transform.position, (int)tileGrid_CellSize);
            hexTileGrid = new HexGrid(
                    tileGridStartPos,
                    (int)tileGrid_CellSize,
                    tileGrid_CellLayers,
                    tileGrid_CellLayerOffset,
                    // (int)HexCellSizes.X_36,
                    tileGrid_GridRadius,
                    Option_CellGridType.Defualt
                );


            _currentHighlightedCluster = null;
            nodeCells_byGroup = null;

            HexGridCityLayout_V2(
                foundation_innersMax,
                foundation_cornersMax,
                foundation_random_Inners,
                foundation_random_Corners,
                foundation_random_Center
            );

            // HexGridCityLayout();
            HexGridToBuildingsSetup();
        }


        if (nodeCells_byGroup != null)
        {
            int count = nodeCells_byGroup.Count;
            // Debug.Log("nodeCells_byGroup.Count: " + count);

            if (_highlightedCluster == count) _highlightedCluster = 0;
            _highlightedCluster = Mathf.Clamp(_highlightedCluster, 0, count - 1);
        }
        else _highlightedCluster = -1;


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

        if (show_GridHexEdge)
        {
            if (gridCornerPoints == null)

                Gizmos.color = Color.magenta;
            for (int j = 0; j < gridCornerPoints.Length; j++)
            {
                Gizmos.DrawSphere(gridCornerPoints[j], 1);
            }
            VectorUtil.DrawHexagonPointLinesInGizmos(gridCornerPoints);
        }

        if (show_nodeGrid && hexNodeGrid != null && hexNodeGrid.cellLookup_ByLayer_BySize != null && hexNodeGrid.cellLookup_ByLayer_BySize.Count > 0)
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
                        Gizmos.color = Color.black;
                        Gizmos.DrawSphere(cell.center, 12 / 2);
                        // if (cell.IsEdge() == false) continue;
                    }
                }


            }



            Gizmos.color = Color.white;
            VectorUtil.DrawRectangleLines(gridBounds);

            if (show_doorways)
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

            if (show_stairwayNodes && stairwayCells != null)
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

            if (show_clearingBounds && clearWithinBounds != null)
            {
                Gizmos.color = customColors["orange"];
                foreach (var item in clearWithinBounds)
                {
                    item.DrawPoints();
                }
            }

            if (show_HighlightedCluster && nodeCells_byGroup != null)
            {
                for (int i = 0; i < nodeCells_byGroup.Count; i++)
                {
                    if (!show_HighlightedCluster || (show_HighlightedCluster && _highlightedCluster == i))
                    {
                        foreach (var cell in nodeCells_byGroup[i])
                        {
                            Gizmos.color = Color.black;
                            if (cell.layerNeighbors[1] == null) Gizmos.color = Color.red;
                            Gizmos.DrawSphere(cell.center, cell.size / 3);
                        }
                    }
                }
            }


            // if (baseNodeCenters != null)
            // {
            //     foreach (var item in baseNodeCenters.Values)
            //     {
            //         // Gizmos.color = Color.yellow;
            //         // VectorUtil.DrawHexagonPointLinesInGizmos(item, 12);

            //         Gizmos.color = Color.blue;
            //         Gizmos.DrawWireSphere(item, 12 / 2);
            //     }
            // }

            // Gizmos.color = Color.green;
            // foreach (var item in bufferNodes.Values)
            // {
            //     Gizmos.DrawSphere(item, size / 2);
            // }

            // Gizmos.color = Color.red;
            // foreach (var item in buildingBlockClusters.Values)
            // {
            //     foreach (var pt in item.Values)
            //     {
            //         Gizmos.DrawSphere(pt, size / 2);
            //     }
            //     break;
            // }

        }


        if (show_tileGrid && hexTileGrid.cellLookup_ByLayer_BySize != null && hexTileGrid.cellLookup_ByLayer_BySize.Count > 0)
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
            }


            if (pathCells != null)
            {
                // Debug.LogError("pathCells.Count: " + pathCells.Count);

                foreach (var cell in pathCells)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(cell.center, cell.size / 2);
                }
            }

            if (overPathCells != null)
            {
                // Debug.LogError("overPathCells.Count: " + overPathCells.Count);

                foreach (var cell in overPathCells)
                {
                    Gizmos.color = customColors["purple"];
                    Gizmos.DrawWireSphere(cell.center, cell.size / 2);
                }
            }
            if (overPathCellConnectors != null)
            {
                // Debug.LogError("overPathCellConnectors.Count: " + overPathCellConnectors.Count);

                foreach (var cell in overPathCellConnectors.Values)
                {
                    Gizmos.color = customColors["orange"];
                    Gizmos.DrawWireSphere(cell.center, cell.size / 2);
                }
            }

            // if (basePathCenters != null)
            // {
            //     foreach (var k in basePathCenters.Keys)
            //     {
            //         Gizmos.color = Color.green;
            //         Gizmos.DrawSphere(basePathCenters[k], 4 / 2);
            //     }
            // }

            // if (baseCellClustersList != null)
            // {
            //     foreach (var ix in baseCellClustersList.Keys)
            //     {
            //         foreach (var cell in baseCellClustersList[ix])
            //         {
            //             Gizmos.color = Color.black;
            //             Gizmos.DrawSphere(cell.center, cell.size / 2);
            //         }
            //     }
            // }

        }

        if (show_blockGrid)
        {
            if (surfaceBlocksGrid == null)
            {
                int height = ((nodeGrid_CellLayers) * nodeGrid_CellLayerOffset);

                gridBounds = VectorUtil.CalculateBounds(structureBounds);
                (
                    Vector3[,,] points,
                    float spacing
                ) = VectorUtil.Generate3DGrid(gridBounds, blockSize, transform.position.y, height);

                Dictionary<Vector3, SurfaceBlock> blockCenterLookup = new Dictionary<Vector3, SurfaceBlock>();

                surfaceBlocksGrid = SurfaceBlock.CreateSurfaceBlocks(
                        points,
                        spacing,
                        hexNodeGrid.cellLookup_ByLayer_BySize[nodeGrid_CellSize][hexNodeGrid.baseLayer],
                        blockCenterLookup
                        );

                // surfaceBlocksGrid = SurfaceBlock.CreateSurfaceBlocks(points, hexNodeGrid.cellLookup_ByLayer_BySize[nodeGrid_CellSize][hexNodeGrid.baseLayer], );
                // surfaceBlocksGrid = SurfaceBlock.CreateSurfaceBlocks(points, structureBounds, spacing);

                surfaceBlocksByCell = SurfaceBlock.GetSurfaceBlocksByCell(
                        surfaceBlocksGrid,
                        hexTileGrid.cellLookup_ByLayer_BySize[(int)tileGrid_CellSize],
                        tileGrid_CellLayerOffset
                    );

                // surfaceBlocksGrid = SurfaceBlock.ClearInnerBlocks(surfaceBlocksGrid);

                // surfaceBlocksGrid = SurfaceBlock.ClearInnerBlocks(surfaceBlocksGrid, clearWithinBounds);

                foreach (var item in clearWithinBounds)
                {
                    surfaceBlocksGrid = SurfaceBlock.ClearInnerBlocks(surfaceBlocksGrid, item);
                }

                SurfaceBlock.EvaluateTileEdges(surfaceBlocksGrid);

                tileInnerEdgesByCellSide = SurfaceBlock.GetTileInnerEdgesByCellSide(surfaceBlocksByCell);

                Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, TileSocketProfile>> cellTileSocketProfiles =
                    SurfaceBlock.Generate_CellTileSocketProfiles(tileInnerEdgesByCellSide);
            }
            else
            {
                SurfaceBlock.DrawGrid(surfaceBlocksGrid);


                // if (tileInnerEdgesByCellSide != null)
                // {

                //     foreach (var cell in tileInnerEdgesByCellSide.Keys)
                //     {
                //         // Gizmos.color = Color.black;
                //         // Gizmos.DrawSphere(cell.center, 1f);

                //         // if (showHighlightedCell && cell != _currentHighlightedCell) continue;
                //         foreach (var side in tileInnerEdgesByCellSide[cell].Keys)
                //         {
                //             // if (highlight_SurfaceBlockEdgeSide && side != highlightSide) continue;

                //             // Gizmos.color = Color.magenta;
                //             // foreach (var block in tileInnerEdgesByCellSide[cell][side])
                //             // {
                //             //     Gizmos.DrawSphere(block.Position, 0.3f);
                //             // }
                //         }

                //     }
                // }
            }
        }

    }



    public static void MapNoiseElevationToBaseCellGroups(
        List<List<HexagonCellPrototype>> cellGroups,
        List<LayeredNoiseOption> layerdNoises_terrain,
        float globalTerrainHeight,
        float globalElevation,
        int cellLayerOffset = 3,
        int offsetMult = 3
    )
    {
        int initialElevation = 0;

        foreach (var group in cellGroups)
        {
            bool initialAssigned = false;
            int baseElevation = 0;

            foreach (var cell in group)
            {
                Vector3 center = cell.center;

                if (!initialAssigned)
                {
                    baseElevation = cellLayerOffset * UnityEngine.Random.Range(0, offsetMult + 1);

                    // float baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)center.x, (int)center.z, globalTerrainHeight, layerdNoises_terrain);
                    // baseNoiseHeight += globalElevation;

                    // baseElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(baseNoiseHeight, cellLayerOffset);
                    // Debug.Log("baseElevation: " + baseElevation + ",  baseNoiseHeight: " + baseNoiseHeight);


                    cell.UpdateLayer((int)baseElevation);

                    initialAssigned = true;
                }
                else
                {
                    cell.UpdateLayer((int)baseElevation);
                }
            }
        }
    }

    public static void MapNoiseElevationToFoundationNodeGroups(
        Dictionary<int, Dictionary<Vector2, Vector3>> nodeGroups,
        List<LayeredNoiseOption> layerdNoises_terrain,
        float globalTerrainHeight,
        float globalElevation,
        int cellLayerOffset = 3,
        int offsetMult = 2
    )
    {
        List<int> indexes = nodeGroups.Keys.ToList();

        foreach (var ix in indexes)
        {
            List<Vector2> lookups = nodeGroups[ix].Keys.ToList();
            bool initialAssigned = false;
            float baseElevation = 0;

            foreach (var lookup in lookups)
            {
                Vector3 center = nodeGroups[ix][lookup];

                if (!initialAssigned)
                {
                    float baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)center.x, (int)center.z, globalTerrainHeight, layerdNoises_terrain);
                    baseNoiseHeight += globalElevation;

                    baseElevation = (int)UtilityHelpers.RoundHeightToNearestElevation(baseNoiseHeight, cellLayerOffset);
                    Debug.Log("baseElevation: " + baseElevation + ",  baseNoiseHeight: " + baseNoiseHeight);

                    center.y = baseElevation;
                    nodeGroups[ix][lookup] = center;

                    initialAssigned = true;
                }
                else
                {
                    center.y = baseElevation;
                    nodeGroups[ix][lookup] = center;
                }
            }
        }
    }

    public void HexGridCityLayout_V2(
        int foundation_innersMax,
        int foundation_cornersMax,
        int foundation_random_Inners = 40,
        int foundation_random_Corners = 40,
        int foundation_random_Center = 100

    )
    {
        Vector3 gridStartPos = HexCoreUtil.Calculate_ClosestHexCenter_V2(transform.position, (int)36);

        (Dictionary<Vector2, Vector3> _baseNodeCenters,
        Dictionary<Vector2, Vector3> _bufferNodes
        ) = HexCoreUtil.Generate_FoundationPoints(
            gridStartPos,
            36,
            foundation_innersMax,
            foundation_cornersMax,
            foundation_random_Inners,
            foundation_random_Corners,
            foundation_random_Center
        );

        List<Vector3> hostPoints = HexCoreUtil.GenerateHexCenterPoints_X13(gridStartPos, 36);
        HashSet<Vector2> added = new HashSet<Vector2>();
        HashSet<Vector2> excludeList = new HashSet<Vector2>();
        foreach (var k in _bufferNodes.Keys)
        {
            excludeList.Add(k);
        }

        Dictionary<int, Dictionary<Vector2, Vector3>> new_buildingBlockClusters = HexCoreUtil.Generate_BaseBuildingNodeGroups(
               gridStartPos,
               excludeList,
               (int)HexCellSizes.Worldspace
           );


        // MapNoiseElevationToFoundationNodeGroups(
        //      new_buildingBlockClusters,
        //      layerdNoises_terrain,
        //      globalTerrainHeight,
        //      globalElevation,
        //      nodeGrid_CellLayerOffset,
        //      2
        //  );


        List<HexagonCellPrototype> new_baseCellsInStructureBounds = new List<HexagonCellPrototype>();
        Dictionary<int, List<HexagonCellPrototype>> new_baseCellClustersList = new Dictionary<int, List<HexagonCellPrototype>>();
        pathCells = new List<HexagonCellPrototype>();


        (
            Dictionary<int, Dictionary<Vector2, Vector3>> new_buildingBlockClusters__,
            Dictionary<Vector2, Vector3> _pathCenters
        ) = HexCoreUtil.Generate_CellCityClusterCenters(
            gridStartPos,
            // HexCoreUtil.GetRandomCellCenterPointWithinRadius(transform.position, 36),
            (int)HexCellSizes.X_4,
            36,
            99,
            10,
            true,
            65,
            60
        );


        foreach (var key in hexTileGrid.cellLookup_ByLayer_BySize[(int)tileGrid_CellSize][hexTileGrid.baseLayer].Keys)
        {
            HexagonCellPrototype cell = hexTileGrid.cellLookup_ByLayer_BySize[(int)tileGrid_CellSize][hexTileGrid.baseLayer][key];

            if (HexCoreUtil.IsAnyHexPointOutsidePolygon(cell.center, cell.size, gridCornerPoints) == false)
            // if (VectorUtil.IsPointWithinBounds(structureBounds, cell.center))
            {
                new_baseCellsInStructureBounds.Add(cell);

                if (_pathCenters.ContainsKey(key))
                {
                    cell.SetPathCell(true);
                    pathCells.Add(cell);
                }
                else
                {

                    foreach (int ix in new_buildingBlockClusters.Keys)
                    {
                        var group = new_buildingBlockClusters[ix];
                        if (group.ContainsKey(key))
                        {
                            if (new_baseCellClustersList.ContainsKey(ix) == false) new_baseCellClustersList.Add(ix, new List<HexagonCellPrototype>());

                            new_baseCellClustersList[ix].Add(cell);
                            break;
                        }
                    }
                }
            }
        }

        basePathCenters = _pathCenters;
        baseNodeCenters = _baseNodeCenters;
        baseCellClustersList = new_baseCellClustersList;
        // buildingBlockClusters = new_buildingBlockClusters;


        // List<HexagonCellPrototype> new_baseCellsInStructureBounds = new List<HexagonCellPrototype>();
        // Dictionary<int, List<HexagonCellPrototype>> new_baseCellClustersList = new Dictionary<int, List<HexagonCellPrototype>>();
        // pathCells = new List<HexagonCellPrototype>();

        // buildingBlockClusters = new_buildingBlockClusters;


        // foreach (var key in cellLookup_ByLayer_BySize[(int)cellSize][_baseLayer].Keys)
        // {
        //     HexagonCellPrototype cell = cellLookup_ByLayer_BySize[(int)cellSize][_baseLayer][key];

        //     if (VectorUtil.IsPointWithinBounds(structureBounds, cell.center))
        //     {
        //         new_baseCellsInStructureBounds.Add(cell);

        //         if (pathCenters.ContainsKey(key))
        //         {
        //             cell.SetPathCell(true);
        //             pathCells.Add(cell);
        //         }
        //         else
        //         {
        //             foreach (int ix in buildingBlockClusters.Keys)
        //             {
        //                 var group = buildingBlockClusters[ix];
        //                 if (group.ContainsKey(key))
        //                 {
        //                     if (new_baseCellClustersList.ContainsKey(ix) == false) new_baseCellClustersList.Add(ix, new List<HexagonCellPrototype>());

        //                     new_baseCellClustersList[ix].Add(cell);
        //                     break;
        //                 }
        //             }
        //         }
        //     }
        // }

    }


    public void HexGridCityLayout()
    {
        // Vector3 gridStartPos = HexCoreUtil.Calculate_ClosestHexCenter_V2(transform.position, (int)path_SnapSize);
        Vector3 gridStartPos = HexCoreUtil.GetRandomCellCenterPointWithinRadius(transform.position, tileGrid_GridRadius, (int)path_SnapSize);
        (
            Dictionary<int, Dictionary<Vector2, Vector3>> new_buildingBlockClusters,
            Dictionary<Vector2, Vector3> _pathCenters
        ) = HexCoreUtil.Generate_CellCityClusterCenters(
                gridStartPos,
                (int)path_cellSize,
                path_HostCellSize,
                99,
                path_StepDensity,
                path_RandomIze
            );

        List<HexagonCellPrototype> new_baseCellsInStructureBounds = new List<HexagonCellPrototype>();
        Dictionary<int, List<HexagonCellPrototype>> new_baseCellClustersList = new Dictionary<int, List<HexagonCellPrototype>>();
        pathCells = new List<HexagonCellPrototype>();


        foreach (var key in hexTileGrid.cellLookup_ByLayer_BySize[(int)tileGrid_CellSize][hexTileGrid.baseLayer].Keys)
        {
            HexagonCellPrototype cell = hexTileGrid.cellLookup_ByLayer_BySize[(int)tileGrid_CellSize][hexTileGrid.baseLayer][key];

            if (HexCoreUtil.IsAnyHexPointOutsidePolygon(cell.center, cell.size, gridCornerPoints) == false)
            // if (VectorUtil.IsPointWithinBounds(structureBounds, cell.center))
            {
                new_baseCellsInStructureBounds.Add(cell);

                if (_pathCenters.ContainsKey(key))
                {
                    cell.SetPathCell(true);
                    pathCells.Add(cell);
                }
                else
                {

                    foreach (int ix in new_buildingBlockClusters.Keys)
                    {
                        var group = new_buildingBlockClusters[ix];
                        if (group.ContainsKey(key))
                        {
                            if (new_baseCellClustersList.ContainsKey(ix) == false) new_baseCellClustersList.Add(ix, new List<HexagonCellPrototype>());

                            new_baseCellClustersList[ix].Add(cell);
                            break;
                        }
                    }
                }
            }
        }

        baseCellClustersList = new_baseCellClustersList;
        // buildingBlockClusters = new_buildingBlockClusters;
        // pathCenters = _pathCenters;
    }

    public void HexGridToBuildingsSetup()
    {
        surfaceBlocksGrid = null;

        baseEdges = new List<HexagonCellPrototype>();
        baseInners = new List<HexagonCellPrototype>();

        rect_doorwaysOuter = new List<RectangleBounds>();
        rect_doorwaysInner = new List<RectangleBounds>();
        rect_stairways = new List<RectangleBounds>();
        clearWithinBounds = new List<BoundsShapeBlock>();

        int entrances = 0;
        Dictionary<HexagonSide, Vector3> extEntrancesByLookupBySide = new Dictionary<HexagonSide, Vector3>();
        HashSet<Vector2> extEntryCells = new HashSet<Vector2>();
        HashSet<Vector3> innerEntrywayLookups = new HashSet<Vector3>();

        List<Bounds> new_structureBounds = new List<Bounds>();

        float roomRadius = (innerRoomRadiusMult * nodeGrid_CellSize);

        bool shouldHaveStairs = nodeGrid_CellLayers > 1;

        Dictionary<int, List<HexagonCellPrototype>> available_stairwayCellsByLayer = new Dictionary<int, List<HexagonCellPrototype>>();

        Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>> updated_cellLookup_ByLayer_BySize = new Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>();
        updated_cellLookup_ByLayer_BySize.Add(nodeGrid_CellSize, new Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>());

        Dictionary<int, List<HexagonCellPrototype>> neighborsToEvaluate_bySize = new Dictionary<int, List<HexagonCellPrototype>>();
        neighborsToEvaluate_bySize.Add(nodeGrid_CellSize, new List<HexagonCellPrototype>());

        availibleOverPathCells = new List<HexagonCellPrototype>();

        foreach (var currentLayer in hexNodeGrid.cellLookup_ByLayer_BySize[nodeGrid_CellSize].Keys)
        {
            if (available_stairwayCellsByLayer.ContainsKey(currentLayer) == false) available_stairwayCellsByLayer.Add(currentLayer, new List<HexagonCellPrototype>());

            updated_cellLookup_ByLayer_BySize[nodeGrid_CellSize].Add(currentLayer, new Dictionary<Vector2, HexagonCellPrototype>());

            foreach (var lookup in hexNodeGrid.cellLookup_ByLayer_BySize[nodeGrid_CellSize][currentLayer].Keys)
            {
                HexagonCellPrototype cell = hexNodeGrid.cellLookup_ByLayer_BySize[nodeGrid_CellSize][currentLayer][lookup];
                bool removeNode = false;

                if (HexCoreUtil.IsAnyHexPointOutsidePolygon(cell.center, cell.size, gridCornerPoints))
                {
                    removeNode = true;
                }
                else
                {
                    foreach (HexagonCellPrototype pathCell in pathCells)
                    {
                        if (
                            HexCoreUtil.IsAnyHexPointWithinPolygon(pathCell.center, pathCell.size, cell.cornerPoints)
                        // && HexCoreUtil.IsCellA_Within_CellB_VerticalBounds(pathCell, cell)
                        )
                        {
                            availibleOverPathCells.Add(cell);

                            removeNode = true;
                            break;
                        }
                    }
                }

                if (removeNode)
                {
                    // hexNodeGrid.cellLookup_ByLayer_BySize[nodeGrid_CellSize][currentLayer][lookup] = null;
                    continue;
                }

                updated_cellLookup_ByLayer_BySize[nodeGrid_CellSize][currentLayer].Add(lookup, cell);

                cell = updated_cellLookup_ByLayer_BySize[nodeGrid_CellSize][currentLayer][lookup];
                cell.ClearNeighborLists();
                // cell.layerNeighbors = new HexagonCellPrototype[2];

                neighborsToEvaluate_bySize[nodeGrid_CellSize].Add(cell);

                // clearWithinBounds.Add(new BoundsShapeBlock(cell, 0.75f, 0.8f));

                // bool edge = cell.IsEdge();
                // if (edge) baseEdges.Add(cell);
                // else baseInners.Add(cell);

                // if (edge)
                // {
                //     List<HexagonSide> nullNeighborSides = cell.GetNeighborSides(Filter_CellType.NullValue);
                //     bool entryAssigned = false;

                //     foreach (var side in nullNeighborSides)
                //     {
                //         if (entrances < entrancesMax && (entrances == 0 || UnityEngine.Random.Range(0, 5) < 2))
                //         {
                //             if (extEntryCells.Contains(cell.GetLookup())) continue;
                //             if (extEntrancesByLookupBySide.ContainsKey(side)) continue;

                //             extEntryCells.Add(cell.GetLookup());
                //             entryAssigned = true;

                //             Vector3 pos = cell.sidePoints[(int)side];

                //             extEntrancesByLookupBySide.Add(side, pos);

                //             RectangleBounds new_rect = new RectangleBounds(pos, doorwayRadius, HexCoreUtil.GetRotationFromSide(side), extDoor_dimensions);
                //             rect_doorwaysOuter.Add(new_rect);
                //             clearWithinBounds.Add(new BoundsShapeBlock(new_rect));

                //             entrances++;
                //         }
                //     }

                //     if (!entryAssigned && nullNeighborSides.Count >= 3 && cell.layerNeighbors[1] != null) available_stairwayCellsByLayer[currentLayer].Add(cell);
                // }

                // List<HexagonSide> innerNeighborSides = cell.GetNeighborSides(Filter_CellType.Any);

                // foreach (var side in innerNeighborSides)
                // {
                //     Vector3 pos = cell.sidePoints[(int)side];
                //     Vector3 posLookup = VectorUtil.PointLookupDefault(pos);

                //     if (innerEntrywayLookups.Contains(posLookup)) continue;

                //     innerEntrywayLookups.Add(posLookup);

                //     RectangleBounds new_rect = new RectangleBounds(pos, doorwayRadius, HexCoreUtil.GetRotationFromSide(side), intDoor_dimensions);
                //     rect_doorwaysInner.Add(new_rect);
                //     clearWithinBounds.Add(new BoundsShapeBlock(new_rect));
                // }

                // Vector3[] boundsCorners = HexCoreUtil.GenerateHexagonPoints(cell.center, cell.size);
                // Bounds bounds = VectorUtil.CalculateBounds_V2(boundsCorners.ToList());
                // new_structureBounds.Add(bounds);
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
        //     // }
        //     // else sorted = sorted.OrderByDescending(e => e.GetNeighborSides(Filter_CellType.NullValue)).ToList();

        //     stairwayCells.Add(sorted[0].GetLookup(), sorted[0]);
        //     HexagonCellPrototype topNeighbor = sorted[0].layerNeighbors[1];
        //     if (topNeighbor != null && stairwayCells.ContainsKey(topNeighbor.GetLookup()) == false)
        //     {
        //         stairwayCells.Add(topNeighbor.GetLookup(), topNeighbor);
        //     }

        //     clearWithinBounds.Add(new BoundsShapeBlock(sorted[0], 0.7f, 1.8f));
        //     // RectangleBounds new_rect = new RectangleBounds(pos, doorwayRadius, HexCoreUtil.GetRotationFromSide(side), extDoor_dimensions);
        //     // rect_stairways.Add(new_rect);
        // }

        hexNodeGrid.SetCells(updated_cellLookup_ByLayer_BySize, neighborsToEvaluate_bySize);
        nodeCells_byGroup = null;
        nodeCells_byGroup = HexGridPathingUtil.GetConsecutiveClustersList(hexNodeGrid.cellLookup_ByLayer_BySize[nodeGrid_CellSize]);

        overPathCells = new List<HexagonCellPrototype>();
        overPathCellConnectors = new Dictionary<Vector2, HexagonCellPrototype>();

        if (availibleOverPathCells != null && availibleOverPathCells.Count > 0)
        {
            // if (hexNodeGrid.cellLookup_ByLayer_BySize.ContainsKey(hexNodeGrid.baseLayer) == false)
            // {
            //     Debug.LogError("(hexNodeGrid.cellLookup_ByLayer_BySize.ContainsKey(hexNodeGrid.baseLayer) == false) - hexNodeGrid.baseLayer: " + hexNodeGrid.baseLayer);
            // }

            foreach (var cell in availibleOverPathCells)
            {
                if (HexCellUtil.IsCellInBetweenNeighborsInLookup(cell, hexNodeGrid.cellLookup_ByLayer_BySize[nodeGrid_CellSize][hexNodeGrid.baseLayer], overPathCellConnectors))
                {
                    overPathCells.Add(cell);
                }
            }
        }

        //temp
        // MapNoiseElevationToBaseCellGroups(
        //      nodeCells_byGroup,
        //      layerdNoises_terrain,
        //      globalTerrainHeight,
        //      globalElevation,
        //      nodeGrid_CellLayerOffset,
        //      2
        //  );

        structureBounds = new_structureBounds;
        gridBounds = VectorUtil.CalculateBounds(structureBounds);

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

}