using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using UnityEditor;
using System.Linq;

[ExecuteInEditMode]
public class ProceduralTerrainMesh8 : MonoBehaviour
{
    public FastNoiseUnity fastNoiseUnity;

    public enum NoiseType { Perlin, Simplex, Value }

    public NoiseType noiseType;

    struct Vertex
    {
        public Vector3 position;
    }

    [Range(8, 256)] public int terrainSize = 64;
    [Range(-32, 72)] public float terrainHeight = 24f;
    public float noiseScale = 32f;
    [Range(-2f, 0.75f)] public float persistence = 0.45f;
    [Range(-1f, 2.6f)] public float lacunarity = 2f;
    [Range(1f, 128f)] public int octaves = 6;

    public bool bFlattenCurve;
    public AnimationCurve terrainFlattenCurve;

    [Header("Location Marker Settings")]
    [Range(0, 24)][SerializeField] private int locationCount = 2;
    [Range(0, 12)][SerializeField] private float minLocationDistance = 2f;
    public AnimationCurve locationRadiusTerrainSmoothCurve;
    [SerializeField] private float minVerticePointLevelRadius = 3f;
    [SerializeField] private float maxVerticePointLevelRadius = 9f;
    [SerializeField] private float generatePointBorderXYOffeset = 2f;
    [SerializeField] private Vector2 generatePointYRange = new Vector2(0, 1);
    [SerializeField] private Color locationPointColor = Color.red;
    private Vector3[] locationPoints;

    #region Saved State
    float _minLocationDistance;
    float _generatePointYRangeMin;
    float _generatePointYRangeMax;
    float _generatePointBorderXYOffeset;
    #endregion

    Mesh mesh;
    Vertex[] vertices;
    // float[] elevations;

    void Start()
    {
        mesh = new Mesh();
        mesh.name = "Procedural Terrain";

        Update();
    }

    private void Update()
    {
        if (!debug_editorUpdateTerrainOnce || _editorUpdate)
        {
            _editorUpdate = false;

            // Generate the mesh data
            vertices = GenerateVertices(locationPoints);
            mesh.vertices = GetVertexPositions(vertices);
            mesh.triangles = GenerateTriangles();
            mesh.uv = GenerateUVs();
            // Recalculate the normals to ensure proper lighting
            mesh.RecalculateNormals();
            // Apply the mesh data to the MeshFilter component
            GetComponent<MeshFilter>().mesh = mesh;
        }
    }

    float GetNoiseHeightValue(float x, float z)
    {
        FastNoise fastNoise = fastNoiseUnity.fastNoise;

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

    Vertex[] GenerateVertices(Vector3[] points)
    {
        FastNoise fastNoise = fastNoiseUnity.fastNoise;

        // Create an array to store the vertex data
        Vertex[] vertices = new Vertex[terrainSize * terrainSize];

        // Iterate through the vertex data and set the height and color of each point
        for (int x = 0; x < terrainSize; x++)
        {
            for (int y = 0; y < terrainSize; y++)
            {
                float noiseHeight = GetNoiseHeightValue(x, y);

                // Set the height and color of the current point
                vertices[x + y * terrainSize] = new Vertex
                {
                    position = new Vector3(x, noiseHeight * terrainHeight, y),
                };

                if (points != null && points.Length > 0)
                {
                    // Find the nearest point to the current vertex
                    Vector3 nearestPoint = points[0];
                    float nearestDist = float.MaxValue;
                    for (int i = 0; i < points.Length; i++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(points[i].x, points[i].z));
                        if (dist < nearestDist)
                        {
                            nearestDist = dist;
                            nearestPoint = points[i];
                        }
                    }
                    if (nearestDist < minVerticePointLevelRadius)
                    {
                        vertices[x + y * terrainSize].position.y = nearestPoint.y;
                    }

                    // If the distance to the nearest point is within the minVerticePointLevelRadius,
                    // gradually smooth the height of the vertex to match the elevation of the nearest point
                    else if (nearestDist <= maxVerticePointLevelRadius)
                    {
                        // Calculate the distance as a percentage of the minVerticePointLevelRadius
                        float distPercent = nearestDist / maxVerticePointLevelRadius;
                        // Evaluate the Animation curve using the distance percentage
                        float curveValue = locationRadiusTerrainSmoothCurve.Evaluate(distPercent);
                        // Set the height of the vertex to be a blend of the point's y position and the original height
                        vertices[x + y * terrainSize].position.y = Mathf.Lerp(vertices[x + y * terrainSize].position.y, nearestPoint.y, curveValue);
                    }
                }
                else
                {
                    if (bFlattenCurve)
                    {
                        // Use the flatten curve to control the height of the terrain
                        vertices[x + y * terrainSize].position.y = terrainFlattenCurve.Evaluate(noiseHeight) * terrainHeight;
                    }
                }
            }
        }
        return vertices;
    }


