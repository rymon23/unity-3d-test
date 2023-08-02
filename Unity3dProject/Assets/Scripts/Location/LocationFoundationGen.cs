using System.Collections.Generic;
using UnityEngine;
using ProceduralBase;
using WFCSystem;
using System.Linq;

public class LocationFoundationGen : MonoBehaviour
{
    [SerializeField] private Vector2 _currentWorldPos_WorldspaceLookup = Vector2.zero;
    [SerializeField] private Vector2 _currentWorldPos_AreaLookup = Vector2.zero;
    [SerializeField] private Vector2 _currentWorldPos_RegionLookup = Vector2.zero;
    [Header(" ")]
    private List<Vector2> _active_worldspaceLookups = new List<Vector2>();
    private List<Vector2> _active_areaLookups = new List<Vector2>();

    public void UpdatePositionData(
        Vector2 worldspaceLookup,
        Vector2 areaLookup,
        Vector2 regionLookup
    )
    {
        _currentWorldPos_WorldspaceLookup = worldspaceLookup;
        _currentWorldPos_AreaLookup = areaLookup;
        _currentWorldPos_RegionLookup = regionLookup;
    }

    [SerializeField] private bool enableinternakCellTracker;
    [SerializeField] private HexCellSizes debug_currentCellSize = HexCellSizes.X_36;
    [SerializeField] private HexCellSizes debug_currentCellSnapSize = HexCellSizes.Default;
    [Header(" ")]
    [Range(3, 48)][SerializeField] private int debug_edgeBasedPathingDensity = 6;
    [SerializeField] private bool debug_highlightCluster;
    [Range(0, 99)][SerializeField] private int _currentCluster = 0;
    [Header(" ")]
    [Header(" ")]
    [SerializeField] private int debug_currentCellHeight = 2;
    [SerializeField] private float debug_centerRadiusMult = 0.5f;
    private float debug_updateDistanceMult = 0.25f;

    [SerializeField] private bool debug_terrainPlatform;
    [Header(" ")]
    [SerializeField] private bool resetPrototypes;
    [Header(" ")]
    [SerializeField] private bool show_foundationNodes;
    [SerializeField] private bool show_groundCellInfluanceRadius;
    [SerializeField] private bool show_bufferPath;
    [Header(" ")]

    [SerializeField] private bool show_buildingBoundsShells;
    [Header(" ")]
    [SerializeField] private bool enable_overPathCenters;
    [SerializeField] private bool show_overPathCenters;
    [Header(" ")]
    [SerializeField] private bool showGlobalVertexGrid;
    [SerializeField] private bool show_gridBounds;

    [Header(" ")]
    [Header("Building Protoypes")]
    [Header(" ")]
    [SerializeField] private bool enable_BuildingPrototypes;
    [SerializeField] private BuildingPrototypeDisplaySettings buildingPrototypeDisplaySettings = new BuildingPrototypeDisplaySettings();
    [Header(" ")]
    [Range(1, 50)][SerializeField] int defaultBuilding_layersMin = 1;
    [Range(1, 50)][SerializeField] int defaultBuilding_layersMax = 10;
    [Range(0.5f, 50f)][SerializeField] float defaultBuilding_size = 2f;
    [Header(" ")]
    [Range(1, 7)][SerializeField] int buildingNode_membersMax = 3;
    [Range(1, 8)][SerializeField] int buildingNode_layerOffset = 4;
    [Header(" ")]
    [Range(1, 10)][SerializeField] int buildingConnector_baseOffesetMin = 2;
    [Range(1, 10)][SerializeField] int buildingConnector_baseOffesetMax = 4;
    [Range(1, 4)][SerializeField] int buildingConnector_layersMax = 2;
    [Header(" ")]

    [Range(1, 3)][SerializeField] private float groundCellInfluenceRadiusMult = 1.29f;
    [Range(0.1f, 1f)][SerializeField] private float bufferZoneLerpMult = 0.55f;
    [Header(" ")]
    [Range(0.1f, 1f)][SerializeField] private float pathCellLerpMult = 0.5f;

    [Header(" ")]
    [SerializeField] private List<LayeredNoiseOption> layeredNoise_terrainGlobal;
    [Range(2, 10)][SerializeField] private int foundation_layerOffset = 2;
    [Range(1, 12)][SerializeField] private int foundation_maxLayers = 6;
    [Range(1, 12)][SerializeField] private int foundation_maxLayerDifference = 4;
    [Header(" ")]
    [Range(1, 12)][SerializeField] private int foundation_innersMax = 4;
    [Range(1, 12)][SerializeField] private int foundation_cornersMax = 1;
    [Header(" ")]
    [Range(0, 100)][SerializeField] private int foundation_random_Inners = 40;
    [Range(0, 100)][SerializeField] private int foundation_random_Corners = 40;
    [Range(0, 100)][SerializeField] private int foundation_random_Center = 90;

    [Header(" ")]
    [Range(-32, 768)][SerializeField] private float terrainHeightDefault = 10f;
    [Range(1, 5)][SerializeField] int vertexDensity = 3;
    [Header(" ")]
    [SerializeField] Vector3 defaultBuilding_dimensions = new Vector3(10f, 12f, 6f);

    [SerializeField] private GameObject worldAreaMeshObjectPrefab;
    List<RectangleBounds> buildingBoundsShells = null;
    Dictionary<int, Dictionary<Vector2, Vector3>> buildingBlockClusters = null;
    Dictionary<int, Dictionary<Vector2, Vector3>> buildingClusters = null;
    Dictionary<int, BuildingPrototype> buildingPrototypes = null;

    private Vector2 terrainChunkSizeXZ = new Vector2(164, 95);
    GameObject meshChunkObject = null;
    Dictionary<Vector2, TerrainVertex> globalTerrainVertexGrid = null;
    Bounds activeGridBounds;
    HexagonCellPrototype worldspaceCell;
    List<(Vector2[,], Vector2)> allGridChunksWithCenters = null;
    List<Vector3> worldspaceChunkCenters = null;
    Dictionary<Vector2, Vector3> allCenterPointsAdded = null;
    Dictionary<Vector2, Vector3> cellCenters_ByLookup = null;
    Dictionary<Vector2, Vector3> pathCenters = null;
    Dictionary<Vector2, Vector3> overPathCenters = new Dictionary<Vector2, Vector3>();
    Dictionary<Vector2, Vector3> foundOverPathCellConnectors = new Dictionary<Vector2, Vector3>();

    #region Saved
    private HexagonCellPrototype debug_currentHexCell;
    private Vector3 debug_lastPosition;

    private HexCellSizes _currentCellSize_;
    private HexagonSide _currentSide_;
    private int _currentCellHeight_;
    private int _currentCellLayer_;
    #endregion

    public Transform folder_Main { get; private set; } = null;
    public void Evalaute_Folder()
    {
        if (folder_Main == null)
        {
            folder_Main = new GameObject("Terrain" + this.gameObject.name).transform;
            folder_Main.transform.SetParent(this.transform);
        }
    }

