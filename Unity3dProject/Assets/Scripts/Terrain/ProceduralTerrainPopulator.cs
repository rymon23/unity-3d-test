using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ProceduralTerrainPopulator : MonoBehaviour
{
    public int terrainSize = 64;
    public float terrainHeight = 10f;
    public float cellSize = 1f;
    public int octaves = 6;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public float noiseScale = 50f;
    public float heightScale = 1f;
    public float frequency;
    // The amplitude of the noise
    public float amplitude;
    // The minimum height of the terrain
    public float minHeight;
    // The maximum height of the terrain
    public float maxHeight;
    // The material to use for the terrain
    public Material material;
    // The gameobject to place on the terrain
    // public GameObject objectToPlace;
    // The number of objects to place on the terrain
    public int numObjects;
    // The minimum height at which to place objects
    public float minHeightForObjects;
    // The maximum height at which to place objects
    public float maxHeightForObjects;
    // The target number of points to evenly distribute on the terrain
    public int targetNumPoints;

    // The mesh data for the terrain
    Mesh mesh;
    // The list of vertices for the terrain
    Vertex[] vertices;
    // The list of triangles for the terrain
    int[] triangles;
    // The list of UVs for the terrain
    Vector2[] uvs;
    // The list of points on the terrain
    List<Vector3> points;
    // The list of evenly distributed points on the terrain
    List<Vector3> evenlyDistributedPoints;
    // The list of placed objects on the terrain
    // List<GameObject> placedObjects;

    void Start()
    {
        // Initialize the mesh data
        mesh = new Mesh();
        vertices = new Vertex[terrainSize * terrainSize];
        triangles = new int[(terrainSize - 1) * (terrainSize - 1) * 6];
        uvs = new Vector2[terrainSize * terrainSize];
        points = new List<Vector3>();
        evenlyDistributedPoints = new List<Vector3>();
        // placedObjects = new List<GameObject>();
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

        // Clear the list of points
        points.Clear();

        // Iterate through the vertices and store the position of each vertex
        for (int y = 0; y < terrainSize; y++)
        {
            for (int x = 0; x < terrainSize; x++)
            {
                points.Add(vertices[x + y * terrainSize].position);
            }
        }

        // Clear the list of evenly distributed points
        evenlyDistributedPoints.Clear();

        // Distribute the points evenly on the terrain
        evenlyDistributedPoints = EvenlyDistributePoints(points, targetNumPoints);

        // Clear the list of placed objects
        // placedObjects.Clear();

        // Place the objects on the terrain
        // for (int i = 0; i < numObjects; i++)
        // {
        //     // Choose a random point from the evenly distributed points
        //     int index = Random.Range(0, evenlyDistributedPoints.Count);
        //     Vector3 point = evenlyDistributedPoints[index];
        //     // Remove the chosen point from the list
        //     evenlyDistributedPoints.RemoveAt(index);

        //     // Check if the point is within the desired height range
        //     if (point.y >= minHeightForObjects && point.y <= maxHeightForObjects)
        //     {
        //         // Instantiate the object at the point
        //         GameObject obj = Instantiate(objectToPlace, point, Quaternion.identity);
        //         // Add the object to the list of placed objects
        //         placedObjects.Add(obj);
        //     }
        // }
    }
    Vertex[] GenerateVertices()
    {
        // Initialize the list of vertices
        Vertex[] vertices = new Vertex[terrainSize * terrainSize];

        // Iterate through the vertices and generate their position and height
        for (int y = 0; y < terrainSize; y++)
        {
            for (int x = 0; x < terrainSize; x++)
            {
                // Calculate the position of the vertex
                Vector3 position = new Vector3(x, 0, y);
                // Calculate the height of the vertex
                float height = CalculateHeight(position);

                // Flatten the terrain if the height falls within a certain range
                if (height >= minHeight && height <= maxHeight)
                {
                    height = 0;
                }

                // Store the vertex data
                vertices[x + y * terrainSize] = new Vertex(position, height);
            }
        }

        return vertices;
    }

    float CalculateHeight(Vector3 position)
    {
        // Initialize the height to 0
        float height = 0;

        // Generate the noise for the position
        float noise = Mathf.PerlinNoise(position.x / noiseScale, position.z / noiseScale);
        // Add the noise to the height
        height += noise * amplitude;

        // Iterate through the octaves and add them to the height
        for (int i = 0; i < octaves; i++)
        {
            // Increase the frequency with each octave
            float frequency = this.frequency * (i + 1);
            // Decrease the amplitude with each octave
            float amplitude = this.amplitude / (i + 1);
            // Generate the noise for the position
            float octaveNoise = Mathf.PerlinNoise(position.x / noiseScale * frequency, position.z / noiseScale * frequency);
            // Add the noise to the height
            height += octaveNoise * amplitude;
        }

        return height;
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
        // Initialize the list of triangles
        int[] triangles = new int[(terrainSize - 1) * (terrainSize - 1) * 6];

        // Iterate through the vertices and create triangles between them
        for (int y = 0; y < terrainSize - 1; y++)
        {
            for (int x = 0; x < terrainSize - 1; x++)
            {
                // Calculate the indices for the vertices of the triangle
                int index = (x + y * (terrainSize - 1)) * 6;
                triangles[index] = x + y * terrainSize;
                triangles[index + 1] = x + (y + 1) * terrainSize;
                triangles[index + 2] = (x + 1) + y * terrainSize;
                triangles[index + 3] = (x + 1) + y * terrainSize;
                triangles[index + 4] = x + (y + 1) * terrainSize;
                triangles[index + 5] = (x + 1) + (y + 1) * terrainSize;
            }
        }

        return triangles;
    }

    Vector2[] GenerateUVs()
    {
        // Initialize the list of UVs
        Vector2[] uvs = new Vector2[terrainSize * terrainSize];

        // Iterate through the vertices and calculate their UVs
        for (int y = 0; y < terrainSize; y++)
        {
            for (int x = 0; x < terrainSize; x++)
            {
                // Calculate the UVs for the vertex
                Vector2 uv = new Vector2((float)x / terrainSize, (float)y / terrainSize);
                // Store the UVs
                uvs[x + y * terrainSize] = uv;
            }
        }

        return uvs;
    }

    float GetHeight(float x, float z)
    {
        // Calculate the normalized position of the point within the terrain
        float normX = (x - transform.position.x + terrainSize / 2 * cellSize) / (terrainSize * cellSize);
        float normZ = (z - transform.position.z + terrainSize / 2 * cellSize) / (terrainSize * cellSize);

        // Calculate the height of the terrain at this point
        float height = Mathf.PerlinNoise(normX * noiseScale, normZ * noiseScale) * heightScale;

        // // Flatten the terrain if the height falls within a certain range
        // if (height < flattenHeightMin)
        // {
        //     height = flattenHeightMin;
        // }
        // else if (height > flattenHeightMax)
        // {
        //     height = flattenHeightMax;
        // }

        return height;
    }


    List<Vector3> EvenlyDistributePoints(List<Vector3> points, int numPoints)
    {
        // Calculate the bounds of the terrain
        float minX = transform.position.x - terrainSize / 2 * cellSize;
        float maxX = transform.position.x + terrainSize / 2 * cellSize;
        float minZ = transform.position.z - terrainSize / 2 * cellSize;
        float maxZ = transform.position.z + terrainSize / 2 * cellSize;

        // Calculate the step size for each point
        float stepX = (maxX - minX) / (numPoints - 1);
        float stepZ = (maxZ - minZ) / (numPoints - 1);

        // Iterate through the points and add them to the list
        for (int i = 0; i < numPoints; i++)
        {
            // Calculate the position of the point
            float x = minX + i * stepX;
            float z = minZ + i * stepZ;
            // Get the height of the terrain at this position
            float y = GetHeight(x, z);
            // Add the point to the list
            points.Add(new Vector3(x, y, z));
        }

        return points;
    }

    void UpdateMesh()
    {
        // Generate the vertices, triangles, and UVs for the mesh
        Vertex[] vertices = GenerateVertices();
        int[] triangles = GenerateTriangles();
        Vector2[] uvs = GenerateUVs();

        // Assign the data to the mesh
        mesh.vertices = GetVertexPositions(vertices);
        mesh.triangles = triangles;
        mesh.uv = uvs;

        // Recalculate the normals for the mesh
        mesh.RecalculateNormals();
    }

    void OnValidate()
    {
        // Update the mesh when a parameter is changed in the editor
        UpdateMesh();
    }

    // A struct to store vertex data
    struct Vertex
    {
        public Vector3 position;
        public float height;

        public Vertex(Vector3 position, float height)
        {
            this.position = position;
            this.height = height;
        }
    }
}