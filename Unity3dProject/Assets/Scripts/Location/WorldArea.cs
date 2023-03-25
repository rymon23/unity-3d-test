using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using WFCSystem;

using UnityEngine.Jobs;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

namespace ProceduralBase
{
    public enum NoiseType { Perlin, Simplex, Value }

    [ExecuteInEditMode]
    public class WorldArea : MonoBehaviour
    {
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
        public List<Location> locations;
        [Header("Settings")]
        [Range(12, 296)] public int areaSize = 72;

        [Header("Terrain Gen Settings")]
        [SerializeField] private bool useTerrainGenerator = true;
        [SerializeField] private bool enableTerrainEditMode = true;
        [Range(-32, 72)] public float terrainHeight = 24f;
        [SerializeField] private NoiseType noiseType;
        [SerializeField] private float noiseScale = 32f;
        [Range(-2f, 0.75f)][SerializeField] private float persistence = 0.45f;
        [Range(-1f, 2.6f)][SerializeField] private float lacunarity = 2f;
        [Range(1f, 128f)][SerializeField] private int octaves = 6;

        [Header("Terrain Smoothing")]
        [Range(1f, 3f)][SerializeField] private float blendRadiusMult = 1.6f;
        [Range(0.05f, 1.5f)][SerializeField] private float groundSlopeElevationStep = 0.45f;

        [SerializeField] private TerrainType[] terrainTypes;
        [SerializeField] private TerrainType[] pathTypes;
        // [Header(" ")]
        // [SerializeField] private bool resetPrototypes;

        [Header("Generate")]
        [SerializeField] private bool updateTerrainTexture;
        [SerializeField] private bool generateTerrain;
        // [SerializeField] private bool generateNavmesh;

        [Header("Vertex Settings")]
        [SerializeField] private bool useStepSize;
        [Range(1f, 10f)][SerializeField] private float stepSize = 3f;
        public TerrainVertex[,] vertexGrid { get; private set; }
        private List<TerrainVertex> accessibleVertices;

        #region Saved State
        Vector3 _position;
        int _areaSize;
        float _stepSize;
        float _terrainHeight;
        float _fastNoise_Seed;
        private bool _editorUpdate;
        private bool _doOnce;
        #endregion
        private Mesh mesh;

        [SerializeField] private bool revaluate;

        [Header("Generate")]
        [SerializeField] private bool generateAll;

        [Header("World Space Data")]
        [SerializeField] private List<Transform> entranceMarkers;

        private void InitialSetup()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            fastNoiseUnity = GetComponent<FastNoiseUnity>();

            cellManager = GetComponent<HexagonCellManager>();
            locations = GetComponentsInChildren<Location>().ToList();
            navMeshSurface = GetComponent<NavMeshSurface>();
            mapDisplay = GetComponent<MapDisplay>();

            wfc = GetComponent<IWFCSystem>();
            if (wfc == null) Debug.LogError("Missing WFC system component!");

            if (!mesh)
            {
                mesh = new Mesh();
                mesh.name = "Procedural Terrain";
            }
        }

        public void Generate(bool force = false, bool resetVertexGrid = false)
        {
            generateAll = false;

            if (_doOnce && !force) return;
            _doOnce = true;

            Debug.Log("WorldArea - Generate!");

            if (resetVertexGrid) ResetVertexGrid();

            // Setup Cells
            cellManager.GenerateGridsByLayer();
            SetupCellPrototypes(true);

            if (cellManager.cellPrototypesByLayer_V2 != null)
            {
                cellManager.GenerateCells(true, false, cellManager.cellPrototypesByLayer_V2);
            }
            else
            {
                Debug.LogError("EMPTY cellPrototypesByLayer_V2!");
            }

            (Dictionary<int, List<HexagonCell>> _allCellsByLayer, List<HexagonCell> _allCells) = cellManager.GetCells();

            // Add cells to WFC
            wfc.SetRadius(cellManager.radius);
            wfc.SetCells(_allCellsByLayer, _allCells);

            // Run WFC
            wfc.ExecuteWFC();

            // CreatePathCellMicroGrid();

            // Generate Ground Mesh
            // GenerateGroundMesh();

            // CreateMeshFromVertices(vertexGrid, meshFilter);
            // RefreshTerrainMesh();

            UpdateTerrainTexture();

            // Setup Locations

            // SetupMarkers(); 
            // DisableCells();
        }