    Vector3[] GetVertexPositions(Vertex[] vertices)
    {
        // Create an array to store the vertex positions
        Vector3[] vertexPositions = new Vector3[vertices.Length];

        // Iterate through the vertices and store the position of each vertex
        for (int i = 0; i < vertices.Length; i++)
        {
            vertexPositions[i] = vertices[i].position;
        }

        return vertexPositions;
    }

    int[] GenerateTriangles()
    {
        // Create an array to store the triangle indices
        int[] triangles = new int[(terrainSize - 1) * (terrainSize - 1) * 6];

        // Iterate through the grid and create the triangles
        int index = 0;
        for (int x = 0; x < terrainSize - 1; x++)
        {
            for (int y = 0; y < terrainSize - 1; y++)
            {
                triangles[index++] = x + y * terrainSize;
                triangles[index++] = x + (y + 1) * terrainSize;
                triangles[index++] = x + 1 + y * terrainSize;

                triangles[index++] = x + 1 + y * terrainSize;
                triangles[index++] = x + (y + 1) * terrainSize;
                triangles[index++] = x + 1 + (y + 1) * terrainSize;
            }
        }

        return triangles;
    }

    Vector2[] GenerateUVs()
    {
        // Create an array to store the UV data
        Vector2[] uvs = new Vector2[vertices.Length];

        // Iterate through the vertices and set the UVs of each vertex
        for (int x = 0; x < terrainSize; x++)
        {
            for (int y = 0; y < terrainSize; y++)
            {
                uvs[x + y * terrainSize] = new Vector2(x / (float)terrainSize, y / (float)terrainSize);
            }
        }

        return uvs;
    }

    void GeneratePoints()
    {
        locationPoints = new Vector3[locationCount];
        for (int i = 0; i < locationCount; i++)
        {
            // Generate a random position within the bounds of the terrain
            float xPos = UnityEngine.Random.Range(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset);
            float zPos = UnityEngine.Random.Range(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset);
            float yPos = UnityEngine.Random.Range(generatePointYRange.x, generatePointYRange.y);

            locationPoints[i] = new Vector3(xPos, yPos, zPos);

            // Use the transform scale to check distance
            Vector3 scale = transform.lossyScale;

            // Ensure that the point is a minimum distance away from all other points
            bool tooClose = false;
            do
            {
                tooClose = false;
                foreach (Vector3 point in locationPoints)
                {
                    if (point != locationPoints[i] && Vector3.Distance(point, locationPoints[i]) < minLocationDistance * scale.x)
                    {
                        tooClose = true;
                        xPos = UnityEngine.Random.Range(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset);
                        zPos = UnityEngine.Random.Range(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset);
                        yPos = UnityEngine.Random.Range(generatePointYRange.x, generatePointYRange.y);
                        locationPoints[i] = new Vector3(xPos, 0, zPos);
                        break;
                    }
                }
            } while (tooClose);
        }
    }

