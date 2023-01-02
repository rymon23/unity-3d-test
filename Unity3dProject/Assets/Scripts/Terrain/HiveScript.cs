using UnityEngine;

public class HiveScript : MonoBehaviour
{
    void Start()
    {
        // Generate the hive mesh
        Mesh hiveMesh = GenerateHiveMesh();

        // Assign the hive mesh to the GameObject's MeshFilter component
        GetComponent<MeshFilter>().mesh = hiveMesh;

        // GenerateHive();
    }

    // void GenerateHive()
    // {
    //     // Create a new GameObject and add a MeshFilter component
    //     GameObject hiveObject = new GameObject();
    //     MeshFilter meshFilter = hiveObject.AddComponent<MeshFilter>();

    //     // Generate the hive mesh using the GenerateHiveMesh method from HiveScript
    //     Mesh hiveMesh = GenerateHiveMesh();

    //     // Assign the generated mesh to the MeshFilter component
    //     meshFilter.mesh = hiveMesh;
    // }

    Mesh GenerateHiveMesh()
    {
        // Create a new mesh
        Mesh mesh = new Mesh();

        // Define the vertices of the hive
        Vector3[] vertices = new Vector3[] {
        // bottom ring of hive
        new Vector3(-1f, 0, 1f),
        new Vector3(-1f, 0, 0f),
        new Vector3(-1f, 0, -1f),
        new Vector3(0f, 0, -1f),
        new Vector3(1f, 0, -1f),
        new Vector3(1f, 0, 0f),
        new Vector3(1f, 0, 1f),
        new Vector3(0f, 0, 1f),
        // middle ring of hive
        new Vector3(-0.8f, 0.5f, 0.8f),
        new Vector3(-0.8f, 0.5f, 0f),
        new Vector3(-0.8f, 0.5f, -0.8f),
        new Vector3(0f, 0.5f, -0.8f),
        new Vector3(0.8f, 0.5f, -0.8f),
        new Vector3(0.8f, 0.5f, 0f),
        new Vector3(0.8f, 0.5f, 0.8f),
        new Vector3(0f, 0.5f, 0.8f),
        // top of hive
        new Vector3(0f, 1f, 0f)
        };
        // Define the triangles of the hive
        int[] triangles = new int[] {
            // bottom ring of hive
            0, 1, 8,
            1, 2, 8,
            2, 3, 8,
            3, 4, 8,
            4, 5, 8,
            5, 6, 8,
            6, 7, 8,
            7, 0, 8,
            // middle ring of hive
            9, 10, 17,
            10, 11, 17,
            11, 12, 17,
            12, 13, 17,
            13, 14, 17,
            14, 15, 17,
            15, 16, 17,
            16, 9, 17,
            // top of hive
            8, 17, 18
        };

        // Check for invalid indices in the triangles array
        for (int i = 0; i < triangles.Length; i++)
        {
            if (triangles[i] >= vertices.Length)
            {
                // Debug.LogError("Invalid indices in triangles array!");
                return new Mesh();
            }
        }

        // Assign the vertices and triangles to the mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // Calculate the normals for the mesh
        mesh.RecalculateNormals();

        // Return the generated mesh
        return mesh;
    }

}
