using UnityEngine;

[ExecuteInEditMode]
public class BuildingGenerator : MonoBehaviour
{
    // Define the size of the building
    public float width = 10f;
    public float height = 20f;

    void Update()
    {
        // Generate the vertices of the building
        Vector3[] vertices = new Vector3[] {
            // Bottom left corner
            new Vector3(-width / 2f, 0f, -width / 2f),
            // Bottom right corner
            new Vector3(width / 2f, 0f, -width / 2f),
            // Top left corner
            new Vector3(-width / 2f, height, -width / 2f),
            // Top right corner
            new Vector3(width / 2f, height, -width / 2f),
            // Top front left corner
            new Vector3(-width / 2f, height, width / 2f),
            // Top front right corner
            new Vector3(width / 2f, height, width / 2f)
        };

        // Generate the triangles of the building
        int[] triangles = new int[] {
            // Left side
            0, 2, 3,
            3, 1, 0,
            // Right side
            1, 3, 5,
            5, 4, 1,
            // Front side
            4, 5, 2,
            2, 0, 4,
            // Back side
            0, 1, 4,
            4, 2, 0
        };

        // Modify the vertices of the building using Perlin noise
        for (int i = 0; i < vertices.Length; i++)
        {
            float x = vertices[i].x;
            float y = vertices[i].y;
            float z = vertices[i].z;
            float noise = Mathf.PerlinNoise(x * 0.1f, z * 0.1f) * 0.2f;
            vertices[i] = new Vector3(x, y + noise, z);
        }

        // Create a new mesh and assign the vertices and triangles
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // Calculate the normals for the mesh
        mesh.RecalculateNormals();

        // Assign the mesh to a MeshFilter component
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;
    }
}
