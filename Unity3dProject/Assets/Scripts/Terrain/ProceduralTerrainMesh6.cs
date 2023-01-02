using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[ExecuteInEditMode]
public class ProceduralTerrainMesh6 : MonoBehaviour
{
    public FastNoiseUnity fastNoiseUnity;

    public enum NoiseType { Perlin, Simplex, Value }

    public NoiseType noiseType;

    struct Vertex
    {
        public Vector3 position;
    }

    public int terrainSize = 64;
    public float terrainHeight = 10f;
    public float noiseScale = 50f;
    public int octaves = 6;
    [Range(-2f, 0.75f)] public float persistence = 0.45f;
    [Range(-1f, 2.6f)] public float lacunarity = 2f;
    public bool bFlattenCurve;
    public AnimationCurve flattenCurve;

    [Header("Point Marker Settings")]
    [SerializeField] private int numPoints = 10;
    [SerializeField] private float minPointDistance = 6f;
    [SerializeField] private Vector2 generatePointYRange = new Vector2(0, 1);
    [SerializeField] private float generatePointBorderXYOffeset = 2f;
    [SerializeField] private float minVerticePointLevelRadius = 12f;
    [SerializeField] private float maxVerticePointLevelRadius = 24f;
    [SerializeField] private float verticePointLevelRadiusOutsideMult = 1.25f;
    [SerializeField] private Color pointColor = Color.red;
    [SerializeField] private Vector3[] points;

    #region Saved State
    float _minPointDistance;
    float _generatePointYRangeMin;
    float _generatePointYRangeMax;
    float _generatePointBorderXYOffeset;
    #endregion

    Mesh mesh;
    Vertex[] vertices;
    float[] elevations;

    void Start()
    {
        mesh = new Mesh();
        mesh.name = "Procedural Terrain";

        Update();
    }

    void Update()
    {
        // Debug.Log("Updating");

        // Generate the mesh data
        vertices = GenerateVertices(points);
        mesh.vertices = GetVertexPositions(vertices);
        mesh.triangles = GenerateTriangles();
        mesh.uv = GenerateUVs();
        // Recalculate the normals to ensure proper lighting
        mesh.RecalculateNormals();
        // Apply the mesh data to the MeshFilter component
        GetComponent<MeshFilter>().mesh = mesh;

        // Calculate the elevations of the vertices
        elevations = new float[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            elevations[i] = vertices[i].position.y;
        }
    }

    // Vertex[] GenerateVertices(Vector3[] points)
    // {
    //     FastNoise fastNoise = fastNoiseUnity.fastNoise;

    //     // Create an array to store the vertex data
    //     Vertex[] vertices = new Vertex[terrainSize * terrainSize];

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

    //             // Set the height and color of the current point
    //             vertices[x + y * terrainSize] = new Vertex
    //             {
    //                 position = new Vector3(x, noiseHeight * terrainHeight, y),
    //             };

    //             // Set the height of the terrain at the points' x and z positions to be the same as their y position
    //             for (int i = 0; i < points.Length; i++)
    //             {
    //                 float dist = Vector2.Distance(new Vector2(x, y), new Vector2(points[i].x, points[i].z));
    //                 if (dist < minVerticePointLevelRadius)
    //                 {
    //                     vertices[x + y * terrainSize].position.y = points[i].y;
    //                 }
    //             }

    //             if (bFlattenCurve)
    //             {
    //                 // Use the flatten curve to control the height of the terrain
    //                 vertices[x + y * terrainSize].position.y = flattenCurve.Evaluate(noiseHeight) * terrainHeight;
    //             }
    //         }
    //     }
    //     return vertices;
    // }

    // Vertex[] GenerateVertices(Vector3[] points)
    // {
    //     FastNoise fastNoise = fastNoiseUnity.fastNoise;

    //     // Create an array to store the vertex data
    //     Vertex[] vertices = new Vertex[terrainSize * terrainSize];

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

    //             // Set the height and color of the current point
    //             vertices[x + y * terrainSize] = new Vertex
    //             {
    //                 position = new Vector3(x, noiseHeight * terrainHeight, y),
    //             };

    //             // Set the height of the terrain at the points' x and z positions to be the same as their y position
    //             // Gradually flatten the vertices using an Animation curve
    //             for (int i = 0; i < points.Length; i++)
    //             {
    //                 float dist = Vector2.Distance(new Vector2(x, y), new Vector2(points[i].x, points[i].z));
    //                 if (dist < minVerticePointLevelRadius)
    //                 {
    //                     // Calculate the distance as a percentage of the minVerticePointLevelRadius
    //                     float distPercent = dist / minVerticePointLevelRadius;
    //                     // Evaluate the Animation curve using the distance percentage
    //                     float curveValue = flattenCurve.Evaluate(distPercent);
    //                     // Set the height of the vertex to be a blend of the point's y position and the original height
    //                     // vertices[x + y * terrainSize].position.y = Mathf.Lerp(vertices[x + y * terrainSize].position.y, points[i].y, curveValue);
    //                     vertices[x + y * terrainSize].position.y = curveValue * terrainHeight;
    //                 }
    //             }
    //         }
    //     }
    //     return vertices;
    // }
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

                // Set the height and color of the current point
                vertices[x + y * terrainSize] = new Vertex
                {
                    position = new Vector3(x, noiseHeight * terrainHeight, y),
                };

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
                    float curveValue = flattenCurve.Evaluate(distPercent);
                    // Set the height of the vertex to be a blend of the point's y position and the original height
                    vertices[x + y * terrainSize].position.y = Mathf.Lerp(vertices[x + y * terrainSize].position.y, nearestPoint.y, curveValue);
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
        points = new Vector3[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            // Generate a random position within the bounds of the terrain
            float xPos = Random.Range(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset);
            float zPos = Random.Range(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset);
            float yPos = Random.Range(generatePointYRange.x, generatePointYRange.y);

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
                        xPos = Random.Range(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset);
                        zPos = Random.Range(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset);
                        yPos = Random.Range(generatePointYRange.x, generatePointYRange.y);
                        points[i] = new Vector3(xPos, 0, zPos);
                        break;
                    }
                }
            } while (tooClose);
        }
    }

    [Header("Debug Settings")]
    [SerializeField] private bool debug_showPoints = true;
    [SerializeField] private bool debug_minPointDistance;
    [SerializeField] private bool debug_pointLevelRadius;
    void OnDrawGizmos()
    {
        if (!debug_showPoints && !debug_pointLevelRadius && !debug_minPointDistance) return;

        // Draw a sphere at each point's position
        foreach (Vector3 point in points)
        {
            Vector3 scale = transform.lossyScale;
            Vector3 pointWorldPos = transform.TransformPoint(point);
            Gizmos.color = pointColor;

            if (debug_showPoints)
            {
                Gizmos.DrawSphere(pointWorldPos, 6f);
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
    }

}
