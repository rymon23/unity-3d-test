
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProceduralBase;

namespace WFCSystem
{
    public enum Option_CellGridType { Defualt = 0, RandomConsecutive = 1, ConsecutiveHost, RandomConsecutiveByHost }
    public enum BuildStructureType { Building, OutPost, BuildingV2 }

    public class HexTileGrid : MonoBehaviour
    {
        Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>> cellLookup_ByLayer_BySize = new Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>();
        List<HexagonCellPrototype> baseEdges = new List<HexagonCellPrototype>();
        List<HexagonCellPrototype> allBaseCells = new List<HexagonCellPrototype>();
        List<HexagonCellPrototype> baseCellsInStructureBounds = new List<HexagonCellPrototype>();
        List<HexagonCellPrototype> pathCells = new List<HexagonCellPrototype>();
        Dictionary<int, List<HexagonCellPrototype>> baseCellClustersList = null;
        Dictionary<int, Dictionary<Vector2, Vector3>> buildingBlockClusters = null;

        List<HexTileBuilder> hexTileBuilders = null;

        // Dictionary<HexagonCellPrototype, Dictionary<HexagonSide, Vector3>> edgeCellConnectorPoints = new Dictionary<HexagonCellPrototype, Dictionary<HexagonSide, Vector3>>();
        // Dictionary<HexagonCellPrototype, Dictionary<HexagonSide, int>> edgeCellConnectorStep = new Dictionary<HexagonCellPrototype, Dictionary<HexagonSide, int>>();
        List<Bounds> structureBounds = new List<Bounds>();
        List<Vector3> boundsEdgePoints = new List<Vector3>();
        Dictionary<Vector3, Node> nodes = null;


        List<HexagonTileTemplate> generatedTiles = null;

        HexagonCellPrototype entranceCell = null;
        HexagonSide entranceSide;
        Bounds entranceBounds;
        Bounds gridStructureBounds;
        SurfaceBlock[,,] surfaceBlocksGrid;
        BuildingNode[,,] buildingNodeGrid;
        List<List<BuildingNode>> buildingNodeClusters = null;
        List<List<SurfaceBlock>> surfaceBlockClusters = null;
        Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, List<Vector3>>> intersectionPointsBySideByCell = null;
        Dictionary<HexagonCellPrototype, List<SurfaceBlock>> surfaceBlocksByCell = null;
        Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, List<SurfaceBlock>>> tileInnerEdgesByCellSide = null;

        [SerializeField] private BuildStructureType buildStructureType = BuildStructureType.Building;
        [Header(" ")]
        [SerializeField] private bool showCellGrids;
        [SerializeField] private CellDisplay_Type cellDisplayType = CellDisplay_Type.DrawCenterAndLines;
        [SerializeField] private bool showHighlights;
        [SerializeField] private GridFilter_Level gridFilter_Level;
        [SerializeField] private GridFilter_Type gridFilter_Type;
        [SerializeField] private HexCellSizes gridFilter_size = HexCellSizes.Default;
        [Header(" ")]
        [SerializeField] private bool showTotalStructureBounds;
        [SerializeField] private bool showInnerStructureBounds;
        [SerializeField] private bool showBuildingStructures;
        [Header(" ")]
        [SerializeField] private bool showGridBounds;
        [SerializeField] private bool showSurfaceBlocks = true;
        [SerializeField] private bool showSurfaceBlockGrid = true;
        [SerializeField] private bool showSurfaceBlockEdgeSockets = true;
        [Header(" ")]
        [SerializeField] private bool highlight_SurfaceBlockEdgeSide;
        [SerializeField] private HexagonTileSide highlightSide;
        [Header(" ")]

        [SerializeField] private bool showHighlightedCell;
        [SerializeField] private bool showNodes = true;
        [SerializeField] private bool showEdgeLines;
        // [SerializeField] private bool showRawBoundsEdge;
        [Header(" ")]

        [Header("Cell Grid Settings")]
        [SerializeField] private Option_CellGridType cellGridType = Option_CellGridType.Defualt;
        [Range(2, 108)][SerializeField] private int randomGridMembersMax = 14;
        [SerializeField] private bool randomGridExpansion;
        [SerializeField] private bool consecutiveExpansionShuffle;
        [Range(1, 72)][SerializeField] private int consecutiveExpansionGridHostMembersMax = 3;
        [Header(" ")]
        [SerializeField] private HexCellSizes cellSize = HexCellSizes.Default;
        [SerializeField] private int radius = 12;
        [Range(0, 3)][SerializeField] private int radiusMult = 1;
        public void SetElevationOffset(int value)
        {
            centerPosYOffset = value;
        }

        [Header(" ")]
        [Range(0, 128)][SerializeField] private int _highlightedCell;
        private HexagonCellPrototype _currentHighlightedCell = null;

        [Header(" ")]

        [Header("Layer Settings")]
        [Range(1, 48)][SerializeField] private int cellLayersMax = 3;
        [Range(2, 12)][SerializeField] private int cellLayerOffset = 3;
        [SerializeField] private bool randomizeCellLayers;
        public int GetCellInLayerElevation() => cellLayerOffset;
        public int CalculateRandomLayers() => UnityEngine.Random.Range(1, cellLayersMax + 1);

        [Header("Build Error Correction")]
        [Range(0, 5)][SerializeField] private int buildReattempts = 3;

        [Header("Surface Block Settings")]
        [Range(0.25f, 10f)][SerializeField] private float blockSize = 1f;
        [Range(5, 50)][SerializeField] private int gridSizeXZ = 10;
        [Range(2, 50)][SerializeField] private int gridSizeY = 3;
        [Range(0, 99)][SerializeField] private int blockGrid_clustersMax = 2;
        [SerializeField] private Vector3 blockGrid_cluster_LWH_Min = new Vector3(0, 0, 0);
        [SerializeField]
        private List<SurfaceBlockState> blockGrid_filterOnStates = new List<SurfaceBlockState>() {
            SurfaceBlockState.Entry,
        };
        [Range(0, 4)][SerializeField] private int blockGrid_entrancesMax = 2;


        [Header("Building Node Settings")]
        [Range(6f, 50f)][SerializeField] private float buildingNode_Size = 6f;
        [Range(2, 50)][SerializeField] private int buildingNode_gridSizeY = 3;
        [Header(" ")]
        [Range(1, 6)][SerializeField] private int buildingNode_clusterSizeMin = 1;
        [Range(1, 12)][SerializeField] private int buildingNode_clusterSizeMax = 6;
        [Header(" ")]
        [Range(12, 48)][SerializeField] private int cellPathing_hostSizeMin = 36;
        [Range(12, 48)][SerializeField] private int cellPathing_hostSizeMax = 36;

        [Header("Structure Prototype Settings")]
        [Range(1, 48)][SerializeField] private int model_layersMax = 3;
        [Range(0.5f, 5f)][SerializeField] private float model_layerOffeset = 1f;
        [Range(1f, 2f)][SerializeField] private float model_cellBoundsSizeMult = 1f;

        [Header(" ")]
        [Range(0.2f, 2f)][SerializeField] private float model_cellBoundsSizeMult_entrance = 0.6f;
        [Range(1f, 10f)][SerializeField] private float boundEdgePointdistanceThreshold = 6f;
        [Range(2, 20)][SerializeField] private int boundEdgeLineDensity = 2;
        [Header(" ")]
        [Range(0.1f, 0.9f)][SerializeField] private float cellEdgeStep = 1f;
        // [SerializeField] private bool randomizeCellLayers;
        [Header(" ")]
        [SerializeField] private float centerPosYOffset = 0;
        [SerializeField] private bool resetPrototypes;
        [Header(" ")]
        // [SerializeField] 
        private bool generate_meshTemplate;
        [Header(" ")]
        [SerializeField] private bool generate_surfaceMeshTemplate;
        [Header(" ")]
        [SerializeField] private bool generate_tiles;
        [SerializeField] private bool useCompatibilityCheck;
        [SerializeField] private bool instantiateOnGenerate;
        [Header(" ")]
        [SerializeField] private bool execute_WFC;
        [SerializeField] private bool allowRotations;
        [Header(" ")]
        [SerializeField] private TileDirectory tileDirectory;
        [SerializeField] private LocationMarkerPrefabOption locationPrefabOption;
        [SerializeField] private GameObject prefab;

        #region Saved State
        Vector3 gridCenterPos;
        Vector3 _lastPosition;
        HexCellSizes _cellSize;
        int _radius;
        int _radiusMult;
        int _cellLayersMax;
        int _cellLayerOffset;
        float _centerPosYOffset;
        float _randomGridMembersMax;
        float _model_cellBoundsSizeMult;
        float _model_cellBoundsSizeMult_entrance;
        float _gridSizeXZ;
        float _gridSizeY;

        float _buildingNode_Size;
        int _buildingNode_gridSizeY;

        int _baseLayer;
        #endregion
        private Vector3[] cornerPoints;
        private Vector3[] sidePoints;
        private void RecalculateEdgePoints()
        {
            cornerPoints = HexCoreUtil.GenerateHexagonPoints(transform.position, radius);
            sidePoints = HexagonGenerator.GenerateHexagonSidePoints(cornerPoints);
        }
        public Transform folder_Main { get; private set; } = null;
        public Transform folder_MeshObject { get; private set; } = null;
        public Transform folder_GeneratedTiles { get; private set; } = null;
        public Transform[] _testPTs;

        #region Core / Init
        public void InitialSetup() { }
        private bool _shouldUpdate;
        private void OnValidate()
        {
            if (
                _gridSizeXZ != gridSizeXZ ||
                _gridSizeY != gridSizeY
            )
            {
                gridSizeXZ = gridSizeXZ % 2 == 0 ? gridSizeXZ : (int)UtilityHelpers.RoundToNearestStep(gridSizeXZ, 2f);
                _gridSizeXZ = gridSizeXZ;

                gridSizeY = gridSizeY % 2 == 0 ? gridSizeY : (int)UtilityHelpers.RoundToNearestStep(gridSizeY, 2f);
                _gridSizeY = gridSizeY;

                resetPrototypes = true;
            }

            if (
                generate_meshTemplate ||
                cellLookup_ByLayer_BySize == null || cellLookup_ByLayer_BySize.Count == 0
                || resetPrototypes == true
                || _lastPosition != transform.position
                || _cellSize != cellSize
                || _cellLayersMax != cellLayersMax
                || _cellLayerOffset != cellLayerOffset
                || _centerPosYOffset != centerPosYOffset
                || _randomGridMembersMax != randomGridMembersMax
                || _model_cellBoundsSizeMult != model_cellBoundsSizeMult
                || _model_cellBoundsSizeMult_entrance != model_cellBoundsSizeMult_entrance
                || cornerPoints == null || sidePoints == null
                || _radius != radius
                || _radiusMult != radiusMult
                || _buildingNode_Size != buildingNode_Size
                || _buildingNode_gridSizeY != buildingNode_gridSizeY
                )
            {
                resetPrototypes = false;

                _lastPosition = transform.position;
                _cellSize = cellSize;
                _cellLayersMax = cellLayersMax;
                _cellLayerOffset = cellLayerOffset;

                _centerPosYOffset = centerPosYOffset;

                _randomGridMembersMax = randomGridMembersMax;

                _buildingNode_Size = buildingNode_Size;
                _buildingNode_gridSizeY = buildingNode_gridSizeY;

                model_cellBoundsSizeMult = UtilityHelpers.RoundToNearestStep(model_cellBoundsSizeMult, 0.1f);
                _model_cellBoundsSizeMult = model_cellBoundsSizeMult;

                model_cellBoundsSizeMult_entrance = UtilityHelpers.RoundToNearestStep(model_cellBoundsSizeMult_entrance, 0.1f);
                _model_cellBoundsSizeMult_entrance = model_cellBoundsSizeMult_entrance;

                int totalBufferRadius = radiusMult == 0 ? 12 : HexCellUtil.CalculateExpandedHexRadius((int)_cellSize, radiusMult);
                radius = totalBufferRadius;

                _radiusMult = radiusMult;
                _radius = radius;

                _shouldUpdate = true;
            }

            if (_shouldUpdate)
            {
                _shouldUpdate = false;
                // Debug.LogError("Update!");
                RecalculateEdgePoints();
                Generate();
            }

            if (allBaseCells != null)
            {
                int count = allBaseCells.Count;
                if (_highlightedCell == count) _highlightedCell = 0;
                _highlightedCell = Mathf.Clamp(_highlightedCell, 0, count - 1);
            }
            else _highlightedCell = -1;


            if (generate_surfaceMeshTemplate || generate_tiles)
            {
                generate_surfaceMeshTemplate = false;

                Evalaute_Folder();

                // if (surfaceBlocksGrid != null) SurfaceBlock.Generate_MeshObjects(surfaceBlocksGrid, prefab, transform, null, folder_MeshObject);
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

            if (execute_WFC)
            {
                execute_WFC = false;
                Execute_WFC();
            }
        }

        private void Awake() => InitialSetup();

        private void Start()
        {
            InitialSetup();
        }

        #endregion


        public void ResetPrototypes()
        {
            resetPrototypes = true;
            OnValidate();
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
                // folder_GeneratedTiles.transform.position = Vector3.zero;
                // folder_GeneratedTiles.transform.localPosition = Vector3.zero;
            }
            if (folder_Main == null)
            {
                folder_Main = new GameObject("WFC Tile" + this.gameObject.name).transform;
                folder_Main.transform.SetParent(this.transform);
            }
        }

