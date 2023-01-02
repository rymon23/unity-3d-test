using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CityGenerator : MonoBehaviour
{
    // Variables to control the size and appearance of the city
    public int citySize = 64; // size of the city grid
    public int cityBlocks = 6; // number of city blocks
    public float blockSize = 10f; // size of the city blocks
    public int numBuildings = 50; // number of buildings in the city
    public float buildingHeight = 10f; // height of the buildings
    public float noiseScale = 50f; // scale of the noise function
    public bool bFlattenCurve; // flag to control the use of the flatten curve
    public AnimationCurve flattenCurve; // curve to control the height of the terrain

    Mesh mesh;

    void Start()
    {
        mesh = new Mesh();
        mesh.name = "City";

        Update();
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
    }

    // Vertex[] GenerateVertices()
    // {
    //     // Create an array to store the vertex data
    //     Vertex[] vertices = new Vertex[citySize * citySize];

    //     // Iterate through the vertex data and set the height and color of each point
    //     for (int x = 0; x < citySize; x++)
    //     {
    //         for (int y = 0; y < citySize; y++)
    //         {
    //             // Calculate the height of the current point
    //             float noiseHeight = 0;
    //             float frequency = 1;
    //             float amplitude = 1;
    //             float sampleX = x / noiseScale * frequency;

    //             for (int i = 0; i < cityBlocks; i++)
    //             {
    //                 float sampleY = y / noiseScale * frequency;

    //                 float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
    //                 noiseHeight += perlinValue * amplitude;

    //                 amplitude *= 0.5f;
    //                 frequency *= 2f;
    //             }

    //             // Set the height and color of the current point
    //             vertices[x + y * citySize] = new Vertex
    //             {
    //                 position = new Vector3(x * blockSize, 0, y * blockSize)
    //             };

    //             // Use the noise height value to determine if the current point is a building or a street
    //             float threshold = 0.5f;
    //             if (noiseHeight > threshold)
    //             {
    //                 vertices[x + y * citySize].position.y = buildingHeight;
    //             }

    //             if (bFlattenCurve)
    //             {
    //                 // Use the flatten curve to control the height of the terrain
    //                 vertices[x + y * citySize].position.y = flattenCurve.Evaluate(noiseHeight) * buildingHeight;
    //             }
    //         }
    //     }

    //     return vertices;
    // }


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
        int[] triangles = new int[(citySize - 1) * (citySize - 1) * 6];

        // Iterate through the grid and create the triangles
        int index = 0;
        for (int x = 0; x < citySize - 1; x++)
        {
            for (int y = 0; y < citySize - 1; y++)
            {
                triangles[index] = x + y * citySize;
                triangles[index + 1] = x + (y + 1) * citySize;
                triangles[index + 2] = x + 1 + y * citySize;

                triangles[index + 3] = x + 1 + y * citySize;
                triangles[index + 4] = x + (y + 1) * citySize;
                triangles[index + 5] = x + 1 + (y + 1) * citySize;

                index += 6;
            }
        }

        return triangles;
    }

    Vector2[] GenerateUVs()
    {
        // Create an array to store the UV coordinates
        Vector2[] uvs = new Vector2[citySize * citySize];

        // Iterate through the vertices and set the UV coordinates of each vertex
        for (int x = 0; x < citySize; x++)
        {
            for (int y = 0; y < citySize; y++)
            {
                uvs[x + y * citySize] = new Vector2(x / (float)citySize, y / (float)citySize);
            }
        }

        return uvs;
    }

    // Vertex[] GenerateVertices()
    // {
    //     // Create an array to store the vertex data
    //     Vertex[] vertices = new Vertex[citySize * citySize];

    //     // Iterate through the vertex data and set the height and color of each point
    //     for (int x = 0; x < citySize; x++)
    //     {
    //         for (int y = 0; y < citySize; y++)
    //         {
    //             // Calculate the height of the current point
    //             float noiseHeight = 0;
    //             float frequency = 1;
    //             float amplitude = 1;
    //             float sampleX = x / noiseScale * frequency;

    //             // Increase the number of octaves to add more variance in elevation and pattern
    //             int octaves = 8;
    //             for (int i = 0; i < octaves; i++)
    //             {
    //                 float sampleY = y / noiseScale * frequency;

    //                 float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
    //                 noiseHeight += perlinValue * amplitude;

    //                 // Adjust the persistence and lacunarity values to add more variance in elevation and pattern
    //                 amplitude *= 0.75f;
    //                 frequency *= 1.5f;
    //             }

    //             // Set the height and color of the current point
    //             vertices[x + y * citySize] = new Vertex
    //             {
    //                 position = new Vector3(x * blockSize, 0, y * blockSize)
    //             };

    //             // Use the noise height value to determine if the current point is a building or a street
    //             float threshold = 0.5f;
    //             if (noiseHeight > threshold)
    //             {
    //                 vertices[x + y * citySize].position.y = buildingHeight;
    //             }
    //             if (bFlattenCurve)
    //             {
    //                 // Use the flatten curve to control the height of the terrain
    //                 vertices[x + y * citySize].position.y = flattenCurve.Evaluate(noiseHeight) * buildingHeight;
    //             }
    //         }
    //     }

    //     return vertices;
    // }



    Vertex[] GenerateVertices()
    {
        // Create an array to store the vertex data
        Vertex[] vertices = new Vertex[citySize * citySize];

        // Iterate through the vertex data and set the height and color of each point
        for (int x = 0; x < citySize; x++)
        {
            for (int y = 0; y < citySize; y++)
            {
                // Calculate the height of the current point
                float noiseHeight = 0;
                float frequency = 1;
                float amplitude = 1;
                float sampleX = x / noiseScale * frequency;

                int octaves = 8;
                for (int i = 0; i < octaves; i++)
                {
                    float sampleY = y / noiseScale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                    noiseHeight += perlinValue * amplitude;

                    // Adjust the persistence and lacunarity values to add more variance in elevation and pattern
                    amplitude *= 0.75f;
                    frequency *= 1.5f;
                }

                // Set the height and color of the current point
                vertices[x + y * citySize] = new Vertex
                {
                    position = new Vector3(x * blockSize, 0, y * blockSize)
                };

                // Use the noise height value to determine the height of the block
                vertices[x + y * citySize].position.y = noiseHeight * buildingHeight;

                if (bFlattenCurve)
                {
                    // Use the flatten curve to control the height of the terrain
                    vertices[x + y * citySize].position.y = flattenCurve.Evaluate(noiseHeight) * buildingHeight;
                }
            }
        }

        return vertices;
    }

}