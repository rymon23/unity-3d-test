using UnityEngine;

[ExecuteInEditMode]
public class MushroomScript : MonoBehaviour
{
    //     // Mesh and mesh data
    //     Mesh mesh;
    //     Vector3[] vertices;
    //     int[] triangles;

    //     // Perlin noise parameters
    //     public float noiseScale = 50f;
    //     public int octaves = 6;
    //     public float persistence = 0.5f;
    //     public float lacunarity = 2f;

    //     // Mesh parameters
    //     public float mushroomHeight = 10f;
    //     public float mushroomScale = 1f;

    //     void Start()
    //     {
    //         // Create the mesh and set up the mesh data arrays
    //         mesh = new Mesh();
    //         mesh.name = "Mushroom Mesh";
    //         vertices = new Vector3[(int)Mathf.Pow(2, octaves) * (int)Mathf.Pow(2, octaves)];
    //         triangles = new int[(int)Mathf.Pow(2, octaves) * (int)Mathf.Pow(2, octaves) * 6];

    //         // Generate the vertices and triangles
    //         GenerateMushroom();

    //         // Assign the mesh data to the MeshFilter component
    //         GetComponent<MeshFilter>().mesh = mesh;
    //     }

    //     void GenerateMushroom()
    //     {
    //         int index = 0;
    //         float frequency = 1;
    //         float amplitude = 1;

    //         // Generate the vertices and triangles for the mesh
    //         for (int x = 0; x < Mathf.Pow(2, octaves); x++)
    //         {
    //             for (int y = 0; y < Mathf.Pow(2, octaves); y++)
    //             {
    //                 // Calculate the vertex position using Perlin noise
    //                 float noiseX = x / noiseScale * frequency;
    //                 float noiseY = y / noiseScale * frequency;
    //                 float noiseHeight = Mathf.PerlinNoise(noiseX, noiseY) * mushroomHeight;
    //                 Vector3 vertexPos = new Vector3(x, noiseHeight, y) * mushroomScale;
    //                 vertices[index] = vertexPos;

    //                 // Set the triangle indices
    //                 triangles[index * 6] = index;
    //                 triangles[index * 6 + 1] = index + 1;
    //                 triangles[index * 6 + 2] = index + (int)Mathf.Pow(2, octaves);
    //                 triangles[index * 6 + 3] = index + 1;
    //                 triangles[index * 6 + 4] = index + (int)Mathf.Pow(2, octaves) + 1;
    //                 triangles[index * 6 + 5] = index + (int)Mathf.Pow(2, octaves);

    //                 index++;
    //             }
    //         }
    //         // Assign the vertices and triangles to the mesh
    //         mesh.vertices = vertices;
    //         mesh.triangles = triangles;
    //     }
    // }



    // Mesh and mesh data
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;

    // Perlin noise parameters
    public float noiseScale = 50f;
    public int octaves = 6;
    public float persistence = 0.5f;
    public float lacunarity = 2f;

    // Mesh parameters
    public float mushroomHeight = 10f;
    public float mushroomScale = 1f;

    void Start()
    {
        // Create the mesh and set up the mesh data arrays
        mesh = new Mesh();
        mesh.name = "Mushroom Mesh";
        vertices = new Vector3[(int)Mathf.Pow(2, octaves) * (int)Mathf.Pow(2, octaves)];
        triangles = new int[vertices.Length];

        // Generate the vertices and triangles
        GenerateMushroom();

        // Assign the mesh data to the MeshFilter component
        GetComponent<MeshFilter>().mesh = mesh;
    }


    // void GenerateMushroom()
    // {
    //     int index = 0;
    //     float frequency = 1;
    //     float amplitude = 1;

    //     Debug.Log("triangles: " + triangles.Length + "vertices: " + vertices.Length);

    //     // Generate the vertices and triangles for the mesh
    //     for (int x = 0; x < Mathf.Pow(2, octaves); x++)
    //     {
    //         for (int y = 0; y < Mathf.Pow(2, octaves); y++)
    //         {
    //             // Calculate the vertex position using Perlin noise
    //             float noiseX = x / noiseScale * frequency;
    //             float noiseY = y / noiseScale * frequency;
    //             float noiseHeight = Mathf.PerlinNoise(noiseX, noiseY) * mushroomHeight;
    //             Vector3 vertexPos = new Vector3(x, noiseHeight, y) * mushroomScale;
    //             vertices[index] = vertexPos;

    //             // Set the triangle indices
    //             triangles[index * 2] = index;
    //             triangles[index * 2 + 1] = index + 1;

    //             index++;
    //         }
    //     }

    //     // Assign the vertices and triangles to the mesh
    //     mesh.vertices = vertices;
    //     mesh.triangles = triangles;
    // }

    void GenerateMushroom()
    {
        // Set the size of the triangles array to match the size of the vertices array
        triangles = new int[vertices.Length * 3];

        // Generate the triangles for the mesh
        for (int i = 0; i < vertices.Length / 3; i++)
        {
            triangles[i * 3] = i * 3;
            triangles[i * 3 + 1] = i * 3 + 1;
            triangles[i * 3 + 2] = i * 3 + 2;
        }

        Debug.Log("triangles: " + triangles.Length + "vertices: " + vertices.Length);



        // Assign the vertices and triangles to the mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
    }

}