    public void Generate_Platfrom()
    {
        cellCenters_ByLookup = new Dictionary<Vector2, Vector3>();
        allCenterPointsAdded = new Dictionary<Vector2, Vector3>();

        buildingBlockClusters = Generate_TerrainPlatform(
            debug_currentHexCell.center,
            (int)debug_currentCellSize,
            foundation_layerOffset,
            foundation_maxLayers,
            HexCellSizes.Worldspace,
            foundation_innersMax,
            foundation_cornersMax,
            foundation_random_Inners,
            foundation_random_Corners,
            foundation_random_Center,
            cellCenters_ByLookup,
            allCenterPointsAdded,
            layeredNoise_terrainGlobal,
            terrainHeightDefault,
            foundation_maxLayerDifference
        );

        (
            Dictionary<int, Dictionary<Vector2, Vector3>> _blockCluster_,
            Dictionary<Vector2, Vector3> _pathCenters
        ) = HexCoreUtil.Generate_CellCityClusterCenters(
                debug_currentHexCell.center,
                (int)HexCellSizes.X_4,
                (int)debug_currentCellSize,
                99,
                14,
                true,
                65,
                60
            );

        pathCenters = _pathCenters;

        List<Vector2> keys = pathCenters.Keys.ToList();
        foreach (var lookup in keys)
        {
            Vector3 center = pathCenters[lookup];

            (Vector2 nearestLookup, float nearestDist, float avgElevation) = GetCloseest_HexLookupInDictionary_withDistance_ElevationLerp(
                center,
                12,
                cellCenters_ByLookup,
                pathCellLerpMult
            );

            // (Vector2 nearestLookup, float nearestDist) = GetCloseest_HexLookupInDictionary_withDistance(
            //     center,
            //     12,
            //     cellCenters_ByLookup
            // );

            float pathNoiseHeight = 0;
            if (nearestDist != float.MaxValue)
            {
                if (nearestDist < (4 * (groundCellInfluenceRadiusMult)))
                {
                    // pathNoiseHeight = avgElevation;
                    // pathNoiseHeight = cellCenters_ByLookup[nearestLookup].y;
                    pathNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)center.x, (int)center.z, terrainHeightDefault, layeredNoise_terrainGlobal);
                    pathNoiseHeight = Mathf.Lerp(pathNoiseHeight, avgElevation, 0.6f);
                }
                else
                {
                    pathNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)center.x, (int)center.z, terrainHeightDefault, layeredNoise_terrainGlobal);
                    pathNoiseHeight = Mathf.Lerp(pathNoiseHeight, avgElevation, pathCellLerpMult);
                    // pathNoiseHeight = UtilityHelpers.RoundToNearestStep(pathNoiseHeight, 0.3f);
                    // pathNoiseHeight = Mathf.Lerp(pathNoiseHeight, cellCenters_ByLookup[nearestLookup].y, 0.56f);
                    // center.y = pathNoiseHeight;
                }
                center.y = pathNoiseHeight;
                pathCenters[lookup] = center;
            }
        }


        buildingBoundsShells = new List<RectangleBounds>();
        overPathCenters = new Dictionary<Vector2, Vector3>();
        foundOverPathCellConnectors = new Dictionary<Vector2, Vector3>();

        if (enable_overPathCenters)
        {
            keys = allCenterPointsAdded.Keys.ToList();
            foreach (var lookup in keys)
            {
                if (cellCenters_ByLookup.ContainsKey(lookup)) continue;

                Vector3 hexCenter = allCenterPointsAdded[lookup];

                Dictionary<Vector2, Vector3> _foundConnectors = new Dictionary<Vector2, Vector3>();

                if (HexCoreUtil.IsHexCenterBetweenNeighborsInLookup(
                    hexCenter,
                    12,
                    cellCenters_ByLookup,
                    _foundConnectors
                ))
                {
                    float heighestY = 0.1f;
                    foreach (var k in _foundConnectors.Keys)
                    {
                        Vector3 connectorFoundation = _foundConnectors[k];

                        if (heighestY == 0.1f || heighestY < connectorFoundation.y) heighestY = connectorFoundation.y;

                        if (foundOverPathCellConnectors.ContainsKey(k) == false) foundOverPathCellConnectors.Add(k, connectorFoundation);
                    }

                    // float elevation = Mathf.Clamp(heighestY, heighestY, UnityEngine.Random.Range(heighestY, (heighestY + (defaultBuilding_layersMax * foundation_layerOffset)) + 1));
                    // hexCenter.y = (UnityEngine.Random.Range(heighestY, defaultBuilding_layersMax + 1) * foundation_layerOffset);
                    hexCenter.y = heighestY + UnityEngine.Random.Range(buildingConnector_baseOffesetMin, buildingConnector_baseOffesetMax) * foundation_layerOffset;
                    overPathCenters.Add(lookup, hexCenter);
                    allCenterPointsAdded[lookup] = hexCenter;


                    Vector3 dimensions = new Vector3(
                        defaultBuilding_dimensions.x,
                        UnityEngine.Random.Range(1, buildingConnector_layersMax + 1) * foundation_layerOffset,
                        defaultBuilding_dimensions.z
                    );
                    RectangleBounds rect = new RectangleBounds(hexCenter, defaultBuilding_size, UnityEngine.Random.Range(0, 6), dimensions);
                    buildingBoundsShells.Add(rect);
                }
            }
        }


        foreach (var ix in buildingBlockClusters.Keys)
        {
            foreach (var lookup in buildingBlockClusters[ix].Keys)
            {
                Vector3 pos = buildingBlockClusters[ix][lookup];

                Vector3 dimensions = Vector3.zero;
                float height = 0;
                if (foundOverPathCellConnectors.ContainsKey(lookup))
                {
                    height = UnityEngine.Random.Range(buildingConnector_baseOffesetMax + buildingConnector_layersMax, (buildingConnector_baseOffesetMax + buildingConnector_layersMax) + 2) * foundation_layerOffset;
                    dimensions = new Vector3(
                        defaultBuilding_dimensions.x,
                        height,
                        defaultBuilding_dimensions.z
                    );
                }
                else
                {
                    dimensions = new Vector3(
                        defaultBuilding_dimensions.x * UtilityHelpers.RoundToNearestStep(UnityEngine.Random.Range(0.6f, 1f), 0.2f),
                        UnityEngine.Random.Range(defaultBuilding_layersMin, defaultBuilding_layersMax + 1) * foundation_layerOffset,
                        defaultBuilding_dimensions.z
                    );
                }

                RectangleBounds rect = new RectangleBounds(pos, defaultBuilding_size, UnityEngine.Random.Range(0, 6), dimensions);
                buildingBoundsShells.Add(rect);
            }
        }


        if (enable_BuildingPrototypes)
        {
            buildingPrototypes = BuildingPrototype.Generate_BuildingPrototypesFromBlockClusters(
                buildingBlockClusters,
                buildingNode_membersMax,
                transform,
                null
            );
        }

        // buildingClusters = HexCoreUtil.Generate_BaseBuildingClusters(
        //     buildingBlockClusters,
        //     5,
        //     null
        // );
        // if (buildingClusters != null)
        // {
        //     buildingPrototypes = new Dictionary<int, BuildingPrototype>();

        //     foreach (var ix in buildingClusters.Keys)
        //     {
        //         List<Vector3> points = buildingClusters[ix].Values.ToList();
        //         Vector3 clusterCenter = VectorUtil.Calculate_CenterPositionFromPoints(points);
        //         Vector3 gridStartPos = HexCoreUtil.Calculate_ClosestHexCenter_V2(clusterCenter, (int)HexCellSizes.X_12);
        //         gridStartPos.y = points[0].y;

        //         BuildingPrototype new_buildingPrototype = new BuildingPrototype(
        //             gridStartPos,
        //             new Dictionary<int, Dictionary<Vector2, Vector3>> {
        //                 {(int)HexCellSizes.X_12,  buildingClusters[ix]}
        //             },
        //             transform
        //         );

        //         buildingPrototypes.Add(ix, new_buildingPrototype);

        //         //Temp
        //         break;
        //     }
        // }

        if (worldAreaMeshObjectPrefab == null)
        {
            Debug.LogError("worldAreaMeshObjectPrefab is null");
            return;
        }

        worldspaceCell = HexagonCellPrototype.Generate_NearestHexCell(debug_currentHexCell.center, (int)HexCellSizes.Worldspace, HexCellSizes.Default, 0);
        activeGridBounds = VectorUtil.CalculateBounds_V2(worldspaceCell.cornerPoints.ToList());

        Dictionary<Vector2, TerrainVertex> _globalTerrainVertexGrid = new Dictionary<Vector2, TerrainVertex>();

        Vector2[,] gridLookups = Generate_TerrainFoundationVertices(
                activeGridBounds,
                transform,
                vertexDensity,
                HexCellSizes.X_12,
                cellCenters_ByLookup,
                allCenterPointsAdded,
                pathCenters,
                layeredNoise_terrainGlobal,
                _globalTerrainVertexGrid,
                terrainHeightDefault,
                groundCellInfluenceRadiusMult,
                bufferZoneLerpMult,
                foundation_layerOffset
            );

        globalTerrainVertexGrid = _globalTerrainVertexGrid;

        worldspaceChunkCenters = worldspaceCell.GetTerrainChunkCoordinates().ToList();

        allGridChunksWithCenters = new List<(Vector2[,], Vector2)>();

        allGridChunksWithCenters.AddRange(WorldManagerUtil.GetVertexGridChunkKeys(
                globalTerrainVertexGrid,
                worldspaceChunkCenters,
                vertexDensity,
                terrainChunkSizeXZ
            ));


        // for (int i = 0; i < allGridChunksWithCenters.Count; i++)
        // {
        //     Vector2 chunkCenterLookup = TerrainChunkData.CalculateTerrainChunkLookup(allGridChunksWithCenters[i].Item2);

        //     // if (Evaluate_WorldAreaFolder_ByLookup(chunkCenterLookup) == false)
        //     // {
        //     //     continue;
        //     // }

        //     // Extract Grid Data
        //     Vector2[,] gridChunkKeys = allGridChunksWithCenters[i].Item1;

        (Vector3[] vertexPositions, Vector2[] uvs, HashSet<Vector2> meshTraingleExcludeList) = TerrainVertexUtil.ExtractVertexWorldPositionsAndUVs_V2(
            gridLookups,
            globalTerrainVertexGrid,
            transform
        );

        if (meshChunkObject == null)
        {
            meshChunkObject = MeshUtil.InstantiatePrefabWithMesh(worldAreaMeshObjectPrefab, new Mesh(), transform.position);
            meshChunkObject.name = "Foundation";
            if (folder_Main != null)
            {
                meshChunkObject.transform.SetParent(folder_Main);
            }
            else meshChunkObject.transform.SetParent(transform);
        }

        // Get the MeshFilter component from the instantiatedPrefab
        MeshFilter meshFilter = meshChunkObject.GetComponent<MeshFilter>();
        MeshCollider meshCollider = meshChunkObject.GetComponent<MeshCollider>();
        Mesh finalMesh = meshFilter.sharedMesh;

        finalMesh.name = "Terrain Mesh";
        finalMesh.Clear();
        finalMesh.vertices = vertexPositions;
        finalMesh.triangles = WorldAreaManager.GenerateTerrainTriangles_V2(gridLookups, meshTraingleExcludeList);
        finalMesh.uv = uvs;

        // Refresh Terrain Mesh
        finalMesh.RecalculateNormals();
        finalMesh.RecalculateBounds();

        // Apply the mesh data to the MeshFilter component
        meshFilter.sharedMesh = finalMesh;
        meshCollider.sharedMesh = finalMesh;
        // }
    }


    public void Debug_EvaluatePosition()
    {
        if (
            resetPrototypes ||
            debug_currentHexCell == null ||
            _currentCellSize_ != debug_currentCellSize ||
            _currentCellHeight_ != debug_currentCellHeight ||
            Vector3.Distance(debug_lastPosition, transform.position) > ((int)debug_currentCellSize * debug_updateDistanceMult)
        )
        {
            resetPrototypes = false;

            _currentCellSize_ = debug_currentCellSize;
            _currentCellHeight_ = debug_currentCellHeight;

            debug_lastPosition = transform.position;

            debug_currentHexCell = HexagonCellPrototype.Generate_NearestHexCell(transform.position, (int)debug_currentCellSize, debug_currentCellSnapSize, debug_currentCellHeight);

            int layer = HexCoreUtil.Calculate_CellLayer(transform.position, debug_currentCellHeight);
            if (_currentCellLayer_ != layer)
            {
                _currentCellLayer_ = layer;

                Debug.Log("Current Layer: " + _currentCellLayer_);
            }

            if (debug_terrainPlatform)
            {

                Generate_Platfrom();


                //     cellCenters_ByLookup = new Dictionary<Vector2, Vector3>();
                //     allCenterPointsAdded = new Dictionary<Vector2, Vector3>();

                //     buildingBlockClusters = Generate_TerrainPlatform(
                //         debug_currentHexCell.center,
                //         (int)debug_currentCellSize,
                //         foundation_layerOffset,
                //         foundation_maxLayers,
                //         HexCellSizes.Worldspace,
                //         foundation_innersMax,
                //         foundation_cornersMax,
                //         foundation_random_Inners = 40,
                //         foundation_random_Corners = 40,
                //         foundation_random_Center = 100,
                //         cellCenters_ByLookup,
                //         allCenterPointsAdded,
                //         layeredNoise_terrainGlobal,
                //         terrainHeightDefault,
                //         foundation_maxLayerDifference
                //     );

                //     // (
                //     //     Dictionary<int, Dictionary<Vector2, Vector3>> _blockCluster_,
                //     //     Dictionary<Vector2, Vector3> _pathCenters
                //     // ) = HexCoreUtil.Generate_CellCityClusterCenters(debug_currentHexCell.center, (int)HexCellSizes.X_4, debug_alternateCell_size, 99, 14);

                //     (
                //         Dictionary<int, Dictionary<Vector2, Vector3>> _blockCluster_,
                //         Dictionary<Vector2, Vector3> _pathCenters
                //     ) = HexCoreUtil.Generate_CellCityClusterCenters(
                //             debug_currentHexCell.center,
                //             (int)HexCellSizes.X_4,
                //             (int)debug_currentCellSize,
                //             99,
                //             14,
                //             true,
                //             65,
                //             60
                //         );

                //     pathCenters = _pathCenters;

                //     List<Vector2> keys = pathCenters.Keys.ToList();
                //     foreach (var lookup in keys)
                //     {
                //         Vector3 center = pathCenters[lookup];

                //         (Vector2 nearestLookup, float nearestDist, float avgElevation) = GetCloseest_HexLookupInDictionary_withDistance_ElevationLerp(
                //             center,
                //             12,
                //             cellCenters_ByLookup,
                //             pathCellLerpMult
                //         );

                //         // (Vector2 nearestLookup, float nearestDist) = GetCloseest_HexLookupInDictionary_withDistance(
                //         //     center,
                //         //     12,
                //         //     cellCenters_ByLookup
                //         // );

                //         float pathNoiseHeight = 0;
                //         if (nearestDist != float.MaxValue)
                //         {
                //             if (nearestDist < (4 * (groundCellInfluenceRadiusMult)))
                //             {
                //                 // pathNoiseHeight = avgElevation;
                //                 // pathNoiseHeight = cellCenters_ByLookup[nearestLookup].y;
                //                 pathNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)center.x, (int)center.z, terrainHeightDefault, layeredNoise_terrainGlobal);
                //                 pathNoiseHeight = Mathf.Lerp(pathNoiseHeight, avgElevation, 0.6f);
                //             }
                //             else
                //             {
                //                 pathNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)center.x, (int)center.z, terrainHeightDefault, layeredNoise_terrainGlobal);
                //                 pathNoiseHeight = Mathf.Lerp(pathNoiseHeight, avgElevation, pathCellLerpMult);
                //                 // pathNoiseHeight = UtilityHelpers.RoundToNearestStep(pathNoiseHeight, 0.3f);
                //                 // pathNoiseHeight = Mathf.Lerp(pathNoiseHeight, cellCenters_ByLookup[nearestLookup].y, 0.56f);
                //                 // center.y = pathNoiseHeight;
                //             }
                //             center.y = pathNoiseHeight;
                //             pathCenters[lookup] = center;
                //         }
                //     }


                //     buildingBoundsShells = new List<RectangleBounds>();
                //     overPathCenters = new Dictionary<Vector2, Vector3>();
                //     foundOverPathCellConnectors = new Dictionary<Vector2, Vector3>();

                //     if (enable_overPathCenters)
                //     {
                //         keys = allCenterPointsAdded.Keys.ToList();
                //         foreach (var lookup in keys)
                //         {
                //             if (cellCenters_ByLookup.ContainsKey(lookup)) continue;

                //             Vector3 hexCenter = allCenterPointsAdded[lookup];

                //             Dictionary<Vector2, Vector3> _foundConnectors = new Dictionary<Vector2, Vector3>();

                //             if (HexCoreUtil.IsHexCenterBetweenNeighborsInLookup(
                //                 hexCenter,
                //                 12,
                //                 cellCenters_ByLookup,
                //                 _foundConnectors
                //             ))
                //             {
                //                 float heighestY = 0.1f;
                //                 foreach (var k in _foundConnectors.Keys)
                //                 {
                //                     Vector3 connectorFoundation = _foundConnectors[k];

                //                     if (heighestY == 0.1f || heighestY < connectorFoundation.y) heighestY = connectorFoundation.y;

                //                     if (foundOverPathCellConnectors.ContainsKey(k) == false) foundOverPathCellConnectors.Add(k, connectorFoundation);
                //                 }

                //                 // float elevation = Mathf.Clamp(heighestY, heighestY, UnityEngine.Random.Range(heighestY, (heighestY + (defaultBuilding_layersMax * foundation_layerOffset)) + 1));
                //                 // hexCenter.y = (UnityEngine.Random.Range(heighestY, defaultBuilding_layersMax + 1) * foundation_layerOffset);
                //                 hexCenter.y = heighestY + UnityEngine.Random.Range(buildingConnector_baseOffesetMin, buildingConnector_baseOffesetMax) * foundation_layerOffset;
                //                 overPathCenters.Add(lookup, hexCenter);
                //                 allCenterPointsAdded[lookup] = hexCenter;


                //                 Vector3 dimensions = new Vector3(
                //                     defaultBuilding_dimensions.x,
                //                     UnityEngine.Random.Range(1, buildingConnector_layersMax + 1) * foundation_layerOffset,
                //                     defaultBuilding_dimensions.z
                //                 );
                //                 RectangleBounds rect = new RectangleBounds(hexCenter, defaultBuilding_size, UnityEngine.Random.Range(0, 6), dimensions);
                //                 buildingBoundsShells.Add(rect);
                //             }
                //         }
                //     }


                //     foreach (var ix in buildingBlockClusters.Keys)
                //     {
                //         foreach (var lookup in buildingBlockClusters[ix].Keys)
                //         {
                //             Vector3 pos = buildingBlockClusters[ix][lookup];

                //             Vector3 dimensions = Vector3.zero;
                //             float height = 0;
                //             if (foundOverPathCellConnectors.ContainsKey(lookup))
                //             {
                //                 height = UnityEngine.Random.Range(buildingConnector_baseOffesetMax + buildingConnector_layersMax, (buildingConnector_baseOffesetMax + buildingConnector_layersMax) + 2) * foundation_layerOffset;
                //                 dimensions = new Vector3(
                //                     defaultBuilding_dimensions.x,
                //                     height,
                //                     defaultBuilding_dimensions.z
                //                 );
                //             }
                //             else
                //             {
                //                 dimensions = new Vector3(
                //                     defaultBuilding_dimensions.x * UtilityHelpers.RoundToNearestStep(UnityEngine.Random.Range(0.6f, 1f), 0.2f),
                //                     UnityEngine.Random.Range(defaultBuilding_layersMin, defaultBuilding_layersMax + 1) * foundation_layerOffset,
                //                     defaultBuilding_dimensions.z
                //                 );
                //             }

                //             RectangleBounds rect = new RectangleBounds(pos, defaultBuilding_size, UnityEngine.Random.Range(0, 6), dimensions);
                //             buildingBoundsShells.Add(rect);
                //         }
                //     }


                //     if (enable_BuildingPrototypes)
                //     {
                //         buildingPrototypes = BuildingPrototype.Generate_BuildingPrototypesFromBlockClusters(
                //             buildingBlockClusters,
                //             buildingNode_membersMax,
                //             transform,
                //             null
                //         );
                //     }

                //     // buildingClusters = HexCoreUtil.Generate_BaseBuildingClusters(
                //     //     buildingBlockClusters,
                //     //     5,
                //     //     null
                //     // );
                //     // if (buildingClusters != null)
                //     // {
                //     //     buildingPrototypes = new Dictionary<int, BuildingPrototype>();

                //     //     foreach (var ix in buildingClusters.Keys)
                //     //     {
                //     //         List<Vector3> points = buildingClusters[ix].Values.ToList();
                //     //         Vector3 clusterCenter = VectorUtil.Calculate_CenterPositionFromPoints(points);
                //     //         Vector3 gridStartPos = HexCoreUtil.Calculate_ClosestHexCenter_V2(clusterCenter, (int)HexCellSizes.X_12);
                //     //         gridStartPos.y = points[0].y;

                //     //         BuildingPrototype new_buildingPrototype = new BuildingPrototype(
                //     //             gridStartPos,
                //     //             new Dictionary<int, Dictionary<Vector2, Vector3>> {
                //     //                 {(int)HexCellSizes.X_12,  buildingClusters[ix]}
                //     //             },
                //     //             transform
                //     //         );

                //     //         buildingPrototypes.Add(ix, new_buildingPrototype);

                //     //         //Temp
                //     //         break;
                //     //     }
                //     // }

                //     if (worldAreaMeshObjectPrefab == null)
                //     {
                //         Debug.LogError("worldAreaMeshObjectPrefab is null");
                //         return;
                //     }

                //     worldspaceCell = Generate_NearestHexCell(debug_currentHexCell.center, (int)HexCellSizes.Worldspace, HexCellSizes.Default, 0);
                //     activeGridBounds = VectorUtil.CalculateBounds_V2(worldspaceCell.cornerPoints.ToList());

                //     Dictionary<Vector2, TerrainVertex> _globalTerrainVertexGrid = new Dictionary<Vector2, TerrainVertex>();

                //     Vector2[,] gridLookups = Generate_TerrainFoundationVertices(
                //             activeGridBounds,
                //             transform,
                //             vertexDensity,
                //             HexCellSizes.X_12,
                //             cellCenters_ByLookup,
                //             allCenterPointsAdded,
                //             pathCenters,
                //             layeredNoise_terrainGlobal,
                //             _globalTerrainVertexGrid,
                //             terrainHeightDefault,
                //             groundCellInfluenceRadiusMult,
                //             bufferZoneLerpMult,
                //             foundation_layerOffset
                //         );

                //     globalTerrainVertexGrid = _globalTerrainVertexGrid;

                //     worldspaceChunkCenters = worldspaceCell.GetTerrainChunkCoordinates().ToList();

                //     allGridChunksWithCenters = new List<(Vector2[,], Vector2)>();

                //     allGridChunksWithCenters.AddRange(WorldManagerUtil.GetVertexGridChunkKeys(
                //             globalTerrainVertexGrid,
                //             worldspaceChunkCenters,
                //             vertexDensity,
                //             terrainChunkSizeXZ
                //         ));


                //     // for (int i = 0; i < allGridChunksWithCenters.Count; i++)
                //     // {
                //     //     Vector2 chunkCenterLookup = TerrainChunkData.CalculateTerrainChunkLookup(allGridChunksWithCenters[i].Item2);

                //     //     // if (Evaluate_WorldAreaFolder_ByLookup(chunkCenterLookup) == false)
                //     //     // {
                //     //     //     continue;
                //     //     // }

                //     //     // Extract Grid Data
                //     //     Vector2[,] gridChunkKeys = allGridChunksWithCenters[i].Item1;

                //     (Vector3[] vertexPositions, Vector2[] uvs, HashSet<Vector2> meshTraingleExcludeList) = TerrainVertexUtil.ExtractVertexWorldPositionsAndUVs_V2(
                //         gridLookups,
                //         globalTerrainVertexGrid,
                //         transform
                //     );

                //     if (meshChunkObject == null)
                //     {
                //         meshChunkObject = MeshUtil.InstantiatePrefabWithMesh(worldAreaMeshObjectPrefab, new Mesh(), transform.position);
                //         meshChunkObject.name = "Foundation";
                //         if (folder_Main != null)
                //         {
                //             meshChunkObject.transform.SetParent(folder_Main);
                //         }
                //         else meshChunkObject.transform.SetParent(transform);
                //     }

                //     // Get the MeshFilter component from the instantiatedPrefab
                //     MeshFilter meshFilter = meshChunkObject.GetComponent<MeshFilter>();
                //     MeshCollider meshCollider = meshChunkObject.GetComponent<MeshCollider>();
                //     Mesh finalMesh = meshFilter.sharedMesh;

                //     finalMesh.name = "Terrain Mesh";
                //     finalMesh.Clear();
                //     finalMesh.vertices = vertexPositions;
                //     finalMesh.triangles = WorldAreaManager.GenerateTerrainTriangles_V2(gridLookups, meshTraingleExcludeList);
                //     finalMesh.uv = uvs;

                //     // Refresh Terrain Mesh
                //     finalMesh.RecalculateNormals();
                //     finalMesh.RecalculateBounds();

                //     // Apply the mesh data to the MeshFilter component
                //     meshFilter.sharedMesh = finalMesh;
                //     meshCollider.sharedMesh = finalMesh;
                //     // }
            }
        }
    }



    public static Dictionary<int, Dictionary<Vector2, Vector3>> Generate_TerrainPlatform(
        Vector3 gridCenter,
        int hostCellSize,
        int layerOffset,
        int maxLayers,
        HexCellSizes maxGridSize,
        int foundation_innersMax,
        int foundation_cornersMax,
        int foundation_random_Inners = 40,
        int foundation_random_Corners = 40,
        int foundation_random_Center = 100,
        Dictionary<Vector2, Vector3> cellCenters_ByLookup = null,
        Dictionary<Vector2, Vector3> allCenterPointsAdded = null,
        List<LayeredNoiseOption> layerdNoises_terrain = null,
        float terrainHeight = 1,
        int maxLayerDifference = 2
    )
    {
        int baseLayer = HexCoreUtil.Calculate_CellSnapLayer(layerOffset, gridCenter.y);
        int _maxGridSize = (int)maxGridSize;

        (
            Dictionary<Vector2, Vector3> foundationNodes,
            Dictionary<Vector2, Vector3> bufferNodes
        ) = HexCoreUtil.Generate_FoundationPoints(
            gridCenter,
            hostCellSize,
            foundation_innersMax,
            foundation_cornersMax,
            foundation_random_Inners,
            foundation_random_Corners,
            foundation_random_Center,
            allCenterPointsAdded
        );

        List<Vector3> hostPoints = HexCoreUtil.GenerateHexCenterPoints_X13(gridCenter, hostCellSize);
        HashSet<Vector2> added = new HashSet<Vector2>();
        HashSet<Vector2> excludeList = new HashSet<Vector2>();
        foreach (var k in bufferNodes.Keys)
        {
            excludeList.Add(k);
        }

        Dictionary<int, Dictionary<Vector2, Vector3>> new_buildingBlockClusters = new Dictionary<int, Dictionary<Vector2, Vector3>>();

        new_buildingBlockClusters = HexCoreUtil.Generate_BaseBuildingNodeGroups(
                gridCenter,
                excludeList,
               _maxGridSize
           );


        if (cellCenters_ByLookup == null) cellCenters_ByLookup = new Dictionary<Vector2, Vector3>();
        // if (pathCenters_ByLookup != null) pathCenters_ByLookup = bufferNodes;

        List<int> keys = new_buildingBlockClusters.Keys.ToList();
        bool initalBaseNoisdElevationSet = false;

        foreach (var group in keys)
        {
            List<Vector2> lookups = new_buildingBlockClusters[group].Keys.ToList();
            bool elevationSet = false;
            float groundElevation = 0;

            if (layerdNoises_terrain == null)
            {
                int groundLayer = UnityEngine.Random.Range(baseLayer, baseLayer + (maxLayers + 1));
                groundElevation = (groundLayer * layerOffset);
                elevationSet = true;
            }

            foreach (Vector2 lookup in lookups)
            {
                Vector3 centerPos = new_buildingBlockClusters[group][lookup];
                if (elevationSet == false)
                {
                    groundElevation = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)centerPos.x, (int)centerPos.z, terrainHeight, layerdNoises_terrain);
                    elevationSet = true;

                    if (initalBaseNoisdElevationSet == false)
                    {
                        initalBaseNoisdElevationSet = true;

                        baseLayer = HexCoreUtil.Calculate_CellSnapLayer(layerOffset, groundElevation);
                    }
                    else
                    {
                        int groundLayer = HexCoreUtil.Calculate_CellSnapLayer(layerOffset, groundElevation);

                        int layerDifference = Mathf.Abs(groundLayer - baseLayer);
                        if (layerDifference > maxLayerDifference)
                        {
                            // Debug.Log("(layerDifference > maxLayerDifference), layerDifference: " + layerDifference + ", maxLayerDifference: " + maxLayerDifference);

                            groundLayer = (groundLayer > baseLayer) ? baseLayer + maxLayerDifference : baseLayer - maxLayerDifference;
                            groundElevation = (groundLayer * layerOffset);
                        }
                    }
                }

                centerPos.y = groundElevation;
                new_buildingBlockClusters[group][lookup] = centerPos;

                if (cellCenters_ByLookup.ContainsKey(lookup) == false) cellCenters_ByLookup.Add(lookup, centerPos);
            }
        }

        return new_buildingBlockClusters;
    }


    public static (Vector2, float) GetCloseest_HexLookupInDictionary_withDistance(
        Vector3 position,
        int cellSize,
        Dictionary<Vector2, Vector3> cellCenters_ByLookup
    // bool useV3 = false
    )
    {
        List<Vector2> nearestLookups_X13 = HexCoreUtil.Calculate_ClosestHexLookups_X13(position, cellSize);

        Vector2 nearestLookup = Vector2.positiveInfinity;
        Vector3 nearest = Vector3.positiveInfinity;
        float nearestDistance = float.MaxValue;

        foreach (Vector2 lookup in nearestLookups_X13)
        {
            float dist = VectorUtil.DistanceXZ(position, lookup);
            if (dist < nearestDistance)
            {
                if (cellCenters_ByLookup.ContainsKey(lookup))
                {
                    // if (useV3) dist = Vector3.Distance(position, nearest);
                    nearestLookup = lookup;
                    nearest = cellCenters_ByLookup[lookup];

                    nearestDistance = dist;

                    bool isin = VectorUtil.IsPositionWithinPolygon(HexCoreUtil.GenerateHexagonPoints(nearest, cellSize), position);
                    if (isin || nearestDistance < cellSize * 0.5f) break;
                }
            }
        }
        return (nearestLookup, nearestDistance);
    }

    public static (Vector2, float, float) GetCloseest_HexLookupInDictionary_withDistance_ElevationLerp(
        Vector3 position,
        int cellSize,
        Dictionary<Vector2, Vector3> cellCenters_ByLookup,
        float lerpMult = 0.5f,
        bool useV3 = false
    )
    {
        List<Vector2> nearestLookups_X13 = HexCoreUtil.Calculate_ClosestHexLookups_X13(position, cellSize);
        Vector2 nearestLookup = Vector2.positiveInfinity;
        Vector3 nearest = Vector3.positiveInfinity;
        float nearestDistance = float.MaxValue;
        float sum = 0;
        int found = 0;
        float average = 0.1f;

        foreach (Vector2 lookup in nearestLookups_X13)
        {
            float dist = VectorUtil.DistanceXZ(position, lookup);
            if (cellCenters_ByLookup.ContainsKey(lookup))
            {
                found++;
                sum += cellCenters_ByLookup[lookup].y;

                if (average == 0.1f)
                {
                    average = cellCenters_ByLookup[lookup].y;
                }
                else average = Mathf.Lerp(average, cellCenters_ByLookup[lookup].y, lerpMult);

                if (dist < nearestDistance)
                {
                    if (useV3) dist = Vector3.Distance(position, nearest);

                    nearestDistance = dist;
                    nearestLookup = lookup;
                    nearest = cellCenters_ByLookup[lookup];

                    average = Mathf.Lerp(average, cellCenters_ByLookup[lookup].y, 0.5f);

                    bool isin = VectorUtil.IsPositionWithinPolygon(HexCoreUtil.GenerateHexagonPoints(nearest, cellSize), position);
                    if (isin || nearestDistance < cellSize * 0.5f)
                    {
                        average = cellCenters_ByLookup[lookup].y;
                        break;
                    }
                }
            }
        }
        // float average = (found > 0) ? (sum / found) : 0;
        return (nearestLookup, nearestDistance, average);
    }

    public static Vector2[,] Generate_TerrainFoundationVertices(
        Bounds bounds,
        Transform transform,
        float steps,
        HexCellSizes cellSize,
        Dictionary<Vector2, Vector3> cellCenters_ByLookup,
        Dictionary<Vector2, Vector3> allCenterPointsAdded,
        Dictionary<Vector2, Vector3> pathCenters,
        List<LayeredNoiseOption> layerdNoises_terrain,
        Dictionary<Vector2, TerrainVertex> gridPoints,
        float terrainHeight,
        float groundCellInfluenceRadiusMult = 1.3f,
        float bufferZoneLerpMult = 0.5f,
        int cellLayerOffset = 2,
        int treeStep = 4,
        int seaLevel = 0
    )
    {
        int worldSpaceSize = (int)HexCellSizes.Worldspace;
        int _cellSize = (int)cellSize;

        // Calculate the minimum x and z positions that are divisible by steps
        float minX = Mathf.Floor(bounds.min.x / steps) * steps;
        float minZ = Mathf.Floor(bounds.min.z / steps) * steps;

        // Calculate the number of steps along the x and z axis based on the spacing
        int xSteps = Mathf.CeilToInt((bounds.max.x - minX) / steps);
        int zSteps = Mathf.CeilToInt((bounds.max.z - minZ) / steps);

        if (gridPoints == null) gridPoints = new Dictionary<Vector2, TerrainVertex>();
        Vector2[,] gridLookups = new Vector2[xSteps + 1, zSteps + 1];

        // Dictionary<Vector2, List<Vector3>> treeSpawnPoints_byWorldspace = new Dictionary<Vector2, List<Vector3>>();

        Vector3 currentTrackPos = Vector3.zero;
        // List<Vector3> closestWorldspaceCellCenters = new List<Vector3>();
        // Vector2 closest_WorldspaceLookup = Vector2.positiveInfinity;
        // Vector2 closest_WorldspaceTerraformLookup = Vector2.positiveInfinity;
        // Vector3 closest_WorldspaceTerraformPos = Vector3.positiveInfinity;

        Vector2 closest_subCellTerraformLookup = Vector2.positiveInfinity;
        int checkingLayer = int.MaxValue;
        float noiseBias = 0.5f;
        float previousGroundHeight = float.MaxValue;

        HashSet<CellStatus> includeCellStatusList = new HashSet<CellStatus>() {
                CellStatus.FlatGround
            };

        int iterations = 0;
        int added = 0;

        // Loop through each vertex in the grid
        for (int z = 0; z <= zSteps; z++)
        {
            for (int x = 0; x <= xSteps; x++)
            {
                iterations++;

                // Calculate the x and z coordinates of the current grid point
                float xPos = minX + x * steps;
                float zPos = minZ + z * steps;

                Vector3 position = new Vector3(xPos, 0, zPos);


                Vector3 worldCoord = transform.TransformVector(position);
                Vector3 rounded = VectorUtil.RoundVector3To1Decimal(position);
                Vector2 aproximateCoord = new Vector2(rounded.x, rounded.z);
                Vector2Int noiseCoordinate = new Vector2Int((int)worldCoord.x, (int)worldCoord.z);

                // float worldspaceBaseNoiseHeight = float.MaxValue;
                float baseNoiseHeight = 0;
                bool terraformed = false;
                // bool canPlaceTree = (closest_WorldspaceLookup != Vector2.positiveInfinity);
                bool markedForRemoval = false;
                bool updateBufferCell = false;

                // Evaluate nearest subcell
                (Vector2 nearestLookup, float nearestDist) = GetCloseest_HexLookupInDictionary_withDistance(
                    position,
                    _cellSize,
                    cellCenters_ByLookup
                // allCenterPointsAdded
                );

                if (nearestLookup != null && nearestLookup != Vector2.positiveInfinity && nearestDist != float.MaxValue)
                {
                    // Debug.Log("nearestLookup: " + nearestLookup + ",  nearestDist: " + nearestDist);
                    bool isPath = false; // cellCenters_ByLookup.ContainsKey(nearestLookup) == false;

                    Vector3 nearestHexCenter = isPath ? allCenterPointsAdded[nearestLookup] : cellCenters_ByLookup[nearestLookup];

                    // float cellBaseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)nearestHexCenter.x, (int)nearestHexCenter.z, terrainHeight, layerdNoises_terrain);
                    float cellNoiseHeight = nearestHexCenter.y;

                    if (isPath == false) previousGroundHeight = cellNoiseHeight;

                    // bool isPath = false; //nearestGroundCell.IsPath();  // false; //hNoiseValue > locationNoise_pathNoiseMin);

                    if (nearestDist < (_cellSize * groundCellInfluenceRadiusMult) && !isPath)
                    {
                        // baseNoiseHeight = UtilityHelpers.RoundHeightToNearestElevation(cellNoiseHeight, cellLayerElevation);
                        baseNoiseHeight = cellNoiseHeight;
                        terraformed = true;
                        // canPlaceTree = false;
                    }
                    else
                    {
                        // Debug.Log("nearestLookup: " + nearestLookup + ",  nearestDist: " + nearestDist);
                        Vector3 closestHexCenter_X4 = HexCoreUtil.Calculate_ClosestHexCenter_V2(position, 4);
                        float hexNoiseX4 = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)closestHexCenter_X4.x, (int)closestHexCenter_X4.z, terrainHeight, layerdNoises_terrain);
                        baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate(noiseCoordinate.x, noiseCoordinate.y, terrainHeight, layerdNoises_terrain);
                        baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, hexNoiseX4, 0.5f);


                        if (nearestDist < _cellSize * 6.6f)
                        {
                            baseNoiseHeight += transform.position.y;

                            // if (baseNoiseHeight < cellNoiseHeight) baseNoiseHeight += noiseBias;
                            // else if (baseNoiseHeight > cellNoiseHeight) baseNoiseHeight -= noiseBias;

                            // float outterEdgeMult = (groundCellInfluenceRadiusMult + 0.33f);
                            // if (nearestDist < (_cellSize * outterEdgeMult))
                            // {
                            //     baseNoiseHeight = Mathf.Clamp(baseNoiseHeight, cellNoiseHeight - 0.9f, cellNoiseHeight + 0.9f);
                            // }
                            // else baseNoiseHeight = UtilityHelpers.RoundToNearestStep(baseNoiseHeight, 0.45f);

                            // float distRadiusMod = 4f;
                            // float distMult = (1.01f - Mathf.Clamp01(nearestDist / (_cellSize * distRadiusMod)));
                            // baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, cellNoiseHeight, distMult);

                            // float roundedValue = UtilityHelpers.RoundHeightToNearestElevation(baseNoiseHeight, cellLayerOffset);
                            // baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, roundedValue, distMult);
                            // float roundedValue = Mathf.Lerp(baseNoiseHeight, cellNoiseHeight, distMult);
                            // baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, roundedValue, 0.2f);

                            // baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, cellNoiseHeight, distMult);

                            baseNoiseHeight = Mathf.Lerp(cellNoiseHeight, baseNoiseHeight, 0.99f);
                            // int lerps = 1;
                            // for (int i = 0; i < lerps; i++)
                            // {
                            // baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, cellNoiseHeight, bufferZoneLerpMult);
                            // }

                            (Vector2 nearestPathLookup, float nearestPathDist) = GetCloseest_HexLookupInDictionary_withDistance(
                                position,
                                4,
                                pathCenters
                            );

                            isPath = (
                                nearestPathLookup != Vector2.positiveInfinity
                                && nearestPathDist != float.MaxValue
                                && (nearestPathDist < (4 * 4.0f))
                            );

                            if (isPath)
                            {

                                Vector3 pathCenter = pathCenters[nearestPathLookup];
                                // float pathNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)pathCenter.x, (int)pathCenter.z, terrainHeight, layerdNoises_terrain);
                                // pathNoiseHeight = Mathf.Lerp(pathNoiseHeight, cellNoiseHeight, 0.3f);
                                float pathNoiseHeight = pathCenters[nearestPathLookup].y;

                                if (nearestPathDist < (4 * 0.8f))
                                {
                                    // baseNoiseHeight = pathNoiseHeight;
                                    baseNoiseHeight = Mathf.Lerp(pathNoiseHeight, baseNoiseHeight, 0.6f);

                                    terraformed = true;
                                }
                                else
                                {
                                    pathNoiseHeight = Mathf.Lerp(cellNoiseHeight, pathNoiseHeight, bufferZoneLerpMult);
                                    baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, pathNoiseHeight, bufferZoneLerpMult);
                                }
                                terraformed = true;
                            }
                            // else
                            // {
                            //     baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, cellNoiseHeight, bufferZoneLerpMult);
                            // }

                        }
                        else
                        {

                            baseNoiseHeight += transform.position.y;

                            if (baseNoiseHeight < cellNoiseHeight) baseNoiseHeight += noiseBias;
                            else if (baseNoiseHeight > cellNoiseHeight) baseNoiseHeight -= noiseBias;

                            baseNoiseHeight = UtilityHelpers.RoundToNearestStep(baseNoiseHeight, 0.4f);

                            baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, cellNoiseHeight, 0.9f);
                        }
                    }
                }
                else
                {
                    // if (worldspaceBaseNoiseHeight != float.MaxValue)
                    // {
                    baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate(noiseCoordinate.x, noiseCoordinate.y, terrainHeight, layerdNoises_terrain);

                    // if (baseNoiseHeight < worldspaceBaseNoiseHeight) baseNoiseHeight += 0.3f;
                    // else if (baseNoiseHeight > worldspaceBaseNoiseHeight) baseNoiseHeight -= 0.3f;

                    baseNoiseHeight += transform.position.y;

                    baseNoiseHeight = UtilityHelpers.RoundToNearestStep(baseNoiseHeight, 0.3f);
                    // }
                }

                // terraformed = true;


                if (!terraformed)
                {
                    baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate(noiseCoordinate.x, noiseCoordinate.y, terrainHeight, layerdNoises_terrain);
                    baseNoiseHeight += transform.position.y;
                }

                position.y = baseNoiseHeight;


                // if (canPlaceTree)
                // {
                //     if ((z % treeStep == 0 || x % treeStep == 0) && UnityEngine.Random.Range(0, 100) < 30)
                //     {
                //         Vector3 treePos = position;
                //         treePos.y -= 0.35f;
                //         treeSpawnPoints_byWorldspace[closest_WorldspaceLookup].Add(treePos);
                //     }
                // }

                // Create the TerrainVertex object
                TerrainVertex vertex = new TerrainVertex()
                {
                    noiseCoordinate = noiseCoordinate,
                    aproximateCoord = aproximateCoord,
                    position = position,
                    index_X = x,
                    index_Z = z,

                    type = VertexType.Generic,
                    markedForRemoval = markedForRemoval,
                    isInHexBounds = false,

                    worldspaceOwnerCoordinate = Vector2.positiveInfinity,
                    parallelWorldspaceOwnerCoordinate = Vector2.positiveInfinity,
                    parallelVertexIndex_X = -1,
                    parallelVertexIndex_Z = -1,
                };

                // Add the vertex to the grid
                gridPoints[aproximateCoord] = vertex;
                gridLookups[x, z] = aproximateCoord;

                added++;
            }
        }

        // Debug.Log("gridPoints: " + gridPoints.Count + ", iterations: " + iterations + ",  added: " + added);
        return gridLookups;
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.5f);


        if (enableinternakCellTracker)
        {
            Debug_EvaluatePosition();

            if (debug_currentHexCell != null)
            {
                Gizmos.color = Color.red;
                VectorUtil.DrawHexagonPointLinesInGizmos(debug_currentHexCell.cornerPoints);
            }

            if (debug_terrainPlatform && buildingBlockClusters != null)
            {

                if (show_gridBounds)
                {
                    Gizmos.color = Color.white;
                    VectorUtil.DrawRectangleLines(activeGridBounds);
                }

                bool doOnce = false;

                if (show_foundationNodes)
                {
                    foreach (var item in buildingBlockClusters.Values)
                    {
                        bool highlight = false;

                        if (!doOnce)
                        {
                            doOnce = true;
                            highlight = true;
                        }

                        foreach (var pos in item.Values)
                        {
                            // Gizmos.DrawSphere(pos, size / 2);

                            Gizmos.color = Color.yellow;
                            VectorUtil.DrawHexagonPointLinesInGizmos(pos, (int)debug_currentCellSize / 3, false);

                            if (show_groundCellInfluanceRadius)
                            {
                                Gizmos.color = Color.blue;
                                Gizmos.DrawWireSphere(pos, 12 * groundCellInfluenceRadiusMult);

                                float outterEdgeMult = (groundCellInfluenceRadiusMult + 0.33f);
                                Gizmos.color = Color.white;
                                Gizmos.DrawWireSphere(pos, 12 * outterEdgeMult);
                            }


                            // Gizmos.color = Color.white;
                            // Vector3 dimensions = new Vector3(defaultBuilding_dimensions.x, UnityEngine.Random.Range(1, 6) * foundation_layerOffset, defaultBuilding_dimensions.z);
                            // RectangleBounds rect = new RectangleBounds(pos, defaultBuilding_size, UnityEngine.Random.Range(0, 6), dimensions);
                            // rect.Draw();

                            // Gizmos.color = highlight ? Color.red : Color.black;
                            // if (highlight) Gizmos.DrawSphere(pos, size / 3);
                        }
                        // break;
                    }
                }

                doOnce = false;
                if (buildingClusters != null)
                {
                    foreach (var item in buildingClusters.Values)
                    {
                        Vector3 clusterCenter = VectorUtil.Calculate_CenterPositionFromPoints(item.Values.ToList());
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawSphere(clusterCenter, 3);

                        // bool highlight = false;
                        // if (!doOnce)
                        // {
                        //     doOnce = true;
                        //     highlight = true;
                        // }

                        // foreach (var pos in item.Values)
                        // {
                        //     // Gizmos.color = Color.yellow;
                        //     // VectorUtil.DrawHexagonPointLinesInGizmos(pos, debug_alternateCell_size / 3, false);

                        //     Gizmos.color = highlight ? Color.red : Color.black;
                        //     if (highlight) Gizmos.DrawSphere(pos, 12 / 3);
                        // }
                        // break;
                    }
                }

                if (enable_BuildingPrototypes && buildingPrototypes != null)
                {
                    foreach (var item in buildingPrototypes.Values)
                    {
                        item.Draw(buildingPrototypeDisplaySettings);
                        //Temp
                        break;
                    }
                }


                if (show_bufferPath && pathCenters != null)
                {
                    foreach (var item in pathCenters.Values)
                    {
                        Gizmos.color = Color.green;
                        // Vector3 pos = item;
                        // float baseNoiseHeight = transform.position.y + (terrainHeightDefault * 0.3f);
                        // pos.y = baseNoiseHeight;

                        // Gizmos.DrawSphere(pos, 1);
                        VectorUtil.DrawHexagonPointLinesInGizmos(item, 4);
                    }
                }

                if (show_overPathCenters && overPathCenters != null)
                {
                    foreach (var item in overPathCenters.Values)
                    {
                        Gizmos.color = Color.magenta;
                        // Vector3 pos = item;
                        // float baseNoiseHeight = transform.position.y + (terrainHeightDefault * 0.3f);
                        // pos.y = baseNoiseHeight;

                        // Gizmos.DrawSphere(pos, 1);
                        VectorUtil.DrawHexagonPointLinesInGizmos(item, 12);
                    }
                }

                if (show_buildingBoundsShells && buildingBoundsShells != null)
                {
                    foreach (var item in buildingBoundsShells)
                    {
                        Gizmos.color = Color.white;
                        item.Draw();
                    }
                }
                // if (allCenterPointsAdded != null)
                // {
                //     foreach (var k in allCenterPointsAdded.Keys)
                //     {
                //         if (cellCenters_ByLookup.ContainsKey(k)) continue;

                //         Gizmos.color = Color.green;
                //         Vector3 pos = allCenterPointsAdded[k];

                //         float baseNoiseHeight = transform.position.y + LayerdNoise.Calculate_NoiseHeightForCoordinate((int)pos.x, (int)pos.z, terrainHeightDefault, layeredNoise_terrainGlobal);
                //         pos.y = baseNoiseHeight;

                //         Gizmos.DrawSphere(pos, 1f);
                //     }
                // }

                // if (worldspaceChunkCenters != null)
                // {
                //     foreach (var item in worldspaceChunkCenters)
                //     {
                //         Gizmos.color = Color.green;
                //         Gizmos.DrawSphere(item, 2f);
                //     }
                // }


                if (showGlobalVertexGrid && globalTerrainVertexGrid != null)
                {
                    // foreach (var vertex in globalTerrainVertexGrid.Values)
                    // {
                    //     float rad = 0.33f;
                    //     Gizmos.color = Color.black;
                    //     Gizmos.DrawSphere(vertex.position, rad);
                    //     // TerrainVertexUtil.DisplayTerrainVertex(vertex, ShowVertexState.All, transform);
                    // }

                    if (allGridChunksWithCenters != null)
                    {
                        foreach (var item in allGridChunksWithCenters)
                        {
                            // Vector2 chunkCenterLookup = TerrainChunkData.CalculateTerrainChunkLookup(gridChunksWithCenterPos[i].Item2);
                            Vector2[,] gridChunkKeys = item.Item1;

                            foreach (var k in gridChunkKeys)
                            {
                                if (globalTerrainVertexGrid.ContainsKey(k) == false)
                                {
                                    // Debug.LogError("Missing key: " + k);

                                    Gizmos.color = Color.magenta;
                                    Gizmos.DrawSphere(new Vector3(k.x, 0, k.y), 1f);
                                    continue;
                                }

                                TerrainVertexUtil.DisplayTerrainVertex(globalTerrainVertexGrid[k], ShowVertexState.All, transform);
                            }
                        }
                    }
                }



            }

        }
    }
}