        /// <summary>
        ///  TODO: 
        /// 
        ///     Consolidate All host Cells and subCells for easy lookup
        ///     
        ///     GetMarkerPoints from Cells
        ///      
        ///     Disable / Destroy Cells once build is done
        /// </summary>

        public void SetupCellPrototypes(bool updateGroundMesh = true)
        {
            int topLayer = cellManager.cellPrototypesByLayer_V2.Keys.Count - 1;

            HexagonCellPrototype.AssignTerrainVerticesToPrototypes(cellManager.cellPrototypesByLayer_V2[topLayer], vertexGrid, false);

            HexagonCellPrototype.GroundPrototypesToTerrainVertexElevation(cellManager.cellPrototypesByLayer_V2, vertexGrid);
            HexagonCellPrototype.CleanupCellIslandLayerPrototypes(cellManager.cellPrototypesByLayer_V2, 3);

            HexagonCellPrototype.AssignTerrainVerticesToGroundPrototypes(cellManager.cellPrototypesByLayer_V2, vertexGrid);
            HexagonCellPrototype.CleanupCellIslandLayerPrototypes(cellManager.cellPrototypesByLayer_V2, 3);

            HexagonCellPrototype.AssignRampsForIslandLayerPrototypes(cellManager.cellPrototypesByLayer_V2, vertexGrid, 4, groundSlopeElevationStep);

            (List<HexagonCellPrototype> path, List<HexagonCellPrototype> entry) = cellManager.GeneratePrototypeGridEntryAndPaths();
            // foreach (var item in path)
            // {
            //     Debug.Log("paths - X12, path - id: " + item.id + ", uid: " + item.uid);
            // }
            cellManager.GenerateMicroGridFromHosts(path, 4, 3);

            HexagonCellPrototype.AssignPathCenterVertices(path, vertexGrid);
            HexagonCellPrototype.SmoothVertexElevationAlongPath(path, vertexGrid);

            // HexagonCellPrototype.SmoothElevationAlongPathNeighbors(path, vertexGrid);

            if (updateGroundMesh)
            {
                GenerateTerrainMesh();

                debug_vertexRemapDoOnce = true;
                // UpdateTerrainTexture();
            }
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

            // Generate cells  

            // HexGridArea gridArea = cellPathMicroGridManager.gameObject.GetComponent<HexGridArea>();
            // gridArea.InitialSetup();
            // gridArea.Generate();
        }


        public void GenerateGroundMesh()
        {
            if (cellManager == null || vertexGrid == null) return;
            List<HexagonCell> groundCells = cellManager.GetAllCellsList().FindAll(c => c.isLeveledGroundCell || (c.GetGridLayer() == 0 && c.isLeveledEdge || !c.isLeveledCell));

            foreach (var item in groundCells)
            {
                item._vertexIndices.Clear();
            }

            HexagonCell.AssignVerticesToCells(vertexGrid, groundCells);

            List<HexagonCell> path = cellManager.GetAllCellsList().FindAll(c => c.isPathCell || c.isEntryCell);

            HexagonCell.SmoothElevationAlongPath(path, vertexGrid);
            HexagonCell.SmoothElevationAlongPathNeighbors(path.FindAll(c => c.isEntryCell == false), vertexGrid);

            // SmoothTerrainVerticesToCellGrid();

            CreateMeshFromVertices(vertexGrid, meshFilter);
            RefreshTerrainMesh();
        }

