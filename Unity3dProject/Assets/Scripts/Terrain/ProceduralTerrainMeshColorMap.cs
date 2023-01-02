using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class ProceduralTerrainMeshColorMap : MonoBehaviour
{
    // Terrain size and height
    public int terrainSize = 256;
    public int terrainHeight = 64;
    // Noise parameters
    public float noiseScale = 0.1f;
    public int octaves = 6;
    public float persistence = 0.5f;
    public float lacunarity = 2.0f;

    // Mesh object to store the generated mesh data
    Mesh mesh;
    public Texture2D colorMap;
    public Gradient elevationColors;

    void Start()
    {
        // Create the mesh object
        mesh = new Mesh();

#if UNITY_EDITOR
        // Set the mesh to be dirty to trigger a rebuild in the editor
        EditorUtility.SetDirty(mesh);
#endif
    }

    void Update()
    {
        // Generate the mesh data
        Vertex[] vertices = GenerateVertices();
        mesh.vertices = GetVertexPositions(vertices);
        mesh.triangles = GenerateTriangles();
        mesh.uv = GenerateUVs();
        // Recalculate the normals to ensure proper lighting
        mesh.RecalculateNormals();
        // Apply the mesh data to the MeshFilter component
        GetComponent<MeshFilter>().mesh = mesh;

        // Set the vertex colors of the mesh renderer
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        Color[] colors = GenerateColorMap(vertices);
        renderer.sharedMaterial.color = Color.white;
        renderer.material.SetColorArray("_Colors", colors);
    }


    // Function to generate the vertex data
    Vertex[] GenerateVertices()
    {
        if (colorMap == null) colorMap = new Texture2D(terrainSize, terrainSize);

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
                    color = colorMap.GetPixel(x, y)
                };
            }
        }

        return vertices;
    }


    // Function to generate the triangle data
    int[] GenerateTriangles()
    {
        // Create an array to store the triangle data
        int[] triangles = new int[(terrainSize - 1) * (terrainSize - 1) * 6];

        // Iterate through the triangle data and set the indices for the triangles
        int index = 0;
        for (int x = 0; x < terrainSize - 1; x++)
        {
            for (int y = 0; y < terrainSize - 1; y++)
            {
                triangles[index] = x + y * terrainSize;
                triangles[index + 1] = x + (y + 1) * terrainSize;
                triangles[index + 2] = (x + 1) + y * terrainSize;

                triangles[index + 3] = (x + 1) + y * terrainSize;
                triangles[index + 4] = x + (y + 1) * terrainSize;
                triangles[index + 5] = (x + 1) + (y + 1) * terrainSize;

                index += 6;
            }
        }

        return triangles;
    }

    // Function to generate the UV data
    Vector2[] GenerateUVs()
    {
        // Create an array to store the UV data
        Vector2[] uvs = new Vector2[terrainSize * terrainSize];

        // Iterate through the UV data and set the UV coordinates for each vertex
        for (int x = 0; x < terrainSize; x++)
        {
            for (int y = 0; y < terrainSize; y++)
            {
                uvs[x + y * terrainSize] = new Vector2((float)x / terrainSize, (float)y / terrainSize);
            }
        }

        return uvs;
    }

    Color[] GenerateColorMap(Vertex[] vertices)
    {
        // Create an array to store the colors
        Color[] colors = new Color[vertices.Length];

        // Iterate through the vertices and set the color based on the elevation
        for (int i = 0; i < vertices.Length; i++)
        {
            // Get the height of the current vertex
            float height = vertices[i].position.y;
            // Get the color from the gradient based on the height
            Color color = elevationColors.Evaluate(height);
            // Set the color of the current vertex
            colors[i] = color;
        }

        return colors;
    }

    // Helper function to get the vertex positions from the Vertex array
    Vector3[] GetVertexPositions(Vertex[] vertices)
    {
        Vector3[] positions = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            positions[i] = vertices[i].position;
        }
        return positions;
    }
}
struct Vertex
{
    public Vector3 position;
    public Color color;
}