        public bool Execute_WFC()
        {
            if (tileDirectory == null)
            {
                Debug.LogError("tileDirectory is invalid or unset!");
                return false;
            }
            if (locationPrefabOption.locationType == LocationType.Unset)
            {
                Debug.LogError("locationPrefabOption is invalid or unset!");
                return false;
            }

            Evalaute_Folder();

            if (generatedTiles != null && generatedTiles.Count > 0)
            {
                WFC_Template wfc = new WFC_Template(
                        generatedTiles,
                        HexCellUtil.OrganizeByLayer(cellLookup_ByLayer_BySize[(int)cellSize]),
                        tileDirectory.GetSocketDirectory(),
                        locationPrefabOption,
                        folder_Main,
                        allowRotations
                    );
                wfc.ExecuteWFC();
            }
            else
            {
                WFC_Core wfc = new WFC_Core(
                        tileDirectory,
                        HexCellUtil.OrganizeByLayer(cellLookup_ByLayer_BySize[(int)cellSize]),
                        locationPrefabOption,
                        folder_Main
                    );
                wfc.ExecuteWFC();
            }
            return true;
        }

        public void Generate()
        {
            int attempts = buildReattempts;
            bool failed = false;
            do
            {
                int baseLayer = Generate_Grid();
                failed = !Generate_Structure(baseLayer);

                if (failed) attempts--;
            } while (attempts > 0 && failed);
        }

        public int Generate_Grid()
        {
            Evalaute_Folder();

            // Vector3 gridCenterPos = transform.position;
            gridCenterPos = HexCoreUtil.Calculate_ClosestHexCenter_V2(transform.position, (int)HexCellSizes.X_36);
            gridCenterPos.y += centerPosYOffset;

            int baseLayer = HexCoreUtil.Calculate_CellSnapLayer(cellLayerOffset, gridCenterPos.y);
            _baseLayer = baseLayer;

            HashSet<string> neighborIDsToEvaluate = new HashSet<string>();
            Dictionary<int, List<HexagonCellPrototype>> neighborsToEvaluate_bySize = new Dictionary<int, List<HexagonCellPrototype>>();

            Dictionary<int, Dictionary<Vector2, Vector3>> new_cellCenters_ByLookup_BySize = null;

            if (cellGridType == Option_CellGridType.RandomConsecutive)
            {
                int _hostCellSize = (int)cellSize * 3;
                // HexCellSizes hostCellSize = (HexCellSizes)_hostCellSize;

                Dictionary<Vector2, Vector3> hostPoints = HexGridPathingUtil.GetConsecutiveCellPoints(
                    gridCenterPos,
                    consecutiveExpansionGridHostMembersMax,
                    _hostCellSize,
                    radius,
                    HexNeighborExpansionSize.Default,
                    HexNeighborExpansionSize.Default
                );

                Dictionary<Vector2, Vector3> new_cellCenters_ByLookup = HexGridUtil.Generate_HexGridCenterPointsWithinHosts(hostPoints.Values.ToList(), (int)cellSize, _hostCellSize, false);

                new_cellCenters_ByLookup_BySize = new Dictionary<int, Dictionary<Vector2, Vector3>>() {
                    { (int)cellSize, new_cellCenters_ByLookup },
                    // { _hostCellSize, hostPoints }
                };

                // new_cellCenters_ByLookup_BySize = HexGridUtil.Generate_RandomHexGridCenterPoints_BySize(gridCenterPos, cellSize, radius, randomGridMembersMax, randomGridExpansion);
                // Debug.Log("new_cellCenters_ByLookup_BySize " + new_cellCenters_ByLookup_BySize[(int)cellSize].Count);
            }
            else if (cellGridType == Option_CellGridType.ConsecutiveHost)
            {
                new_cellCenters_ByLookup_BySize = HexGridUtil.Generate_RandomHexGridCenterPoints_BySize(
                    gridCenterPos,
                    (int)cellSize,
                    new Vector2Int(consecutiveExpansionGridHostMembersMax, consecutiveExpansionGridHostMembersMax),
                    radius,
                    consecutiveExpansionShuffle
                );
            }
            else
            {
                new_cellCenters_ByLookup_BySize = radiusMult < 1 ?
                HexGridUtil.Generate_HexGridCenterPoints_X7(gridCenterPos, (int)cellSize) :

                HexGridUtil.Generate_HexGridCenterPoints_BySize(
                    gridCenterPos,
                    (int)HexCellSizes.X_4,
                    radius
                );
            }

            Vector3[] radiusCorners = HexCoreUtil.GenerateHexagonPoints(gridCenterPos, radius);
            int created = 0;


            Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>> new_cellLookup_ByLayer_BySize = new Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>();

            foreach (int currentSize in new_cellCenters_ByLookup_BySize.Keys)
            {
                if (currentSize >= radius) continue;
                if (currentSize > (int)HexCellSizes.Default) continue;

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

                        bool addChildren = cellSize > HexCellSizes.X_4;

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

                        if (cellLayersMax < 2 || (currentSize != (int)HexCellSizes.X_12 && currentSize != (int)HexCellSizes.X_4)) continue;

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

                                // if (nextLayerChildCells[0].layer != nextLayerCell.layer || currentLayer != nextLayerCell.layer) Debug.LogError("startingLayer: " + startingLayer + ", currentLayer: " + currentLayer + ", parent/child layer mismatch detected - parentlayer: " + nextLayerCell.layer + ", this cell's layer: " + nextLayerChildCells[0].layer + ", cellLayerOffset: " + cellLayerOffset);
                            }
                        }

                        // Add Underground Layers
                        // if (useUnderGround)
                        // {
                        //     previousCell = newCell;
                        //     previousChildCells = childCells;
                        //     startingLayer = (currentGroundLayer - 1);

                        //     for (int currentLayer = startingLayer; currentLayer > bottomLayer - 1; currentLayer--)
                        //     {
                        //         HexagonCellPrototype nextLayerCell = HexagonCellPrototype.DuplicateCellToNewLayer_Below(previousCell, cellLayerOffset, null);

                        //         if (new_cellLookup_ByLayer_BySize[currentSize].ContainsKey(currentLayer) == false)
                        //         {
                        //             new_cellLookup_ByLayer_BySize[currentSize].Add(currentLayer, new Dictionary<Vector2, HexagonCellPrototype>());
                        //         }
                        //         else if (new_cellLookup_ByLayer_BySize[currentSize][currentLayer].ContainsKey(nextLayerCell.GetLookup())) continue;

                        //         if (groundTypeFound == CellStatus.FlatGround || (allowBufferStatusAssignment && (startingLayer - currentLayer) > 1))
                        //         {
                        //             nextLayerCell.SetCellStatus(CellStatus.UnderGround);
                        //         }

                        //         new_cellLookup_ByLayer_BySize[currentSize][currentLayer].Add(nextLayerCell.GetLookup(), nextLayerCell);

                        //         if (neighborIDsToEvaluate.Contains(nextLayerCell.Get_Uid()) == false)
                        //         {
                        //             neighborIDsToEvaluate.Add(nextLayerCell.Get_Uid());
                        //             neighborsToEvaluate_bySize[currentSize].Add(nextLayerCell);
                        //         }

                        //         previousCell = nextLayerCell;


                        //         // Duplicate Children Cells to Next Layer
                        //         if (previousChildCells != null)
                        //         // if (groundTypeFound == CellStatus.FlatGround && previousChildCells != null)
                        //         {
                        //             if (new_cellLookup_ByLayer_BySize[childSize].ContainsKey(currentLayer) == false) new_cellLookup_ByLayer_BySize[childSize].Add(currentLayer, new Dictionary<Vector2, HexagonCellPrototype>());

                        //             List<HexagonCellPrototype> nextLayerChildCells = new List<HexagonCellPrototype>();
                        //             foreach (HexagonCellPrototype prevChild in previousChildCells)
                        //             {
                        //                 if (new_cellLookup_ByLayer_BySize[childSize][currentLayer].ContainsKey(prevChild.GetLookup())) continue;

                        //                 HexagonCellPrototype newChildCell = HexagonCellPrototype.DuplicateCellToNewLayer_Below(prevChild, cellLayerOffset, nextLayerCell);

                        //                 new_cellLookup_ByLayer_BySize[childSize][currentLayer].Add(newChildCell.GetLookup(), newChildCell);

                        //                 nextLayerChildCells.Add(newChildCell);

                        //                 if (neighborIDsToEvaluate.Contains(newChildCell.Get_Uid()) == false)
                        //                 {
                        //                     neighborIDsToEvaluate.Add(newChildCell.Get_Uid());
                        //                     neighborsToEvaluate_bySize[childSize].Add(newChildCell);
                        //                 }
                        //             }

                        //             previousChildCells = nextLayerChildCells;
                        //         }
                        //     }
                        // }