        private void OnValidate()
        {
            InitialSetup();

            _editorUpdate = true;

            bool shouldUpdate = false;

            Debug.Log("WorldArea - OnValidate");


            if (bSaveMesh)
            {
                bSaveMesh = false;
                if (mesh != null) SaveMeshAsset(mesh, "New World Area Mesh");
            }

            if (_fastNoise_Seed != fastNoiseUnity.fastNoise.GetSeed())
            {
                _fastNoise_Seed = fastNoiseUnity.fastNoise.GetSeed();
                shouldUpdate = true;
            }

            if (_position != transform.position || _terrainHeight != terrainHeight)
            {
                // Debug.Log("WorldArea - _position: " + _position + ", transform.position: " + transform.TransformPoint(transform.position)); //+ ", cellPrototypesByLayer_V2: " + cellPrototypesByLayer_V2.Count);

                _position = transform.position;
                _terrainHeight = terrainHeight;
                shouldUpdate = true;
            }

            if (revaluate || stepSize != _stepSize)
            {
                _stepSize = stepSize;

                shouldUpdate = true;
            }

            if (revaluate || areaSize != _areaSize)
            {
                if (areaSize % 2 != 0) areaSize += 1;
                _areaSize = areaSize;

                shouldUpdate = true;
            }

            if (shouldUpdate)
            {
                // cellManager.radius = areaSize / 2;

                ResetVertexGrid();

                cellManager.ResetPrototypes();
            }

            generateTerrain = useTerrainGenerator && (generateTerrain || shouldUpdate || generateAll);

            if (generateAll && useTerrainGenerator == false) Generate(Application.isPlaying == false);

            if (updateTerrainTexture)
            {
                updateTerrainTexture = false;

                UpdateTerrainTexture();
            }
            // if (generateNavmesh)
            // {
            //     generateNavmesh = false;
            //     navMeshSurface.BuildNavMesh();
            // }

            revaluate = false;
        }

