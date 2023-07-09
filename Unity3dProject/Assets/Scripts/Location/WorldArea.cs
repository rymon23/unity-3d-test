using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using WFCSystem;
using UnityEngine.AI;

using UnityEngine.Jobs;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

namespace ProceduralBase
{
    public enum NoiseType { Perlin, Simplex, Value }

    [ExecuteInEditMode]
    public class WorldArea : MonoBehaviour, IUid
    {

        #region Controls
        [SerializeField] private bool trigger_resetGridState;
        [SerializeField] private bool trigger_resetTerrain;
        [SerializeField] private bool trigger_revaluate;

        #endregion


        public string Uid { get; private set; }
        public DateTime CreationDate { get; private set; }
        public HexagonCellPrototype hexagonCell { get; private set; }
        [Header(" ")]
        public WorldAreaManager worldAreaManager;

        #region Manager Methods

        public bool preprocessCompleted = false;

        public void ApplyCellManagerSettings(HexagonCellManagerSettings settings)
        {
            if (cellManager != null) cellManager.ApplySettings(settings);
        }
        public List<HexagonCellPrototype> GetCellGridEdges()
        {
            if (cellManager != null) return cellManager.GetAllPrototypeEdgesOfType(EdgeCellType.Default);
            return null;
        }
        public void SetCellGridElevation(float elevation)
        {
            if (cellManager != null) cellManager.SetManagedElevation(elevation);
        }
        public void SetHexagonCell(HexagonCellPrototype cell)
        {
            hexagonCell = cell;
        }
        public void SetNoise(FastNoiseUnity noise)
        {
            fastNoiseUnity = noise;
        }
        public void SetPersistence(float _persistence)
        {
            persistence = _persistence;
        }
        public void SetHeight(float height)
        {
            terrainHeight = height;
        }
        public Mesh GetMesh() => meshFilter.sharedMesh;
        public TerrainVertex[,] GetTerrainVertices() => vertexGrid;
        #endregion

        public Vector3 CenterPosition() => transform.position;

        [SerializeField] private bool isManaged = true;
        [SerializeField] private bool enableEditMode = true;

        [SerializeField] private HexagonCellManager cellManager;
        [SerializeField] private HexagonCellManager cellPathMicroGridManager;
        [SerializeField] private HexagonTileCore tilePrefabs_MicroClusterParent;

        [SerializeField] private IWFCSystem wfc;

        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private MeshCollider meshCollider;
        [SerializeField] private FastNoiseUnity fastNoiseUnity;


        [SerializeField] private NavMeshSurface navMeshSurface;
        [SerializeField] private MapDisplay mapDisplay;

        [Header("Settings")]
        [Range(12, 296)] public int areaSize = 72;

        [SerializeField] private bool autoAdjustCellGridElevation = true;

        [Header("Terrain Gen Settings")]
        [SerializeField] private bool useTerrainGenerator = true;
        [SerializeField] private bool enableTerrainEditMode = true;
        [Range(-32, 248)] public float terrainHeight = 48f;
        [SerializeField] private NoiseType noiseType;
        [SerializeField] private float noiseScale = 32f;
        [Range(-1.4f, 0.45f)][SerializeField] private float persistence = 0.2f;
        [Range(-1f, 2.6f)][SerializeField] private float lacunarity = 2f;
        [Range(1f, 128f)][SerializeField] private int octaves = 6;

        [Header("Terrain Smoothing")]
        [Range(0, 1f)][SerializeField] float smoothingFactor = 1f;
        [Range(1, 24)][SerializeField] int smoothingNeighborDepth = 4;
        [Range(1, 24)][SerializeField] int cellGridVertexWeight = 3;
        [Range(1, 24)][SerializeField] int cellGridVertexNeighborEvaluationDepth = 8;
        [Range(1, 24)][SerializeField] int inheritedVertexWeight = 4;
        [Header(" ")]

        [Range(1f, 3f)][SerializeField] private float blendRadiusMult = 1.6f;
        [Range(0.05f, 1.5f)][SerializeField] private float groundSlopeElevationStep = 0.45f;

        [Range(1f, 6f)][SerializeField] float cellVertexSearchRadiusMult = 1.4f;
        [Range(3f, 108f)][SerializeField] float gridEdgeSmoothingRadius = 48;
        [Range(0f, 100f)][SerializeField] float smoothingSigma = 0.5f;

        [Header("Vertex Settings")]
        [Range(24, 200)][SerializeField] private int vertexDensity = 100;

        [Header("Terrain Texturing")]
        [SerializeField] private TerrainType[] terrainTypes;
        [SerializeField] private TerrainType[] pathTypes;

        [Header("Procedual Placement")]
        [Range(0.01f, 1f)][SerializeField] private float placement_density = 0.75f;
        [SerializeField] private GameObject placeObjectPrefab;

        [Header("Tunnels ")]
        [SerializeField] private bool allowTunnels = true;
        [SerializeField] private GameObject tunnelMeshParentPrefab;
        [SerializeField] private GameObject _tunnelMeshParent;
        [Range(1, 24)][SerializeField] private int maxTunnelMemberSize = 6;

        [Header("Generate")]
        [SerializeField] private bool updateTerrainTexture;
        [SerializeField] private bool generateTerrain;
        [SerializeField] private bool generatePlacedObjects;
        // [SerializeField] private bool generateNavmesh;

        public TerrainVertex[,] vertexGrid { get; private set; }
        private List<TerrainVertex> accessibleVertices;
        private HashSet<(int, int)> meshTraingleExcludeList;


        #region Saved State
        Vector3 _position;
        int _areaSize;
        float _stepSize;
        float _terrainHeight;
        float _fastNoise_Seed;
        private bool _editorUpdate;
        private bool _doOnce;

        float _gridEdgeSmoothingRadius;
        float _smoothingFactor;
        float _smoothingSigma;

        #endregion
        private Mesh mesh;

        [Header(" ")]


        [Header("Generate")]
        [SerializeField] private bool generateAll;

        [Header("World Space Data")]
        [SerializeField] private List<Transform> entranceMarkers;
        [SerializeField] private List<GameObject> allMarkers;
        [SerializeField] private List<LocationData> locationData;
        [SerializeField] private GameObject locationPrefab;

        [SerializeField] private List<Faction> test_territoryFactions;

        [SerializeField] private int locations = 0;
        [SerializeField] private List<Location> _locations;
        [SerializeField] private List<Territory> _territories;

        [SerializeField] private GameObject _temp_TerrainSurfacePrefab;
        [SerializeField] private List<GameObject> alltunnelMesheParents;


        public Dictionary<HexagonSide, List<Vector3>> terrainVertexIndiciesBySide { get; private set; }
        [SerializeField] private Dictionary<HexagonSide, List<Vector3>> neighborTerrainVertexIndiciesBySide;


        // public void GetTerrainVertexIndiciesBySide(int size = 108)
        // {
        //     Dictionary<HexagonSide, List<Vector3>> _terrainVertexIndiciesBySide = new Dictionary<HexagonSide, List<Vector3>>(){
        //         {HexagonSide.Front, new List<Vector3>()},
        //         {HexagonSide.FrontLeft, new List<Vector3>()},
        //         {HexagonSide.FrontRight, new List<Vector3>()},
        //         {HexagonSide.Back, new List<Vector3>()},
        //         {HexagonSide.BackLeft, new List<Vector3>()},
        //         {HexagonSide.BackRight, new List<Vector3>()},
        //     };

        //     Vector3[] corners = ProceduralTerrainUtility.GenerateHexagonPoints(CenterPosition(), 108);

        //     for (int x = 0; x < vertexGrid.GetLength(0); x++)
        //     {
        //         for (int z = 0; z < vertexGrid.GetLength(1); z++)
        //         {
        //             Vector3 vertexPos = vertexGrid[x, z].position;

