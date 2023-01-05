using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using UnityEditor;
using System.Linq;

[ExecuteInEditMode]
public class ProceduralTerrainMesh7 : MonoBehaviour
{
    public FastNoiseUnity fastNoiseUnity_layer1;
    public FastNoiseUnity fastNoiseUnity_layer2;
    public MeshFilter meshFilter;

    public enum NoiseType { Perlin, Simplex, Value }

    public NoiseType noiseType;

    struct Vertex
    {
        public Vector3 position;
    }
    // struct VerticleData
    // {
    //     public VerticleData(Vector3 _pos, int _index)
    //     {
    //         index = _index;
    //         position = _pos;
    //     }
    //     public Vector3 position;
    //     public int index;
    // }

    [Range(8, 256)] public int baseTerrainSize = 64;
    [Range(8, 256)] public int terrainSize = 64;
    [Range(-32, 72)] public float terrainHeight = 24f;
    public float noiseScale = 32f;
    [Range(-2f, 0.75f)] public float persistence = 0.45f;
    [Range(-1f, 2.6f)] public float lacunarity = 2f;
    [Range(1f, 128f)] public int octaves = 6;

    public AnimationCurve flattenCurve;

    public bool bFlattenCurve;
    public AnimationCurve terrainFlattenCurve;

    [Header("Point Marker Settings")]
    [Range(0, 24)][SerializeField] private int numPoints = 4;
    [Range(0, 12)][SerializeField] private float minPointDistance = 2f;
    [SerializeField] private float minVerticePointLevelRadius = 3f;
    [SerializeField] private float maxVerticePointLevelRadius = 9f;
    [SerializeField] private float generatePointBorderXYOffeset = 2f;
    [SerializeField] private Vector2 generatePointYRange = new Vector2(0, 1);
    [SerializeField] private Color pointColor = Color.red;
    [SerializeField] private Vector3[] points;


    [Header("Layer 2 Settings")]
    [Range(-32, 72)] public float terrainHeight_L2 = -0.3f;
    [Range(-2f, 0.75f)] public float persistence_L2 = 0.5f;
    [Range(0, 4)] public int vertexMult_L2 = 1;


    [Header("Cluster Marker Settings")]
    [SerializeField] private bool enableCityBlockPlotPoints;
    [Range(0.1f, 48f)][SerializeField] private float clusterDistanceMax = 7f;
    [SerializeField] private int clusterCount = 0;


    #region Saved State
    float _terrainSize;
    float _terrainHeight;
    float _minPointDistance;
    float _generatePointYRangeMin;
    float _generatePointYRangeMax;
    float _generatePointBorderXYOffeset;
    float _clusterDistanceMax;
    float _vertexMult_L2;

    #endregion

    Mesh mesh;
    private Vertex[] vertices;
    private float[] elevations;

    List<Vector3> cityLandPlotPoints;
    List<PointCluster> cityLandPointClusters;