        private float updateTime = 1f, timer;
        private void Update()
        {
            if (Application.isPlaying) return;

            // if (_position != transform.position) OnValidate();

            if (!useTerrainGenerator || !enableTerrainEditMode) return;

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
                    GenerateTerrainMesh();
                    if (generateAll) Generate(Application.isPlaying == false);
                }
            }
        }

        private void Awake() => InitialSetup();

        private void Start()
        {
            if (Application.isPlaying == false) return;

            InitialSetup();

            _doOnce = false;

            if (useTerrainGenerator) GenerateTerrainMesh();
            Generate();
        }

        #region Save Mesh
        [SerializeField] private bool bSaveMesh = false;
        [SerializeField] private Mesh lastSavedMesh;
        void SaveMeshAsset(Mesh mesh, string assetName)
        {
            // Create a new mesh asset
            lastSavedMesh = Instantiate(mesh) as Mesh;
            lastSavedMesh.name = assetName;

            // Save the mesh asset to the project
            AssetDatabase.CreateAsset(lastSavedMesh, "Assets/Terrain/" + assetName + ".asset");
            AssetDatabase.SaveAssets();
        }
        #endregion


        #region Terrain Generation

        // public void ResetVertexGrid() => vertexGrid = useStepSize ? HexagonCell.GenerateVertexGrid(gameObject.transform.TransformPoint(transform.position), areaSize, stepSize)
        //                                 : HexagonCell.GenerateVertexGrid(gameObject.transform.TransformPoint(transform.position), areaSize);
        public void ResetVertexGrid() => vertexGrid = useStepSize ? HexagonCell.GenerateVertexGrid(gameObject.transform.position, areaSize, stepSize)
                                        : HexagonCell.GenerateVertexGrid(gameObject.transform.position, areaSize);

        public void RefreshTerrainMesh()
        {
            mesh = meshFilter.mesh;
            // Recalculate the normals to ensure proper lighting
            mesh.RecalculateNormals();
            // Apply the mesh data to the MeshFilter component
            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;
        }
        public void GenerateTerrainMesh()
        {
            Vector3[] vertexPositions = UpdateVertexElevations().ToArray();
            Debug.Log("GenerateMesh, vertexPositions: " + vertexPositions.Length);

            mesh.Clear();
            mesh.vertices = vertexPositions;
            mesh.triangles = ProceduralTerrainUtility.GenerateTerrainTriangles(vertexGrid);
            // mesh.triangles = GenerateTerrainTriangles(vertexGrid.GetLength(0));
            mesh.uv = ProceduralTerrainUtility.GenerateTerrainUVs(vertexGrid);

            RefreshTerrainMesh();
            // // Recalculate the normals to ensure proper lighting
            // mesh.RecalculateNormals();
            // // Apply the mesh data to the MeshFilter component
            // meshFilter.sharedMesh = mesh;
            // meshCollider.sharedMesh = mesh;
        }

        private List<Vector3> UpdateVertexElevations()
        {
            List<TerrainVertex> newAccessibleVertices = new List<TerrainVertex>();
            List<Vector3> positions = new List<Vector3>();

            if (vertexGrid != null)
            {
                for (int x = 0; x < vertexGrid.GetLength(0); x++)
                {
                    for (int z = 0; z < vertexGrid.GetLength(1); z++)
                    {
                        if (vertexGrid[x, z].type == VertexType.Generic || vertexGrid[x, z].type == VertexType.Unset)
                        {
                            vertexGrid[x, z].position.y = CalculateNoiseHeightForVertex(x, z, terrainHeight, noiseType, fastNoiseUnity.fastNoise, persistence, octaves, noiseScale, lacunarity);
                            vertexGrid[x, z].position = transform.TransformPoint(vertexGrid[x, z].position);
                            newAccessibleVertices.Add(vertexGrid[x, z]);
                        }

                        // Vector3 worldPosition = meshFilter.gameObject.transform.InverseTransformPoint(vertexGrid[x, z].position);
                        positions.Add(transform.InverseTransformPoint(vertexGrid[x, z].position));
                        // positions.Add(vertexGrid[x, z].position);
                    }
                }
            }
            accessibleVertices = newAccessibleVertices;

            debug_vertexRemapDoOnce = false;
            return positions;
        }

        private void UpdateTerrainTexture()
        {
            MapData mapData = GenerateMapData(vertexGrid, terrainTypes, pathTypes);
            // MapData mapData = GenerateMapData();
            // Texture2D texture2D = TextureGenerator.TextureFromHeightMap(mapData.heightMap, terrainHeight);
            // Texture2D texture2D = TextureGenerator.TextureFromMaterialMap(mapData.materialMap,
            //     vertexGrid.GetLength(0),
            //     vertexGrid.GetLength(1));
            Texture2D texture2D = TextureGenerator.TextureFromColourMap(mapData.colourMap,
                vertexGrid.GetLength(0),
                vertexGrid.GetLength(1));

            mapDisplay.DrawTexture(texture2D);
        }


        public static void SmoothVertexList(List<TerrainVertex> vertexList, TerrainVertex[,] vertexGrid)
        {
            vertexList.Sort((v1, v2) =>
            {
                int result = v1.position.x.CompareTo(v2.position.x);
                if (result == 0)
                    result = v1.position.z.CompareTo(v2.position.z);
                return result;
            });

            for (int i = 1; i < vertexList.Count - 1; i++)
            {
                TerrainVertex currVertex = vertexList[i];
                // if (currVertex.isCellCenterPoint) continue;

                TerrainVertex prevVertex = vertexList[i - 1];
                TerrainVertex nextVertex = vertexList[i + 1];
                Vector2 currentPosXZ = new Vector2(currVertex.position.x, currVertex.position.z);
                Vector2 prevPosXZ = new Vector2(prevVertex.position.x, prevVertex.position.z);
                Vector2 nextPosXZ = new Vector2(nextVertex.position.x, nextVertex.position.z);

                float slopeY = Mathf.Lerp(prevVertex.position.y, nextVertex.position.y, 0.02f);
                currVertex.position.y = slopeY;

                vertexList[i] = currVertex;
                vertexGrid[vertexList[i].index / vertexGrid.GetLength(0), vertexList[i].index % vertexGrid.GetLength(0)].position = currVertex.position;
                vertexGrid[vertexList[i].index / vertexGrid.GetLength(0), vertexList[i].index % vertexGrid.GetLength(0)].type = VertexType.Road;
            }
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
                List<HexagonCell> edgeCells = HexagonCell.GetEdgeCells(cellManager.GetAllCellsList()).FindAll(c => c.GetGridLayer() == 0 || c.isLeveledGroundCell);

                Vector2 centerPos = new Vector2(transform.position.x, transform.position.z);
                float smoothRadius = cellManager.radius * blendRadiusMult;

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
                                (HexagonCell nearestCell, float nearestDistance) = HexagonCell.GetClosestCell(edgeCells, posXZ);
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
        [SerializeField] private bool debug_showBounds;
        [SerializeField] private bool debug_showBlendRadius;
        [SerializeField] private bool debug_editorUpdateTerrainOnce;
        [SerializeField] private bool debug_vertexRemapDoOnce;
        public enum ShowVertexState { None, All, Path, Terrain, Cell, CellCenter, CellCorner, CellRamp }

        private void OnDrawGizmos()
        {
            if (debug_showBounds)
            {
                Gizmos.color = Color.black;
                float radius = areaSize / 2;
                Gizmos.DrawWireSphere(transform.position, radius);
            }

            if (debug_showBlendRadius)
            {
                Gizmos.color = Color.red;
                float radius = cellManager.radius * blendRadiusMult;
                Gizmos.DrawWireSphere(transform.position, radius);
            }

            if (debug_showVertices != ShowVertexState.None)
            {

                if (vertexGrid != null)
                {
                    for (int x = 0; x < vertexGrid.GetLength(0); x++)
                    {
                        for (int z = 0; z < vertexGrid.GetLength(1); z++)
                        {
                            TerrainVertex currentVertex = vertexGrid[x, z];
                            bool show = false;
                            float rad = 0.33f;
                            // Debug.Log("currentVertex - elevationY: " + currentVertex.position.y);

                            Gizmos.color = Color.black;
                            if (debug_showVertices == ShowVertexState.All)
                            {
                                show = true;
                            }
                            else if (debug_showVertices == ShowVertexState.Cell)
                            {
                                show = currentVertex.type == VertexType.Cell;
                                Gizmos.color = Color.red;
                                rad = 0.66f;
                            }
                            else if (debug_showVertices == ShowVertexState.Path)
                            {
                                show = currentVertex.type == VertexType.Road;
                                Gizmos.color = Color.red;
                                rad = 0.66f;
                            }
                            else if (debug_showVertices == ShowVertexState.Terrain)
                            {
                                show = currentVertex.type == VertexType.Generic;
                                Gizmos.color = Color.red;
                                rad = 0.66f;
                            }
                            else if (debug_showVertices == ShowVertexState.CellCenter)
                            {
                                show = currentVertex.isCellCenterPoint;
                                Gizmos.color = Color.red;
                                rad = 0.66f;
                            }
                            else if (debug_showVertices == ShowVertexState.CellCorner)
                            {
                                show = currentVertex.isCellCornerPoint;
                                Gizmos.color = Color.red;
                                rad = 0.66f;
                            }

                            Vector3 worldPosition = currentVertex.position;
                            // Vector3 worldPosition = gameObject.transform.TransformPoint(currentVertex.position);
                            if (show) Gizmos.DrawSphere(worldPosition, rad);
                        }
                    }


                }
            }

            if (debug_showPrototypes && vertexGrid != null)
            {
                if (Application.isPlaying == false && cellManager.cellPrototypesByLayer_V2 != null)
                {
                    if (!debug_vertexRemapDoOnce)
                    {
                        debug_vertexRemapDoOnce = true;

                        SetupCellPrototypes(true);
                    }
                }
            }
        }

        public static void CreateMeshFromVertices(TerrainVertex[,] vertices, MeshFilter meshFilter)
        {
            List<int> triangles = new List<int>();
            List<Vector3> verticePositions = new List<Vector3>();

            int rowCount = vertices.GetLength(0);
            int columnCount = vertices.GetLength(1);

            for (int row = 0; row < rowCount - 1; row++)
            {
                for (int col = 0; col < columnCount - 1; col++)
                {
                    int bottomLeft = row * columnCount + col;
                    int bottomRight = bottomLeft + 1;
                    int topLeft = bottomLeft + columnCount;
                    int topRight = topLeft + 1;

                    triangles.Add(topLeft);
                    triangles.Add(bottomLeft);
                    triangles.Add(bottomRight);

                    triangles.Add(topLeft);
                    triangles.Add(bottomRight);
                    triangles.Add(topRight);
                }
            }

            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    Vector3 worldPosition = meshFilter.gameObject.transform.InverseTransformPoint(vertices[row, col].position);
                    // Vector3 worldPosition = meshFilter.transform.TransformPoint(vertices[row, col].position);
                    verticePositions.Add(worldPosition);
                }
            }

            // Set the mesh data
            Mesh mesh = new Mesh();
            mesh.Clear();
            mesh.SetVertices(verticePositions);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();

            meshFilter.sharedMesh = mesh;
        }

        #region MultiThreaded

        #endregion
    }
}