        //             // if (VectorUtil.DistanceXZ(vertexPos, CenterPosition()) < size / 2f) continue;

        //             for (int side = 0; side < 6; side++)
        //             {
        //                 (int cornerA, int cornerB) = HexagonCellPrototype.GetCornersFromSide_Condensed((HexagonSide)side);
        //                 Vector3 corner_A = new Vector3(corners[(int)cornerA].x, corners[(int)cornerA].y, corners[(int)cornerA].z);
        //                 Vector3 corner_B = new Vector3(corners[(int)cornerB].x, corners[(int)cornerB].y, corners[(int)cornerB].z);

        //                 if (VectorUtil.IsPointOnLineXZ(vertexPos, corner_A, corner_B))
        //                 {
        //                     _terrainVertexIndiciesBySide[(HexagonSide)side].Add(vertexPos);
        //                     // _terrainVertexIndiciesBySide[(HexagonSide)side].Add(transform.TransformVector(vertexPos));
        //                     vertexGrid[x, z].isMeshEdge = true;
        //                     break;
        //                 }
        //             }
        //         }
        //     }
        //     terrainVertexIndiciesBySide = _terrainVertexIndiciesBySide;
        // }

        // [SerializeField] private Dictionary<Vector2, List<(int, int)>> terrainVertexSurfacesIndiciesByCellXZCenter;

        // public List<Mesh> GenerateMeshesFromTerrainVertexSurfaceCenters(Dictionary<Vector2, List<TerrainVertex>> vertexSurfaceSetsByXZPos)
        // {
        //     List<Mesh> surfaceMeshes = new List<Mesh>();

        //     foreach (var kvp in vertexSurfaceSetsByXZPos)
        //     {
        //         Vector3 pos = new Vector3(kvp.Key.x, 0, kvp.Key.y);
        //         Vector3[,] rawVertexGrid2D = HexagonCellPrototype.GenerateRectangularGrid(pos, 20, 20);
        //         List<Vector3> finalVertexPositions = UpdateVertexElevations(rawVertexGrid2D);

        //         // Create a new mesh to represent the current surface
        //         Mesh surfaceMesh = new Mesh();

        //         surfaceMesh.vertices = finalVertexPositions.ToArray();
        //         surfaceMesh.triangles = ProceduralTerrainUtility.GenerateTerrainTriangles(rawVertexGrid2D);
        //         surfaceMesh.uv = ProceduralTerrainUtility.GenerateTerrainUVs(rawVertexGrid2D);

        //         // Recalculate normals and bounds for the surface mesh
        //         surfaceMesh.RecalculateNormals();
        //         surfaceMesh.RecalculateBounds();

        //         surfaceMeshes.Add(surfaceMesh);
        //     }
        //     return surfaceMeshes;
        // }
        // private void GenerateMeshesFromTerrainVertexSurfaces()
        // {
        //     if (_temp_TerrainSurfacePrefab == null)
        //     {
        //         Debug.LogError("_temp_TerrainSurfacePrefab is null");
        //         return;
        //     }

        //     if (terrainVertexSurfacesIndiciesByCellXZCenter == null)
        //     {
        //         Debug.LogError("terrainVertexSurfacesIndiciesByCellXZCenter is null");
        //         return;
        //     }
        //     // List<List<Vector3>> surfaces = GetTerrainVertexSurfaces(terrainVertexSurfacesIndiciesByCellXZCenter, vertexGrid);
        //     Dictionary<Vector2, List<TerrainVertex>> surfaces = TerrainVertexUtil.GetTerrainVertexSurfacesByXZPos_TVert(terrainVertexSurfacesIndiciesByCellXZCenter, vertexGrid);
        //     // Dictionary<Vector2, List<Vector3>> surfaces = GetTerrainVertexSurfacesByXZPos(terrainVertexSurfacesIndiciesByCellXZCenter, vertexGrid);

        //     // List<Mesh> tunnelMeshes = HexagonCellPrototype.MeshGenerator.GenerateMeshesFromTerrainVertexSurfaces(surfaces);

        //     List<Mesh> surfaceMeshes = GenerateMeshesFromTerrainVertexSurfaceCenters(surfaces);

        //     if (surfaceMeshes != null && _temp_TerrainSurfacePrefab != null)
        //     {
        //         Transform surfaceMeshesParent = new GameObject("WorldSpace Surfaces").transform;

        //         foreach (var item in surfaceMeshes)
        //         {
        //             if (item != null)
        //             {
        //                 GameObject go = MeshUtil.InstantiatePrefabWithMesh(_temp_TerrainSurfacePrefab, item, transform.position);
        //                 go.transform.SetParent(surfaceMeshesParent);
        //             }
        //         }
        //     }
        // }

        private void EvaluateTerritories()
        {
            if (_territories.Count > 0 && test_territoryFactions.Count > 0)
            {
                int factionIX = 0;
                foreach (Territory territory in _territories)
                {
                    territory.UpdateOwner(test_territoryFactions[factionIX % test_territoryFactions.Count]);
                    factionIX++;
                    // territory.UpdateOwner(test_territoryFactions[UnityEngine.Random.Range(0, test_territoryFactions.Count)]);
                }
                Debug.Log("Randomly Set Territory Owners!");
            }
        }

        private void InitialSetup()
        {
            if (Uid == null) Uid = GetInstanceID().ToString();

            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            // if (isManaged == false) fastNoiseUnity = GetComponent<FastNoiseUnity>();

            cellManager = GetComponent<HexagonCellManager>();
            navMeshSurface = GetComponent<NavMeshSurface>();
            mapDisplay = GetComponent<MapDisplay>();

            wfc = GetComponent<IWFCSystem>();

            if (!enableEditMode) return;

            if (wfc == null) Debug.LogError("Missing WFC system component!");

            if (cellManager != null)
            {
                if (hexagonCell != null) cellManager.SetRadius(hexagonCell.size);
            }

            // if (allowTunnels && _tunnelMeshParent == null)
            // {
            //     _tunnelMeshParent = Instantiate(tunnelMeshParentPrefab, transform.position, Quaternion.identity);
            // }

            if (!mesh || !meshFilter.sharedMesh)
            {
                mesh = new Mesh();
                mesh.name = "WorldArea Terrain";
                meshFilter.sharedMesh = mesh;
                meshCollider.sharedMesh = mesh;
            }
        }


        public void SetupGridPreset_Outpost()
        {
            List<HexagonCellPrototype> presetPath = cellManager.Generate_GridPreset_Outpost_WPath(vertexGrid, cellVertexSearchRadiusMult);

            List<HexagonCellCluster> clusters = cellManager.GetPrototypeClustersOfType(CellClusterType.Outpost);

            locationData = LocationData.GenerateLocationDataFromClusters(clusters, LocationType.Outpost, new LocationMarkerPrefabOption());

            locations = locationData.Count;

            foreach (HexagonCellCluster cluster in clusters)
            {
                foreach (HexagonCellPrototype parentMember in cluster.prototypes)
                {
                    HexGridVertexUtil.AssignParentCellVerticesToChildCells(parentMember, vertexGrid);
                }

                cluster.GetMemberChildEdgesOnNeighborBorder(vertexGrid);
                // cellManager.SlopeElevationWithinCluster(cluster, vertexGrid);
            }

            // GetLocationBorderPointsFromGridEdgeVertices(locationData, vertexGrid);
        }


