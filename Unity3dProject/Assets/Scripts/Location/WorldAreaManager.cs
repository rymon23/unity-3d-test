using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using WFCSystem;
using Newtonsoft.Json;
using UnityEngine.AI;
using Unity.Collections;
using Unity.Jobs;
using System.Threading.Tasks;
using System.Threading;

// Database Port: 5432
// Database Superuser: postgres

namespace ProceduralBase
{
    enum WorldExpansionType { Cluster = 0, Directional }

    [RequireComponent(typeof(WorldLocationManager))]
    public class WorldAreaManager : MonoBehaviour
    {
        #region Singleton
        private static WorldAreaManager _instance;

        public static WorldAreaManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<WorldAreaManager>();
                }
                return _instance;
            }
        }
        private void Awake()
        {
            // Make sure only one instance of WorldAreaManager exists in the scene
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            InitialSetup();
        }
        #endregion

        WorldLocationManager _worldLocationManager;

        #region World Cell Data
        Dictionary<string, Vector2> _worldRegionsLookupById = new Dictionary<string, Vector2>();
        Dictionary<Vector2, HexagonCellPrototype> _worldRegionsLookup = new Dictionary<Vector2, HexagonCellPrototype>();
        Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> _worldAreas_ByRegion = new Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>>();
        Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> _worldSpaces_ByArea = new Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>>();
        Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> _subCellTerraforms_ByWorldspace = new Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>>();
        Dictionary<Vector2, GameObject> _worldspaceTerrainChunks_ByLookup = new Dictionary<Vector2, GameObject>();
        Dictionary<Vector2, TerrainChunkData> _terrainChunkData_ByLookup = new Dictionary<Vector2, TerrainChunkData>();
        Dictionary<Vector2, Dictionary<Vector2, WorldspaceData>> _worldspaceData_ByArea = new Dictionary<Vector2, Dictionary<Vector2, WorldspaceData>>();

        private Dictionary<Vector2, HexagonGrid> _worldSpaceCellGridByCenterCoordinate = new Dictionary<Vector2, HexagonGrid>();
        Dictionary<Vector2, Dictionary<int, Dictionary<Vector2, Vector3>>> temp_cellCenters_ByLookup_BySize_ByWorldSpace = new Dictionary<Vector2, Dictionary<int, Dictionary<Vector2, Vector3>>>();
        Dictionary<Vector2, Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>> cellLookup_ByLayer_BySize_ByWorldSpace = new Dictionary<Vector2, Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>>();

        private Dictionary<Vector2, Vector3> _worldSpaceCellGridBaseCenters = new Dictionary<Vector2, Vector3>();
        private Dictionary<Vector2, Dictionary<int, List<Vector3>>> _worldSpaceCellGridBaseCentersBySize = new Dictionary<Vector2, Dictionary<int, List<Vector3>>>();

        private Dictionary<Vector2, TerrainVertex> _globalTerrainVertexGridByCoordinate = new Dictionary<Vector2, TerrainVertex>();
        private Vector2[,] _globalTerrainVertexGridCoordinates;
        private Dictionary<Vector2, Vector2[,]> _globalTerrainVertexKeysByWorldspaceCoordinate = new Dictionary<Vector2, Vector2[,]>();
        private Dictionary<Vector2, TerrainVertex[,]> _terrainVertexGridDataByCenterCoordinate = new Dictionary<Vector2, TerrainVertex[,]>();

        private Dictionary<Vector2, Dictionary<Vector2, LocationPrefab>> _locationPrefabs_ByWorldspace_ByArea = new Dictionary<Vector2, Dictionary<Vector2, LocationPrefab>>();
        private Dictionary<Vector2, Dictionary<Vector2, LocationData>> _locations_ByWorldspace_ByArea = new Dictionary<Vector2, Dictionary<Vector2, LocationData>>();
        #endregion


        [SerializeField] private Vector2 _currentWorldPos_WorldspaceLookup = Vector2.zero;
        [SerializeField] private Vector2 _currentWorldPos_AreaLookup = Vector2.zero;
        [SerializeField] private Vector2 _currentWorldPos_RegionLookup = Vector2.zero;
        [Header(" ")]
        [SerializeField] private List<Vector2> _activeWorldspaceLookups = new List<Vector2>();
        private List<HexagonCellPrototype> _activeWorldspaceCells = new List<HexagonCellPrototype>();
        [SerializeField] private List<GameObject> _activeWorldspaceTerrainChunks = new List<GameObject>();
        private List<HexagonCellPrototype> _preassignedWorldspaceCells = new List<HexagonCellPrototype>();

        [Header("Debug Settings")]
        [Header(" ")]
        [SerializeField] private bool trigger_reset = false;
        [SerializeField] private bool trigger_reset_build = false;

        [Header("Expansion")]
        [SerializeField] private bool trigger_expand_World = false;
        [Range(-1, 99)] public int temp_areaChunksMax = 2;
        [Range(1, 99)] public int temp_areasMax = 2;
        [SerializeField] private bool instantiateLocations;
        [SerializeField] private bool instantiateTrees;
        // [SerializeField] private bool instantiateTunnnels;

        [Header(" ")]
        [SerializeField] private bool trigger_generate_CurrentAreaLocations = false;
        // [SerializeField] private bool trigger_expand_WorldSpaceCells = false;
        // [Header(" ")]
        [Header(" ")]
        [SerializeField] private bool trigger_generate_WorldSpaces = false;
        [SerializeField] private bool trigger_generate_WorldAreas = false;
        [SerializeField] private bool trigger_generate_WorldRegions = false;

        [Header(" ")]
        [SerializeField] private bool trigger_EvaluateLocationMarkers;
        [Header(" ")]
        [SerializeField] private bool trigger_GenerateTrees;

        [Header(" ")]
        [SerializeField] private bool updateGlobalWorldElevationOffset = false;

        [Header(" ")]
        [SerializeField] private bool showCellGrids;
        [SerializeField] private CellDisplay_Type cellDisplayType = CellDisplay_Type.DrawCenterAndLines;

        [SerializeField] private bool showHighlights;
        [SerializeField] private GridFilter_Level gridFilter_Level;
        [SerializeField] private GridFilter_Type gridFilter_Type;
        [SerializeField] private HexCellSizes gridFilter_size = HexCellSizes.Default;

        [Header(" ")]
        [SerializeField] private bool show_Locations;

        [Header("World Cells")]
        [SerializeField] private bool show_LocalCellHierarchyOnly;
        [Header(" ")]
        [SerializeField] private bool show_SubCells;
        [SerializeField] private bool show_Worldspaces;
        [SerializeField] private bool show_WorldAreas;
        [SerializeField] private bool show_WorldRegions;
        [SerializeField] private bool show_WorldBounds;
        [SerializeField] private bool show_NoiseHighlights;
        [Header("Area Chunks")]
        [SerializeField] private bool show_AreaChunks;
        [Range(4, 10)][SerializeField] private int areaChunks = 4;
        [Range(0f, 2f)][SerializeField] private float areaChunkOverlap = 0;

        [Header("Terrain Chunks")]
        [SerializeField] private bool show_TerrainChunkGrid;
        [SerializeField] private bool show_TerrainChunkLookups;

        [Header("Tracking / Loading")]
        [SerializeField] private bool enable_WorldPositionTracking;
        [SerializeField] private bool show_WorldPositionTracking;
        [SerializeField] private bool show_loadTrackerCells;
        [SerializeField] private bool show_LoadedWorldspaces;

        [Header(" ")]
        [SerializeField] private bool showGlobalVertexGrid;
        [SerializeField] private bool showGridChunks;
        [SerializeField] private ShowVertexState showVerticesType;
        [Header(" ")]

        [SerializeField] private bool showPathGrid;
        public int temp_gridPath_columns = 4;
        public int temp_gridPath_rows = 4;
        public int temp_gridPath_lineDensity = 5;
        public float temp_gridPathRadius = 5f;

        public float temp_gridPathCellWidth = 12f;
        public float temp_gridPathCellHeight = 12f;

        [Header(" ")]
        [SerializeField] private bool showPreAssignedWorldSpaces;

        [Header("Global World Space Settings")]
        [Range(1, 296)][SerializeField] private int _minWorldSpaces = 7;
        [Range(0, 296)][SerializeField] private int _minWorldAreas;
        // [Range(0, 296)][SerializeField] private int _minWorldRegions;
        private int _currentRadius = 108;
        // [Header("World Sizing")]
        private int _worldspaceSize = 108;
        private int _worldAreaSize = 2916;
        private int _worldRegionSize = 78732;

        [Header("Location Noise")]
        [SerializeField] private List<LayeredNoiseOption> layeredNoise_terrainGlobal;
        [Header(" ")]
        [SerializeField] private List<LayeredNoiseOption> layeredNoise_locationMarker;
        [SerializeField] private LayeredNoiseOption layeredNoise_locationPath;
        [SerializeField] private LayeredNoiseOption layeredNoise_regionPath;
        [Range(0.1f, 1.5f)][SerializeField] private float locationNoise_pathNoiseMin = 0.9f;

        [Header(" ")]
        [Range(-1f, 10f)][SerializeField] private float locationNoise_RangeMin = 0f;
        [Range(-1f, 10f)][SerializeField] private float locationNoise_RangeMax = 1f;
        [Header(" ")]
        [Range(0.1f, 1f)][SerializeField] private float locationNoise_terrainBlendMult = 0.6f;
        [SerializeField] private float locationNoise_weightMult = 1f;
        [SerializeField] private bool evaluate_NoiseRanges;
        [SerializeField] private bool randomize_ColorAssignment;
        [Header(" ")]
        [Range(0, 10)][SerializeField] private int debug_currentLocationNoiseIndex = 0;
        [SerializeField] private List<Vector2> locationNoise_ranges = new List<Vector2>();
        [Header(" ")]

        [Header("Location Marker Settings")]
        [SerializeField] private List<LocationPrefab> locationMarkerPrefabOptions;
        [SerializeField] private List<LocationPrefab> locationMarkerPrefabs_ToPreAssign;
        [SerializeField] private List<LocationPrefab> locationMarkerPrefabs_Default;

        private int locationsPerWorldMax = 6;

        [Header("Global Cell Grid Settings")]
        [SerializeField] private HexCellSizes _global_defaultCellSize = HexCellSizes.Default;
        [Range(2, 48)][SerializeField] private int cellLayersMax = 12;
        [Range(2, 12)][SerializeField] private int cellLayerElevation = 3;

        [Header(" ")]
        [Range(0, 12f)][SerializeField] private float viableFlatGroundCellSteepnessThreshhold = 4.5f;

        [Header("Terrain Settings")]

        [Header("Global Height")]
        [Range(-32, 768)][SerializeField] private float _global_terrainHeightDefault = 178f;
        [Header(" ")]
        [SerializeField] private bool randomizeTerrainHeight;
        [Header(" ")]
        [Range(-32, 768)] public float _global_terrainHeightMin = 72f;
        [Range(56, 768)] public float _global_terrainHeightMax = 364f;
        [SerializeField] private bool useRandomTerrainHeight;

        [Header("Global Elevation ")]
        [Range(-2, 12f)][SerializeField] private float _global_worldElevationOffset = 1f;
        [SerializeField] private float _global_worldElevationOffsetMax = 12f;
        [SerializeField] private float _global_cellGridElevation = 1f;
        [SerializeField] private float _globalSeaLevel = 0f;

        [Header("Vertex Settings")]
        [Range(1, 5)][SerializeField] int vertexDensity = 3;
        [Range(3, 25)][SerializeField] int vertexTreeStep = 7;

        // [Header("Procedural Placement")]
        // [Range(0.01f, 1f)][SerializeField] private float placement_density = 0.75f;

        [Header("Terrain Noise Settings")]
        [Range(-1.3f, 0.45f)][SerializeField] private float persistence = -0.816f;
        [Range(-1f, 2.6f)][SerializeField] private float lacunarity = 2f;
        [Range(1f, 128f)][SerializeField] private int octaves = 6;

        [Range(1f, 6f)][SerializeField] float cellVertexSearchRadiusMult = 1.4f;
        [Range(3f, 108f)][SerializeField] float gridEdgeSmoothingRadius = 48;
        [Range(0.05f, 1.5f)][SerializeField] private float groundSlopeElevationStep = 0.45f;

        // [Header("Smoothing Settings")]
        // [Range(0, 1f)][SerializeField] float smoothingFactor = 1f;
        // [Range(1, 24)][SerializeField] int smoothingNeighborDepth = 4;
        // [Range(1, 24)][SerializeField] int cellGridVertexWeight = 3;
        // [Range(1, 24)][SerializeField] int cellGridVertexNeighborEvaluationDepth = 8;
        // [Range(1, 24)][SerializeField] int inheritedVertexWeight = 4;
        // [Header(" ")]
        // [Range(1f, 3f)][SerializeField] private float blendRadiusMult = 1.6f;

        // [Range(0f, 100f)][SerializeField] float smoothingSigma = 0.5f;

        // [Header("Tunnel Settings")]
        // [SerializeField] private bool allowTunnels = true;
        // [Range(1, 24)][SerializeField] private int maxTunnelMemberSize = 8;

        [Header("Mesh Chunking")]
        private Vector2 terrainChunkSizeXZ = new Vector2(164, 95);
        [Header(" ")]
        [SerializeField] private Vector2 meshChunkSize = new Vector2(162f, 93.5f);
        private int _currentMeshChunkDivider;
        private int maxChunkSize = 200;

        [Header("Terrain Texturing")]
        [SerializeField] private TerrainType[] terrainTypes;
        [SerializeField] private TerrainType[] pathTypes;

        [Header("World Noise")]
        [SerializeField] private FastNoiseUnity globalNoise;
        [SerializeField] private IWFCSystem wfc;

        [Header("World Tracking / Dynamic Loading")]
        [SerializeField] private Transform _currentFocusPoint = null;
        [SerializeField] private WorldPositionTracker _positionTracker = null;
        public Vector3 GetCurrentWorldFocusPosition() => _currentFocusPoint.position;

        [SerializeField] private float global_trackerUpdateDistanceMin = 12f;
        [SerializeField] private float global_worldspaceLoadRadius = 272f;
        [SerializeField] private Vector3 _worldTrackerLastPos;
        [SerializeField] private List<WorldAreaObjectData> _worldAreaObjectData = null;
        // [SerializeField] private List<WorldRegionObjectData> _worldRegionObjectDatas = null;

        [Header("Navmesh")]
        [SerializeField] private bool bakeNavmesh;
        [SerializeField] private NavMeshSurface navMeshSurface;

        [Header("Prefabs")]
        [SerializeField] private GameObject worldAreaMeshObjectPrefab;
        [SerializeField] private GameObject tunnelPrefab;
        [SerializeField] private GameObject testTreePrefab;
        [SerializeField] private HexagonTileCore wfcClusterPrefab;

        [Header(" ")]
        private List<HexagonCellPrototype> _locationMarkerCells;
        DateTime _buildStartTime;
        Dictionary<Vector2, Vector3> temp_globalGrid;
        List<Vector2> temp_chunkCenters;
        private List<Vector2[,]> _gridChunks;


        [Header("Folders")]
        private Transform _worldspaceFolder = null;

        public Transform WorldParentFolder()
        {
            Evalaute_WorldParentFolder();
            return _worldspaceFolder;
        }
        private void Evalaute_WorldParentFolder()
        {
            if (_worldspaceFolder == null)
            {
                _worldspaceFolder = new GameObject("World").transform;
                _worldspaceFolder.transform.SetParent(_worldspaceFolder);
            }
        }

        private void TimeLog(string str)
        {
            UtilityHelpers.LogTime(_buildStartTime, str);
        }

        private void InitialSetup()
        {
            _worldLocationManager = GetComponent<WorldLocationManager>();
        }

        private void FixedUpdate()
        {
            if (enable_WorldPositionTracking && Application.isPlaying)
            {
                Evaluate_TrackedPosition();
            }
        }

        public HexagonCellPrototype GetCurrentWorldSpaceCell() => _worldSpaces_ByArea[_currentWorldPos_AreaLookup][_currentWorldPos_WorldspaceLookup];
        public HexagonCellPrototype GetCurrentWorldAreaCell() => _worldAreas_ByRegion[_currentWorldPos_RegionLookup][_currentWorldPos_AreaLookup];

        public List<Vector2> GetCurrentWorldAreaNeighborLookups()
        {
            HexagonCellPrototype current_AreaCell = GetCurrentWorldAreaCell();
            List<Vector2> calculatedAreaNeighborLookups = HexCoreUtil.GenerateNeighborLookupCoordinates(current_AreaCell.center, current_AreaCell.size);
            List<Vector2> results = new List<Vector2>();

            foreach (Vector2 areaLookup in calculatedAreaNeighborLookups)
            {
                if (_worldAreas_ByRegion[_currentWorldPos_RegionLookup].ContainsKey(areaLookup) == false) continue;
                if (results.Contains(areaLookup)) results.Add(areaLookup);
            }
            return results;
        }

        public HexagonGrid GetWorldSpaceCellGrid(Vector2 lookup)
        {
            if (_worldSpaceCellGridByCenterCoordinate.ContainsKey(lookup) == false)
            {
                Debug.LogError("No WorldSpace Cell Grid at lookup coordinate: " + lookup);
                return null;
            }
            return _worldSpaceCellGridByCenterCoordinate[lookup];
        }

        public List<HexagonCellPrototype> World_GetAllViableCellsAndCenterPoints(Dictionary<Vector2, HexagonGrid> cellGrid_ByWorldSpace)
        {
            List<HexagonCellPrototype> results = new List<HexagonCellPrototype>();
            foreach (var kvp in cellGrid_ByWorldSpace)
            {
                Dictionary<int, List<HexagonCellPrototype>> cellGrid = kvp.Value.GetDefaultPrototypesByLayer();
                int totalFoundInWS = 0;

                foreach (var kvpB in cellGrid)
                {
                    List<HexagonCellPrototype> found = kvpB.Value.FindAll(c =>
                        c.IsFlatGround() &&
                        c.GetCellStatus() != CellStatus.Remove &&
                        c.IsUnderWater() == false &&
                        c.center.y >= _globalSeaLevel
                    );

                    if (found.Count > 0)
                    {
                        results.AddRange(found);
                        totalFoundInWS++;
                    }
                }
                // Debug.Log("total available cells Found In WS - " + kvp.Key + ":  " + totalFoundInWS);
            }
            return results;
        }

        #region Expansion

        public WorldAreaObjectData GetWorldAreaObjectData(Vector2 areaLookup)
        {
            if (_worldAreaObjectData == null || _worldAreaObjectData.Count == 0)
            {
                Debug.LogError("No _worldAreaObjectData");
                return null;
            }
            WorldAreaObjectData data = _worldAreaObjectData.Find(w => w.worldAreaLookup == areaLookup);
            return data;
        }

        public GameObject Get_TerrainChunkObjectByIndexData_V2(Vector2 chunkLookup, bool doNotCreate = false)
        {
            Vector2 areaLookup;
            WorldAreaObjectData data = null;

            if (_terrainChunkData_ByLookup.ContainsKey(chunkLookup) == false)
            {
                Debug.LogError("_terrainChunkData_ByLookup does NOT contain chunkLookup: " + chunkLookup);
                return null;
            }

            areaLookup = _terrainChunkData_ByLookup[chunkLookup].worldAreaLookup;
            data = GetWorldAreaObjectData(areaLookup);

            TerrainChunkData chunkData = _terrainChunkData_ByLookup[chunkLookup];
            // Debug.Log("chunkData found for chunkLookup: " + chunkLookup);
            // if (chunkData.objectIndex < 0 || chunkData.objectIndex > 800) Debug.LogError("chunkData.objectIndex: " + chunkData.objectIndex);
            // Debug.LogError("chunkLookup: " + chunkLookup + ",  chunkData.objectIndex: " + _terrainChunkData_ByLookup[chunkLookup].objectIndex);

            int objectIndex = chunkData.objectIndex;
            // if (objectIndex > -1 && objectIndex < data.terrainChunks.Count) return data.terrainChunks[objectIndex];

            if (objectIndex > -1 && objectIndex < data.terrainChunks.Count)
            {
                // return data.terrainChunks[objectIndex];
            }
            else if (objectIndex > -1) UtilityHelpers.ExpandListToFitIndex(objectIndex, data.terrainChunks);

            if (objectIndex > -1)
            // if (!doNotCreate && objectIndex > -1)
            {
                UtilityHelpers.ExpandListToFitIndex(objectIndex, data.terrainChunks);

                if (objectIndex < data.terrainChunks.Count)
                {
                    GameObject meshChunkObject = data.terrainChunks[objectIndex];
                    if (meshChunkObject == null)
                    {
                        meshChunkObject = MeshUtil.InstantiatePrefabWithMesh(worldAreaMeshObjectPrefab, new Mesh(), transform.position);
                    }
                    else
                    {
                        meshChunkObject.transform.position = transform.position;
                    }

                    meshChunkObject.transform.SetParent(data.TerrainFolder());
                    data.terrainChunks[objectIndex] = meshChunkObject;

                    return data.terrainChunks[objectIndex];
                }
            }

            Debug.LogError("objectIndex: " + objectIndex + ", data.terrainChunks.Count: " + data.terrainChunks.Count);
            return null;

            // if (doNotCreate) return null;

            // int new_terrainChunkIndex = -1;
            // if (data.terrainChunks.Count == 0)
            // {
            //     GameObject meshChunkObject = MeshUtil.InstantiatePrefabWithMesh(worldAreaMeshObjectPrefab, new Mesh(), transform.position);
            //     meshChunkObject.transform.SetParent(data.TerrainFolder());
            //     data.terrainChunks.Add(meshChunkObject);
            //     new_terrainChunkIndex = 0;
            // }
            // else
            // {
            //     int nullIndex = data.terrainChunks.FindIndex(d => d == null);
            //     if (nullIndex > -1)
            //     {
            //         GameObject meshChunkObject = MeshUtil.InstantiatePrefabWithMesh(worldAreaMeshObjectPrefab, new Mesh(), transform.position);
            //         meshChunkObject.transform.SetParent(data.TerrainFolder());
            //         data.terrainChunks[nullIndex] = meshChunkObject;
            //         new_terrainChunkIndex = nullIndex;
            //     }
            //     else
            //     {
            //         GameObject meshChunkObject = MeshUtil.InstantiatePrefabWithMesh(worldAreaMeshObjectPrefab, new Mesh(), transform.position);
            //         meshChunkObject.transform.SetParent(data.TerrainFolder());
            //         data.terrainChunks.Add(meshChunkObject);
            //         new_terrainChunkIndex = data.terrainChunks.Count - 1;
            //     }
            // }

            // if (new_terrainChunkIndex > -1) _terrainChunkData_ByLookup[chunkLookup].SetChunkObjectIndex(new_terrainChunkIndex);

            // return data.terrainChunks[new_terrainChunkIndex];
        }

        public static List<Vector3> GetWorldspaceMeshChunkCornerPoints(HexagonCellPrototype worldspaceCell, Vector2 chunkSize)
        {
            if (worldspaceCell.size != 108)
            {
                Debug.LogError("This cell is not the right size for a wWorldSpace!");
                return null;
            }
            List<Vector3> chunkCorners = new List<Vector3>();
            for (int i = 0; i < worldspaceCell.sidePoints.Length; i++)
            {
                if (i == 0 || i == 3) continue;
                chunkCorners.AddRange(VectorUtil.GenerateRectangleCorners(worldspaceCell.sidePoints[i], chunkSize.x, chunkSize.y));
            }
            return chunkCorners;
        }

        private void Initialize_WorldAreas(Vector2 initialAreaLookup, Vector2 initialRegionLookup, int amount)
        {
            if (_worldAreas_ByRegion.ContainsKey(initialAreaLookup) == false || _worldAreas_ByRegion[initialAreaLookup].ContainsKey(initialAreaLookup) == false)
            {
                Debug.LogError("initialAreaLookup: " + initialAreaLookup + " not found in _worldAreas_ByRegion");
                return;
            }
            HexagonCellPrototype initialParentCell = _worldAreas_ByRegion[initialRegionLookup][initialAreaLookup];

            if (initialParentCell.neighbors.Count == 0) HexGridPathingUtil.Rehydrate_CellNeighbors(initialRegionLookup, initialAreaLookup, _worldAreas_ByRegion, true);
            if (initialParentCell.neighbors.Count == 0)
            {
                Debug.LogError("No neighbors for WorldArea coordinate: " + initialAreaLookup);
                return;
            }

            List<HexagonCellPrototype> parentCellsToFill = new List<HexagonCellPrototype>();
            if (amount > 1)
            {
                // Get Parent Cells
                parentCellsToFill = HexGridPathingUtil.GetConsecutiveInactiveWorldSpaceNeighbors(initialParentCell, _worldAreas_ByRegion, amount);
                Debug.LogError("parentCellsToFill: " + parentCellsToFill.Count);
            }
            else
            {
                parentCellsToFill.Add(initialParentCell);
            }

            foreach (HexagonCellPrototype areaCell in parentCellsToFill)
            {
                Vector2 areaLookup = areaCell.GetLookup();

                Debug.LogError("areaCell: " + areaLookup);

                Evaluate_WorldAreaFolder(areaLookup);

                Initialize_WorldAreaCells(areaLookup, areaCell.GetParentLookup());
            }
        }

        private void Initialize_WorldAreaCells(Vector2 areaLookup, Vector2 regionLookup)
        {
            //Temp
            int temp_chunkLimit = temp_areaChunksMax < 1 ? 999 : temp_areaChunksMax;

            Bounds currentWorldAreaBounds = VectorUtil.CalculateBounds(_worldAreas_ByRegion[regionLookup][areaLookup].cornerPoints.ToList());
            List<Vector2[]> areaChunkCorners = VectorUtil.DivideBoundsIntoChunks(currentWorldAreaBounds, areaChunks, areaChunkOverlap);
            List<Bounds> areaBounds = new List<Bounds>();
            HashSet<Vector2> initialized = new HashSet<Vector2>();

            List<Vector2> initializedWorldspaceLookups = new List<Vector2>();

            WorldAreaObjectData areaObjectData = GetWorldAreaObjectData(areaLookup);

            foreach (Vector2[] corners in areaChunkCorners)
            {
                temp_chunkLimit--;
                if (temp_chunkLimit <= 0) break;

                Bounds boundChunk = VectorUtil.CalculateBounds(corners.ToList());
                areaBounds.Add(boundChunk);
                List<HexagonCellPrototype> chunkWorldspaceCells = new List<HexagonCellPrototype>();
                List<Vector3> allWorldspaceMeshChunkCorners = new List<Vector3>();

                foreach (HexagonCellPrototype worldspaceCell in _worldSpaces_ByArea[areaLookup].Values)
                {
                    Vector2 worldspaceLookup = worldspaceCell.GetLookup();
                    if (VectorUtil.IsPointWithinBounds(boundChunk, worldspaceCell.center) && initialized.Contains(worldspaceLookup) == false)
                    {
                        areaObjectData.AddWorldspaceObjectData(worldspaceLookup, _worldspaceData_ByArea);

                        chunkWorldspaceCells.Add(worldspaceCell);
                        initialized.Add(worldspaceLookup);

                        // allWorldspaceMeshChunkCorners.AddRange(GetWorldspaceMeshChunkCornerPoints(worldspaceCell, meshChunkSize));
                    }
                }

                // if (allWorldspaceMeshChunkCorners.Count == 0) Debug.LogError("allWorldspaceMeshChunkCorners is empty");

                // Bounds activeGridBounds = VectorUtil.CalculateBounds_V2(allWorldspaceMeshChunkCorners);
                // temp_gridBounds = activeGridBounds;
                // Initialize_Worldspaces(chunkWorldspaceCells, areaObjectData, activeGridBounds);

                Initialize_Worldspaces(chunkWorldspaceCells, areaObjectData);
            }

            if (instantiateTrees || instantiateLocations)
            {
                foreach (Vector2 worldspaceLookup in initialized)
                {
                    if (instantiateLocations)
                    {
                        if (Instantiate_Location(areaLookup, worldspaceLookup))
                        {
                            break; //Testing just 1 for now
                        }
                    }
                    // if (instantiateTrees) Instantiate_Trees(areaLookup, worldspaceLookup);
                }
            }
        }

        private void Initialize_Worldspaces(List<HexagonCellPrototype> worldspaceCells, WorldAreaObjectData areaObjectData)
        // private void Initialize_Worldspaces(List<HexagonCellPrototype> worldspaceCells, WorldAreaObjectData areaObjectData, Bounds activeGridBounds)
        {
            Bounds activeGridBounds = GetWorldGridBounds(worldspaceCells, terrainChunkSizeXZ);
            temp_gridBounds = activeGridBounds;


            // Bounds bounds = VectorUtil.CalculateBounds_V2(GetWorldspaceMeshChunkCornerPoints(worldspaceCell, meshChunkSize));
            // List<Vector2> preAssignWorldspaceCoords = PreassignWorldSpaces(worldspaceCells);
            // (
            //     Dictionary<Vector2, TerrainVertex> globalTerrainVertexGridByCoord,
            //     Vector2[,] globalTerrainVertexGridCoordinates
            // ) = Generate_GlobalVertexGrid_WithNoise_V7(
            // // ) = Generate_GlobalVertexGrid_WithNoise_V6(
            //     activeGridBounds,
            //     transform,
            //     vertexDensity,
            //     layeredNoise_terrainGlobal,
            //     layeredNoise_locationPath,
            //     locationNoise_pathNoiseMin,

            //     _subCellTerraforms_ByWorldspace,

            //     _global_defaultCellSize,
            //     _global_terrainHeightDefault,
            //     persistence,
            //     octaves,
            //     lacunarity,
            //     cellLayerElevation
            // );


            (
                Dictionary<Vector2, TerrainVertex> globalTerrainVertexGridByCoord,
                Dictionary<Vector2, List<Vector3>> treeSpawnPoints_byWorldspace
            ) = Generate_GlobalVertexGrid_WithNoise_V8(
            // ) = Generate_GlobalVertexGrid_WithNoise_V7(
                activeGridBounds,
                transform,
                vertexDensity,
                layeredNoise_terrainGlobal,
                layeredNoise_locationPath,
                locationNoise_pathNoiseMin,

                _subCellTerraforms_ByWorldspace,
                cellLookup_ByLayer_BySize_ByWorldSpace,

                _global_defaultCellSize,
                _global_terrainHeightDefault,

                cellLayerElevation,
                vertexTreeStep
            );

            _globalTerrainVertexGridByCoordinate = globalTerrainVertexGridByCoord;

            if (_globalTerrainVertexGridByCoordinate == null || _globalTerrainVertexGridByCoordinate.Count == 0)
            {
                Debug.LogError("_globalTerrainVertexGridByCoordinate is invalid");
                return;
            }

            Generate_ActiveWorldGrid(worldspaceCells, activeGridBounds, treeSpawnPoints_byWorldspace, areaObjectData);
        }

        public void Generate_ActiveWorldGrid(
            List<HexagonCellPrototype> worldspaceCells,
            Bounds activeGridBounds,
            Dictionary<Vector2, List<Vector3>> treeSpawnPoints_byWorldspace,
            WorldAreaObjectData areaObjectData
        )
        {
            List<(Vector2[,], Vector2)> allGridChunksWithCenters = new List<(Vector2[,], Vector2)>();
            List<Vector3> chunkCenterCoordinates = new List<Vector3>();

            foreach (HexagonCellPrototype worldspaceCell in worldspaceCells)
            {
                Vector2 worldspaceLookup = worldspaceCell.GetLookup();
                Vector2 areaLookup = worldspaceCell.GetParentLookup();

                List<Vector3> chunkCenterPts = worldspaceCell.GetTerrainChunkCoordinates().ToList();
                chunkCenterCoordinates.AddRange(chunkCenterPts);

                allGridChunksWithCenters.AddRange(WorldManagerUtil.GetVertexGridChunkKeys(
                        _globalTerrainVertexGridByCoordinate,
                        chunkCenterPts,
                        vertexDensity,
                        terrainChunkSizeXZ
                    ));

                if (instantiateTrees && areaObjectData != null && treeSpawnPoints_byWorldspace.ContainsKey(worldspaceLookup))
                {
                    WorldspaceObjectData worldspaceObjectData = areaObjectData.GetWorldspaceObjectData(_worldspaceData_ByArea[areaLookup][worldspaceLookup].objectIndex);
                    Debug.Log("treeSpawnPoints: " + treeSpawnPoints_byWorldspace[worldspaceLookup].Count);

                    if (worldspaceObjectData != null) worldspaceObjectData.trees = Generate_Trees(treeSpawnPoints_byWorldspace[worldspaceLookup], worldspaceObjectData.TreeFolder());
                }


                // Bounds bounds = VectorUtil.CalculateBounds(GetWorldspaceMeshChunkCornerPoints(worldspaceCell, terrainChunkSizeXZ));
                // Bounds bounds = VectorUtil.CalculateBounds(GetWorldspaceMeshChunkCornerPoints(worldspaceCell, meshChunkSize));
                // Vector2[,] worldspaceVertexKeys = GetLocalVertexGridKeys_V2(
                //     bounds,
                //     _globalTerrainVertexGridByCoordinate,
                //     _worldSpaces_ByArea[areaLookup][worldLookup],
                //     transform,
                //     vertexDensity
                // );
                // if (_globalTerrainVertexKeysByWorldspaceCoordinate.ContainsKey(worldLookup) == false)
                // {
                //     _globalTerrainVertexKeysByWorldspaceCoordinate.Add(worldLookup, worldspaceVertexKeys);
                // }
                // else
                // {
                //     _globalTerrainVertexKeysByWorldspaceCoordinate[worldLookup] = worldspaceVertexKeys;
                // }

                // HexagonGrid hexGrid = GetWorldSpaceCellGrid(worldLookup);
                // if (hexGrid == null)
                // {
                //     Debug.LogError("No hexGrid found for worldLookup: " + worldLookup);
                //     continue;
                // }

                // Dictionary<int, List<HexagonCellPrototype>> gridCellPrototypesByLayer = hexGrid.GetDefaultPrototypesByLayer();
                // SetupCellGridState(
                //     _globalTerrainVertexGridByCoordinate,
                //     _globalTerrainVertexKeysByWorldspaceCoordinate[worldLookup],
                //     hexGrid,
                //     transform,
                //     viableFlatGroundCellSteepnessThreshhold,
                //     cellVertexSearchRadiusMult,
                //     groundSlopeElevationStep,
                //     _globalSeaLevel
                // );
            }

            TimeLog("Pre - RefreshTerrainMeshes.");

            RefreshTerrainMeshes_V2(
                allGridChunksWithCenters,
                _globalTerrainVertexGridByCoordinate,
                transform,
                activeGridBounds,
                worldspaceCells
            );
        }


        private List<HexagonCellPrototype> AssignActiveWorldSpaces()
        {
            HexagonCellPrototype headCell = GetCurrentWorldSpaceCell();
            List<HexagonCellPrototype> activeWorldspaceCells = HexGridPathingUtil.GetConsecutiveInactiveWorldSpaceNeighbors(headCell, _worldSpaces_ByArea, _minWorldSpaces);

            Debug.Log("activeWorldspaceCells: " + activeWorldspaceCells.Count);

            return activeWorldspaceCells;
        }

        private Bounds GetWorldGridBounds(List<HexagonCellPrototype> worldspaceCells, Vector2 meshChunkSize)
        {
            List<Vector3> allChunkCorners = new List<Vector3>();
            foreach (var worldspaceCell in worldspaceCells)
            {
                if (worldspaceCell != null && worldspaceCell.HasWorldCoordinate()) allChunkCorners.AddRange(GetWorldspaceMeshChunkCornerPoints(worldspaceCell, meshChunkSize));
            }
            if (allChunkCorners.Count == 0) Debug.LogError("allChunkCorners is empty");

            Bounds activeGridBounds = VectorUtil.CalculateBounds_V2(allChunkCorners);
            return activeGridBounds;
        }

        #endregion

        private void Generate_WorldSpace_CellGrid_Location(HexagonCellPrototype worldspaceCell, int gridBaseElevation, int layersMax, Dictionary<Vector2, HexagonCellPrototype> subCellTerraforms, bool enableLog = true)
        {
            HashSet<string> neighborIDsToEvaluate = new HashSet<string>();
            Dictionary<int, List<HexagonCellPrototype>> neighborsToEvaluate_bySize = new Dictionary<int, List<HexagonCellPrototype>>() {
                {(int)HexCellSizes.Default, new List<HexagonCellPrototype>()},
                {(int)HexCellSizes.X_4, new List<HexagonCellPrototype>()},
            };

            int baseLayer = HexCoreUtil.Calculate_CurrentLayer(cellLayerElevation, gridBaseElevation);
            // int totalCreated = 0;
            // Debug.Log("gridBaseElevation: " + gridBaseElevation + ", baseLayer: " + baseLayer);

            Vector3 worldSpaceCenterPoint = worldspaceCell.center;
            Vector2 worldspaceLookup = worldspaceCell.GetLookup();
            Vector2 areaLookup = worldspaceCell.GetParentLookup();

            Vector3 gridCenterPos = worldspaceCell.center;
            gridCenterPos.y = gridBaseElevation;
            Dictionary<int, Dictionary<Vector2, Vector3>> new_cellCenters_ByLookup_BySize = HexGridUtil.Generate_HexGridCenterPoints_BySize(
                    gridCenterPos,
                    (int)HexCellSizes.X_4,
                    _worldspaceSize
                );

            Vector3[] worldspaceCorners = HexCoreUtil.GenerateHexagonPoints(worldSpaceCenterPoint, _worldspaceSize);
            int created = 0;

            if (cellLookup_ByLayer_BySize_ByWorldSpace == null) cellLookup_ByLayer_BySize_ByWorldSpace = new Dictionary<Vector2, Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>>();
            if (cellLookup_ByLayer_BySize_ByWorldSpace.ContainsKey(worldspaceLookup) == false) cellLookup_ByLayer_BySize_ByWorldSpace.Add(worldspaceLookup, new Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>());

            //Setup grid layer structure
            LocationPrefab locationPrefab = _locationPrefabs_ByWorldspace_ByArea[areaLookup][worldspaceLookup];

            bool useUnderGround = locationPrefab.GetSettings().locationType == LocationType.Tunnel;
            //Setup layers in dictionary
            int startingLayer = baseLayer + 1;
            int topLayer = startingLayer + layersMax;
            int bottomLayer = baseLayer;

            if (useUnderGround)
            {
                int undergroundLayers = (int)(layersMax * 0.75f);
                int upperlayers = (layersMax - undergroundLayers);
                topLayer = baseLayer + upperlayers;
                bottomLayer = (baseLayer - undergroundLayers);
                startingLayer = bottomLayer;

                // Debug.Log("useUnderGround - baseLayer: " + baseLayer + ", bottomLayer: " + bottomLayer + ", topLayer: " + topLayer);
            }

            foreach (int currentSize in new_cellCenters_ByLookup_BySize.Keys)
            {
                if (currentSize > (int)HexCellSizes.Default) continue;

                int childSize = (int)HexCellSizes.X_4;

                //Add currentSize & childSize
                if (cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup].ContainsKey(currentSize) == false)
                {
                    cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup].Add(currentSize, new Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>() {
                            { baseLayer, new Dictionary<Vector2, HexagonCellPrototype>() }
                        });
                }

                if (cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup].ContainsKey(childSize) == false)
                {
                    cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup].Add(childSize, new Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>(){
                            { baseLayer, new Dictionary<Vector2, HexagonCellPrototype>() }
                        });
                }


                foreach (var kvp in new_cellCenters_ByLookup_BySize[currentSize])
                {
                    Vector3 point = kvp.Value;

                    Vector2 newPos = CalculateCoordinateBaseGridPosition(point);
                    Vector2 newCoordinate = VectorUtil.Calculate_AproximateCoordinate(point);
                    Vector2 pointLookup = HexCoreUtil.Calculate_CenterLookup(newCoordinate, currentSize);

                    bool foundExisting = _worldSpaces_ByArea[areaLookup][worldspaceLookup].neighbors.Any(n =>
                        cellLookup_ByLayer_BySize_ByWorldSpace.ContainsKey(n.GetLookup()) &&
                        cellLookup_ByLayer_BySize_ByWorldSpace[n.GetLookup()][currentSize][baseLayer].ContainsKey(pointLookup));

                    if (foundExisting)
                    {
                        // Debug.LogError("Existing pointLookup found for center point: " + point + ", skipping duplicate");
                        continue;
                    }

                    if (subCellTerraforms.ContainsKey(pointLookup) == false || subCellTerraforms[pointLookup].size != currentSize) continue;

                    if (HexCoreUtil.IsAnyHexPointWithinPolygon(point, currentSize, worldspaceCorners))
                    {
                        HexagonCellPrototype newCell = subCellTerraforms[pointLookup];
                        int currentGroundLayer = newCell.layer;

                        if (cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize].ContainsKey(currentGroundLayer) == false) cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize].Add(currentGroundLayer, new Dictionary<Vector2, HexagonCellPrototype>());
                        if (cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize][currentGroundLayer].ContainsKey(pointLookup)) continue;

                        newCell.SetWorldCoordinate(newPos);
                        newCell.SetWorldSpaceLookup(worldspaceLookup);

                        CellStatus groundTypeFound = CellStatus.Unset;
                        bool allowBufferStatusAssignment = false;

                        List<HexagonCellPrototype> childCells = null;

                        // if (subCellTerraforms.ContainsKey(pointLookup) && subCellTerraforms[pointLookup].size == currentSize && (int)subCellTerraforms[pointLookup].center.y == (int)newCell.center.y)
                        // {
                        groundTypeFound = newCell.GetCellStatus();

                        if (layersMax > 1 && groundTypeFound == CellStatus.GenericGround)
                        {
                            allowBufferStatusAssignment = HexCellUtil.IsCellInBeteenNeighborsOfStatus(newCell, CellStatus.FlatGround, subCellTerraforms);
                            // if (allowBufferStatusAssignment) Debug.Log("allowBufferStatusAssignment " + allowBufferStatusAssignment);
                            if (allowBufferStatusAssignment == false) continue;
                        }

                        if (cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup].ContainsKey(childSize) == false)
                        {
                            cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup].Add(childSize, new Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>());
                        }

                        if (cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][childSize].ContainsKey(currentGroundLayer) == false)
                        {
                            cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][childSize].Add(currentGroundLayer, new Dictionary<Vector2, HexagonCellPrototype>());
                        }

                        // Generate child cells within here
                        List<Vector3> childrenX4 = HexCoreUtil.GenerateHexCenterPoints_X13(newCell.center, childSize);
                        childCells = new List<HexagonCellPrototype>();
                        foreach (Vector3 childPoint in childrenX4)
                        {
                            Vector2 childLookup = HexCoreUtil.Calculate_CenterLookup(childPoint, childSize);
                            if (
                                cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][childSize][currentGroundLayer].ContainsKey(childLookup) == false &&
                                HexCoreUtil.IsAnyHexPointWithinPolygon(childPoint, childSize, worldspaceCorners)
                            )
                            {
                                HexagonCellPrototype newChildCell = new HexagonCellPrototype(childPoint, childSize, newCell, cellLayerElevation);
                                newChildCell.SetWorldSpaceLookup(worldspaceLookup);
                                newChildCell.SetCellStatus(groundTypeFound);

                                cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][childSize][currentGroundLayer].Add(childLookup, newChildCell);

                                childCells.Add(newChildCell);

                                if (neighborIDsToEvaluate.Contains(newChildCell.Get_Uid()) == false)
                                {
                                    neighborIDsToEvaluate.Add(newChildCell.Get_Uid());
                                    neighborsToEvaluate_bySize[childSize].Add(newChildCell);
                                }
                            }
                        }

                        if (childCells[0].layer != newCell.layer) Debug.LogError("currentGroundLayer: " + currentGroundLayer + ", parent/child layer mismatch detected - parentlayer: " + newCell.layer + ", this cell's layer: " + childCells[0].layer);
                        // }

                        cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize][currentGroundLayer].Add(pointLookup, newCell);

                        // Add to neighborsToEvaluate_bySize
                        if (neighborIDsToEvaluate.Contains(newCell.Get_Uid()) == false)
                        {
                            neighborIDsToEvaluate.Add(newCell.Get_Uid());
                            neighborsToEvaluate_bySize[currentSize].Add(newCell);
                        }


                        if (layersMax < 2 || (currentSize != (int)HexCellSizes.X_12 && currentSize != (int)HexCellSizes.X_4)) continue;

                        // Generate new layers 
                        HexagonCellPrototype previousCell = newCell;
                        List<HexagonCellPrototype> previousChildCells = childCells;

                        // Debug.LogError("currentGroundLayer: " + currentGroundLayer + ", newCell.layer: " + newCell.layer + ", cellLayerElevation: " + cellLayerElevation);

                        // Add Upper Layers
                        startingLayer = (currentGroundLayer + 1);

                        for (int currentLayer = startingLayer; currentLayer < topLayer; currentLayer++)
                        {
                            HexagonCellPrototype nextLayerCell = HexagonCellPrototype.DuplicateCellToNewLayer_Above(previousCell, cellLayerElevation, null);

                            if (cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize].ContainsKey(currentLayer) == false)
                            {
                                cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize].Add(currentLayer, new Dictionary<Vector2, HexagonCellPrototype>());
                            }
                            else if (cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize][currentLayer].ContainsKey(nextLayerCell.GetLookup())) continue;


                            if (groundTypeFound == CellStatus.Unset && subCellTerraforms.ContainsKey(pointLookup) && subCellTerraforms[pointLookup].size == currentSize && (int)subCellTerraforms[pointLookup].center.y == (int)nextLayerCell.center.y)
                            {
                                groundTypeFound = newCell.GetCellStatus();
                            }
                            else if (groundTypeFound != CellStatus.Unset)
                            {
                                if (groundTypeFound == CellStatus.FlatGround || (allowBufferStatusAssignment && (currentLayer - startingLayer) > 1)) nextLayerCell.SetCellStatus(CellStatus.AboveGround);
                            }

                            cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize][currentLayer].Add(nextLayerCell.GetLookup(), nextLayerCell);

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
                                if (cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][childSize].ContainsKey(currentLayer) == false) cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][childSize].Add(currentLayer, new Dictionary<Vector2, HexagonCellPrototype>());

                                List<HexagonCellPrototype> nextLayerChildCells = new List<HexagonCellPrototype>();
                                foreach (HexagonCellPrototype prevChild in previousChildCells)
                                {
                                    if (cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][childSize][currentLayer].ContainsKey(prevChild.GetLookup())) continue;

                                    HexagonCellPrototype newChildCell = HexagonCellPrototype.DuplicateCellToNewLayer_Above(prevChild, cellLayerElevation, nextLayerCell);

                                    cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][childSize][currentLayer].Add(newChildCell.GetLookup(), newChildCell);

                                    nextLayerChildCells.Add(newChildCell);

                                    if (neighborIDsToEvaluate.Contains(newChildCell.Get_Uid()) == false)
                                    {
                                        neighborIDsToEvaluate.Add(newChildCell.Get_Uid());
                                        neighborsToEvaluate_bySize[childSize].Add(newChildCell);
                                    }
                                }

                                previousChildCells = nextLayerChildCells;

                                if (nextLayerChildCells[0].layer != nextLayerCell.layer || currentLayer != nextLayerCell.layer) Debug.LogError("startingLayer: " + startingLayer + ", currentLayer: " + currentLayer + ", parent/child layer mismatch detected - parentlayer: " + nextLayerCell.layer + ", this cell's layer: " + nextLayerChildCells[0].layer + ", cellLayerElevation: " + cellLayerElevation);
                            }
                        }

                        // Add Underground Layers
                        if (useUnderGround)
                        {
                            previousCell = newCell;
                            previousChildCells = childCells;
                            startingLayer = (currentGroundLayer - 1);

                            for (int currentLayer = startingLayer; currentLayer > bottomLayer - 1; currentLayer--)
                            {
                                HexagonCellPrototype nextLayerCell = HexagonCellPrototype.DuplicateCellToNewLayer_Below(previousCell, cellLayerElevation, null);

                                if (cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize].ContainsKey(currentLayer) == false)
                                {
                                    cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize].Add(currentLayer, new Dictionary<Vector2, HexagonCellPrototype>());
                                }
                                else if (cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize][currentLayer].ContainsKey(nextLayerCell.GetLookup())) continue;

                                if (groundTypeFound == CellStatus.FlatGround || (allowBufferStatusAssignment && (startingLayer - currentLayer) > 1))
                                {
                                    nextLayerCell.SetCellStatus(CellStatus.UnderGround);
                                }

                                cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize][currentLayer].Add(nextLayerCell.GetLookup(), nextLayerCell);

                                if (neighborIDsToEvaluate.Contains(nextLayerCell.Get_Uid()) == false)
                                {
                                    neighborIDsToEvaluate.Add(nextLayerCell.Get_Uid());
                                    neighborsToEvaluate_bySize[currentSize].Add(nextLayerCell);
                                }

                                previousCell = nextLayerCell;


                                // Duplicate Children Cells to Next Layer
                                if (previousChildCells != null)
                                // if (groundTypeFound == CellStatus.FlatGround && previousChildCells != null)
                                {
                                    if (cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][childSize].ContainsKey(currentLayer) == false) cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][childSize].Add(currentLayer, new Dictionary<Vector2, HexagonCellPrototype>());

                                    List<HexagonCellPrototype> nextLayerChildCells = new List<HexagonCellPrototype>();
                                    foreach (HexagonCellPrototype prevChild in previousChildCells)
                                    {
                                        if (cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][childSize][currentLayer].ContainsKey(prevChild.GetLookup())) continue;

                                        HexagonCellPrototype newChildCell = HexagonCellPrototype.DuplicateCellToNewLayer_Below(prevChild, cellLayerElevation, nextLayerCell);

                                        cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][childSize][currentLayer].Add(newChildCell.GetLookup(), newChildCell);

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
                        }

                        created++;

                    }
                }
            }
            // Debug.Log("Created " + created + " center points within WorldSpace coordinate: " + worldspaceLookup);
            // totalCreated += created;

            foreach (var kvp in neighborsToEvaluate_bySize)
            {
                int currentSize = kvp.Key;
                // Debug.Log("subcell neighbors To evaluate - size: " + currentSize + " - " + neighborsToEvaluate_bySize[currentSize].Count);
                bool log = currentSize == 4;
                HexCellUtil.Evaluate_SubCellNeighbors(
                    neighborsToEvaluate_bySize[currentSize],
                    worldspaceCell,
                    cellLookup_ByLayer_BySize_ByWorldSpace,
                    cellLayerElevation,
                    log
                );
            }

            HexCellSizes desiredSize = locationPrefab.GetSettings().tileDirectory.HasTileSize(HexCellSizes.X_4) ? HexCellSizes.X_4 : HexCellSizes.Default;

            HexagonCellCluster newPreassignedCluster = HexagonCellManager.Generate_ClusterSubGridFromPrototypesByLayer(
                            cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][(int)desiredSize],
                            cellLookup_ByLayer_BySize_ByWorldSpace,
                            CellClusterType.Other,
                            true,
                            null
                        );

            LocationData locationData = AddNew_LocationData(
                areaLookup,
                worldspaceLookup,
                newPreassignedCluster,
                locationPrefab.GetSettings()
            );

            if (useUnderGround)
            {
                WorldAreaObjectData areaObjectData = GetWorldAreaObjectData(areaLookup);
                if (areaObjectData != null)
                {
                    WorldspaceObjectData worldspaceObjectData = areaObjectData.AddWorldspaceObjectData(worldspaceLookup, _worldspaceData_ByArea);
                    // if (instantiateTunnnels)
                    GameObject tunnelGameObject = Generate_UndergroundTunnel(locationData, worldspaceObjectData.TunnelFolder());
                    if (tunnelGameObject != null)
                    {
                        worldspaceObjectData.tunnels.Add(tunnelGameObject);
                    }
                }
            }
        }

        private List<HexagonCellPrototype> Generate_WorldSpace_CellGrids(List<HexagonCellPrototype> worldspacesToFill, Vector2 areaLookup)
        {
            if (_worldSpaces_ByArea == null || _worldSpaces_ByArea.ContainsKey(areaLookup) == false)
            {
                Debug.LogError("_worldSpaces_ByArea is null or does not contain areaLookup: " + areaLookup);
                return null;
            }

            Debug.Log("WorldSpace cell grids to generate: " + worldspacesToFill.Count);

            Dictionary<Vector2, Dictionary<int, Dictionary<Vector2, Vector3>>> new_cellCenters_ByLookup_BySize_ByWorldSpace = new Dictionary<Vector2, Dictionary<int, Dictionary<Vector2, Vector3>>>();
            Dictionary<Vector2, Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>> new_cellLookup_ByLayer_BySize_ByWorldSpace = new Dictionary<Vector2, Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>>();

            HashSet<string> neighborIDsToEvaluate = new HashSet<string>();
            List<HexagonCellPrototype> neighborsToEvaluate = new List<HexagonCellPrototype>();
            List<HexagonGrid> newlyAddedGrids = new List<HexagonGrid>();
            int totalCreated = 0;
            int baseLayer = 0;

            foreach (HexagonCellPrototype worldSpaceCell in worldspacesToFill)
            {
                Vector3 worldSpaceCenterPoint = worldSpaceCell.center;
                Vector2 worldspaceLookup = worldSpaceCell.GetLookup();

                if (new_cellCenters_ByLookup_BySize_ByWorldSpace.ContainsKey(worldspaceLookup) == false)
                {
                    new_cellCenters_ByLookup_BySize_ByWorldSpace.Add(worldspaceLookup, new Dictionary<int, Dictionary<Vector2, Vector3>>());
                    new_cellLookup_ByLayer_BySize_ByWorldSpace.Add(worldspaceLookup, new Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>());
                }

                Vector3 gridCenterPos = worldSpaceCell.center;
                gridCenterPos.y = _global_cellGridElevation;

                Dictionary<int, Dictionary<Vector2, Vector3>> new_cellCenters_ByLookup_BySize = HexGridUtil.Generate_HexGridCenterPoints_BySize(
                // Dictionary<int, Dictionary<Vector2, Vector3>> new_cellCenters_ByLookup_BySize = HexGridUtil.GenerateHexGridCenterPoints_V3(
                        gridCenterPos,
                        (int)HexCellSizes.X_4,
                        _worldspaceSize
                    // worldSpaceCell,
                    // transform
                    );

                Vector3[] worldspaceCorners = HexCoreUtil.GenerateHexagonPoints(worldSpaceCenterPoint, _worldspaceSize);
                int created = 0;

                foreach (int currentSize in new_cellCenters_ByLookup_BySize.Keys)
                {

                    if (currentSize > (int)HexCellSizes.Default) continue;

                    if (new_cellCenters_ByLookup_BySize_ByWorldSpace[worldspaceLookup].ContainsKey(currentSize) == false)
                    {
                        new_cellCenters_ByLookup_BySize_ByWorldSpace[worldspaceLookup].Add(currentSize, new Dictionary<Vector2, Vector3>());
                        new_cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup].Add(currentSize, new Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>() {
                            { baseLayer, new Dictionary<Vector2, HexagonCellPrototype>() }
                        });

                        //Setup layers in dictionary
                        int startingLayer = baseLayer;
                        for (int currentLayer = startingLayer + 1; currentLayer < cellLayersMax; currentLayer++)
                        {
                            if (new_cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize].ContainsKey(currentLayer) == false)
                            {
                                new_cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize].Add(currentLayer, new Dictionary<Vector2, HexagonCellPrototype>());
                            }
                        }
                    }

                    foreach (var kvp in new_cellCenters_ByLookup_BySize[currentSize])
                    {
                        Vector3 point = kvp.Value;

                        Vector2 newPos = CalculateCoordinateBaseGridPosition(point);
                        Vector2 newCoordinate = VectorUtil.Calculate_AproximateCoordinate(point);
                        Vector2 pointLookup = HexCoreUtil.Calculate_CenterLookup(newCoordinate, currentSize);

                        if (_worldSpaces_ByArea[areaLookup].ContainsKey(worldspaceLookup))
                        {
                            bool foundExisting = _worldSpaces_ByArea[areaLookup][worldspaceLookup].neighbors.Any(n =>
                                new_cellCenters_ByLookup_BySize_ByWorldSpace.ContainsKey(n.GetLookup()) &&
                                new_cellCenters_ByLookup_BySize_ByWorldSpace[n.GetLookup()][currentSize].ContainsKey(pointLookup));

                            if (foundExisting)
                            {
                                // Debug.LogError("Existing pointLookup found for center point: " + point + ", skipping duplicate");
                                continue;
                            }
                        }

                        if (VectorUtil.IsPointWithinPolygon(point, worldspaceCorners) || VectorUtil.DistanceXZ(point, worldSpaceCenterPoint) < _worldspaceSize * 0.95f)
                        {
                            if (new_cellCenters_ByLookup_BySize_ByWorldSpace[worldspaceLookup][currentSize].ContainsKey(pointLookup) == false)
                            {
                                new_cellCenters_ByLookup_BySize_ByWorldSpace[worldspaceLookup][currentSize].Add(pointLookup, point);

                                HexagonCellPrototype newCell = new HexagonCellPrototype(point, currentSize, worldSpaceCell, cellLayerElevation);
                                newCell.SetWorldCoordinate(newPos);
                                newCell.SetWorldSpaceLookup(worldspaceLookup);

                                new_cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize][baseLayer].Add(pointLookup, newCell);

                                if (neighborIDsToEvaluate.Contains(newCell.Get_Uid()) == false)
                                {
                                    neighborIDsToEvaluate.Add(newCell.Get_Uid());
                                    neighborsToEvaluate.Add(newCell);
                                }

                                // Generate layers 
                                if (currentSize != (int)HexCellSizes.X_12 && currentSize != (int)HexCellSizes.X_4) continue;

                                if (cellLayersMax > 1)
                                {
                                    int startingLayer = baseLayer;
                                    HexagonCellPrototype bottomCell = newCell;

                                    for (int currentLayer = startingLayer + 1; currentLayer < cellLayersMax; currentLayer++)
                                    {
                                        HexagonCellPrototype nextLayerCell = HexagonCellPrototype.DuplicateCellToNewLayer_Above(bottomCell, cellLayerElevation, null);
                                        if (new_cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize][currentLayer].ContainsKey(nextLayerCell.GetLookup())) continue;

                                        new_cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][currentSize][currentLayer].Add(nextLayerCell.GetLookup(), nextLayerCell);

                                        if (neighborIDsToEvaluate.Contains(nextLayerCell.Get_Uid()) == false)
                                        {
                                            neighborIDsToEvaluate.Add(nextLayerCell.Get_Uid());
                                            neighborsToEvaluate.Add(nextLayerCell);
                                        }

                                        bottomCell = nextLayerCell;
                                    }
                                }

                                created++;
                            }
                        }
                    }
                }
                // Debug.Log("Created " + created + " center points within WorldSpace coordinate: " + worldspaceLookup);
                totalCreated += created;
            }

            // Evaluate / Assign Cell Neighbors Lists
            if (neighborsToEvaluate.Count > 2)
            {
                foreach (HexagonCellPrototype cell in neighborsToEvaluate)
                {
                    int neighborsFound = 0;

                    // List<Vector2> neighborLookups = HexagonCellPrototype.GenerateNeighborLookupCoordinates(cell.center, cell.size);
                    Dictionary<HexagonSide, Vector2> neighborLookupsBySide = HexCoreUtil.GenerateNeighborLookupCoordinatesBySide(cell.center, cell.size);
                    foreach (var kvp in neighborLookupsBySide)
                    {
                        Vector2 neighborLookup = kvp.Value;
                        HexagonCellPrototype neighbor = new_cellLookup_ByLayer_BySize_ByWorldSpace[cell.GetWorldSpaceLookup()][cell.size][cell.GetGridLayer()].ContainsKey(neighborLookup)
                                                                ? new_cellLookup_ByLayer_BySize_ByWorldSpace[cell.GetWorldSpaceLookup()][cell.size][cell.GetGridLayer()][neighborLookup]
                                                                : null;

                        if (neighbor != null && neighbor.uid != cell.uid)
                        {
                            cell.AssignSideNeighbor(neighbor, kvp.Key);

                            neighborsFound++;
                            continue;
                        }
                        foreach (var worldSpaceCell in worldspacesToFill)
                        {
                            Vector2 worldspaceLookup = worldSpaceCell.GetLookup();
                            if (worldspaceLookup == cell.GetWorldSpaceLookup()) continue;

                            if (
                                !new_cellLookup_ByLayer_BySize_ByWorldSpace.ContainsKey(worldspaceLookup) ||
                                !new_cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup].ContainsKey(cell.size) ||
                                !new_cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][cell.size].ContainsKey(cell.GetGridLayer())
                            ) continue;

                            neighbor = new_cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][cell.size][cell.GetGridLayer()].ContainsKey(neighborLookup)
                                                            ? new_cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][cell.size][cell.GetGridLayer()][neighborLookup]
                                                            : null;

                            // neighbor = new_cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup][cell.size][cell.GetGridLayer()].Find(n => n.GetLookup() == lookup);
                            if (neighbor != null && neighbor.uid != cell.uid)
                            {
                                cell.AssignSideNeighbor(neighbor, kvp.Key);

                                neighborsFound++;
                                break;
                            }
                        }
                    }

                    HexagonCellPrototype.EvaluateForEdge(cell, EdgeCellType.Default, true);

                    if (neighborsFound == 0 || neighborsFound > 8) Debug.LogError("cell neighbors found: " + neighborsFound);
                    // Debug.Log("cell neighbors found: " + neighborsFound + ", layer: " + cell.GetGridLayer());
                }
                // Debug.Log("WorldSpace neighbors To evaluate: " + neighborsToEvaluate.Count);
                // HexagonCellPrototype.PopulateNeighborsFromCornerPointsXZ(neighborsToEvaluate, transform, 12f);
            }

            temp_cellCenters_ByLookup_BySize_ByWorldSpace = new_cellCenters_ByLookup_BySize_ByWorldSpace;
            cellLookup_ByLayer_BySize_ByWorldSpace = new_cellLookup_ByLayer_BySize_ByWorldSpace;

            List<Vector2> new_activeWorldspaceLookups = new List<Vector2>();

            //Generate / Assign HexGrid Classes
            foreach (HexagonCellPrototype worldSpaceCell in worldspacesToFill)
            {
                Vector3 worldSpaceCenterPoint = worldSpaceCell.center;
                Vector2 worldspaceLookup = worldSpaceCell.GetLookup();

                if (_worldSpaceCellGridByCenterCoordinate.ContainsKey(worldspaceLookup) == false)
                {
                    Vector3 centerPos = worldSpaceCell.center;
                    HexagonGrid newHexGrid = new HexagonGrid(
                     _global_defaultCellSize,
                     cellLayersMax,
                     cellLayersMax,
                     cellLayerElevation,
                     GridPreset.Outpost
                    );

                    newHexGrid.AssignGridCells(cellLookup_ByLayer_BySize_ByWorldSpace[worldspaceLookup]);
                    newHexGrid.EvaluateCellParents(cellLookup_ByLayer_BySize_ByWorldSpace);

                    _worldSpaceCellGridByCenterCoordinate.Add(worldspaceLookup, newHexGrid);

                    if (newHexGrid != null) newlyAddedGrids.Add(newHexGrid);

                    if (new_activeWorldspaceLookups.Contains(worldspaceLookup) == false) new_activeWorldspaceLookups.Add(worldspaceLookup);
                }
            }

            _activeWorldspaceLookups = new_activeWorldspaceLookups;

            Debug.Log("Created " + totalCreated + " new center points across " + temp_cellCenters_ByLookup_BySize_ByWorldSpace.Count + " WorldSpaces");

            return worldspacesToFill;
        }



        private void GenerateWorldCells_WorldSpace(int amount = 12, bool enableLog = false)
        {
            // temp
            // _terrainChunkData_ByLookup = new Dictionary<Vector2, TerrainChunkData>();

            if (_worldAreas_ByRegion == null || _worldAreas_ByRegion.Count == 0) GenerateWorldCells_Area();
            if (_worldAreas_ByRegion == null)
            {
                Debug.LogError("No _worldAreas_ByRegion");
                return;
            }

            Vector2 initialLookup = VectorUtil.Calculate_AproximateCoordinate(transform.position);
            if (_worldAreas_ByRegion.ContainsKey(initialLookup) == false || _worldAreas_ByRegion[initialLookup].ContainsKey(initialLookup) == false)
            {
                Debug.LogError("initialLookup: " + initialLookup + " not found in _worldAreas_ByRegion");
                return;
            }

            HexagonCellPrototype initialParentCell = _worldAreas_ByRegion[initialLookup][initialLookup];

            if (initialParentCell.neighbors.Count == 0) HexGridPathingUtil.Rehydrate_CellNeighbors(initialLookup, initialLookup, _worldAreas_ByRegion, true);
            if (initialParentCell.neighbors.Count == 0)
            {
                Debug.LogError("No neighbors for WorldArea coordinate: " + initialLookup);
                return;
            }
            // Get Parent Cells
            List<HexagonCellPrototype> parentCellsToFill = HexGridPathingUtil.GetConsecutiveInactiveWorldSpaceNeighbors(initialParentCell, _worldAreas_ByRegion, amount);
            if (enableLog) Debug.LogError("parentCellsToFill: " + parentCellsToFill.Count);

            Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> new_worldSpaces_ByArea = new Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>>();
            HashSet<string> neighborIDsToEvaluate = new HashSet<string>();
            Dictionary<Vector2, HexagonCellPrototype> neighborsToEvaluate = new Dictionary<Vector2, HexagonCellPrototype>();
            int totalCreated = 0;

            int cellSize = _worldspaceSize;
            int parentCellSize = _worldAreaSize;

            foreach (HexagonCellPrototype areaCell in parentCellsToFill)
            {
                Vector3 areaCenterPoint = areaCell.center;
                Vector2 areaLookup = areaCell.GetLookup();
                Vector2 areaParentLookup = areaCell.GetParentLookup();

                if (new_worldSpaces_ByArea.ContainsKey(areaLookup) == false) new_worldSpaces_ByArea.Add(areaLookup, new Dictionary<Vector2, HexagonCellPrototype>());

                int totalBufferRadius = HexCellUtil.CalculateExpandedHexRadius(cellSize, 3);
                Dictionary<Vector2, Vector3> newCenterPoints = HexGridUtil.Generate_HexagonGridCenterPoints(
                    areaCenterPoint,
                    cellSize,
                    totalBufferRadius
                );

                Vector3[] parentCellCorners = HexCoreUtil.GenerateHexagonPoints(areaCenterPoint, parentCellSize);
                int created = 0;
                int currentChunkIX = 0;

                foreach (Vector3 point in newCenterPoints.Values)
                {
                    Vector2 newWorldCoordinate = VectorUtil.Calculate_Coordinate(point);
                    Vector2 worldSpaceLookup = HexCoreUtil.Calculate_CenterLookup(newWorldCoordinate, cellSize);

                    if (_worldAreas_ByRegion.ContainsKey(areaParentLookup) && _worldAreas_ByRegion[areaParentLookup].ContainsKey(areaLookup))
                    {
                        bool foundExisting = areaCell.neighbors.Any(ar =>
                            new_worldSpaces_ByArea.ContainsKey(ar.GetLookup()) &&
                            new_worldSpaces_ByArea[ar.GetLookup()].ContainsKey(worldSpaceLookup));

                        if (foundExisting)
                        {
                            // Debug.LogError("Existing lookupCoordinate found for worldSpaceLookup: " + worldSpaceLookup + ", skipping duplicate");
                            continue;
                        }
                    }

                    if (
                        new_worldSpaces_ByArea[areaLookup].ContainsKey(worldSpaceLookup) == false && neighborsToEvaluate.ContainsKey(worldSpaceLookup) == false &&
                        HexCoreUtil.IsAnyHexPointWithinPolygon(point, cellSize, parentCellCorners)
                    )
                    {
                        // HexagonCellPrototype newWorldSpaceCell = new HexagonCellPrototype(CalculateCoordinateWorldPosition(newWorldCoordinate), cellSize, null);
                        // newWorldSpaceCell.SetWorldCoordinate(newWorldCoordinate);
                        // newWorldSpaceCell.SetParentLookup(areaLookup);
                        HexagonCellPrototype newWorldSpaceCell = HexagonCellPrototype.New_WorldCell(CalculateCoordinateWorldPosition(newWorldCoordinate), cellSize, areaLookup);
                        newWorldSpaceCell.objectIndex = created;

                        new_worldSpaces_ByArea[areaLookup].Add(newWorldSpaceCell.GetLookup(), newWorldSpaceCell);

                        if (worldSpaceLookup != newWorldSpaceCell.GetLookup()) Debug.LogError("Inconsistent lookup coordinates found! - newWorldSpaceCell.GetLookup(): " + newWorldSpaceCell.GetLookup() + ", worldSpaceLookup: " + worldSpaceLookup);

                        if (neighborIDsToEvaluate.Contains(newWorldSpaceCell.Get_Uid()) == false)
                        {
                            neighborIDsToEvaluate.Add(newWorldSpaceCell.Get_Uid());
                            neighborsToEvaluate.Add(newWorldSpaceCell.GetLookup(), newWorldSpaceCell);
                        }

                        // Add terrain chunk center lookups to dictionary; 
                        int chunksAdded = TerrainChunkData.Generate_WorldspaceChunkData(newWorldSpaceCell, _terrainChunkData_ByLookup, currentChunkIX);
                        currentChunkIX += chunksAdded;

                        created++;
                    }
                }

                Debug.Log("Created " + created + " WorldSpaces within WorldArea coordinate: " + areaLookup + ", terrainChunks added: " + currentChunkIX);
                totalCreated += created;
            }

            _worldSpaces_ByArea = new_worldSpaces_ByArea;

            if (neighborsToEvaluate.Count > 1)
            {
                if (enableLog) Debug.Log("World Area neighbors To evaluate: " + neighborsToEvaluate.Count);
                HexCellUtil.Evaluate_WorldCellNeighbors(neighborsToEvaluate, enableLog);
            }
            if (enableLog) Debug.Log("Created " + totalCreated + " new WorldSpaces across " + _worldSpaces_ByArea.Count + " WorldAreas");
        }


        private void GenerateWorldCells_SubCells(List<HexagonCellPrototype> worldspaceCells, bool enableLog = false)
        {
            _subCellTerraforms_ByWorldspace = new Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>>();

            Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> new_subCells_ByWorldspace = new Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>>();
            HashSet<string> neighborIDsToEvaluate = new HashSet<string>();
            Dictionary<Vector2, HexagonCellPrototype> neighborsToEvaluate = new Dictionary<Vector2, HexagonCellPrototype>();
            int totalCreated = 0;
            int subCellSize = (int)_global_defaultCellSize;
            int parentCellSize = _worldspaceSize;

            foreach (HexagonCellPrototype worldspaceCell in worldspaceCells)
            {
                Vector3 worldspaceCenterPoint = worldspaceCell.center;
                Vector2 wordlspaceLookup = worldspaceCell.GetLookup();

                if (new_subCells_ByWorldspace.ContainsKey(wordlspaceLookup) == false) new_subCells_ByWorldspace.Add(wordlspaceLookup, new Dictionary<Vector2, HexagonCellPrototype>());

                (
                    Dictionary<int, List<Vector3>> newCenterPoints_bySize,
                    Dictionary<Vector2, Vector3> islandbufferPoints,
                    Vector2Int lowestHighestPointHeight
                ) = HexGridPathingUtil.GetConsecutiveCellPointsWithintNoiseElevationRange_V7(
                    worldspaceCenterPoint,
                    300,
                    layeredNoise_terrainGlobal,
                    _global_defaultCellSize,
                    _global_terrainHeightDefault,
                    transform.position.y,
                    cellLayerElevation,
                    2
                );
                // (
                //     Dictionary<int, Dictionary<Vector2, Vector3>> newCenterPoints_bySize,
                //     Dictionary<Vector2, Vector3> islandbufferPoints,
                //     Vector2Int lowestHighestPointHeight
                // ) = HexGridPathingUtil.GetConsecutiveCellPointsWithintNoiseElevationRange_V6(
                //     worldspaceCenterPoint,
                //     300,
                //     layeredNoise_terrainGlobal,
                //     _global_defaultCellSize,
                //     _global_terrainHeightDefault,
                //     transform.position.y,
                //     cellLayerElevation,
                //     2
                // );
                // (
                //     Dictionary<int, Dictionary<Vector2, Vector3>> newCenterPoints_bySize,
                //     Dictionary<Vector2, Vector3> islandbufferPoints,
                //     Vector2Int lowestHighestPointHeight
                // ) = HexGridPathingUtil.GetConsecutiveCellPointsWithintNoiseElevationRange_V5(
                //     worldspaceCenterPoint,
                //     300,
                //     layeredNoise_terrainGlobal,
                //     _global_defaultCellSize,
                //     _global_terrainHeightDefault,
                //     transform.position.y,
                //     cellLayerElevation,
                //     2
                // );
                // (Dictionary<int, List<Vector3>> newCenterPoints_bySize, Vector2Int lowestHighestPointHeight) = HexGridPathingUtil.GetConsecutiveCellPointsWithintNoiseElevationRange_V2(
                //     worldspaceCenterPoint,
                //     300,
                //     layeredNoise_terrainGlobal,
                //     _global_defaultCellSize,
                //     _global_terrainHeightDefault,
                //     transform.position.y,
                //     cellLayerElevation,
                //     1
                // );

                Vector3[] parentCellCorners = HexCoreUtil.GenerateHexagonPoints(worldspaceCenterPoint, parentCellSize);
                int created = 0;
                foreach (int currentSize in newCenterPoints_bySize.Keys)
                {
                    foreach (Vector3 point in newCenterPoints_bySize[currentSize])
                    // foreach (Vector2 newCellLookup in newCenterPoints_bySize[currentSize].Keys)
                    {
                        // Vector3 point = newCenterPoints_bySize[currentSize][newCellLookup];
                        Vector2 newWorldCoordinate = VectorUtil.Calculate_Coordinate(point);
                        Vector2 newCellLookup = HexCoreUtil.Calculate_CenterLookup(newWorldCoordinate, currentSize);

                        if (_worldRegionsLookup.ContainsKey(wordlspaceLookup))
                        {
                            bool foundExisting = _worldRegionsLookup[wordlspaceLookup].neighbors.Any(r =>
                                new_subCells_ByWorldspace.ContainsKey(r.GetLookup()) &&
                                new_subCells_ByWorldspace[r.GetLookup()].ContainsKey(newCellLookup));

                            if (foundExisting)
                            {
                                // Debug.LogError("Existing lookupCoordinate found for WorldArea: " + newCellLookup + ", skipping duplicate");
                                continue;
                            }
                        }

                        if (HexCoreUtil.IsAnyHexPointWithinPolygon(point, currentSize, parentCellCorners))
                        {
                            // if (islandbufferPoints.ContainsKey(newCellLookup)) continue;

                            if (new_subCells_ByWorldspace[wordlspaceLookup].ContainsKey(newCellLookup) == false && neighborsToEvaluate.ContainsKey(newCellLookup) == false)
                            {
                                HexagonCellPrototype new_subCell = new HexagonCellPrototype(point, currentSize, worldspaceCell, cellLayerElevation);
                                new_subCell.SetWorldCoordinate(newWorldCoordinate);
                                new_subCell.SetParentLookup(wordlspaceLookup);
                                new_subCell.SetWorldSpaceLookup(wordlspaceLookup);

                                if (islandbufferPoints.ContainsKey(newCellLookup))
                                {
                                    new_subCell.Highlight(true);
                                    new_subCell.SetToGround(false);
                                    // new_subCell.SetPathCell(true);
                                }

                                // float pathNoiseValue = LayerdNoise.Calculate_NoiseForCoordinate((int)new_subCell.center.x, (int)new_subCell.center.z, layeredNoise_locationPath);
                                // if (pathNoiseValue > locationNoise_pathNoiseMin)
                                // {
                                //     new_subCell.SetToGround(false);
                                //     new_subCell.SetPathCell(true);
                                // }
                                else new_subCell.SetToGround(true);

                                new_subCells_ByWorldspace[wordlspaceLookup].Add(new_subCell.GetLookup(), new_subCell);

                                if (newCellLookup != new_subCell.GetLookup()) Debug.LogError("Inconsistent lookup coordinates found! - new_subCell.GetLookup(): " + new_subCell.GetLookup() + ", newCellLookup: " + newCellLookup);

                                if (neighborIDsToEvaluate.Contains(new_subCell.Get_Uid()) == false)
                                {
                                    neighborIDsToEvaluate.Add(new_subCell.Get_Uid());
                                    neighborsToEvaluate.Add(new_subCell.GetLookup(), new_subCell);
                                }
                                created++;

                                if (_subCellTerraforms_ByWorldspace.ContainsKey(wordlspaceLookup) == false) _subCellTerraforms_ByWorldspace.Add(wordlspaceLookup, new Dictionary<Vector2, HexagonCellPrototype>());
                                _subCellTerraforms_ByWorldspace[wordlspaceLookup].Add(new_subCell.GetLookup(), new_subCell);
                            }
                        }
                    }
                }

                // Debug.Log("Created " + created + " SubCells within Worldspace coordinate: " + wordlspaceLookup);
                totalCreated += created;

                Generate_WorldSpace_CellGrid_Location(worldspaceCell, lowestHighestPointHeight.x, cellLayersMax, _subCellTerraforms_ByWorldspace[wordlspaceLookup], enableLog);
            }

            if (neighborsToEvaluate.Count > 1)
            {
                if (enableLog) Debug.Log("SubCell neighbors To evaluate: " + neighborsToEvaluate.Count);
                HexCellUtil.Evaluate_WorldCellNeighbors(neighborsToEvaluate, enableLog);
            }

            if (enableLog) Debug.Log("Created " + totalCreated + " new SubCells across " + new_subCells_ByWorldspace.Count + " worldspaces");
        }


        private void GenerateWorldCells_Area(int amount = 20, bool enableLog = false)
        {
            if (_worldRegionsLookup == null || _worldRegionsLookup.Count == 0) GenerateWorldCells_Region(_worldRegionSize);
            if (_worldRegionsLookup == null)
            {
                Debug.LogError("No _worldRegionsLookup");
                return;
            }

            Vector2 initialLookup = VectorUtil.Calculate_AproximateCoordinate(transform.position);
            HexagonCellPrototype initialParentCell = _worldRegionsLookup[initialLookup];

            if (initialParentCell.neighbors.Count == 0) HexGridPathingUtil.Rehydrate_CellNeighbors(initialLookup, _worldRegionsLookup, true);
            if (initialParentCell.neighbors.Count == 0)
            {
                Debug.LogError("No neighbors for world region coordinate: " + initialLookup);
                return;
            }

            // Get Parent Cells
            List<HexagonCellPrototype> parentCellsToFill = HexGridPathingUtil.GetConsecutiveNeighborsFromStartCell(initialParentCell, _worldRegionsLookup, amount);

            Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> new_worldAreas_ByRegion = new Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>>();
            HashSet<string> neighborIDsToEvaluate = new HashSet<string>();
            Dictionary<Vector2, HexagonCellPrototype> neighborsToEvaluate = new Dictionary<Vector2, HexagonCellPrototype>();
            int totalCreated = 0;

            int cellSize = _worldAreaSize;
            int parentCellSize = _worldRegionSize;

            foreach (HexagonCellPrototype regionCell in parentCellsToFill)
            {
                Vector3 regionCenterPoint = regionCell.center;
                Vector2 regionLookup = regionCell.GetLookup();

                if (new_worldAreas_ByRegion.ContainsKey(regionLookup) == false) new_worldAreas_ByRegion.Add(regionLookup, new Dictionary<Vector2, HexagonCellPrototype>());

                int totalBufferRadius = HexCellUtil.CalculateExpandedHexRadius(cellSize, 3);
                Dictionary<Vector2, Vector3> newCenterPoints = HexGridUtil.Generate_HexagonGridCenterPoints(
                    regionCenterPoint,
                    cellSize,
                    totalBufferRadius
                );

                Vector3[] parentCellCorners = HexCoreUtil.GenerateHexagonPoints(regionCenterPoint, parentCellSize);
                int created = 0;
                foreach (Vector3 point in newCenterPoints.Values)
                {
                    Vector2 newWorldCoordinate = VectorUtil.Calculate_Coordinate(point);
                    Vector2 areaLookup = HexCoreUtil.Calculate_CenterLookup(newWorldCoordinate, cellSize);

                    if (_worldRegionsLookup.ContainsKey(regionLookup))
                    {
                        bool foundExisting = _worldRegionsLookup[regionLookup].neighbors.Any(r =>
                            new_worldAreas_ByRegion.ContainsKey(r.GetLookup()) &&
                            new_worldAreas_ByRegion[r.GetLookup()].ContainsKey(areaLookup));

                        if (foundExisting)
                        {
                            // Debug.LogError("Existing lookupCoordinate found for WorldArea: " + areaLookup + ", skipping duplicate");
                            continue;
                        }
                    }

                    if (
                        new_worldAreas_ByRegion[regionLookup].ContainsKey(areaLookup) == false && neighborsToEvaluate.ContainsKey(areaLookup) == false &&
                        HexCoreUtil.IsAnyHexPointWithinPolygon(point, cellSize, parentCellCorners))
                    {

                        HexagonCellPrototype newAreaCell = HexagonCellPrototype.New_WorldCell(CalculateCoordinateWorldPosition(newWorldCoordinate), cellSize, regionLookup);

                        new_worldAreas_ByRegion[regionLookup].Add(newAreaCell.GetLookup(), newAreaCell);

                        if (areaLookup != newAreaCell.GetLookup())
                        {
                            Debug.LogError("newAreaCell.GetLookup(): " + newAreaCell.GetLookup() + ", areaLookup: " + areaLookup);
                        }

                        if (neighborIDsToEvaluate.Contains(newAreaCell.Get_Uid()) == false)
                        {
                            neighborIDsToEvaluate.Add(newAreaCell.Get_Uid());
                            neighborsToEvaluate.Add(newAreaCell.GetLookup(), newAreaCell);
                        }

                        // Assign World Status from noise
                        float baseNoiseHeight = WorldManagerUtil.CalculateNoiseHeightForVertex((int)newAreaCell.center.x, (int)newAreaCell.center.z, _global_terrainHeightDefault, globalNoise.fastNoise, persistence, octaves, lacunarity);
                        if (baseNoiseHeight < _globalSeaLevel)
                        {
                            newAreaCell.worldCellStatus = WorldCellStatus.Ocean;
                        }
                        else newAreaCell.worldCellStatus = WorldCellStatus.Land;

                        created++;
                    }
                }

                Debug.Log("Created " + created + " WorldAreas within WorldRegion coordinate: " + regionLookup);
                totalCreated += created;
            }

            if (neighborsToEvaluate.Count > 1)
            {
                if (enableLog) Debug.Log("World Area neighbors To evaluate: " + neighborsToEvaluate.Count);
                HexCellUtil.Evaluate_WorldCellNeighbors(neighborsToEvaluate, enableLog);
            }

            _worldAreas_ByRegion = new_worldAreas_ByRegion;

            Debug.Log("Created " + totalCreated + " new WorldAreas across " + _worldAreas_ByRegion.Count + " WorldRegions");
        }


        private void GenerateWorldCells_Region(int regionSize, int radiusMult = 4, bool enableLog = false)
        {
            Dictionary<Vector2, HexagonCellPrototype> new_regionCellsByLookup = new Dictionary<Vector2, HexagonCellPrototype>();
            Dictionary<string, Vector2> newWorldRegionsLookupById = new Dictionary<string, Vector2>();

            int totalBufferRadius = HexCellUtil.CalculateExpandedHexRadius(regionSize, radiusMult);
            float regionMiles = VectorUtil.MetersToMiles(totalBufferRadius) * 2;
            Debug.Log("World Regions - totalBufferRadius: " + totalBufferRadius + ", miles: " + regionMiles);

            Dictionary<Vector2, Vector3> newCenterPoints = HexGridUtil.Generate_HexagonGridCenterPoints(
                transform.TransformPoint(transform.position),
                regionSize,
                totalBufferRadius
            );

            HashSet<string> neighborIDsToEvaluate = new HashSet<string>();
            List<HexagonCellPrototype> neighborsToEvaluate = new List<HexagonCellPrototype>();

            foreach (Vector3 centerPoint in newCenterPoints.Values)
            {
                Vector2 newCoordinate = VectorUtil.Calculate_AproximateCoordinate(centerPoint);
                Vector2 newLookup = HexCoreUtil.Calculate_CenterLookup(newCoordinate, regionSize);

                if (new_regionCellsByLookup.ContainsKey(newLookup))
                {
                    // Debug.LogError("Existing region newLookup coordinate found: " + newLookup + ", skipping duplicate");
                    continue;
                }
                else
                {
                    // HexagonCellPrototype newRegionCell = new HexagonCellPrototype(CalculateCoordinateWorldPosition(newCoordinate), regionSize, null);
                    // newRegionCell.SetWorldCoordinate(newCoordinate);
                    HexagonCellPrototype newRegionCell = HexagonCellPrototype.New_WorldCell(CalculateCoordinateWorldPosition(newCoordinate), regionSize);

                    new_regionCellsByLookup.Add(newLookup, newRegionCell);
                    newWorldRegionsLookupById.Add(newRegionCell.Get_Uid(), newLookup);

                    if (neighborIDsToEvaluate.Contains(newRegionCell.Get_Uid()) == false)
                    {
                        neighborIDsToEvaluate.Add(newRegionCell.Get_Uid());
                        neighborsToEvaluate.Add(newRegionCell);
                    }
                }
            }

            if (neighborsToEvaluate.Count > 1)
            {
                if (enableLog) Debug.Log("World Region neighbors To evaluate: " + neighborsToEvaluate.Count);
                HexCellUtil.Evaluate_WorldCellNeighbors(neighborsToEvaluate, new_regionCellsByLookup, enableLog);
            }

            _worldRegionsLookup = new_regionCellsByLookup;
            _worldRegionsLookupById = newWorldRegionsLookupById;
        }


        private HexagonGrid AddNew_WorldSpaceHexGrid(HexagonCellPrototype worldSpaceCell, Dictionary<int, List<Vector3>> baseCenterPointsBySize)
        {
            if (worldSpaceCell.HasWorldCoordinate() == false)
            {
                Debug.LogError("WorldSpaceCell is missing coordinates");
                return null;
            }

            Vector2 lookupCoord = worldSpaceCell.GetLookup();
            if (_worldSpaceCellGridByCenterCoordinate.ContainsKey(lookupCoord))
            {
                Debug.LogError("A Hex Grid already exists at coordinate: " + lookupCoord);
                return _worldSpaceCellGridByCenterCoordinate[lookupCoord];
            }

            HexagonGrid newGrid = Create_WorldSpaceCellGrid(worldSpaceCell, baseCenterPointsBySize);
            return newGrid;
        }

        public HexagonGrid Create_WorldSpaceCellGrid(HexagonCellPrototype worldSpaceCell, Dictionary<int, List<Vector3>> baseCenterPointsBySize = null)
        {
            Vector3 centerPos = worldSpaceCell.center;

            HexagonGrid newHexGrid = new HexagonGrid(
             _global_defaultCellSize,
             cellLayersMax,
             cellLayersMax,
             cellLayerElevation,
             GridPreset.Outpost
            );

            Dictionary<int, Dictionary<int, List<HexagonCellPrototype>>> newPrototypesBySizeByLayer = newHexGrid.CreateWorldSpaceCellGrid_FromBasePoints(
                                baseCenterPointsBySize,
                                worldSpaceCell,
                                _global_cellGridElevation,
                                transform
                            );
            _worldSpaceCellGridByCenterCoordinate.Add(worldSpaceCell.GetLookup(), newHexGrid);
            return newHexGrid;
        }



        public Dictionary<Vector2, TerrainVertex[,]> GetWorldSpaceNeighborVertexGrids_V2(List<HexagonCellPrototype> waCellNeighbors)
        {
            Dictionary<Vector2, TerrainVertex[,]> worldSpaceNeighborVertexGridsByCoord = new Dictionary<Vector2, TerrainVertex[,]>();

            foreach (HexagonCellPrototype waHexCell in waCellNeighbors)
            {
                if (!waHexCell.HasWorldCoordinate())
                {
                    Debug.LogError("Missing WorldSpace Coordinate!");
                    continue;
                }

                Vector2 worldLookupCoord = waHexCell.GetLookup();
                worldSpaceNeighborVertexGridsByCoord.Add(worldLookupCoord, _terrainVertexGridDataByCenterCoordinate[worldLookupCoord]);
            }
            return worldSpaceNeighborVertexGridsByCoord;
        }

        public List<HexagonCellPrototype> FindInRange_WorldCells(Dictionary<Vector2, HexagonCellPrototype> worldCellLookup, Vector2 coord, int searchRange)
        {
            if (worldCellLookup == null)
            {
                Debug.LogError("worldCellLookup is null");
                return null;
            }
            List<HexagonCellPrototype> neighborsInRange = new List<HexagonCellPrototype>();

            foreach (Vector2 worldspaceCoord in worldCellLookup.Keys)
            {
                float distance = Vector2.Distance(coord, worldspaceCoord);
                // Debug.Log("distance: " + distance + ", searchRange: " + searchRange);
                if (distance <= searchRange) neighborsInRange.Add(worldCellLookup[worldspaceCoord]);
            }
            return neighborsInRange;
        }

        public List<HexagonCellPrototype> FindInRange_WorldRegions(Vector2 coordinate, Dictionary<Vector2, HexagonCellPrototype> regionCellsByCoordinate, int searchRange)
        {
            List<HexagonCellPrototype> neighborsInRange = new List<HexagonCellPrototype>();
            foreach (var kvp in regionCellsByCoordinate)
            {
                Vector2 otherCoordinate = kvp.Key;
                float distance = Vector2.Distance(coordinate, otherCoordinate);
                // Debug.Log("distance: " + distance + ", searchRange: " + searchRange);
                if (distance <= searchRange) neighborsInRange.Add(kvp.Value);

            }
            return neighborsInRange;
        }

        public (List<HexagonGrid>, List<Vector2>) GetWorldSpaceeHexGridsInRangeOfPosition(Vector3 position, int searchRange)
        {
            List<HexagonGrid> cellGridsInRange = new List<HexagonGrid>();
            List<Vector2> wsCoordinates = new List<Vector2>();

            foreach (var kvp in _worldSpaceCellGridByCenterCoordinate)
            {
                Vector2 otherCoordinate = kvp.Key;
                float distance = VectorUtil.DistanceXZ(position, otherCoordinate);
                if (distance <= searchRange)
                {
                    HexagonGrid hexGrid = kvp.Value;
                    cellGridsInRange.Add(hexGrid);
                    wsCoordinates.Add(otherCoordinate);
                }
            }
            return (cellGridsInRange, wsCoordinates);
        }


        private void OnValidate()
        {
            wfc = GetComponent<IWFCSystem>();

            if (sceneName == null)
            {
                Scene currentScene = SceneManager.GetActiveScene();
                sceneName = currentScene.name;
            }

            if (save__default)
            {
                save__default = false;
                Save();
            }
            // if (save__chunkData)
            // {
            //     save__chunkData = false;
            //     Save__ChunkData();
            // }

            Load();

            if (bakeNavmesh)
            {
                bakeNavmesh = false;
                if (navMeshSurface != null) navMeshSurface.BuildNavMesh();
            }



            debug_currentLocationNoiseIndex = Mathf.Clamp(debug_currentLocationNoiseIndex, 0, locationNoise_ranges.Count - 1);

            if (enable_WorldPositionTracking)
            {
                if (_currentFocusPoint == null)
                {
                    Debug.LogError("loadFocusPosition is null");
                }
                else
                {
                    Evalaute_WorldParentFolder();
                    Evaluate_TrackedPosition();
                }
            }

            if (updateGlobalWorldElevationOffset || transform.position.y != _global_worldElevationOffset)
            {
                updateGlobalWorldElevationOffset = false;
                UpdateGlobalWorldElevationOffset();
            }

            UpdateGlobalCellGridElevation();

            if (trigger_reset || trigger_reset_build)
            {
                Debug.Log("Resetting Initial Grid");

                trigger_expand_World = trigger_reset_build;
                trigger_reset = false;
                trigger_reset_build = false;

                // trigger_expand_WorldSpaceCells = true;
                _currentRadius = _worldspaceSize;

                if (useRandomTerrainHeight) _global_terrainHeightDefault = CalculateRandomHeight();

                _terrainVertexGridDataByCenterCoordinate = new Dictionary<Vector2, TerrainVertex[,]>();
                _worldSpaceCellGridByCenterCoordinate = new Dictionary<Vector2, HexagonGrid>();
                _globalTerrainVertexGridByCoordinate = new Dictionary<Vector2, TerrainVertex>();

                _worldSpaceCellGridBaseCenters = new Dictionary<Vector2, Vector3>();
                _worldSpaceCellGridBaseCentersBySize = new Dictionary<Vector2, Dictionary<int, List<Vector3>>>();

                Evaluate_LocationMarkerPrefabs();
            }


            if (trigger_generate_WorldRegions || (trigger_generate_WorldAreas && (_worldRegionsLookup == null || _worldRegionsLookup.Count == 0)))
            {
                trigger_generate_WorldRegions = false;
                GenerateWorldCells_Region(_worldRegionSize);
            }
            if (trigger_generate_WorldAreas)
            {
                trigger_generate_WorldAreas = false;
                GenerateWorldCells_Area();
            }
            if (trigger_generate_WorldSpaces)
            {
                trigger_generate_WorldSpaces = false;
                GenerateWorldCells_WorldSpace();
            }

            if (trigger_generate_CurrentAreaLocations)
            {
                trigger_generate_CurrentAreaLocations = false;
                evaluate_NoiseRanges = true;
                // Generate_WorldSpace_CellGrids(AssignActiveWorldSpaces(), _currentWorldPos_AreaLookup);
            }

            if (evaluate_NoiseRanges)
            {
                evaluate_NoiseRanges = false;

                Evaluate_LocationPrefabNoiseRanges();
                Assign_LocationsToWorldArea(_currentWorldPos_AreaLookup, false);
            }

            // if (trigger_expand_WorldSpaceCells)
            // {
            //     trigger_expand_WorldSpaceCells = false;
            //     Expand_WorldSpaceCells();
            // }

            if (trigger_expand_World)
            {
                trigger_expand_World = false;

                Evalaute_WorldParentFolder();

                Evaluate_LocationMarkerPrefabs();

                _buildStartTime = DateTime.Now;

                Initialize_WorldAreas(_currentWorldPos_AreaLookup, _currentWorldPos_RegionLookup, temp_areaChunksMax > 0 ? 1 : temp_areasMax);

                TimeLog("World Build timer.");
            }

            if (randomizeTerrainHeight)
            {
                randomizeTerrainHeight = false;
                _global_terrainHeightDefault = CalculateRandomHeight();
            }
        }

        public Vector3 CalculateCoordinateWorldPosition(Vector2 coordinate) => new Vector3(coordinate.x, transform.position.y, coordinate.y);
        public Vector3 CalculateCoordinateBaseGridPosition(Vector2 coordinate) => new Vector3(coordinate.x, _global_cellGridElevation, coordinate.y);
        public float GetBaseCellGridElevation() => _global_cellGridElevation;
        public float GetInitialGridHeight() => (cellLayersMax * cellLayerElevation);
        private float CalculateRandomHeight() => UnityEngine.Random.Range(_global_terrainHeightMin, _global_terrainHeightMax);
        private float CalculateGlobalWorldElevationOffset()
        {
            float avg = UtilityHelpers.CalculateAverage(_global_terrainHeightMin, _globalSeaLevel) / 2f;
            if (_global_worldElevationOffsetMax < avg)
            {
                avg = _global_worldElevationOffsetMax;
            }
            // Debug.Log("CalculateGlobalWorldElevationOffset: " + avg);
            return avg;
        }

        private void UpdateGlobalWorldElevationOffset()
        {
            float offset = CalculateGlobalWorldElevationOffset();
            _global_worldElevationOffset = offset;

            Vector3 newPos = transform.position;
            newPos.y = _global_worldElevationOffset;
            transform.position = newPos;

            UpdateGlobalCellGridElevation();
        }

        private void UpdateGlobalCellGridElevation()
        {
            float avgA = UtilityHelpers.CalculateAverage(_globalSeaLevel, _global_worldElevationOffset);
            float avgB = UtilityHelpers.CalculateAverage(GetInitialGridHeight(), avgA);
            float offset;
            if (avgB > _global_worldElevationOffset)
            {
                offset = _global_worldElevationOffset - avgB;
            }
            else
            {
                offset = avgB;
            }
            _global_cellGridElevation = offset;
        }

        public void Update_TrackedPosition()
        {
            _worldTrackerLastPos = GetCurrentWorldFocusPosition();
            // Debug.Log("_worldTrackerLastPos updated: " + _worldTrackerLastPos);
        }

        public void SetCurrentWorldLookup_Region(Vector2 newLookup)
        {
            if (_currentWorldPos_RegionLookup != newLookup)
            {
                _currentWorldPos_RegionLookup = newLookup;
                Debug.Log("Current WorldRegion changed: " + _currentWorldPos_RegionLookup);
            }
        }
        public void SetCurrentWorldLookup_Area(Vector2 newLookup)
        {
            if (_currentWorldPos_AreaLookup != newLookup)
            {
                _currentWorldPos_AreaLookup = newLookup;
                Debug.Log("Current WorldArea changed: " + _currentWorldPos_AreaLookup);

                Evaluate_WorldAreaFolder(_currentWorldPos_AreaLookup);
            }
        }
        public void SetCurrentWorldLookup_Worldspace(Vector2 newLookup)
        {
            if (_currentWorldPos_WorldspaceLookup != newLookup)
            {
                _currentWorldPos_WorldspaceLookup = newLookup;
                // Debug.Log("Current Worldspace changed: " + _currentWorldPos_WorldspaceLookup);
            }
        }

        public void Evaluate_TrackedPosition()
        {
            float distanceChangeXZ = VectorUtil.DistanceXZ(_worldTrackerLastPos, GetCurrentWorldFocusPosition());
            if (distanceChangeXZ > global_trackerUpdateDistanceMin)
            {
                Update_TrackedPosition();

                Vector3 closest_Region = HexCoreUtil.Calculate_ClosestHexCenter(_currentFocusPoint.position, _worldRegionSize);
                Vector3 regionLookup = HexCoreUtil.Calculate_CenterLookup(closest_Region, _worldRegionSize);
                if (_worldRegionsLookup.ContainsKey(regionLookup)) SetCurrentWorldLookup_Region(regionLookup);

                Vector3 closest_Area = HexCoreUtil.Calculate_ClosestHexCenter(_currentFocusPoint.position, _worldAreaSize);
                Vector3 areaLookup = HexCoreUtil.Calculate_CenterLookup(closest_Area, _worldAreaSize);
                if (_worldAreas_ByRegion[_currentWorldPos_RegionLookup].ContainsKey(areaLookup))
                {
                    SetCurrentWorldLookup_Area(areaLookup);
                }
                else
                {
                    Debug.LogError("areaLookup not found: " + areaLookup);
                    return;
                }

                List<Vector3> closest_Worldspaces = HexCoreUtil.Calculate_ClosestHexCenterPoints_X13(_currentFocusPoint.position, _worldspaceSize);
                if (closest_Worldspaces.Count == 0)
                {
                    Debug.LogError("closest_Worldspaces NOT found");
                    return;
                }


                Vector3 closest_worldspacePos = HexCoreUtil.Calculate_ClosestHexCenter(_currentFocusPoint.position, _worldspaceSize);
                Vector3 closest_worldspaceLookup = HexCoreUtil.Calculate_CenterLookup(closest_worldspacePos, _worldspaceSize);

                HexagonCellPrototype currentWorldspaceCell = null;
                bool foundCurrentWorldspace = false;
                List<Vector3> closest_Areas = HexCoreUtil.Calculate_ClosestHexCenterPoints_X7(_currentFocusPoint.position, _worldAreaSize);

                if (_worldSpaces_ByArea.ContainsKey(areaLookup) && _worldSpaces_ByArea[areaLookup].ContainsKey(closest_worldspaceLookup))
                {
                    currentWorldspaceCell = _worldSpaces_ByArea[areaLookup][closest_worldspaceLookup];
                    SetCurrentWorldLookup_Worldspace(closest_worldspaceLookup);
                    SetCurrentWorldLookup_Area(areaLookup);
                    foundCurrentWorldspace = true;
                }
                else
                {
                    foreach (Vector2 areaCenter in closest_Areas)
                    {
                        areaLookup = HexCoreUtil.Calculate_CenterLookup(areaCenter, _worldAreaSize);

                        if (_worldAreas_ByRegion[_currentWorldPos_RegionLookup].ContainsKey(areaLookup) == false) continue;

                        if (_worldSpaces_ByArea.ContainsKey(areaLookup) && _worldSpaces_ByArea[areaLookup].ContainsKey(closest_worldspaceLookup))
                        {
                            currentWorldspaceCell = _worldSpaces_ByArea[areaLookup][closest_worldspaceLookup];
                            SetCurrentWorldLookup_Worldspace(closest_worldspaceLookup);
                            SetCurrentWorldLookup_Area(areaLookup);
                            foundCurrentWorldspace = true;
                            // Debug.Log("Found current worldspace: " + closest_worldspaceLookup);
                            break;
                        }
                    }
                }

                if (_positionTracker != null) _positionTracker.UpdatePositionData(_currentWorldPos_WorldspaceLookup, _currentWorldPos_AreaLookup, _currentWorldPos_RegionLookup);

                if (!foundCurrentWorldspace)
                {
                    Debug.LogError("worldspace NOT found");
                    return;
                }

                Evaluate_LocalCellsToLoad(closest_Areas, closest_Worldspaces);

                Load_LocalTerrainChunks();
            }
        }

        public void Evaluate_LocalCellsToLoad(List<Vector3> closest_Areas, List<Vector3> closest_Worldspaces)
        {
            // Debug.Log("closest_Worldspaces: " + closest_Worldspaces.Count + ", closest_Areas: " + closest_Areas.Count);
            List<Vector2> new_loadWorldspaceLookups = new List<Vector2>();
            List<HexagonCellPrototype> new_loadWorldspaceCells = new List<HexagonCellPrototype>();
            HashSet<Vector2> foundLookups = new HashSet<Vector2>();
            List<Vector2> temp_loadAreaLookups = new List<Vector2>();

            foreach (Vector3 worldspaceCenter in closest_Worldspaces)
            {
                Vector2 worldspaceLookup = HexCoreUtil.Calculate_CenterLookup(worldspaceCenter, _worldspaceSize);
                if (foundLookups.Contains(worldspaceLookup)) continue;
                // int i = 0;
                // Debug.Log(i + "  worldspaceLookup: " + worldspaceLookup);

                if (_worldSpaces_ByArea.ContainsKey(_currentWorldPos_AreaLookup) && _worldSpaces_ByArea[_currentWorldPos_AreaLookup].ContainsKey(worldspaceLookup))
                {
                    foundLookups.Add(worldspaceLookup);
                    new_loadWorldspaceLookups.Add(worldspaceLookup);
                    new_loadWorldspaceCells.Add(_worldSpaces_ByArea[_currentWorldPos_AreaLookup][worldspaceLookup]);
                }

                // foreach (Vector2 areaCenter in closest_Areas)
                // {
                //     Vector2 areaLookup = HexCoreUtil.Calculate_CenterLookup(areaCenter, _worldAreaSize);
                //     if (_worldAreas_ByRegion[_currentWorldPos_RegionLookup].ContainsKey(areaLookup) == false) continue;

                //     Debug.Log(i + "  areaLookup: " + areaLookup);
                //     i++;

                //     temp_loadAreaLookups.Add(areaLookup);

                //     if (_worldSpaces_ByArea.ContainsKey(areaLookup) && _worldSpaces_ByArea[areaLookup].ContainsKey(worldspaceLookup))
                //     {
                //         foundLookups.Add(worldspaceLookup);
                //         new_loadWorldspaceLookups.Add(worldspaceLookup);
                //         new_loadWorldspaceCells.Add(_worldSpaces_ByArea[areaLookup][worldspaceLookup]);
                //         break;
                //     }
                // }
            }

            if (_positionTracker != null)
            {
                _positionTracker._active_worldspaceLookups = new_loadWorldspaceLookups;
                _positionTracker._active_areaLookups = temp_loadAreaLookups;
            }

            _activeWorldspaceLookups = new_loadWorldspaceLookups;
            _activeWorldspaceCells = new_loadWorldspaceCells;
            // Debug.Log("_activeWorldspaceLookups: " + _activeWorldspaceLookups.Count);
        }

        public void Load_LocalTerrainChunks()
        {
            if (_activeWorldspaceCells == null && _activeWorldspaceCells.Count == 0)
            {
                Debug.LogError("_activeWorldspaceCells is empty");
                return;
            }

            Evalaute_WorldParentFolder();

            List<GameObject> new_loadTerrainChunks = new List<GameObject>();
            HashSet<GameObject> toLoad = new HashSet<GameObject>();

            List<Vector2> terrainChunkLookups = WorldManagerUtil.GetWorldspaceTerrainChunkLookups(_activeWorldspaceCells);

            foreach (Vector2 chunkLookup in terrainChunkLookups)
            {
                GameObject terrainObject = Get_TerrainChunkObjectByIndexData_V2(chunkLookup, true);
                if (terrainObject != null)
                {
                    new_loadTerrainChunks.Add(terrainObject);
                    toLoad.Add(terrainObject);
                    terrainObject.SetActive(true);
                }
            }

            if (_activeWorldspaceTerrainChunks != null && _activeWorldspaceTerrainChunks.Count > 0)
            {
                List<GameObject> unloadTerrainChunks = _activeWorldspaceTerrainChunks.FindAll(t => t != null && toLoad.Contains(t) == false);
                foreach (var item in unloadTerrainChunks)
                {
                    item.SetActive(false);
                }
            }
            _activeWorldspaceTerrainChunks = new_loadTerrainChunks;
            // Debug.Log("_activeWorldspaceTerrainChunks: " + _activeWorldspaceTerrainChunks.Count);
        }


        // public void AssessCurrentActiveWorldSpaces()
        // {
        //     HexagonCellPrototype currentWorldSpaceCell = GetCurrentWorldSpaceCell();

        //     if (currentWorldSpaceCell.neighbors.Count == 0) HexGridPathingUtil.Rehydrate_CellNeighbors(_currentWorldPos_AreaLookup, _currentWorldPos_WorldspaceLookup, _worldSpaces_ByArea, true);
        //     if (currentWorldSpaceCell.neighbors.Count == 0)
        //     {
        //         Debug.LogError("No neighbors for current WorldArea or WorldSpace coordinate");
        //         return;
        //     }

        //     List<HexagonCellPrototype> new_activeWorldSpaceCellsToLoad = new List<HexagonCellPrototype>();
        //     List<Vector2> new_activeWorldSpaceToLoad = new List<Vector2>();

        //     new_activeWorldSpaceCellsToLoad.Add(currentWorldSpaceCell);
        //     new_activeWorldSpaceToLoad.Add(_currentWorldPos_WorldspaceLookup);

        //     foreach (HexagonCellPrototype neighbor in currentWorldSpaceCell.neighbors)
        //     {
        //         if (new_activeWorldSpaceCellsToLoad.Contains(neighbor) == false)
        //         {
        //             new_activeWorldSpaceCellsToLoad.Add(neighbor);
        //             new_activeWorldSpaceToLoad.Add(neighbor.GetLookup());
        //         }
        //     }
        //     foreach (HexagonCellPrototype neighbor in currentWorldSpaceCell.neighbors)
        //     {
        //         if (new_activeWorldSpaceCellsToLoad.Contains(neighbor) == false)
        //         {
        //             new_activeWorldSpaceCellsToLoad.Add(neighbor);
        //             new_activeWorldSpaceToLoad.Add(neighbor.GetLookup());
        //         }
        //     }

        //     _activeWorldspaceLookups.AddRange(new_activeWorldSpaceToLoad.FindAll(c => _activeWorldspaceLookups.Contains(c) == false));
        //     _activeWorldspaceCells.AddRange(new_activeWorldSpaceCellsToLoad.FindAll(c => _activeWorldspaceCells.Contains(c) == false));

        //     Debug.Log("_activeWorldspaceCells: " + new_activeWorldSpaceCellsToLoad.Count);
        // }


        // public bool Evaluate_CurrentWorldAreaChange()
        // {
        //     // Evaluate Current World Area
        //     Vector3 currentAreaCenterPos = CalculateCoordinateWorldPosition(_currentWorldPos_AreaLookup);
        //     HexagonCellPrototype currentAreaCell = GetCurrentWorldAreaCell();
        //     Vector3[] areaCorners = HexCoreUtil.GenerateHexagonPoints(currentAreaCell.center, _worldAreaSize);

        //     if (VectorUtil.IsPointWithinPolygon(GetCurrentWorldFocusPosition(), areaCorners) == false)
        //     {

        //         List<Vector2> neighborLookups = HexagonCellPrototype.GenerateNeighborLookupCoordinates(
        //                                currentAreaCell.center,
        //                                _worldAreaSize
        //                            //    CalculateCoordinateWorldPosition(_currentWorldPos_AreaLookup),
        //                            );

        //         Debug.Log("Checking neighbor WorldAreas ... neighbor WorldArea lookups: " + neighborLookups.Count);

        //         foreach (var neighborAreaLookup in neighborLookups)
        //         {
        //             // if (neighborAreaLookup == _currentWorldPos_AreaLookup) continue;
        //             // Debug.Log("neighbor WorldArea lookup: " + neighborAreaLookup);
        //             if (_worldAreas_ByRegion[_currentWorldPos_RegionLookup].ContainsKey(neighborAreaLookup) == false) continue;

        //             Vector3 areaCenterPos = CalculateCoordinateWorldPosition(neighborAreaLookup);
        //             Vector3[] areaCornersB = HexCoreUtil.GenerateHexagonPoints(areaCenterPos, _worldAreaSize);

        //             if (VectorUtil.IsPointWithinPolygon(GetCurrentWorldFocusPosition(), areaCornersB))
        //             {
        //                 _currentWorldPos_AreaLookup = neighborAreaLookup;
        //                 Debug.Log("current WorldArea lookup updated: " + _currentWorldPos_AreaLookup);
        //                 return true;
        //                 // break
        //             }
        //         }
        //     }
        //     return false;
        // }

        // public void EvaluateActiveWorldSpaces()
        // {
        //     if (Evaluate_CurrentWorldAreaChange())
        //     {
        //         Vector2 nearestLookup = Vector3.positiveInfinity;
        //         float nearestDistance = float.MaxValue;
        //         foreach (Vector2 worldspaceLookup in _worldSpaces_ByArea[_currentWorldPos_AreaLookup].Keys)
        //         {
        //             float dist = VectorUtil.DistanceXZ(GetCurrentWorldFocusPosition(), worldspaceLookup);
        //             if (dist < nearestDistance)
        //             {
        //                 nearestDistance = dist;
        //                 nearestLookup = worldspaceLookup;
        //             }
        //         }
        //         if (nearestLookup != Vector2.positiveInfinity)
        //         {
        //             _currentWorldPos_WorldspaceLookup = nearestLookup;

        //             HexagonCellPrototype currentWorldSpaceCell = GetCurrentWorldSpaceCell();
        //             if (currentWorldSpaceCell == null)
        //             {
        //                 Debug.LogError("currentWorldSpaceCell not found!");
        //                 return;
        //             }

        //             temp_currentWS = currentWorldSpaceCell;
        //             Debug.Log("Closest worldspace: " + currentWorldSpaceCell.GetLookup() + ", distanct: " + nearestDistance);

        //             List<Vector2> new_activeWorldSpaceToLoad = new List<Vector2>();
        //             List<HexagonCellPrototype> new_activeWorldSpaceCellsToLoad = new List<HexagonCellPrototype>();

        //             new_activeWorldSpaceCellsToLoad.Add(currentWorldSpaceCell);
        //             new_activeWorldSpaceToLoad.Add(nearestLookup);

        //             if (currentWorldSpaceCell.neighbors.Count == 0) HexGridPathingUtil.Rehydrate_CellNeighbors(currentWorldSpaceCell.GetParentLookup(), _currentWorldPos_WorldspaceLookup, _worldSpaces_ByArea, true);
        //             if (currentWorldSpaceCell.neighbors.Count == 0)
        //             {
        //                 Debug.LogError("No neighbors for current WorldArea or WorldSpace coordinate");
        //                 return;
        //             }

        //             foreach (var neighbor in currentWorldSpaceCell.neighbors)
        //             {
        //                 if (new_activeWorldSpaceCellsToLoad.Contains(neighbor) == false)
        //                 {
        //                     new_activeWorldSpaceCellsToLoad.Add(neighbor);
        //                     new_activeWorldSpaceToLoad.Add(neighbor.GetLookup());
        //                 }
        //             }

        //             _activeWorldspaceLookups = new_activeWorldSpaceToLoad;
        //             _activeWorldspaceCells = new_activeWorldSpaceCellsToLoad;
        //             return;
        //         }
        //     }

        //     if (_activeWorldspaceCells == null || _activeWorldspaceCells.Count == 0)
        //     {
        //         AssessCurrentActiveWorldSpaces();
        //     }
        //     else UpdateClosestActiveWorldSpaces();
        // }

        // public void UpdateClosestActiveWorldSpaces()
        // {
        //     (Vector2 closestLookup, float closestDistance) = VectorUtil.GetClosestPoint_XZ_WithDistance(_activeWorldspaceLookups, GetCurrentWorldFocusPosition());

        //     if (_activeWorldspaceLookups.Contains(closestLookup) == false)
        //     {
        //         Debug.LogError("Error with closestLookup: " + closestLookup);
        //         return;
        //     }
        //     HexagonCellPrototype currentWorldSpaceCell = _activeWorldspaceCells.Find(c => c.GetLookup() == closestLookup);

        //     if (currentWorldSpaceCell == null)
        //     {
        //         Debug.LogError("currentWorldSpaceCell not found in _activeWorldspaceCells. Expanding search ...");

        //         Vector2 nearestLookup = Vector3.positiveInfinity;
        //         float nearestDistance = float.MaxValue;
        //         foreach (Vector2 worldspaceLookup in _worldSpaces_ByArea[_currentWorldPos_AreaLookup].Keys)
        //         {
        //             float dist = VectorUtil.DistanceXZ(worldspaceLookup, GetCurrentWorldFocusPosition());
        //             if (dist < nearestDistance)
        //             {
        //                 nearestDistance = dist;
        //                 nearestLookup = worldspaceLookup;
        //             }
        //         }

        //         if (nearestLookup != Vector2.positiveInfinity)
        //         {
        //             currentWorldSpaceCell = _worldSpaces_ByArea[_currentWorldPos_AreaLookup][nearestLookup];
        //         }
        //     }
        //     // else
        //     // {
        //     //     if (closestDistance > _worldAreaSize)
        //     //     {
        //     //         if (currentWorldSpaceCell.neighbors.Count == 0) Rehydrate_WorldSpaceNeighbors(currentWorldSpaceCell.GetParentLookup(), closestLookup, true);
        //     //         if (currentWorldSpaceCell.neighbors.Count == 0)
        //     //         {
        //     //             Debug.LogError("No neighbors for current WorldArea or WorldSpace coordinate");
        //     //             return;
        //     //         }

        //     //         HexagonCellPrototype currentWorldSpaceCell = _activeWorldspaceCells.Find(c => c.GetLookup() == closestLookup);

        //     //     }
        //     // }

        //     List<Vector2> new_activeWorldSpaceToLoad = new List<Vector2>();
        //     List<HexagonCellPrototype> new_activeWorldSpaceCellsToLoad = new List<HexagonCellPrototype>();

        //     if (currentWorldSpaceCell == null)
        //     {
        //         Debug.LogError("currentWorldSpaceCell not found!");
        //         return;
        //     }

        //     Debug.Log("Closest worldspace: " + currentWorldSpaceCell.GetLookup());
        //     new_activeWorldSpaceCellsToLoad.Add(currentWorldSpaceCell);
        //     new_activeWorldSpaceToLoad.Add(closestLookup);

        //     if (currentWorldSpaceCell.neighbors.Count == 0) HexGridPathingUtil.Rehydrate_CellNeighbors(currentWorldSpaceCell.GetParentLookup(), closestLookup, _worldSpaces_ByArea, true);
        //     if (currentWorldSpaceCell.neighbors.Count == 0)
        //     {
        //         Debug.LogError("No neighbors for current WorldArea or WorldSpace coordinate");
        //         return;
        //     }

        //     foreach (var neighbor in currentWorldSpaceCell.neighbors)
        //     {
        //         if (new_activeWorldSpaceCellsToLoad.Contains(neighbor) == false)
        //         {
        //             new_activeWorldSpaceCellsToLoad.Add(neighbor);
        //             new_activeWorldSpaceToLoad.Add(neighbor.GetLookup());
        //         }
        //     }

        //     // (Vector2 closestLookup2, float closestDistance2) = VectorUtil.GetClosestPoint_XZ_WithDistance(new_activeWorldSpaceToLoad, GetCurrentWorldFocusPosition());
        //     // foreach (var worldspaceLookup in _activeWorldspaceLookups)
        //     // {
        //     //     float dist = VectorUtil.DistanceXZ(worldspaceLookup, GetCurrentWorldFocusPosition());
        //     //     if (dist < global_worldspaceLoadRadius)
        //     //     {
        //     //         new_activeWorldSpaceToLoad.Add(worldspaceLookup);
        //     //         nearestLookup = worldspaceLookup;
        //     //     }

        //     // }

        //     _activeWorldspaceLookups = new_activeWorldSpaceToLoad;
        //     _activeWorldspaceCells = new_activeWorldSpaceCellsToLoad;
        // }

        Bounds temp_gridBounds;
        private HexagonCellPrototype temp_currentWS;

        Dictionary<string, Color> customColors = null;

        private void OnDrawGizmos()
        {
            if (customColors == null) customColors = UtilityHelpers.CustomColorDefaults();

            // List<Vector3> chunkPoints = VectorUtil.HexagonCornersToRectangleCorner_2(transform.position, _worldspaceSize);
            // // foreach (Vector3 point in chunkPoints)
            // // {
            // //     Gizmos.color = Color.yellow;
            // //     Gizmos.DrawSphere(point, 12f);
            // // }
            // List<Vector3> pts = VectorUtil.CalculateGoldenRatioPattern(new Vector2(5, 5), 600);
            // List<Vector3> pts = VectorUtil.GenerateVoronoiDiagram(_worldAreaSize, 50);
            // List<Vector3> pts = PlotLocationPoints(_currentWorldPos_AreaLookup, 50);
            // foreach (Vector3 point in pts)
            // {
            //     Gizmos.color = Color.green;
            //     Gizmos.DrawSphere(point, _worldspaceSize / 2);
            //     // Gizmos.DrawSphere(point, _worldspaceSize / 3f);
            // }

            // List<Vector3> sectionPoints = HexCoreUtil.GenerateHexCenterPoints_X7(CalculateCoordinateWorldPosition(_currentWorldPos_AreaLookup), _worldAreaSize / 3);
            // foreach (Vector3 point in sectionPoints)
            // {
            //     Gizmos.color = Color.red;
            //     Gizmos.DrawWireSphere(point, _worldAreaSize / 3);
            // }


            // Vector2 stepSizes = VectorUtil.CalculateStepSizes(VectorUtil.CalculateBounds(chunkPoints));
            // Debug.LogError("stepSizes: " + stepSizes);
            // Vector2 stepSizes = VectorUtil.CalculateStepSizes(chunkPoints);

            // // HexagonCellPrototype temp = new HexagonCellPrototype(transform.position, _worldspaceSize * 2);
            // HexagonCellPrototype temp = _worldAreas_ByRegion[_currentWorldPos_RegionLookup][_currentWorldPos_AreaLookup];
            // // HexagonCellPrototype temp = _worldSpaces_ByArea[_currentWorldPos_AreaLookup][_currentWorldPos_WorldspaceLookup];
            // foreach (Vector3 point in temp.cornerPoints.ToList())
            // {
            //     Gizmos.color = Color.green;
            //     Gizmos.DrawSphere(point, 12f);
            // }

            if (show_AreaChunks)
            {
                if (_worldAreas_ByRegion != null && _worldAreas_ByRegion.Count > 0)
                {
                    Vector2 initialCoord = CalculateCoordinateWorldPosition(transform.position);
                    Bounds bds = VectorUtil.CalculateBounds(_worldAreas_ByRegion[_currentWorldPos_RegionLookup][_currentWorldPos_AreaLookup].cornerPoints.ToList());


                    Bounds bounds = VectorUtil.CalculateBounds_V2(GetWorldspaceMeshChunkCornerPoints(_worldSpaces_ByArea[_currentWorldPos_AreaLookup][_currentWorldPos_WorldspaceLookup], meshChunkSize));
                    Gizmos.color = Color.green;
                    VectorUtil.DrawRectangleLines(bounds);

                    // Vector3[] boundsCorners = VectorUtil.GetBoundsCorners(bds);
                    // foreach (Vector3 point in boundsCorners)
                    // {
                    //     Gizmos.color = Color.yellow;
                    //     Gizmos.DrawSphere(point, 108f);
                    // }

                    // List<Vector3[]> silces = VectorUtil.DivideRectangle(boundsCorners, 6);
                    List<Vector2[]> silces = VectorUtil.DivideBoundsIntoChunks(bds, areaChunks, areaChunkOverlap);
                    Gizmos.color = Color.cyan;
                    VectorUtil.DrawRectangleLines(bds);
                    foreach (Vector2[] corners in silces)
                    {
                        Gizmos.color = Color.cyan;
                        VectorUtil.DrawRectangleLines(VectorUtil.CalculateBounds(corners.ToList()));
                        foreach (Vector2 point in corners)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawSphere(new Vector3(point.x, 0, point.y), 90f);
                        }
                    }
                }
            }

            // List<Vector3> points = VectorUtil.HexagonCornersToRectangleCorner_2(transform.position, _worldAreaSize);
            // foreach (Vector3 point in points)
            // {
            //     Gizmos.color = Color.red;
            //     Gizmos.DrawSphere(point, 90f);
            //     // Gizmos.DrawSphere(new Vector3(point.x, 0, point.y), 12);
            // }

            // HexagonCellPrototype temp = _worldSpaces_ByArea[_currentWorldPos_AreaLookup][_currentWorldPos_WorldspaceLookup];
            // Gizmos.color = Color.blue;
            // Gizmos.DrawSphere(temp.center, 12f);

            // // List<Vector3> gridPts = GetWorldspaceMeshChunkCornerPoints(temp, meshChunkSize);
            // Bounds bds = VectorUtil.CalculateBounds_V2(GetWorldspaceMeshChunkCornerPoints(temp, meshChunkSize));
            // VectorUtil.DrawRectangleLines(bds);

            if (temp_gridBounds != null)
            {
                Gizmos.color = Color.yellow;
                VectorUtil.DrawRectangleLines(temp_gridBounds);
            }

            if (temp_chunkCenters != null)
            {
                foreach (Vector3 point in temp_chunkCenters)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(new Vector3(point.x, 0, point.y), 6);
                }
            }

            if (temp_globalGrid != null)
            {
                foreach (Vector3 point in temp_globalGrid.Values)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(point, 0.5f);
                }
            }

            if (show_TerrainChunkGrid)
            {
                if (_worldSpaces_ByArea != null && _worldSpaces_ByArea.Count > 0)
                {
                    Gizmos.color = Color.black;
                    foreach (var kvp in _worldSpaces_ByArea.Values)
                    {
                        foreach (HexagonCellPrototype cell in kvp.Values)
                        {
                            Gizmos.color = Color.red;
                            for (int i = 0; i < cell.sidePoints.Length; i++)
                            {
                                if (i == 0 || i == 3) continue;
                                // Gizmos.DrawSphere(cell.sidePoints[i], 6f);
                                Gizmos.color = Color.red;
                                Vector3[] corners = VectorUtil.GenerateRectangleCorners(cell.sidePoints[i], meshChunkSize.x, meshChunkSize.y);
                                VectorUtil.DrawRectangleLines(corners);
                            }
                        }
                    }
                }
            }

            if (show_TerrainChunkLookups)
            {
                if (_terrainChunkData_ByLookup != null && _terrainChunkData_ByLookup.Count > 0)
                {
                    Gizmos.color = Color.red;
                    foreach (TerrainChunkData data in _terrainChunkData_ByLookup.Values)
                    {
                        if (data.worldAreaLookup != _currentWorldPos_AreaLookup) continue;
                        // Gizmos.DrawSphere(new Vector3(data.chunkLookup.x, 0, data.chunkLookup.y), 12f);

                        Vector3 centerPt = new Vector3(data.chunkCoordinate.x, 0, data.chunkCoordinate.y);
                        Gizmos.DrawSphere(centerPt, 12f);

                        Bounds bounds = VectorUtil.GenerateBoundsFromCenter(terrainChunkSizeXZ, centerPt);
                        VectorUtil.DrawRectangleLines(bounds);
                    }
                }
            }

            //Location Tracking / Loading
            if ((show_loadTrackerCells || enable_WorldPositionTracking || show_LoadedWorldspaces) && _currentFocusPoint != null)
            {

                if (enable_WorldPositionTracking)
                {
                    Evaluate_TrackedPosition();

                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(_currentFocusPoint.position, 0.5f);
                    Gizmos.DrawWireSphere(_currentFocusPoint.position, global_worldspaceLoadRadius);

                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(_worldTrackerLastPos, 0.5f);
                    Gizmos.DrawWireSphere(_worldTrackerLastPos, global_trackerUpdateDistanceMin);
                }

                if (show_loadTrackerCells)
                {
                    int cSize = _worldspaceSize;
                    List<Vector3> closestCenters = HexCoreUtil.Calculate_ClosestHexCenterPoints_X13(_currentFocusPoint.position, cSize);
                    foreach (Vector3 point in closestCenters)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireSphere(point, cSize / 3);
                        // HexagonCellPrototype.DrawHexagonCellPrototype(
                        //     new HexagonCellPrototype(new Vector3(point.x, transform.position.y, point.z), cSize, false),
                        //     (cSize / 3),
                        //     false,
                        //     false,
                        //     false
                        //     );
                    }

                    if (show_WorldAreas)
                    {
                        cSize = _worldAreaSize;
                        closestCenters = HexCoreUtil.Calculate_ClosestHexCenterPoints_X7(_currentFocusPoint.position, cSize);
                        foreach (Vector3 point in closestCenters)
                        {
                            Gizmos.color = Color.magenta;
                            Gizmos.DrawWireSphere(point, cSize / 3);
                        }
                    }

                    if (show_WorldRegions)
                    {
                        cSize = _worldRegionSize;
                        closestCenters = HexCoreUtil.Calculate_ClosestHexCenterPoints_X7(_currentFocusPoint.position, cSize);
                        foreach (Vector3 point in closestCenters)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawWireSphere(point, cSize / 3);
                        }
                    }
                }

                if (show_LoadedWorldspaces && _activeWorldspaceCells != null)
                {
                    foreach (HexagonCellPrototype cell in _activeWorldspaceCells)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawSphere(cell.center, 6f);
                        VectorUtil.DrawHexagonPointLinesInGizmos(cell.cornerPoints);
                    }
                }
            }

            if (showGlobalVertexGrid && _globalTerrainVertexGridCoordinates != null && _globalTerrainVertexGridByCoordinate != null)
            {
                // if (_gridChunks == null) _gridChunks = GenerateVertexGridChunks(_globalTerrainVertexGridCoordinates, 2);
                if (showGridChunks && _gridChunks == null) showGridChunks = false;

                if (showGridChunks == false)
                {
                    foreach (Vector2 coord in _globalTerrainVertexGridCoordinates)
                    {
                        TerrainVertex vertex = _globalTerrainVertexGridByCoordinate[coord];
                        TerrainVertexUtil.DisplayTerrainVertex(vertex, showVerticesType, transform);
                    }
                }
                else
                {
                    foreach (var chunk in _gridChunks)
                    {
                        foreach (var index in chunk)
                        {
                            TerrainVertex vertex = _globalTerrainVertexGridByCoordinate[index];
                            TerrainVertexUtil.DisplayTerrainVertex(vertex, showVerticesType, transform);
                        }
                    }
                }
            }

            if (show_Locations)
            {
                if (_locationPrefabs_ByWorldspace_ByArea != null && _locationPrefabs_ByWorldspace_ByArea.Count > 0)
                {
                    foreach (var areaLookup in _locationPrefabs_ByWorldspace_ByArea.Keys)
                    {
                        foreach (var kvp in _locationPrefabs_ByWorldspace_ByArea[areaLookup])
                        {
                            HexagonCellPrototype worldspaceCell = _worldSpaces_ByArea[areaLookup][kvp.Key];
                            LocationPrefab locationPrefab = kvp.Value;

                            int rad = worldspaceCell.size / 2;
                            // Debug.Log("locationMarkerPrefab: " + locationPrefab.name + ", color: " + Color.green);
                            Gizmos.color = (Color)locationPrefab.color;
                            Gizmos.DrawSphere(worldspaceCell.center, rad);
                        }
                    }
                }

                // Color orange = new Color(1f, 0.5f, 0f);
                // foreach (var kvp in _worldSpace_LocationData_ByCoordinate)
                // {
                //     foreach (LocationData locData in kvp.Value)
                //     {
                //         if (locData.locationType == LocationType.Tunnel)
                //         {
                //             Gizmos.color = Color.red;
                //             Gizmos.DrawWireSphere(locData.centerPosition, locData.radius);
                //         }
                //         else
                //         {
                //             Gizmos.color = orange;
                //             Gizmos.DrawSphere(locData.centerPosition, 9);

                //             // Gizmos.color = Color.cyan;
                //             Gizmos.DrawWireSphere(locData.centerPosition, locData.radius);

                //             Gizmos.color = Color.red;
                //             Gizmos.DrawWireSphere(locData.centerPosition, 108);
                //         }

                //         foreach (var member in locData.cluster.prototypes)
                //         {
                //             if (member.isTunnel)
                //             {
                //                 Gizmos.color = Color.black;
                //             }
                //             else Gizmos.color = Color.cyan;

                //             // Gizmos.DrawWireSphere(member.center, temp_gridPathRadius);
                //             Gizmos.DrawWireSphere(member.center, 6f);
                //         }
                //     }
                // }
            }

            if (show_SubCells && _subCellTerraforms_ByWorldspace != null && _subCellTerraforms_ByWorldspace.Count > 0)
            {
                Gizmos.color = Color.black;

                if (show_NoiseHighlights)
                {
                    foreach (var kvp in _subCellTerraforms_ByWorldspace.Values)
                    {
                        foreach (HexagonCellPrototype cell in kvp.Values)
                        {
                            int rad = cell.size / 3;

                            if (showHighlights && cell.isHighlighted)
                            {
                                rad /= 2;
                                Gizmos.color = Color.grey;
                                Gizmos.DrawSphere(cell.center, rad);
                                Gizmos.color = Color.black;
                                VectorUtil.DrawHexagonPointLinesInGizmos(cell.cornerPoints);
                                continue;
                            }

                            // float pathNoiseValue = LayerdNoise.Calculate_NoiseForCoordinate((int)cell.center.x, (int)cell.center.z, layeredNoise_locationPath);
                            // if (pathNoiseValue > locationNoise_pathNoiseMin)
                            // {
                            //     Gizmos.color = Color.green;
                            //     Gizmos.DrawSphere(cell.center, rad);
                            // }
                            // else
                            // {
                            Gizmos.color = customColors["brown"];
                            Gizmos.DrawSphere(cell.center, rad);
                            Gizmos.color = Color.black;
                            VectorUtil.DrawHexagonPointLinesInGizmos(cell.cornerPoints);
                            // }
                            // float locationNoiseValue = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)cell.center.x, (int)cell.center.z, _global_terrainHeightDefault, layeredNoise_locationMarker);

                            // // float locationNoiseValue = GetNoiseHeightValue((int)cell.center.x, (int)cell.center.z, locationSubNoise.fastNoise, locationNoise_persistence, 2, locationNoise_lacunarity);
                            // // float baseNoiseHeight = CalculateNoiseHeightForVertex((int)cell.center.x, (int)cell.center.z, _global_terrainHeightDefault, globalNoise.fastNoise, persistence, octaves, lacunarity);
                            // if (locationNoiseValue < _globalSeaLevel)
                            // {
                            //     // Gizmos.color = Color.black;
                            //     // VectorUtil.DrawHexagonPointLinesInGizmos(cell.cornerPoints);
                            //     // Gizmos.DrawSphere(cell.center, rad);
                            // }
                            // else
                            // {
                            // float noiseHeight = GetNoiseHeightValue((int)cell.center.x, (int)cell.center.z, globalNoise.fastNoise, persistence, octaves, lacunarity);
                            // float baseNoiseHeight = CalculateNoiseHeightForVertex((int)cell.center.x, (int)cell.center.z, _global_terrainHeightDefault, noise_area, persistence, octaves, lacunarity);
                            // baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, locationNoiseValue, locationNoise_terrainBlendMult);
                            // baseNoiseHeight += transform.position.y;

                            // baseNoiseHeight = UtilityHelpers.RoundHeightToNearestElevation(baseNoiseHeight, cellLayerElevation);

                            // Gizmos.color = customColors["brown"];
                            // Gizmos.DrawSphere(cell.center, rad);
                            // // Gizmos.DrawSphere(new Vector3(cell.center.x, baseNoiseHeight, cell.center.z), rad);
                            // Gizmos.color = Color.black;
                            // VectorUtil.DrawHexagonPointLinesInGizmos(cell.cornerPoints);

                        }
                    }
                }
                else
                {
                    HexagonCellPrototype.DrawHexagonCellPrototypeGrid_WithTracking(
                        _subCellTerraforms_ByWorldspace,
                        _currentFocusPoint,
                        show_WorldPositionTracking,
                        Color.magenta
                    );
                }
            }

            if (showCellGrids)
            {
                if (cellLookup_ByLayer_BySize_ByWorldSpace != null && cellLookup_ByLayer_BySize_ByWorldSpace.Count > 0)
                {
                    foreach (var pairs in cellLookup_ByLayer_BySize_ByWorldSpace.Values)
                    {
                        foreach (var kvp in pairs)
                        {
                            int currentSize = kvp.Key;
                            if (currentSize != (int)gridFilter_size) continue;

                            HexagonCellPrototype.DrawHexagonCellPrototypeGrid(
                                pairs[currentSize],
                                gameObject.transform,
                                gridFilter_Type,
                                GridFilter_Level.HostCells,
                                cellDisplayType,
                                false,
                                showHighlights
                            );
                        }

                    }
                }
            }

            if (show_Worldspaces)
            {
                if (_worldSpaces_ByArea != null && _worldSpaces_ByArea.Count > 0)
                {

                    if (show_NoiseHighlights)
                    {
                        foreach (var kvp in _worldSpaces_ByArea.Values)
                        {
                            foreach (HexagonCellPrototype cell in kvp.Values)
                            {
                                int rad = cell.size / 2;

                                if (cell.isHighlighted)
                                {
                                    Gizmos.color = Color.yellow;
                                    Gizmos.DrawSphere(cell.center, rad);

                                    Gizmos.color = Color.black;
                                    VectorUtil.DrawHexagonPointLinesInGizmos(cell.cornerPoints);
                                    continue;
                                }

                                // float baseNoiseHeight = CalculateNoiseHeightForVertex((int)cell.center.x, (int)cell.center.z, _global_terrainHeightDefault, noise_regional, persistence, octaves, lacunarity);
                                float baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)cell.center.x, (int)cell.center.z, _global_terrainHeightDefault, layeredNoise_terrainGlobal);
                                if (baseNoiseHeight < _globalSeaLevel)
                                {
                                    Gizmos.color = Color.blue;
                                    Gizmos.DrawSphere(cell.center, rad);
                                }
                                else
                                {
                                    float locationNoiseValue = LayerdNoise.Calculate_NoiseForCoordinate((int)cell.center.x, (int)cell.center.z, layeredNoise_locationMarker);

                                    // float noiseHeight = GetNoiseHeightValue((int)cell.center.x, (int)cell.center.z, locationNoise.fastNoise, locationNoise_persistence, 2, locationNoise_lacunarity);
                                    Vector2 noiseRange = locationNoise_ranges[debug_currentLocationNoiseIndex];
                                    if (locationNoiseValue < noiseRange.y && locationNoiseValue > noiseRange.x)
                                    {
                                        rad = cell.size / 2;
                                        Gizmos.color = Color.cyan;
                                        Gizmos.DrawSphere(cell.center, rad);
                                    }
                                    else
                                    {
                                        float pathNoiseValue = LayerdNoise.Calculate_NoiseForCoordinate((int)cell.center.x, (int)cell.center.z, layeredNoise_regionPath);
                                        if (pathNoiseValue > locationNoise_pathNoiseMin)
                                        {
                                            rad = cell.size / 2;
                                            Gizmos.color = Color.green;
                                            Gizmos.DrawSphere(cell.center, rad);
                                        }
                                        else
                                        {
                                            // Gizmos.color = customColors["brown"];
                                            // Gizmos.DrawSphere(cell.center, cell.size / 2f);
                                            Gizmos.color = Color.black;
                                            VectorUtil.DrawHexagonPointLinesInGizmos(cell.cornerPoints);
                                        }

                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Gizmos.color = Color.black;
                        HexagonCellPrototype.DrawHexagonCellPrototypeGrid_WithTracking(
                            _worldSpaces_ByArea,
                            _currentFocusPoint,
                            show_WorldPositionTracking,
                            Color.blue
                        );
                    }

                    //     foreach (var kvp in _worldSpaces_ByArea.Values)
                    //     {
                    //         foreach (HexagonCellPrototype cell in kvp.Values)
                    //         {
                    //             if (show_LoadedWorldspaces && _activeWorldspaceCells != null && _activeWorldspaceCells.Contains(cell)) continue;
                    //             Gizmos.color = Color.black;
                    //             if (cell.IsRemoved())
                    //             {
                    //                 Gizmos.color = Color.yellow;
                    //                 Gizmos.DrawSphere(cell.center, 108f);
                    //             }
                    //             // Gizmos.DrawSphere(cell.center, cell.IsRemoved() ? 108 : 8);
                    //             VectorUtil.DrawHexagonPointLinesInGizmos(cell.cornerPoints);
                    //         }
                    //     }
                    // }
                }
            }

            if (show_WorldAreas)
            {
                if (_worldAreas_ByRegion != null && _worldAreas_ByRegion.Count > 0)
                {
                    Gizmos.color = Color.magenta;

                    if (show_NoiseHighlights)
                    {
                        foreach (var kvp in _worldAreas_ByRegion.Values)
                        {
                            foreach (HexagonCellPrototype cell in kvp.Values)
                            {
                                int rad = cell.size / 3;

                                if (cell.worldCellStatus != WorldCellStatus.Unset)
                                {
                                    if (cell.worldCellStatus == WorldCellStatus.Ocean)
                                    {
                                        Gizmos.color = Color.blue;
                                        Gizmos.DrawSphere(cell.center, rad);
                                    }
                                    else
                                    {
                                        Gizmos.color = Color.magenta;
                                        VectorUtil.DrawHexagonPointLinesInGizmos(cell.cornerPoints);
                                    }
                                    continue;
                                }

                                float baseNoiseHeight = WorldManagerUtil.CalculateNoiseHeightForVertex((int)cell.center.x, (int)cell.center.z, _global_terrainHeightDefault, globalNoise.fastNoise, persistence, octaves, lacunarity);
                                if (baseNoiseHeight < _globalSeaLevel)
                                {
                                    Gizmos.color = Color.blue;
                                    Gizmos.DrawSphere(cell.center, rad);
                                }
                                else
                                {
                                    // float noiseHeight = GetNoiseHeightValue((int)cell.center.x, (int)cell.center.z, globalNoise.fastNoise, persistence, octaves, lacunarity);
                                    // if (noiseHeight < locationNoise_RangeMax && noiseHeight > locationNoise_RangeMin)
                                    // {
                                    //     rad = cell.size / 2;
                                    //     Gizmos.color = Color.cyan;
                                    //     Gizmos.DrawSphere(cell.center, rad);
                                    // }
                                    // else
                                    // {
                                    Gizmos.color = Color.magenta;
                                    VectorUtil.DrawHexagonPointLinesInGizmos(cell.cornerPoints);
                                }
                            }
                        }
                    }
                    else
                    {
                        HexagonCellPrototype.DrawHexagonCellPrototypeGrid_WithTracking(
                            _worldAreas_ByRegion,
                            _currentFocusPoint,
                            show_WorldPositionTracking,
                            Color.magenta
                        );
                    }

                }
            }

            if (show_WorldRegions)
            {
                if (_worldRegionsLookup != null && _worldRegionsLookup.Count > 0)
                {
                    foreach (HexagonCellPrototype cell in _worldRegionsLookup.Values)
                    {
                        Gizmos.color = Color.red;
                        VectorUtil.DrawHexagonPointLinesInGizmos(cell.cornerPoints);
                        int rad = cell.size / 3;
                        if (cell.IsEdge())
                        {
                            Gizmos.color = Color.black;
                            Gizmos.DrawSphere(cell.center, rad);
                        }
                        else
                        {
                            if (show_WorldPositionTracking && VectorUtil.DistanceXZ(cell.center, _currentFocusPoint.position) < cell.size)
                            {

                                if (cell.neighbors.Count > 0)
                                {
                                    Gizmos.color = Color.yellow;
                                    Gizmos.DrawSphere(cell.center, rad);

                                    foreach (var item in cell.neighbors)
                                    {
                                        // Debug.Log("neighbor lookup: " + item.GetLookup());
                                        Gizmos.color = Color.green;
                                        Gizmos.DrawSphere(item.center, cell.size / 2);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (show_WorldBounds)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position, _currentRadius);
                HexagonCellPrototype totalWorldBoundsHex = new HexagonCellPrototype(transform.position, ((_worldAreaSize * 3) * 3) * 3, false);
                VectorUtil.DrawHexagonPointLinesInGizmos(totalWorldBoundsHex.cornerPoints);
                // Gizmos.DrawWireSphere(transform.position, (((_worldAreaSize * 3) * 3) * 3));
            }

            if (showPathGrid && _preassignedWorldspaceCells != null && _preassignedWorldspaceCells.Count > 0)
            {
                List<Vector3> wsCorners = new List<Vector3>();
                foreach (var item in _preassignedWorldspaceCells)
                {
                    wsCorners.AddRange(item.cornerPoints);
                }
                Bounds bounds = VectorUtil.CalculateBounds(wsCorners);
                List<Vector2> dottedGrid = VectorUtil.GenerateDottedGridLines(bounds, temp_gridPathCellWidth, temp_gridPathCellHeight, temp_gridPath_lineDensity);
                // List<Vector2> dottedGrid = VectorUtil.GenerateDottedGridLines(bounds, temp_gridPath_columns, temp_gridPath_rows, temp_gridPath_lineDensity);
                Gizmos.color = Color.blue;
                foreach (var item in dottedGrid)
                {
                    Gizmos.DrawSphere(new Vector3(item.x, 0, item.y), temp_gridPathRadius);
                }
            }
        }


        public void SetupCellGridState(
            Dictionary<Vector2, TerrainVertex> globalTerrainVertexGridByCoordinate,
            Vector2[,] worldspaceVertexKeys,
            HexagonGrid hexagonGrid,
            Transform transform,
            float viableFlatGroundCellSteepnessThreshhold,
            float cellVertexSearchRadiusMult = 1.4f,
            float groundSlopeElevationStep = 0.45f,
            float seaLevel = 0f
        )
        {
            Dictionary<int, List<HexagonCellPrototype>> gridCellPrototypesByLayer = hexagonGrid.GetDefaultPrototypesByLayer();

            HexGridVertexUtil.AssignTerrainVerticesToPrototypeGrid_V2(
                gridCellPrototypesByLayer,
                globalTerrainVertexGridByCoordinate,
                worldspaceVertexKeys,
                null,
                cellLayerElevation,
                viableFlatGroundCellSteepnessThreshhold,
                true,
                false,
                cellVertexSearchRadiusMult,
                seaLevel
            );
        }


        public static int[] GenerateTerrainTriangles_V2(Vector2[,] worldspaceVertexKeys, HashSet<Vector2> meshTriangleExcludeList = null)
        {
            int numVerticesX = worldspaceVertexKeys.GetLength(0);
            int numVerticesZ = worldspaceVertexKeys.GetLength(1);

            // Calculate the number of triangles in the terrain
            int numTrianglesX = numVerticesX - 1;
            int numTrianglesZ = numVerticesZ - 1;
            int numTriangles = numTrianglesX * numTrianglesZ * 6;

            // Create an array to store the triangle indices
            int[] triangles = new int[numTriangles];

            // Iterate through the grid and create the triangles
            int index = 0;
            for (int x = 0; x < numTrianglesX; x++)
            {
                for (int z = 0; z < numTrianglesZ; z++)
                {
                    Vector2 vertexKey = worldspaceVertexKeys[x, z];

                    if (meshTriangleExcludeList != null && meshTriangleExcludeList.Contains(vertexKey))
                    {
                        continue;
                    }

                    int vertexIndex = x + z * numVerticesX;
                    triangles[index++] = vertexIndex;
                    triangles[index++] = vertexIndex + numVerticesX + 1;
                    triangles[index++] = vertexIndex + 1;

                    triangles[index++] = vertexIndex;
                    triangles[index++] = vertexIndex + numVerticesX;
                    triangles[index++] = vertexIndex + numVerticesX + 1;
                }
            }

            return triangles;
        }

        // public static int[] GenerateTerrainTriangles_V2(Vector2[,] worldspaceVertexKeys, HashSet<Vector2> meshTriangleExcludeList = null)
        // {
        //     int numVerticesX = worldspaceVertexKeys.GetLength(0);
        //     int numVerticesZ = worldspaceVertexKeys.GetLength(1);

        //     // Calculate the number of triangles in the terrain
        //     int numTrianglesX = numVerticesX - 1;
        //     int numTrianglesZ = numVerticesZ - 1;
        //     int numTriangles = numTrianglesX * numTrianglesZ * 6;

        //     // Create an array to store the triangle indices
        //     int[] triangles = new int[numTriangles];

        //     // Iterate through the grid and create the triangles
        //     int index = 0;
        //     for (int x = 0; x < numTrianglesX; x++)
        //     {
        //         for (int z = 0; z < numTrianglesZ; z++)
        //         {
        //             Vector2 vertexKey = worldspaceVertexKeys[x, z];

        //             if (meshTriangleExcludeList != null && meshTriangleExcludeList.Contains(vertexKey))
        //             {
        //                 continue;
        //             }

        //             int vertexIndex = x + z * numVerticesX;
        //             triangles[index++] = vertexIndex;
        //             triangles[index++] = vertexIndex + 1;
        //             triangles[index++] = vertexIndex + numVerticesX + 1;

        //             triangles[index++] = vertexIndex;
        //             triangles[index++] = vertexIndex + numVerticesX + 1;
        //             triangles[index++] = vertexIndex + numVerticesX;
        //         }
        //     }

        //     return triangles;
        // }

        public static int[] GenerateTerrainTriangles(Vector2[,] worldspaceVertexKeys, HashSet<Vector2> meshTriangleExcludeList = null)
        {
            int numVerticesX = worldspaceVertexKeys.GetLength(0);
            int numVerticesZ = worldspaceVertexKeys.GetLength(1);
            // Create an array to store the triangle indices
            int[] triangles = new int[(numVerticesZ - 1) * (numVerticesX - 1) * 6];
            // Iterate through the grid and create the triangles
            int index = 0;
            for (int x = 0; x < numVerticesX - 1; x++)
            {
                for (int z = 0; z < numVerticesZ - 1; z++)
                {
                    if (meshTriangleExcludeList != null && meshTriangleExcludeList.Contains(worldspaceVertexKeys[z, x]))
                    {
                        continue;
                    }

                    int vertexIndex = x + z * numVerticesX;
                    triangles[index++] = vertexIndex;
                    triangles[index++] = vertexIndex + 1;
                    triangles[index++] = vertexIndex + numVerticesX;

                    triangles[index++] = vertexIndex + 1;
                    triangles[index++] = vertexIndex + numVerticesX + 1;
                    triangles[index++] = vertexIndex + numVerticesX;
                }
            }
            return triangles;
        }

        private void CalculateMeshChunks(Vector2[,] worldspaceVertexKeys, int maxChunkSize)
        {
            if (_minWorldSpaces == 1)
            {
                _currentMeshChunkDivider = 1;
            }

            int gridSizeX = worldspaceVertexKeys.GetLength(0);
            int gridSizeZ = worldspaceVertexKeys.GetLength(1);

            int chunkSizeX = Mathf.CeilToInt((float)gridSizeX / maxChunkSize);
            int chunkSizeZ = Mathf.CeilToInt((float)gridSizeZ / maxChunkSize);

            int numChunks = chunkSizeX * chunkSizeZ;

            while (numChunks % 2 != 0)
            {
                // If the number of chunks is not divisible by 2, decrease the chunk size until it is
                maxChunkSize -= 1;
                chunkSizeX = Mathf.CeilToInt((float)gridSizeX / maxChunkSize);
                chunkSizeZ = Mathf.CeilToInt((float)gridSizeZ / maxChunkSize);
                numChunks = chunkSizeX * chunkSizeZ;
            }

            _currentMeshChunkDivider = numChunks;
        }


        public static List<Vector2> DivideBoundsIntoChunks(Bounds bounds, Vector2Int chunksXZ)
        {
            List<Vector2> chunkCenters = new List<Vector2>();

            float chunkSizeX = bounds.size.x / chunksXZ.x;
            float chunkSizeZ = bounds.size.z / chunksXZ.y;

            Vector3 startPoint = bounds.min + new Vector3(chunkSizeX / 2f, 0f, chunkSizeZ / 2f);

            for (int x = 0; x < chunksXZ.x; x++)
            {
                for (int z = 0; z < chunksXZ.y; z++)
                {
                    Vector3 center = startPoint + new Vector3(chunkSizeX * x, 0f, chunkSizeZ * z);
                    chunkCenters.Add(new Vector2(center.x, center.z));
                }
            }

            return chunkCenters;
        }


        public static Vector2Int CalculateMeshChunkCount(Bounds bounds, Vector2 meshChunkSize)
        {
            float chunkCountX = Mathf.FloorToInt(bounds.size.x / meshChunkSize.x);
            float chunkCountZ = Mathf.FloorToInt(bounds.size.z / meshChunkSize.y);

            return new Vector2Int((int)chunkCountX, (int)chunkCountZ);
        }


        public HexagonCellPrototype Find_FirstWorldspaceCellWithTerrainChunkLookup(Vector2 chunkLookup, List<HexagonCellPrototype> activeWorldspaceCells)
        {
            return activeWorldspaceCells.Find(c => c.HasTerrainChunkLookup(chunkLookup));
        }

        public bool Evaluate_WorldAreaFolder_ByLookup(Vector2 chunkLookup)
        {
            if (_terrainChunkData_ByLookup.ContainsKey(chunkLookup) == false)
            {
                Debug.LogError("chunkLookup: " + chunkLookup + " does NOT exist in _terrainChunkData_ByLookup");
                return false;
            }
            Vector2 areaLookup = _terrainChunkData_ByLookup[chunkLookup].worldAreaLookup;
            Evaluate_WorldAreaFolder(areaLookup);

            return true;
        }

        public void Evaluate_WorldAreaFolder(Vector2 areaLookup)
        {
            WorldAreaObjectData data = GetWorldAreaObjectData(areaLookup);
            if (data == null)
            {
                data = new WorldAreaObjectData(areaLookup, WorldParentFolder());
                AddNew_WorldAreaFolder(data);
            }
            else data.Evalaute_Folders(WorldParentFolder());
        }

        private void AddNew_WorldAreaFolder(WorldAreaObjectData worldAreaObjectData)
        {
            if (_worldAreaObjectData == null) _worldAreaObjectData = new List<WorldAreaObjectData>();

            Vector2 areaLookup = worldAreaObjectData.worldAreaLookup;
            if (_worldAreaObjectData.Count == 0)
            {
                _worldAreaObjectData.Add(worldAreaObjectData);
            }
            else
            {
                int nullIndex = _worldAreaObjectData.FindIndex(d => d == null);
                if (nullIndex > -1)
                {
                    _worldAreaObjectData[nullIndex] = worldAreaObjectData;
                }
                else _worldAreaObjectData.Add(worldAreaObjectData);
            }
        }

        public void RefreshTerrainMeshes_V2(
            List<(Vector2[,], Vector2)> gridChunksWithCenterPos,
            Dictionary<Vector2, TerrainVertex> globalTerrainVertexGrid,
            Transform transform,
            Bounds activeGridBounds,
            List<HexagonCellPrototype> activeWorldspaceCells
        )
        {
            Dictionary<Vector2, GameObject> new_worldspaceTerrainChunks_ByLookup = new Dictionary<Vector2, GameObject>();

            for (int i = 0; i < gridChunksWithCenterPos.Count; i++)
            {
                Vector2 chunkCenterLookup = TerrainChunkData.CalculateTerrainChunkLookup(gridChunksWithCenterPos[i].Item2);

                if (Evaluate_WorldAreaFolder_ByLookup(chunkCenterLookup) == false)
                {
                    // Debug.LogError("No worldspaceCell found for chunkCenterLookup: " + chunkCenterLookup);
                    continue;
                }
                // HexagonCellPrototype worldspaceCell = Find_FirstWorldspaceCellWithTerrainChunkLookup(chunkCenterLookup, activeWorldspaceCells);
                // if (worldspaceCell == null)
                // {
                //     // Debug.LogError("No worldspaceCell found for chunkCenterLookup: " + chunkCenterLookup);
                //     continue;
                // }

                // Extract Grid Data
                Vector2[,] gridChunkKeys = gridChunksWithCenterPos[i].Item1;

                (Vector3[] vertexPositions, Vector2[] uvs, HashSet<Vector2> meshTraingleExcludeList) = TerrainVertexUtil.ExtractVertexWorldPositionsAndUVs_V2(
                    gridChunkKeys,
                    globalTerrainVertexGrid,
                    transform
                );

                GameObject meshChunkObject = Get_TerrainChunkObjectByIndexData_V2(chunkCenterLookup);
                if (meshChunkObject == null)
                {
                    Debug.LogError("meshChunkObject is null.  chunkCenterLookup: " + chunkCenterLookup);
                    continue;
                }

                // Get the MeshFilter component from the instantiatedPrefab
                MeshFilter meshFilter = meshChunkObject.GetComponent<MeshFilter>();
                MeshCollider meshCollider = meshChunkObject.GetComponent<MeshCollider>();
                Mesh finalMesh = meshFilter.sharedMesh;

                finalMesh.name = "World Mesh";
                finalMesh.Clear();
                finalMesh.vertices = vertexPositions;
                finalMesh.triangles = GenerateTerrainTriangles_V2(gridChunkKeys, meshTraingleExcludeList);
                finalMesh.uv = uvs;

                // Refresh Terrain Mesh
                finalMesh.RecalculateNormals();
                finalMesh.RecalculateBounds();

                // Apply the mesh data to the MeshFilter component
                meshFilter.sharedMesh = finalMesh;
                meshCollider.sharedMesh = finalMesh;

                if (new_worldspaceTerrainChunks_ByLookup.ContainsKey(chunkCenterLookup) == false)
                {
                    new_worldspaceTerrainChunks_ByLookup.Add(chunkCenterLookup, meshChunkObject);
                    meshChunkObject.gameObject.name = "Terrain Chunk_" + chunkCenterLookup;
                    if (enable_WorldPositionTracking) meshChunkObject.SetActive(false);
                }
            }

            _worldspaceTerrainChunks_ByLookup = new_worldspaceTerrainChunks_ByLookup;
        }


        public static List<Vector2[,]> GenerateVertexGridChunks(Vector2[,] vertexGrid, int chunks)
        {
            int gridSizeX = vertexGrid.GetLength(0);
            int gridSizeZ = vertexGrid.GetLength(1);

            int chunkSizeX = Mathf.CeilToInt((float)gridSizeX / chunks);
            int chunkSizeZ = Mathf.CeilToInt((float)gridSizeZ / chunks);

            List<Vector2[,]> chunksList = new List<Vector2[,]>();

            for (int i = 0; i < chunks; i++)
            {
                for (int j = 0; j < chunks; j++)
                {
                    Vector2[,] chunk = new Vector2[chunkSizeX, chunkSizeZ];

                    for (int x = 0; x < chunkSizeX; x++)
                    {
                        for (int z = 0; z < chunkSizeZ; z++)
                        {
                            int indexX = (i * (chunkSizeX - 1)) + x;
                            int indexZ = (j * (chunkSizeZ - 1)) + z;

                            if (indexX >= gridSizeX || indexZ >= gridSizeZ)
                            {
                                chunk[x, z] = vertexGrid[gridSizeX - 1, gridSizeZ - 1];
                            }
                            else
                            {
                                chunk[x, z] = vertexGrid[indexX, indexZ];
                            }
                        }
                    }

                    chunksList.Add(chunk);
                }
            }

            return chunksList;
        }


        public static List<(Vector2[,], Vector2)> GenerateVertexGridChunks_V2(Vector2[,] vertexGrid, Vector2Int chunksXZ, Bounds bounds)
        {
            // Calculate divide for center pos
            float chunkDivideSizeX = bounds.size.x / chunksXZ.x;
            float chunkDivideSizeZ = bounds.size.z / chunksXZ.y;
            Vector3 startPoint = bounds.min + new Vector3(chunkDivideSizeX / 2f, 0f, chunkDivideSizeZ / 2f);

            // Calculate divide for vertexGrid points
            int gridSizeX = vertexGrid.GetLength(0);
            int gridSizeZ = vertexGrid.GetLength(1);
            int overlapX = 1;
            int overlapZ = 1;

            int chunkSizeX = Mathf.CeilToInt(((float)(gridSizeX + 1) - overlapX) / chunksXZ.x);
            int chunkSizeZ = Mathf.CeilToInt(((float)(gridSizeZ + 1) - overlapZ) / chunksXZ.y);

            List<(Vector2[,], Vector2)> gridChunksWithCenterPos = new List<(Vector2[,], Vector2)>();

            for (int i = 0; i < chunksXZ.x; i++)
            {
                for (int j = 0; j < chunksXZ.y; j++)
                {
                    int startX = i * chunkSizeX - i * overlapX;
                    int startZ = j * chunkSizeZ - j * overlapZ;
                    int endX = Mathf.Min(startX + chunkSizeX, gridSizeX);
                    int endZ = Mathf.Min(startZ + chunkSizeZ, gridSizeZ);

                    Vector2[,] chunk = new Vector2[endX - startX, endZ - startZ];

                    for (int x = 0; x < chunk.GetLength(0); x++)
                    {
                        for (int z = 0; z < chunk.GetLength(1); z++)
                        {
                            int indexX = startX + x;
                            int indexZ = startZ + z;

                            if (indexX >= gridSizeX || indexZ >= gridSizeZ)
                            {
                                chunk[x, z] = vertexGrid[gridSizeX - 1, gridSizeZ - 1];
                            }
                            else
                            {
                                chunk[x, z] = vertexGrid[indexX, indexZ];
                            }
                        }
                    }

                    Vector3 center = startPoint + new Vector3(chunkDivideSizeX * i, 0f, chunkDivideSizeZ * j);

                    gridChunksWithCenterPos.Add((chunk, new Vector2(center.x, center.z)));
                }
            }
            return gridChunksWithCenterPos;
        }

        public static List<Vector2[,]> GenerateVertexGridChunks(Vector2[,] vertexGrid, int chunksX, int chunksZ)
        {
            int gridSizeX = vertexGrid.GetLength(0);
            int gridSizeZ = vertexGrid.GetLength(1);

            int overlapX = 1;
            int overlapZ = 1;

            int chunkSizeX = Mathf.CeilToInt(((float)(gridSizeX + 1) - overlapX) / chunksX);
            int chunkSizeZ = Mathf.CeilToInt(((float)(gridSizeZ + 1) - overlapZ) / chunksZ);

            List<Vector2[,]> chunksList = new List<Vector2[,]>();

            for (int i = 0; i < chunksX; i++)
            {
                for (int j = 0; j < chunksZ; j++)
                {
                    int startX = i * chunkSizeX - i * overlapX;
                    int startZ = j * chunkSizeZ - j * overlapZ;
                    int endX = Mathf.Min(startX + chunkSizeX, gridSizeX);
                    int endZ = Mathf.Min(startZ + chunkSizeZ, gridSizeZ);

                    Vector2[,] chunk = new Vector2[endX - startX, endZ - startZ];

                    for (int x = 0; x < chunk.GetLength(0); x++)
                    {
                        for (int z = 0; z < chunk.GetLength(1); z++)
                        {
                            int indexX = startX + x;
                            int indexZ = startZ + z;

                            if (indexX >= gridSizeX || indexZ >= gridSizeZ)
                            {
                                chunk[x, z] = vertexGrid[gridSizeX - 1, gridSizeZ - 1];
                            }
                            else
                            {
                                chunk[x, z] = vertexGrid[indexX, indexZ];
                            }
                        }
                    }

                    chunksList.Add(chunk);
                }
            }
            return chunksList;
        }

        // public static List<Vector2[,]> GenerateVertexGridChunks(Vector2[,] vertexGrid, int chunksX, int chunksZ)
        // {
        //     int gridSizeX = vertexGrid.GetLength(0);
        //     int gridSizeZ = vertexGrid.GetLength(1);

        //     int overlapX = 1;
        //     int overlapZ = 1;

        //     int chunkSizeX = Mathf.CeilToInt((float)(gridSizeX - overlapX) / chunksX);
        //     int chunkSizeZ = Mathf.CeilToInt((float)(gridSizeZ - overlapZ) / chunksZ);

        //     List<Vector2[,]> chunksList = new List<Vector2[,]>();

        //     for (int i = 0; i < chunksX; i++)
        //     {
        //         for (int j = 0; j < chunksZ; j++)
        //         {
        //             int startX = i * chunkSizeX - i * overlapX;
        //             int startZ = j * chunkSizeZ - j * overlapZ;
        //             int endX = Mathf.Min(startX + chunkSizeX, gridSizeX);
        //             int endZ = Mathf.Min(startZ + chunkSizeZ, gridSizeZ);

        //             Vector2[,] chunk = new Vector2[endX - startX, endZ - startZ];

        //             for (int x = 0; x < chunk.GetLength(0); x++)
        //             {
        //                 for (int z = 0; z < chunk.GetLength(1); z++)
        //                 {
        //                     int indexX = startX + x;
        //                     int indexZ = startZ + z;

        //                     if (indexX >= gridSizeX || indexZ >= gridSizeZ)
        //                     {
        //                         chunk[x, z] = vertexGrid[gridSizeX - 1, gridSizeZ - 1];
        //                     }
        //                     else
        //                     {
        //                         chunk[x, z] = vertexGrid[indexX, indexZ];
        //                     }
        //                 }
        //             }

        //             chunksList.Add(chunk);
        //         }
        //     }

        //     return chunksList;
        // }


        // public static List<Vector2[,]> GenerateVertexGridChunks(Vector2[,] vertexGrid, int chunksX, int chunksZ)
        // {
        //     int gridSizeX = vertexGrid.GetLength(0);
        //     int gridSizeZ = vertexGrid.GetLength(1);

        //     int chunkSizeX = Mathf.CeilToInt((float)gridSizeX / chunksX);
        //     int chunkSizeZ = Mathf.CeilToInt((float)gridSizeZ / chunksZ);

        //     List<Vector2[,]> chunksList = new List<Vector2[,]>();

        //     for (int i = 0; i < chunksX; i++)
        //     {
        //         for (int j = 0; j < chunksZ; j++)
        //         {
        //             int startX = i * chunkSizeX;
        //             int startZ = j * chunkSizeZ;
        //             int endX = Mathf.Min(startX + chunkSizeX, gridSizeX);
        //             int endZ = Mathf.Min(startZ + chunkSizeZ, gridSizeZ);

        //             Vector2[,] chunk = new Vector2[endX - startX, endZ - startZ];

        //             for (int x = 0; x < chunk.GetLength(0); x++)
        //             {
        //                 for (int z = 0; z < chunk.GetLength(1); z++)
        //                 {
        //                     int indexX = startX + x;
        //                     int indexZ = startZ + z;

        //                     if (indexX >= gridSizeX || indexZ >= gridSizeZ)
        //                     {
        //                         chunk[x, z] = vertexGrid[gridSizeX - 1, gridSizeZ - 1];
        //                     }
        //                     else
        //                     {
        //                         chunk[x, z] = vertexGrid[indexX, indexZ];
        //                     }
        //                 }
        //             }

        //             chunksList.Add(chunk);
        //         }
        //     }

        //     return chunksList;
        // }

        // public static List<Vector2[,]> GenerateVertexGridChunks(Vector2[,] vertexGrid, Vector2 chunkSizes)
        // {
        //     int gridSizeX = vertexGrid.GetLength(0);
        //     int gridSizeZ = vertexGrid.GetLength(1);

        //     int chunkSizeX = Mathf.FloorToInt(chunkSizes.x);
        //     int chunkSizeZ = Mathf.FloorToInt(chunkSizes.y);

        //     int chunksX = Mathf.CeilToInt((float)gridSizeX / (chunkSizeX - 1));
        //     int chunksZ = Mathf.CeilToInt((float)gridSizeZ / (chunkSizeZ - 1));

        //     List<Vector2[,]> chunksList = new List<Vector2[,]>();

        //     for (int i = 0; i < chunksX; i++)
        //     {
        //         for (int j = 0; j < chunksZ; j++)
        //         {
        //             int startX = i * (chunkSizeX - 1);
        //             int startZ = j * (chunkSizeZ - 1);
        //             int endX = Mathf.Min(startX + chunkSizeX, gridSizeX);
        //             int endZ = Mathf.Min(startZ + chunkSizeZ, gridSizeZ);

        //             Vector2[,] chunk = new Vector2[endX - startX, endZ - startZ];

        //             for (int x = 0; x < chunk.GetLength(0); x++)
        //             {
        //                 for (int z = 0; z < chunk.GetLength(1); z++)
        //                 {
        //                     int indexX = startX + x;
        //                     int indexZ = startZ + z;

        //                     if (indexX >= gridSizeX || indexZ >= gridSizeZ)
        //                     {
        //                         chunk[x, z] = vertexGrid[gridSizeX - 1, gridSizeZ - 1];
        //                     }
        //                     else
        //                     {
        //                         chunk[x, z] = vertexGrid[indexX, indexZ];
        //                     }
        //                 }
        //             }

        //             chunksList.Add(chunk);
        //         }
        //     }

        //     return chunksList;
        // }

        // public static List<Vector2[,]> GenerateVertexGridChunks(Vector2[,] vertexGrid, Vector2 chunkSizes)
        // {
        //     int gridSizeX = vertexGrid.GetLength(0);
        //     int gridSizeZ = vertexGrid.GetLength(1);

        //     int chunkSizeX = Mathf.CeilToInt(chunkSizes.x);
        //     int chunkSizeZ = Mathf.CeilToInt(chunkSizes.y);

        //     int chunksX = Mathf.CeilToInt((float)gridSizeX / (chunkSizeX - 1));
        //     int chunksZ = Mathf.CeilToInt((float)gridSizeZ / (chunkSizeZ - 1));

        //     List<Vector2[,]> chunksList = new List<Vector2[,]>();

        //     for (int i = 0; i < chunksX; i++)
        //     {
        //         for (int j = 0; j < chunksZ; j++)
        //         {
        //             int startX = i * (chunkSizeX - 1);
        //             int startZ = j * (chunkSizeZ - 1);
        //             int endX = Mathf.Min(startX + chunkSizeX, gridSizeX);
        //             int endZ = Mathf.Min(startZ + chunkSizeZ, gridSizeZ);

        //             Vector2[,] chunk = new Vector2[endX - startX, endZ - startZ];

        //             for (int x = 0; x < chunk.GetLength(0); x++)
        //             {
        //                 for (int z = 0; z < chunk.GetLength(1); z++)
        //                 {
        //                     int indexX = startX + x;
        //                     int indexZ = startZ + z;

        //                     if (indexX >= gridSizeX || indexZ >= gridSizeZ)
        //                     {
        //                         chunk[x, z] = vertexGrid[gridSizeX - 1, gridSizeZ - 1];
        //                     }
        //                     else
        //                     {
        //                         chunk[x, z] = vertexGrid[indexX, indexZ];
        //                     }
        //                 }
        //             }

        //             chunksList.Add(chunk);
        //         }
        //     }

        //     return chunksList;
        // }


        // public static List<Vector2[,]> GenerateVertexGridChunks(Vector2[,] vertexGrid, Vector2 chunkSizes)
        // {
        //     int gridSizeX = vertexGrid.GetLength(0);
        //     int gridSizeZ = vertexGrid.GetLength(1);

        //     int chunkSizeX = Mathf.CeilToInt(chunkSizes.x);
        //     int chunkSizeZ = Mathf.CeilToInt(chunkSizes.y);

        //     int chunksX = Mathf.CeilToInt((float)gridSizeX / chunkSizeX);
        //     int chunksZ = Mathf.CeilToInt((float)gridSizeZ / chunkSizeZ);

        //     List<Vector2[,]> chunksList = new List<Vector2[,]>();

        //     for (int i = 0; i < chunksX; i++)
        //     {
        //         for (int j = 0; j < chunksZ; j++)
        //         {
        //             int startX = i * chunkSizeX;
        //             int startZ = j * chunkSizeZ;
        //             int endX = Mathf.Min(startX + chunkSizeX, gridSizeX);
        //             int endZ = Mathf.Min(startZ + chunkSizeZ, gridSizeZ);

        //             Vector2[,] chunk = new Vector2[endX - startX, endZ - startZ];

        //             for (int x = 0; x < chunk.GetLength(0); x++)
        //             {
        //                 for (int z = 0; z < chunk.GetLength(1); z++)
        //                 {
        //                     int indexX = startX + x;
        //                     int indexZ = startZ + z;

        //                     if (indexX >= gridSizeX || indexZ >= gridSizeZ)
        //                     {
        //                         chunk[x, z] = vertexGrid[gridSizeX - 1, gridSizeZ - 1];
        //                     }
        //                     else
        //                     {
        //                         chunk[x, z] = vertexGrid[indexX, indexZ];
        //                     }
        //                 }
        //             }

        //             chunksList.Add(chunk);
        //         }
        //     }

        //     return chunksList;
        // }


        // public static List<Vector2[,]> GenerateVertexGridChunks(Vector2[,] vertexGrid, int chunks)
        // {
        //     int gridSizeX = vertexGrid.GetLength(0);
        //     int gridSizeY = vertexGrid.GetLength(1);

        //     int chunkSizeX = gridSizeX / chunks;
        //     int chunkSizeY = gridSizeY / chunks;

        //     List<Vector2[,]> chunksList = new List<Vector2[,]>();

        //     for (int i = 0; i < chunks; i++)
        //     {
        //         for (int j = 0; j < chunks; j++)
        //         {
        //             Vector2[,] chunk = new Vector2[chunkSizeX, chunkSizeY];

        //             for (int x = 0; x < chunkSizeX; x++)
        //             {
        //                 for (int y = 0; y < chunkSizeY; y++)
        //                 {
        //                     chunk[x, y] = vertexGrid[(i * chunkSizeX) + x, (j * chunkSizeY) + y];
        //                 }
        //             }

        //             chunksList.Add(chunk);
        //         }
        //     }

        //     return chunksList;
        // }


        // public static List<Vector2[,]> GenerateVertexGridChunks(Vector2[,] globalVertexGrid, int chunks)
        // {
        //     // Determine the number of vertices in each direction
        //     int numVerticesX = globalVertexGrid.GetLength(0);
        //     int numVerticesZ = globalVertexGrid.GetLength(1);

        //     // Determine the number of vertices in each chunk in each direction
        //     int chunkSizeX = Mathf.CeilToInt((float)numVerticesX / chunks);
        //     int chunkSizeZ = Mathf.CeilToInt((float)numVerticesZ / chunks);

        //     // Create a list to hold the mesh chunks
        //     List<Vector2[,]> meshChunks = new List<Vector2[,]>();

        //     // Iterate through the chunks and create a mesh for each
        //     for (int chunkX = 0; chunkX < chunks; chunkX++)
        //     {
        //         for (int chunkZ = 0; chunkZ < chunks; chunkZ++)
        //         {
        //             // Determine the starting and ending indices for the current chunk
        //             int startX = chunkX * chunkSizeX;
        //             int startZ = chunkZ * chunkSizeZ;
        //             int endX = Mathf.Min(startX + chunkSizeX, numVerticesX);
        //             int endZ = Mathf.Min(startZ + chunkSizeZ, numVerticesZ);

        //             // Create an array to hold the vertices for the current chunk
        //             Vector2[,] chunkVertices = new Vector2[endX - startX, endZ - startZ];

        //             // Copy the relevant vertices from the original grid to the chunk
        //             for (int x = startX; x < endX; x++)
        //             {
        //                 for (int z = startZ; z < endZ; z++)
        //                 {
        //                     chunkVertices[x - startX, z - startZ] = new Vector3(globalVertexGrid[x, z].x, 0, globalVertexGrid[x, z].y);
        //                 }
        //             }

        //             // Add the chunk to the list
        //             meshChunks.Add(chunkVertices);
        //         }
        //     }

        //     // Return the list of mesh chunks
        //     return meshChunks;
        // }


        private void UpdateTerrainTexture(
            GameObject worldSpaceObject,
            TerrainVertex[,] vertexGrid
        )
        {
            MapDisplay mapDisplay = worldSpaceObject.GetComponent<MapDisplay>();
            if (mapDisplay == null)
            {
                Debug.LogError("Missing Map Display");
                return;
            }

            MapData mapData = GenerateMapData(vertexGrid, terrainTypes, pathTypes);
            Texture2D texture2D = TextureGenerator.TextureFromColourMap(mapData.colourMap,
                vertexGrid.GetLength(0),
                vertexGrid.GetLength(1));

            // (Texture2D texture2D, Color[] colorMap) = TextureGenerator.TextureFromMaterialMap(mapData.materialMap, vertexGrid.GetLength(0), vertexGrid.GetLength(1), mesh, meshRenderer);
            // Texture2D texture2D = TextureGenerator.CombineMaterials(mapData.materialMap, vertexGrid.GetLength(0), vertexGrid.GetLength(1));
            mapDisplay.DrawTexture(texture2D);
        }

        private void UpdateTerrainTexture_V2(GameObject worldSpaceObject)
        {
            MapDisplay mapDisplay = worldSpaceObject.GetComponent<MapDisplay>();
            if (mapDisplay == null)
            {
                Debug.LogError("Missing Map Display");
                return;
            }

            MapData mapData = GenerateMapData(
                _globalTerrainVertexGridCoordinates,
                _globalTerrainVertexGridByCoordinate,
                transform,
                terrainTypes,
                pathTypes
            );

            Texture2D texture2D = TextureGenerator.TextureFromColourMap(mapData.colourMap,
                _globalTerrainVertexGridCoordinates.GetLength(0),
                _globalTerrainVertexGridCoordinates.GetLength(1));

            mapDisplay.DrawTexture(texture2D);
        }


        public static MapData GenerateMapData(
            TerrainVertex[,] vertexGrid,
            TerrainType[] terrainTypes,
            TerrainType[] pathTypes
        )
        {
            int gridSizeX = vertexGrid.GetLength(0);
            int gridSizeZ = vertexGrid.GetLength(1);

            Color[] colourMap = new Color[gridSizeX * gridSizeZ];
            float[,] heightMap = new float[gridSizeX, gridSizeZ];

            float heighest = float.MinValue;

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    float currentHeight = vertexGrid[x, z].position.y;
                    bool isPath = vertexGrid[x, z].type == VertexType.Road;
                    bool isCellCenterPoint = vertexGrid[x, z].isCellCenterPoint;

                    if (heighest < currentHeight) heighest = currentHeight;

                    heightMap[x, z] = currentHeight;

                    if (isPath)
                    {
                        if (pathTypes != null)
                        {
                            colourMap[x * gridSizeX + z] = isCellCenterPoint ? pathTypes[1].colour : pathTypes[0].colour;
                        }
                        else
                        {
                            colourMap[x * gridSizeX + z] = terrainTypes[0].colour;
                        }
                        continue;
                    }

                    for (int i = 0; i < terrainTypes.Length; i++)
                    {
                        if (currentHeight <= terrainTypes[i].height)
                        {
                            // Debug.Log("GenerateMapData, currentHeight: " + currentHeight + ", terrainType: " + terrainTypes[i].name);
                            colourMap[x * gridSizeX + z] = terrainTypes[i].colour;
                            // break;
                        }
                    }
                }
            }
            // Debug.Log("GenerateMapData, heighest: " + heighest);
            return new MapData(heightMap, colourMap);
        }


        public static MapData GenerateMapData(
            Vector2[,] worldspaceVertexKeys,
            Dictionary<Vector2, TerrainVertex> globalTerrainVertexGrid,
            Transform transform,
            TerrainType[] terrainTypes,
            TerrainType[] pathTypes
        )
        {
            HashSet<(int, int)> meshExcludeList = new HashSet<(int, int)>();
            int gridSizeX = worldspaceVertexKeys.GetLength(0);
            int gridSizeZ = worldspaceVertexKeys.GetLength(1);

            Color[] colourMap = new Color[gridSizeX * gridSizeZ];
            float[,] heightMap = new float[gridSizeX, gridSizeZ];

            float heighest = float.MinValue;

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    TerrainVertex currentVertex = globalTerrainVertexGrid[worldspaceVertexKeys[x, z]];

                    float currentHeight = currentVertex.position.y;
                    bool isPath = currentVertex.type == VertexType.Road;
                    bool isCellCenterPoint = currentVertex.isCellCenterPoint;

                    if (heighest < currentHeight) heighest = currentHeight;

                    heightMap[x, z] = currentHeight;

                    if (isPath)
                    {
                        if (pathTypes != null)
                        {
                            colourMap[x * gridSizeX + z] = isCellCenterPoint ? pathTypes[1].colour : pathTypes[0].colour;
                        }
                        else
                        {
                            colourMap[x * gridSizeX + z] = terrainTypes[0].colour;
                        }
                        continue;
                    }

                    for (int i = 0; i < terrainTypes.Length; i++)
                    {
                        if (currentHeight <= terrainTypes[i].height)
                        {
                            // Debug.Log("GenerateMapData, currentHeight: " + currentHeight + ", terrainType: " + terrainTypes[i].name);
                            colourMap[x * gridSizeX + z] = terrainTypes[i].colour;
                            // break;
                        }
                    }
                }
            }
            // Debug.Log("GenerateMapData, heighest: " + heighest);
            return new MapData(heightMap, colourMap);
        }


        public static (Dictionary<Vector2, TerrainVertex>, Dictionary<Vector2, List<Vector3>>) Generate_GlobalVertexGrid_WithNoise_V8(
            Bounds bounds,
            Transform transform,
            float steps,
            List<LayeredNoiseOption> layerdNoises_terrain,
            LayeredNoiseOption layeredNoise_regionPath,
            float locationNoise_pathNoiseMin,
            Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> subCellTerraformLookups_ByWorldspace,
            Dictionary<Vector2, Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>> cellLookup_ByLayer_BySize_ByWorldSpace,
            HexCellSizes subCellSize,
            float terrainHeight,
            int cellLayerElevation = 3,
            int treeStep = 4,
            int seaLevel = 0,
            int worldSpaceSize = 108
        )
        {
            // Calculate the minimum x and z positions that are divisible by steps
            float minX = Mathf.Floor(bounds.min.x / steps) * steps;
            float minZ = Mathf.Floor(bounds.min.z / steps) * steps;

            // Calculate the number of steps along the x and z axis based on the spacing
            int xSteps = Mathf.CeilToInt((bounds.max.x - minX) / steps);
            int zSteps = Mathf.CeilToInt((bounds.max.z - minZ) / steps);

            Dictionary<Vector2, TerrainVertex> gridPoints = new Dictionary<Vector2, TerrainVertex>();
            Dictionary<Vector2, List<Vector3>> treeSpawnPoints_byWorldspace = new Dictionary<Vector2, List<Vector3>>();

            Vector3 currentTrackPos = Vector3.zero;
            List<Vector3> closestWorldspaceCellCenters = new List<Vector3>();

            Vector2 closest_WorldspaceLookup = Vector2.positiveInfinity;
            Vector2 closest_WorldspaceTerraformLookup = Vector2.positiveInfinity;
            Vector3 closest_WorldspaceTerraformPos = Vector3.positiveInfinity;

            Vector2 closest_subCellTerraformLookup = Vector2.positiveInfinity;
            int checkingLayer = int.MaxValue;
            float noiseBias = 0.5f;

            HashSet<CellStatus> includeCellStatusList = new HashSet<CellStatus>() {
                CellStatus.FlatGround
            };

            // Loop through each vertex in the grid
            for (int z = 0; z <= zSteps; z++)
            {
                for (int x = 0; x <= xSteps; x++)
                {
                    // Calculate the x and z coordinates of the current grid point
                    float xPos = minX + x * steps;
                    float zPos = minZ + z * steps;

                    Vector3 position = new Vector3(xPos, 0, zPos);

                    // Evaluate closest worldspace
                    if (closestWorldspaceCellCenters.Count == 0 || VectorUtil.DistanceXZ(position, currentTrackPos) > worldSpaceSize * 0.4f)
                    {
                        currentTrackPos = position;
                        closestWorldspaceCellCenters = HexCoreUtil.Calculate_ClosestHexCenterPoints_X7(position, worldSpaceSize);

                        closest_WorldspaceLookup = HexCoreUtil.Calculate_CenterLookup(closestWorldspaceCellCenters[0], worldSpaceSize);
                        if (treeSpawnPoints_byWorldspace.ContainsKey(closest_WorldspaceLookup) == false) treeSpawnPoints_byWorldspace.Add(closest_WorldspaceLookup, new List<Vector3>());

                        if (closestWorldspaceCellCenters.Count > 0 && subCellTerraformLookups_ByWorldspace.Count > 0)
                        {
                            Vector2 nearestTerraformLookup = Vector2.positiveInfinity;
                            Vector3 nearestTerraformPoint = Vector2.positiveInfinity;
                            float nearestDistance = float.MaxValue;

                            for (int i = 0; i < closestWorldspaceCellCenters.Count; i++)
                            {
                                float dist = VectorUtil.DistanceXZ(position, closestWorldspaceCellCenters[i]);
                                if (dist < nearestDistance)
                                {
                                    Vector2 worldspaceLookup = HexCoreUtil.Calculate_CenterLookup(closestWorldspaceCellCenters[i], worldSpaceSize);
                                    if (subCellTerraformLookups_ByWorldspace.ContainsKey(worldspaceLookup))
                                    {
                                        nearestDistance = dist;
                                        nearestTerraformLookup = worldspaceLookup;
                                        nearestTerraformPoint = closestWorldspaceCellCenters[i];

                                        if (nearestDistance < worldSpaceSize * 0.5f) break;
                                    }
                                }
                            }
                            closest_WorldspaceTerraformLookup = nearestTerraformLookup;
                            closest_WorldspaceTerraformPos = nearestTerraformPoint;

                            //Find a layer to ckeck against
                            if (cellLookup_ByLayer_BySize_ByWorldSpace.ContainsKey(closest_WorldspaceTerraformLookup) && cellLookup_ByLayer_BySize_ByWorldSpace[closest_WorldspaceTerraformLookup].ContainsKey(12))
                            {
                                foreach (int layer in cellLookup_ByLayer_BySize_ByWorldSpace[closest_WorldspaceTerraformLookup][12].Keys)
                                {
                                    checkingLayer = layer;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            closest_WorldspaceTerraformLookup = Vector2.positiveInfinity;
                            checkingLayer = int.MaxValue;
                        }
                    }

                    Vector3 worldCoord = transform.TransformVector(position);
                    Vector3 rounded = VectorUtil.RoundVector3To1Decimal(position);
                    Vector2 aproximateCoord = new Vector2(rounded.x, rounded.z);
                    Vector2Int noiseCoordinate = new Vector2Int((int)worldCoord.x, (int)worldCoord.z);

                    float worldspaceBaseNoiseHeight = float.MaxValue;
                    float baseNoiseHeight = 0;
                    bool terraformed = false;
                    bool canPlaceTree = (closest_WorldspaceLookup != Vector2.positiveInfinity);
                    bool markedForRemoval = false;
                    bool updateBufferCell = false;

                    if (closest_WorldspaceTerraformLookup != Vector2.positiveInfinity)
                    {
                        float distance = VectorUtil.DistanceXZ(position, closest_WorldspaceTerraformPos);
                        if (distance < worldSpaceSize * 1.7f)
                        {
                            worldspaceBaseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)closest_WorldspaceTerraformPos.x, (int)closest_WorldspaceTerraformPos.z, terrainHeight, layerdNoises_terrain);

                            // Evaluate nearest subcell
                            Dictionary<int, List<Vector3>> closest_subCellCentersBySize = HexCoreUtil.Calculate_ClosestHexCenterPoints_X13(position, new int[] { 12, 4 });
                            (HexagonCellPrototype nearestGroundCell, float dist) = HexCoreUtil.GetCloseestCellLookupInDictionary_withDistance(
                                    position,
                                    closest_subCellCentersBySize,
                                    subCellTerraformLookups_ByWorldspace[closest_WorldspaceTerraformLookup],
                                    includeCellStatusList
                               );

                            if (nearestGroundCell != null)
                            {
                                float cellBaseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)nearestGroundCell.center.x, (int)nearestGroundCell.center.z, terrainHeight, layerdNoises_terrain);
                                // float cellNoiseHeight = (cellBaseNoiseHeight + transform.position.y);
                                float cellNoiseHeight = nearestGroundCell.center.y;

                                float pathNoiseValue = LayerdNoise.Calculate_NoiseForCoordinate((int)nearestGroundCell.center.x, (int)nearestGroundCell.center.z, layeredNoise_regionPath);
                                bool isPath = nearestGroundCell.IsPath();  // false; //hNoiseValue > locationNoise_pathNoiseMin);

                                if (dist < (nearestGroundCell.size * 1.3f) && !isPath)
                                {
                                    // baseNoiseHeight = UtilityHelpers.RoundHeightToNearestElevation(cellNoiseHeight, cellLayerElevation);
                                    baseNoiseHeight = cellNoiseHeight;

                                    terraformed = true;
                                    canPlaceTree = false;

                                    // Tunnel Entry Cell
                                    if (nearestGroundCell.isTunnelGroundEntry && dist < (nearestGroundCell.size * 1.01f))
                                    {
                                        // Debug.Log("TunnelGroundEntry - dist: " + dist);
                                        float maxEdgeDistance = 0.46f;
                                        (bool isInBounds, Vector3 edgePointIfAny, float edgeDistance) = HexagonCellPrototype.IsPointWithinEdgeBounds_WithEdgePoint(position, nearestGroundCell, maxEdgeDistance);
                                        bool IsOnEdge = edgePointIfAny != Vector3.zero && edgeDistance > -1 && edgeDistance < maxEdgeDistance;

                                        // Basement
                                        if (IsOnEdge)
                                        {
                                            position = edgePointIfAny;
                                        }
                                        else
                                        {
                                            if (dist < nearestGroundCell.size * 0.5f) markedForRemoval = true;

                                            if (edgeDistance > -1)
                                            {
                                                Vector3 closesCorner = VectorUtil.GetClosestPoint_XZ(nearestGroundCell.cornerPoints, position);
                                                position = closesCorner;
                                            }
                                        }
                                    }
                                    // vertexGrid[x, z].position.y = vertexGrid[x, z].tunnelCellRoofPosY;

                                    // if (checkingLayer != int.MaxValue && (z % 2 == 0 || x % 2 == 0))
                                    // {
                                    //     Vector2 nearestCellLookup = nearestGroundCell.GetLookup();
                                    //     int groundLayer = HexCoreUtil.Calculate_CurrentLayer(cellLayerElevation, (int)baseNoiseHeight);

                                    //     if (cellLookup_ByLayer_BySize_ByWorldSpace[closest_WorldspaceTerraformLookup][nearestGroundCell.size].ContainsKey(groundLayer) &&
                                    //         cellLookup_ByLayer_BySize_ByWorldSpace[closest_WorldspaceTerraformLookup][nearestGroundCell.size][groundLayer].ContainsKey(nearestCellLookup)
                                    //     )
                                    //     {
                                    //         // HexagonCellPrototype currentCell = cellLookup_ByLayer_BySize_ByWorldSpace[closest_WorldspaceTerraformLookup][12][groundLayer][nearestCellLookup];
                                    //         // CellStatus desiredStatus = HexCoreUtil.Calculate_CellStatusFromNoise(currentCell.center, baseNoiseHeight, cellLayerElevation, seaLevel);
                                    //         // Debug.LogError("desiredStatus: " + desiredStatus + ", baseNoiseHeight: " + baseNoiseHeight + ", targetLayer: " + groundLayer + ", cell y pos: " + currentCell.center.y);
                                    //         cellLookup_ByLayer_BySize_ByWorldSpace[closest_WorldspaceTerraformLookup][nearestGroundCell.size][groundLayer][nearestCellLookup].SetCellStatus(CellStatus.FlatGround);
                                    //     }
                                    // }
                                }
                                else
                                {
                                    baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate(noiseCoordinate.x, noiseCoordinate.y, terrainHeight, layerdNoises_terrain);

                                    if (dist < nearestGroundCell.size * 6.6f)
                                    {
                                        baseNoiseHeight += transform.position.y;

                                        if (isPath)
                                        {
                                            float distMult = (1.01f - Mathf.Clamp01(dist / nearestGroundCell.size));

                                            if (baseNoiseHeight < cellNoiseHeight) baseNoiseHeight += noiseBias;
                                            else if (baseNoiseHeight > cellNoiseHeight) baseNoiseHeight -= noiseBias;

                                            // if (dist < nearestGroundCell.size * 1.3f)
                                            // {
                                            //     float roundedValue = UtilityHelpers.RoundHeightToNearestElevation(baseNoiseHeight, cellLayerElevation);
                                            //     baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, roundedValue, 0.5f);
                                            // }
                                            // // if (dist < nearestGroundCell.size * 0.6f)
                                            // // {
                                            // //     baseNoiseHeight = UtilityHelpers.RoundToNearestStep(baseNoiseHeight, 0.9f);
                                            // // }
                                            // else baseNoiseHeight = UtilityHelpers.RoundToNearestStep(baseNoiseHeight, 0.6f);

                                            float roundedValue = UtilityHelpers.RoundHeightToNearestElevation(baseNoiseHeight, cellLayerElevation);
                                            baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, roundedValue, distMult);
                                            baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, cellNoiseHeight, distMult);
                                        }
                                        else
                                        {
                                            if (baseNoiseHeight < cellNoiseHeight) baseNoiseHeight += noiseBias;
                                            else if (baseNoiseHeight > cellNoiseHeight) baseNoiseHeight -= noiseBias;


                                            if (dist < (nearestGroundCell.size * 1.6f))
                                            {
                                                baseNoiseHeight = Mathf.Clamp(baseNoiseHeight, cellNoiseHeight - 0.9f, cellNoiseHeight + 0.9f);
                                                updateBufferCell = true;
                                            }
                                            else
                                            {
                                                baseNoiseHeight = UtilityHelpers.RoundToNearestStep(baseNoiseHeight, 0.45f);
                                            }

                                            // float diff = (baseNoiseHeight > cellNoiseHeight) ? (baseNoiseHeight - cellNoiseHeight) : (cellNoiseHeight - baseNoiseHeight);
                                            // if (diff > cellLayerElevation) {
                                            //     baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, cellNoiseHeight, 0.1f);

                                            // float distMult = Mathf.Clamp01(dist / (nearestGroundCell.size * 3.8f));
                                            float distMult = (1.01f - Mathf.Clamp01(dist / (nearestGroundCell.size * 3.3f)));
                                            // baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, cellNoiseHeight, distMult);

                                            // float roundedValue = UtilityHelpers.RoundHeightToNearestElevation(baseNoiseHeight, cellLayerElevation);  
                                            // baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, roundedValue, distMult);

                                            // float roundedValue = Mathf.Lerp(baseNoiseHeight, cellNoiseHeight, distMult);
                                            // baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, roundedValue, 0.2f);
                                            baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, cellNoiseHeight, distMult);
                                            baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, cellNoiseHeight, distMult);

                                            if (updateBufferCell)
                                            {
                                                Vector3 nearestCellCenter = HexCoreUtil.Calculate_ClosestHexCenter_V2(position, (int)nearestGroundCell.size);
                                                Vector2 nearestCellLookup = HexCoreUtil.Calculate_CenterLookup(nearestCellCenter, (int)nearestGroundCell.size);

                                                int currentLayer = HexCoreUtil.Calculate_CurrentLayer(cellLayerElevation, baseNoiseHeight);
                                                if (
                                                    cellLookup_ByLayer_BySize_ByWorldSpace[closest_WorldspaceTerraformLookup][nearestGroundCell.size].ContainsKey(currentLayer) &&
                                                    cellLookup_ByLayer_BySize_ByWorldSpace[closest_WorldspaceTerraformLookup][nearestGroundCell.size][currentLayer].ContainsKey(nearestCellLookup)
                                                )
                                                {
                                                    // cellLookup_ByLayer_BySize_ByWorldSpace[closest_WorldspaceTerraformLookup][nearestGroundCell.size][currentLayer][nearestCellLookup].SetCellStatus(CellStatus.GenericGround);

                                                    // int layerBelow = currentLayer - 3;
                                                    // if (
                                                    //     cellLookup_ByLayer_BySize_ByWorldSpace[closest_WorldspaceTerraformLookup][nearestGroundCell.size].ContainsKey(layerBelow) &&
                                                    //     cellLookup_ByLayer_BySize_ByWorldSpace[closest_WorldspaceTerraformLookup][nearestGroundCell.size][layerBelow].ContainsKey(nearestCellLookup)
                                                    // )
                                                    // {
                                                    //     cellLookup_ByLayer_BySize_ByWorldSpace[closest_WorldspaceTerraformLookup][nearestGroundCell.size][layerBelow][nearestCellLookup].SetCellStatus(CellStatus.UnderGround);
                                                    // }

                                                    // int layerAbove = currentLayer + 1;
                                                    // if (
                                                    //     cellLookup_ByLayer_BySize_ByWorldSpace[closest_WorldspaceTerraformLookup][nearestGroundCell.size].ContainsKey(layerAbove) &&
                                                    //     cellLookup_ByLayer_BySize_ByWorldSpace[closest_WorldspaceTerraformLookup][nearestGroundCell.size][layerAbove].ContainsKey(nearestCellLookup)
                                                    // )
                                                    // {
                                                    //     cellLookup_ByLayer_BySize_ByWorldSpace[closest_WorldspaceTerraformLookup][nearestGroundCell.size][layerAbove][nearestCellLookup].SetCellStatus(CellStatus.AboveGround);
                                                    // }

                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        baseNoiseHeight += transform.position.y;

                                        if (baseNoiseHeight < cellNoiseHeight) baseNoiseHeight += noiseBias;
                                        else if (baseNoiseHeight > cellNoiseHeight) baseNoiseHeight -= noiseBias;

                                        baseNoiseHeight = UtilityHelpers.RoundToNearestStep(baseNoiseHeight, 0.4f);
                                    }
                                }
                            }
                            else
                            {
                                if (worldspaceBaseNoiseHeight != float.MaxValue)
                                {
                                    baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate(noiseCoordinate.x, noiseCoordinate.y, terrainHeight, layerdNoises_terrain);

                                    if (baseNoiseHeight < worldspaceBaseNoiseHeight) baseNoiseHeight += 0.3f;
                                    else if (baseNoiseHeight > worldspaceBaseNoiseHeight) baseNoiseHeight -= 0.3f;

                                    baseNoiseHeight += transform.position.y;

                                    baseNoiseHeight = UtilityHelpers.RoundToNearestStep(baseNoiseHeight, 0.3f);
                                }
                            }
                            terraformed = true;
                        }
                    }

                    if (!terraformed)
                    {
                        baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate(noiseCoordinate.x, noiseCoordinate.y, terrainHeight, layerdNoises_terrain);
                        baseNoiseHeight += transform.position.y;
                    }

                    position.y = baseNoiseHeight;


                    if (canPlaceTree)
                    {
                        if ((z % treeStep == 0 || x % treeStep == 0) && UnityEngine.Random.Range(0, 100) < 30)
                        {
                            Vector3 treePos = position;
                            treePos.y -= 0.35f;
                            treeSpawnPoints_byWorldspace[closest_WorldspaceLookup].Add(treePos);
                        }
                    }

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
                }
            }
            return (gridPoints, treeSpawnPoints_byWorldspace);
        }

        public static (Dictionary<Vector2, TerrainVertex>, Vector2[,]) Generate_GlobalVertexGrid_WithNoise_V7(
            Bounds bounds,
            Transform transform,
            float steps,
            List<LayeredNoiseOption> layerdNoises_terrain,
            LayeredNoiseOption layeredNoise_regionPath,
            float locationNoise_pathNoiseMin,
            Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> subCellTerraformLookups_ByWorldspace,
            HexCellSizes subCellSize,
            float terrainHeight,
            float persistence,
            float octaves,
            float lacunarity,
            int cellLayerElevation = 3,

            int seaLevel = 0,
            int worldSpaceSize = 108
        )
        {
            // Calculate the minimum x and z positions that are divisible by steps
            float minX = Mathf.Floor(bounds.min.x / steps) * steps;
            float minZ = Mathf.Floor(bounds.min.z / steps) * steps;

            // Calculate the number of steps along the x and z axis based on the spacing
            int xSteps = Mathf.CeilToInt((bounds.max.x - minX) / steps);
            int zSteps = Mathf.CeilToInt((bounds.max.z - minZ) / steps);

            Dictionary<Vector2, TerrainVertex> gridPoints = new Dictionary<Vector2, TerrainVertex>();
            Vector2[,] grid = new Vector2[xSteps + 1, zSteps + 1];

            Vector3 currentTrackPos = Vector3.zero;
            List<Vector3> closestWorldspaceCellCenters = new List<Vector3>();

            Vector2 closest_WorldspaceTerraformLookup = Vector2.positiveInfinity;
            Vector3 closest_WorldspaceTerraformPos = Vector3.positiveInfinity;

            Vector2 closest_subCellTerraformLookup = Vector2.positiveInfinity;
            // HexagonCellPrototype closest_subCellTerraform = null;

            // Loop through each vertex in the grid
            for (int z = 0; z <= zSteps; z++)
            {
                for (int x = 0; x <= xSteps; x++)
                {
                    // Calculate the x and z coordinates of the current grid point
                    float xPos = minX + x * steps;
                    float zPos = minZ + z * steps;

                    Vector3 position = new Vector3(xPos, 0, zPos);

                    if (closestWorldspaceCellCenters.Count == 0 || VectorUtil.DistanceXZ(position, currentTrackPos) > worldSpaceSize * 0.5f)
                    {
                        closestWorldspaceCellCenters = HexCoreUtil.Calculate_ClosestHexCenterPoints_X7(position, worldSpaceSize);
                        currentTrackPos = position;

                        if (closestWorldspaceCellCenters.Count > 0 && subCellTerraformLookups_ByWorldspace.Count > 0)
                        {
                            Vector2 nearestTerraformLookup = Vector2.positiveInfinity;
                            Vector3 nearestTerraformPoint = Vector2.positiveInfinity;
                            float nearestDistance = float.MaxValue;

                            for (int i = 0; i < closestWorldspaceCellCenters.Count; i++)
                            {
                                float dist = VectorUtil.DistanceXZ(position, closestWorldspaceCellCenters[i]);
                                if (dist < nearestDistance)
                                {
                                    Vector2 cellLookup = HexCoreUtil.Calculate_CenterLookup(closestWorldspaceCellCenters[i], worldSpaceSize);
                                    if (subCellTerraformLookups_ByWorldspace.ContainsKey(cellLookup))
                                    {
                                        nearestDistance = dist;
                                        nearestTerraformLookup = cellLookup;
                                        nearestTerraformPoint = closestWorldspaceCellCenters[i];

                                        if (nearestDistance < worldSpaceSize * 0.5f) break;
                                    }
                                }
                            }
                            closest_WorldspaceTerraformLookup = nearestTerraformLookup;
                            closest_WorldspaceTerraformPos = nearestTerraformPoint;
                        }
                        else closest_WorldspaceTerraformLookup = Vector2.positiveInfinity;
                    }

                    Vector3 worldCoord = transform.TransformVector(position);
                    Vector3 rounded = VectorUtil.RoundVector3To1Decimal(position);
                    Vector2 aproximateCoord = new Vector2(rounded.x, rounded.z);
                    Vector2Int noiseCoordinate = new Vector2Int((int)worldCoord.x, (int)worldCoord.z);


                    float worldspaceBaseNoiseHeight = float.MaxValue;
                    float baseNoiseHeight = 0;
                    bool terraformed = false;

                    if (closest_WorldspaceTerraformLookup != Vector2.positiveInfinity)
                    {

                        float distance = VectorUtil.DistanceXZ(position, closest_WorldspaceTerraformPos);
                        if (distance < worldSpaceSize * 1.7f)
                        {
                            worldspaceBaseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)closest_WorldspaceTerraformPos.x, (int)closest_WorldspaceTerraformPos.z, terrainHeight, layerdNoises_terrain);

                            Dictionary<int, List<Vector3>> closest_subCellCentersBySize = HexCoreUtil.Calculate_ClosestHexCenterPoints_X13(position, new int[] { 12 });
                            (HexagonCellPrototype neartestCell, float dist) = HexCoreUtil.GetCloseestCellLookupInDictionary_withDistance(
                                   position,
                                   closest_subCellCentersBySize,
                                   subCellTerraformLookups_ByWorldspace[closest_WorldspaceTerraformLookup]
                               );

                            // List<Vector3> closest_subCellCenters = HexCoreUtil.Calculate_ClosestHexCenterPoints_X13(position, (int)subCellSize);
                            // (HexagonCellPrototype neartestCell, float dist) = HexCoreUtil.GetCloseestCellLookupInDictionary_withDistance(
                            //        position,
                            //        closest_subCellCenters,
                            //        subCellTerraformLookups_ByWorldspace[closest_WorldspaceTerraformLookup],
                            //        (int)subCellSize
                            //    );

                            if (neartestCell != null)
                            {
                                float cellBaseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)neartestCell.center.x, (int)neartestCell.center.z, terrainHeight, layerdNoises_terrain);
                                float cellNoiseHeight = neartestCell.center.y;

                                // float cellBaseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)neartestCell.center.x, (int)neartestCell.center.z, terrainHeight, layerdNoises_terrain);
                                // float cellNoiseHeight = (cellBaseNoiseHeight + transform.position.y);

                                float pathNoiseValue = LayerdNoise.Calculate_NoiseForCoordinate((int)neartestCell.center.x, (int)neartestCell.center.z, layeredNoise_regionPath);
                                bool isPath = (pathNoiseValue > locationNoise_pathNoiseMin);

                                if (dist < neartestCell.size && !isPath)
                                {
                                    // baseNoiseHeight = UtilityHelpers.RoundHeightToNearestElevation(cellNoiseHeight, cellLayerElevation);
                                    baseNoiseHeight = cellNoiseHeight;

                                    terraformed = true;
                                }
                                else
                                {
                                    baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate(noiseCoordinate.x, noiseCoordinate.y, terrainHeight, layerdNoises_terrain);

                                    if (dist < neartestCell.size * 6f)
                                    {
                                        baseNoiseHeight += transform.position.y;
                                        baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, cellNoiseHeight, 0.09f);

                                        // float roundedValue = UtilityHelpers.RoundHeightToNearestElevation(cellNoiseHeight, cellLayerElevation);
                                        // baseNoiseHeight += transform.position.y;
                                        // baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, roundedValue, 0.09f);
                                    }
                                    else
                                    {
                                        if (baseNoiseHeight < cellBaseNoiseHeight)
                                        {
                                            baseNoiseHeight += 0.2f;
                                        }
                                        else if (baseNoiseHeight > cellBaseNoiseHeight) baseNoiseHeight -= 0.2f;

                                        baseNoiseHeight += transform.position.y;

                                        baseNoiseHeight = UtilityHelpers.RoundToNearestStep(baseNoiseHeight, 0.4f);
                                    }
                                }
                            }
                            else
                            {
                                if (worldspaceBaseNoiseHeight != float.MaxValue)
                                {
                                    baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate(noiseCoordinate.x, noiseCoordinate.y, terrainHeight, layerdNoises_terrain);

                                    if (baseNoiseHeight < worldspaceBaseNoiseHeight)
                                    {
                                        baseNoiseHeight += 0.2f;
                                    }
                                    else if (baseNoiseHeight > worldspaceBaseNoiseHeight) baseNoiseHeight -= 0.2f;

                                    baseNoiseHeight += transform.position.y;

                                    baseNoiseHeight = UtilityHelpers.RoundToNearestStep(baseNoiseHeight, 0.3f);
                                }
                            }
                            terraformed = true;
                        }
                    }

                    if (!terraformed)
                    {
                        baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate(noiseCoordinate.x, noiseCoordinate.y, terrainHeight, layerdNoises_terrain);
                        // baseNoiseHeight = CalculateNoiseHeightForVertex(noiseCoordinate.x, noiseCoordinate.y, terrainHeight, noiseFunctions, persistence, octaves, lacunarity);
                        // float locationNoiseValue = GetNoiseHeightValue(noiseCoordinate.x, noiseCoordinate.y, locationSubNoise.fastNoise, locationNoise_persistence, 2, 2f);
                        // baseNoiseHeight = Mathf.Lerp(baseNoiseHeight, locationNoiseValue, 0.4f);
                        baseNoiseHeight += transform.position.y;
                    }

                    position.y = baseNoiseHeight;
                    // Create the TerrainVertex object
                    TerrainVertex vertex = new TerrainVertex()
                    {
                        noiseCoordinate = noiseCoordinate,
                        aproximateCoord = aproximateCoord,
                        position = position,
                        index_X = x,
                        index_Z = z,

                        type = VertexType.Generic,
                        markedForRemoval = false,
                        isInHexBounds = false,

                        worldspaceOwnerCoordinate = Vector2.positiveInfinity,
                        parallelWorldspaceOwnerCoordinate = Vector2.positiveInfinity,
                        parallelVertexIndex_X = -1,
                        parallelVertexIndex_Z = -1,
                    };

                    // Add the vertex to the grid
                    gridPoints[aproximateCoord] = vertex;
                    grid[x, z] = aproximateCoord;
                }
            }
            return (gridPoints, grid);
        }


        public void Save()
        {
            if (_worldSpaces_ByArea != null)
            {
                WorldSaveLoadUtil.SaveData_WorldCell_ByParentCell(_worldSpaces_ByArea, savedfilePath, filename_worldSpace);
            }
            if (_worldAreas_ByRegion != null)
            {
                WorldSaveLoadUtil.SaveData_WorldCell_ByParentCell(_worldAreas_ByRegion, savedfilePath, filename_worldArea);
            }
            if (_worldRegionsLookup != null)
            {
                WorldSaveLoadUtil.SaveData_WorldRegion(_worldRegionsLookup, savedfilePath, filename_worldRegion);
            }
            if (_terrainChunkData_ByLookup != null)
            {
                WorldSaveLoadUtil.SaveData_WorldAreaTerrainChunkData_ByLookup(_terrainChunkData_ByLookup, savedfilePath, filename_worldAreaTerrainChunkData_ByLookup);
            }
            // if (_worldAreaTerrainChunkIndex_ByLookup != null)
            // {
            //     WorldSaveLoadUtil.SaveData_WorldAreaTerrainChunkIndex_ByLookup(_worldAreaTerrainChunkIndex_ByLookup, savedfilePath, filename_worldAreaTerrainChunkIndex_ByLookup);
            // }
            // if (_worldAreaTerrainChunkArea_ByLookup != null)
            // {
            //     WorldSaveLoadUtil.SaveData_Vector2Dictionary(_worldAreaTerrainChunkArea_ByLookup, savedfilePath, filename_terrainChunkWorldArea_ByLookup);
            // }
            // if (cellLookup_ByLayer_BySize_ByWorldSpace != null)
            // {
            //     SaveData_CellGrid_ByParentWorldSpace_ByWorldArea(cellLookup_ByLayer_BySize_ByWorldSpace, savedfilePath, filename_celllookup_byLayer_bySize_ByWorldSpace);
            // }
        }

        // public void Save__TerrainChunkData()
        // {
        //     if (_terrainChunkVertexData_ByLookup != null)
        //     {
        //         WorldSaveLoadUtil.SaveData_TerrainChunkVertexData_ByLookup(_terrainChunkVertexData_ByLookup, savedfilePath_terrainChunks, filename_terrainChunkVertexData_ByLookup);
        //     }
        // }

        public void Load()
        {
            if (savedfilePath == null) return;

            if (filename_worldRegion != null && (_worldRegionsLookup == null || _worldRegionsLookup.Count == 0))
            {
                Dictionary<Vector2, HexagonCellPrototype> worldRegionData = WorldSaveLoadUtil.LoadData_CellDictionary(savedfilePath, filename_worldRegion, _worldRegionSize);
                if (worldRegionData != null)
                {
                    _worldRegionsLookup = worldRegionData;
                    // Debug.Log("_worldRegionsLookup: " + _worldRegionsLookup.Count);
                }
            }

            if (_worldRegionsLookup != null && filename_worldArea != null && (_worldAreas_ByRegion == null || _worldAreas_ByRegion.Count == 0))
            {
                Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> worldAreas_ByRegion = WorldSaveLoadUtil.LoadData_WorldCell_ByParentCell(savedfilePath, filename_worldArea, _worldAreaSize);
                if (worldAreas_ByRegion != null)
                {
                    _worldAreas_ByRegion = worldAreas_ByRegion;
                    Debug.Log("_worldAreas_ByRegion - regions: " + _worldAreas_ByRegion.Count);
                }
            }

            if (_worldAreas_ByRegion != null && filename_worldSpace != null && (_worldSpaces_ByArea == null || _worldSpaces_ByArea.Count == 0))
            {
                Dictionary<Vector2, Dictionary<Vector2, HexagonCellPrototype>> worldSpaces_ByArea = WorldSaveLoadUtil.LoadData_WorldCell_ByParentCell(savedfilePath, filename_worldSpace, _worldspaceSize);
                if (worldSpaces_ByArea != null)
                {
                    _worldSpaces_ByArea = worldSpaces_ByArea;
                    Debug.Log("_worldSpaces_ByArea - areas: " + _worldSpaces_ByArea.Count);
                }
            }

            if (filename_worldAreaTerrainChunkData_ByLookup != null && (_terrainChunkData_ByLookup == null || _terrainChunkData_ByLookup.Count == 0))
            {
                Dictionary<Vector2, TerrainChunkData> terrainChunkData_ByLookup = WorldSaveLoadUtil.LoadData_WorldAreaTerrainChunkData(savedfilePath, filename_worldAreaTerrainChunkData_ByLookup);
                if (terrainChunkData_ByLookup != null)
                {
                    _terrainChunkData_ByLookup = terrainChunkData_ByLookup;
                    Debug.Log("_terrainChunkData_ByLookup: " + _terrainChunkData_ByLookup.Count);
                }
            }
            // if (filename_worldAreaTerrainChunkIndex_ByLookup != null)
            // {
            //     Dictionary<Vector2, (Vector2, int)> worldAreaTerrainChunkIndex_ByLookup = WorldSaveLoadUtil.LoadData_WorldAreaTerrainChunkIndex_ByLookup(savedfilePath, filename_worldAreaTerrainChunkIndex_ByLookup);
            //     if (worldAreaTerrainChunkIndex_ByLookup != null)
            //     {
            //         _worldAreaTerrainChunkIndex_ByLookup = worldAreaTerrainChunkIndex_ByLookup;
            //         Debug.Log("_worldAreaTerrainChunkIndex_ByLookup: " + _worldAreaTerrainChunkIndex_ByLookup.Count);
            //     }
            // }

            // if (filename_terrainChunkWorldArea_ByLookup != null)
            // {
            //     Dictionary<Vector2, Vector2> worldAreaTerrainChunkArea_ByLookup = WorldSaveLoadUtil.LoadData_Vector2Dictionary(savedfilePath, filename_terrainChunkWorldArea_ByLookup);
            //     if (worldAreaTerrainChunkArea_ByLookup != null)
            //     {
            //         _worldAreaTerrainChunkArea_ByLookup = worldAreaTerrainChunkArea_ByLookup;
            //         Debug.Log("_worldAreaTerrainChunkArea_ByLookup: " + _worldAreaTerrainChunkArea_ByLookup.Count);
            //     }
            // }
        }

        [Header("Save / Load Settings")]
        [SerializeField] private string sceneName = null;
        [SerializeField] private string savedfilePath = "Assets/WFC/worlddata";
        [SerializeField] private string savedfilePath_terrainChunks = "Assets/WFC/worlddata/terrain_chunkdata";
        [SerializeField] private string filenameHead = "hexCenterPoints_BySize_ByCoordinate";
        [SerializeField] private string filename_celllookup_byLayer_bySize_ByWorldSpace = "cellgrid_data_by_worldspace";
        [SerializeField] private string filename_worldSpace = "world_space_data";
        [SerializeField] private string filename_worldArea = "world_area_data";
        [SerializeField] private string filename_worldRegion = "world_region_data";
        [SerializeField] private string filename_worldAreaTerrainChunkData_ByLookup = "data_worldAreaTerrainChunkData_ByLookup";
        // [SerializeField] private string filename_terrainChunkVertexData_ByLookup = "data_terrainChunkVertexData_ByLookup";
        // [SerializeField] private string filename_worldAreaTerrainChunkIndex_ByLookup = "data_worldAreaTerrainChunkIndex_ByLookup";
        // [SerializeField] private string filename_terrainChunkWorldArea_ByLookup = "data_terrainChunkWorldArea_ByLookup";
        // [SerializeField] private string filename_cellGrid_byWorldSpace_byArea = "worldspace_cell_grid_data_worldarea_uid_";

        [Header(" ")]
        [SerializeField] private bool save__default;
        // [SerializeField] private bool save__chunkData;


        public Dictionary<Vector2, Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>> LoadData_CellGrid_ByParentWorldSpace_ByWorldArea(string directoryPath, string fileName, int layerOffset)
        {
            if (WorldSaveLoadUtil.LoadFilePath(directoryPath, fileName, out string filePath) == false) return null;

            string json = File.ReadAllText(filePath);
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new Vector2Converter(), },
                Error = (sender, args) =>
                {
                    Debug.LogError(args.ErrorContext.Error.Message);
                    args.ErrorContext.Handled = true;
                }
            };

            Dictionary<Vector2, Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>> dictionary = null;
            try
            {
                // var result = JsonConvert.DeserializeObject<Dictionary<string(json, settings);
                var result = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<int, Dictionary<int, Dictionary<string, WorldCellData>>>>>(json, settings);
                // var result = JsonConvert.DeserializeObject<Dictionary<string, WorldCellData>>(json, settings);
                dictionary = new Dictionary<Vector2, Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>>();

                foreach (var item in result)
                {
                    string s_worldspaceLookup = item.Key;
                    var worldspaceLookup = Vector2.positiveInfinity;

                    if (new Vector2Converter().TryConvertFrom(item.Key, out worldspaceLookup))
                    {
                        if (dictionary.ContainsKey(worldspaceLookup) == false) dictionary.Add(worldspaceLookup, new Dictionary<int, Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>>());

                        // Debug.LogError("item.Key: " + item.Key + ", worldspaceLookup: " + worldspaceLookup);

                        foreach (int currentSize in result[s_worldspaceLookup].Keys)
                        {
                            if (dictionary[worldspaceLookup].ContainsKey(currentSize) == false) dictionary[worldspaceLookup].Add(currentSize, new Dictionary<int, Dictionary<Vector2, HexagonCellPrototype>>());

                            foreach (int currentLayer in result[s_worldspaceLookup][currentSize].Keys)
                            {

                                if (dictionary[worldspaceLookup][currentSize].ContainsKey(currentLayer) == false)
                                {
                                    dictionary[worldspaceLookup][currentSize].Add(currentLayer, new Dictionary<Vector2, HexagonCellPrototype>());
                                }

                                foreach (WorldCellData cellData in result[s_worldspaceLookup][currentSize][currentLayer].Values)
                                {
                                    Vector2 lookup = cellData.lookup.ToVector2();
                                    Vector3 center = cellData.center.ToVector3();
                                    HexagonCellPrototype cell = new HexagonCellPrototype(center, currentSize, null, layerOffset);
                                    cellData.PastToCell(cell);

                                    dictionary[worldspaceLookup][currentSize][currentLayer].Add(lookup, cell);
                                }
                            }
                        }
                    }
                }
                Debug.Log("Loaded, file: " + fileName);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error while deserializing JSON: {e.Message}");
            }
            return dictionary;
        }


        private void Instantiate_Trees(Vector2 areaLookup, Vector2 worldspaceLookup)
        {
            WorldAreaObjectData areaObjectData = GetWorldAreaObjectData(areaLookup);
            if (areaObjectData != null)
            {
                int objectIndex = _worldspaceData_ByArea[areaLookup][worldspaceLookup].objectIndex;

                WorldspaceObjectData worldspaceObjectData = areaObjectData.GetWorldspaceObjectData(objectIndex);

                if (worldspaceObjectData != null) Generate_Trees_MT(worldspaceObjectData.treeSpawnPoints, worldspaceObjectData.TreeFolder());
            }
        }

        private List<GameObject> Generate_Trees(List<Vector3> treeSpawnPoints, Transform parentFolder)
        {
            if (testTreePrefab == null)
            {
                Debug.LogError("No Tree Prefab!");
                return null;
            }
            if (treeSpawnPoints == null || treeSpawnPoints.Count == 0)
            {
                Debug.LogError("NO treeSpawnPoints!");
                return null;
            }

            List<GameObject> newTrees = new List<GameObject>();

            foreach (Vector3 point in treeSpawnPoints)
            {
                GameObject newTree = WorldArea.InstantiateObjectWithRandomRotation(testTreePrefab, point);
                newTree.transform.SetParent(parentFolder);
                newTrees.Add(newTree);
            }
            return newTrees;
        }


        private List<GameObject> Generate_Trees_MT(List<Vector3> treeSpawnPoints, Transform parentFolder)
        {
            if (testTreePrefab == null)
            {
                Debug.LogError("No Tree Prefab!");
                return null;
            }
            if (treeSpawnPoints == null || treeSpawnPoints.Count == 0)
            {
                Debug.LogError("NO treeSpawnPoints!");
                return null;
            }

            List<GameObject> newTrees = new List<GameObject>();

            // Generate trees in parallel using multiple threads
            Parallel.ForEach(treeSpawnPoints, point =>
            {
                // GameObject newTree = InstantiateObjectWithRandomRotation(testTreePrefab, point);
                GameObject newTree = WorldArea.InstantiateObjectWithRandomRotation(testTreePrefab, point);
                newTree.transform.SetParent(parentFolder);

                // Add the new tree to the list in a thread-safe manner
                lock (newTrees)
                {
                    newTrees.Add(newTree);
                }
            });

            return newTrees;
        }

        public static GameObject InstantiateObjectWithRandomRotation(GameObject objectToInstantiate, Vector3 position)
        {
            System.Random random = new System.Random();

            // Generate a random rotation along the y-axis
            Quaternion yRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);

            // Generate a random rotation within the specified range along the x and z axes
            Quaternion xzRotation = Quaternion.Euler(
                (float)random.NextDouble() * 36f - 18f,
                0f,
                (float)random.NextDouble() * 36f - 18f);

            // Combine the rotations
            Quaternion finalRotation = yRotation * xzRotation;

            // Instantiate the object with the random rotation and position
            GameObject instantiatedObject = Instantiate(objectToInstantiate, position, finalRotation);

            return instantiatedObject;
        }




        #region Location Methods - To Relocate

        public void Evaluate_LocationPrefabNoiseRanges()
        {
            locationNoise_ranges = UtilityHelpers.Evaluate_NoiseRangeChunks(locationNoise_RangeMin, locationNoise_RangeMax, locationMarkerPrefabs_Default.Count, locationNoise_weightMult);
        }

        public void Assign_LocationsToWorldArea(Vector2 areaLookup, bool enableLog = false)
        {
            List<Color> colors = UtilityHelpers.GenerateUniqueRandomColors(locationMarkerPrefabs_Default.Count);

            //temp
            _worldAreaObjectData = null;

            _locationPrefabs_ByWorldspace_ByArea = new Dictionary<Vector2, Dictionary<Vector2, LocationPrefab>>();
            _locations_ByWorldspace_ByArea = new Dictionary<Vector2, Dictionary<Vector2, LocationData>>();

            if (_locationPrefabs_ByWorldspace_ByArea.ContainsKey(areaLookup) == false) _locationPrefabs_ByWorldspace_ByArea.Add(areaLookup, new Dictionary<Vector2, LocationPrefab>());

            List<HexagonCellPrototype> assignedWorldspaceCells = new List<HexagonCellPrototype>();

            foreach (HexagonCellPrototype cell in _worldSpaces_ByArea[areaLookup].Values)
            {
                // float baseNoiseHeight = CalculateNoiseHeightForVertex((int)cell.center.x, (int)cell.center.z, _global_terrainHeightDefault, noise_regional, persistence, octaves, lacunarity);
                float baseNoiseHeight = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)cell.center.x, (int)cell.center.z, _global_terrainHeightDefault, layeredNoise_terrainGlobal);
                if (baseNoiseHeight < _globalSeaLevel) continue;

                baseNoiseHeight += transform.position.y;
                // float locationNoiseValue = GetNoiseHeightValue((int)cell.center.x, (int)cell.center.z, locationNoise.fastNoise, locationNoise_persistence, 2, locationNoise_lacunarity);
                // float locationNoiseValue = LayerdNoise.Calculate_NoiseHeightForCoordinate((int)cell.center.x, (int)cell.center.z, _global_terrainHeightDefault, layeredNoise_locationMarker);
                float locationNoiseValue = LayerdNoise.Calculate_NoiseForCoordinate((int)cell.center.x, (int)cell.center.z, layeredNoise_locationMarker);

                int locationIndex = UtilityHelpers.Find_IndexOfFirstRangeContainingValue(locationNoiseValue, locationNoise_ranges);
                if (locationIndex == -1)
                {
                    // float pathNoiseValue = LayerdNoise.Calculate_NoiseForCoordinate((int)cell.center.x, (int)cell.center.z, layeredNoise_regionPath);
                    // if (pathNoiseValue > locationNoise_pathNoiseMin) 
                    continue;
                }

                LocationPrefab locationMarkerPrefab = locationMarkerPrefabs_Default[locationIndex];

                if (UtilityHelpers.IsValueWithinRange(baseNoiseHeight, locationMarkerPrefab.GetSettings().elevationRangeMinMax) == false) continue;

                if (randomize_ColorAssignment) locationMarkerPrefab.color = colors[locationIndex];

                Vector2 cellLookup = cell.GetLookup();
                // Debug.Log("locationMarkerPrefab: " + locationMarkerPrefab.name + ", color: " + locationMarkerPrefab.color);

                if (_locationPrefabs_ByWorldspace_ByArea[areaLookup].ContainsKey(cellLookup) == false)
                {
                    _locationPrefabs_ByWorldspace_ByArea[areaLookup].Add(cellLookup, locationMarkerPrefab);
                }
                else _locationPrefabs_ByWorldspace_ByArea[areaLookup][cellLookup] = locationMarkerPrefab;

                assignedWorldspaceCells.Add(cell);
            }
            // Debug.Log("_locationPrefabs_ByWorldspace_ByArea: " + _locationPrefabs_ByWorldspace_ByArea[areaLookup].Count);

            Evaluate_WorldAreaFolder(areaLookup);

            GenerateWorldCells_SubCells(assignedWorldspaceCells, enableLog);


            if (instantiateLocations)
            {
                foreach (var wsCell in assignedWorldspaceCells)
                {
                    if (Instantiate_Location(areaLookup, wsCell.GetLookup()))
                    {
                        break; //Testing just 1 for now
                    }
                }
            }

        }


        private List<Vector2> PreassignWorldSpaces(List<HexagonCellPrototype> worldspaceCellsToPreasign)
        {
            if (locationMarkerPrefabs_ToPreAssign.Count == 0) return null;

            List<Vector2> preAssignWorldspaceCoords = new List<Vector2>();
            int desiredPreassignCount = 0;

            foreach (var locationPrefab in locationMarkerPrefabs_ToPreAssign)
            {
                desiredPreassignCount += UnityEngine.Random.Range(locationPrefab.GetSettings().worldSpacesMin, locationPrefab.GetSettings().worldSpacesMax);
            }

            if (desiredPreassignCount > 0)
            {
                foreach (HexagonCellPrototype worldspace in worldspaceCellsToPreasign)
                {
                    if (preAssignWorldspaceCoords.Count < desiredPreassignCount)
                    {
                        preAssignWorldspaceCoords.Add(worldspace.GetLookup());
                        _preassignedWorldspaceCells.Add(worldspace);
                    }
                }
            }
            Debug.Log("Desired worldspace Pre-assign count: " + desiredPreassignCount);
            return preAssignWorldspaceCoords;
        }


        public LocationData AddNew_LocationData(Vector2 areaLookup, Vector2 worldspaceLookup, HexagonCellCluster newCluster, LocationMarkerPrefabOption prefabSettings)
        {
            if (_locations_ByWorldspace_ByArea.ContainsKey(areaLookup) == false) _locations_ByWorldspace_ByArea.Add(areaLookup, new Dictionary<Vector2, LocationData>());

            LocationData newLocationData = LocationData.GenerateLocationDataFromCluster(newCluster, prefabSettings.locationType, prefabSettings);

            if (_locations_ByWorldspace_ByArea[areaLookup].ContainsKey(worldspaceLookup))
            {
                _locations_ByWorldspace_ByArea[areaLookup][worldspaceLookup] = newLocationData;
            }
            else _locations_ByWorldspace_ByArea[areaLookup].Add(worldspaceLookup, newLocationData);

            return newLocationData;
        }


        public void EvaluateLocationMarkers()
        {
            if (locationMarkerPrefabOptions == null || locationMarkerPrefabOptions.Count == 0)
            {
                Debug.LogError("locationMarkerPrefabOptions has no entries");
            }

            List<HexagonCellPrototype> allAvailableCells = World_GetAllViableCellsAndCenterPoints(_worldSpaceCellGridByCenterCoordinate);
            Debug.LogError("allAvailableCells: " + allAvailableCells.Count);

            _locationMarkerCells = AssignLocationMarkers(allAvailableCells, locationMarkerPrefabs_Default, locationMarkerPrefabs_ToPreAssign);

            int placedMarkerCount = _locationMarkerCells.Count;

            if (placedMarkerCount > 0)
            {
                Debug.Log("Location Markers placed: " + placedMarkerCount);
            }
            else Debug.LogError("No Location Markers were placed");
        }

        private static LocationPrefab Select_RandomLocationPrefab(List<LocationPrefab> allLocationPrefabs)
        {
            if (allLocationPrefabs.Count == 1) return allLocationPrefabs[0];
            return allLocationPrefabs[UnityEngine.Random.Range(0, allLocationPrefabs.Count)];
        }

        public bool Generate_Location(LocationData locationData, Transform folder)
        {
            //Temp
            if (locationData.locationType == LocationType.Tunnel) return false;

            WFC_Core wfc = new WFC_Core(
                locationData.prefabSettings.tileDirectory,
                HexCellUtil.OrganizeByLayer(locationData.cluster.prototypes),

                cellLookup_ByLayer_BySize_ByWorldSpace,

                locationData.prefabSettings,
                folder
            );

            wfc.ExecuteWFC();
            return true;
        }

        public GameObject Generate_UndergroundTunnel(LocationData locationData, Transform folder)
        {
            LocationMarkerPrefabOption settings = locationData.prefabSettings;
            int tunnelMemberCount = UnityEngine.Random.Range(settings.cluster_TunnelMemberMin, settings.cluster_TunnelMemberMax);

            HexagonCellCluster tunnelCluster = HexagonCellManager.GenerateCluster_UnderGroundTunnel(locationData.cluster, tunnelMemberCount, CellSearchPriority.LayerNeighbors);
            if (tunnelCluster == null)
            {
                Debug.LogError("tunnelCluster is null");
                return null;
            }

            // tunnelCluster.prototypes[0].isLocationMarker = true;
            GameObject tunnelGameObject = HexagonCellManager.CreateTunnelGameObjectCluster(
                tunnelCluster,
                tunnelPrefab,
                transform,
                cellLayerElevation
            );

            if (folder != null) tunnelGameObject.transform.SetParent(folder);
            return tunnelGameObject;
        }

        private bool Instantiate_Location(Vector2 areaLookup, Vector2 worldspaceLookup)
        {
            if (_locations_ByWorldspace_ByArea.ContainsKey(areaLookup) == false || _locations_ByWorldspace_ByArea[areaLookup].ContainsKey(worldspaceLookup) == false)
            {
                return false;
            }
            return Generate_Location(_locations_ByWorldspace_ByArea[areaLookup][worldspaceLookup], WorldParentFolder());
        }

        private void Evaluate_LocationMarkerPrefabs()
        {
            (List<LocationPrefab> filteredDefault, List<LocationPrefab> preassignable) = WorldLocationManager.Evaluate_LocationMarkerPrefabs(locationMarkerPrefabOptions);

            locationMarkerPrefabs_Default = filteredDefault;
            locationMarkerPrefabs_ToPreAssign = preassignable;

            if (locationMarkerPrefabs_ToPreAssign.Count > 0)
            {
                Debug.Log("Detected " + preassignable.Count + " pre-assignable location prefabs");
            }
        }

        public List<HexagonCellPrototype> AssignLocationMarkers(List<HexagonCellPrototype> allAvailable, List<LocationPrefab> locationPrefabsDefault, List<LocationPrefab> locationPrefabsPreassigned)
        {
            List<HexagonCellPrototype> results = new List<HexagonCellPrototype>();
            List<Vector3> avoidPoints = new List<Vector3>();
            int fails = 0;

            List<CellStatus> ignoresStatus = new List<CellStatus>() {
                    CellStatus.GenericGround,

                    CellStatus.Remove,
                    CellStatus.AboveGround,
                    CellStatus.UnderGround,
                    CellStatus.Underwater,
            };

            Dictionary<LocationType, List<LocationData>> placedLocationsByType = new Dictionary<LocationType, List<LocationData>>();

            // Handle Pre-assigned first
            HashSet<string> processedWSIds = new HashSet<string>();

            if (_preassignedWorldspaceCells.Count > 0)
            {
                foreach (HexagonCellPrototype preassignedWS in _preassignedWorldspaceCells)
                {
                    if (processedWSIds.Contains(preassignedWS.uid)) continue;

                    List<HexagonCellPrototype> groupNeighbors = HexGridPathingUtil.GetConsecutiveNeighborsList(
                                                        preassignedWS,
                                                        99,
                                                        _preassignedWorldspaceCells,
                                                        CellSearchPriority.SideNeighbors,
                                                        null,
                                                        null,
                                                        false,
                                                        true
                                                    );

                    HashSet<Vector2> preassignedParentCoord = new HashSet<Vector2>();
                    preassignedParentCoord.Add(preassignedWS.GetLookup());
                    // HashSet<string> preassignedParentIds = new HashSet<string>();
                    // preassignedParentIds.Add(preassignedWS.GetId());

                    List<Vector3> worldspaceCorners = new List<Vector3>();

                    int totalWSGroupSize = groupNeighbors.Count;

                    if (totalWSGroupSize > 0)
                    {
                        foreach (var item in groupNeighbors)
                        {
                            if (processedWSIds.Contains(item.uid)) continue;

                            processedWSIds.Add(item.uid);
                            preassignedParentCoord.Add(item.GetLookup());
                            worldspaceCorners.AddRange(item.cornerPoints);
                            // preassignedParentIds.Add(item.GetId());
                        }
                    }

                    List<HexagonCellPrototype> preassignedWSCellGroup = allAvailable.FindAll(c => preassignedParentCoord.Contains(c.GetWorldSpaceLookup()));

                    Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer = HexagonCellManager.Select_NormalizedPrototypeSubGridByLayer(preassignedWSCellGroup);
                    HexagonCellCluster newPreassignedCluster = HexagonCellManager.Generate_ClusterSubGridFromPrototypesByLayer(
                                    prototypesByLayer,
                                    CellClusterType.Other,
                                    true,
                                    null
                                );

                    List<HexagonCellPrototype> groundEdges = preassignedWSCellGroup.FindAll(c => c.IsEdge() && c.IsGroundCell());

                    if (groundEdges == null || groundEdges.Count == 0)
                    {
                        Debug.LogError("No groundEdges found");
                    }
                    else
                    {
                        int entrances = 2 + (totalWSGroupSize - 1);

                        List<HexagonCellPrototype> entryCells = WFCUtilities_V2.Select_RandomEntryCellsFromEdges(
                                    groundEdges,
                                    entrances,
                                    true,
                                    32f,
                                    preassignedWSCellGroup,
                                    2
                                    );


                        Bounds bounds = VectorUtil.CalculateBounds(worldspaceCorners);
                        // int cols = 7;
                        // int rows = 7;
                        // if (totalWSGroupSize > 1)
                        // {
                        //     // pointCenterRadius = 6.8f;
                        //     cols = (6 * totalWSGroupSize);
                        //     rows = (6 * totalWSGroupSize);
                        // }
                        float pointCenterRadius = 11f;

                        List<HexagonCellPrototype> path = HexGridPathingUtil.Generate_CityGridPaths(
                            bounds,
                            preassignedWSCellGroup,
                            pointCenterRadius,
                            true,
                            4
                        );

                        // HexagonCellCluster newPreassignedCluster = new HexagonCellCluster(
                        //         preassignedWSCellGroup[0].GetId(),
                        //         preassignedWSCellGroup,
                        //         CellClusterType.Other,
                        //         ClusterGroundCellLayerRule.Unset);

                        newPreassignedCluster.prototypes[0].isLocationMarker = true;
                        results.Add(newPreassignedCluster.prototypes[0]);
                        avoidPoints.Add(newPreassignedCluster.prototypes[0].center);

                        // LocationData locationData = AddNew_ClusterLocation(newPreassignedCluster.prototypes[0].GetLookup(), newPreassignedCluster, LocationType.City, locationMarkerPrefabs_ToPreAssign[0].GetSettings());

                        // LocationType locationType = locationPrefabsPreassigned[0].GetSettings().locationType;
                        // if (placedLocationsByType.ContainsKey(locationType) == false)
                        // {
                        //     placedLocationsByType.Add(locationType, new List<LocationData>());
                        // }
                        // placedLocationsByType[locationType].Add(locationData);
                    }


                }
            }

            foreach (HexagonCellPrototype worldspaceCell in _activeWorldspaceCells)
            {
                if (processedWSIds.Contains(worldspaceCell.uid)) continue;


                for (int i = 0; i < locationsPerWorldMax; i++)
                {
                    bool added = false;

                    LocationPrefab locationPrefab = Select_RandomLocationPrefab(locationPrefabsDefault);
                    Debug.LogError("locationPrefab - locationType: " + locationPrefab.GetSettings().locationType);

                    HexagonCellPrototype found = WorldLocationManager.FindViableCellForLocationPrefab(
                        locationPrefab,
                        allAvailable,
                        placedLocationsByType
                    );

                    // HexagonCellPrototype found = World_GetRandomPoint(allAvailable, avoidPoints, avoidRadius);
                    if (found != null)
                    {
                        LocationMarkerPrefabOption prefabOption = locationPrefab.GetSettings();
                        int memberCount = UnityEngine.Random.Range(prefabOption.cluster_memberMin, prefabOption.cluster_memberMax);

                        HexagonCellCluster newCluster = HexagonCellManager.Generate_ClusterFromStartCell(
                                found,
                                memberCount,
                                CellClusterType.Outpost,
                                ignoresStatus,
                                false,
                                CellSearchPriority.SideAndSideLayerNeighbors
                        );

                        // Dictionary<int, List<HexagonCellPrototype>> prototypesByLayer = HexagonCellManager.GetPrototypesGroupByLayerFromConsecutiveSearch(
                        //         found,
                        //         membersMinMax,
                        //         ignoresStatus,
                        //         CellSearchPriority.SideAndSideLayerNeighbors,
                        //         false
                        //     );

                        // HexagonCellCluster newCluster = HexagonCellManager.Generate_ClusterSubGridFromPrototypesByLayer(
                        //     prototypesByLayer,
                        //     CellClusterType.Outpost,
                        //     membersMinMax,
                        //     locationMarkerPrefab.radius,
                        //     true,
                        //     ignoresStatus
                        // );


                        // HexagonCellCluster newCluster = HexagonCellManager.Generate_ClusterSubGridWithinStartCellRadius(
                        //     found,
                        //     allAvailable,
                        //     CellClusterType.Outpost,
                        //     membersMinMax,
                        //     locationMarkerPrefab.radius,
                        //     true,
                        //     ignoresStatus
                        // );


                        // HexagonCellCluster newCluster = HexagonCellManager.GenerateCellClusterFromStartCell_Radius(
                        //     found,
                        //     allAvailable,
                        //     membersMinMax,
                        //     locationMarkerPrefab.radius,
                        //     CellClusterType.Outpost,
                        //     ignoresStatus,
                        //     false
                        // );


                        if (newCluster != null)
                        {
                            // if (prefabOption.tileDirectory.HasMicroTiles())
                            // {
                            //     HexagonGrid.Generate_MicroGrid(newCluster, 4, true, cellLayerElevation, transform);
                            // }

                            found.isLocationMarker = true;
                            results.Add(found);
                            avoidPoints.Add(found.center);
                            added = true;

                            // Vector2 worldspaceLookup = found.GetLookup();
                            // LocationData locationData = AddNew_ClusterLocation(worldspaceLookup, newCluster, prefabOption.locationType, locationPrefab.GetSettings());

                            // LocationType locationType = locationData.locationType;
                            // if (placedLocationsByType.ContainsKey(locationType) == false)
                            // {
                            //     placedLocationsByType.Add(locationType, new List<LocationData>());
                            // }
                            // placedLocationsByType[locationType].Add(locationData);


                            // if (prefabOption.locationType == LocationType.Tunnel)
                            // {
                            //     int tunnelMemberCount = UnityEngine.Random.Range(prefabOption.cluster_TunnelMemberMin, prefabOption.cluster_TunnelMemberMax);

                            //     HexagonCellCluster tunnelCluster = HexagonCellManager.GenerateCluster_UnderGroundTunnel(newCluster, tunnelMemberCount, prefabOption.cellSearchPriority);
                            //     if (tunnelCluster != null)
                            //     {
                            //         tunnelCluster.prototypes[0].isLocationMarker = true;
                            //         locationData = AddNew_ClusterLocation(worldspaceLookup, tunnelCluster, prefabOption.locationType, locationPrefab.GetSettings());

                            //         locationType = locationData.locationType;
                            //         if (placedLocationsByType.ContainsKey(locationType) == false)
                            //         {
                            //             placedLocationsByType.Add(locationType, new List<LocationData>());
                            //         }
                            //         placedLocationsByType[locationType].Add(locationData);


                            //         GameObject tunnel = HexagonCellManager.CreateTunnelGameObjectCluster(
                            //             tunnelCluster,
                            //             tunnelPrefab,
                            //             transform,
                            //             cellLayerElevation
                            //         );

                            //         tunnel.transform.SetParent(WorldParentFolder());
                            //     }
                            //     else
                            //     {
                            //         Debug.LogError("tunnelCluster is null");
                            //     }
                            // }
                        }
                        else
                        {
                            Debug.LogError("newCluster is null");
                        }
                    }

                    if (added == false) fails++;
                }

            }

            if (fails > 0)
            {
                Debug.LogError("Some Location Markers Failed to be placed");
            }

            return results;
        }


        #endregion

        // public static TerrainVertex[,] GenerateRectangleGridInHexagon(
        //     Dictionary<Vector2, TerrainVertex> globalVertexGrid,
        //     Vector3 topLeft,
        //     Vector3 topRight,
        //     Vector3 bottomLeft,
        //     Vector3 bottomRight,
        //     Vector2 _worldspaceOwnerCoordinate,
        //     int vertexDensity = 100
        // )
        // {
        //     // Calculate the step sizes along each axis
        //     float xStep = 1f / vertexDensity * (topRight - topLeft).magnitude;
        //     float zStep = 1f / vertexDensity * (bottomLeft - topLeft).magnitude;

        //     // Create the grid
        //     TerrainVertex[,] grid = new TerrainVertex[vertexDensity + 1, vertexDensity + 1];

        //     // Loop through each vertex in the grid
        //     for (int z = 0; z <= vertexDensity; z++)
        //     {
        //         for (int x = 0; x <= vertexDensity; x++)
        //         {
        //             // Calculate the position of the vertex
        //             Vector3 pos = topLeft + xStep * x * (topRight - topLeft).normalized + zStep * z * (bottomLeft - topLeft).normalized;

        //             // Check if the position is within the bounds of the rectangle
        //             if (!IsPointWithinRectangleBounds(pos, topLeft, topRight, bottomLeft, bottomRight))
        //             {
        //                 continue;
        //             }

        //             // Find the closest vertex in the x direction
        //             TerrainVertex closestVertex = FindClosestVertexInXDirection(globalVertexGrid, new TerrainVertex() { position = pos }, xStep);

        //             // Create the TerrainVertex object
        //             TerrainVertex vertex = new TerrainVertex()
        //             {
        //                 worldspaceOwnerCoordinate = _worldspaceOwnerCoordinate,
        //                 coordinate = closestVertex != null ? closestVertex.coordinate : new Vector2Int((int)pos.x, (int)pos.z),
        //                 position = pos,
        //                 index_X = x,
        //                 index_Z = z,
        //                 type = VertexType.Generic,
        //                 markedForRemoval = false,

        //                 parallelWorldspaceOwnerCoordinate = Vector2.positiveInfinity,
        //                 parallelVertexIndex_X = -1,
        //                 parallelVertexIndex_Z = -1,

        //                 isEdgeVertex = (z == 0 || x == 0 || z == vertexDensity || x == vertexDensity),
        //                 isInHexBounds = true
        //             };

        //             // Add the vertex to the grid
        //             grid[x, z] = vertex;
        //         }
        //     }

        //     return grid;
        // }
    }

    // public HexagonGrid CreateWorldSpaceCellGrid(HexagonCellPrototype worldAreaHexCell, List<Vector3> baseCenterPoints = null, HexCellSizes defaultCellSize = HexCellSizes.Default)
    // {
    //     Vector3 centerPos = worldAreaHexCell.center;

    //     HexagonGrid newHexGrid = new HexagonGrid(
    //      (int)defaultCellSize,
    //      cellLayersMax,
    //      cellLayersMax,
    //      cellLayerElevation,
    //      GridPreset.Outpost
    //     );

    //     Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer;

    //     if (baseCenterPoints != null && baseCenterPoints.Count > 0)
    //     {
    //         newPrototypesByLayer = newHexGrid.CreateWorldSpaceCellGrid_FromBasePoints(
    //                         baseCenterPoints,
    //                         worldAreaHexCell,
    //                         _worldspaceSize,
    //                         (int)defaultCellSize,
    //                         _global_cellGridElevation,
    //                         transform
    //                     );
    //     }
    //     else
    //     {
    //         newPrototypesByLayer = newHexGrid.CreateWorldSpaceCellGrid(
    //                         worldAreaHexCell,
    //                         _worldspaceSize,
    //                         (int)defaultCellSize,
    //                         _global_cellGridElevation,
    //                         transform
    //                     );
    //     }

    //     // (
    //     //     Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer  = newHexGrid.CreateWorldSpaceCellGrid(
    //     //                     worldAreaHexCell,
    //     //                     _worldspaceSize,
    //     //                     defaultCellSize,
    //     //                     _global_cellGridElevation,
    //     //                     transform
    //     //                 );
    //     // // (
    //     //     Dictionary<int, List<HexagonCellPrototype>> newPrototypesByLayer,
    //     //     List<HexagonCellPrototype> outOfBoundsPoints,
    //     //     List<Vector2> allXZCenterPoints

    //     // ) = newHexGrid.CreateWorldSpaceCellGrid(
    //     //                     worldAreaHexCell,
    //     //                     _worldspaceSize,
    //     //                     defaultCellSize,
    //     //                     _global_cellGridElevation,
    //     //                     transform
    //     //                 );

    //     _worldSpaceCellGridByCenterCoordinate.Add(worldAreaHexCell.GetLookup(), newHexGrid);
    //     return newHexGrid;
    // }
    // public List<TerrainVertex[,]> GetWorldSpaceNeighborVertexGrids(List<HexagonCellPrototype> waCellNeighbors, bool filterPreprocessCompleted)
    // {
    //     List<TerrainVertex[,]> worldSpaceNeighborVertexGrids = new List<TerrainVertex[,]>();

    //     foreach (HexagonCellPrototype waHexCell in waCellNeighbors)
    //     {
    //         if (!waHexCell.HasWorldCoordinate()) continue;

    //         Vector2 worldSpaceCoordinate = waHexCell.GetWorldCordinate();
    //         (HexagonCellPrototype neighborCell, WorldArea neighborWa) = _worldAreaDataByCenterCoordinate[worldSpaceCoordinate];

    //         if (filterPreprocessCompleted && (neighborWa == null || neighborWa.preprocessCompleted == false)) continue;

    //         TerrainVertex[,] waVertexGrid = neighborWa.GetTerrainVertices();
    //         worldSpaceNeighborVertexGrids.Add(waVertexGrid);

    //         if (_terrainVertexGridDataByCenterCoordinate.ContainsKey(worldSpaceCoordinate) == false)
    //         {
    //             _terrainVertexGridDataByCenterCoordinate.Add(worldSpaceCoordinate, waVertexGrid);
    //         }
    //     }

    //     return worldSpaceNeighborVertexGrids;
    // }


    // public Dictionary<Vector2, TerrainVertex[,]> GetWorldSpaceNeighborVertexGridsByCoordinate(List<HexagonCellPrototype> waCellNeighbors, bool filterPreprocessCompleted)
    // {
    //     Dictionary<Vector2, TerrainVertex[,]> worldSpaceNeighborVertexGrids = new Dictionary<Vector2, TerrainVertex[,]>();

    //     foreach (HexagonCellPrototype waHexCell in waCellNeighbors)
    //     {
    //         if (!waHexCell.HasWorldCoordinate()) continue;

    //         Vector2 worldSpaceCoordinate = waHexCell.GetWorldCordinate();
    //         (HexagonCellPrototype neighborCell, WorldArea neighborWa) = _worldAreaDataByCenterCoordinate[worldSpaceCoordinate];

    //         if (filterPreprocessCompleted && (neighborWa == null || neighborWa.preprocessCompleted == false)) continue;

    //         TerrainVertex[,] waVertexGrid = neighborWa.GetTerrainVertices();

    //         if (_terrainVertexGridDataByCenterCoordinate.ContainsKey(worldSpaceCoordinate) == false)
    //         {
    //             _terrainVertexGridDataByCenterCoordinate.Add(worldSpaceCoordinate, waVertexGrid);
    //         }

    //         worldSpaceNeighborVertexGrids.Add(worldSpaceCoordinate, waVertexGrid);
    //     }

    //     return worldSpaceNeighborVertexGrids;
    // }

    // private void EvaluateUnclaimedPrototypes()
    // {
    //     if (_remainderPrototypes.Count > 0)
    //     {
    //         Dictionary<Vector2, List<HexagonCellPrototype>> toAddByWSCoordinate = new Dictionary<Vector2, List<HexagonCellPrototype>>();
    //         float searchRange = _worldspaceSize * 1.65f;

    //         while (_remainderPrototypes.Count > 0)
    //         {
    //             HexagonCellPrototype current = _remainderPrototypes[0];

    //             (List<HexagonGrid> cellGridsInRange, List<Vector2> wsCoordinates) = GetWorldSpaceeHexGridsInRangeOfPosition(current.center, (int)searchRange);
    //             if (cellGridsInRange.Count == 0 || IsCellCenterPointAssignedToAGrid(current, cellGridsInRange))
    //             {
    //                 _remainderPrototypes.Remove(current);
    //                 continue;
    //             }
    //             Vector2 closestWSCoordinate = VectorUtil.GetClosestPoint_XZ(wsCoordinates, current.center);
    //             if (closestWSCoordinate != Vector2.positiveInfinity)
    //             {
    //                 if (toAddByWSCoordinate.ContainsKey(closestWSCoordinate) == false)
    //                 {
    //                     toAddByWSCoordinate.Add(closestWSCoordinate, new List<HexagonCellPrototype>());
    //                 }
    //                 toAddByWSCoordinate[closestWSCoordinate].Add(current);
    //                 _remainderPrototypes.Remove(current);
    //             }
    //         }
    //         if (toAddByWSCoordinate.Count > 0)
    //         {
    //             foreach (var kvp in toAddByWSCoordinate)
    //             {
    //                 Vector2 wsCoordinate = kvp.Key;
    //                 HexagonCellPrototype wsCell = _worldspaceHexCellByCenterCoordinate[wsCoordinate];
    //                 bool added = _worldSpaceCellGridByCenterCoordinate[wsCoordinate].AddCenterPoints(kvp.Value, wsCell);
    //                 if (added == false)
    //                 {
    //                     Debug.LogError("Failed at add AddCenterPoints");
    //                 }
    //             }
    //         }
    //     }
    // }


    // public void Initialize_WorldspaceCells(List<HexagonCellPrototype> activeWorldspaceCells)
    // {
    //     // List<HexagonCellPrototype> elderWorldSpaceCells = new List<HexagonCellPrototype>();

    //     // Bounds bounds = CalculateBounds(worldspaceCells[0].cornerPoints.ToList());
    //     // temp_globalGrid = GenerateGridPoints(bounds, vertexDensity);

    //     // List<Vector3> rectangleCorners = VectorUtil.HexagonCornersToRectangle(worldspaceCells[1].center, worldspaceCells[1].size);
    //     // Vector3 topLeft = rectangleCorners[2];
    //     // Vector3 topRight = rectangleCorners[3];
    //     // Vector3 bottomLeft = rectangleCorners[0];
    //     // Vector3 bottomRight = rectangleCorners[1];

    //     // temp_globalGrid = GenerateGridPoints(topLeft, topRight, bottomLeft, bottomRight, vertexDensity);
    //     // Debug.LogError("temp_globalGrid: " + temp_globalGrid.Keys.Count);

    //     // int found = 0;
    //     // foreach (var item in temp_globalGrid.Keys)
    //     // {
    //     //     if (IsApproximatelyInList(item, _globalTerrainVertexGrid.Keys.ToList(), 0.04f))
    //     //     {
    //     //         found++;
    //     //     }
    //     // }
    //     // Debug.LogError("temp_globalGrid - found: " + found + " of " + temp_globalGrid.Count);

    //     Vector2 initialCoord = VectorUtil.Calculate_AproximateCoordinate(transform.position);

    //     foreach (var worldspaceCell in activeWorldspaceCells)
    //     {
    //         if (!worldspaceCell.HasWorldCoordinate())
    //         {
    //             Debug.LogError("Missing WorldSpace Coordinate!");
    //             continue;
    //         }

    //         Vector2 worldLookupCoord = worldspaceCell.GetLookup();

    //         TerrainVertex[,] vertexGrid;

    //         if (disable_terrainGeneration)
    //         {
    //             Bounds bounds = VectorUtil.CalculateBounds(_worldSpaces_ByArea[initialCoord][worldLookupCoord].cornerPoints.ToList());
    //             Vector2[,] worldSpaceVertexKeys = GetLocalVertexGridKeys(
    //                 bounds,
    //                 _globalTerrainVertexGridByCoordinate,
    //                 _worldSpaces_ByArea[initialCoord][worldLookupCoord],
    //                 transform,
    //                 vertexDensity
    //             );

    //             if (_globalTerrainVertexKeysByWorldspaceCoordinate.ContainsKey(worldLookupCoord) == false)
    //             {
    //                 _globalTerrainVertexKeysByWorldspaceCoordinate.Add(worldLookupCoord, worldSpaceVertexKeys);
    //             }
    //             else
    //             {
    //                 // Debug.LogError("vertexGrid in _globalTerrainVertexKeysByWorldspaceCoordinate was overwritten at coordinate: " + worldLookupCoord);
    //                 _globalTerrainVertexKeysByWorldspaceCoordinate[worldLookupCoord] = worldSpaceVertexKeys;
    //             }

    //             // vertexGrid = Generate_WorldSpaceVertexGrid(worldLookupCoord, worldspaceMeshGO);
    //             // vertexGrid = Generate_WorldSpaceVertexGrid(worldLookupCoord, worldspaceMeshGO, gridCoordinatesByWorldCoord[worldCoord]);
    //         }
    //         else
    //         {

    //             vertexGrid = Generate_WorldSpace_VertexGrid_WithNoise(
    //                 worldspaceCell,
    //                 transform,
    //                 fastNoiseUnity,
    //                 _global_terrainHeightDefault,
    //                 persistence,
    //                 octaves,
    //                 lacunarity
    //             );


    //             if (_terrainVertexGridDataByCenterCoordinate.ContainsKey(worldLookupCoord) == false)
    //             {
    //                 _terrainVertexGridDataByCenterCoordinate.Add(worldLookupCoord, vertexGrid);
    //             }
    //             else
    //             {
    //                 Debug.LogError("vertexGrid in _terrainVertexGridDataByCenterCoordinate was overwritten at coordinate: " + worldLookupCoord);
    //                 _terrainVertexGridDataByCenterCoordinate[worldLookupCoord] = vertexGrid;
    //             }
    //         }
    //         // if (elderWorldSpaceCells.Contains(worldspaceCell) == false) elderWorldSpaceCells.Add(worldspaceCell);
    //         // Initial Mesh Generate
    //         // Update_TerrainMeshOnObject(
    //         //     worldspaceMeshGO,
    //         //     worldCoord,
    //         //     _terrainVertexGridDataByCenterCoordinate,
    //         //     fastNoiseUnity,
    //         //     transform,
    //         //     _global_terrainHeightDefault,
    //         //     persistence,
    //         //     octaves,
    //         //     lacunarity,
    //         //     false // initial smooth
    //         // );

    //         HexagonGrid hexGrid = GetWorldSpaceCellGrid(worldLookupCoord);

    //         if (hexGrid == null) continue;

    //         Dictionary<int, List<HexagonCellPrototype>> gridCellPrototypesByLayer = hexGrid.GetDefaultPrototypesByLayer();

    //         SetupCellGridState(
    //             _globalTerrainVertexGridByCoordinate,
    //             _globalTerrainVertexKeysByWorldspaceCoordinate[worldLookupCoord],
    //             hexGrid,
    //             transform,
    //             viableFlatGroundCellSteepnessThreshhold,
    //             cellVertexSearchRadiusMult,
    //             groundSlopeElevationStep,
    //             _globalSeaLevel
    //         );

    //         // if (disable_terrainGeneration)
    //         // {
    //         //     RefreshTerrainMeshOnObject(
    //         //         worldspaceMeshGO,
    //         //         _globalTerrainVertexKeysByWorldspaceCoordinate[worldCoord],
    //         //         _globalTerrainVertexGrid,
    //         //         transform
    //         //     );
    //         // }
    //         // if (disable_terrainGeneration) RefreshTerrainMeshOnObject(worldspaceMeshGO, vertexGrid, transform);
    //     }

    //     if (!disable_terrainGeneration)
    //     {


    //         List<Vector2> refreshCoordinates = Update_WorldSpaceTerrains(
    //             activeWorldspaceCells,
    //             _worldAreaMeshObjectByCenterCoordinate,
    //             _terrainVertexGridDataByCenterCoordinate,
    //             transform,
    //             fastNoiseUnity,
    //             _global_terrainHeightDefault,
    //             persistence,
    //             octaves,
    //             lacunarity
    //         );

    //         if (refreshCoordinates != null && refreshCoordinates.Count > 0)
    //         {
    //             RefreshTerrainMeshCoordinates(refreshCoordinates);
    //         }
    //     }
    //     else
    //     {
    //         RefreshTerrainMeshObjects(
    //             _globalTerrainVertexGridCoordinates,
    //             _globalTerrainVertexGridByCoordinate,
    //             transform
    //         );

    //         // Mesh newMesh = new Mesh();
    //         // worldMeshObject = MeshUtil.InstantiatePrefabWithMesh(worldAreaMeshObjectPrefab, newMesh, transform.position);
    //         // worldMeshObject.transform.SetParent(worldspaceFolder);
    //         // RefreshTerrainMeshOnObject(
    //         //     worldMeshObject,
    //         //     _globalTerrainVertexGridCoordinates,
    //         //     _globalTerrainVertexGridByCoordinate,
    //         //     transform
    //         // );

    //         // UpdateTerrainTexture_V2(worldMeshObject);
    //     }
    // }

    // public bool IsWorldAreaCoordinateInRegion(Vector2 areaLookupCoord, Vector2 regionLookupCoord)
    // {
    //     return (_worldAreas_ByRegion.ContainsKey(regionLookupCoord) && _worldAreas_ByRegion[regionLookupCoord].ContainsKey(areaLookupCoord));
    // }
    // public bool IsWorldAreaCoordinateInRegionNeighbors(Vector2 areaLookupCoord, Vector2 regionLookupCoord)
    // {
    //     if (_worldRegionsLookup.ContainsKey(regionLookupCoord))
    //     {
    //         return _worldRegionsLookup[regionLookupCoord].neighbors.Any(n => IsWorldAreaCoordinateInRegion(areaLookupCoord, CalculateAproximateCoordinate(n.worldCoordinate)));
    //     }
    //     return false;
    // }

    // public static void Update_TerrainMeshOnObject(
    //     GameObject meshObject,
    //     Vector2 coordinate,
    //     Dictionary<Vector2, TerrainVertex[,]> vertexGridDataByWorldSpaceCoordinate,
    //     FastNoiseUnity fastNoiseUnity,
    //     Transform transform,
    //     float terrainHeight,
    //     float persistence,
    //     float octaves,
    //     float lacunarity,
    //     bool initialSmooth = false
    // )
    // {

    //     if (meshObject == null || fastNoiseUnity == null)
    //     {
    //         Debug.LogError("Null meshObject or fastNoiseUnity");
    //         return;
    //     }

    //     TerrainVertex[,] vertexGrid = vertexGridDataByWorldSpaceCoordinate[coordinate];

    //     // Get the MeshFilter component from the instantiatedPrefab
    //     MeshFilter meshFilter = meshObject.GetComponent<MeshFilter>();
    //     MeshCollider meshCollider = meshObject.GetComponent<MeshCollider>();

    //     Mesh mesh = meshFilter.sharedMesh;
    //     mesh.name = "World Spaace Mesh";


    //     UpdateTerrainNoiseVertexElevations(
    //         fastNoiseUnity,
    //         vertexGrid,
    //         transform,
    //         terrainHeight,
    //         persistence,
    //         octaves,
    //         lacunarity
    //     );

    //     bool useIgnoreRules = true;
    //     int neighborDepth = 2;
    //     int cellGridWeight = 1;
    //     float ratio = 1f;
    //     int inheritedWeight = 2;

    //     // if (initialSmooth)
    //     // {
    //     // Debug.Log("initialSmooth!");

    //     // TerrainVertexUtil.SmoothWorldAreaVertexElevationTowardsCenter__V2(
    //     //     vertexGrid,
    //     //     vertexGridDataByWorldSpaceCoordinate,
    //     //     meshObject.transform.position,
    //     //     useIgnoreRules,
    //     //     neighborDepth,
    //     //     cellGridWeight,
    //     //     inheritedWeight,
    //     //     ratio
    //     // );

    //     // TerrainVertexUtil.SmoothWorldAreaVertexElevationTowardsCenter(vertexGrid, meshObject.transform.position, false);
    //     // }

    //     // int cylces = 2;
    //     // do
    //     // {
    //     //     HexGridVertexUtil.SmoothGridEdgeVertexList__V2(
    //     //         vertexGrid,
    //     //         vertexGridDataByWorldSpaceCoordinate,
    //     //         neighborDepth,
    //     //         ratio,
    //     //         cellGridWeight,
    //     //         2
    //     //     );
    //     //     // HexGridVertexUtil.SmoothGridEdgeVertexList(
    //     //     //     vertexGrid,
    //     //     //     neighborDepth,
    //     //     //     ratio,
    //     //     //     cellGridWeight,
    //     //     //     3
    //     //     // );
    //     //     // TerrainVertexUtil.SmoothElevationAroundCellGrid(vertexGrid, 4, 1, 2);

    //     //     cylces--;
    //     // } while (cylces > 0);

    //     // HexGridVertexUtil.SmoothGridEdgeVertexIndices(
    //     //     cellManager.GetAllPrototypesOfCellStatus(CellStatus.Ground).FindAll(p => p.isEdge && p.IsRemoved() == false),
    //     //     vertexGrid, cellVertexSearchRadiusMult, gridEdgeSmoothingRadius, smoothingFactor, smoothingSigma);

    //     RefreshTerrainMeshOnObject(meshObject, vertexGrid, transform);
    //     // Debug.Log("Generate_TerrainMeshOnObject - Complete");
    // }


    // public void RefreshTerrainMeshes(
    //     Vector2[,] worldspaceVertexKeys,
    //     Dictionary<Vector2, TerrainVertex> globalTerrainVertexGrid,
    //     Transform transform,
    //     Bounds activeGridBounds,
    //     List<HexagonCellPrototype> activeWorldspaceCells,
    //     List<Vector3> chunkCenterPositions = null
    // )
    // {
    //     // List<Vector2[,]> gridChunks = GenerateVertexGridChunks_V3(worldspaceVertexKeys, meshChunkSize, activeGridBounds, chunkCenterPositions);
    //     // List<Vector2[,]> gridChunks = Generate_VertexGridChunksFromCenterPoints(activeGridBounds, globalTerrainVertexGrid, transform, vertexDensity, chunkCenterPositions);

    //     Vector2Int chunksXZ = CalculateMeshChunkCount(activeGridBounds, meshChunkSize);
    //     temp_chunkCenters = DivideBoundsIntoChunks(activeGridBounds, chunksXZ);

    //     List<(Vector2[,], Vector2)> gridChunksWithCenterPos = GenerateVertexGridChunks_V2(worldspaceVertexKeys, chunksXZ, activeGridBounds);
    //     // List<Vector2[,]> gridChunks = GenerateVertexGridChunks(worldspaceVertexKeys, chunksXZ.x, chunksXZ.y);
    //     // _gridChunks = gridChunks;
    //     Dictionary<Vector2, GameObject> new_worldspaceTerrainChunks_ByLookup = new Dictionary<Vector2, GameObject>();

    //     for (int i = 0; i < gridChunksWithCenterPos.Count; i++)
    //     // for (int i = 0; i < gridChunks.Count; i++)
    //     {
    //         // Vector2 chunkCenterLookup = TerrainChunkData.CalculateTerrainChunkLookup(chunkCenterPositions[i]);
    //         Vector2 chunkCenterLookup = TerrainChunkData.CalculateTerrainChunkLookup(gridChunksWithCenterPos[i].Item2);
    //         Evaluate_WorldAreaFolder_ByLookup(chunkCenterLookup);

    //         HexagonCellPrototype worldspaceCell = Find_FirstWorldspaceCellWithTerrainChunkLookup(chunkCenterLookup, activeWorldspaceCells);
    //         if (worldspaceCell == null)
    //         {
    //             // Debug.LogError("No worldspaceCell found for chunkCenterLookup: " + chunkCenterLookup);
    //             // return;
    //             continue;
    //         }

    //         // Extract Grid Data
    //         // Vector2[,] chunk = gridChunks[i];
    //         Vector2[,] chunk = gridChunksWithCenterPos[i].Item1;

    //         (Vector3[] vertexPositions, Vector2[] uvs, HashSet<Vector2> meshTraingleExcludeList) = TerrainVertexUtil.ExtractVertexWorldPositionsAndUVs_V2(
    //             chunk,
    //             globalTerrainVertexGrid,
    //             transform
    //         );

    //         GameObject meshChunkObject = Get_TerrainChunkObjectByIndexData_V2(chunkCenterLookup);
    //         if (meshChunkObject == null)
    //         {
    //             Debug.LogError("meshChunkObject is null.  chunkCenterLookup: " + chunkCenterLookup);
    //             continue;
    //         }

    //         // Get the MeshFilter component from the instantiatedPrefab
    //         MeshFilter meshFilter = meshChunkObject.GetComponent<MeshFilter>();
    //         MeshCollider meshCollider = meshChunkObject.GetComponent<MeshCollider>();
    //         Mesh finalMesh = meshFilter.sharedMesh;

    //         finalMesh.name = "World Mesh";
    //         finalMesh.Clear();
    //         finalMesh.vertices = vertexPositions;
    //         finalMesh.triangles = GenerateTerrainTriangles_V2(chunk, meshTraingleExcludeList);
    //         finalMesh.uv = uvs;

    //         // Refresh Terrain Mesh
    //         finalMesh.RecalculateNormals();
    //         finalMesh.RecalculateBounds();

    //         // Apply the mesh data to the MeshFilter component
    //         meshFilter.sharedMesh = finalMesh;
    //         meshCollider.sharedMesh = finalMesh;

    //         if (new_worldspaceTerrainChunks_ByLookup.ContainsKey(chunkCenterLookup) == false)
    //         {
    //             new_worldspaceTerrainChunks_ByLookup.Add(chunkCenterLookup, meshChunkObject);
    //             meshChunkObject.gameObject.name = "Terrain Chunk_" + chunkCenterLookup;
    //             if (enable_WorldPositionTracking) meshChunkObject.SetActive(false);
    //         }
    //     }

    //     _worldspaceTerrainChunks_ByLookup = new_worldspaceTerrainChunks_ByLookup;
    // }

    // public void RefreshTerrainMeshObjects(
    //     Vector2[,] worldspaceVertexKeys,
    //     Dictionary<Vector2, TerrainVertex> globalTerrainVertexGrid,
    //     Transform transform
    // )
    // {
    //     CalculateMeshChunks(worldspaceVertexKeys, maxChunkSize);

    //     List<Vector2[,]> gridChunks = GenerateVertexGridChunks(worldspaceVertexKeys, _currentMeshChunkDivider);

    //     _gridChunks = gridChunks;

    //     for (int i = 0; i < gridChunks.Count; i++)
    //     {
    //         Vector2[,] chunk = gridChunks[i];

    //         (Vector3[] vertexPositions, Vector2[] uvs, HashSet<Vector2> meshTraingleExcludeList) = TerrainVertexUtil.ExtractVertexWorldPositionsAndUVs(
    //             chunk,
    //             globalTerrainVertexGrid,
    //             transform
    //         );

    //         GameObject meshChunkObject;

    //         if (worldMeshGameObjects.Count - 1 < i || worldMeshGameObjects[i] == null)
    //         {
    //             Mesh newMesh = new Mesh();
    //             meshChunkObject = MeshUtil.InstantiatePrefabWithMesh(worldAreaMeshObjectPrefab, newMesh, transform.position);
    //             meshChunkObject.transform.SetParent(WorldParentFolder());

    //             if (worldMeshGameObjects.Count - 1 < i)
    //             {
    //                 worldMeshGameObjects.Add(meshChunkObject);
    //             }
    //             else worldMeshGameObjects[i] = meshChunkObject;
    //         }
    //         else
    //         {
    //             meshChunkObject = worldMeshGameObjects[i];
    //         }

    //         // Get the MeshFilter component from the instantiatedPrefab
    //         MeshFilter meshFilter = meshChunkObject.GetComponent<MeshFilter>();
    //         MeshCollider meshCollider = meshChunkObject.GetComponent<MeshCollider>();
    //         Mesh finalMesh = meshFilter.sharedMesh;

    //         finalMesh.name = "World Spaace Mesh";
    //         finalMesh.Clear();
    //         finalMesh.vertices = vertexPositions;
    //         finalMesh.triangles = GenerateTerrainTriangles(chunk, meshTraingleExcludeList);
    //         finalMesh.uv = uvs;

    //         // Refresh Terrain Mesh
    //         finalMesh.RecalculateNormals();
    //         finalMesh.RecalculateBounds();

    //         // Apply the mesh data to the MeshFilter component
    //         meshFilter.sharedMesh = finalMesh;
    //         meshCollider.sharedMesh = finalMesh;
    //     }
    // }


    // public void RefreshTerrainMeshOnObject(
    //     GameObject meshObject,
    //     Vector2[,] worldspaceVertexKeys,
    //     Dictionary<Vector2, TerrainVertex> globalTerrainVertexGrid,
    //     Transform transform
    // )
    // {
    //     (Vector3[] vertexPositions, Vector2[] uvs, HashSet<Vector2> meshTraingleExcludeList) = TerrainVertexUtil.ExtractVertexWorldPositionsAndUVs(
    //         worldspaceVertexKeys,
    //         globalTerrainVertexGrid,
    //         transform
    //     );

    //     // Get the MeshFilter component from the instantiatedPrefab
    //     MeshFilter meshFilter = meshObject.GetComponent<MeshFilter>();
    //     MeshCollider meshCollider = meshObject.GetComponent<MeshCollider>();
    //     Mesh mesh = meshFilter.sharedMesh;

    //     mesh.name = "World Spaace Mesh";
    //     mesh.Clear();
    //     mesh.vertices = vertexPositions;
    //     mesh.triangles = ProceduralTerrainUtility.GenerateTerrainTriangles(worldspaceVertexKeys, meshTraingleExcludeList);
    //     mesh.uv = uvs;

    //     // Refresh Terrain Mesh
    //     mesh.RecalculateNormals();
    //     mesh.RecalculateBounds();

    //     // Apply the mesh data to the MeshFilter component
    //     meshFilter.sharedMesh = mesh;
    //     meshCollider.sharedMesh = mesh;
    // }

}