using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;
using System.Linq;

[ExecuteInEditMode]
public class ProceduralCityGenerator : MonoBehaviour
{
    public FastNoiseUnity fastNoiseUnity;

    public enum NoiseType { Perlin, Simplex, Value }

    public NoiseType noiseType;

    public enum VertexType { Road = 0, Block = 1 }

    struct Vertex
    {
        public Vector3 position;
        public VertexType vertexType;
    }

    [Range(8, 256)] public int terrainSize = 64;
    [Range(-32, 72)] public float terrainHeight = 24f;
    public float noiseScale = 32f;
    [Range(-2f, 0.75f)] public float persistence = 0.45f;
    [Range(-1f, 2.6f)] public float lacunarity = 2f;
    [Range(1f, 128f)] public int octaves = 6;

    public AnimationCurve flattenCurve;

    public bool bFlattenCurve;
    public AnimationCurve terrainFlattenCurve;

    [Header("Point Marker Settings")]
    [Range(0, 24)][SerializeField] private int numPoints = 4;
    [Range(0, 12)][SerializeField] private float minPointDistance = 2f;
    [SerializeField] private float minVerticePointLevelRadius = 3f;
    [SerializeField] private float maxVerticePointLevelRadius = 9f;
    [SerializeField] private float generatePointBorderXYOffeset = 2f;
    [SerializeField] private Vector2 generatePointYRange = new Vector2(0, 1);
    [SerializeField] private Color pointColor = Color.red;
    [SerializeField] private Vector3[] points;


    [Header("Cluster Marker Settings")]
    [SerializeField] private bool enableLocationBlockPlotPoints;
    [SerializeField] private bool enableRoadPlotPoints;
    [SerializeField] private bool resetCityBlockPlotPoints;
    [Range(0.1f, 48f)][SerializeField] private float clusterDistanceMax = 7f;
    [SerializeField] private int clusterCount = 0;


    #region Saved State
    float _terrainSize;
    float _terrainHeight;
    float _minPointDistance;
    float _generatePointYRangeMin;
    float _generatePointYRangeMax;
    float _generatePointBorderXYOffeset;
    float _clusterDistanceMax;
    #endregion

    Mesh mesh;
    Vertex[] vertices;
    float[] elevations;

    List<Vector3> locationLandPlotVertices = new List<Vector3>();
    List<PointCluster> locationLandPlotClusters = new List<PointCluster>();
    List<Vector3> locationRoadVertices = new List<Vector3>();

    public AnimationCurve slopeCurve;

    void Start()
    {
        mesh = new Mesh();
        mesh.name = "Procedural Terrain";

        Update();
    }