        public static void FormatCellGridVertexFoundation(
            Dictionary<int, List<HexagonCellPrototype>> gridCellPrototypesByLayer,
            TerrainVertex[,] vertexGrid,
            HexagonGrid hexGrid,
            Transform transform,
            float viableFlatGroundCellSteepnessThreshhold,
            float cellVertexSearchRadiusMult = 1.3f,
            float groundSlopeElevationStep = 0.45f,
            float seaLevel = 0f
        )
        {
            if (gridCellPrototypesByLayer == null)
            {
                Debug.LogError("gridCellPrototypesByLayer is null");
                return;
            }

            if (vertexGrid == null)
            {
                Debug.LogError("vertexGrid is null");
                return;
            }

            // int topLayer = gridCellPrototypesByLayer.Keys.Count - 1;
            // HexGridVertexUtil.AssignTerrainVerticesToPrototypes(gridCellPrototypesByLayer[topLayer], vertexGrid, null, false, true, false, cellVertexSearchRadiusMult);

            int cellLayerElevation = 3;
            HexGridVertexUtil.AssignTerrainVerticesToPrototypeGrid(
                gridCellPrototypesByLayer,
                vertexGrid,
                hexGrid,
                null,
                cellLayerElevation,
                viableFlatGroundCellSteepnessThreshhold,
                true,
                false,
                cellVertexSearchRadiusMult,
                seaLevel
            );


            // HexGridVertexUtil.GroundPrototypesToTerrainVertexElevation(gridCellPrototypesByLayer, vertexGrid, null, 0.1f, viableFlatGroundCellSteepnessThreshhold);

            // HexGridVertexUtil.AssignTerrainVerticesToGroundPrototypes_V2(gridCellPrototypesByLayer, vertexGrid, viableFlatGroundCellSteepnessThreshhold, transform);

            // HexGridVertexUtil.GetViableCellsForFlatGroundTerrain(gridCellPrototypesByLayer[topLayer], vertexGrid, viableFlatGroundCellSteepnessThreshhold, false);

            // HexagonCellPrototype.CleanupCellIslandLayerPrototypes(gridCellPrototypesByLayer, 3);
            // HexGridVertexUtil.AssignTerrainVerticesToGroundPrototypes(gridCellPrototypesByLayer, vertexGrid, cellVertexSearchRadiusMult);

            // HexagonCellPrototype.CleanupCellIslandLayerPrototypes(gridCellPrototypesByLayer, 3);

            // HexGridVertexUtil.AssignRampsForIslandLayerPrototypes(gridCellPrototypesByLayer, vertexGrid, 4, groundSlopeElevationStep);

            // TerrainVertexUtil.SmoothElevationAroundCellGrid(vertexGrid, 3, 1, 2);


            // Debug.Log("FormatCellGridVertexFoundation - Complete");
            // if (terrainVertexSurfacesIndiciesByCellXZCenter == null)
            // {
            //     terrainVertexSurfacesIndiciesByCellXZCenter = HexGridVertexUtil.AssignTerrainVerticesToSurfaceMapsByPrototype(
            //             gridCellPrototypesByLayer[topLayer]
            //             , vertexGrid);
            // }
        }


        public void SetupCellGridState(bool updateGroundMesh = true)
        {
            if (vertexGrid == null)
            {
                Debug.LogError("vertexGrid is null");
                return;
            }
            if (cellManager == null)
            {
                Debug.LogError("cellManager is null");
                return;
            }

            float viableFlatGroundCellSteepnessThreshhold = 2f;

            FormatCellGridVertexFoundation(
                cellManager.cellPrototypesByLayer_V2,
                vertexGrid,
                null,
                transform,
                viableFlatGroundCellSteepnessThreshhold,
                cellVertexSearchRadiusMult,
                groundSlopeElevationStep
            );

            // Inital WorldArea Path
            List<HexagonCellPrototype> mainWorldAreaPath = cellManager.Setup_WorldAreaGrid_MainPath();

            //
            List<HexagonCellPrototype> gridPath = new List<HexagonCellPrototype>();
            if (cellManager.GetGridPreset() == GridPreset.Outpost)
            {
                SetupGridPreset_Outpost();
            }
            else
            {
                gridPath = cellManager.Generate_GridPreset_City_WPath();
            }


            // HexagonCellPrototype.GroundPrototypesToTerrainVertexElevation(cellManager.cellPrototypesByLayer_V2, vertexGrid);
            // cellManager.GenerateMicroGridFromHosts(path, 4, 3);

            cellManager.GenerateMicroGridFromClusters(3, true);

            int cylces = 2;
            do
            {
                if (mainWorldAreaPath != null && mainWorldAreaPath.Count > 0)
                {
                    HexGridVertexUtil.AssignPathCenterVertices(mainWorldAreaPath, vertexGrid);
                    HexGridVertexUtil.SmoothVertexElevationAlongPath(mainWorldAreaPath, vertexGrid);

                    // HexGridVertexUtil.SmoothElevationAlongPathNeighbors(path, vertexGrid);
                }

                if (gridPath != null && gridPath.Count > 0)
                {
                    HexGridVertexUtil.AssignPathCenterVertices(gridPath, vertexGrid);
                    HexGridVertexUtil.SmoothVertexElevationAlongPath(gridPath, vertexGrid);

                    // HexGridVertexUtil.SmoothElevationAlongPathNeighbors(path, vertexGrid);
                }

                cylces--;
            } while (cylces > 0);



            // HexGridVertexUtil.GetGridEdgeVertexIndices(
            //     cellManager.GetAllPrototypesOfCellStatus(CellStatus.Ground).FindAll(p => p.isEdge && p.IsRemoved() == false),
            //     vertexGrid, gridEdgeSmoothingRadius);


            // HexGridVertexUtil.SmoothGridEdgeVertexList(
            //     cellManager.GetAllPrototypesOfCellStatus(CellStatus.Ground).FindAll(p => p.isEdge && p.IsRemoved() == false),
            //     vertexGrid,
            //     cellVertexSearchRadiusMult);


            // HexagonCellPrototype.GetGridEdgeVertexIndices(
            //     HexagonCellPrototype.GetEdgeCornersOfEdgePrototypes(
            //         cellManager.GetAllPrototypesOfCellStatus(CellStatus.Ground).FindAll(p => p.isEdge && p.IsRemoved() == false)
            //     ),
            //     vertexGrid
            // );

            // ReadjustVerticesAroundTunnelClusters(cellManager.GetPrototypeClustersOfType(CellClusterType.Tunnel), vertexGrid, cellManager.GetCellInLayerElevation(), 2, null);

            if (updateGroundMesh)
            {
                Generate_TerrainMesh();
                // cellManager.CreateTunnelMeshFromClusters(alltunnelMesheParents);


                // GenerateMeshesFromTerrainVertexSurfaces();
                // UpdateTerrainTexture();
                debug_vertexRemapDoOnce = true;
            }
        }




        public void Revaluate()
        {
            trigger_revaluate = true;
            OnValidate();
        }

        public void ResetWorldState()
        {
            ResetTerrainVertexGrid();
            if (cellManager != null) cellManager.ResetPrototypes();

            if (Handle_ResetGridState(true) == false)
            {
                Generate_TerrainMesh();
                debug_vertexRemapDoOnce = true;
            }
        }

        public bool Handle_ResetGridState(bool force)
        {
            if (force || trigger_resetGridState)
            {
                trigger_resetGridState = false;

                if (!debug_disableCellManager &&
                    !Application.isPlaying &&
                    vertexGrid != null &&
                    cellManager != null &&
                    cellManager.cellPrototypesByLayer_V2 != null
                )
                {
                    SetupCellGridState(true);
                    return true;

                }
            }
            return false;
        }

        public void Handle_UpdateTerrainMesh()
        {
            if (trigger_resetTerrain)
            {
                trigger_resetTerrain = false;

                if (!debug_disableCellManager &&
                    !Application.isPlaying &&
                    vertexGrid != null
                )
                {
                    Generate_TerrainMesh();
                }
            }
        }

