using UnityEngine;

public class TerrainPointDistributor : MonoBehaviour
{
    public int terrainSize = 64;

    public int pointCount = 10;
    public float minHeight = 0f;
    public float maxHeight = 10f;
    public bool drawGizmos = true;

    public MeshFilter meshFilter;
    public Mesh mesh;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.sharedMesh;
    }


    void OnValidate()
    {
        meshFilter = GetComponent<MeshFilter>();

        if (meshFilter != null)
        {
            mesh = meshFilter.sharedMesh;
        }

        // Get the mesh from the attached mesh filter
        // Mesh mesh = GetComponent<MeshFilter>().sharedMesh;

        if (!mesh) return;

        // Get the terrain bounds
        Bounds bounds = mesh.bounds;

        // Get the terrain size
        Vector3 size = bounds.size;

        // Generate evenly distributed points on the terrain
        points = EvenlyDistributePoints(pointCount, size.x, size.z, minHeight, maxHeight);

        // // Draw gizmos at each point
        // if (drawGizmos)
        // {
        //     foreach (Vector3 point in points)
        //     {
        //         Gizmos.DrawWireSphere(point, 0.5f);
        //     }
        // }
    }

    Vector3[] EvenlyDistributePoints(int pointCount, float width, float depth, float minHeight, float maxHeight)
    {
        Vector3[] newPoints = new Vector3[pointCount];

        // Calculate the cell size based on the number of points and the terrain size
        float cellSize = Mathf.Sqrt(width * depth / pointCount);

        // Iterate through the points
        for (int i = 0; i < pointCount; i++)
        {
            // Calculate the x and z position of the point
            float x = (i % (int)(width / cellSize)) * cellSize + cellSize / 2;
            float z = (i / (int)(width / cellSize)) * cellSize + cellSize / 2;

            // Get the height at this position
            float y = GetHeight(x, z);

            // Clamp the height to the min and max height
            y = Mathf.Clamp(y, minHeight, maxHeight);

            // Store the point
            newPoints[i] = new Vector3(x, y, z);
        }

        return newPoints;
    }

    float GetHeight(float x, float z)
    {
        // Get the mesh from the attached mesh filter
        // Mesh mesh = GetComponent<MeshFilter>().mesh;

        // Get the terrain size
        Vector3 size = mesh.bounds.size;

        // Calculate the UV coordinates based on the terrain size and the x and z position
        Vector2 uv = new Vector2(x / size.x, z / size.z);

        // Get the height at this UV position
        float height = mesh.vertices[(int)(uv.x * terrainSize) + (int)(uv.y * terrainSize) * terrainSize].y;

        // Convert the height from local to world space
        height *= transform.localScale.y;
        height += transform.position.y;

        // Return the height
        return height;
    }


    // float GetHeight(float x, float z)
    // {
    //     // Get the mesh from the attached mesh filter
    //     Mesh mesh = GetComponent<MeshFilter>().mesh;

    //     // Convert the x and z position to UV coordinates
    //     Vector2 uv = new Vector2(x, z);
    //     uv = uv - new Vector2(transform.position.x, transform.position.z); //transform.position;
    //     uv = uv / mesh.bounds.size.x;
    //     uv = (uv + Vector2.one) / 2f;

    //     // Get the height at this UV position
    //     float height = mesh.vertices[(int)(uv.x * terrainSize) + (int)(uv.y * terrainSize) * terrainSize].y;

    //     // Convert the height from local to world space
    //     height *= transform.localScale.y;
    //     height += transform.position.y;

    //     // Return the height
    //     return height;
    // }

    public Vector3[] points;
    private void OnDrawGizmos()
    {
        if (points.Length > 0)
        {
            // Draw gizmos at each point
            if (drawGizmos)
            {
                foreach (Vector3 point in points)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(point, 0.5f);
                }
            }
        }
    }
}