    private void Update()
    {
        if (!debug_editorUpdateTerrainOnce || _editorUpdate)
        {
            _editorUpdate = false;

            // Generate the mesh data
            vertices = GenerateVertices(points);
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
    }


    int ConvertNoiseToBinary(float noiseValue)
    {
        // If the noise value is greater than or equal to 0.5, return 1
        if (noiseValue >= 0.5f)
        {
            return 1;
        }
        // Otherwise, return 0
        else
        {
            return 0;
        }
    }


    float GetNoiseHeightValue(float x, float z)
    {
        FastNoise fastNoise = fastNoiseUnity.fastNoise;

        // Calculate the height of the current point
        float noiseHeight = 0;
        float frequency = 1;
        float amplitude = 1;
        float sampleX = x / noiseScale * frequency;

        for (int i = 0; i < octaves; i++)
        {
            float sampleY = z / noiseScale * frequency;

            float noiseValue = 0;
            if (noiseType == NoiseType.Perlin)
            {
                noiseValue = Mathf.PerlinNoise(sampleX, sampleY);
            }

            else if (noiseType == NoiseType.Simplex)
            {
                noiseValue = (float)fastNoise.GetNoise(x, z);
            }

            noiseHeight += noiseValue * amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }
        return noiseHeight;
    }


    // Adjust this value to control how close to the border the effect takes place
    public float borderDistance = 2f;
    Vertex[] GenerateVertices(Vector3[] points)
    {
        // Create an array to store the vertex data
        Vertex[] vertices = new Vertex[terrainSize * terrainSize];

        // Iterate through the vertex data and set the height and color of each point
        for (int x = 0; x < terrainSize; x++)
        {
            for (int y = 0; y < terrainSize; y++)
            {
                float noiseHeight = GetNoiseHeightValue(x, y);

                // Set the height and color of the current point
                vertices[x + y * terrainSize] = new Vertex
                {
                    position = new Vector3(x, noiseHeight * terrainHeight, y),
                    vertexType = (VertexType)ConvertNoiseToBinary(noiseHeight)
                };

                if (points != null && points.Length > 0)
                {
                    // Find the nearest point to the current vertex
                    Vector3 nearestPoint = points[0];
                    float nearestDist = float.MaxValue;
                    for (int i = 0; i < points.Length; i++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(points[i].x, points[i].z));
                        if (dist < nearestDist)
                        {
                            nearestDist = dist;
                            nearestPoint = points[i];
                        }
                    }
                    if (nearestDist < minVerticePointLevelRadius)
                    {
                        vertices[x + y * terrainSize].position.y = nearestPoint.y;
                    }

                    // If the distance to the nearest point is within the minVerticePointLevelRadius,
                    // gradually smooth the height of the vertex to match the elevation of the nearest point
                    else if (nearestDist <= maxVerticePointLevelRadius)
                    {
                        // Calculate the distance as a percentage of the minVerticePointLevelRadius
                        float distPercent = nearestDist / maxVerticePointLevelRadius;
                        // Evaluate the Animation curve using the distance percentage
                        float curveValue = flattenCurve.Evaluate(distPercent);
                        // Set the height of the vertex to be a blend of the point's y position and the original height
                        vertices[x + y * terrainSize].position.y = Mathf.Lerp(vertices[x + y * terrainSize].position.y, nearestPoint.y, curveValue);
                    }
                }
                else
                {
                    if (bFlattenCurve)
                    {
                        // Use the flatten curve to control the height of the terrain
                        vertices[x + y * terrainSize].position.y = terrainFlattenCurve.Evaluate(noiseHeight) * terrainHeight;
                    }

                    if (x < borderDistance || x > terrainSize - borderDistance || y < borderDistance || y > terrainSize - borderDistance)
                    {
                        // Calculate the distance from the current vertex to the nearest border
                        float distToBorder = Mathf.Min(x, y, terrainSize - x, terrainSize - y);
                        // Evaluate the AnimationCurve using the distance as the parameter
                        float curveValue = slopeCurve.Evaluate(distToBorder / borderDistance);
                        // Adjust the height of the vertex using the curve value
                        vertices[x + y * terrainSize].position.y *= curveValue;
                    }
                }
            }
        }
        return vertices;
    }

    public Vector3[] AnimateVertices(Vector3[] vertices)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            float scale = slopeCurve.Evaluate(i / (float)vertices.Length);
            vertices[i] = new Vector3(vertices[i].x * scale, vertices[i].y * scale, vertices[i].z * scale);
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
                triangles[index++] = x + y * terrainSize;
                triangles[index++] = x + (y + 1) * terrainSize;
                triangles[index++] = x + 1 + y * terrainSize;