        private void OnValidate()
        {
            InitialSetup();
            _editorUpdate = true;
            bool resetWorldState = false;

            if (
                trigger_resetTerrain ||
                trigger_revaluate ||
                _position != CenterPosition() ||
                _terrainHeight != terrainHeight ||
                areaSize != _areaSize
            )
            {
                if (areaSize % 2 != 0) areaSize += 1;
                _areaSize = areaSize;

                _position = CenterPosition();
                _terrainHeight = terrainHeight;
                trigger_revaluate = false;

                resetWorldState = true;
            }

            if (resetWorldState)
            {
                ResetWorldState();
            }

            Handle_UpdateTerrainMesh();

            Handle_ResetGridState(false);

            // if (_gridEdgeSmoothingRadius != gridEdgeSmoothingRadius ||
            //     _smoothingFactor != smoothingFactor ||
            //     _smoothingSigma != smoothingSigma)
            // {
            //     _smoothingFactor = smoothingFactor;
            //     _smoothingSigma = smoothingSigma;
            //     _gridEdgeSmoothingRadius = gridEdgeSmoothingRadius;

            //     if ((cellManager != null) && cellManager.cellPrototypesByLayer_V2 != null)
            //     {

            //         // HexGridVertexUtil.SmoothGridEdgeVertexIndices(
            //         //     cellManager.GetAllPrototypesOfCellStatus(CellStatus.Ground).FindAll(p => p.isEdge && p.IsRemoved() == false),
            //         //     vertexGrid, cellVertexSearchRadiusMult, gridEdgeSmoothingRadius, smoothingFactor, smoothingSigma);

            //         Generate_TerrainMesh();
            //         debug_vertexRemapDoOnce = true;
            //     }
            // }

            if (updateTerrainTexture)
            {
                updateTerrainTexture = false;

                UpdateTerrainTexture();
            }

            if (generatePlacedObjects)
            {
                generatePlacedObjects = false;

                if (placeObjectPrefab != null) PlaceObjects(vertexGrid, placeObjectPrefab, placement_density, fastNoiseUnity.fastNoise);
            }

            Handle_SaveMesh();
        }

        private float updateTime = 1f, timer;
        private void Update()
        {
            if (Application.isPlaying) return;

            if (!enableEditMode) return;

            if (!useTerrainGenerator || !enableTerrainEditMode || !enableEditMode) return;

            if (!_editorUpdate && timer > 0f)
            {
                timer -= Time.fixedDeltaTime;
                return;
            }
            timer = updateTime;

            if (!debug_editorUpdateTerrainOnce || _editorUpdate)
            {
                _editorUpdate = false;

                if (generateTerrain)
                {
                    generateTerrain = false;
                    Generate_TerrainMesh();
                    if (generateAll) GenerateWorldAreaStructure(Application.isPlaying == false);
                }
            }
        }

        private void Awake()
        {
            CreationDate = DateTime.Now;
            Debug.Log("CreationDate: " + CreationDate);

            InitialSetup();
        }

        private void Start()
        {
            if (Application.isPlaying == false) return;

            if (!enableEditMode) return;

            InitialSetup();

            _doOnce = false;

            GenerateWorldAreaStructure();
        }


        public void GenerateWorldAreaStructure(bool force = false, bool resetVertexGrid = false)
        {
            generateAll = false;

            if (_doOnce && !force) return;
            _doOnce = true;

            Debug.Log("Generate WorldArea Structure!");

            if (resetVertexGrid) ResetTerrainVertexGrid();

            // Setup Cells
            cellManager.GeneratePrototypeGridsByLayer(hexagonCell);

            SetupCellGridState(true);

            if (cellManager.cellPrototypesByLayer_V2 == null)
            {
                Debug.LogError("EMPTY cellPrototypesByLayer_V2!");

            }
            else
            {
                cellManager.GenerateCells(true, false);

                (
                    Dictionary<int, List<HexagonCell>> _allCellsByLayer,
                    List<HexagonCell> _allCells, Dictionary<HexagonCellCluster,
                    Dictionary<int, List<HexagonCell>>> _allCellsByLayer_X4_ByCluster
                ) = cellManager.GetCellsSet();

                // Add cells to WFC
                wfc.SetRadius(cellManager.GetRadius());
                wfc.AssignCells(_allCellsByLayer, _allCells, _allCellsByLayer_X4_ByCluster);

                // Run WFC
                wfc.ExecuteWFC();
            }



            UpdateTerrainTexture();

            if (placeObjectPrefab != null) PlaceObjects(vertexGrid, placeObjectPrefab, placement_density, fastNoiseUnity.fastNoise);

            if (navMeshSurface != null) navMeshSurface.BuildNavMesh();

            // Setup Locations
            if (locationData != null && locationData.Count > 0)
            {
                // GetLocationBorderPointsFromGridEdgeVertices(locationData, vertexGrid);
                List<GameObject> newLocations = SpawnLocations(locationData, locationPrefab);
                if (newLocations.Count > 0)
                {
                    _locations = new List<Location>();
                    _territories = new List<Territory>();
                    foreach (var item in newLocations)
                    {
                        Location newLoc = item.GetComponent<Location>();
                        Territory newTerr = item.GetComponent<Territory>();
                        if (newLoc != null)
                        {
                            newLoc.SetWorldArea(this);
                            newLoc.Revaluate();
                            _locations.Add(newLoc);
                        }
                        if (newTerr != null) _territories.Add(newTerr);
                    }
                }

                Invoke("EvaluateTerritories", 0.6f);
            }

            // allMarkers = UtilityHelpers.FindGameObjectsWithTagInChildren(this.transform, "Marker");
            // if (allMarkers == null || allMarkers.Count == 0)
            // {
            //     Debug.LogError("No markers found among children!");
            // }
            // else
            // {
            //     Debug.Log("Markers found: " + allMarkers.Count);
            // }

            // SetupMarkers(); 
            // DisableCells();
        }

        public void CreatePathCellMicroGrid()
        {
            if (tilePrefabs_MicroClusterParent == null)
            {
                Debug.LogError("No prefab for tilePrefabs_MicroClusterParent!");
                return;
            }
            // Get all path cells from cellManager
            List<HexagonCell> pathCells = cellManager.GetAllCellsList().FindAll(c => c.isPathCell);

            if (pathCells == null || pathCells.Count == 0)
            {
                Debug.LogError("No cells found!");
                return;
            }
            // assign one as parent cell
            // Instantiate microGrid tile and initialize with path cells
            (HexagonCellManager parentCellManager, List<HexagonCell> pathClusterCells) = WFCUtilities.SetupMicroCellClusterFromHosts(pathCells, tilePrefabs_MicroClusterParent, 2, 4, this.transform);
            cellPathMicroGridManager = parentCellManager;
        }

        private static List<GameObject> SpawnLocations(List<LocationData> locationData, GameObject locationPrefab, Transform parentFolder = null)
        {
            // if (locationPrefab == null || locationData == null) return;
            List<GameObject> newLocations = new List<GameObject>();

            Transform parent = new GameObject("Locations").transform;
            foreach (LocationData data in locationData)
            {

                GameObject go = Instantiate(locationPrefab, data.centerPosition, Quaternion.identity);
                go.transform.SetParent(parent);

                Location location = go.GetComponent<Location>();
                location.SetLocationData(data);
                // location.Revaluate();
                newLocations.Add(go);
            }

            if (parentFolder != null) parent.SetParent(parentFolder);

            return newLocations;
        }