    Dictionary<Vector3, List<Vector3>> sitePointVertices; // Stores the index of the vertices

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }
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

            if (!meshFilter) meshFilter = GetComponent<MeshFilter>();

            // Generate the mesh data
            vertices = GenerateVertices(points);
            Vector3[] vertices_L1 = GetVertexPositions(vertices);
            mesh.vertices = vertices_L1;
            mesh.triangles = GenerateTriangles();
            mesh.uv = GenerateUVs(vertices_L1);

            // vertices = GenerateVertices(points);

            // Vector3[] vertices_L1 = GetVertexPositions(GenerateVertices(points));
            // Vector3[] vertices_L2 = GenerateLayer2Vertices();

            // Vector3[] newVertices = vertices_L1.Concat(vertices_L2).ToArray();
            // int[] triangles = CreateTriangles(newVertices);

            // mesh.vertices = newVertices;
            // mesh.triangles = triangles;
            // mesh.uv = GenerateUVs(newVertices);

            // Recalculate the normals to ensure proper lighting
            mesh.RecalculateNormals();
            // Apply the mesh data to the MeshFilter component
            meshFilter.mesh = mesh;


            Mesh mesh_L2 = CreateLayer2Mesh();

            // Combine the original mesh with the new mesh using the CombineMeshes method
            CombineInstance[] combine = new CombineInstance[2];
            combine[0].mesh = mesh;
            combine[1].mesh = mesh_L2;
            combine[1].transform = Matrix4x4.identity;

            Mesh finalMesh = new Mesh();
            finalMesh.CombineMeshes(combine, true, false);

            finalMesh.RecalculateNormals();
            // Apply the mesh data to the MeshFilter component
            meshFilter.mesh = finalMesh;

            // // Create Layer 2 mesh data
            // Vector3[] vertices_L2 = GenerateLayer2Vertices();
            // int[] triangles_L2 = CreateLayer2Triangles(vertices_L2);

            // mesh = AddTrianglesToMesh(vertices_L2, triangles_L2, mesh);

            // // Recalculate the normals to ensure proper lighting
            // mesh.RecalculateNormals();
            // // Apply the mesh data to the MeshFilter component
            // meshFilter.mesh = mesh;

            // Calculate the elevations of the vertices
            // elevations = new float[vertices.Length];
            // for (int i = 0; i < vertices.Length; i++)
            // {
            //     elevations[i] = vertices[i].position.y;
            // }
        }
    }

    // Vertex[] GenerateVertices(Vector3[] points)
    // {
    //     FastNoise fastNoise = fastNoiseUnity_layer1.fastNoise;

    //     // Create an array to store the vertex data
    //     Vertex[] vertices = new Vertex[terrainSize * terrainSize];

    //     Dictionary<Vector3, List<VerticleData>> newSitePointVertices = new Dictionary<Vector3, List<VerticleData>>();

    //     // Iterate through the vertex data and set the height and color of each point
    //     for (int x = 0; x < terrainSize; x++)
    //     {
    //         for (int y = 0; y < terrainSize; y++)
    //         {
    //             // Calculate the height of the current point
    //             float noiseHeight = 0;
    //             float frequency = 1;
    //             float amplitude = 1;
    //             float sampleX = x / noiseScale * frequency;

    //             for (int i = 0; i < octaves; i++)
    //             {
    //                 float sampleY = y / noiseScale * frequency;

    //                 float noiseValue = 0;
    //                 if (noiseType == NoiseType.Perlin)
    //                 {
    //                     noiseValue = Mathf.PerlinNoise(sampleX, sampleY);
    //                 }

    //                 else if (noiseType == NoiseType.Simplex)
    //                 {
    //                     noiseValue = (float)fastNoise.GetNoise(x, y);
    //                 }

    //                 noiseHeight += noiseValue * amplitude;
    //                 amplitude *= persistence;
    //                 frequency *= lacunarity;
    //             }

    //             int verticeIndex = x + y * terrainSize;

    //             // Set the height and color of the current point
    //             vertices[verticeIndex] = new Vertex
    //             {
    //                 position = new Vector3(x, noiseHeight * terrainHeight, y),
    //             };

    //             if (points != null && points.Length > 0)
    //             {
    //                 // Find the nearest point to the current vertex
    //                 Vector3 nearestPoint = points[0];
    //                 float nearestDist = float.MaxValue;
    //                 for (int i = 0; i < points.Length; i++)
    //                 {
    //                     float dist = Vector2.Distance(new Vector2(x, y), new Vector2(points[i].x, points[i].z));
    //                     if (dist < nearestDist)
    //                     {
    //                         nearestDist = dist;
    //                         nearestPoint = points[i];
    //                     }
    //                 }

    //                 if (nearestDist < minVerticePointLevelRadius)
    //                 {
    //                     vertices[verticeIndex].position.y = nearestPoint.y;
    //                     if (!newSitePointVertices.ContainsKey(nearestPoint))
    //                     {
    //                         newSitePointVertices.Add(nearestPoint, new List<VerticleData>());
    //                     }
    //                     newSitePointVertices[nearestPoint].Add(new VerticleData(vertices[verticeIndex].position, verticeIndex));

    //                     // for (int i = 0; i < vertexMult_L2; i++)
    //                     // {
    //                     //     vertices[verticeIndex] = new Vertex
    //                     //     {
    //                     //         position = new Vector3(x, noiseHeight * terrainHeight, y),
    //                     //     };
    //                     // }
    //                 }

    //                 // If the distance to the nearest point is within the minVerticePointLevelRadius,
    //                 // gradually smooth the height of the vertex to match the elevation of the nearest point
    //                 else if (nearestDist <= maxVerticePointLevelRadius)
    //                 {
    //                     // Calculate the distance as a percentage of the minVerticePointLevelRadius
    //                     float distPercent = nearestDist / maxVerticePointLevelRadius;
    //                     // Evaluate the Animation curve using the distance percentage
    //                     float curveValue = flattenCurve.Evaluate(distPercent);
    //                     // Set the height of the vertex to be a blend of the point's y position and the original height
    //                     vertices[verticeIndex].position.y = Mathf.Lerp(vertices[verticeIndex].position.y, nearestPoint.y, curveValue);
    //                 }
    //             }
    //             else
    //             {
    //                 if (bFlattenCurve)
    //                 {
    //                     // Use the flatten curve to control the height of the terrain
    //                     vertices[verticeIndex].position.y = terrainFlattenCurve.Evaluate(noiseHeight) * terrainHeight;
    //                 }
    //             }
    //         }
    //     }

    //     sitePointVertices = newSitePointVertices;

    //     return vertices;
    // }


    Vertex[] GenerateVertices(Vector3[] points)
    {
        FastNoise fastNoise = fastNoiseUnity_layer1.fastNoise;

        // Create an array to store the vertex data
        Vertex[] vertices = new Vertex[terrainSize * terrainSize];

        Dictionary<Vector3, List<Vector3>> newSitePointVertices = new Dictionary<Vector3, List<Vector3>>();

        // Iterate through the vertex data and set the height and color of each point
        for (int x = 0; x < terrainSize; x++)
        {
            for (int y = 0; y < terrainSize; y++)
            {
                // Calculate the height of the current point
                float noiseHeight = 0;
                float frequency = 1;
                float amplitude = 1;
                float sampleX = x / noiseScale * frequency;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleY = y / noiseScale * frequency;

                    float noiseValue = 0;
                    if (noiseType == NoiseType.Perlin)
                    {
                        noiseValue = Mathf.PerlinNoise(sampleX, sampleY);
                    }

                    else if (noiseType == NoiseType.Simplex)
                    {
                        noiseValue = (float)fastNoise.GetNoise(x, y);
                    }

                    noiseHeight += noiseValue * amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                int verticeIndex = x + y * terrainSize;

                // Set the height and color of the current point
                vertices[verticeIndex] = new Vertex
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
                        vertices[verticeIndex].position.y = nearestPoint.y;

                        if (!newSitePointVertices.ContainsKey(nearestPoint))
                        {
                            newSitePointVertices.Add(nearestPoint, new List<Vector3>());
                        }
                        newSitePointVertices[nearestPoint].Add(vertices[verticeIndex].position);

                        // If the distance to the nearest point is within the maxVerticePointLevelRadius but outside the minVerticePointLevelRadius,
                        // add a set number of new vertices at the point
                        // for (int i = 0; i < vertexMult_L2; i++)
                        // {

                        //     // Add the new vertex to the vertices array
                        //     Vertex newVertex = new Vertex
                        //     {
                        //         position = new Vector3(x + (1 + i * 0.2f), noiseHeight * terrainHeight, y + (1 + i * 0.2f)),
                        //     };
                        //     Array.Resize(ref vertices, vertices.Length + 1);
                        //     vertices[vertices.Length - 1] = newVertex;

                        //     newSitePointVertices[nearestPoint].Add(new VerticleData(nearestPoint, vertices.Length - 1));
                        // }

                    }

                    // If the distance to the nearest point is within the minVerticePointLevelRadius,
                    // gradually smooth the height of the vertex to match the elevation of the nearest point
                    else if (nearestDist <= maxVerticePointLevelRadius)
                    {
                        // Calculate the distance as a percentage of the minVerticePointLevelRadius
                        float distPercent = nearestDist / maxVerticePointLevelRadius;
                        // Evaluate the Animation curve using the distance percentage
                        float curveValue = flattenCurve.Evaluate(distPercent);
                        // Set the height of the vertex to be a blend of the point's y position and the original height
                        vertices[verticeIndex].position.y = Mathf.Lerp(vertices[verticeIndex].position.y, nearestPoint.y, curveValue);
                    }
                }
                else
                {
                    if (bFlattenCurve)
                    {
                        // Use the flatten curve to control the height of the terrain
                        vertices[verticeIndex].position.y = terrainFlattenCurve.Evaluate(noiseHeight) * terrainHeight;
                    }
                }
            }
        }

        sitePointVertices = newSitePointVertices;

        return vertices;
    }




    Vector3 UpdateLayer2Vertice(Vector3 vertice, FastNoise fastNoise)
    {
        float x = vertice.x;
        float z = vertice.z;

        // Calculate the height of the current point
        float noiseHeight = 0;
        float frequency = 1;
        float amplitude = 1;
        float sampleX = x / noiseScale * frequency;

        for (int i = 0; i < octaves; i++)
        {
            float sampleY = z / noiseScale * frequency;

            float noiseValue = 0;

            noiseValue = (float)fastNoise.GetNoise(x, z);

            noiseHeight += noiseValue * amplitude;
            amplitude *= persistence_L2;
            frequency *= lacunarity;
        }

        return new Vector3(x, noiseHeight * terrainHeight_L2, z);
    }

    // Vertex UpdateLayer2Vertice(Vertex vertice, FastNoise fastNoise)
    // {
    //     float x = vertice.position.x;
    //     float z = vertice.position.z;

    //     // Calculate the height of the current point
    //     float noiseHeight = 0;
    //     float frequency = 1;
    //     float amplitude = 1;
    //     float sampleX = x / noiseScale * frequency;

    //     for (int i = 0; i < octaves; i++)
    //     {
    //         float sampleY = z / noiseScale * frequency;

    //         float noiseValue = 0;

    //         noiseValue = (float)fastNoise.GetNoise(x, z);

    //         noiseHeight += noiseValue * amplitude;
    //         amplitude *= persistence_L2;
    //         frequency *= lacunarity;
    //     }

    //     // Set the height and color of the current point
    //     Vertex newVertice = new Vertex
    //     {
    //         position = new Vector3(x, noiseHeight * terrainHeight_L2, z),
    //     };
    //     return newVertice;
    // }

    // Vertex[] GenerateLayer2Vertices(Vertex[] vertices)
    // {
    //     if (sitePointVertices != null && sitePointVertices.Count > 0)
    //     {
    //         FastNoise fastNoise = fastNoiseUnity_layer2.fastNoise;

    //         foreach (KeyValuePair<Vector3, List<VerticleData>> entry in sitePointVertices)
    //         {
    //             Vector3 position = entry.Key;
    //             List<VerticleData> lists = entry.Value;

    //             foreach (VerticleData item in lists)
    //             {
    //                 Vertex vertex = vertices[item.index];
    //                 vertices[item.index] = UpdateLayer2Vertice(vertex, fastNoise);
    //             }
    //         }
    //     }
    //     return vertices;
    // }

    // Vertex[] GenerateLayer2Vertices(Vertex[] vertices)
    // {
    //     if (sitePointVertices != null && sitePointVertices.Count > 0)
    //     {
    //         FastNoise fastNoise = fastNoiseUnity_layer2.fastNoise;

    //         foreach (KeyValuePair<Vector3, List<VerticleData>> entry in sitePointVertices)
    //         {
    //             Vector3 position = entry.Key;
    //             List<VerticleData> lists = entry.Value;

    //             // Spread the lists around the position within the radius
    //             lists = SpreadVerticesAroundPosition(lists, position, minVerticePointLevelRadius);

    //             foreach (VerticleData item in lists)
    //             {
    //                 Vertex vertex = vertices[item.index];
    //                 vertices[item.index] = UpdateLayer2Vertice(vertex, fastNoise);
    //             }
    //         }
    //     }
    //     return vertices;
    // }

    // Vertex[] GenerateLayer2Vertices(Vertex[] vertices)
    // {
    //     if (sitePointVertices != null && sitePointVertices.Count > 0)
    //     {
    //         FastNoise fastNoise = fastNoiseUnity_layer2.fastNoise;

    //         foreach (KeyValuePair<Vector3, List<VerticleData>> entry in sitePointVertices)
    //         {
    //             Vector3 position = entry.Key;
    //             List<VerticleData> lists = entry.Value;

    //             // Spread the lists around the position within the radius
    //             lists = SpreadVerticesAroundPosition(lists, position, minVerticePointLevelRadius);

    //             foreach (VerticleData item in lists)
    //             {
    //                 Vertex vertex = vertices[item.index];
    //                 vertices[item.index] = UpdateLayer2Vertice(vertex, fastNoise);
    //             }
    //         }
    //     }
    //     return vertices;
    // }

    Vector3[] GenerateLayer2Vertices()
    {
        List<Vector3> newVertices = new List<Vector3>();

        if (points != null && points.Length > 0)
        {
            foreach (Vector3 point in points)
            {
                List<Vector3> newPointVertices = CreateVerticesAroundPoint(point, minVerticePointLevelRadius);
                newVertices.AddRange(newPointVertices);

                sitePointVertices[point].AddRange(newPointVertices);

                Debug.Log("Point - vertices: " + newPointVertices.Count);
            }
        }
        return newVertices.ToArray();
    }

    // List<VerticleData> SpreadVerticesAroundPosition(List<VerticleData> vertices, Vector3 position, float radius)
    // {
    //     // Calculate the number of rows and columns of vertices to generate
    //     int rows = (int)(radius / 0.2f);
    //     int cols = rows;
    //     // Calculate the distance between each row and column
    //     float rowDistance = radius / rows;
    //     float colDistance = rowDistance;
    //     // Iterate through the rows and columns and generate a vertex at each position
    //     for (int row = 0; row < rows; row++)
    //     {
    //         for (int col = 0; col < cols; col++)
    //         {
    //             // Calculate the position of the current vertex
    //             float xPos = position.x - radius / 2 + row * rowDistance;
    //             float zPos = position.z - radius / 2 + col * colDistance;
    //             Vector3 vertPos = new Vector3(xPos, position.y, zPos);
    //             // Add the new vertex to the vertices list
    //             vertices.Add(new VerticleData(vertPos, 0));
    //         }
    //     }
    //     return vertices;
    // }

    List<Vector3> CreateVerticesAroundPoint(Vector3 position, float radius)
    {
        List<Vector3> newVertices = new List<Vector3>();

        FastNoise fastNoise = fastNoiseUnity_layer2.fastNoise;

        // Calculate the number of rows and columns of vertices to generate
        int rows = (int)(radius / 0.5f);
        int cols = rows;
        // Calculate the distance between each row and column
        float rowDistance = radius / rows;
        float colDistance = rowDistance;
        // Iterate through the rows and columns and generate a vertex at each position
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                // Calculate the position of the current vertex
                float xPos = position.x - radius / 2 + row * rowDistance;
                float zPos = position.z - radius / 2 + col * colDistance;
                Vector3 vertPos = new Vector3(xPos, position.y, zPos);
                // Add the new vertex to the vertices list

                newVertices.Add(UpdateLayer2Vertice(vertPos, fastNoise));
            }
        }
        return newVertices;
    }

    Mesh CreateLayer2Mesh()
    {
        Mesh newMesh = new Mesh();

        Vector3[] vertices_L2 = GenerateLayer2Vertices();
        int[] triangles_L2 = CreateTriangles(vertices_L2);
        newMesh.vertices = vertices_L2;
        newMesh.triangles = triangles_L2;
        newMesh.uv = GenerateUVs(vertices_L2);

        return newMesh;
    }

    int[] CreateTriangles(Vector3[] vertices)
    {
        int numVertices = vertices.Length;
        int numTriangles = numVertices - 2;
        int[] triangles = new int[numTriangles * 3];
        for (int i = 0; i < numTriangles; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }
        return triangles;
    }
    Mesh AddTrianglesToMesh(Vector3[] newVertices, int[] newTriangles, Mesh mesh)
    {
        Mesh resultMesh = new Mesh();

        // Create a new mesh to hold the new triangles
        Mesh newMesh = new Mesh();
        newMesh.vertices = newVertices;
        newMesh.triangles = newTriangles;

        // Combine the original mesh with the new mesh using the CombineMeshes method
        CombineInstance[] combine = new CombineInstance[2];
        combine[0].mesh = mesh;
        combine[1].mesh = newMesh;
        combine[1].transform = Matrix4x4.identity;

        resultMesh.CombineMeshes(combine, true, false);
        return resultMesh;
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
        int[] triangles = new int[(terrainSize - 1) * (terrainSize - 1) * (6)];

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

    Vector2[] GenerateUVs(Vector3[] vertices)
    {
        // Create an array to store the UV data
        Vector2[] uvs = new Vector2[vertices.Length];

        // Iterate through the vertices and map the positions to UV coordinates
        for (int i = 0; i < vertices.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
        }

        return uvs;
    }

    // Vector2[] GenerateUVs()
    // {
    //     // Create an array to store the UV data
    //     Vector2[] uvs = new Vector2[vertices.Length];

    //     // Iterate through the vertices and set the UVs of each vertex
    //     for (int x = 0; x < terrainSize; x++)
    //     {
    //         for (int y = 0; y < terrainSize; y++)
    //         {
    //             uvs[x + y * terrainSize] = new Vector2(x / (float)terrainSize, y / (float)terrainSize);
    //         }
    //     }

    //     return uvs;
    // }

    void GeneratePoints()
    {
        points = new Vector3[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            // Generate a random position within the bounds of the terrain
            float xPos = UnityEngine.Random.Range(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset);
            float zPos = UnityEngine.Random.Range(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset);
            float yPos = UnityEngine.Random.Range(generatePointYRange.x, generatePointYRange.y);

            points[i] = new Vector3(xPos, yPos, zPos);

            // Use the transform scale to check distance
            Vector3 scale = transform.lossyScale;

            // Ensure that the point is a minimum distance away from all other points
            bool tooClose = false;
            do
            {
                tooClose = false;
                foreach (Vector3 point in points)
                {
                    if (point != points[i] && Vector3.Distance(point, points[i]) < minPointDistance * scale.x)
                    {
                        tooClose = true;
                        xPos = UnityEngine.Random.Range(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset);
                        zPos = UnityEngine.Random.Range(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset);
                        yPos = UnityEngine.Random.Range(generatePointYRange.x, generatePointYRange.y);
                        points[i] = new Vector3(xPos, 0, zPos);
                        break;
                    }
                }
            } while (tooClose);
        }
    }


    public float minHeight = 0f;
    public float maxHeight = 1f;
    public AnimationCurve pointHeightRange;

    List<Vector3> CalculatePlacementPoints()
    {
        // Sort the elevations and vertices arrays
        elevations = elevations.OrderBy(x => x).ToArray();
        vertices = vertices.OrderBy(x => x.position.y).ToArray();

        // Calculate the scale of the mesh transform
        Vector3 scale = transform.lossyScale;

        // Create a list to store the placement points
        List<Vector3> placementPoints = new List<Vector3>();

        // Iterate through the vertices and add the points that meet the placement rules
        for (int i = 0; i < vertices.Length; i++)
        {
            // Calculate the normalized height of the vertex, taking into account the x and z scale of the mesh transform
            float normalizedHeight = (vertices[i].position.y / scale.y - minHeight) / (maxHeight - minHeight);
            normalizedHeight = Mathf.Clamp(normalizedHeight, 0, 1);

            float height = pointHeightRange.Evaluate(normalizedHeight);
            if (height > 0)
            {
                // Transform the position of the vertex to world space
                Vector3 worldPosition = transform.TransformPoint(vertices[i].position);
                placementPoints.Add(worldPosition);
            }
        }

        return placementPoints;
    }

    private List<PointCluster> ConsolidatePointsIntoClusters(List<Vector3> points)
    {
        List<PointCluster> clusters = new List<PointCluster>();
        Vector3 scale = transform.lossyScale;

        points = points.OrderBy(pos => pos.x).ThenBy(pos => pos.z).ToList();

        // Iterate through all of the points
        for (int i = 0; i < points.Count; i++)
        {
            // Check if the current point is within an existing cluster
            bool foundCluster = false;
            foreach (PointCluster cluster in clusters)
            {
                // Calculate the distance between the current point and the center of the cluster
                float distance = Vector2.Distance(new Vector2(cluster.center.x, cluster.center.z), new Vector2(points[i].x, points[i].z));
                if (distance < clusterDistanceMax)
                {
                    // Add the point to the cluster and update the center
                    cluster.AddPoint(points[i]);
                    foundCluster = true;
                    break;
                }

                List<Vector3> clusterPoints = cluster.GetPoints();
                foreach (Vector3 clusterPoint in clusterPoints)
                {
                    distance = Vector2.Distance(new Vector2(clusterPoint.x, clusterPoint.z), new Vector2(points[i].x, points[i].z));
                    if (distance < clusterDistanceMax)
                    {
                        // Add the point to the cluster and update the center
                        cluster.AddPoint(points[i]);
                        foundCluster = true;
                        break;
                    }
                }


            }

            // If the point was not within an existing cluster, create a new cluster for it
            if (!foundCluster)
            {
                PointCluster newCluster = new PointCluster(points[i]);
                clusters.Add(newCluster);
            }
        }

        return clusters;
    }

    private void UpdateCityLandPlotPoints()
    {
        cityLandPlotPoints = CalculatePlacementPoints();

        cityLandPointClusters = ConsolidatePointsIntoClusters(cityLandPlotPoints);

        // Add unique color for each cluster
        if (cityLandPointClusters.Count > 0)
        {
            clusterCount = cityLandPointClusters.Count;

            HashSet<Color> generatedColors = new HashSet<Color>();

            foreach (PointCluster cluster in cityLandPointClusters)
            {
                Color randomColor;

                // Generate a random color until a unique color is found
                do
                {
                    // Generate a random hue value between 0 and 1
                    float hue = UnityEngine.Random.value;

                    // Generate a random saturation value between 0.5 and 1
                    float saturation = UnityEngine.Random.Range(0.5f, 1f);

                    // Generate a random value value between 0.5 and 1
                    float value = UnityEngine.Random.Range(0.5f, 1f);

                    // Convert the HSV values to RGB
                    randomColor = Color.HSVToRGB(hue, saturation, value);
                } while (generatedColors.Contains(randomColor));

                // Add the unique color to the hash set
                generatedColors.Add(randomColor);
                cluster.color = randomColor;
            }
        }

    }

    [Header("Debug Settings")]
    [SerializeField] private bool debug_showPoints = true;
    [SerializeField] private bool debug_minPointDistance;
    [SerializeField] private bool debug_pointLevelRadius;
    [SerializeField] private bool debug_editorUpdateTerrainOnce;
    private bool _editorUpdate;


    [SerializeField] private bool enableSiteVericlePoints;

    public void DrawSpheres()
    {
        foreach (KeyValuePair<Vector3, List<Vector3>> entry in sitePointVertices)
        {
            Vector3 position = entry.Key;
            List<Vector3> lists = entry.Value;

            // Debug.Log("lists length: " + lists.Count);

            foreach (Vector3 point in lists)
            {
                // Debug.Log("lists pos: " + item.position);

                Vector3 pointWorldPos = transform.TransformPoint(point);
                Gizmos.DrawSphere(pointWorldPos, 0.3f);
            }
        }
    }


    void OnDrawGizmos()
    {
        if (!debug_showPoints && !debug_pointLevelRadius && !debug_minPointDistance) return;

        Gizmos.color = pointColor;

        if (enableSiteVericlePoints)
        {
            if (sitePointVertices != null && sitePointVertices.Count > 0)
            {
                DrawSpheres();
            }
        }

        // Draw the cluster points
        if (enableCityBlockPlotPoints)
        {
            if (cityLandPointClusters != null && cityLandPointClusters.Count > 0)
            {


                foreach (PointCluster cluster in cityLandPointClusters)
                {

                    Gizmos.color = cluster.color;

                    List<Vector3> pts = cluster.GetPoints();
                    foreach (Vector3 point in pts)
                    {
                        Gizmos.DrawSphere(point, 0.25f);
                    }

                    List<Vector3> borderPts = cluster.GetBorderPoints();
                    foreach (Vector3 point in borderPts)
                    {
                        Gizmos.DrawWireSphere(point, 2f);
                        // Gizmos.DrawSphere(point, 0.6f);
                    }

                    Gizmos.DrawSphere(cluster.center, 1f);
                    Gizmos.DrawWireSphere(cluster.center, cluster.GetBoundsRadius());
                }


            }
            else if (cityLandPlotPoints.Count > 0)
            {
                foreach (Vector3 point in cityLandPlotPoints)
                {
                    Gizmos.DrawSphere(point, 0.25f);
                }
            }
        }

        // Draw a sphere at each point's position
        foreach (Vector3 point in points)
        {
            Vector3 scale = transform.lossyScale;
            Vector3 pointWorldPos = transform.TransformPoint(point);

            if (debug_showPoints)
            {
                // Gizmos.DrawSphere(pointWorldPos, 6f);
            }
            if (debug_minPointDistance)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(pointWorldPos, minPointDistance * scale.x);
            }
            if (debug_pointLevelRadius)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(pointWorldPos, minVerticePointLevelRadius * scale.x);
            }

        }
    }

    void OnValidate()
    {
        _editorUpdate = true;

        if (!meshFilter) meshFilter = GetComponent<MeshFilter>();

        if (numPoints != points.Length ||
            _minPointDistance != minPointDistance ||
            _generatePointYRangeMin != generatePointYRange.x ||
            _generatePointYRangeMax != generatePointYRange.y ||
            _generatePointBorderXYOffeset != generatePointBorderXYOffeset
            )
        {
            _minPointDistance = minPointDistance;
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

            GeneratePoints();
        }

        if (enableCityBlockPlotPoints && _terrainHeight != terrainHeight || _terrainSize != terrainSize || _clusterDistanceMax != clusterDistanceMax)
        {
            _terrainHeight = terrainHeight;
            _terrainSize = terrainSize;
            _clusterDistanceMax = clusterDistanceMax;

            UpdateCityLandPlotPoints();
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
}