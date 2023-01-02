using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;


[ExecuteInEditMode]
public class ProceduralTerrainMesh4 : MonoBehaviour
{
    public int terrainSize = 64;
    public int octaves = 6;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public float terrainHeight = 10f;
    public float noiseScale = 50f;

    public bool bFlattenCurve;
    public AnimationCurve flattenCurve;
    public AnimationCurve pointHeightRange;

    public float proximityThreshold = 10f;
    public Vector2 clusterAverageElevationOffset = new Vector2(3f, 3f);

    public float minHeight = 0f;
    public float maxHeight = 1f;
    public int numPoints = 10;
    public Color pointColor = Color.red;

    Mesh mesh;
    Vertex[] vertices;
    float[] elevations;
    List<List<Vector3>> placementPoints; // added

    void Start()
    {
        mesh = new Mesh();
        mesh.name = "Procedural Terrain";

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
        // Apply the mesh data to the MeshFilter component
        GetComponent<MeshFilter>().mesh = mesh;

        // Calculate the elevations of the vertices
        elevations = new float[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            elevations[i] = vertices[i].position.y;
        }

        // Group adjacent placementPoints together if they are at similar elevation and within a set distance
        placementPoints = new List<List<Vector3>>(); // added
        for (int i = 0; i < numPoints; i++)
        {
            Vector3 point = pointHeightRange.Evaluate(elevations[i]) * vertices[i].position;
            bool placed = false;
            for (int j = 0; j < placementPoints.Count; j++)
            {
                List<Vector3> group = placementPoints[j];
                Vector3 averageElevation = new Vector3(0, group.Average(p => p.y), 0);
                // Vector3 averageElevation = group.Average(p => p.y);
                // if (Mathf.Abs(point.y - averageElevation) < clusterAverageElevationOffset.x && Vector3.Distance(point, group[0]) < proximityThreshold)
                if (Mathf.Abs(point.y - averageElevation.y) < clusterAverageElevationOffset.x && Vector3.Distance(point, group[0]) < proximityThreshold)
                {
                    group.Add(point);
                    placed = true;
                    break;
                }
            }
            if (!placed)
            {
                placementPoints.Add(new List<Vector3> { point });
            }
        }
    }


    Vertex[] GenerateVertices()
    {
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

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                // Set the height and color of the current point
                vertices[x + y * terrainSize] = new Vertex
                {
                    position = new Vector3(x, noiseHeight * terrainHeight, y),
                };

                if (bFlattenCurve)
                {
                    // Use the flatten curve to control the height of the terrain
                    vertices[x + y * terrainSize].position.y = flattenCurve.Evaluate(noiseHeight) * terrainHeight;
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
                // Add the triangle indices for the current quad
                triangles[index++] = x + y * terrainSize;
                triangles[index++] = x + (y + 1) * terrainSize;
                triangles[index++] = (x + 1) + y * terrainSize;

                triangles[index++] = (x + 1) + y * terrainSize;
                triangles[index++] = x + (y + 1) * terrainSize;
                triangles[index++] = (x + 1) + (y + 1) * terrainSize;
            }
        }

        return triangles;
    }

    Vector2[] GenerateUVs()
    {
        // Create an array to store the UV coordinates
        Vector2[] uvs = new Vector2[terrainSize * terrainSize];

        // Iterate through the vertices and set the UV coordinates of each vertex
        for (int x = 0; x < terrainSize; x++)
        {
            for (int y = 0; y < terrainSize; y++)
            {
                uvs[x + y * terrainSize] = new Vector2(x / (float)terrainSize, y / (float)terrainSize);
            }
        }

        return uvs;
    }

    void OnDrawGizmos()
    {
        if (vertices == null)
        {
            return;
        }

        // Display the placement points in the editor
        Gizmos.color = pointColor;
        for (int i = 0; i < placementPoints.Count; i++)
        {
            List<Vector3> group = placementPoints[i];
            for (int j = 0; j < group.Count; j++)
            {
                Gizmos.DrawSphere(group[j], 0.5f);
            }
        }
    }



}