        public static void GetLocationBorderPointsFromGridEdgeVertices(List<LocationData> locationData, TerrainVertex[,] vertexGrid)
        {
            // Vector2 vertexPosXZ = new Vector2(currentVertex.position.x, currentVertex.position.z);
            foreach (LocationData locData in locationData)
            {
                List<Vector3> newBorderPoints = new List<Vector3>();

                for (int x = 0; x < vertexGrid.GetLength(0); x++)
                {
                    for (int z = 0; z < vertexGrid.GetLength(1); z++)
                    {
                        TerrainVertex currentVertex = vertexGrid[x, z];
                        if (currentVertex.isOnTheEdgeOftheGrid || currentVertex.type == VertexType.Road)
                        {
                            // Vector2 currentPosXZ = new Vector2(edge.center.x, edge.center.z);
                            float dist = Vector3.Distance(locData.centerPosition, currentVertex.position);
                            if (dist < locData.radius * 1.5f)
                            {
                                newBorderPoints.Add(currentVertex.position);
                            }
                        }
                    }
                }

                if (newBorderPoints.Count > 0) locData.borderPoints = newBorderPoints.ToArray();
            }
        }


        #region Terrain Generation

        public void ResetTerrainVertexGrid()
        {
            if (trigger_resetTerrain == false && worldAreaManager != null && vertexGrid != null)
            {
                Debug.Log("Skip reset for vertexGrid: " + vertexGrid.GetLength(0));
                return;
            }

            Vector3 centerPos = CenterPosition();

            #region Hex approach
            List<Vector3> rectangleCorners = VectorUtil.HexagonCornersToRectangle(centerPos, 108);

            Vector2 worldspaceCoordinate;

            if (hexagonCell != null)
            {
                worldspaceCoordinate = hexagonCell.GetLookup();
            }
            else
            {
                worldspaceCoordinate = HexCoreUtil.Calculate_CenterLookup(transform.position, areaSize);
                Debug.LogError(gameObject.name + " worldspaceCoordinate: " + worldspaceCoordinate);
            }

            vertexGrid = TerrainVertexUtil.GenerateRectangleGridInHexagon(centerPos,
                                108,
                                rectangleCorners[2],
                                rectangleCorners[3],
                                rectangleCorners[0],
                                rectangleCorners[1],
                                transform,
                                worldspaceCoordinate,
                                vertexDensity,
                                5
                            );

            // #region Rectangle approach
            // List<Vector3> rectangleCorners = VectorUtil.HexagonCornersToRectangleCorner(centerPos, 108);
            // vertexGrid = TerrainVertexUtil.GenerateRectangleGrid(rectangleCorners[2], rectangleCorners[3], rectangleCorners[0], rectangleCorners[1], transform, 100);


            // #endregion

            // List<Vector3> rectangleCorners = VectorUtil.HexagonCornersToRectangleCorners(transform.position, 108);

            // Debug.Log("neighborTerrainEdgeVertices: " + neighborTerrainEdgeVertices.Count);
            // vertexGrid = TerrainVertexUtil.GenerateVertexGridInHexagon(centerPos, 108, rectangleCorners[2], rectangleCorners[3], rectangleCorners[0], rectangleCorners[1],
            //     neighborTerrainEdgeVertices,
            //     100
            // );
            // GetTerrainVertexIndiciesBySide(108);

            // vertexGrid = useStepSize ? TerrainVertexUtil.GenerateVertexGrid(gameObject.transform.position, areaSize, stepSize)
            //                             : TerrainVertexUtil.GenerateVertexGrid(gameObject.transform.position, areaSize);
        }

        public void Generate_TerrainMesh()
        {
            if (fastNoiseUnity == null) return;

            UpdateTerrainNoiseVertexElevations();

            int cylces = 1;
            do
            {
                HexGridVertexUtil.SmoothGridEdgeVertexList(
                    vertexGrid,
                    smoothingNeighborDepth,
                    smoothingFactor,
                    cellGridVertexWeight,
                    cellGridVertexNeighborEvaluationDepth
                );
                // TerrainVertexUtil.SmoothElevationAroundCellGrid(vertexGrid, smoothingNeighborDepth, smoothingFactor, cellGridVertexWeight);

                cylces--;
            } while (cylces > 0);

            // HexGridVertexUtil.SmoothGridEdgeVertexIndices(
            //     cellManager.GetAllPrototypesOfCellStatus(CellStatus.Ground).FindAll(p => p.isEdge && p.IsRemoved() == false),
            //     vertexGrid, cellVertexSearchRadiusMult, gridEdgeSmoothingRadius, smoothingFactor, smoothingSigma);


            (Vector3[] vertexPositions, HashSet<(int, int)> meshTraingleExcludeList) = TerrainVertexUtil.ExtractFinalVertexWorldPositions(vertexGrid, transform);
            // Vector3[] vertexPositions = positions.ToArray();

            // Vector3[] vertexPositions = UpdateVertexElevations().ToArray();
            if (vertexGrid == null || vertexPositions.Length == 0)
            {
                Debug.LogError("Null vertexGrid or no vertexPositions");
                return;
            }
            // Debug.Log("GenerateMesh, vertexPositions: " + vertexPositions.Length);

            mesh.Clear();
            mesh.vertices = vertexPositions;
            mesh.triangles = ProceduralTerrainUtility.GenerateTerrainTriangles(vertexGrid, meshTraingleExcludeList);
            mesh.uv = ProceduralTerrainUtility.GenerateTerrainUVs(vertexGrid);

            RefreshTerrainMesh();
        }

        public void Generate_WorldAreaFoundation(List<TerrainVertex[,]> elderVertexGrids, List<HexagonCellPrototype> neighborCellGridEdges)
        {
            ResetTerrainVertexGrid();

            foreach (var elderGrid in elderVertexGrids)
            {
                vertexGrid = TerrainVertexUtil.MergeElderVertexData(vertexGrid, elderGrid, hexagonCell);
            }

            mesh = new Mesh();
            mesh.name = "World Space Mesh" + gameObject.name;
            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;

            UpdateTerrainNoiseVertexElevations();

            TerrainVertexUtil.SmoothWorldAreaVertexElevationTowardsCenter(vertexGrid, CenterPosition(), true);

            cellManager.GeneratePrototypeGridsByLayer(hexagonCell);

            if (neighborCellGridEdges != null)
            {
                cellManager.EvaluateNewPrototypeGridEdgeNeihgbors(neighborCellGridEdges);
            }

            SetupCellGridState(true);

            Generate_TerrainMesh();
        }

