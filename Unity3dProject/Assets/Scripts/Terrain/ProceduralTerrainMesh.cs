using UnityEngine;

// public class ProceduralTerrainMesh : MonoBehaviour
// {
//     // Terrain size and height
//     public int terrainSize = 256;
//     public int terrainHeight = 64;
//     // Noise parameters
//     public float noiseScale = 0.1f;
//     public int octaves = 6;
//     public float persistence = 0.5f;
//     public float lacunarity = 2.0f;

//     void Start()
//     {
//         // Generate the mesh data
//         Vector3[] vertices = GenerateVertices();
//         int[] triangles = GenerateTriangles();
//         Vector2[] uvs = GenerateUVs();

//         // Create the mesh
//         Mesh mesh = new Mesh();
//         mesh.vertices = vertices;
//         mesh.triangles = triangles;
//         mesh.uv = uvs;

//         // Apply the mesh data to the game object
//         GetComponent<MeshFilter>().mesh = mesh;
//     }

//     // Function to generate the vertex data
//     Vector3[] GenerateVertices()
//     {
//         // Create an array to store the vertex data
//         Vector3[] vertices = new Vector3[terrainSize * terrainSize];

//         // Iterate through the vertex data and generate the heights using Perlin noise
//         for (int x = 0; x < terrainSize; x++)
//         {
//             for (int y = 0; y < terrainSize; y++)
//             {
//                 float amplitude = 1;
//                 float frequency = 1;
//                 float noiseHeight = 0;

//                 // Generate the Perlin noise
//                 for (int i = 0; i < octaves; i++)
//                 {
//                     float sampleX = x / noiseScale * frequency;
//                     float sampleY = y / noiseScale * frequency;

//                     float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
//                     noiseHeight += perlinValue * amplitude;

//                     amplitude *= persistence;
//                     frequency *= lacunarity;
//                 }

//                 // Set the height of the current point
//                 vertices[x + y * terrainSize] = new Vector3(x, noiseHeight * terrainHeight, y);
//             }
//         }

//         return vertices;
//     }

//     // Function to generate the triangle data
//     int[] GenerateTriangles()
//     {
//         // Create an array to store the triangle data
//         int[] triangles = new int[(terrainSize - 1) * (terrainSize - 1) * 6];

//         // Iterate through the triangle data and set the indices for the triangles
//         int index = 0;
//         for (int x = 0; x < terrainSize - 1; x++)
//         {
//             for (int y = 0; y < terrainSize - 1; y++)
//             {
//                 triangles[index] = x + y * terrainSize;
//                 triangles[index + 1] = x + (y + 1) * terrainSize;
//                 triangles[index + 2] = (x + 1) + y * terrainSize;

//                 triangles[index + 3] = (x + 1) + y * terrainSize;
//                 triangles[index + 4] = x + (y + 1) * terrainSize;
//                 triangles[index + 5] = (x + 1) + (y + 1) * terrainSize;

//                 index += 6;
//             }
//         }

//         return triangles;
//     }

//     // Function to generate the UV data
//     Vector2[] GenerateUVs()
//     {
//         // Create an array to store the UV data
//         Vector2[] uvs = new Vector2[terrainSize * terrainSize];

//         // Iterate through the UV data and set the UVs for each vertex
//         for (int x = 0; x < terrainSize; x++)
//         {
//             for (int y = 0; y < terrainSize; y++)
//             {
//                 uvs[x + y * terrainSize] = new Vector2(x / (float)terrainSize, y / (float)terrainSize);
//             }
//         }

//         return uvs;
//     }
// }
using UnityEditor;

[ExecuteInEditMode]
public class ProceduralTerrainMesh : MonoBehaviour
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
        mesh.vertices = GenerateVertices();
        mesh.triangles = GenerateTriangles();
        mesh.uv = GenerateUVs();
        // Recalculate the normals to ensure proper lighting
        mesh.RecalculateNormals();
        // Apply the mesh data to the MeshFilter component
        GetComponent<MeshFilter>().mesh = mesh;
    }

    // Function to generate the vertex data
    Vector3[] GenerateVertices()
    {
        // Create an array to store the vertex data
        Vector3[] vertices = new Vector3[terrainSize * terrainSize];

        // Iterate through the vertex data and set the height of each point
        for (int x = 0; x < terrainSize; x++)
        {
            for (int y = 0; y < terrainSize; y++)
            {
                float noiseHeight = 0;
                float frequency = 1;
                float amplitude = 1;
                float sampleX = x / noiseScale * frequency;

                // Add Perlin noise to the height of the current point
                for (int i = 0; i < octaves; i++)
                {
                    float sampleY = y / noiseScale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                // Set the height of the current point
                vertices[x + y * terrainSize] = new Vector3(x, noiseHeight * terrainHeight, y);
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
}
