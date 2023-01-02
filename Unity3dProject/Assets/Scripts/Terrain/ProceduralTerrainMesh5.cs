using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[ExecuteInEditMode]
public class ProceduralTerrainMesh5 : MonoBehaviour
{
    public int terrainSize = 64;
    public int octaves = 6;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public float terrainHeight = 10f;
    public float noiseScale = 50f;

    public bool bFlattenCurve;
    public AnimationCurve flattenCurve;

    public float minHeight = 0f;
    public float maxHeight = 1f;
    public int numPoints = 10;
    public float minDistance = 4f;
    public float blendRadius = 0.1f;
    public float levelDistance = 6f;

    public Color pointColor = Color.red;

    Mesh mesh;
    Vertex[] vertices;
    [SerializeField] private Vector3[] points;

    void Start()
    {
        mesh = new Mesh();
        mesh.name = "Procedural Terrain";
        GeneratePoints();
        Update();
    }

    void Update()
    {
        // Generate the mesh data
        vertices = GenerateVertices();
        mesh.vertices = GetVertexPositions(vertices);
        mesh.triangles = GenerateTriangles();
        mesh.uv = GenerateUVs();
        // Recalculate the normals to ensure proper lighting
        mesh.RecalculateNormals();

        // UpdatePointElevation();

        // Apply the mesh data to the MeshFilter component
        GetComponent<MeshFilter>().mesh = mesh;

    }


    void UpdatePointElevation()
    {
        if (points.Length > 0)
        {
            for (int i = 0; i < points.Length; i++)
            {
                float height = GetHeightAtPoint(points[i]);
                // Clamp the height to the desired range
                height = Mathf.Clamp(height, minHeight, maxHeight);
                points[i] = new Vector3(points[i].x, height, points[i].z);
            }
        }
    }


    void GeneratePoints()
    {
        points = new Vector3[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            // Generate a random position within the bounds of the terrain
            float xPos = Random.Range(0, terrainSize);
            float zPos = Random.Range(0, terrainSize);

            // Scale the point position by the transform scale
            // points[i] = Vector3.Scale(new Vector3(xPos, 0, zPos), transform.localScale);
            points[i] = new Vector3(xPos, 0, zPos);

            // Ensure that the point is a minimum distance away from all other points
            bool tooClose = false;
            do
            {
                tooClose = false;
                foreach (Vector3 point in points)
                {
                    if (point != points[i] && Vector3.Distance(point, points[i]) < minDistance)
                    {
                        tooClose = true;
                        xPos = Random.Range(0, terrainSize);
                        zPos = Random.Range(0, terrainSize);
                        points[i] = new Vector3(xPos, 0, zPos);
                        break;
                    }
                }
                // Check if the point is within the minimum radius on the x and z axes
                if (Mathf.Abs(xPos) < blendRadius || Mathf.Abs(zPos) < blendRadius)
                {
                    tooClose = true;
                    xPos = Random.Range(0, terrainSize);
                    zPos = Random.Range(0, terrainSize);
                    points[i] = new Vector3(xPos, 0, zPos);
                }
            } while (tooClose);
        }
    }


    float GetHeightAtPoint(Vector3 point)
    {
        Mesh sharedMesh = GetComponent<MeshFilter>().sharedMesh;

        // Get the triangle that the point lies in
        int[] triangles = sharedMesh.triangles;
        Vector3[] vertices = sharedMesh.vertices;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];