        private List<Vector3> UpdateTerrainNoiseVertexElevations()
        {
            List<TerrainVertex> newAccessibleVertices = new List<TerrainVertex>();
            List<Vector3> positions = new List<Vector3>();

            HashSet<(int, int)> _meshTraingleExcludeList = new HashSet<(int, int)>();

            if (vertexGrid != null)
            {
                for (int x = 0; x < vertexGrid.GetLength(0); x++)
                {
                    for (int z = 0; z < vertexGrid.GetLength(1); z++)
                    {
                        TerrainVertex currentVertex = vertexGrid[x, z];

                        if (currentVertex.markedForRemoval)
                        {
                            _meshTraingleExcludeList.Add((x, z));
                            // positions.Add(transform.InverseTransformPoint(vertexGrid[x, z].position));

                            // positions.Add(vertexGrid[x, z].position);
                        }

                        // else if (vertexGrid[x, z].markedIgnore || vertexGrid[x, z].isMeshEdge)
                        // {
                        //     // positions.Add(transform.InverseTransformPoint(vertexGrid[x, z].position));

                        //     // positions.Add(vertexGrid[x, z].position);
                        // }
                        // else
                        // {

                        int posX = (int)currentVertex.noiseCoordinate.x;
                        int posZ = (int)currentVertex.noiseCoordinate.y;
                        // Debug.Log("coordinates: X: " + posX + ", Y: " + posZ);

                        float baseNoiseHeight = CalculateNoiseHeightForVertex(posX, posZ, terrainHeight, noiseType, fastNoiseUnity.fastNoise, persistence, octaves, noiseScale, lacunarity);
                        baseNoiseHeight += transform.position.y;

                        if (vertexGrid[x, z].isOnTunnelCell && !vertexGrid[x, z].isOnTunnelGroundEntry
                            && vertexGrid[x, z].type != VertexType.Cell
                            && vertexGrid[x, z].type != VertexType.Road
                            && vertexGrid[x, z].isOnTheEdgeOftheGrid == false
                        )
                        {
                            vertexGrid[x, z].position.y = baseNoiseHeight;
                            vertexGrid[x, z].baseNoiseHeight = vertexGrid[x, z].position.y;

                            if (vertexGrid[x, z].baseNoiseHeight < vertexGrid[x, z].tunnelCellRoofPosY)
                            {
                                float modY = vertexGrid[x, z].tunnelCellRoofPosY; // Mathf.Lerp(baseNoiseHeight, vertexGrid[x, z].tunnelCellRoofPosY, 0.9f);
                                vertexGrid[x, z].position.y = modY;
                                vertexGrid[x, z].baseNoiseHeight = vertexGrid[x, z].position.y;
                            }
                        }
                        else
                        {
                            if (!currentVertex.isOnTheEdgeOftheGrid && !currentVertex.isInherited && !currentVertex.ignoreSmooth && (currentVertex.type == VertexType.Generic || vertexGrid[x, z].type == VertexType.Unset))
                            {
                                // float baseNoiseHeight = CalculateNoiseHeightForVertex(x, z, terrainHeight, noiseType, fastNoiseUnity.fastNoise, persistence, octaves, noiseScale, lacunarity);
                                vertexGrid[x, z].position.y = baseNoiseHeight;
                                vertexGrid[x, z].baseNoiseHeight = vertexGrid[x, z].position.y;
                                newAccessibleVertices.Add(vertexGrid[x, z]);
                            }
                        }
                        // }

                        // vertexGrid[x, z].position = transform.TransformPoint(vertexGrid[x, z].position);

                        // Vector3 worldPosition = meshFilter.gameObject.transform.InverseTransformPoint(vertexGrid[x, z].position);
                        // positions.Add(vertexGrid[x, z].position);
                        // positions.Add(transform.InverseTransformVector(vertexGrid[x, z].position));

                        if (UtilityHelpers.Vector3HasNaN(transform.InverseTransformPoint(vertexGrid[x, z].position)))
                        {
                            Debug.LogError("NaN detected in vertex");

                        }
                        positions.Add(transform.InverseTransformPoint(vertexGrid[x, z].position));
                    }
                }
            }

            meshTraingleExcludeList = _meshTraingleExcludeList;

            accessibleVertices = newAccessibleVertices;

            debug_vertexRemapDoOnce = false;
            return positions;
        }

        public void SetMesh(Mesh newMesh)
        {
            mesh = newMesh;
            RefreshTerrainMesh();
            _editorUpdate = true;
        }

        // public void GenerateGroundMesh()
        // {
        //     if (cellManager == null || vertexGrid == null) return;
        //     List<HexagonCell> groundCells = cellManager.GetAllCellsList().FindAll(c => c.isLeveledGroundCell || (c.GetLayer() == 0 && c.isLeveledEdge || !c.isLeveledCell));

        //     foreach (var item in groundCells)
        //     {
        //         item._vertexIndices.Clear();
        //     }

        //     HexagonCell.AssignVerticesToCells(vertexGrid, groundCells);

        //     List<HexagonCell> path = cellManager.GetAllCellsList().FindAll(c => c.isPathCell || c.isEntryCell);

        //     HexagonCell.SmoothElevationAlongPath(path, vertexGrid);
        //     HexagonCell.SmoothElevationAlongPathNeighbors(path.FindAll(c => c.isEntryCell == false), vertexGrid);

        //     // SmoothTerrainVerticesToCellGrid();

        //     MeshUtil.CreateMeshFromVertices(vertexGrid, meshFilter);
        //     RefreshTerrainMesh();
        // }

        public void RefreshTerrainMesh()
        {
            // mesh = meshFilter.sharedMesh;
            // Recalculate the normals to ensure proper lighting
            mesh.RecalculateNormals();
            // Apply the mesh data to the MeshFilter component
            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;
        }

        #endregion

        private void UpdateCellGridElevation()
        {
            float avgElevation = CalculateAverageElevation(vertexGrid, transform);
            cellManager.CalculateCenterElevationOffset(avgElevation);
            // Debug.Log("avgElevation: " + avgElevation + ", initialGridHeight: " + initialGridHeight);
        }



        private void UpdateTerrainTexture()
        {
            MapData mapData = GenerateMapData(vertexGrid, terrainTypes, pathTypes);

            // (Texture2D texture2D, Color[] colorMap) = TextureGenerator.TextureFromMaterialMap(mapData.materialMap, vertexGrid.GetLength(0), vertexGrid.GetLength(1), mesh, meshRenderer);
            // Texture2D texture2D = TextureGenerator.CombineMaterials(mapData.materialMap, vertexGrid.GetLength(0), vertexGrid.GetLength(1));
            Texture2D texture2D = TextureGenerator.TextureFromColourMap(mapData.colourMap,
                vertexGrid.GetLength(0),
                vertexGrid.GetLength(1));

            mapDisplay.DrawTexture(texture2D);
        }


        #region Save Mesh

        [SerializeField] private bool bSaveMesh = false;
        [SerializeField] private Mesh lastSavedMesh;
        private void Handle_SaveMesh()
        {
            if (bSaveMesh)
            {
                bSaveMesh = false;
                if (mesh != null) SaveMesh();
                //  SaveMeshAsset(mesh, "New World Area Mesh");
            }
        }

        void SaveMeshAsset(Mesh mesh, string assetName)
        {
            // Create a new mesh asset
            lastSavedMesh = Instantiate(mesh) as Mesh;
            lastSavedMesh.name = assetName;

            // Save the mesh asset to the project
            AssetDatabase.CreateAsset(lastSavedMesh, "Assets/Terrain/" + assetName + ".asset");
            AssetDatabase.SaveAssets();
        }

        public void SaveMesh()
        {
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                Debug.LogError("No mesh found to save.");
                return;
            }

            // Get current date and time
            DateTime now = DateTime.Now;
            // Append time to mesh name
            string meshName = "New_Worldspace_Mesh_" + now.ToString("yyyyMMdd_HHmmss");

            string path = EditorUtility.SaveFilePanel("Save Mesh", "Assets/Terrain/", meshName, "asset");

            if (string.IsNullOrEmpty(path))
                return;

            // Make sure the path is relative to the project folder
            path = FileUtil.GetProjectRelativePath(path);

            // Clone the mesh to prevent modifying the original mesh
            Mesh meshToSave = Mesh.Instantiate(meshFilter.sharedMesh);
            lastSavedMesh = meshToSave;

            // Optimize the mesh to reduce its size
            MeshUtility.Optimize(meshToSave);

            // Save the mesh asset to the project folder
            AssetDatabase.CreateAsset(meshToSave, path);


            if (Application.isPlaying)
            {
                enableTerrainEditMode = false;

                if (enableTerrainEditMode)
                {

                }
            }


            Debug.Log("Mesh saved to " + path);
        }

        #endregion


