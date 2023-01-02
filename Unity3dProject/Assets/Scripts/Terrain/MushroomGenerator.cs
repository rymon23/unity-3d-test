using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MushroomGenerator : MonoBehaviour
{
    public float noiseScale = 0.1f;

    // Parameters that control the shape of the mushroom
    public float capHeight = 1.0f;
    public float capRadius = 1.0f;
    public float stemHeight = 1.0f;
    public float stemRadius = 0.5f;
    public int numVertices = 12;

    // Old parameter values
    float oldCapHeight;
    float oldCapRadius;
    float oldStemHeight;
    float oldStemRadius;
    int oldNumVertices;

    // Mesh data
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;

    void Start()
    {
        // Create a new mesh
        mesh = new Mesh();
        mesh.name = "Mushroom Mesh";

        // Generate the initial mesh data
        UpdateMesh();
    }

    void Update()
    {
        // Check if any of the parameters have changed
        if (capHeight != oldCapHeight || capRadius != oldCapRadius || stemHeight != oldStemHeight || stemRadius != oldStemRadius || numVertices != oldNumVertices)
        {
            // Update the old parameter values
            oldCapHeight = capHeight;
            oldCapRadius = capRadius;
            oldStemHeight = stemHeight;
            oldStemRadius = stemRadius;
            oldNumVertices = numVertices;

            // Update the mesh
            UpdateMesh();
        }
    }

    void UpdateMesh()
    {

        // Generate the vertices and triangles for the mesh
        vertices = GenerateVertices();
        triangles = GenerateTriangles();

        Debug.Log("triangles: " + triangles.Length + "  vertices: " + vertices.Length);

        // Assign the vertices and triangles to the mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // Recalculate the normals to ensure proper lighting
        mesh.RecalculateNormals();
        // Apply the mesh data to the MeshFilter component
        GetComponent<MeshFilter>().mesh = mesh;
    }

    // Vector3[] GenerateVertices()
    // {
    //     // Calculate the number of vertices in the mushroom
    //     int numCapVertices = numVertices + 1;
    //     int numStemVertices = numVertices * 2;
    //     int numVerts = numCapVertices + numStemVertices;

    //     // Create an array to store the vertices
    //     Vector3[] vertices = new Vector3[numVerts];

    //     // Generate the vertices for the mushroom cap
    //     for (int i = 0; i < numCapVertices; i++)
    //     {
    //         float angle = (float)i / numVertices * Mathf.PI * 2;
    //         vertices[i] = new Vector3(Mathf.Cos(angle) * capRadius, capHeight, Mathf.Sin(angle) * capRadius);
    //     }

    //     // Generate the vertices for the mushroom stem
    //     for (int i = 0; i < numStemVertices; i++)
    //     {
    //         float angle = (float)i / numVertices * Mathf.PI * 2;
    //         vertices[numCapVertices + i] = new Vector3(Mathf.Cos(angle) * stemRadius, -stemHeight / 2 + stemHeight * (float)i / numStemVertices, Mathf.Sin(angle) * stemRadius);
    //     }

    //     return vertices;
    // }


    int[] GenerateTriangles()
    {
        // Calculate the number of triangles in the mushroom
        int numCapTriangles = numVertices * 3;
        int numStemTriangles = numVertices * 6;
        int numTris = numCapTriangles + numStemTriangles;

        // Create an array to store the triangles
        int[] triangles = new int[numTris];

        // Generate the triangles for the mushroom cap
        for (int i = 0; i < numVertices; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        // Generate the triangles for the mushroom stem
        for (int i = 0; i < numVertices; i++)
        {
            triangles[numCapTriangles + i * 6] = numVertices + 1 + i;
            triangles[numCapTriangles + i * 6 + 1] = numVertices + 2 + i;
            triangles[numCapTriangles + i * 6 + 2] = numVertices + 3 + i;
            triangles[numCapTriangles + i * 6 + 3] = numVertices + 3 + i;
            triangles[numCapTriangles + i * 6 + 4] = numVertices + 2 + i;
            triangles[numCapTriangles + i * 6 + 5] = numVertices + 4 + i;
        }

        return triangles;
    }
    Vector3[] GenerateVertices()
    {
        // Calculate the number of vertices in the mushroom
        int numCapVertices = numVertices + 1;
        int numStemVertices = numVertices * 2;
        int numVerts = numCapVertices + numStemVertices;

        // Create an array to store the vertices
        Vector3[] vertices = new Vector3[numVerts];

        // Generate the vertices for the mushroom cap
        for (int i = 0; i < numCapVertices; i++)
        {
            float angle = (float)i / numVertices * Mathf.PI * 2;
            float noise = Mathf.PerlinNoise(Mathf.Cos(angle) * noiseScale, Mathf.Sin(angle) * noiseScale);
            vertices[i] = new Vector3(Mathf.Cos(angle) * capRadius, capHeight * noise, Mathf.Sin(angle) * capRadius);
        }

        // Generate the vertices for the mushroom stem
        for (int i = 0; i < numStemVertices; i++)
        {
            float angle = (float)i / numVertices * Mathf.PI * 2;
            float noise = Mathf.PerlinNoise(Mathf.Cos(angle) * noiseScale, Mathf.Sin(angle) * noiseScale);
            vertices[numCapVertices + i] = new Vector3(Mathf.Cos(angle) * stemRadius, -stemHeight / 2 + stemHeight * (float)i / numStemVertices, Mathf.Sin(angle) * stemRadius);
        }

        return vertices;
    }

}
