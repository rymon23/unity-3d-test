using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class ProceduralTerrainMesh2 : MonoBehaviour
{
    public int terrainSize = 64;
    public int octaves = 6;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public float terrainHeight = 10f;
    public float noiseScale = 50f;

    public bool bFlattenCurve;
    public AnimationCurve flattenCurve;

    Mesh mesh;

    void Start()
    {
        mesh = new Mesh();
        mesh.name = "Procedural Terrain";

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



    void SaveMeshAsset(Mesh mesh, string assetName)
    {
        // Create a new mesh asset
        Mesh meshAsset = Instantiate(mesh) as Mesh;
        meshAsset.name = assetName;

        // Save the mesh asset to the project
        AssetDatabase.CreateAsset(meshAsset, "Assets/Terrain/" + assetName + ".asset");
        AssetDatabase.SaveAssets();
    }

    public bool bSaveMesh = false;
    void OnValidate()
    {
        if (bSaveMesh)
        {
            bSaveMesh = false;
            SaveMeshAsset(mesh, "New Mesh");
        }
    }

}

// Vertex struct to store the position and color of each vertex
// struct Vertex
// {
//     public Vector3 position;
// }