        private List<Vector3> UpdateVertexElevations(Vector3[,] vertexGrid2D)
        {
            List<Vector3> finalPositions = new List<Vector3>();

            if (vertexGrid2D != null)
            {
                for (int x = 0; x < vertexGrid2D.GetLength(0); x++)
                {
                    for (int z = 0; z < vertexGrid2D.GetLength(1); z++)
                    {
                        int posX = (int)vertexGrid2D[x, z].x;
                        int posZ = (int)vertexGrid2D[x, z].z;
                        float baseNoiseHeight = CalculateNoiseHeightForVertex(posX, posZ, terrainHeight, noiseType, fastNoiseUnity.fastNoise, persistence, octaves, noiseScale, lacunarity);

                        vertexGrid2D[x, z].y = baseNoiseHeight;
                        vertexGrid2D[x, z] = transform.TransformPoint(vertexGrid2D[x, z]);


                        finalPositions.Add(transform.InverseTransformPoint(vertexGrid2D[x, z]));
                    }
                }
            }
            return finalPositions;
        }

        private static List<Vector3> UpdateVertexElevations(TerrainVertex[,] vertexGrid, MeshFilter meshFilter, NoiseType noiseType, FastNoise fastNoise, float terrainHeight, float persistence, float octaves, float noiseScale, float lacunarity)
        {
            List<Vector3> positions = new List<Vector3>();
            if (vertexGrid != null)
            {
                for (int x = 0; x < vertexGrid.GetLength(0); x++)
                {
                    for (int z = 0; z < vertexGrid.GetLength(1); z++)
                    {
                        if (vertexGrid[x, z].type == VertexType.Generic || vertexGrid[x, z].type == VertexType.Unset)
                        {
                            vertexGrid[x, z].position.y = CalculateNoiseHeightForVertex(x, z, terrainHeight, noiseType, fastNoise, persistence, octaves, noiseScale, lacunarity);
                        }
                        Vector3 worldPosition = meshFilter.gameObject.transform.InverseTransformPoint(vertexGrid[x, z].position);
                        positions.Add(vertexGrid[x, z].position);
                    }
                }
            }
            return positions;
        }
        public static float CalculateNoiseHeightForVertex(int indexX, int indexZ, float terrainHeight, NoiseType noiseType, FastNoise fastNoise, float persistence, float octaves, float noiseScale, float lacunarity)
        {
            float noiseHeight = GetNoiseHeightValue(indexX, indexZ, noiseType, fastNoise, persistence, octaves, noiseScale, lacunarity);
            float basePosY = noiseHeight * terrainHeight;
            return basePosY;
        }

