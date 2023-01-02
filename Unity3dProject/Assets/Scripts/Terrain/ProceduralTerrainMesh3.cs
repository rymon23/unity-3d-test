using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[ExecuteInEditMode]
public class ProceduralTerrainMesh3 : MonoBehaviour
{
    public int terrainSize = 64;
    public float terrainHeight = 10f;
    public float noiseScale = 50f;
    public int octaves = 6;
    [Range(-2f, 0.75f)] public float persistence = 0.45f;
    [Range(-1f, 2.6f)] public float lacunarity = 2f;
    public bool bFlattenCurve;
    public AnimationCurve flattenCurve;
    public AnimationCurve pointHeightRange;
    public float proximityThreshold = 10f;
    public Vector2 clusterAverageElevationOffset = new Vector2(3f, 3f);

    public float minHeight = 0f;
    public float maxHeight = 1f;
    public int numPoints = 10;
    public Color pointColor = Color.red;

    Mesh mesh;
    Vertex[] vertices;
    float[] elevations;

    void Start()
    {
        mesh = new Mesh();
        mesh.name = "Procedural Terrain";

        Update();
    }

    void Update()
    {
        // Debug.Log("Updating");

        // Generate the mesh data
        vertices = GenerateVertices();
        mesh.vertices = GetVertexPositions(vertices);
        mesh.triangles = GenerateTriangles();
        mesh.uv = GenerateUVs();
        // Recalculate the normals to ensure proper lighting
        mesh.RecalculateNormals();
        // Apply the mesh data to the MeshFilter component
        GetComponent<MeshFilter>().mesh = mesh;

        // Calculate the elevations of the vertices
        elevations = new float[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            elevations[i] = vertices[i].position.y;
        }
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
        Vector2[] uvs = new Vector2[vertices.Length];

        // Iterate through the vertices and set the UV coordinates
        for (int x = 0; x < terrainSize; x++)
        {
            for (int y = 0; y < terrainSize; y++)
            {
                uvs[x + y * terrainSize] = new Vector2(x / (float)terrainSize, y / (float)terrainSize);
            }
        }

        return uvs;
    }

    // Custom struct to store vertex data
    struct Vertex
    {
        public Vector3 position;
    }

    // private void OnDrawGizmos()
    // {
    //     // Check if the vertices and elevations arrays have been initialized
    //     if (vertices == null || elevations == null)
    //     {
    //         return;
    //     }

    //     Debug.Log(" OnDrawGizmos");
    //     // Calculate the elevations of the vertices
    //     elevations = new float[vertices.Length];
    //     for (int i = 0; i < vertices.Length; i++)
    //     {
    //         elevations[i] = vertices[i].position.y;
    //     }

    //     // Sort the elevations array and the corresponding vertices array
    //     elevations = elevations.OrderBy(x => x).ToArray();
    //     vertices = vertices.OrderBy(x => x.position.y).ToArray();


    //     // Calculate the interval between points
    //     int interval = vertices.Length / numPoints;