                        created++;
                    }
                }
            }
            // Debug.Log("Created " + created + " center points within WorldSpace coordinate: " + worldspaceLookup);
            // totalCreated += created;

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

            return baseLayer;
            // int attempts = 3;
            // bool failed = false;
            // do
            // {
            //     failed = !Generate_Structure(baseLayer);

            //     if (failed) attempts--;
            // } while (attempts > 0 && failed);
        }




        private bool Generate_Structure(int baseLayer)
        {
            List<HexagonCellPrototype> new_baseEdges = new List<HexagonCellPrototype>();
            List<HexagonCellPrototype> new_allBaseCells = new List<HexagonCellPrototype>();

            buildingNodeGrid = null;


            entranceCell = null;
            structureBounds = new List<Bounds>();

            foreach (var key in cellLookup_ByLayer_BySize[(int)cellSize][baseLayer].Keys)
            {
                HexagonCellPrototype cell = cellLookup_ByLayer_BySize[(int)cellSize][baseLayer][key];
                new_allBaseCells.Add(cell);

                if (cell.IsEdge())
                {
                    new_baseEdges.Add(cell);
                }
                else
                {
                    Vector3[] boundsCorners = HexCoreUtil.GenerateHexagonPoints(cell.center, cell.size * model_cellBoundsSizeMult);
                    Bounds bounds = VectorUtil.CalculateBounds_V2(boundsCorners.ToList());
                    structureBounds.Add(bounds);
                }
            }

            Dictionary<Vector2, List<Vector3>> boundsEdgePointsByCellLookup = new Dictionary<Vector2, List<Vector3>>();
            List<Vector3> new_boundsEdgePoints = new List<Vector3>();
            for (int i = 0; i < structureBounds.Count; i++)
            {
                Vector3[] boundsCorners = VectorUtil.GetBoundsCorners_X8(structureBounds[i]);
                // List<Vector3> boundsCorners = VectorUtil.GetBoundsDottedEdge(structureBounds[i], boundEdgeLineDensity);
                List<Vector3> toAdd = new List<Vector3>();

                foreach (var point in boundsCorners)
                {
                    bool skip = false;
                    for (int j = 0; j < structureBounds.Count; j++)
                    {
                        if (i == j) continue;
                        Bounds otherBounds = structureBounds[j];
                        if (VectorUtil.IsPointWithinBoundsExcludingBorder(otherBounds, point))
                        // if (VectorUtil.IsPointWithinBounds(otherBounds, point))
                        {
                            skip = true;
                            break;
                        }
                    }

                    if (skip == false)
                    {
                        toAdd.Add(point);
                        // new_boundsEdgePoints.Add(point);

                        foreach (var cell in new_baseEdges)
                        {
                            if (VectorUtil.IsPointWithinPolygon(point, cell.cornerPoints) && VectorUtil.DistanceXZ(point, cell.center) < cell.size * 1.01f)
                            {
                                Vector2 lookup = cell.GetLookup();
                                if (boundsEdgePointsByCellLookup.ContainsKey(lookup) == false) boundsEdgePointsByCellLookup.Add(cell.GetLookup(), new List<Vector3>());

                                boundsEdgePointsByCellLookup[lookup].Add(point);
                                break;
                            }
                        }
                    }

                }
                new_boundsEdgePoints.AddRange(toAdd);
            }

            // VectorUtil.SortPointsByDistance(new_boundsEdgePoints);
            // VectorUtil.SortPointsForNonOverlappingBorder(new_boundsEdgePoints);
            // new_boundsEdgePoints = VectorUtil.FilterPointsByDistance(new_boundsEdgePoints, boundEdgePointdistanceThreshold);

            List<Vector3> temp = new List<Vector3>();

            bool failed = false;
            nodes = Node.AssignClosestNeighbors(new_boundsEdgePoints);
            temp = Node.GetOrderedPoints(nodes);
            failed = nodes.Count != temp.Count;

            if (failed)
            {
                // Debug.Log("nodes: " + nodes.Count + ", new_boundsEdgePoints: " + temp.Count);
                return false;
            }

            boundsEdgePoints = temp;

            baseEdges = new_baseEdges;
            allBaseCells = new_allBaseCells;

            HashSet<Vector3> intersectonPoints = new HashSet<Vector3>();

            _currentHighlightedCell = null;

            foreach (var cell in baseEdges)
            {
                HashSet<Vector3> visited = new HashSet<Vector3>();

                foreach (var kvp in cell.borderPoints)
                {
                    Vector3 lookup = kvp.Key;
                    Vector3 point = kvp.Value;
                    if (nodes.ContainsKey(lookup) == false || visited.Contains(lookup)) continue;

                    Node node = nodes[lookup];
                    if (node.NeighborA.cellOwner == cell && visited.Contains(node.NeighborA.Lookup()) == false)
                    {
                        HexSideStructure new_hexSideStructure = new HexSideStructure(new Vector3[2] { node.Position, node.NeighborA.Position }, model_layersMax, model_layerOffeset);
                        cell.borderSurfaceStructures.Add(new_hexSideStructure);
                    }
                    if (node.NeighborB.cellOwner == cell && visited.Contains(node.NeighborB.Lookup()) == false)
                    {
                        HexSideStructure new_hexSideStructure = new HexSideStructure(new Vector3[2] { node.Position, node.NeighborB.Position }, model_layersMax, model_layerOffeset);
                        cell.borderSurfaceStructures.Add(new_hexSideStructure);
                    }

                    // visited.Add(lookup);
                }

                if (generate_meshTemplate) Create_MeshGameObject(cell, prefab);
            }



            gridStructureBounds = VectorUtil.CalculateBounds(structureBounds);

            surfaceBlocksGrid = null;


            generate_meshTemplate = false;
            return true;
        }


        Dictionary<string, Color> customColors = UtilityHelpers.CustomColorDefaults();
        private void OnDrawGizmos()
        {
            if (_lastPosition != transform.position)
            {
                // resetPrototypes = true;
                ResetPrototypes();
            }

            if (showGridBounds)
            {
                Gizmos.color = Color.magenta;
                for (int j = 0; j < cornerPoints.Length; j++)
                {
                    Gizmos.DrawSphere(cornerPoints[j], 1);
                }
                VectorUtil.DrawHexagonPointLinesInGizmos(cornerPoints);
            }

            if (showTotalStructureBounds && structureBounds != null)
            {
                Gizmos.color = Color.yellow;

                Bounds totalBounds = VectorUtil.CalculateBounds(structureBounds);
                VectorUtil.DrawRectangleLines(totalBounds);

                gridStructureBounds = totalBounds;

                if (buildStructureType != BuildStructureType.Building)
                {

                    if (buildingNodeGrid == null)
                    {
                        (Vector3[,,] points, float spacing
                        ) = VectorUtil.Generate3DGrid(totalBounds, buildingNode_Size, transform.position.y, ((cellLayersMax - 1) * cellLayerOffset));
                        buildingNodeGrid = BuildingNode.CreateSurfaceBlocks(points, structureBounds, spacing);


                        // List<Vector3> dottedPathPoints = HexCoreUtil.Generate_DottedCellEdgedPathPoints(, (int)HexCellSizes.X_36, 6)
                        // Dictionary<Vector2, Vector3> pathCenters = HexCoreUtil.Generate_CellEdgedPathCellsCenters(transform.position, 4, cellPathing_hostSizeMin, 6);
                        (
                            Dictionary<int, Dictionary<Vector2, Vector3>> new_buildingBlockClusters,
                            Dictionary<Vector2, Vector3> pathCenters
                        ) = HexCoreUtil.Generate_CellCityClusterCenters(transform.position, 4, 36, 99, 10);
                        // ) = HexCoreUtil.Generate_CellCityClusterCenters(transform.position, 4, cellPathing_hostSizeMin, 99, 6);

                        List<HexagonCellPrototype> new_baseCellsInStructureBounds = new List<HexagonCellPrototype>();
                        Dictionary<int, List<HexagonCellPrototype>> new_baseCellClustersList = new Dictionary<int, List<HexagonCellPrototype>>();
                        pathCells = new List<HexagonCellPrototype>();

                        buildingBlockClusters = new_buildingBlockClusters;


                        foreach (var key in cellLookup_ByLayer_BySize[(int)cellSize][_baseLayer].Keys)
                        {
                            HexagonCellPrototype cell = cellLookup_ByLayer_BySize[(int)cellSize][_baseLayer][key];

                            if (VectorUtil.IsPointWithinBounds(structureBounds, cell.center))
                            {
                                new_baseCellsInStructureBounds.Add(cell);

                                if (pathCenters.ContainsKey(key))
                                {
                                    cell.SetPathCell(true);
                                    pathCells.Add(cell);
                                    // cellLookup_ByLayer_BySize[(int)cellSize][_baseLayer][key].SetPathCell(true);
                                }
                                else
                                {

                                    foreach (int ix in buildingBlockClusters.Keys)
                                    {
                                        var group = buildingBlockClusters[ix];
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
                        baseCellsInStructureBounds = new_baseCellsInStructureBounds;

                        if (baseCellClustersList != null)
                        {

                            hexTileBuilders = new List<HexTileBuilder>();

                            foreach (var ix in baseCellClustersList.Keys)
                            {

                                List<Bounds> clusterStructureBounds = new List<Bounds>();
                                List<HexagonCellPrototype> clusterbaseEdges = new List<HexagonCellPrototype>();

                                foreach (var cell in baseCellClustersList[ix])
                                {
                                    bool isClusterEdge = cell.IsEdge();

                                    if (!isClusterEdge)
                                    {
                                        Vector2[] neighborLookups = HexCoreUtil.Generate_NeighborLookups_X6(cell.center, cell.size);
                                        foreach (var item in neighborLookups)
                                        {
                                            if (buildingBlockClusters[ix].ContainsKey(item) == false)
                                            {
                                                isClusterEdge = true;
                                                break;
                                            }
                                        }
                                    }

                                    if (isClusterEdge)
                                    {
                                        clusterbaseEdges.Add(cell);
                                    }
                                    else
                                    {
                                        Vector3[] boundsCorners = HexCoreUtil.GenerateHexagonPoints(cell.center, cell.size * model_cellBoundsSizeMult);
                                        Bounds bounds = VectorUtil.CalculateBounds_V2(boundsCorners.ToList());
                                        clusterStructureBounds.Add(bounds);
                                    }
                                }

                                HexTileBuilder hexTileBuilder = new HexTileBuilder();

                                hexTileBuilder.Generate_Structure(
                                    VectorUtil.CalculateBounds(clusterStructureBounds),
                                    clusterStructureBounds,
                                    blockSize,
                                    transform.position.y,
                                    cellLookup_ByLayer_BySize,
                                    baseCellClustersList[ix],
                                    clusterbaseEdges,
                                    (int)cellSize,
                                    cellLayerOffset,
                                    cellLayersMax,
                                    blockGrid_cluster_LWH_Min,
                                    blockGrid_clustersMax
                                );

                                hexTileBuilders.Add(hexTileBuilder);
                                // break;
                            }
                        }


                        // List<SurfaceBlockState> filterOnStates = new List<SurfaceBlockState>() {
                        //     SurfaceBlockState.Entry,
                        //     // SurfaceBlockState.Corner,
                        // };

                        // buildingNodeClusters = BuildingNode.GetConsecutiveClusters(
                        //     buildingNodeGrid,
                        //     99,
                        //     new Vector2Int(buildingNode_clusterSizeMin, buildingNode_clusterSizeMax),
                        //     null,
                        //     blockGrid_cluster_LWH_Min,
                        //     CellSearchPriority.SideNeighbors
                        // );

                    }
                    else
                    {

                        // int pathCellSize = 4;
                        // Dictionary<Vector3, Vector3> pathCenters = HexCoreUtil.Generate_CellEdgedPathCellsCenters(gridCenterPos, 4, 36, 6);
                        // foreach (var item in pathCenters.Values)
                        // {
                        //     Gizmos.color = Color.green;
                        //     VectorUtil.DrawHexagonPointLinesInGizmos(item, pathCellSize);
                        //     Gizmos.DrawSphere(item, pathCellSize / 3);
                        // }

                        // if (showSurfaceBlockGrid)
                        // {
                        // BuildingNode.DrawGrid(buildingNodeGrid);
                        // }

                        // if (buildingBlockClusters != null)
                        // {
                        //     for (int i = 0; i < buildingBlockClusters.Count; i++)
                        //     {
                        //         var kvp = buildingBlockClusters[i];

                        //         foreach (var item in kvp.Values)
                        //         {
                        //             Gizmos.color = Color.black;
                        //             // VectorUtil.DrawHexagonPointLinesInGizmos(item, pathCellSize);
                        //             Gizmos.DrawSphere(item, 4 / 2);
                        //         }
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
                        if (pathCells != null)
                        {
                            foreach (var cell in pathCells)
                            {
                                Gizmos.color = Color.green;
                                Gizmos.DrawSphere(cell.center, cell.size / 2);
                            }
                        }

                        if (showBuildingStructures && hexTileBuilders != null)
                        {
                            foreach (var item in hexTileBuilders)
                            {
                                item.DrawGrid();
                            }
                        }
                        // foreach (var cell in baseCellsInStructureBounds)
                        // {
                        //     if (cell.IsPath())
                        //     {
                        //         Gizmos.color = Color.green;
                        //         Gizmos.DrawSphere(cell.center, cell.size / 2);
                        //     }
                        // }

                        // if (buildingNodeClusters != null && buildingNodeClusters.Count > 0)
                        // {
                        //     foreach (var cluster in buildingNodeClusters)
                        //     {
                        //         foreach (var item in cluster)
                        //         {
                        //             bool skip = false;

                        //             foreach (var cell in baseCellsInStructureBounds)
                        //             {

                        //                 if (cell.IsPath() && VectorUtil.AnyPointsWithinPolygon(cell.cornerPoints.ToList(), item.corners.Take(4).ToArray()))
                        //                 {
                        //                     skip = true;
                        //                     break;
                        //                 }
                        //                 // if (VectorUtil.IsPointWithinBounds(cell.cornerPoints, currentSize, radiusCorners)) {
                        //                 // }
                        //             }
                        //             if (!skip)
                        //             {
                        //                 Gizmos.color = Color.black;
                        //                 Gizmos.DrawSphere(item.Position, buildingNode_Size / 2);
                        //             }
                        //         }
                        //     }
                        // }
                    }
                }

            }

            if (showInnerStructureBounds && structureBounds != null)
            {
                Gizmos.color = Color.white;
                foreach (var item in structureBounds)
                {
                    VectorUtil.DrawRectangleLines(item);
                }

                Gizmos.color = Color.red;
                List<Bounds> filtered = VectorUtil.FilterInterlockingBounds(structureBounds);
                foreach (var item in filtered)
                {
                    VectorUtil.DrawRectangleLines(item);
                }

                if (entranceCell != null && entranceBounds != null)
                {
                    Gizmos.color = Color.magenta;
                    List<HexagonSide> neighborSides = entranceCell.GetNeighborSides(Filter_CellType.NoEdge);
                    Gizmos.DrawSphere(entranceCell.sidePoints[(int)neighborSides[0]], entranceCell.size * 0.6f);
                    // Vector3[] boundsCorners = VectorUtil.GetBoundsCorners_X8(entranceBounds);
                    // // List<Vector3> boundsCorners = VectorUtil.GetBoundsDottedEdge(entranceBounds, boundEdgeLineDensity);
                    // foreach (var item in boundsCorners)
                    // {
                    //     Gizmos.DrawSphere(item, 0.55f);
                    // }
                    // VectorUtil.DrawRectangleLines(entranceBounds);
                }
            }

            if (showNodes && nodes != null)
            {
                foreach (var node in nodes.Values)
                {
                    node.DrawLinesAndPoints();
                }
            }


            if (showSurfaceBlocks)
            {
                if (gridStructureBounds != null)
                {
                    Gizmos.color = Color.white;
                    VectorUtil.DrawRectangleLines(gridStructureBounds);
                    // Vector3[,,] points = VectorUtil.Generate3DGrid(gridStructureBounds, 10, 5, 10, transform.position.y);

                    if (surfaceBlocksGrid == null)
                    {
                        (Vector3[,,] points, float spacing) = VectorUtil.Generate3DGrid(gridStructureBounds, blockSize, transform.position.y, ((cellLayersMax - 1) * cellLayerOffset));
                        // (Vector3[,,] points, float spacing) = VectorUtil.Generate3DGrid(gridStructureBounds, baseCellSize, gridSizeY, transform.position.y);
                        // (Vector3[,,] points, float spacing) = VectorUtil.Generate3DGrid(gridStructureBounds, gridSizeXZ, gridSizeY, transform.position.y);
                        surfaceBlocksGrid = SurfaceBlock.CreateSurfaceBlocks(points, structureBounds, spacing);
                        // surfaceBlocksGrid = SurfaceBlock.ClearInnerBlocks(surfaceBlocksGrid);

                        SurfaceBlock.GetViableEntryways(surfaceBlocksGrid, baseEdges, 3);

                        surfaceBlocksByCell = SurfaceBlock.GetSurfaceBlocksByCell(surfaceBlocksGrid, cellLookup_ByLayer_BySize[(int)cellSize], cellLayerOffset);
                        surfaceBlocksGrid = SurfaceBlock.ClearInnerBlocks(surfaceBlocksGrid);

                        SurfaceBlock.EvaluateTileEdges(surfaceBlocksGrid);

                        List<SurfaceBlockState> filterOnStates = new List<SurfaceBlockState>() {
                            SurfaceBlockState.Entry,
                            // SurfaceBlockState.Corner,
                        };

                        surfaceBlockClusters = SurfaceBlock.GetConsecutiveClusters(
                            surfaceBlocksGrid,
                            blockGrid_clustersMax,
                            filterOnStates,
                            blockGrid_cluster_LWH_Min,
                            CellSearchPriority.SideNeighbors
                        );

                        if (surfaceBlockClusters != null && surfaceBlockClusters.Count > 0)
                        {
                            foreach (var cluster in surfaceBlockClusters)
                            {
                                foreach (var item in cluster)
                                {
                                    item.SetIgnored(true);
                                }
                            }
                        }
                        tileInnerEdgesByCellSide = SurfaceBlock.GetTileInnerEdgesByCellSide(surfaceBlocksByCell);

                        Dictionary<HexagonCellPrototype, Dictionary<HexagonTileSide, TileSocketProfile>> cellTileSocketProfiles =
                            SurfaceBlock.Generate_CellTileSocketProfiles(tileInnerEdgesByCellSide);

                        // intersectionPointsBySideByCell = SurfaceBlock.GetIntersectionPointsBySideByCell(surfaceBlocksByCell);
                    }
                    else
                    {
                        if (showSurfaceBlockGrid)
                        {
                            SurfaceBlock.DrawGrid(surfaceBlocksGrid, showHighlightedCell ? _currentHighlightedCell : null);
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

                            // Gizmos.color = Color.magenta;

                            // foreach (var cell in surfaceBlocksByCell.Keys)
                            // {
                            //     if (showHighlightedCell == false || (showHighlightedCell && cell == _currentHighlightedCell))
                            //     {
                            //         if (cell.tileSocketProfileBySide == null) continue;

                            //         foreach (var socketProfile in cell.tileSocketProfileBySide.Values)
                            //         {
                            //             if (socketProfile != null && socketProfile.sockets != null)
                            //             {
                            //                 foreach (var point in socketProfile.sockets.Keys)
                            //                 {
                            //                     Gizmos.DrawSphere(point, 0.1f);
                            //                 }
                            //             }
                            //         }
                            //     }
                            // }
                        }

                        // if (surfaceBlockClusters != null && surfaceBlockClusters.Count > 0)
                        // {
                        //     Gizmos.color = Color.red;
                        //     foreach (var cluster in surfaceBlockClusters)
                        //     {
                        //         foreach (var item in cluster)
                        //         {
                        //             Gizmos.DrawSphere(item.Position, 0.33f);
                        //         }
                        //     }
                        // }

                        // if (showSurfaceBlockEdgeSockets && intersectionPointsBySideByCell != null)
                        // {
                        //     Gizmos.color = Color.magenta;
                        //     foreach (var kvp in intersectionPointsBySideByCell.Values)
                        //     {
                        //         foreach (var points in kvp.Values)
                        //         {
                        //             foreach (var point in points)
                        //             {
                        //                 Gizmos.DrawSphere(point, 0.1f);
                        //             }
                        //         }
                        //     }
                        // }
                    }
                }
            }

            // if (showRawBoundsEdge && boundsEdgePoints != null)
            // {
            //     Gizmos.color = Color.cyan;
            //     foreach (var item in boundsEdgePoints)
            //     {
            //         Gizmos.DrawSphere(item, 0.4f);

            //         // Gizmos.color = Color.white;
            //         // Vector3[] cubePTs = VectorUtil.CreateCube(item, 1f);
            //         // foreach (var pt in cubePTs)
            //         // {
            //         //     Gizmos.DrawSphere(pt, 0.2f);
            //         // }
            //     }
            //     VectorUtil.DrawPointLinesInGizmos(boundsEdgePoints);
            // }


            if (showCellGrids)
            {
                if (cellLookup_ByLayer_BySize != null && cellLookup_ByLayer_BySize.Count > 0)
                {
                    foreach (var kvp in cellLookup_ByLayer_BySize)
                    {
                        int currentSize = kvp.Key;
                        if (currentSize != (int)gridFilter_size) continue;

                        HexagonCellPrototype.DrawHexagonCellPrototypeGrid(
                            cellLookup_ByLayer_BySize[currentSize],
                            gridFilter_Type,
                            GridFilter_Level.HostCells,
                            cellDisplayType,
                            false,
                            showHighlights
                        );
                    }
                }
            }

            if (showEdgeLines)
            {
                if (allBaseCells == null || allBaseCells.Count == 0)
                {
                    showEdgeLines = false;
                }
                else
                {
                    List<Vector3> edgeSocketPoints = new List<Vector3>();

                    for (var i = 0; i < allBaseCells.Count; i++)
                    {
                        HexagonCellPrototype currentCell = allBaseCells[i];

                        if (showHighlightedCell && _highlightedCell == i)
                        {
                            if (_currentHighlightedCell != currentCell) _currentHighlightedCell = currentCell;

                            Gizmos.color = customColors["purple"];
                            Gizmos.DrawWireSphere(currentCell.center, 1f);

                            if (currentCell.borderPoints != null)
                            {
                                Gizmos.color = customColors["orange"];
                                foreach (var item in currentCell.borderPoints.Values)
                                {
                                    Gizmos.DrawSphere(item, 0.4f);
                                }
                            }
                        }
                        else
                        {
                            // if (edge.isEntryCell)
                            // {
                            //     Gizmos.color = Color.magenta;
                            //     Gizmos.DrawWireSphere(edge.center, 0.5f);
                            // }
                            // else
                            // {
                            //     Gizmos.color = Color.red;
                            //     Gizmos.DrawWireSphere(edge.center, 0.33f);
                            // }


                            // Gizmos.color = Color.black;
                            // foreach (var item in edge.borderSurfaceStructures)
                            // {
                            //     item.DrawSurfacePoints();
                            // }

                            // List<HexagonSide> neighborSides = edge.GetNeighborSides(Filter_CellType.Edge);

                            // foreach (HexagonSide side in neighborSides)
                            // {
                            //     int _side = (int)side;

                            //     int step = edgeCellConnectorStep[edge][side];
                            //     Vector3 sidePoint = Calculate_SocketEdgePoint(edge, step, side);
                            //     HexagonSide relativeSide = HexCoreUtil.GetRelativeHexagonSideOnSharedRotation(side);

                            //     HexagonCellPrototype sideNeighbor = edge.neighborsBySide[(int)side];
                            //     Vector3 sideNeighborPoint = Calculate_SocketEdgePoint(sideNeighbor, edgeCellConnectorStep[sideNeighbor][relativeSide], relativeSide);

                            //     edgeSocketPoints.Add(sidePoint);
                            // }

                            // List<HexagonSide> emptySides = edge.GetNeighborSides(Filter_CellType.NullValue);

                            // if (edge.isEntryCell)
                            // {
                            //     Dictionary<HexagonSide, Vector3[]> emptyPointsBySide = GenerateHexChildPointsOnSides(edge.center, edge.size, emptySides);
                            //     foreach (HexagonSide side in emptySides)
                            //     {
                            //         Vector3 sidePoint = emptyPointsBySide[side][0];
                            //         Gizmos.color = Color.blue;
                            //         Gizmos.DrawSphere(sidePoint, 0.34f);
                            //         // Gizmos.color = Color.yellow;
                            //         // edge.sideEdgeStructures[side].DrawSurfacePoints();
                            //     }

                            //     Vector3[] corners = HexCoreUtil.GetSideCorners(edge, entranceSide);
                            //     Gizmos.color = Color.magenta;
                            //     foreach (var item in corners)
                            //     {
                            //         Gizmos.DrawSphere(item, 0.24f);
                            //     }
                            //     List<Vector3> baseDoorwayPoints = Generate_DoorwayPoints(2, corners[0], corners[1]);

                            //     List<Vector3> allDoorwayPoints = VectorUtil.DuplicatePositionsToNewYPos(baseDoorwayPoints, model_layerOffeset, model_layersMax, true);
                            //     foreach (var item in allDoorwayPoints)
                            //     {
                            //         Gizmos.DrawSphere(item, 0.24f);
                            //     }


                            // }

                        }

                    }

                    // Gizmos.color = Color.red;
                    // VectorUtil.DrawPointLinesInGizmos(_points);

                    // List<Vector3> allPTS = VectorUtil.DuplicatePositionsToNewYPos(edgeSocketPoints, model_layerOffeset, model_layersMax);
                    // Gizmos.color = Color.black;
                    // foreach (var item in allPTS)
                    // {
                    //     Gizmos.DrawSphere(item, 0.24f);
                    // }
                }
            }

        }

        public static List<Bounds> GetStructureBoundsFromCellGrid(Dictionary<Vector2, HexagonCellPrototype> cellLookup, float cellBoundsSizeMult)
        {
            List<Bounds> structureBounds = new List<Bounds>();
            foreach (var key in cellLookup.Keys)
            {
                HexagonCellPrototype cell = cellLookup[key];
                if (cell.IsEdge() == false)
                {
                    Vector3[] boundsCorners = HexCoreUtil.GenerateHexagonPoints(cell.center, cell.size * cellBoundsSizeMult);
                    Bounds bounds = VectorUtil.CalculateBounds_V2(boundsCorners.ToList());
                    structureBounds.Add(bounds);
                }
            }
            return structureBounds;
        }

        public static int InvertSocketEdgeStep(int step)
        {
            if (step == 0) return 10;
            if (step == 10) return 0;
            int inv = Mathf.Abs((step - 10) % 10);
            // Debug.LogError("step: " + step + ", inverted: " + inv);
            return inv;
        }
        public static int Calculate_RandomSocketEdgeStep()
        {
            return UnityEngine.Random.Range(0, 11);
        }

        public static float Calculate_LerpStepMultOfPoint(Vector3 point, Vector3 cornerA, Vector3 cornerB)
        {
            float distance = Vector3.Distance(cornerA, cornerB); // Calculate the distance between cornerA and cornerB
            float pointDistance = Vector3.Distance(cornerA, point); // Calculate the distance between cornerA and the given point
            float lerpStep = pointDistance / distance; // Calculate the lerp step value

            float roundedLerpStep = Mathf.Round(lerpStep * 10f) / 10f; // Round the lerp step to the nearest 0.1f

            return Mathf.Clamp(roundedLerpStep, 0f, 1f); // Clamp the rounded lerp step value between 0 and 1
        }

        public static int Calculate_LerpStepOfPoint(Vector3 point, Vector3 cornerA, Vector3 cornerB)
        {
            float distance = Vector3.Distance(cornerA, cornerB); // Calculate the distance between cornerA and cornerB
            float pointDistance = Vector3.Distance(cornerA, point); // Calculate the distance between cornerA and the given point
            float lerpStep = pointDistance / distance; // Calculate the lerp step value

            int roundedLerpStep = Mathf.RoundToInt(lerpStep * 10f); // Round the lerp step to the nearest integer between 0 and 10

            return Mathf.Clamp(roundedLerpStep, 0, 10); // Clamp the rounded lerp step value between 0 and 10
        }


        public static Vector3 Calculate_SocketEdgePoint(int step, Vector3 cornerA, Vector3 cornerB)
        {
            return Vector3.Lerp(cornerA, cornerB, (step * 0.1f));
        }

        public static Vector3 Calculate_SocketEdgePoint(HexagonCellPrototype cell, int step, HexagonSide side)
        {
            Vector2Int cornerIX = HexCoreUtil.GetCornersFromSide_Default(side);
            return Vector3.Lerp(cell.cornerPoints[cornerIX.x], cell.cornerPoints[cornerIX.y], (step * 0.1f));
        }

        public static (Vector3, List<Vector3>) GetCellSideIntersectionPoint_WithLinePoints(HexagonCellPrototype cell, List<Vector3> points, Vector3 lineStart, Vector3 lineEnd)
        {
            bool intersectionFound = false;
            List<Vector3> insideCellBounds = new List<Vector3>();
            Vector3 intersectionPoint = Vector3.zero;

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 pointA = points[i];
                Vector3 pointB = points[(i + 1) % points.Count];

                if (intersectionFound == false)
                {
                    intersectionPoint = VectorUtil.FindIntersectionPoint(lineStart, lineEnd, pointA, pointB);
                    if (intersectionPoint != Vector3.zero &&
                        VectorUtil.IsPointOnLine(intersectionPoint, lineStart, lineEnd) &&
                        VectorUtil.IsPointOnLine(intersectionPoint, pointA, pointB) &&
                        VectorUtil.DistanceXZ(intersectionPoint, cell.center) < cell.size * 1.01f
                    )
                    {
                        intersectionFound = true;
                        insideCellBounds.Add(intersectionPoint);
                    }
                }

                if (VectorUtil.IsPointWithinPolygon(pointA, cell.cornerPoints) && VectorUtil.DistanceXZ(pointA, cell.center) < cell.size * 1.01f) insideCellBounds.Add(pointA);
            }
            return (intersectionPoint, insideCellBounds);
        }

        public static (Vector3, List<Vector3>) GetCellSideIntersectionPoint_WithLinePoints(HexagonCellPrototype cell, List<Node> nodes, Vector3 lineStart, Vector3 lineEnd)
        {
            bool intersectionFound = false;
            List<Vector3> insideCellBounds = new List<Vector3>();
            Vector3 intersectionPoint = Vector3.zero;

            for (var j = 0; j < nodes.Count; j++)
            {
                Node node = nodes[j];

                List<Node> members = new List<Node>() {
                    node.NeighborA,
                    node,
                    node.NeighborB
                };

                for (int i = 1; i < members.Count; i++)
                {
                    Vector3 pointA = members[i - 1].Position;
                    Vector3 pointB = members[i].Position;

                    if (intersectionFound == false)
                    {
                        intersectionPoint = VectorUtil.FindIntersectionPoint(lineStart, lineEnd, pointA, pointB);
                        if (intersectionPoint != Vector3.zero &&
                            VectorUtil.IsPointOnLine(intersectionPoint, lineStart, lineEnd) &&
                            VectorUtil.IsPointOnLine(intersectionPoint, pointA, pointB) &&
                            VectorUtil.DistanceXZ(intersectionPoint, cell.center) < cell.size * 1.01f
                        )
                        {
                            intersectionFound = true;

                            Node intersectionNode = new Node(intersectionPoint);
                            nodes.Add(intersectionNode);
                            intersectionNode.InjectBetweenNeighbors(members[i - 1], members[i]);
                        }
                    }

                    if (VectorUtil.IsPointWithinPolygon(pointA, cell.cornerPoints) && VectorUtil.DistanceXZ(pointA, cell.center) < cell.size * 1.01f) insideCellBounds.Add(pointA);
                }
            }

            return (intersectionPoint, insideCellBounds);
        }


        public static Dictionary<HexagonSide, Vector3[]> GenerateHexChildPointsOnSides(Vector3 center, int size, List<HexagonSide> sides)
        {
            Dictionary<HexagonSide, Vector3[]> availablePointsBySide = new Dictionary<HexagonSide, Vector3[]>();

            Vector3[] cornerPoints = HexCoreUtil.GenerateHexagonPoints(center, size / 3);
            Dictionary<int, Vector3> pointsBySide = new Dictionary<int, Vector3>();
            for (int i = 0; i < 6; i++)
            {
                Vector3 sidePoint = Vector3.Lerp(cornerPoints[i], cornerPoints[(i + 1) % 6], 0.5f);
                Vector3 direction = (sidePoint - center).normalized;
                float edgeDistance = Vector2.Distance(new Vector2(sidePoint.x, sidePoint.z), new Vector2(center.x, center.z));
                sidePoint = center + direction * (edgeDistance * 2f);
                pointsBySide.Add(((i + 5) % 6), sidePoint);
            }
            foreach (HexagonSide s in sides)
            {
                availablePointsBySide.Add(s, new Vector3[3]);
                availablePointsBySide[s][0] = pointsBySide[(int)s];
                availablePointsBySide[s][1] = pointsBySide[((int)s + 5) % 6];
                availablePointsBySide[s][2] = pointsBySide[((int)s + 1) % 6];
            }

            return availablePointsBySide;
        }

        public static List<Vector3> Generate_DoorwayPoints(int step, Vector3 cornerA, Vector3 cornerB)
        {
            float stepMult = (step * 0.1f);

            List<Vector3> allPoints = new List<Vector3>();

            Vector3[] outerCorners = new Vector3[2] {
                cornerA,
                cornerB
            };
            // outerCorners[0] = cornerA;
            // outerCorners[1] = cornerA;

            Vector3[] innerCorners = new Vector3[2];
            innerCorners[0] = Vector3.Lerp(cornerA, cornerB, stepMult);
            innerCorners[1] = Vector3.Lerp(cornerB, cornerA, stepMult);

            allPoints.AddRange(outerCorners);
            allPoints.AddRange(innerCorners);

            return allPoints;
        }

        public static Vector3[] Generate_RandomSurfaceCornerPoints(int step, Vector3 cornerA, Vector3 cornerB)
        {
            float stepMult = (step * 0.1f);
            Vector3[] surfaceEdgeCorners = new Vector3[2];
            surfaceEdgeCorners[0] = Vector3.Lerp(cornerA, cornerB, stepMult);
            surfaceEdgeCorners[1] = Vector3.Lerp(cornerB, cornerA, stepMult);

            return surfaceEdgeCorners;
        }
        public static Vector3[] Generate_RandomSurfaceCornerPoints(Vector3 hexCenter, int cornerStep, int centerStep, Vector3 cornerA, Vector3 cornerB)
        {
            float stepMultCenter = (centerStep * 0.1f);
            float stepMultCorner = (cornerStep * 0.1f);

            Vector3 cornerA_Mod = Vector3.Lerp(cornerA, hexCenter, stepMultCenter);
            Vector3 cornerB_Mod = Vector3.Lerp(cornerB, hexCenter, stepMultCenter);

            Vector3[] surfaceEdgeCorners = new Vector3[2];
            surfaceEdgeCorners[0] = Vector3.Lerp(cornerA_Mod, cornerB_Mod, stepMultCorner);
            surfaceEdgeCorners[1] = Vector3.Lerp(cornerB_Mod, cornerA_Mod, stepMultCorner);
            return surfaceEdgeCorners;
        }

        public static void Initialize_CellBorderSurfacesAndSideSockets(HexagonCellPrototype cell, Dictionary<Vector3, Node> nodes, int layersMax, float layerOffeset, HashSet<Vector3> intersectonPoints)
        {
            // bool isEntry = cell.IsEntry();
            // HexSideStructure widestSurface = null;
            // float widestSurfaceDist = float.MinValue;
            List<HexagonSide> neighborSides = cell.GetNeighborSides(Filter_CellType.Edge);
            HashSet<Node> addedNodes = new HashSet<Node>();

            List<Node> allCellNodes = new List<Node>();
            foreach (var side in neighborSides)
            {

                allCellNodes.AddRange(Initialize_CellBorderSurfacesAndSideSocket(cell, nodes, side, layersMax, layerOffeset, addedNodes, intersectonPoints));
            }

            if (allCellNodes.Count == 0 || allCellNodes.Count == 1)
            {
                Debug.LogError("allCellNodes.Count: " + allCellNodes.Count);
                return;
            }


            // int desiredSurfaces = allCellNodes.Count - 1;
            // List<HexSideStructure> hexSideStructures = new List<HexSideStructure>();

            // // HashSet<Node> visited = new HashSet<Node>();
            // HashSet<Node> availibleNodes = new HashSet<Node>();

            // Dictionary<Node, int> visited = new Dictionary<Node, int>();
            // foreach (var node in allCellNodes)
            // {
            //     availibleNodes.Add(node);
            // }

            // foreach (var node in allCellNodes)
            // {
            //     if (visited.ContainsKey(node) == false) visited.Add(node, 0);

            //     hexSideStructures.AddRange(
            //         node.Generate_HexSideStructures(layersMax, layerOffeset, availibleNodes, visited)
            //     );

            //     // if (visited.ContainsKey(item) && visited[item] > 1) continue;

            //     // Node[] members = item.Members();

            //     // for (int i = 1; i < members.Length; i++)
            //     // {
            //     //     Node nodeA = members[i - 1];
            //     //     Node nodeB = members[i];

            //     //     if (visited.Contains(nodeA) || !allCellNodes.Contains(nodeA)) continue;
            //     //     if (visited.Contains(nodeB) || !allCellNodes.Contains(nodeB)) continue;

            //     //     Vector3 pointA = nodeA.Position;
            //     //     Vector3 pointB = nodeB.Position;
            //     //     HexSideStructure new_hexSideStructure = new HexSideStructure(new Vector3[2] { pointA, pointB }, layersMax, layerOffeset);
            //     //     hexSideStructures.Add(new_hexSideStructure);
            //     //     visited.Add(nodeA);
            //     //     visited.Add(nodeB);
            //     // }
            // }

            // Debug.Log("allCellNodes: " + allCellNodes.Count + ", hexSideStructures.Count: " + hexSideStructures.Count + ", desiredSurfaces: " + desiredSurfaces);

            // // List<HexSideStructure> hexSideStructures = new List<HexSideStructure>();
            // // foreach (var node in insideCellBounds)
            // // {
            // //     hexSideStructures.AddRange(node.Generate_HexSideStructures(layersMax, layerOffeset));
            // // }
            // cell.borderSurfaceStructures = hexSideStructures;
        }

        public static List<Node> Initialize_CellBorderSurfacesAndSideSocket(
            HexagonCellPrototype cell,
            Dictionary<Vector3, Node> nodes,
            HexagonSide side,
            int layersMax,
            float layerOffeset,
            HashSet<Node> addedNodes,
            HashSet<Vector3> intersectonPoints
        )
        {
            Vector3[] corners = HexCoreUtil.GetSideCorners(cell, side);
            Vector3 lineStart = corners[0];
            Vector3 lineEnd = corners[1];

            List<Node> insideCellBounds = new List<Node>();
            Vector3 intersectionPoint = Vector3.zero;
            bool intersectionFound = false;

            float cellRadius = cell.size * 1.01f;

            List<Node> nodeValues = nodes.Values.ToList();
            foreach (var node in nodeValues)
            {
                List<Node> members = new List<Node>() {
                    node.NeighborA,
                    node,
                    node.NeighborB
                };

                for (int i = 1; i < members.Count; i++)
                {
                    Node nodeA = members[i - 1];
                    Node nodeB = members[i];
                    Vector3 pointA = nodeA.Position;
                    Vector3 pointB = nodeB.Position;

                    // if (nodeA.isIntersectionNode || nodeB.isIntersectionNode) continue;

                    if (intersectionFound == false)
                    {
                        intersectionPoint = VectorUtil.FindIntersectionPoint(lineStart, lineEnd, pointA, pointB);

                        if (intersectionPoint != Vector3.zero &&
                            VectorUtil.IsPointOnLine(intersectionPoint, lineStart, lineEnd) &&
                            VectorUtil.IsPointOnLine(intersectionPoint, pointA, pointB) &&
                            VectorUtil.DistanceXZ(intersectionPoint, cell.center) < cellRadius
                        )
                        {
                            intersectionFound = true;
                            Vector3 lookup = VectorUtil.PointLookupDefault(intersectionPoint);

                            if (cell.borderPoints.ContainsKey(lookup) == false) cell.borderPoints.Add(lookup, intersectionPoint);

                            if (intersectonPoints.Contains(lookup) == false)
                            {
                                Node intersectionNode = null;
                                intersectonPoints.Add(lookup);


                                // if (nodes.ContainsKey(lookup) && nodes[lookup].isIntersectionNode == false)
                                // {
                                //     intersectionNode = new Node(intersectionPoint);
                                //     intersectionNode.InjectBetweenNeighbors(nodeA, nodeB);

                                //     nodes[lookup] = intersectionNode;
                                // }
                                // else
                                // {
                                //     intersectionNode = new Node(intersectionPoint);
                                //     intersectionNode.InjectBetweenNeighbors(nodeA, nodeB);

                                //     nodes.Add(lookup, intersectionNode);
                                // }
                                if (nodes.ContainsKey(lookup))
                                {
                                    intersectionNode = nodes[lookup];
                                }
                                else
                                {
                                    intersectionNode = new Node(intersectionPoint);
                                    intersectionNode.InjectBetweenNeighbors(nodeA, nodeB);

                                    nodes.Add(lookup, intersectionNode);
                                }

                                intersectionNode.isIntersectionNode = true;
                                insideCellBounds.Add(intersectionNode);
                                addedNodes.Add(intersectionNode);
                            }

                        }
                    }

                    if (addedNodes.Contains(nodeA) == false && VectorUtil.IsPointWithinPolygon(pointA, cell.cornerPoints) && VectorUtil.DistanceXZ(pointA, cell.center) < cellRadius)
                    {
                        insideCellBounds.Add(nodeA);
                        addedNodes.Add(nodeA);

                        nodeA.cellOwner = cell;

                        Vector3 lookup = VectorUtil.PointLookupDefault(pointA);
                        if (cell.borderPoints.ContainsKey(lookup) == false) cell.borderPoints.Add(lookup, pointA);
                    }
                    if (addedNodes.Contains(nodeB) == false && VectorUtil.IsPointWithinPolygon(pointB, cell.cornerPoints) && VectorUtil.DistanceXZ(pointB, cell.center) < cellRadius)
                    {
                        insideCellBounds.Add(nodeB);
                        addedNodes.Add(nodeB);

                        nodeB.cellOwner = cell;

                        Vector3 lookup = VectorUtil.PointLookupDefault(pointB);
                        if (cell.borderPoints.ContainsKey(lookup) == false) cell.borderPoints.Add(lookup, pointB);
                    }
                }
            }

            // for (var j = 0; j < nodes.Count; j++)
            // {
            //     Node node = nodes[j];
            //     List<Node> members = new List<Node>() {
            //         node.NeighborA,
            //         node,
            //         node.NeighborB
            //     };

            //     for (int i = 1; i < members.Count; i++)
            //     {
            //         Node nodeA = members[i - 1];
            //         Node nodeB = members[i];
            //         Vector3 pointA = nodeA.Position;
            //         Vector3 pointB = nodeB.Position;

            //         if (intersectionFound == false)
            //         {
            //             intersectionPoint = VectorUtil.FindIntersectionPoint(lineStart, lineEnd, pointA, pointB);
            //             if (intersectionPoint != Vector3.zero &&
            //                 VectorUtil.IsPointOnLine(intersectionPoint, lineStart, lineEnd) &&
            //                 VectorUtil.IsPointOnLine(intersectionPoint, pointA, pointB) &&
            //                 VectorUtil.DistanceXZ(intersectionPoint, cell.center) < cellRadius
            //             )
            //             {
            //                 intersectionFound = true;
            //                 Vector3 lookup = VectorUtil.ToVector3Int(intersectionPoint);

            //                 if (intersectonPoints.Contains(lookup) == false)
            //                 {
            //                     intersectonPoints.Add(lookup);

            //                     Node intersectionNode = new Node(intersectionPoint);
            //                     intersectionNode.InjectBetweenNeighbors(nodeA, nodeB);

            //                     intersectionNode.isIntersectionNode = true;
            //                     nodes.Add(intersectionNode);

            //                     insideCellBounds.Add(intersectionNode);
            //                     addedNodes.Add(intersectionNode);
            //                 }
            //             }
            //         }

            //         if (addedNodes.Contains(nodeA) == false && VectorUtil.IsPointWithinPolygon(pointA, cell.cornerPoints) && VectorUtil.DistanceXZ(pointA, cell.center) < cellRadius)
            //         {
            //             insideCellBounds.Add(nodeA);
            //             addedNodes.Add(nodeA);

            //             nodeA.cellOwner = cell;
            //         }
            //         if (addedNodes.Contains(nodeB) == false && VectorUtil.IsPointWithinPolygon(pointB, cell.cornerPoints) && VectorUtil.DistanceXZ(pointB, cell.center) < cellRadius)
            //         {
            //             insideCellBounds.Add(nodeB);
            //             addedNodes.Add(nodeB);

            //             nodeB.cellOwner = cell;
            //         }
            //     }
            // }
            if (!intersectionFound) return insideCellBounds;

            // cell.sideEdgeSocket.Add(side, new List<HexSideEdgeSocket>());
            // HexSideEdgeSocket edgeSocket = new HexSideEdgeSocket(intersectionPoint, HexTileGrid.Calculate_LerpStepOfPoint(intersectionPoint, lineStart, lineEnd));
            // cell.sideEdgeSocket[side].Add(edgeSocket);

            // List<HexSideStructure> hexSideStructures = new List<HexSideStructure>();
            // foreach (var node in insideCellBounds)
            // {
            //     hexSideStructures.AddRange(node.Generate_HexSideStructures(layersMax, layerOffeset));
            // }
            // cell.borderSurfaceStructures = hexSideStructures;

            return insideCellBounds;
        }


        // public static Dictionary<HexagonSide, HexSideStructure> Generate_HexSideStructures(HexagonCellPrototype cell, int vertexLayers, float vertexLayerOffset)
        // {
        //     List<HexagonSide> neighborSides = cell.GetNeighborSides(Filter_CellType.Edge);
        //     List<HexagonSide> emptySides = cell.GetNeighborSides(Filter_CellType.NullValue);

        //     // Start on side with outer next neighbor surface 
        //     HexagonSide startSide = neighborSides.Find((s) => emptySides.Contains(HexCoreUtil.NextSide(s, true)));

        //     Dictionary<HexagonSide, HexSideStructure> new_sideEdgeStructures = new Dictionary<HexagonSide, HexSideStructure>();
        //     cell.sideEdgeStructures = new Dictionary<HexagonSide, HexSideStructure>();

        //     Vector3 prevCornerB = Vector3.positiveInfinity;
        //     bool prevSet = false;

        //     HexagonSide side = startSide;
        //     // Debug.Log("startSide: " + startSide);

        //     List<Vector3> bottomFloorPoints = new List<Vector3>() {
        //         cell.center
        //     };

        //     for (int i = 0; i < 6; i++)
        //     {
        //         Vector3[] corners = HexCoreUtil.GetSideCorners(cell, side);
        //         Vector3[] surfaceEdgeCorners;

        //         if (neighborSides.Contains(side))
        //         {
        //             surfaceEdgeCorners = Generate_RandomSurfaceCornerPoints(cell.center, cell.sideEdgeSocket[side][0].step, 0, corners[0], corners[1]);
        //             if (prevSet) surfaceEdgeCorners[0] = prevCornerB;

        //             prevCornerB = surfaceEdgeCorners[1];
        //             prevSet = true;

        //             bottomFloorPoints.AddRange(surfaceEdgeCorners);
        //             new_sideEdgeStructures.Add(side, new HexSideStructure(surfaceEdgeCorners, vertexLayers, vertexLayerOffset));
        //         }
        //         else if (emptySides.Contains(side))
        //         {
        //             surfaceEdgeCorners = Generate_RandomSurfaceCornerPoints(cell.center, 1, 2, corners[0], corners[1]);
        //             if (prevSet) surfaceEdgeCorners[0] = prevCornerB;

        //             prevCornerB = surfaceEdgeCorners[1];
        //             prevSet = true;

        //             bottomFloorPoints.AddRange(surfaceEdgeCorners);
        //             new_sideEdgeStructures.Add(side, new HexSideStructure(surfaceEdgeCorners, vertexLayers, vertexLayerOffset));
        //         }

        //         side = HexCoreUtil.NextSide(side, true);
        //     }

        //     cell.bottomFloorPoints = bottomFloorPoints;
        //     cell.sideEdgeStructures = new_sideEdgeStructures;
        //     return cell.sideEdgeStructures;
        // }


        public void Create_MeshGameObject(HexagonCellPrototype cell, GameObject meshObjectPrefab)
        {
            if (!meshObjectPrefab)
            {
                Debug.LogError("NO meshObjectPrefab");
                return;
            }

            Mesh surfaceMesh;

            // foreach (var item in cell.sideEdgeStructures.Values)
            // {
            //     surfaceMesh = item.Generate_Mesh(MeshVertexSurfaceType.SideInner, transform);
            //     GameObject go_inner = MeshUtil.InstantiatePrefabWithMesh(meshObjectPrefab, surfaceMesh, transform.position);
            //     go_inner.transform.SetParent(folder_MeshObject);

            //     surfaceMesh = item.Generate_Mesh(MeshVertexSurfaceType.SideOuter, transform);
            //     GameObject go_outer = MeshUtil.InstantiatePrefabWithMesh(meshObjectPrefab, surfaceMesh, transform.position);
            //     go_outer.transform.SetParent(folder_MeshObject);
            // }

            // Debug.LogError("cell.borderSurfaceStructures: " + cell.borderSurfaceStructures.Count);

            foreach (var item in cell.borderSurfaceStructures)
            {
                if (item.isEntrance) continue;

                surfaceMesh = item.Generate_Mesh(MeshVertexSurfaceType.SideInner, transform);
                GameObject go_inner = MeshUtil.InstantiatePrefabWithMesh(meshObjectPrefab, surfaceMesh, transform.position);
                go_inner.transform.SetParent(folder_MeshObject);

                surfaceMesh = item.Generate_Mesh(MeshVertexSurfaceType.SideOuter, transform);
                GameObject go_outer = MeshUtil.InstantiatePrefabWithMesh(meshObjectPrefab, surfaceMesh, transform.position);
                go_outer.transform.SetParent(folder_MeshObject);
            }

            // surfaceMesh = HexSideStructure.Generate_Mesh(cell.bottomFloorPoints, MeshVertexSurfaceType.Bottom, transform);
            // GameObject go_floor = MeshUtil.InstantiatePrefabWithMesh(meshObjectPrefab, surfaceMesh, transform.position);
            // go_floor.transform.SetParent(folder_MeshObject);
        }

    }


    [System.Serializable]
    public struct HexSideEdgeSocket
    {
        public HexSideEdgeSocket(Vector3 _point, int _step)
        {
            point = _point;
            step = _step;
        }
        public Vector3 point;
        public int step;
    }

    public class HexSideStructure
    {
        public HexSideStructure(Vector3[] _baseSurfaceEdgeAB, int _layers, float _elevationOffset)
        {
            baseSurfaceEdgeAB = _baseSurfaceEdgeAB;
            Generate_SideSurfaceStructure(_layers, _elevationOffset);
        }
        public List<Vector3> edgeVertexPoints = new List<Vector3>();
        public List<Vector3> cornerFaces_outer = new List<Vector3>();
        public Vector3[] baseSurfaceEdgeAB = new Vector3[2];
        public List<Vector3> surfacePoints = new List<Vector3>();
        public bool isEntrance = false;


        // public Dictionary<int, List<Vector3>> surfacePointByLayer = new Vector3[2];
        public Vector3[] GetSurfacePoints(Transform transform)
        {
            Vector3[] worldPositions = new Vector3[surfacePoints.Count];
            for (int i = 0; i < worldPositions.Length; i++)
            {
                worldPositions[i] = transform.InverseTransformPoint(surfacePoints[i]);
            }
            return worldPositions;
        }

        public void Generate_SideSurfaceStructure(int layers, float elevationOffset)
        {
            List<Vector3> topEdgePoints = VectorUtil.DuplicatePositionsToNewYPos_V2(baseSurfaceEdgeAB.ToList(), (elevationOffset * layers), 1);
            List<Vector3> result = new List<Vector3>();
            result.AddRange(baseSurfaceEdgeAB);
            // result.AddRange(centerPoints);
            result.AddRange(topEdgePoints);

            surfacePoints = result;
        }

        public Mesh Generate_Mesh(MeshVertexSurfaceType surfaceType, Transform transform)
        {
            return Generate_Mesh(surfacePoints, surfaceType, transform);
        }
        public static Mesh Generate_Mesh(List<Vector3> points, MeshVertexSurfaceType surfaceType, Transform transform)
        {
            // Create a new mesh to represent the current surface
            Mesh surfaceMesh = new Mesh();
            // surfaceMesh.vertices = GetSurfacePoints(transform);
            surfaceMesh.vertices = VectorUtil.InversePointsToLocal_ToArray(points, transform);

            if (surfaceType == MeshVertexSurfaceType.Bottom)
            {
                // Generate triangles based on the surface vertices
                int[] bottomTriangles = MeshUtil.GenerateTriangles(points.Count);
                surfaceMesh.triangles = bottomTriangles;

                // Reverse the winding order of the triangles to make the surface face upward
                // ReverseNormals(surfaceMesh);
                MeshUtil.ReverseNormals(surfaceMesh);
                MeshUtil.ReverseTriangles(surfaceMesh); // Updated: Reverse the triangles as well
            }
            else if (surfaceType == MeshVertexSurfaceType.Top)
            {
                // Generate triangles based on the surface vertices
                int[] topTriangles = MeshUtil.GenerateTriangles(points.Count);
                surfaceMesh.triangles = topTriangles;

                // Reverse the winding order of the triangles to make the surface face downward
                // ReverseNormals(surfaceMesh);
            }

            if (surfaceType == MeshVertexSurfaceType.SideOuter || surfaceType == MeshVertexSurfaceType.BothSides)
            {
                // Generate triangles based on the surface vertices
                int[] outerSideTriangles = MeshUtil.GenerateRectangularTriangles(points.Count);
                surfaceMesh.triangles = outerSideTriangles;
                MeshUtil.ReverseTriangles(surfaceMesh); // Updated: Reverse the triangles as well

            }
            if (surfaceType == MeshVertexSurfaceType.SideInner || surfaceType == MeshVertexSurfaceType.BothSides)
            {
                // Generate triangles based on the surface vertices
                int[] innerSideTriangles = MeshUtil.GenerateRectangularTriangles(points.Count);
                surfaceMesh.triangles = innerSideTriangles;

                // Reverse the winding order of the triangles
                MeshUtil.ReverseNormals(surfaceMesh);
            }

            // Set the UVs to the mesh (you can customize this based on your requirements)
            Vector2[] uvs = new Vector2[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                uvs[i] = new Vector2(points[i].x, points[i].y); // Use x and y as UV coordinates
            }
            surfaceMesh.uv = uvs;
            // Recalculate normals and bounds for the surface mesh
            surfaceMesh.RecalculateNormals();
            surfaceMesh.RecalculateBounds();

            return surfaceMesh;
        }

        public void DrawSurfacePoints()
        {
            if (isEntrance) Gizmos.color = Color.magenta;
            foreach (var item in surfacePoints)
            {
                Gizmos.DrawSphere(item, 0.24f);
            }
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(baseSurfaceEdgeAB[0], baseSurfaceEdgeAB[1]);
        }
    }



    public class Node
    {
        public HexagonCellPrototype cellOwner = null;
        public bool isIntersectionNode;
        public Vector3 Position { get; private set; }
        public Node NeighborA { get; set; }
        public Node NeighborB { get; set; }

        public Node(Vector3 position)
        {
            Position = position;
        }

        public Vector3 Lookup() => VectorUtil.PointLookupDefault(Position);
        public Node[] Members() => new Node[3] { NeighborA, this, NeighborB };
        public bool HasBothAssignNeighbors() => NeighborA != null && NeighborB != null;
        public bool IsMember(Node node) => node == NeighborA || node == NeighborB || node == this;
        public void AssignNeighbor(Node neighbor, bool allowOverrride = false)
        {
            if (neighbor == null || IsMember(neighbor)) return;

            if (NeighborA == null || allowOverrride)
            {
                NeighborA = neighbor;
                NeighborA.AssignNeighbor(this, allowOverrride);
            }
            else if (NeighborB == null || allowOverrride)
            {
                NeighborB = neighbor;
                NeighborB.AssignNeighbor(this, allowOverrride);
            }
            if (HasBothAssignNeighbors()) SortNeighborsByXZPosition();
        }

        public void AssignNeighbors(Node neighborA, Node neighborB)
        {
            AssignNeighbor(neighborA);
            AssignNeighbor(neighborB);
        }

        public void InjectBetweenNeighbors(Node neighborA, Node neighborB)
        {
            if (neighborA == null || neighborB == null)
            {
                Debug.LogError("A provided neighbor is Null");
                return;
            }

            if (neighborA != this)
            {
                NeighborA = neighborA;
            }
            if (neighborB != this)
            {
                NeighborB = neighborB;
            }

            if (NeighborA.NeighborA == neighborB)
            {
                NeighborA.NeighborA = this;
            }
            else
            {
                NeighborA.NeighborB = this;
            }
            // NeighborA.SortNeighborsByXZPosition();

            NeighborB = neighborB;
            if (NeighborB.NeighborB == neighborB)
            {
                NeighborB.NeighborB = this;
            }
            else
            {
                NeighborB.NeighborA = this;
            }
            // NeighborB.SortNeighborsByXZPosition();

            // SortNeighborsByXZPosition();

            if (!HasBothAssignNeighbors())
            {
                Debug.LogError("Intersection Node is missing a neighbor!");
            }
        }

        public static List<HexSideStructure> Generate_AllHexSideStructures(Dictionary<Vector3, Node> nodes, int layersMax, float layerOffeset)
        {
            Dictionary<Node, List<Node>> visitedPair = new Dictionary<Node, List<Node>>();
            // HashSet<Node> visited = new HashSet<Node>();
            List<HexSideStructure> hexSideStructures = new List<HexSideStructure>();
            foreach (Node node in nodes.Values)
            {
                hexSideStructures.AddRange(
                    node.Generate_HexSideStructures(layersMax, layerOffeset, visitedPair)
                );
            }

            return hexSideStructures;
        }


        // public List<HexSideStructure> Generate_HexSideStructures(int layersMax, float layerOffeset)
        // {
        //     List<HexSideStructure> hexSideStructures = new List<HexSideStructure>();
        //     List<Node> members = new List<Node>() {
        //             NeighborA,
        //             this,
        //             NeighborB
        //         };

        //     for (int i = 1; i < members.Count; i++)
        //     {
        //         Vector3 pointA = members[i - 1].Position;
        //         Vector3 pointB = members[i].Position;

        //         HexSideStructure new_hexSideStructure = new HexSideStructure(new Vector3[2] { pointA, pointB }, layersMax, layerOffeset);
        //         hexSideStructures.Add(new_hexSideStructure);
        //     }
        //     return hexSideStructures;
        // }

        public List<HexSideStructure> Generate_HexSideStructures(int layersMax, float layerOffeset, Dictionary<Node, List<Node>> visitedPair) //, HashSet<Node> availibleNodes, Dictionary<Node, int> visited)
        // public List<HexSideStructure> Generate_HexSideStructures(int layersMax, float layerOffeset, HashSet<Node> visited) //, HashSet<Node> availibleNodes, Dictionary<Node, int> visited)
        {
            List<HexSideStructure> hexSideStructures = new List<HexSideStructure>();
            // List<Node> members = new List<Node>() {
            //     NeighborA,
            //     this,
            //     NeighborB
            // };
            // if (visited.ContainsKey(this) && visited[this] > 1) return hexSideStructures;
            // visited[this] = 2;

            // visited.Add(this);

            HexSideStructure new_hexSideStructure;

            if (!visitedPair.ContainsKey(this)) visitedPair.Add(this, new List<Node>());
            if (!visitedPair.ContainsKey(NeighborA)) visitedPair.Add(NeighborA, new List<Node>());
            if (!visitedPair.ContainsKey(NeighborB)) visitedPair.Add(NeighborB, new List<Node>());

            bool skipA = (visitedPair[this].Contains(NeighborA)) || (visitedPair[NeighborA].Contains(this));
            bool skipB = (visitedPair[this].Contains(NeighborB)) || (visitedPair[NeighborB].Contains(this));

            if (skipA == false)
            {
                new_hexSideStructure = new HexSideStructure(new Vector3[2] { NeighborA.Position, Position }, layersMax, layerOffeset);
                hexSideStructures.Add(new_hexSideStructure);

                if (NeighborA.cellOwner != null)
                {
                    NeighborA.cellOwner.borderSurfaceStructures.Add(new_hexSideStructure);
                }
                else if (this.cellOwner != null)
                {
                    this.cellOwner.borderSurfaceStructures.Add(new_hexSideStructure);
                }

                visitedPair[this].Add(NeighborA);
                visitedPair[NeighborA].Add(this);
            }

            if (skipB == false)
            {
                new_hexSideStructure = new HexSideStructure(new Vector3[2] { Position, NeighborB.Position }, layersMax, layerOffeset);
                hexSideStructures.Add(new_hexSideStructure);

                if (NeighborB.cellOwner != null)
                {
                    NeighborB.cellOwner.borderSurfaceStructures.Add(new_hexSideStructure);
                }
                else if (this.cellOwner != null)
                {
                    this.cellOwner.borderSurfaceStructures.Add(new_hexSideStructure);
                }

                visitedPair[this].Add(NeighborB);
                visitedPair[NeighborB].Add(this);
            }
            // if (skipA == false)
            // {
            //     new_hexSideStructure = new HexSideStructure(new Vector3[2] { NeighborA.Position, Position }, layersMax, layerOffeset);
            //     hexSideStructures.Add(new_hexSideStructure);

            //     if (NeighborA.cellOwner != null)
            //     {
            //         NeighborA.cellOwner.borderSurfaceStructures.Add(new_hexSideStructure);
            //     }
            //     else if (this.cellOwner != null)
            //     {
            //         this.cellOwner.borderSurfaceStructures.Add(new_hexSideStructure);
            //     }
            // }

            // if (visited.Contains(NeighborB) == false)
            // {
            //     new_hexSideStructure = new HexSideStructure(new Vector3[2] { Position, NeighborB.Position }, layersMax, layerOffeset);
            //     hexSideStructures.Add(new_hexSideStructure);

            //     if (NeighborB.cellOwner != null)
            //     {
            //         NeighborB.cellOwner.borderSurfaceStructures.Add(new_hexSideStructure);
            //     }
            //     else if (this.cellOwner != null)
            //     {
            //         this.cellOwner.borderSurfaceStructures.Add(new_hexSideStructure);
            //     }
            // }

            // if (availibleNodes.Contains(NeighborA) && visited.ContainsKey(NeighborA) && visited[NeighborA] < 2)
            // {
            //     HexSideStructure new_hexSideStructure = new HexSideStructure(new Vector3[2] { NeighborA.Position, Position }, layersMax, layerOffeset);
            //     hexSideStructures.Add(new_hexSideStructure);
            //     visited[NeighborA]++;
            // }
            // if (availibleNodes.Contains(NeighborB) && visited.ContainsKey(NeighborB) && visited[NeighborB] < 2)
            // {
            //     HexSideStructure new_hexSideStructure = new HexSideStructure(new Vector3[2] { NeighborB.Position, Position }, layersMax, layerOffeset);
            //     hexSideStructures.Add(new_hexSideStructure);
            //     visited[NeighborB]++;
            // }

            // if (availibleNodes.Contains(NeighborB))
            // {
            //     HexSideStructure new_hexSideStructure = new HexSideStructure(new Vector3[2] { Position, NeighborB.Position }, layersMax, layerOffeset);
            //     hexSideStructures.Add(new_hexSideStructure);
            // }




            // for (int i = 1; i < members.Count; i++)
            // {
            //     Node nodeA = members[i - 1];
            //     Node nodeB = members[i];

            //     if (!availibleNodes.Contains(nodeA)) continue;
            //     if (!availibleNodes.Contains(nodeB)) continue;
            //     // if (visited.Contains(nodeA) || !availibleNodes.Contains(nodeA)) continue;
            //     // if (visited.Contains(nodeB) || !availibleNodes.Contains(nodeB)) continue;

            //     Vector3 pointA = nodeA.Position;
            //     Vector3 pointB = nodeB.Position;

            //     HexSideStructure new_hexSideStructure = new HexSideStructure(new Vector3[2] { pointA, pointB }, layersMax, layerOffeset);
            //     hexSideStructures.Add(new_hexSideStructure);

            //     visited.Add(nodeA);
            // }

            return hexSideStructures;
        }



        public void SortNeighborsByXZPosition()
        {
            // Calculate the squared distances from the base point to the two other points
            float sqrDistance1 = (Position - NeighborA.Position).sqrMagnitude;
            float sqrDistance2 = (Position - NeighborB.Position).sqrMagnitude;

            // Compare the squared distances to determine the sorting order
            if (sqrDistance1 > sqrDistance2)
            {
                // Swap point1 and point2
                Node temp = NeighborA;
                NeighborA = NeighborB;
                NeighborB = temp;
            }
        }

        public static void SortPointsByXZPosition(Vector3 basePoint, ref Vector3 point1, ref Vector3 point2)
        {
            // Calculate the squared distances from the base point to the two other points
            float sqrDistance1 = (basePoint - point1).sqrMagnitude;
            float sqrDistance2 = (basePoint - point2).sqrMagnitude;

            // Compare the squared distances to determine the sorting order
            if (sqrDistance1 > sqrDistance2)
            {
                // Swap point1 and point2
                Vector3 temp = point1;
                point1 = point2;
                point2 = temp;
            }
        }

        public static Dictionary<Vector3, Node> AssignClosestNeighbors(List<Vector3> points, float ignoreAngleDistance = 2f)
        {
            // Create a list of nodes by mapping the points
            //  Dictionary<Vector3, List<Node>> nodes = points.Select(p => new Node(p)).ToList();
            Dictionary<Vector3, Node> nodes = new Dictionary<Vector3, Node>();
            foreach (var point in points)
            {
                Vector3 lookup = VectorUtil.PointLookupDefault(point);
                if (nodes.ContainsKey(lookup) == false) nodes.Add(lookup, new Node(point));
            }

            // Iterate over each node to assign its closest neighbors
            foreach (Node node in nodes.Values)
            {
                Vector3 position = node.Position;
                Node closestNeighborA = null;
                Node closestNeighborB = null;
                float closestDistanceA = float.MaxValue;
                float closestDistanceB = float.MaxValue;

                foreach (Node otherNode in nodes.Values)
                {
                    if (node == otherNode) continue;

                    float distance = Vector3.Distance(position, otherNode.Position);

                    if (distance > ignoreAngleDistance && VectorUtil.HasAngleApproximate(position, otherNode.Position, 180) == false) continue;

                    if (distance < closestDistanceA)
                    {
                        closestDistanceB = closestDistanceA;
                        closestNeighborB = closestNeighborA;

                        closestDistanceA = distance;
                        closestNeighborA = otherNode;
                    }
                    else if (distance < closestDistanceB)
                    {
                        closestDistanceB = distance;
                        closestNeighborB = otherNode;
                    }
                }

                node.AssignNeighbors(closestNeighborA, closestNeighborB);
            }
            return nodes;
        }

        // public static List<Node> AssignClosestNeighbors(List<Vector3> points, float ignoreAngleDistance = 2f)
        // {
        //     // Create a list of nodes by mapping the points
        //     List<Node> nodes = points.Select(p => new Node(p)).ToList();

        //     // Iterate over each node to assign its closest neighbors
        //     foreach (Node node in nodes)
        //     {
        //         Vector3 position = node.Position;
        //         Node closestNeighborA = null;
        //         Node closestNeighborB = null;
        //         float closestDistanceA = float.MaxValue;
        //         float closestDistanceB = float.MaxValue;

        //         foreach (Node otherNode in nodes)
        //         {
        //             if (node != otherNode)
        //             {
        //                 float distance = Vector3.Distance(position, otherNode.Position);

        //                 if (distance > ignoreAngleDistance && VectorUtil.HasAngleApproximate(position, otherNode.Position, 180) == false) continue;

        //                 if (distance < closestDistanceA)
        //                 {
        //                     closestDistanceB = closestDistanceA;
        //                     closestNeighborB = closestNeighborA;

        //                     closestDistanceA = distance;
        //                     closestNeighborA = otherNode;
        //                 }
        //                 else if (distance < closestDistanceB)
        //                 {
        //                     closestDistanceB = distance;
        //                     closestNeighborB = otherNode;
        //                 }
        //             }
        //         }

        //         node.NeighborA = closestNeighborA;
        //         node.NeighborB = closestNeighborB;
        //         node.SortNeighborsByXZPosition();
        //     }

        //     return nodes;
        // }

        public static List<Vector3> GetOrderedPoints(Dictionary<Vector3, Node> nodes)
        {
            List<Vector3> orderedPoints = new List<Vector3>();
            HashSet<Vector3> added = new HashSet<Vector3>();
            // Start with the first node in the list
            Node currentNode = null;
            foreach (var item in nodes.Values)
            {
                currentNode = item;
                break;
            }

            // Keep track of the initial node to detect when the traversal completes
            Node initialNode = currentNode;
            int attempts = 999;
            Node lastNode = currentNode;
            do
            {

                if (added.Contains(currentNode.Position) == false)
                {
                    orderedPoints.Add(currentNode.Position);
                    added.Add(currentNode.Position);
                    lastNode = currentNode;
                    //     currentNode = currentNode.GetClosestNodeInSet(lastNode, added);
                    // }
                    // else
                    // {
                    //     currentNode = currentNode.GetClosestNodeInSet(lastNode, added);
                }

                currentNode = added.Contains(currentNode.NeighborA.Position) ? currentNode.NeighborB : currentNode.NeighborA;
                if (currentNode == null) currentNode = lastNode.GetClosestNodeInSet(lastNode, added);

                attempts--;
            }
            while (currentNode != null && attempts > 0 && orderedPoints.Count < nodes.Count); // currentNode != initialNode);

            return orderedPoints;
        }

        public Node GetClosestNodeInSet(Node outerNode, HashSet<Vector3> added)
        {
            List<Node> members = new List<Node>() {
                this,
                NeighborA,
                NeighborB,
            };
            Node closestMember = null;
            float closestDistance = float.MaxValue;
            foreach (var member in members)
            {
                if (added.Contains(member.Position) || member == outerNode) continue;

                float dist = VectorUtil.DistanceXZ(member.Position, outerNode.Position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestMember = member;
                }
            }
            return closestMember;
        }

        public void DrawLinesAndPoints()
        {
            Gizmos.color = isIntersectionNode ? Color.red : Color.black;
            Gizmos.DrawSphere(Position, 0.4f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(Position, NeighborA.Position);
            Gizmos.DrawLine(Position, NeighborB.Position);

            if (isIntersectionNode)
            {
                Gizmos.DrawWireSphere(NeighborA.Position, 0.4f);
                Gizmos.DrawWireSphere(NeighborB.Position, 0.4f);
            }
        }
    }


}