        private static float GetNoiseHeightValue(float x, float z, NoiseType noiseType, FastNoise fastNoise, float persistence, float octaves, float noiseScale, float lacunarity)
        {
            // Calculate the height of the current point
            float noiseHeight = 0;
            float frequency = 1;
            float amplitude = 1;
            float sampleX = x / noiseScale * frequency;

            for (int i = 0; i < octaves; i++)
            {
                float sampleY = z / noiseScale * frequency;

                float noiseValue = 0;
                if (noiseType == NoiseType.Perlin)
                {
                    noiseValue = Mathf.PerlinNoise(sampleX, sampleY);
                }
                else if (noiseType == NoiseType.Simplex)
                {
                    noiseValue = (float)fastNoise.GetNoise(x, z);
                }

                noiseHeight += noiseValue * amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            return noiseHeight;
        }

        #endregion

        public static bool ShouldIgnoreVertexPosition(TerrainVertex vertex) => vertex.markedForRemoval || vertex.markedIgnore;

        public static float CalculateAverageElevation(TerrainVertex[,] vertices, Transform transform, bool ignoreNegativeY = true)
        {
            int count = 0;
            float totalElevation = 0f;

            for (int x = 0; x < vertices.GetLength(0); x++)
            {
                for (int z = 0; z < vertices.GetLength(1); z++)
                {
                    TerrainVertex vertex = vertices[x, z];
                    if (!ShouldIgnoreVertexPosition(vertex) && (!ignoreNegativeY || (ignoreNegativeY && vertex.position.y >= 0)))
                    {
                        totalElevation += vertices[x, z].position.y;
                        // totalElevation += transform.TransformPoint(vertices[x, z].position).y;
                        count++;
                    }
                }
            }

            if (count > 0)
            {
                return totalElevation / count;
            }
            else
            {
                return 0f; // or return a default value if no vertices found
            }
        }


        #region Procedural Placement
        public static void PlaceObjects(TerrainVertex[,] vertexGrid, GameObject prefab, float density, FastNoise fastNoise, Transform parent = null)
        {
            if (vertexGrid != null)
            {
                if (parent == null) parent = new GameObject("PlacedObjects").transform;

                for (int x = 0; x < vertexGrid.GetLength(0); x++)
                {
                    for (int z = 0; z < vertexGrid.GetLength(1); z++)
                    {
                        TerrainVertex vertex = vertexGrid[x, z];

                        if (vertex.markedForRemoval || vertex.markedIgnore || !vertex.isInHexBounds || vertex.type == VertexType.Road) continue;

                        if (vertex.type == VertexType.Generic || vertex.type == VertexType.Unset || vertex.isFlatGroundCell == false)
                        {
                            if (Fitness(x, z, fastNoise) > 1 - density)
                            {
                                // float noiseValue = (float)fastNoise.GetNoise(x, z);

                                // if (noiseValue > 1 - density)
                                // {
                                Vector3 pos = vertex.position;
                                pos.y -= 0.35f;

                                GameObject go = InstantiateObjectWithRandomRotation(prefab, pos);
                                // GameObject go = Instantiate(prefab, pos, Quaternion.identity);
                                go.transform.SetParent(parent);
                                // vertexGrid[x, z].position = transform.TransformPoint(vertexGrid[x, z].position);
                            }
                        }

                    }
                }
            }
        }

        public static void PlaceObjects(
            GameObject prefab,
            Dictionary<Vector2, TerrainVertex> globalTerrainVertexGridByCoordinate,
            Vector2[,] worldspaceVertexKeys,
            float density,
            FastNoise fastNoise,
            float seaLevel = 0,
            Transform parent = null
        )
        {
            if (worldspaceVertexKeys == null || worldspaceVertexKeys.GetLength(0) == 0)
            {
                Debug.LogError("NO worldspaceVertexKeys");
                return;
            }

            if (parent == null) parent = new GameObject("PlacedObjects").transform;

            for (int x = 0; x < worldspaceVertexKeys.GetLength(0); x++)
            {
                for (int z = 0; z < worldspaceVertexKeys.GetLength(1); z++)
                {
                    TerrainVertex vertex = globalTerrainVertexGridByCoordinate[worldspaceVertexKeys[x, z]];

                    if (vertex.markedForRemoval || vertex.isFlatGroundCell || vertex.markedIgnore || vertex.type == VertexType.Road) continue;

                    Vector3 pos = vertex.position;

                    if (pos.y > seaLevel)
                    // if (pos.y > seaLevel && (vertex.type == VertexType.Generic || vertex.type == VertexType.Unset))
                    {
                        if (Fitness(x, z, fastNoise) > 1 - density)
                        {
                            // float noiseValue = (float)fastNoise.GetNoise(x, z);

                            // if (noiseValue > 1 - density)
                            // {
                            pos.y -= 0.35f;

                            GameObject go = InstantiateObjectWithRandomRotation(prefab, pos);
                            go.transform.SetParent(parent);
                        }
                    }

                }
            }
        }


        public static GameObject InstantiateObjectWithRandomRotation(GameObject objectToInstantiate, Vector3 position)
        {
            // Generate a random rotation along the y-axis
            Quaternion yRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

            // Generate a random rotation within the specified range along the x and z axes
            Quaternion xzRotation = Quaternion.Euler(
                    UnityEngine.Random.Range(-18f, 18f),
                    0f,
                    UnityEngine.Random.Range(-18f, 18f));

            // Combine the rotations
            Quaternion finalRotation = yRotation * xzRotation;

            // Instantiate the object with the random rotation and position
            GameObject instantiatedObject = Instantiate(objectToInstantiate, position, finalRotation);

            return instantiatedObject;
        }



        public static float Fitness(int x, int z, FastNoise fastNoise)
        {
            float fitness = (float)fastNoise.GetNoise(x, z);
            fitness += UnityEngine.Random.Range(-0.3f, 0.3f);

            return fitness;
        }

        #endregion
        private MapData GenerateMapData()
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
                            break;
                        }
                    }
                }
            }
            // Debug.Log("GenerateMapData, heighest: " + heighest);
            return new MapData(heightMap, colourMap);
        }

        private static MapData GenerateMapData(TerrainVertex[,] vertexGrid, TerrainType[] defaultTerrainTypes, TerrainType[] pathTerrainTypes)
        {
            int gridSizeX = vertexGrid.GetLength(0);
            int gridSizeZ = vertexGrid.GetLength(1);
            Color[] colourMap = new Color[gridSizeX * gridSizeZ];
            Material[] materialMap = new Material[gridSizeX * gridSizeZ];
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
                        if (pathTerrainTypes != null)
                        {
                            colourMap[x * gridSizeX + z] = isCellCenterPoint ? pathTerrainTypes[1].colour : pathTerrainTypes[0].colour;
                            materialMap[x * gridSizeX + z] = isCellCenterPoint ? pathTerrainTypes[1].material : pathTerrainTypes[0].material;
                        }
                        else
                        {
                            colourMap[x * gridSizeX + z] = defaultTerrainTypes[0].colour;
                            materialMap[x * gridSizeX + z] = defaultTerrainTypes[0].material;
                        }
                        continue;
                    }

                    for (int i = 0; i < defaultTerrainTypes.Length; i++)
                    {
                        if (currentHeight <= defaultTerrainTypes[i].height)
                        {
                            // Debug.Log("GenerateMapData, currentHeight: " + currentHeight + ", terrainType: " + defaultTerrainTypes[i].name);
                            colourMap[x * gridSizeX + z] = defaultTerrainTypes[i].colour;
                            materialMap[x * gridSizeX + z] = defaultTerrainTypes[i].material;
                            break;
                        }
                    }
                }
            }
            // Debug.Log("GenerateMapData, heighest: " + heighest);
            return new MapData(heightMap, colourMap, materialMap);
        }


        private void SmoothTerrainVerticesToCellGrid()
        {
            // Get all ground edge cells
            // Get Closest edge cell to vertex 
            // Get Closest Cell side point to vertex
            // Slope vertex y pos to side point y based on distanceMult

            if (vertexGrid != null)
            {
                List<HexagonCell> edgeCells = HexCellUtil.GetEdgeCells(cellManager.GetAllCellsList()).FindAll(c => c.GetGridLayer() == 0 || c.isLeveledGroundCell);

                Vector2 centerPos = new Vector2(CenterPosition().x, CenterPosition().z);
                float smoothRadius = cellManager.GetRadius() * blendRadiusMult;

                for (int x = 0; x < vertexGrid.GetLength(0); x++)
                {
                    for (int z = 0; z < vertexGrid.GetLength(1); z++)
                    {
                        TerrainVertex currVertex = vertexGrid[x, z];
                        if (currVertex.type == VertexType.Generic || currVertex.type == VertexType.Unset)
                        {
                            Vector2 posXZ = new Vector2(currVertex.position.x, currVertex.position.z);
                            if (Vector2.Distance(posXZ, centerPos) < smoothRadius)
                            {
                                (HexagonCell nearestCell, float nearestDistance) = HexCellUtil.GetClosestCell(edgeCells, posXZ);
                                if (nearestCell != null)
                                {
                                    float distPercent = 1f - (nearestDistance / smoothRadius);
                                    float slopeY = Mathf.Lerp(currVertex.position.y, nearestCell.transform.position.y, distPercent);
                                    vertexGrid[x, z].position.y = slopeY;
                                    Debug.Log("SmoothTerrainVerticesToCellGrid, distPercent: " + distPercent + ", slopeY: " + slopeY);
                                }
                            }
                        }
                    }
                }
            }
        }

        [Header("Debug Settings")]
        [SerializeField] private ShowVertexState debug_showVertices;
        [SerializeField] private bool debug_showPrototypes;
        [SerializeField] private bool debug_showLocations;
        [SerializeField] private bool debug_disableCellManager;
        [SerializeField] private bool debug_showBounds;
        [SerializeField] private bool debug_showBlendRadius;
        [SerializeField] private bool debug_showNeighbors;

        [SerializeField] private bool debug_showHexSidePoints;
        [SerializeField] private HexagonSide debug_showHexSide;

        [Header(" ")]
        [SerializeField] private bool debug_editorUpdateTerrainOnce;
        [SerializeField] private bool debug_vertexRemapDoOnce;
        private void OnDrawGizmos()
        {
            if (debug_showNeighbors)
            {
                if (hexagonCell == null)
                {
                    debug_showNeighbors = false;
                    Debug.LogError("hexagonCell for world area is null");
                    return;
                }

                HexagonCellPrototype.GizmoShowNeighbors(hexagonCell, 24f);
            }

            if (debug_showHexSidePoints)
            {
                if (hexagonCell == null)
                {
                    debug_showHexSidePoints = false;
                    Debug.LogError("hexagonCell for world area is null");
                    return;
                }
                HexagonCellPrototype.GizmoShowSideAndCorners(hexagonCell, debug_showHexSide, 3, 2);
            }


            if (debug_showBounds)
            {
                Gizmos.color = Color.black;
                float radius = areaSize / 2;
                Gizmos.DrawWireSphere(transform.position, radius);
            }

            if (debug_showBlendRadius)
            {
                Gizmos.color = Color.red;
                float radius = cellManager.GetRadius() * blendRadiusMult;
                Gizmos.DrawWireSphere(transform.position, radius);
            }

            if (debug_showLocations)
            {

                if (locationData != null && locationData.Count > 0)
                {
                    for (int i = 0; i < locationData.Count; i++)
                    {
                        LocationData loc = locationData[i];

                        if (loc.locationType == LocationType.Outpost)
                        {
                            Gizmos.color = Color.yellow;
                            float radius = loc.radius;
                            Gizmos.DrawSphere(loc.centerPosition, 8);
                            Gizmos.DrawWireSphere(loc.centerPosition, radius);

                            float hue = (float)i / locationData.Count;

                            foreach (var item in loc.cluster.prototypes)
                            {
                                Gizmos.color = Color.HSVToRGB(hue, 1f, 1f);
                                // HexagonCellPrototype.DrawHexagonCellPrototype(item, 0.2f, 0.25f, true);
                                Gizmos.DrawWireSphere(item.center, 6);
                            }
                        }
                    }

                }
            }

            if (debug_showVertices != ShowVertexState.None)
            {
                TerrainVertexUtil.DisplayTerrainVertices(vertexGrid, debug_showVertices, null);
            }

            // if (debug_showVertices == ShowVertexState.None)
            // {
            //     // if (terrainVertexSurfacesIndiciesByCellXZCenter != null)
            //     // {
            //     //     DisplayTerrainVertexSurfaces(terrainVertexSurfacesIndiciesByCellXZCenter, vertexGrid);
            //     // }
            // }

            // if (debug_showPrototypes && !debug_disableCellManager && vertexGrid != null && cellManager != null)
            // {
            //     if (Application.isPlaying == false && cellManager.cellPrototypesByLayer_V2 != null)
            //     {
            //         if (!debug_vertexRemapDoOnce)
            //         {
            //             debug_vertexRemapDoOnce = true;

            //             SetupCellPrototypes(true);
            //         }
            //     }
            // }
        }
    }
}