                triangles[index++] = x + 1 + y * terrainSize;
                triangles[index++] = x + (y + 1) * terrainSize;
                triangles[index++] = x + 1 + (y + 1) * terrainSize;
            }
        }

        return triangles;
    }

    Vector2[] GenerateUVs()
    {
        // Create an array to store the UV data
        Vector2[] uvs = new Vector2[vertices.Length];

        // Iterate through the vertices and set the UVs of each vertex
        for (int x = 0; x < terrainSize; x++)
        {
            for (int y = 0; y < terrainSize; y++)
            {
                uvs[x + y * terrainSize] = new Vector2(x / (float)terrainSize, y / (float)terrainSize);
            }
        }

        return uvs;
    }

    void GeneratePoints()
    {
        points = new Vector3[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            // Generate a random position within the bounds of the terrain
            float xPos = Random.Range(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset);
            float zPos = Random.Range(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset);
            float yPos = Random.Range(generatePointYRange.x, generatePointYRange.y);

            points[i] = new Vector3(xPos, yPos, zPos);

            // Use the transform scale to check distance
            Vector3 scale = transform.lossyScale;

            // Ensure that the point is a minimum distance away from all other points
            bool tooClose = false;
            do
            {
                tooClose = false;
                foreach (Vector3 point in points)
                {
                    if (point != points[i] && Vector3.Distance(point, points[i]) < minPointDistance * scale.x)
                    {
                        tooClose = true;
                        xPos = Random.Range(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset);
                        zPos = Random.Range(generatePointBorderXYOffeset, terrainSize - generatePointBorderXYOffeset);
                        yPos = Random.Range(generatePointYRange.x, generatePointYRange.y);
                        points[i] = new Vector3(xPos, 0, zPos);
                        break;
                    }
                }
            } while (tooClose);
        }
    }

    void CalculatePlacementPoints()
    {
        List<Vector3> landPlotVertices = new List<Vector3>();
        List<Vector3> roadVertices = new List<Vector3>();

        if (vertices != null && vertices.Length > 0)
        {
            // // Create a list to store the placement points
            for (int i = 0; i < vertices.Length; i++)
            {
                if (vertices[i].vertexType == VertexType.Block)
                {
                    Vector3 worldPosition = transform.TransformPoint(vertices[i].position);
                    landPlotVertices.Add(worldPosition);
                }
                else if (vertices[i].vertexType == VertexType.Road)
                {
                    Vector3 worldPosition = transform.TransformPoint(vertices[i].position);
                    roadVertices.Add(worldPosition);
                }
            }
        }

        locationLandPlotVertices = landPlotVertices;
        locationRoadVertices = roadVertices;
    }


    Vector2 GetMaximumClusterSize(List<Vector3> clusterPoints, float distance)
    {
        // Sort the cluster of points by their x and z values
        clusterPoints.Sort((p1, p2) => p1.x.CompareTo(p2.x));
        clusterPoints.Sort((p1, p2) => p1.z.CompareTo(p2.z));

        float maxWidth = 0;
        float maxHeight = 0;

        // Iterate through the sorted list of points
        for (int i = 0; i < clusterPoints.Count; i++)
        {
            // Find the neighbors of the current point within the set distance
            List<Vector3> neighbors = clusterPoints.FindAll(p => p.x >= clusterPoints[i].x - distance && p.x <= clusterPoints[i].x + distance && p.z >= clusterPoints[i].z - distance && p.z <= clusterPoints[i].z + distance);

            // Calculate the width and height of the rectangle using the point and its neighbors
            float width = neighbors.Max(p => p.x) - neighbors.Min(p => p.x);
            float height = neighbors.Max(p => p.z) - neighbors.Min(p => p.z);

            // Update the maximum width and height if the calculated width or height is greater than the current value
            maxWidth = Mathf.Max(maxWidth, width);
            maxHeight = Mathf.Max(maxHeight, height);
        }

        // Return the maximum size as a Vector2
        return new Vector2(maxWidth, maxHeight);
    }

    public float clusterSizeCheckDist = 0.6f;

    // private List<PointCluster> ConsolidatePointsIntoClusters(List<Vector3> points)
    // {
    //     List<PointCluster> clusters = new List<PointCluster>();
    //     Vector3 scale = transform.lossyScale;

    //     points = points.OrderBy(pos => pos.x).ThenBy(pos => pos.z).ToList();

    //     // Iterate through all of the points
    //     for (int i = 0; i < points.Count; i++)
    //     {
    //         // Check if the current point is within an existing cluster
    //         bool foundCluster = false;
    //         foreach (PointCluster cluster in clusters)
    //         {
    //             // Calculate the distance between the current point and the center of the cluster
    //             float distance = Vector2.Distance(new Vector2(cluster.center.x, cluster.center.z), new Vector2(points[i].x, points[i].z));
    //             if (distance < clusterDistanceMax)
    //             {
    //                 // Add the point to the cluster and update the center
    //                 cluster.AddPoint(points[i]);
    //                 foundCluster = true;
    //                 break;
    //             }
    //         }

    //         // If the point was not within an existing cluster, create a new cluster for it
    //         if (!foundCluster)
    //         {
    //             PointCluster newCluster = new PointCluster(points[i]);
    //             clusters.Add(newCluster);
    //         }
    //     }

    //     foreach (PointCluster cluster in clusters)
    //     {
    //         cluster.UpdateMaximumClusterSize(clusterSizeCheckDist);

    //         List<Vector3> clusterPoints = cluster.GetPoints();

    //         Vector2 clusterSize = cluster.maxRectangleSize;
    //         // Vector2 clusterSize = GetMaximumClusterSize(clusterPoints, clusterSizeCheckDist);
    //         Debug.Log("clusterSize: " + clusterSize + ", points: " + clusterPoints.Count);
    //     }

    //     return clusters;
    // }

    private void RemoveOverlappingPoints(List<PointCluster> clusters)
    {
        // Iterate through all clusters
        for (int i = 0; i < clusters.Count; i++)
        {
            PointCluster currentCluster = clusters[i];
            List<Vector3> currentPoints = currentCluster.GetPoints();

            // Iterate through all other clusters
            for (int j = 0; j < clusters.Count; j++)
            {
                if (i == j) continue; // Skip the current cluster

                PointCluster otherCluster = clusters[j];
                List<Vector3> otherPoints = otherCluster.GetPoints();

                // Remove any points in the current cluster that are also in the other cluster
                currentPoints.RemoveAll(otherPoints.Contains);

                // Update the current cluster with the remaining points
                currentCluster.SetPoints(currentPoints);
            }
        }
    }

    private List<PointCluster> ConsolidatePointsIntoClusters(List<Vector3> points)
    {
        List<PointCluster> clusters = new List<PointCluster>();
        Vector3 scale = transform.lossyScale;

        points = points.OrderBy(pos => pos.x).ThenBy(pos => pos.z).ToList();

        // Iterate through all of the points
        for (int i = 0; i < points.Count; i++)
        {
            // Check if the current point is within an existing cluster
            bool foundCluster = false;
            foreach (PointCluster cluster in clusters)
            {
                // Calculate the distance between the current point and the center of the cluster
                float distance = Vector2.Distance(new Vector2(cluster.center.x, cluster.center.z), new Vector2(points[i].x, points[i].z));
                if (distance < clusterDistanceMax)
                {
                    // Add the point to the cluster and update the center
                    cluster.AddPoint(points[i]);
                    foundCluster = true;
                    break;
                }

                List<Vector3> clusterPoints = cluster.GetPoints();
                foreach (Vector3 clusterPoint in clusterPoints)
                {
                    distance = Vector2.Distance(new Vector2(clusterPoint.x, clusterPoint.z), new Vector2(points[i].x, points[i].z));
                    if (distance < clusterDistanceMax)
                    {
                        // Add the point to the cluster and update the center
                        cluster.AddPoint(points[i]);
                        foundCluster = true;
                        break;
                    }
                }
            }

            // If the point was not within an existing cluster, create a new cluster for it
            if (!foundCluster)
            {
                PointCluster newCluster = new PointCluster(points[i]);
                clusters.Add(newCluster);
            }
        }

        RemoveOverlappingPoints(clusters);

        foreach (PointCluster cluster in clusters)
        {
            cluster.UpdateMaximumClusterSize(clusterSizeCheckDist);

            List<Vector3> clusterPoints = cluster.GetPoints();

            Vector2 clusterSize = cluster.maxRectangleSize;
            // Vector2 clusterSize = GetMaximumClusterSize(clusterPoints, clusterSizeCheckDist);
            // Debug.Log("clusterSize: " + clusterSize + ", points: " + clusterPoints.Count);
        }

        return clusters;
    }

    private void UpdateCityLandPlotPoints()
    {
        CalculatePlacementPoints();
        locationLandPlotClusters = ConsolidatePointsIntoClusters(locationLandPlotVertices);

        // Add unique color for each cluster
        if (locationLandPlotClusters.Count > 0)
        {
            clusterCount = locationLandPlotClusters.Count;

            HashSet<Color> generatedColors = new HashSet<Color>();

            foreach (PointCluster cluster in locationLandPlotClusters)
            {
                Color randomColor;

                // Generate a random color until a unique color is found
                do
                {
                    // Generate a random hue value between 0 and 1
                    float hue = Random.value;

                    // Generate a random saturation value between 0.5 and 1
                    float saturation = Random.Range(0.5f, 1f);

                    // Generate a random value value between 0.5 and 1
                    float value = Random.Range(0.5f, 1f);

                    // Convert the HSV values to RGB
                    randomColor = Color.HSVToRGB(hue, saturation, value);
                } while (generatedColors.Contains(randomColor));

                // Add the unique color to the hash set
                generatedColors.Add(randomColor);
                cluster.color = randomColor;
            }
        }

    }

    [Header("Debug Settings")]
    [SerializeField] private bool debug_showPoints = true;
    [SerializeField] private bool debug_minPointDistance;
    [SerializeField] private bool debug_pointLevelRadius;
    [SerializeField] private bool debug_editorUpdateTerrainOnce;
    private bool _editorUpdate;
    void OnDrawGizmos()
    {
        if (!debug_showPoints && !debug_pointLevelRadius && !debug_minPointDistance) return;

        Gizmos.color = pointColor;

        // Draw the cluster points
        if (enableLocationBlockPlotPoints && !resetCityBlockPlotPoints)
        {
            // Debug.Log("cityLandPointClusters[0]: " + cityLandPointClusters[0].GetPoints().Count);


            if (locationLandPlotClusters.Count > 0)
            {

                foreach (PointCluster cluster in locationLandPlotClusters)
                {

                    Gizmos.color = cluster.color;

                    List<Vector3> pts = cluster.GetPoints();
                    foreach (Vector3 point in pts)
                    {
                        Gizmos.DrawSphere(point, 0.25f);
                    }

                    List<Vector3> baseGridPoints = cluster.gridPoints;
                    if (baseGridPoints.Count > 0)
                    {
                        foreach (Vector3 point in baseGridPoints)
                        {
                            // Vector3 pointWorldPos = transform.TransformPoint(point);
                            // Gizmos.DrawWireSphere(point, clusterSizeCheckDist);
                            Gizmos.DrawSphere(point, clusterSizeCheckDist);
                        }
                    }

                    List<PointCluster.GridPointPrototype> gridPoints = cluster.gridPointPrototypes;
                    if (gridPoints.Count > 0)
                    {
                        foreach (PointCluster.GridPointPrototype point in gridPoints)
                        {
                            // Vector3 pointWorldPos = transform.TransformPoint(point);
                            // Gizmos.DrawWireSphere(point, clusterSizeCheckDist);
                            Gizmos.DrawSphere(point.position, point.radius);

                            // if (point.radius > clusterSizeCheckDist)
                            // {

                            //     Gizmos.color = Color.black;
                            //     Gizmos.DrawWireSphere(point.position, point.radius);
                            // }
                        }
                    }


                    // Vector3[] points = cluster.maxRectangleBorderPoints;
                    // Gizmos.color = Color.red;
                    // Gizmos.DrawSphere(transform.TransformPoint(points[0]), 1f);
                    // Gizmos.DrawSphere(transform.TransformPoint(points[1]), 1f);
                    // Gizmos.DrawSphere(transform.TransformPoint(points[3]), 1f);
                    // Gizmos.DrawSphere(transform.TransformPoint(points[2]), 1f);

                    // Gizmos.DrawLine(points[0], points[1]);
                    // Gizmos.DrawLine(points[1], points[3]);
                    // Gizmos.DrawLine(points[3], points[2]);
                    // Gizmos.DrawLine(points[2], points[0]);

                    // Debug.Log("cityLandPointClusters[0]: " + cityLandPointClusters[0].GetPoints().Count);
                    // Gizmos.DrawSphere(cluster.center, 1f);
                    // Gizmos.DrawWireSphere(cluster.center, cluster.GetBoundsRadius());

                    // List<Vector3> borderPts = cluster.GetBorderPoints();
                    // foreach (Vector3 point in borderPts)
                    // {
                    //     Gizmos.DrawWireSphere(point, 2f);
                    //     // Gizmos.DrawSphere(point, 0.6f);
                    // }


                    // Matrix4x4 rotationMatrix = Matrix4x4.TRS(cluster.bounds.center, Quaternion.identity, cluster.bounds.size);
                    // Gizmos.matrix = rotationMatrix;
                    // // Gizmos.DrawWireCube(cluster.center, cluster.GetSize());
                    // Gizmos.DrawWireCube(Vector3.zero, Vector3.one);


                    // Vector3 min = cluster.bounds.max;
                    // Vector3 max = cluster.bounds.max;

                    // // Draw the edges of the box
                    // Gizmos.DrawLine(new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z));
                    // Gizmos.DrawLine(new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z));
                    // Gizmos.DrawLine(new Vector3(max.x, min.y, max.z), new Vector3(min.x, min.y, max.z));
                    // Gizmos.DrawLine(new Vector3(min.x, min.y, max.z), new Vector3(min.x, min.y, min.z));
                    // Gizmos.DrawLine(new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z));
                    // Gizmos.DrawLine(new Vector3(max.x, max.y, min.z), new Vector3(max.x, max.y, max.z));
                    // Gizmos.DrawLine(new Vector3(max.x, max.y, max.z), new Vector3(min.x, max.y, max.z));
                    // Gizmos.DrawLine(new Vector3(min.x, max.y, max.z), new Vector3(min.x, max.y, min.z));
                    // Gizmos.DrawLine(new Vector3(min.x, min.y, min.z), new Vector3(min.x, max.y, min.z));
                    // Gizmos.DrawLine(new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z));
                    // Gizmos.DrawLine(new Vector3(max.x, min.y, max.z), new Vector3(max.x, max.y, max.z));
                    // Gizmos.DrawLine(new Vector3(min.x, min.y, max.z), new Vector3(min.x, max.y, max.z));

                }


            }
            else if (locationLandPlotVertices.Count > 0)
            {
                foreach (Vector3 point in locationLandPlotVertices)
                {
                    Gizmos.DrawSphere(point, 0.25f);
                }
            }
        }

        if (enableRoadPlotPoints && !resetCityBlockPlotPoints && locationRoadVertices.Count > 0)
        {
            foreach (Vector3 point in locationRoadVertices)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawSphere(point, 0.25f);
            }
        }

        // Draw a sphere at each point's position
        foreach (Vector3 point in points)
        {
            Vector3 scale = transform.lossyScale;
            Vector3 pointWorldPos = transform.TransformPoint(point);

            if (debug_showPoints)
            {
                Gizmos.DrawSphere(pointWorldPos, 6f);
            }
            if (debug_minPointDistance)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(pointWorldPos, minPointDistance * scale.x);
            }
            if (debug_pointLevelRadius)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(pointWorldPos, minVerticePointLevelRadius * scale.x);
            }

        }
    }

    void OnValidate()
    {
        _editorUpdate = true;

        if (numPoints != points.Length ||
            _minPointDistance != minPointDistance ||
            _generatePointYRangeMin != generatePointYRange.x ||
            _generatePointYRangeMax != generatePointYRange.y ||
            _generatePointBorderXYOffeset != generatePointBorderXYOffeset
            )
        {
            _minPointDistance = minPointDistance;
            _generatePointYRangeMin = generatePointYRange.x;
            _generatePointYRangeMax = generatePointYRange.y;

            if (generatePointBorderXYOffeset > terrainSize * 0.6f)
            {
                generatePointBorderXYOffeset = terrainSize * 0.6f;
            }
            if (generatePointBorderXYOffeset < 0)
            {
                generatePointBorderXYOffeset = 0;
            }
            _generatePointBorderXYOffeset = generatePointBorderXYOffeset;

            GeneratePoints();
        }

        if (enableLocationBlockPlotPoints && resetCityBlockPlotPoints || _terrainHeight != terrainHeight || _terrainSize != terrainSize || _clusterDistanceMax != clusterDistanceMax)
        {
            _terrainHeight = terrainHeight;
            _terrainSize = terrainSize;
            _clusterDistanceMax = clusterDistanceMax;
            locationLandPlotClusters = new List<PointCluster>();

            UpdateCityLandPlotPoints();

            resetCityBlockPlotPoints = false;

            DestroyAllTiles(true);
        }


        if (generateBlockTiles && !resetCityBlockPlotPoints)
        {
            generateBlockTiles = false;
            InstantiateTiles();
        }

        // if (bSaveMesh)
        // {
        //     bSaveMesh = false;
        //     SaveMeshAsset(mesh, "New Terrain Mesh");
        // }
    }

    // [SerializeField] private bool bSaveMesh = false;
    // void SaveMeshAsset(Mesh mesh, string assetName)
    // {
    //     // Create a new mesh asset
    //     Mesh meshAsset = Instantiate(mesh) as Mesh;
    //     meshAsset.name = assetName;

    //     // Save the mesh asset to the project
    //     AssetDatabase.CreateAsset(meshAsset, "Assets/Terrain/" + assetName + ".asset");
    //     AssetDatabase.SaveAssets();
    // }

    [SerializeField] private bool generateBlockTiles;
    [SerializeField] private GameObject proceduralTilePrefab_sm;
    [SerializeField] private GameObject proceduralTilePrefab_md;
    // [SerializeField] private GameObject proceduralTilePrefab_lg;
    [SerializeField] private List<GameObject> allTiles = new List<GameObject>();

    void DestroyAllTiles(bool immediate = false)
    {
        // Destroy any existing objects in the array
        foreach (GameObject obj in allTiles)
        {
            if (immediate)
            {
                DestroyImmediate(obj);
            }
            else
            {
                Destroy(obj);
            }
        }
        // Clear the array
        allTiles.Clear();
    }

    void InstantiateTiles()
    {
        DestroyAllTiles();

        foreach (PointCluster cluster in locationLandPlotClusters)
        {
            List<PointCluster.GridPointPrototype> gridPoints = cluster.gridPointPrototypes;
            if (gridPoints.Count > 0)
            {
                foreach (PointCluster.GridPointPrototype point in gridPoints)
                {
                    GameObject prefab;
                    if (point.radius <= 6f)
                    {
                        prefab = proceduralTilePrefab_sm;
                    }
                    else
                    {
                        prefab = proceduralTilePrefab_md;
                    }
                    Vector3 pos = transform.TransformPoint(point.position);
                    Vector3 scaleXZ = transform.lossyScale;
                    Vector3 scaleY = prefab.transform.lossyScale;
                    // GameObject newObject = Instantiate(prefab, new Vector3(pos.x * scaleXZ.x, pos.y * scaleY.y, pos.z * scaleXZ.z), Quaternion.identity);
                    GameObject newObject = Instantiate(prefab, new Vector3(point.position.x, point.position.y + (scaleY.y * 0.5f), point.position.z), Quaternion.identity);

                    allTiles.Add(newObject);
                    newObject.transform.SetParent(this.gameObject.transform);
                }
            }

        }
    }

}