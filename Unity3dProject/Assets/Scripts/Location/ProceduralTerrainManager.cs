using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using WFCSystem;

namespace ProceduralBase
{
    [ExecuteInEditMode]
    public class ProceduralTerrainManager : MonoBehaviour
    {
        public enum NoiseType { Perlin, Simplex, Value }
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private FastNoiseUnity fastNoiseUnity;

        [Header("Terrain Gen Settings")]
        [SerializeField] private NoiseType noiseType;
        [SerializeField] private float noiseScale = 32f;
        [Range(8, 256)][SerializeField] private int terrainSize = 64;
        public void SetAreaSize(int newSize)
        {
            terrainSize = newSize;
        }

        [Range(-32, 72)] public float terrainHeight = 24f;
        [Range(-2f, 0.75f)][SerializeField] private float persistence = 0.45f;
        [Range(-1f, 2.6f)][SerializeField] private float lacunarity = 2f;
        [Range(1f, 128f)][SerializeField] private int octaves = 6;

        [Header("Generate")]
        [SerializeField] private bool enableTerrainEditMode = true;
        [SerializeField] private bool generate;

        Mesh mesh;
        private TerrainVertex[,] vertexGrid;
        // public void SetVertexGrid(TerrainVertex[,] _vertexGrid)
        // {
        //     vertexGrid = _vertexGrid;
        // }

        public void SetVertexGrid(TerrainVertex[,] _vertexGrid)
        {
            int rows = _vertexGrid.GetLength(0);
            int cols = _vertexGrid.GetLength(1);
            vertexGrid = new TerrainVertex[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    vertexGrid[i, j] = new TerrainVertex
                    {
                        position = _vertexGrid[i, j].position,
                        index = _vertexGrid[i, j].index
                    };
                }
            }
        }

        [SerializeField] private List<TerrainVertex> accessibleVertices;

        #region Saved State
        float _terrainSize;
        float _terrainHeight;

        private bool _editorUpdate;
        #endregion

        [Header("Debug Settings")]
        [SerializeField] private bool debug_editorUpdateTerrainOnce;
        // [SerializeField] private bool debug_showVertices;


        private void InitialSetup()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            fastNoiseUnity = GetComponent<FastNoiseUnity>();

            if (!meshFilter) meshFilter = GetComponent<MeshFilter>();
            if (!meshRenderer) meshRenderer = GetComponent<MeshRenderer>();
            if (!mesh)
            {
                mesh = new Mesh();
                mesh.name = "Procedural Terrain";
            }
        }
        private void Awake() => InitialSetup();

        void OnValidate()
        {
            _editorUpdate = true;

            if (_terrainHeight != terrainHeight || _terrainSize != terrainSize)
            {
                _terrainHeight = terrainHeight;
                _terrainSize = terrainSize;

                generate = true;
            }

            // if (debug_showVertices)
            // {
            //     if (vertexGrid == null) debug_showVertices = false;
            // }
        }

        private float updateTime = 1f;
        private float timer;
        private void Update()
        {
            if (!enableTerrainEditMode) return;

            if (!_editorUpdate && timer > 0f)
            {
                timer -= Time.fixedDeltaTime;
                return;
            }
            timer = updateTime;

            if (!debug_editorUpdateTerrainOnce || _editorUpdate)
            {
                _editorUpdate = false;

                if (generate)
                {
                    generate = false;
                    GenerateMesh();
                }
            }
        }

        public void GenerateMesh()
        {
            if (vertexGrid == null)
            {
                vertexGrid = TerrainVertexUtil.GenerateVertexGrid(transform.position, terrainSize, 6, Vector2.zero);
                Debug.Log("GenerateMesh, vertexGrid: " + vertexGrid.GetLength(0));
            }

            Vector3[] vertexPositions = UpdateVertices().ToArray();
            Debug.Log("GenerateMesh, vertexPositions: " + vertexPositions.Length);

            mesh.Clear();
            mesh.vertices = vertexPositions;
            mesh.triangles = GenerateTerrainTriangles(vertexGrid);
            // mesh.triangles = GenerateTerrainTriangles(terrainSize);
            // mesh.triangles = ProceduralTerrainUtility.GenerateTerrainTriangles(vertexPositions.Length);


            // mesh.uv = ProceduralTerrainUtility.GenerateTerrainUVs(terrainSize, vertexPositions.Length);
            // Recalculate the normals to ensure proper lighting
            mesh.RecalculateNormals();
            // Apply the mesh data to the MeshFilter component
            meshFilter.mesh = mesh;
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
                        if (vertexGrid[x, z].type == VertexType.Generic)
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

        public static int[] GenerateTerrainTriangles(TerrainVertex[,] vertexGrid)
        {
            int numVertices = vertexGrid.GetLength(0);

            // Create an array to store the triangle indices
            int[] triangles = new int[(numVertices - 1) * (numVertices - 1) * 6];

            // Iterate through the grid and create the triangles
            int index = 0;
            for (int x = 0; x < numVertices - 1; x++)
            {
                for (int y = 0; y < numVertices - 1; y++)
                {
                    triangles[index++] = x + y * numVertices;
                    triangles[index++] = x + 1 + y * numVertices;
                    triangles[index++] = x + (y + 1) * numVertices;

                    triangles[index++] = x + 1 + y * numVertices;
                    triangles[index++] = x + 1 + (y + 1) * numVertices;
                    triangles[index++] = x + (y + 1) * numVertices;
                }
            }
            return triangles;
        }

        // public static int[] GenerateTerrainTriangles(int gridSize)
        // {
        //     // Create an array to store the triangle indices
        //     int[] triangles = new int[(gridSize - 1) * (gridSize - 1) * 6];

        //     // Iterate through the grid and create the triangles
        //     int index = 0;
        //     for (int x = 0; x < gridSize - 1; x++)
        //     {
        //         for (int y = 0; y < gridSize - 1; y++)
        //         {
        //             triangles[index++] = x + y * gridSize;
        //             triangles[index++] = x + 1 + y * gridSize;
        //             triangles[index++] = x + (y + 1) * gridSize;

        //             triangles[index++] = x + 1 + y * gridSize;
        //             triangles[index++] = x + 1 + (y + 1) * gridSize;
        //             triangles[index++] = x + (y + 1) * gridSize;
        //         }
        //     }
        //     return triangles;
        // }

    }
}