    //     // Iterate through the vertices and draw a point at the specified interval
    //     for (int i = 0; i < vertices.Length; i += interval)
    //     {
    //         Gizmos.color = pointColor;
    //         Gizmos.DrawSphere(vertices[i].position, 0.5f);
    //     }
    // }


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
            SaveMeshAsset(mesh, "New Terrain Mesh");
        }
    }

    List<Vector3> CalculatePlacementPoints()
    {
        // Sort the elevations and vertices arrays
        elevations = elevations.OrderBy(x => x).ToArray();
        vertices = vertices.OrderBy(x => x.position.y).ToArray();

        // Calculate the scale of the mesh transform
        Vector3 scale = transform.lossyScale;

        // Create a list to store the placement points
        List<Vector3> placementPoints = new List<Vector3>();

        // Iterate through the vertices and add the points that meet the placement rules
        for (int i = 0; i < vertices.Length; i++)
        {
            // Calculate the normalized height of the vertex, taking into account the x and z scale of the mesh transform
            float normalizedHeight = (vertices[i].position.y / scale.y - minHeight) / (maxHeight - minHeight);
            normalizedHeight = Mathf.Clamp(normalizedHeight, 0, 1);

            float height = pointHeightRange.Evaluate(normalizedHeight);
            if (height > 0)
            {
                // Transform the position of the vertex to world space
                Vector3 worldPosition = transform.TransformPoint(vertices[i].position);
                placementPoints.Add(worldPosition);
            }
        }

        return placementPoints;
    }




    // private void OnDrawGizmos()
    // {
    //     // Check if the vertices and elevations arrays have been initialized
    //     if (vertices == null || elevations == null)
    //     {
    //         return;
    //     }

    //     // Calculate the placement points
    //     List<Vector3> placementPoints = CalculatePlacementPoints();

    //     // Draw the points
    //     Gizmos.color = pointColor;
    //     foreach (Vector3 point in placementPoints)
    //     {
    //         Gizmos.DrawSphere(point, 0.5f);
    //     }
    // }



    private void OnDrawGizmos()
    {
        // Calculate the placement points
        List<Vector3> placementPoints = CalculatePlacementPoints();

        // Draw the placement points
        Gizmos.color = Color.red;
        foreach (Vector3 point in placementPoints)
        {
            Gizmos.DrawSphere(point, 0.25f);
        }

        // Calculate the clusters of placement points
        List<Cluster> clusters = GetClusters(placementPoints);

        // Iterate through the clusters and draw the boundaries
        Gizmos.color = Color.blue;
        foreach (Cluster cluster in clusters)
        {
            // Iterate through the points in the cluster and draw lines between adjacent points
            Vector3 previousPoint = cluster.points[0];
            for (int i = 1; i < cluster.points.Count; i++)
            {
                Vector3 currentPoint = cluster.points[i];
                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }

            // Close the boundary by drawing a line between the first and last points
            Gizmos.DrawLine(cluster.points[0], cluster.points[cluster.points.Count - 1]);



            // Calculate the center of the cluster
            Vector3 center = Vector3.zero;
            foreach (Vector3 point in cluster.points)
            {
                center += point;
            }
            center /= cluster.points.Count;

            // Draw the center point
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(center, 0.8f);

        }
    }

    // private List<Cluster> GetClusters(List<Vector3> placementPoints, float proximityThreshold)
    // {
    //     // Create a list to store the clusters
    //     List<Cluster> clusters = new List<Cluster>();

    //     // Iterate through the placement points
    //     while (placementPoints.Count > 0)
    //     {
    //         // Create a new cluster and add the first point to it
    //         Cluster cluster = new Cluster();
    //         cluster.points.Add(placementPoints[0]);
    //         placementPoints.RemoveAt(0);

    //         // Iterate through the remaining points and add them to the cluster if they are within the proximity threshold
    //         for (int i = 0; i < placementPoints.Count; i++)
    //         {
    //             Vector3 point = placementPoints[i];
    //             if (Vector3.Distance(cluster.points[0], point) <= proximityThreshold)
    //             {
    //                 cluster.points.Add(point);
    //                 placementPoints.RemoveAt(i);
    //                 i--;
    //             }
    //         }

    //         // Calculate the average elevation of the points in the cluster
    //         float sum = 0;
    //         foreach (Vector3 point in cluster.points)
    //         {
    //             sum += point.y;
    //         }
    //         cluster.averageElevation = sum / cluster.points.Count;

    //         // Add the cluster to the list
    //         clusters.Add(cluster);
    //     }

    //     return clusters;
    // }

    // private List<Cluster> GetClusters(List<Vector3> placementPoints, float proximityThreshold)
    // {
    //     // Create a list to store the clusters
    //     List<Cluster> clusters = new List<Cluster>();

    //     // Iterate through the placement points
    //     while (placementPoints.Count > 0)
    //     {
    //         // Create a new cluster and add the first point to it
    //         Cluster cluster = new Cluster();
    //         cluster.points.Add(placementPoints[0]);
    //         placementPoints.RemoveAt(0);

    //         // Iterate through the remaining points and add them to the cluster if they are within the proximity threshold
    //         for (int i = 0; i < placementPoints.Count; i++)
    //         {
    //             Vector3 point = placementPoints[i];
    //             if (Vector3.Distance(cluster.points[0], point) <= proximityThreshold)
    //             {
    //                 cluster.points.Add(point);
    //                 placementPoints.RemoveAt(i);
    //                 i--;
    //             }
    //         }

    //         // Calculate the average elevation of the points in the cluster
    //         float sum = 0;
    //         foreach (Vector3 point in cluster.points)
    //         {
    //             sum += point.y;
    //         }
    //         cluster.averageElevation = sum / cluster.points.Count;

    //         // Calculate the height range for the cluster using the height range curve
    //         float minHeight = clusterHeightRange.Evaluate(cluster.averageElevation) * terrainHeight;
    //         float maxHeight = minHeight + clusterHeightRange;

    //         // Place the points in the cluster within the height range
    //         foreach (Vector3 point in cluster.points)
    //         {
    //             point.y = Random.Range(minHeight, maxHeight);
    //         }

    //         // Add the cluster to the list
    //         clusters.Add(cluster);
    //     }

    //     return clusters;
    // }

    // private List<Cluster> GetClusters(List<Vector3> placementPoints)
    // {
    //     // Create a list to store the clusters
    //     List<Cluster> clusters = new List<Cluster>();

    //     // Iterate through the placement points
    //     while (placementPoints.Count > 0)
    //     {
    //         // Create a new cluster and add the first point to it
    //         Cluster cluster = new Cluster();
    //         cluster.points.Add(placementPoints[0]);
    //         placementPoints.RemoveAt(0);

    //         // Iterate through the remaining points and add them to the cluster if they are within the proximity threshold and the height range
    //         for (int i = 0; i < placementPoints.Count; i++)
    //         {
    //             Vector3 point = placementPoints[i];
    //             if (Vector3.Distance(cluster.points[0], point) <= proximityThreshold &&
    //                 Mathf.Abs(point.y - cluster.averageElevation) <= averageElevationOffset)
    //             {
    //                 cluster.points.Add(point);
    //                 placementPoints.RemoveAt(i);
    //                 i--;
    //             }
    //         }

    //         // Calculate the average elevation of the points in the cluster
    //         float sum = 0;
    //         foreach (Vector3 point in cluster.points)
    //         {
    //             sum += point.y;
    //         }
    //         cluster.averageElevation = sum / cluster.points.Count;

    //         // Add the cluster to the list
    //         clusters.Add(cluster);
    //     }

    //     return clusters;
    // }

    // private List<Cluster> GetClusters(List<Vector3> placementPoints)
    // {
    //     // Create a list to store the clusters
    //     List<Cluster> clusters = new List<Cluster>();

    //     // Iterate through the placement points
    //     while (placementPoints.Count > 0)
    //     {
    //         // Create a new cluster and add the first point to it
    //         Cluster cluster = new Cluster();
    //         cluster.points.Add(placementPoints[0]);
    //         placementPoints.RemoveAt(0);

    //         // Iterate through the remaining points and add them to the cluster if they are within the proximity threshold and the height range
    //         for (int i = 0; i < placementPoints.Count; i++)
    //         {
    //             Vector3 point = placementPoints[i];
    //             if (Vector3.Distance(cluster.points[0], point) <= proximityThreshold &&
    //                 Mathf.Abs(point.y - cluster.averageElevation) <= clusterHeightRange.Evaluate(cluster.averageElevation))
    //             {
    //                 cluster.points.Add(point);
    //                 placementPoints.RemoveAt(i);
    //                 i--;
    //             }
    //         }

    //         // Calculate the average elevation of the points in the cluster
    //         float sum = 0;
    //         foreach (Vector3 point in cluster.points)
    //         {
    //             sum += point.y;
    //         }
    //         cluster.averageElevation = sum / cluster.points.Count;

    //         // Add the cluster to the list
    //         clusters.Add(cluster);
    //     }

    //     return clusters;
    // }

    // private List<Cluster> GetClusters(List<Vector3> placementPoints)
    // {
    //     // Create a list to store the clusters
    //     List<Cluster> clusters = new List<Cluster>();

    //     // Iterate through the placement points
    //     while (placementPoints.Count > 0)
    //     {
    //         // Create a new cluster and add the first point to it
    //         Cluster cluster = new Cluster();
    //         cluster.points.Add(placementPoints[0]);
    //         placementPoints.RemoveAt(0);

    //         // Iterate through the remaining points and add them to the cluster if they are within the proximity threshold and the height range
    //         for (int i = 0; i < placementPoints.Count; i++)
    //         {
    //             Vector3 point = placementPoints[i];
    //             if (Vector3.Distance(cluster.points[0], point) <= proximityThreshold &&
    //                 point.y >= cluster.averageElevation - clusterAverageElevationOffset.x && point.y <= cluster.averageElevation + clusterAverageElevationOffset.y)
    //             {
    //                 cluster.points.Add(point);
    //                 placementPoints.RemoveAt(i);
    //                 i--;
    //             }
    //         }

    //         // Calculate the average elevation of the points in the cluster
    //         float sum = 0;
    //         foreach (Vector3 point in cluster.points)
    //         {
    //             sum += point.y;
    //         }
    //         cluster.averageElevation = sum / cluster.points.Count;

    //         // Add the cluster to the list
    //         clusters.Add(cluster);
    //     }

    //     return clusters;
    // }

    private List<Cluster> GetClusters(List<Vector3> placementPoints)
    {
        // Create a list to store the clusters
        List<Cluster> clusters = new List<Cluster>();

        // Iterate through the placement points
        while (placementPoints.Count > 0)
        {
            // Create a new cluster and add the first point to it
            Cluster cluster = new Cluster();
            cluster.points.Add(placementPoints[0]);
            placementPoints.RemoveAt(0);

            // Iterate through the remaining points and add them to the cluster if they are within the proximity threshold and the height range
            for (int i = 0; i < placementPoints.Count; i++)
            {
                Vector3 point = placementPoints[i];
                if (Vector3.Distance(cluster.points[0], point) <= proximityThreshold &&
                    point.y >= cluster.averageElevation - clusterAverageElevationOffset.x && point.y <= cluster.averageElevation + clusterAverageElevationOffset.y)
                {
                    cluster.points.Add(point);
                    placementPoints.RemoveAt(i);
                    i--;
                }
            }

            // Calculate the average elevation of the points in the cluster
            float sum = 0;
            foreach (Vector3 point in cluster.points)
            {
                sum += point.y;
            }
            cluster.averageElevation = sum / cluster.points.Count;

            // Add the cluster to the list if it has more than one point
            if (cluster.points.Count > 1)
            {
                clusters.Add(cluster);
            }
        }

        return clusters;
    }


}