    [Header("Debug Settings")]
    [SerializeField] private bool debug_showPoints = true;
    [SerializeField] private bool debug_minLocationDistance;
    [SerializeField] private bool debug_locationPointLevelRadius;
    [SerializeField] private bool debug_editorUpdateTerrainOnce;
    private bool _editorUpdate;
    void OnDrawGizmos()
    {
        if (!debug_showPoints && !debug_locationPointLevelRadius && !debug_minLocationDistance) return;

        if (locationPoints != null && locationPoints.Length > 0)
        {
            // Draw a sphere at each point's position
            foreach (Vector3 point in locationPoints)
            {
                Vector3 scale = transform.lossyScale;
                Vector3 pointWorldPos = transform.TransformPoint(point);
                Gizmos.color = locationPointColor;

                if (debug_showPoints)
                {
                    Gizmos.DrawSphere(pointWorldPos, 6f);
                }
                if (debug_minLocationDistance)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(pointWorldPos, minLocationDistance * scale.x);
                }
                if (debug_locationPointLevelRadius)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireSphere(pointWorldPos, minVerticePointLevelRadius * scale.x);
                }
            }

        }
    }

    void OnValidate()
    {
        _editorUpdate = true;

        if (locationPoints == null || locationCount != locationPoints.Length ||
            _minLocationDistance != minLocationDistance ||
            _generatePointYRangeMin != generatePointYRange.x ||
            _generatePointYRangeMax != generatePointYRange.y ||
            _generatePointBorderXYOffeset != generatePointBorderXYOffeset
            )
        {
            _minLocationDistance = minLocationDistance;
            _generatePointYRangeMin = generatePointYRange.x;
            _generatePointYRangeMax = generatePointYRange.y;

            if (generatePointBorderXYOffeset > terrainSize * 0.6f)
            {
                generatePointBorderXYOffeset = terrainSize * 0.6f;
            }
            if (generatePointBorderXYOffeset < 0)
            {
                generatePointBorderXYOffeset = 0;
            }
            _generatePointBorderXYOffeset = generatePointBorderXYOffeset;

            // DestroyLocations();
            GeneratePoints();
        }

        if (generateLocations && locationCount > 0)
        {
            generateLocations = false;
            InstantiateLocations();
        }

        if (bSaveMesh)
        {
            bSaveMesh = false;
            SaveMeshAsset(mesh, "New Terrain Mesh");
        }
    }

    [SerializeField] private bool bSaveMesh = false;
    void SaveMeshAsset(Mesh mesh, string assetName)
    {
        // Create a new mesh asset
        Mesh meshAsset = Instantiate(mesh) as Mesh;
        meshAsset.name = assetName;

        // Save the mesh asset to the project
        AssetDatabase.CreateAsset(meshAsset, "Assets/Terrain/" + assetName + ".asset");
        AssetDatabase.SaveAssets();
    }

    [SerializeField] private bool generateLocations;
    [SerializeField] private GameObject proceduralPlacePrefab;
    List<ProceduralCityGenerator> proceduralLocations = new List<ProceduralCityGenerator>();

    // void DestroyLocations()
    // {
    //     // Destroy any existing objects in the array
    //     foreach (ProceduralCityGenerator obj in proceduralLocations)
    //     {
    //         Destroy(obj.gameObject);
    //     }
    //     // Clear the array
    //     proceduralLocations.Clear();
    // }

    void InstantiateLocations()
    {
        // DestroyLocations();

        // Instantiate new objects at the points and store them in the array
        foreach (Vector3 point in locationPoints)
        {
            Vector3 scale = transform.lossyScale;
            Vector3 pos = transform.TransformPoint(point);

            GameObject newObject = Instantiate(proceduralPlacePrefab, pos, Quaternion.identity);
            ProceduralCityGenerator generator = newObject.GetComponent<ProceduralCityGenerator>();
            float halfSize = generator.terrainSize * 0.5f;

            Vector3 newPos = pos - (proceduralPlacePrefab.transform.position);
            float x = (pos.x - halfSize);
            float z = (pos.z - halfSize);
            newObject.transform.position = new Vector3(x, pos.y * scale.y, z);

            // Debug.Log("halfSize: " + halfSize);
            proceduralLocations.Add(generator);
            newObject.transform.SetParent(this.gameObject.transform);
        }
    }

    void UpdatePlaceTerrains()
    {

    }
}