            // Check if the point is within the triangle
            if (PointInTriangle(point, v0, v1, v2))
            {
                // Calculate the height of the point on the triangle
                float height = BarycentricInterpolation(point, v0, v1, v2);
                return height;
            }
        }

        return 0;
    }

    bool PointInTriangle(Vector3 point, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        // Calculate the barycentric coordinates of the point
        float s = CalculateTriangleArea(v0, v1, v2);
        float s1 = CalculateTriangleArea(point, v1, v2) / s;
        float s2 = CalculateTriangleArea(point, v2, v0) / s;
        float s3 = CalculateTriangleArea(point, v0, v1) / s;

        // Check if the point is within the triangle
        if (s1 >= 0 && s1 <= 1 && s2 >= 0 && s2 <= 1 && s3 >= 0 && s3 <= 1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    float CalculateTriangleArea(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        // Calculate the area of the triangle using the cross product
        float a = Vector3.Distance(v1, v2);
        float b = Vector3.Distance(v2, v3);
        float c = Vector3.Distance(v3, v1);
        float s = (a + b + c) / 2;
        float area = Mathf.Sqrt(s * (s - a) * (s - b) * (s - c));
        return area;
    }

    float BarycentricInterpolation(Vector3 point, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        // Calculate the barycentric coordinates of the point
        float s = CalculateTriangleArea(v0, v1, v2);
        float s1 = CalculateTriangleArea(point, v1, v2) / s;
        float s2 = CalculateTriangleArea(point, v2, v0) / s;
        float s3 = CalculateTriangleArea(point, v0, v1) / s;

        // Calculate the height of the point using the barycentric coordinates and the heights of the vertices
        float height = s1 * v0.y + s2 * v1.y + s3 * v2.y;
        return height;
    }


    Vertex[] GenerateVertices()
    {
        // Create an array to store the vertex data
        Vertex[] vertices = new Vertex[terrainSize * terrainSize];

        // Set the height of each vertex based on the distance to the nearest point
        // float levelDistance = 0.1f;
        foreach (Vector3 point in points)
        {
            for (int x = 0; x < terrainSize; x++)
            {
                for (int y = 0; y < terrainSize; y++)
                {
                    float distance = Vector3.Distance(new Vector3(x, 0, y), point);
                    if (distance < levelDistance)
                    {
                        vertices[x + y * terrainSize] = new Vertex
                        {
                            // position = new Vector3(x, point.y, y),
                            position = new Vector3(x, 0, y),
                        };
                    }
                    else
                    {
                        // Generate the initial terrain using Perlin noise
                        float noiseHeight = 0;
                        float frequency = 1;
                        float amplitude = 1;
                        float sampleX = x / noiseScale * frequency;

                        for (int i = 0; i < octaves; i++)
                        {
                            float sampleY = y / noiseScale * frequency;

                            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                            noiseHeight += perlinValue * amplitude;

                            amplitude *= persistence;
                            frequency *= lacunarity;
                        }

                        vertices[x + y * terrainSize] = new Vertex
                        {
                            position = new Vector3(x, noiseHeight * terrainHeight, y),
                        };


                        //

                        for (int i = 0; i < points.Length; i++)
                        {
                            Debug.Log("x: " + x + ", y: " + y + "Point: " + i + ": X: " + points[i].x + " Y: " + points[i].z);


                            if (x == (int)points[i].x && y == (int)points[i].z)
                            {
                                Debug.Log("Point: " + i + ": elevation: " + points[i].y);

                                vertices[x + y * terrainSize].position.y = points[i].y;
                            }
                        }


                        //
                        if (bFlattenCurve)
                        {
                            // Use the flatten curve to control the height of the terrain
                            vertices[x + y * terrainSize].position.y = flattenCurve.Evaluate(noiseHeight) * terrainHeight;
                        }
                    }
                }
            }
        }

        return vertices;
    }


    // Vertex[] GenerateVertices()
    // {
    //     // Create an array to store the vertex data
    //     Vertex[] vertices = new Vertex[terrainSize * terrainSize];

    //     // Set the height of each vertex based on the distance to the nearest point on the x and z axis
    //     foreach (Vector3 point in points)
    //     {
    //         for (int x = 0; x < terrainSize; x++)
    //         {
    //             for (int y = 0; y < terrainSize; y++)
    //             {
    //                 float distanceX = Mathf.Abs(x - point.x);
    //                 float distanceZ = Mathf.Abs(y - point.z);
    //                 if (distanceX < levelDistance || distanceZ < levelDistance)
    //                 {
    //                     vertices[x + y * terrainSize] = new Vertex
    //                     {
    //                         position = new Vector3(x, point.y, y),
    //                     };
    //                 }
    //                 else
    //                 {
    //                     // Generate the initial terrain using Perlin noise
    //                     float noiseHeight = 0;
    //                     float frequency = 1;
    //                     float amplitude = 1;
    //                     float sampleX = x / noiseScale * frequency;

    //                     for (int i = 0; i < octaves; i++)
    //                     {
    //                         float sampleY = y / noiseScale * frequency;

    //                         float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
    //                         noiseHeight += perlinValue * amplitude;

    //                         amplitude *= persistence;
    //                         frequency *= lacunarity;
    //                     }

    //                     vertices[x + y * terrainSize] = new Vertex
    //                     {
    //                         position = new Vector3(x, noiseHeight * terrainHeight, y),
    //                     };

    //                     if (bFlattenCurve)
    //                     {
    //                         // Use the flatten curve to control the height of the terrain
    //                         vertices[x + y * terrainSize].position.y = flattenCurve.Evaluate(noiseHeight) * terrainHeight;
    //                     }
    //                 }
    //             }
    //         }
    //     }
    //     return vertices;
    // }


    // Vertex[] GenerateVertices()
    // {
    //     // Create an array to store the vertex data
    //     Vertex[] vertices = new Vertex[terrainSize * terrainSize];

    //     // Generate the initial terrain using Perlin noise
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

    //                 float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
    //                 noiseHeight += perlinValue * amplitude;

    //                 amplitude *= persistence;
    //                 frequency *= lacunarity;
    //             }

    //             // Set the height and color of the current point
    //             vertices[x + y * terrainSize] = new Vertex
    //             {
    //                 position = new Vector3(x, noiseHeight * terrainHeight, y),
    //             };

    //             if (bFlattenCurve)
    //             {
    //                 // Use the flatten curve to control the height of the terrain
    //                 vertices[x + y * terrainSize].position.y = flattenCurve.Evaluate(noiseHeight) * terrainHeight;
    //             }
    //         }
    //     }

    //     // Blend the initial terrain with the level terrain around the points
    //     foreach (Vector3 point in points)
    //     {
    //         for (int x = 0; x < terrainSize; x++)
    //         {
    //             for (int y = 0; y < terrainSize; y++)
    //             {
    //                 float distance = Vector3.Distance(vertices[x + y * terrainSize].position, point);
    //                 if (distance < blendRadius)
    //                 {
    //                     float blendFactor = 1 - (distance / blendRadius);
    //                     vertices[x + y * terrainSize].position.y = Mathf.Lerp(vertices[x + y * terrainSize].position.y, point.y, blendFactor);
    //                 }
    //             }
    //         }
    //     }

    //     return vertices;
    // }




    // Vertex[] GenerateVertices()
    // {
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

    //                 float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
    //                 noiseHeight += perlinValue * amplitude;

    //                 amplitude *= persistence;
    //                 frequency *= lacunarity;
    //             }

    //             // Set the height and color of the current point
    //             vertices[x + y * terrainSize] = new Vertex
    //             {
    //                 position = new Vector3(x, noiseHeight * terrainHeight, y),
    //             };

    //             if (bFlattenCurve)
    //             {
    //                 // Use the flatten curve to control the height of the terrain
    //                 vertices[x + y * terrainSize].position.y = flattenCurve.Evaluate(noiseHeight) * terrainHeight;
    //             }

    //             // Smooth out the terrain near the points
    //             foreach (Vector3 point in points)
    //             {
    //                 float distance = Vector3.Distance(vertices[x + y * terrainSize].position, point);
    //                 if (distance < minHeight)
    //                 {
    //                     vertices[x + y * terrainSize].position.y = point.y;
    //                 }
    //                 else if (distance < maxHeight)
    //                 {
    //                     vertices[x + y * terrainSize].position.y = Mathf.Lerp(vertices[x + y * terrainSize].position.y, point.y, (distance - minHeight) / (maxHeight - minHeight));
    //                 }
    //             }
    //         }
    //     }

    //     return vertices;
    // }

    // Vector3[] GetVertexPositions(Vertex[] vertices)
    // {
    //     // Create an array to store the vertex positions
    //     Vector3[] vertexPositions = new Vector3[vertices.Length];

    //     // Iterate through the vertices and store the position of each vertex
    //     for (int i = 0; i < vertices.Length; i++)
    //     {
    //         vertexPositions[i] = vertices[i].position;
    //     }

    //     return vertexPositions;
    // }

    Vector3[] GetVertexPositions(Vertex[] vertices)
    {
        Vector3[] vertexPositions = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            // Check if the vertex is within the blend radius of any of the points
            foreach (Vector3 point in points)
            {
                if (Vector3.Distance(vertices[i].position, point) < blendRadius)
                {
                    // Blend the vertex position with the point position based on the distance
                    float blend = 1 - Vector3.Distance(vertices[i].position, point) / blendRadius;
                    vertices[i].position = Vector3.Lerp(vertices[i].position, point, blend);
                    break;
                }
            }
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
                triangles[index] = x + y * terrainSize;
                triangles[index + 1] = x + (y + 1) * terrainSize;
                triangles[index + 2] = x + 1 + y * terrainSize;

                triangles[index + 3] = x + 1 + y * terrainSize;
                triangles[index + 4] = x + (y + 1) * terrainSize;
                triangles[index + 5] = x + 1 + (y + 1) * terrainSize;

                index += 6;
            }
        }

        return triangles;
    }

    Vector2[] GenerateUVs()
    {
        // Create an array to store the UV data
        Vector2[] uvs = new Vector2[vertices.Length];

        // Iterate through the vertices and set the UV data for each vertex
        for (int x = 0; x < terrainSize; x++)
        {
            for (int y = 0; y < terrainSize; y++)
            {
                uvs[x + y * terrainSize] = new Vector2(x / (float)terrainSize, y / (float)terrainSize);
            }
        }

        return uvs;
    }


    List<Vector3> CalculatePlacementPoints()
    {
        // Sort the elevations and vertices arrays
        // elevations = elevations.OrderBy(x => x).ToArray();
        vertices = vertices.OrderBy(x => x.position.y).ToArray();

        // Calculate the scale of the mesh transform
        Vector3 scale = transform.lossyScale;

        // Create a list to store the placement points
        List<Vector3> placementPoints = new List<Vector3>();

        // Iterate through the vertices and add the points that meet the placement rules
        for (int i = 0; i < vertices.Length; i++)
        {

            // Transform the position of the vertex to world space
            Vector3 worldPosition = transform.TransformPoint(vertices[i].position);
            placementPoints.Add(worldPosition);

        }

        return placementPoints;
    }

    void OnDrawGizmos()
    {
        // Draw a sphere at each point's position
        foreach (Vector3 point in points)
        {
            Gizmos.color = pointColor;
            Vector3 worldPosition = transform.TransformPoint(point);
            Gizmos.DrawSphere(worldPosition, 6f);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(point, levelDistance);
        }
    }

    public bool updatePointElevation = false;
    void OnValidate()
    {
        if (numPoints != points.Length)
        {
            GeneratePoints();
        }
        if (updatePointElevation)
        {
            updatePointElevation = false;
            UpdatePointElevation();
        }

    }

}