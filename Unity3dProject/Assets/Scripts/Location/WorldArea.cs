using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using WFCSystem;

namespace ProceduralBase
{
    // [ExecuteInEditMode]
    public class WorldArea : MonoBehaviour
    {
        public enum NoiseType { Perlin, Simplex, Value }
        [SerializeField] private HexagonCellManager cellManager;
        // [SerializeField] private HexagonWaveFunctionCollapse_1 wfc;
        // [SerializeField] private Transform _wfcSource;
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

        [SerializeField] private TerrainType[] terrainTypes;
        [SerializeField] private TerrainType[] pathTypes;

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
        int _areaSize;
        float _stepSize;
        float _terrainHeight;
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

            // wfc = GetComponent<HexagonWaveFunctionCollapse_1>();
            wfc = GetComponent<IWFCSystem>();
            if (wfc == null) Debug.LogError("Missing WFC system component!");

            if (!mesh)
            {
                mesh = new Mesh();
                mesh.name = "Procedural Terrain";
            }
        }

        public void ResetVertexGrid()
        {
            vertexGrid = useStepSize ? HexagonCell.GenerateVertexGrid(transform.position, areaSize, stepSize)
                                        : HexagonCell.GenerateVertexGrid(transform.position, areaSize);
            // Debug.Log("ResetVertexGrid, size: " + vertexGrid.GetLength(0));
        }

        public void Generate()
        {
            if (_doOnce) return;
            _doOnce = true;

            ResetVertexGrid();

            // Setup Cells
            cellManager.GenerateCells(true);
            // Add cells to WFC
            wfc.SetRadius(cellManager.radius);
            wfc.SetCells(cellManager.GetCells());
            // Run WFC
            wfc.ExecuteWFC();

            // Generate Ground Mesh
            GenerateGroundMesh();

            UpdateTerrainTexture();
            // Setup Locations 
        }

        public void GenerateGroundMesh()
        {
            if (cellManager == null || vertexGrid == null) return;
            List<HexagonCell> groundCells = cellManager.GetCells().FindAll(c => c.isLeveledGroundCell || (c.GetGridLayer() == 0 && c.isLeveledEdge || !c.isLeveledCell));

            foreach (var item in groundCells)
            {
                item._vertexIndices.Clear();
            }

            HexagonCell.AssignVerticesToCells(vertexGrid, groundCells);

            List<HexagonCell> path = cellManager.GetCells().FindAll(c => c.isPathCell || c.isEntryCell);

            HexagonCell.SmoothElevationAlongPath(path, vertexGrid);
            HexagonCell.SmoothElevationAlongPathNeighbors(path.FindAll(c => c.isEntryCell == false), vertexGrid);

            // SmoothTerrainVerticesToCellGrid();

            HexagonCell.CreateMeshFromVertices(vertexGrid, meshFilter);
            mesh = meshFilter.mesh;
            meshCollider.sharedMesh = mesh;
        }

        private void OnValidate()
        {
            InitialSetup();

            _editorUpdate = true;

            bool shouldUpdate = false;
            // revaluate = revaluate || updateTerrainTexture;

            if (bSaveMesh)
            {
                bSaveMesh = false;
                if (mesh != null) SaveMeshAsset(mesh, "New World Area Mesh");
            }

            if (_terrainHeight != terrainHeight)
            {
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
                cellManager.radius = areaSize / 2;

                ResetVertexGrid();
            }

            generateTerrain = useTerrainGenerator && (generateTerrain || shouldUpdate);

            // if (generateAll)
            // {
            //     generateAll = false;

            //     Generate();
            // }

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

        private float updateTime = 1f;
        private float timer;
        private void Update()
        {
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
                }
            }
        }

        private void Awake() => InitialSetup();

        private void Start()
        {
            InitialSetup();

            _doOnce = false;
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

        public void GenerateTerrainMesh()
        {
            Vector3[] vertexPositions = UpdateVertices().ToArray();
            Debug.Log("GenerateMesh, vertexPositions: " + vertexPositions.Length);

            mesh.Clear();
            mesh.vertices = vertexPositions;
            mesh.triangles = ProceduralTerrainUtility.GenerateTerrainTriangles(vertexGrid);
            mesh.uv = ProceduralTerrainUtility.GenerateTerrainUVs(vertexGrid);

            // Recalculate the normals to ensure proper lighting
            mesh.RecalculateNormals();
            // Apply the mesh data to the MeshFilter component
            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;
        }

        private List<Vector3> UpdateVertices()
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
                            UpdateVertexData(x, z);
                            newAccessibleVertices.Add(vertexGrid[x, z]);
                        }

                        Vector3 worldPosition = meshFilter.gameObject.transform.InverseTransformPoint(vertexGrid[x, z].position);
                        positions.Add(vertexGrid[x, z].position);
                    }
                }
            }
            accessibleVertices = newAccessibleVertices;
            return positions;
        }

        private void UpdateVertexData(int indexX, int indexZ)
        {
            float noiseHeight = GetNoiseHeightValue(indexX, indexZ, noiseType, fastNoiseUnity.fastNoise, persistence);
            float basePosY = noiseHeight * terrainHeight;

            vertexGrid[indexX, indexZ].position.y = basePosY;
        }

        private void UpdateTerrainTexture()
        {
            MapData mapData = GenerateMapData();
            // Texture2D texture2D = TextureGenerator.TextureFromHeightMap(mapData.heightMap, terrainHeight);
            Texture2D texture2D = TextureGenerator.TextureFromColourMap(mapData.colourMap,
                vertexGrid.GetLength(0),
                vertexGrid.GetLength(1));

            mapDisplay.DrawTexture(texture2D);
        }

        private float GetNoiseHeightValue(float x, float z, NoiseType noiseType, FastNoise fastNoise, float persistence)
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


        private void SmoothTerrainVerticesToCellGrid()
        {
            // Get all ground edge cells
            // Get Closest edge cell to vertex 
            // Get Closest Cell side point to vertex
            // Slope vertex y pos to side point y based on distanceMult

            if (vertexGrid != null)
            {
                List<HexagonCell> edgeCells = HexagonCell.GetEdgeCells(cellManager.GetCells()).FindAll(c => c.GetGridLayer() == 0 || c.isLeveledGroundCell);

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
        [SerializeField] private bool debug_showBounds;
        [SerializeField] private bool debug_showBlendRadius;
        [SerializeField] private bool debug_editorUpdateTerrainOnce;
        public enum ShowVertexState { None, All, Path, Terrain, Cell, CellCenter, CellCorner }

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

                            Gizmos.color = Color.black;
                            if (debug_showVertices == ShowVertexState.All)
                            {
                                show = true;
                            }
                            else if (debug_showVertices == ShowVertexState.CellCenter)
                            {
                                show = currentVertex.isCellCenterPoint;
                                Gizmos.color = Color.red;
                                rad = 0.66f;
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
                            else if (debug_showVertices == ShowVertexState.CellCorner)
                            {
                                show = currentVertex.isCellCornerPoint;
                                Gizmos.color = Color.red;
                                rad = 0.66f;
                            }

                            if (show) Gizmos.DrawSphere(currentVertex.position, rad);
                        }
                    }
                }
            }
        }
    }
}